namespace Rasc.Core;

/// <summary>
/// Deterministic, renderer-independent Classic-style desktop notification timeline.
/// Durations and visible limit are from P000's 2023 VGUI source. Slot reuse,
/// interpolation, and overflow arbitration are explicitly ENGINEERING approximations,
/// not original Steam runtime behavior. Always supply monotonic elapsed time.
/// </summary>
public sealed class ClassicToastScheduler
{
    public static readonly TimeSpan FadeIn = TimeSpan.FromMilliseconds(450);
    public static readonly TimeSpan Visible = TimeSpan.FromSeconds(6);
    public static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(450);
    public static readonly TimeSpan Lifetime = FadeIn + Visible + FadeOut;
    public const int Capacity = 3;
    // P000 has no verified animation amplitude. This is a provisional desktop motion.
    public const double ProvisionalVerticalSlideDips = 12;

    private readonly Queue<UnlockEvent> _waiting = new();
    private readonly Dictionary<int, Active> _active = new();
    private readonly HashSet<string> _tracked = new(StringComparer.Ordinal);
    private TimeSpan _previous;
    private bool _advanced;

    private sealed record Active(UnlockEvent Event, TimeSpan Started);

    public int WaitingCount => _waiting.Count;
    public int ActiveCount => _active.Count;
    public bool IsIdle => _waiting.Count == 0 && _active.Count == 0;

    public bool Enqueue(UnlockEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (string.IsNullOrWhiteSpace(evt.EventId))
            throw new ArgumentException("Notification event must have a stable ID.", nameof(evt));
        if (!_tracked.Add(evt.EventId)) return false;
        _waiting.Enqueue(evt);
        return true;
    }

    /// <summary>Runs the queue to the supplied current elapsed time. Caller owns one UI thread.</summary>
    public IReadOnlyList<ClassicToastFrame> Advance(TimeSpan now)
    {
        if (now < TimeSpan.Zero || (_advanced && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now), "Time must be monotonic.");
        _advanced = true;
        _previous = now;

        foreach (var (slot, active) in _active.ToArray())
        {
            if (now - active.Started < Lifetime) continue;
            _active.Remove(slot);
            _tracked.Remove(active.Event.EventId);
        }

        for (var slot = 0; slot < Capacity && _waiting.Count != 0; slot++)
        {
            if (_active.ContainsKey(slot)) continue;
            var evt = _waiting.Dequeue();
            _active.Add(slot, new Active(evt, now));
        }

        var frames = new List<ClassicToastFrame>(_active.Count);
        foreach (var (slot, active) in _active.OrderBy(kvp => kvp.Key))
        {
            var elapsed = now - active.Started;
            var stage = elapsed < FadeIn ? ClassicToastStage.Entering :
                elapsed < FadeIn + Visible ? ClassicToastStage.Visible : ClassicToastStage.Exiting;

            var opacity = stage switch
            {
                ClassicToastStage.Entering => elapsed.TotalMilliseconds / FadeIn.TotalMilliseconds,
                ClassicToastStage.Visible => 1.0,
                _ => 1.0 - (elapsed - FadeIn - Visible).TotalMilliseconds / FadeOut.TotalMilliseconds
            };

            opacity = Math.Clamp(opacity, 0.0, 1.0);
            // Do NOT attribute this provisional linear vertical motion to the original
            // CNotificationManager. It must be replaced if original evidence differs.
            var travel = stage == ClassicToastStage.Visible
                ? 0
                : ProvisionalVerticalSlideDips * (1 - opacity);
            frames.Add(new ClassicToastFrame(active.Event, slot, stage, opacity, travel));
        }
        return frames;
    }

    /// <summary>Discard all pending/visible presentation; does not affect the source ledger.</summary>
    public void Clear()
    {
        _waiting.Clear();
        _active.Clear();
        _tracked.Clear();
    }
}

public enum ClassicToastStage { Entering, Visible, Exiting }

public sealed record ClassicToastFrame(
    UnlockEvent Event,
    int Slot,
    ClassicToastStage Stage,
    double Opacity,
    double VerticalOffsetDips);
