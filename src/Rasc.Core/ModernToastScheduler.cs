namespace Rasc.Core;

/// <summary>
/// Bounded desktop-achievement scheduler informed by P000's supplied modern
/// Steam UI bundle. The supported profile is NORMAL desktop achievements only:
/// 283px wide, a provisional 70px achievement card, 300ms entrance/exit and
/// a 5000ms default hold, three active slots, and FIFO waiting.
/// Display-alone, alternate capacity, server-data delay, type-specific height,
/// compositor interpolation and in-game overlay paths are NOT implemented.
/// </summary>
public sealed class ModernToastScheduler
{
    public const int Capacity = 3;
    public const double CardWidth = 283;
    // 70px is confirmed for DesktopToastContainer, not all toast window types.
    public const double CardHeight = 70;
    public static readonly TimeSpan Entrance = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan Hold = TimeSpan.FromMilliseconds(5000);
    public static readonly TimeSpan Exit = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan Lifetime = Entrance + Hold + Exit;

    private sealed record Active(UnlockEvent Event, TimeSpan Started, long Order);
    private readonly List<Active> _active = [];
    private readonly Queue<UnlockEvent> _waiting = new();
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private bool _clockStarted;
    private TimeSpan _previous;
    private long _order;

    public int ActiveCount => _active.Count;
    public int WaitingCount => _waiting.Count;
    public bool IsIdle => _active.Count == 0 && _waiting.Count == 0;
    public bool IsAnimating => _active.Any(a =>
    {
        var elapsed = _previous - a.Started;
        return elapsed < Entrance || elapsed >= Entrance + Hold;
    });

    public TimeSpan? NextHoldDeadline
    {
        get
        {
            TimeSpan? next = null;
            foreach (var a in _active)
            {
                var deadline = a.Started + Entrance + Hold;
                if (deadline <= _previous) continue;
                if (next is null || deadline < next.Value) next = deadline;
            }
            return next;
        }
    }

    public bool Enqueue(UnlockEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (string.IsNullOrWhiteSpace(evt.EventId))
            throw new ArgumentException("Stable event ID required.", nameof(evt));
        if (!_known.Add(evt.EventId)) return false;
        _waiting.Enqueue(evt);
        return true;
    }

    public IReadOnlyList<ModernToastFrame> Advance(TimeSpan now)
    {
        if (now < TimeSpan.Zero || (_clockStarted && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now), "Time must be monotonic.");

        var cursor = _clockStarted ? _previous : TimeSpan.Zero;
        _clockStarted = true;

        // Replay completed lifetimes at their exact deadline, so delayed
        // callbacks do not extend past notifications or starve the FIFO.
        for (var step = 0; step < 100000; step++)
        {
            while (_active.Count < Capacity && _waiting.Count != 0)
                _active.Add(new Active(_waiting.Dequeue(), cursor, ++_order));

            if (_active.Count == 0) break;
            var earliestEnd = _active.Min(a => a.Started + Lifetime);
            if (earliestEnd > now) break;
            _active.RemoveAll(a => a.Started + Lifetime <= earliestEnd);
            cursor = earliestEnd;
        }

        _previous = now;
        // Newest at the bottom is an explicit desktop ENGINEERING choice
        // pending original runtime frame evidence.
        var sorted = _active.OrderByDescending(a => a.Started)
            .ThenByDescending(a => a.Order).ToArray();
        var frames = new List<ModernToastFrame>(sorted.Length);
        for (var slot = 0; slot < sorted.Length; slot++)
        {
            var a = sorted[slot];
            var elapsed = now - a.Started;
            var stage = elapsed < Entrance
                ? ModernToastStage.Entering
                : elapsed < Entrance + Hold
                    ? ModernToastStage.Showing
                    : ModernToastStage.Exiting;
            var progress = stage switch
            {
                ModernToastStage.Entering => Math.Clamp(
                    elapsed.TotalMilliseconds / Entrance.TotalMilliseconds, 0, 1),
                ModernToastStage.Exiting => Math.Clamp(
                    (elapsed - Entrance - Hold).TotalMilliseconds /
                    Exit.TotalMilliseconds, 0, 1),
                _ => 1.0
            };
            var offset = stage switch
            {
                ModernToastStage.Entering => CardHeight * (1 - progress),
                ModernToastStage.Exiting => CardHeight * progress,
                _ => 0
            };
            frames.Add(new ModernToastFrame(a.Event, slot, stage,
                -(slot + 1) * CardHeight + offset, progress));
        }
        return frames;
    }

    public void Clear()
    {
        _active.Clear();
        _waiting.Clear();
        _known.Clear();
        _clockStarted = false;
        _previous = TimeSpan.Zero;
        _order = 0;
    }
}

public enum ModernToastStage { Entering, Showing, Exiting }

/// <param name="BottomRelativeY">Logical top edge relative to the work area bottom.</param>
public sealed record ModernToastFrame(
    UnlockEvent Event,
    int Slot,
    ModernToastStage Stage,
    double BottomRelativeY,
    double Progress);
