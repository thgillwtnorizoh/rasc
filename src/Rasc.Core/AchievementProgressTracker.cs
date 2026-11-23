namespace Rasc.Core;

/// <summary>A synthetic presentation milestone; not an original GC progress callback.</summary>
public sealed record AchievementProgressNotification(
    UnlockEvent Event, int Current, int Target, int Percent, string StatId);

/// <summary>
/// Source-scoped, process-local counter baselines and deduplication. Initial and
/// restarted lower readings are SILENT. No progress can survive accepted unlock.
/// Only explicitly enabled, source-verified mappings participate. This engine
/// never mutates GC, achievements.txt, Steam, or persistent award history.
/// </summary>
public sealed class AchievementProgressTracker
{
    private readonly AchievementProgressCatalog _catalog;
    private readonly Dictionary<(string Source, string Id), int> _previous = [];
    private readonly HashSet<(string Source, string Id, int Percent)> _emitted = [];
    private readonly HashSet<(string Source, string Id)> _earned = [];

    public AchievementProgressTracker(AchievementProgressCatalog catalog) =>
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public void AcceptUnlock(string source, string achievementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        _earned.Add((source, achievementId));
    }

    public IReadOnlyList<AchievementProgressNotification> Observe(
        string source, GcSnapshot snapshot, DateTimeOffset observedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(snapshot);
        foreach (var id in snapshot.Achievements.Keys)
            AcceptUnlock(source, id);

        var events = new List<AchievementProgressNotification>();
        foreach (var mapping in _catalog.Entries)
        {
            if (!mapping.IsLiveEligible ||
                _earned.Contains((source, mapping.AchievementId)) ||
                !snapshot.IntegerStats.TryGetValue(mapping.StatId, out var value) ||
                value < 0)
                continue;

            var key = (source, mapping.AchievementId);
            if (!_previous.TryGetValue(key, out var old))
            {
                _previous[key] = value; // initial attach / first valid stat: silent
                continue;
            }
            _previous[key] = value; // rollback establishes a silent new baseline
            if (value <= old || value >= mapping.Target) continue;
            var percent = AchievementProgressMilestonePolicy.HighestNewMilestone(
                old, value, mapping.Target, hasPriorValidCount: true);
            if (percent is not { } milestone ||
                !_emitted.Add((source, mapping.AchievementId, milestone)))
                continue;

            var identity = UnlockEventStore.EventId(
                source + "\0progress\0" + mapping.AchievementId,
                mapping.StatId + "\0" + milestone, 0);
            var evt = new UnlockEvent(identity, source, mapping.AchievementId,
                0, observedAt)
            {
                NotificationKind = AchievementNotificationKind.Progress,
                MilestonePercent = milestone
            };
            events.Add(new AchievementProgressNotification(
                evt, value, mapping.Target, milestone, mapping.StatId));
        }
        return events;
    }
}
