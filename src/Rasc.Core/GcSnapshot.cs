namespace Rasc.Core;

/// <summary>Committed GT-610 local stats snapshot (version 1). Achievement timestamps are Unix seconds.</summary>
public sealed record GcSnapshot(
    IReadOnlyDictionary<string, int> IntegerStats,
    IReadOnlyDictionary<string, float> FloatStats,
    IReadOnlyDictionary<string, uint> Achievements);

public enum AchievementNotificationKind { Unlock, Progress }

/// <summary>
/// The historical UnlockEvent identity remains unchanged. Progress is an explicitly
/// marked synthetic PRESENTATION event, never an award written to GC or Steam.
/// </summary>
public sealed record UnlockEvent(
    string EventId,
    string SourceKey,
    string AchievementId,
    uint UnlockUnixSeconds,
    DateTimeOffset ObservedAt)
{
    public AchievementNotificationKind NotificationKind { get; init; } =
        AchievementNotificationKind.Unlock;
    public int MilestonePercent { get; init; }
    public bool IsProgress => NotificationKind == AchievementNotificationKind.Progress;
}

public enum ReadStatus { Valid, Missing, Invalid, Unavailable }

public sealed record FileReadResult(ReadStatus Status, GcSnapshot? Snapshot, string? Detail)
{
    public static FileReadResult Valid(GcSnapshot value) => new(ReadStatus.Valid, value, null);
    public static FileReadResult Failure(ReadStatus status, string? detail) => new(status, null, detail);
}
