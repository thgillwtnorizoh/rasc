using System.Text;
using System.Text.Json;

namespace Rasc.Core;

/// <summary>Validated live GC JSONL events; file absence alone enables snapshot fallback.</summary>
public sealed record GcJournalUnlock(
    string Session, ulong Sequence, string AchievementId,
    uint UnlockUnixSeconds, DateTimeOffset ObservedAt);

public sealed record GcJournalReadResult(
    ReadStatus Status, IReadOnlyList<GcJournalUnlock> Unlocks, string? Detail)
{
    public static GcJournalReadResult Valid(IReadOnlyList<GcJournalUnlock> unlocks) =>
        new(ReadStatus.Valid, unlocks, null);
    public static GcJournalReadResult Failure(ReadStatus status, string detail) =>
        new(status, Array.Empty<GcJournalUnlock>(), detail);
}

public static class GcAchievementJournal
{
    public const string FileName = "achievement_log";
    public const int MaxBytes = 8 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static async Task<GcJournalReadResult> ReadAsync(
        string path, CancellationToken ct = default)
    {
        try
        {
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 16_384,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var initialLength = file.Length;
            if (initialLength > MaxBytes)
                return GcJournalReadResult.Failure(ReadStatus.Invalid, "Journal exceeds 8 MiB.");
            using var memory = new MemoryStream();
            await file.CopyToAsync(memory, ct);
            if (memory.Length > MaxBytes)
                return GcJournalReadResult.Failure(ReadStatus.Invalid, "Journal grew beyond 8 MiB.");
            if (file.Length != initialLength || memory.Length != initialLength)
                return GcJournalReadResult.Failure(ReadStatus.Unavailable,
                    "Journal changed during read; retrying.");

            var bytes = memory.ToArray();
            // The writer appends one newline-terminated JSON object. Ignore an
            // incomplete trailing record rather than fabricate a partial award.
            var lastNewline = Array.LastIndexOf(bytes, (byte)'\n');
            var validLength = lastNewline < 0 ? 0 : lastNewline + 1;
            var content = StrictUtf8.GetString(bytes, 0, validLength);
            var results = new List<GcJournalUnlock>();
            var identities = new HashSet<(string Session, ulong Sequence)>();
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    root.GetProperty("v").GetInt32() != 1)
                    throw new FormatException("Unsupported journal record version.");
                var session = root.GetProperty("session").GetString();
                var sequence = root.GetProperty("seq").GetUInt64();
                var type = root.GetProperty("type").GetString();
                var ms = root.GetProperty("time_ms").GetInt64();
                if (string.IsNullOrWhiteSpace(session) || session.Length > 128 ||
                    sequence == 0 || string.IsNullOrWhiteSpace(type) ||
                    !identities.Add((session, sequence)))
                    throw new FormatException("Invalid or duplicate journal event identity.");
                if (type != "unlock") continue;
                var id = root.GetProperty("id").GetString();
                var seconds = root.GetProperty("unlock_seconds").GetUInt32();
                if (string.IsNullOrWhiteSpace(id) || id.Length > 256)
                    throw new FormatException("Invalid achievement ID in journal.");
                results.Add(new GcJournalUnlock(session, sequence, id,
                    seconds, DateTimeOffset.FromUnixTimeMilliseconds(ms)));
            }
            return GcJournalReadResult.Valid(results);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (FileNotFoundException)
        {
            return GcJournalReadResult.Failure(ReadStatus.Missing, "Waiting for achievement_log.");
        }
        catch (DirectoryNotFoundException)
        {
            return GcJournalReadResult.Failure(ReadStatus.Missing, "Waiting for GC folder.");
        }
        catch (Exception ex) when (ex is JsonException or FormatException or
            KeyNotFoundException or InvalidOperationException or OverflowException or
            ArgumentOutOfRangeException or DecoderFallbackException)
        {
            return GcJournalReadResult.Failure(ReadStatus.Invalid, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return GcJournalReadResult.Failure(ReadStatus.Unavailable, ex.Message);
        }
    }
}
