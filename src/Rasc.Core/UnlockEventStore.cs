using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Rasc.Core;

/// <summary>
/// Process-local, read-only source observation. There is no SQLite database,
/// no recovered pending queue, and no disk-backed achievement history.
/// Each monitoring start owns a fresh instance; existing sources form a silent
/// baseline, so old awards never get re-queued by a subsequent application run.
/// </summary>
public sealed class UnlockEventStore : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Dictionary<string, uint>> _snapshots =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, ulong>> _journalHighWater =
        new(StringComparer.Ordinal);
    private readonly List<UnlockEvent> _history = [];
    private readonly Dictionary<string, UnlockEvent> _pending = new(StringComparer.Ordinal);
    private readonly HashSet<string> _observedFallbackAwards = new(StringComparer.Ordinal);
    private const int HistoryLimit = 300;

    /// <summary>Returns only new, observed deltas after a valid source baseline.</summary>
    public IReadOnlyList<UnlockEvent> AcceptSnapshot(
        string sourceKey, GcSnapshot snapshot, DateTimeOffset now,
        bool recordNewUnlocks = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync)
        {
            bool wasKnown = _snapshots.TryGetValue(sourceKey, out var before);
            var awards = new List<UnlockEvent>();
            if (wasKnown && recordNewUnlocks)
            {
                foreach (var (id, stamp) in snapshot.Achievements
                    .OrderBy(x => x.Value == 0 ? 1 : 0)
                    .ThenBy(x => x.Value == 0 ? 0u : x.Value)
                    .ThenBy(x => x.Key, StringComparer.Ordinal))
                {
                    if (before!.ContainsKey(id)) continue;
                    var eid = EventId(sourceKey, id, stamp);
                    // A reset and immediate re-write of the same timestamp is
                    // not proof of a fresh grant; do not announce it twice.
                    if (!_observedFallbackAwards.Add(eid)) continue;
                    var item = new UnlockEvent(eid, sourceKey, id, stamp, now);
                    Add(item);
                    awards.Add(item);
                }
            }
            _snapshots[sourceKey] = snapshot.Achievements.ToDictionary(x => x.Key, x => x.Value,
                StringComparer.Ordinal);
            return awards;
        }
    }

    /// <summary>
    /// Journal writes are authoritative only for observed new unlocks.
    /// baselineExisting=true is mandatory when RASC first attaches to an
    /// existing log: everything already in the file is silently acknowledged.
    /// Sequence high-water marks survive each match's file truncation while
    /// RASC stays running, but are never persisted across app restarts.
    /// </summary>
    public IReadOnlyList<UnlockEvent> AcceptJournal(
        string sourceKey, IReadOnlyList<GcJournalUnlock> unlocks,
        DateTimeOffset now, bool baselineExisting = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(unlocks);
        lock (_sync)
        {
            if (!_journalHighWater.TryGetValue(sourceKey, out var bySession))
            {
                bySession = new Dictionary<string, ulong>(StringComparer.Ordinal);
                _journalHighWater.Add(sourceKey, bySession);
            }
            var added = new List<UnlockEvent>();
            foreach (var award in unlocks)
            {
                var highest = bySession.GetValueOrDefault(award.Session);
                if (award.Sequence <= highest) continue;
                bySession[award.Session] = award.Sequence;
                if (baselineExisting) continue;
                var id = JournalEventId(sourceKey, award.Session, award.Sequence);
                var evt = new UnlockEvent(id, sourceKey, award.AchievementId,
                    award.UnlockUnixSeconds, award.ObservedAt);
                if (_snapshots.TryGetValue(sourceKey, out var state))
                    state[award.AchievementId] = award.UnlockUnixSeconds;
                Add(evt);
                added.Add(evt);
            }
            return added;
        }
    }

    public GcCurrentAchievementState GetCurrentState(string sourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        lock (_sync)
        {
            if (!_snapshots.TryGetValue(sourceKey, out var snapshot))
                return GcCurrentAchievementState.Unknown;
            return new GcCurrentAchievementState(true,
                new Dictionary<string, uint>(snapshot, StringComparer.Ordinal));
        }
    }

    public IReadOnlyList<UnlockEvent> GetPending(string sourceKey)
    {
        lock (_sync)
        {
            return _history.Where(x => x.SourceKey == sourceKey &&
                _pending.ContainsKey(x.EventId)).ToArray();
        }
    }

    public IReadOnlyList<UnlockEvent> GetRecent(string sourceKey, int limit = 100)
    {
        lock (_sync)
        {
            return _history.Where(x => x.SourceKey == sourceKey)
                .TakeLast(Math.Clamp(limit, 1, 1000)).Reverse().ToArray();
        }
    }

    public void MarkDelivered(string eventId, DateTimeOffset when)
    {
        lock (_sync) _pending.Remove(eventId);
    }

    private void Add(UnlockEvent evt)
    {
        _history.Add(evt);
        if (_history.Count > HistoryLimit) _history.RemoveAt(0);
        _pending[evt.EventId] = evt;
    }

    public static string JournalEventId(string sourceKey, string session, ulong seq) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            sourceKey + "\0journal\0" + session + "\0" +
            seq.ToString(CultureInfo.InvariantCulture))));

    public static string EventId(string sourceKey, string achievementId, uint seconds) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            sourceKey + "\0" + achievementId + "\0" +
            seconds.ToString(CultureInfo.InvariantCulture))));

    public void Dispose()
    {
        lock (_sync)
        {
            _snapshots.Clear();
            _journalHighWater.Clear();
            _observedFallbackAwards.Clear();
            _history.Clear();
            _pending.Clear();
        }
    }
}
