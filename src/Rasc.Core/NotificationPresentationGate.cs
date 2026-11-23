namespace Rasc.Core;

/// <summary>
/// One ordered cross-profile admission gate. Progress entries are identified before
/// any WPF host receives them, so an unlock can discard waiting progress without
/// resetting unrelated profile transitions, source bursts or visible animations.
/// </summary>
public sealed class NotificationPresentationGate
{
    private sealed record Pending(NotificationProfile Profile, Action Deliver,
        string? AchievementId, bool IsProgress);
    private readonly Func<NotificationProfile, bool> _isIdle;
    private readonly Queue<Pending> _pending = [];
    private readonly HashSet<string> _earned = new(StringComparer.Ordinal);
    private NotificationProfile? _active;
    private bool _pumping;

    public NotificationPresentationGate(Func<NotificationProfile, bool> isIdle)
    {
        _isIdle = isIdle ?? throw new ArgumentNullException(nameof(isIdle));
    }

    public int PendingCount => _pending.Count;
    public NotificationProfile? ActiveProfile => _active;

    // Existing synthetic preview and unlock-batch admissions remain compatible.
    public void Enqueue(NotificationProfile profile, Action deliver)
    {
        ArgumentNullException.ThrowIfNull(deliver);
        if (!Enum.IsDefined(profile))
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (profile == NotificationProfile.Off) return;
        _pending.Enqueue(new Pending(profile, deliver, null, false));
        Pump();
    }

    /// <summary>Returns false when this ID is already earned or display is off.</summary>
    public bool EnqueueProgress(NotificationProfile profile, string achievementId,
        Action deliver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        ArgumentNullException.ThrowIfNull(deliver);
        if (!Enum.IsDefined(profile))
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (profile == NotificationProfile.Off || _earned.Contains(achievementId))
            return false;
        _pending.Enqueue(new Pending(profile, deliver, achievementId, true));
        Pump();
        return true;
    }

    /// <summary>
    /// The source accepted the award. All same-ID progress not yet admitted to a
    /// host is discarded; caller also cancels pending cards inside BOTH hosts.
    /// Existing visible cards are intentionally unaffected.
    /// </summary>
    public int AcceptUnlock(string achievementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        _earned.Add(achievementId);
        var removed = 0;
        var count = _pending.Count;
        for (var i = 0; i < count; i++)
        {
            var item = _pending.Dequeue();
            if (item.IsProgress &&
                string.Equals(item.AchievementId, achievementId, StringComparison.Ordinal))
                removed++;
            else
                _pending.Enqueue(item);
        }
        return removed;
    }

    public bool IsEarned(string achievementId) => _earned.Contains(achievementId);

    /// <summary>
    /// New source observation session: prevent cross-source earned-state leakage
    /// and discard only old queued progress. Already displayed cards finish.
    /// </summary>
    public int BeginProgressSession()
    {
        _earned.Clear();
        var removed = 0;
        var count = _pending.Count;
        for (var i = 0; i < count; i++)
        {
            var item = _pending.Dequeue();
            if (item.IsProgress) removed++;
            else _pending.Enqueue(item);
        }
        return removed;
    }

    public void Pump()
    {
        if (_pumping) return;
        _pumping = true;
        try
        {
            while (_pending.Count > 0)
            {
                var next = _pending.Peek();
                if (_active is { } active && active != next.Profile &&
                    !_isIdle(active))
                    return;
                _active = next.Profile;
                _pending.Dequeue();
                // Guard reentrant unlocks accepted between dequeue and delivery.
                if (next.IsProgress && next.AchievementId is { } id &&
                    _earned.Contains(id))
                    continue;
                next.Deliver();
            }
        }
        finally { _pumping = false; }
    }

    public void Clear()
    {
        _pending.Clear();
        _earned.Clear();
        _active = null;
    }
}
