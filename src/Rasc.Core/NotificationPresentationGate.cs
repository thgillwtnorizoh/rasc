namespace Rasc.Core;

/// <summary>
/// Serializes ownership of two independently animated notification renderers.
/// An existing style finishes its on-screen notification before a different
/// style is permitted to create a WPF window. Normal same-style bursts remain
/// synchronous and retain their existing scheduler/batching semantics.
/// Presentation-only: no file, GC, SQLite or Steam writes.
/// </summary>
public sealed class NotificationPresentationGate
{
    private readonly Func<NotificationProfile, bool> _isIdle;
    private readonly Queue<(NotificationProfile Profile, Action Deliver)> _pending = [];
    private NotificationProfile? _active;
    private bool _pumping;

    public NotificationPresentationGate(Func<NotificationProfile, bool> isIdle)
    {
        _isIdle = isIdle ?? throw new ArgumentNullException(nameof(isIdle));
    }

    public int PendingCount => _pending.Count;
    public NotificationProfile? ActiveProfile => _active;

    public void Enqueue(NotificationProfile profile, Action deliver)
    {
        ArgumentNullException.ThrowIfNull(deliver);
        if (!Enum.IsDefined(profile))
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (profile == NotificationProfile.Off) return;
        _pending.Enqueue((profile, deliver));
        Pump();
    }

    /// <summary>
    /// Called at enqueue and while queued events wait on a previous renderer.
    /// A newly selected profile never starts while old-style HWNDs are active.
    /// Queued events retain enqueue-time profile and arrival order. Off events
    /// are omitted but do not cancel already on-screen notifications.
    /// </summary>
    public void Pump()
    {
        if (_pumping) return;
        _pumping = true;
        try
        {
            while (_pending.Count > 0)
            {
                var next = _pending.Peek();
                // Same-style events continue entering the SAME host/scheduler
                // while active (preserves Classic queuing and Modern 45ms
                // wave batching). A DIFFERENT style is blocked until the
                // previous renderer has removed its windows and animations.
                if (_active is { } active && active != next.Profile &&
                    !_isIdle(active))
                    return;

                _active = next.Profile;
                _pending.Dequeue();
                next.Deliver();
            }
        }
        finally { _pumping = false; }
    }

    public void Clear()
    {
        _pending.Clear();
        _active = null;
    }
}
