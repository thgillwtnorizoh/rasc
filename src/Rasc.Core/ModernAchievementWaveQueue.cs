namespace Rasc.Core;

/// <summary>
/// FIFO of source-confirmed achievements grouped by FIRST detection time,
/// independent of presentation/animation timing. Every group is immutable as
/// soon as admitted, contains at most three awards and has a fixed 800ms
/// collection deadline. No queue item has an expiration time.
/// </summary>
public sealed class ModernAchievementWaveQueue
{
    public static readonly TimeSpan CollectionWindow = TimeSpan.FromMilliseconds(800);

    public sealed record Wave(
        DateTimeOffset FirstSignalAt,
        DateTimeOffset CollectionEndsAt,
        IReadOnlyList<UnlockEvent> Achievements);

    private sealed class OpenGroup(DateTimeOffset first)
    {
        public DateTimeOffset FirstSignalAt { get; } = first;
        public DateTimeOffset CollectionEndsAt { get; } = first + CollectionWindow;
        public List<UnlockEvent> Events { get; } = [];
        public bool CanAppend { get; set; } = true;
    }

    private readonly Queue<OpenGroup> _groups = new();
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly HashSet<string> _earned = new(StringComparer.Ordinal);
    private OpenGroup? _tail;
    private DateTimeOffset? _lastSignalAt;

    public int PendingWaveCount => _groups.Count;
    public int PendingAchievementCount => _groups.Sum(group => group.Events.Count);
    public DateTimeOffset? NextReadyAt => _groups.Count == 0
        ? null : _groups.Peek().CollectionEndsAt;

    /// <summary>
    /// Signal time comes from the monitor's original accepted snapshot
    /// (UnlockEvent.ObservedAt), NOT the time a busy UI thread receives it.
    /// Thus a delayed WPF dispatch cannot silently join a newer batch.
    /// Ties are in the already committed SQLite FIFO order.
    /// </summary>
    public bool Enqueue(UnlockEvent award)
    {
        ArgumentNullException.ThrowIfNull(award);
        if (string.IsNullOrWhiteSpace(award.EventId))
            throw new ArgumentException("Stable event ID required.", nameof(award));
        if (award.IsProgress && _earned.Contains(award.AchievementId)) return false;
        if (!_seen.Add(award.EventId)) return false;

        // A wall-clock rollback cannot be interpreted as an 800ms coherent
        // source burst. Isolate the rolled-back award from BOTH the previous
        // and subsequent cohorts rather than misgrouping it with later events.
        bool clockRolledBack = _lastSignalAt is { } last &&
            award.ObservedAt < last;
        if (_tail is null || clockRolledBack || !_tail.CanAppend ||
            award.ObservedAt < _tail.FirstSignalAt ||
            award.ObservedAt >= _tail.CollectionEndsAt ||
            _tail.Events.Count >= ModernToastScheduler.Capacity)
        {
            _tail = new OpenGroup(award.ObservedAt);
            _groups.Enqueue(_tail);
        }
        _tail.Events.Add(award);
        if (clockRolledBack) _tail.CanAppend = false;
        _lastSignalAt = award.ObservedAt;
        return true;
    }

    /// <summary>
    /// Remove any same-ID progress not yet handed to the presentation scheduler.
    /// Keep original wave deadlines and source FIFO for surviving notifications;
    /// empty waves are dropped and may never block an unrelated next wave.
    /// Already presented waves live in ModernToastScheduler and are untouched.
    /// </summary>
    public IReadOnlyList<string> MarkEarned(string achievementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        _earned.Add(achievementId);
        var canceled = new List<string>();
        var groups = _groups.ToArray();
        _groups.Clear();
        foreach (var group in groups)
        {
            foreach (var item in group.Events.Where(item =>
                item.IsProgress && string.Equals(item.AchievementId,
                    achievementId, StringComparison.Ordinal)).ToArray())
            {
                group.Events.Remove(item);
                _seen.Remove(item.EventId);
                canceled.Add(item.EventId);
            }
            if (group.Events.Count > 0) _groups.Enqueue(group);
        }
        _tail = _groups.LastOrDefault();
        return canceled;
    }

    /// <summary>
    /// Dequeue exactly one SEALED FIFO wave after its original collection
    /// deadline, irrespective of how long it has been waiting for a slot.
    /// Never fast-forward, discard or combine it with a later group.
    /// The presenter starts its own 300+5000+300ms clock AFTER admission.
    /// </summary>
    public Wave? TakeReady(DateTimeOffset now)
    {
        if (_groups.Count == 0 || now < _groups.Peek().CollectionEndsAt)
            return null;
        var group = _groups.Dequeue();
        if (ReferenceEquals(_tail, group)) _tail = null;
        return new Wave(group.FirstSignalAt, group.CollectionEndsAt,
            group.Events.ToArray());
    }

    public void Clear()
    {
        _groups.Clear();
        _seen.Clear();
        _earned.Clear();
        _tail = null;
        _lastSignalAt = null;
    }
}
