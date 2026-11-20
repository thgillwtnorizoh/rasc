using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Rasc.Core;

/// <summary>
/// App-owned SQLite checkpoint and event ledger. First snapshot establishes a non-notifying
/// baseline. Every accepted snapshot update and derived unlock is a single transaction.
/// </summary>
public sealed class UnlockEventStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _sync = new();

    public UnlockEventStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var full = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = full, Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        _connection.Open();
        using var pragmas = _connection.CreateCommand();
        pragmas.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragmas.ExecuteNonQuery();
        using var schema = _connection.CreateCommand();
        schema.CommandText = """
            CREATE TABLE IF NOT EXISTS sources (
              source_key TEXT PRIMARY KEY,
              first_seen_ms INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS achievement_state (
              source_key TEXT NOT NULL,
              achievement_id TEXT NOT NULL,
              unlock_seconds INTEGER NOT NULL,
              PRIMARY KEY(source_key, achievement_id),
              FOREIGN KEY(source_key) REFERENCES sources(source_key)
            );
            CREATE TABLE IF NOT EXISTS unlock_history (
              event_id TEXT PRIMARY KEY,
              source_key TEXT NOT NULL,
              achievement_id TEXT NOT NULL,
              unlock_seconds INTEGER NOT NULL,
              observed_ms INTEGER NOT NULL,
              delivered_ms INTEGER NULL
            );
            CREATE INDEX IF NOT EXISTS idx_unlock_pending
              ON unlock_history(source_key, delivered_ms, observed_ms);
            """;
        schema.ExecuteNonQuery();
    }

    public IReadOnlyList<UnlockEvent> AcceptSnapshot(string sourceKey, GcSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync)
        {
            using var tx = _connection.BeginTransaction();
            bool baseline;
            using (var lookup = Query(tx,
                "SELECT COUNT(*) FROM sources WHERE source_key=$src", ("$src", sourceKey)))
                baseline = Convert.ToInt32(lookup.ExecuteScalar(), CultureInfo.InvariantCulture) == 0;
            if (baseline)
            {
                using var insertSource = Query(tx,
                    "INSERT INTO sources(source_key,first_seen_ms) VALUES($src,$now)",
                    ("$src", sourceKey), ("$now", now.ToUnixTimeMilliseconds()));
                insertSource.ExecuteNonQuery();
            }

            var prior = new Dictionary<string, uint>(StringComparer.Ordinal);
            using (var query = Query(tx,
                "SELECT achievement_id,unlock_seconds FROM achievement_state WHERE source_key=$src",
                ("$src", sourceKey)))
            using (var reader = query.ExecuteReader())
                while (reader.Read()) prior.Add(reader.GetString(0), checked((uint)reader.GetInt64(1)));

            using (var clear = Query(tx, "DELETE FROM achievement_state WHERE source_key=$src", ("$src", sourceKey)))
                clear.ExecuteNonQuery();

            var discovered = new List<UnlockEvent>();
            foreach (var (id, stamp) in snapshot.Achievements
                .OrderBy(x => x.Value == 0 ? 1 : 0)
                .ThenBy(x => x.Value == 0 ? 0u : x.Value)
                .ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                using (var update = Query(tx,
                    "INSERT INTO achievement_state(source_key,achievement_id,unlock_seconds) VALUES($src,$id,$stamp)",
                    ("$src", sourceKey), ("$id", id), ("$stamp", (long)stamp)))
                    update.ExecuteNonQuery();
                if (baseline || prior.ContainsKey(id)) continue;
                var eventId = EventId(sourceKey, id, stamp);
                using var insert = Query(tx, """
                    INSERT OR IGNORE INTO unlock_history
                    (event_id,source_key,achievement_id,unlock_seconds,observed_ms,delivered_ms)
                    VALUES($event,$src,$id,$stamp,$now,NULL)
                    """, ("$event", eventId), ("$src", sourceKey), ("$id", id),
                    ("$stamp", (long)stamp), ("$now", now.ToUnixTimeMilliseconds()));
                if (insert.ExecuteNonQuery() == 1)
                    discovered.Add(new UnlockEvent(eventId, sourceKey, id, stamp, now));
            }
            tx.Commit();
            return discovered;
        }
    }

    /// <summary>
    /// Reconstructs the last successfully committed snapshot for this exact GC source.
    /// HasValidSnapshot=false means Unknown, NOT zero earned or all locked.
    /// The record is historical/last-known until the monitor confirms a live valid read.
    /// Neither this query nor a missing/invalid source file changes the SQLite ledger.
    /// </summary>
    public GcCurrentAchievementState GetCurrentState(string sourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        lock (_sync)
        {
            using var known = _connection.CreateCommand();
            known.CommandText = "SELECT 1 FROM sources WHERE source_key=$src LIMIT 1";
            known.Parameters.AddWithValue("$src", sourceKey);
            if (known.ExecuteScalar() is null)
                return GcCurrentAchievementState.Unknown;

            using var query = _connection.CreateCommand();
            query.CommandText = """
                SELECT achievement_id, unlock_seconds
                FROM achievement_state WHERE source_key=$src
                ORDER BY achievement_id
                """;
            query.Parameters.AddWithValue("$src", sourceKey);
            var values = new Dictionary<string, uint>(StringComparer.Ordinal);
            using var reader = query.ExecuteReader();
            while (reader.Read())
                values.Add(reader.GetString(0), checked((uint)reader.GetInt64(1)));
            return new GcCurrentAchievementState(true, values);
        }
    }

    public IReadOnlyList<UnlockEvent> GetPending(string sourceKey) =>
        ReadEvents("WHERE source_key=$src AND delivered_ms IS NULL ORDER BY observed_ms,rowid",
            sourceKey, 1000);

    public IReadOnlyList<UnlockEvent> GetRecent(string sourceKey, int limit = 100) =>
        ReadEvents("WHERE source_key=$src ORDER BY observed_ms DESC,rowid DESC",
            sourceKey, Math.Clamp(limit, 1, 1000));

    private IReadOnlyList<UnlockEvent> ReadEvents(string where, string sourceKey, int limit)
    {
        lock (_sync)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"""
                SELECT event_id,source_key,achievement_id,unlock_seconds,observed_ms
                FROM unlock_history {where} LIMIT $limit
                """;
            cmd.Parameters.AddWithValue("$src", sourceKey);
            cmd.Parameters.AddWithValue("$limit", limit);
            var result = new List<UnlockEvent>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add(new UnlockEvent(reader.GetString(0), reader.GetString(1),
                    reader.GetString(2), checked((uint)reader.GetInt64(3)),
                    DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4))));
            return result;
        }
    }

    public void MarkDelivered(string eventId, DateTimeOffset when)
    {
        lock (_sync)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                UPDATE unlock_history SET delivered_ms=$now
                WHERE event_id=$id AND delivered_ms IS NULL
                """;
            cmd.Parameters.AddWithValue("$id", eventId);
            cmd.Parameters.AddWithValue("$now", when.ToUnixTimeMilliseconds());
            cmd.ExecuteNonQuery();
        }
    }

    private SqliteCommand Query(SqliteTransaction tx, string sql, params (string key, object value)[] values)
    {
        var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (key, value) in values) cmd.Parameters.AddWithValue(key, value);
        return cmd;
    }

    public static string EventId(string sourceKey, string achievementId, uint seconds) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            sourceKey + "\0" + achievementId + "\0" + seconds.ToString(CultureInfo.InvariantCulture))));

    public void Dispose()
    {
        lock (_sync) _connection.Dispose();
    }
}
