namespace Rasc.Core;

/// <summary>Committed GT-610 local stats snapshot (version 1). Achievement timestamps are Unix seconds.</summary>
public sealed record GcSnapshot(
    IReadOnlyDictionary<string, int> IntegerStats,
    IReadOnlyDictionary<string, float> FloatStats,
    IReadOnlyDictionary<string, uint> Achievements);

public sealed record UnlockEvent(
    string EventId,
    string SourceKey,
    string AchievementId,
    uint UnlockUnixSeconds,
    DateTimeOffset ObservedAt);

public enum ReadStatus { Valid, Missing, Invalid, Unavailable }

public sealed record FileReadResult(ReadStatus Status, GcSnapshot? Snapshot, string? Detail)
{
    public static FileReadResult Valid(GcSnapshot value) => new(ReadStatus.Valid, value, null);
    public static FileReadResult Failure(ReadStatus status, string? detail) => new(status, null, detail);
}
