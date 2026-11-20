namespace Rasc.Core;

/// <summary>
/// P003-C02 owner-target NORMAL desktop achievement waves. All events admitted
/// while idle form one bounded wave of up to three notifications. Events earned
/// during an active wave wait until that wave has fully returned to the common
/// off-screen origin. This is an ENGINEERING reading of owner-observed motion,
/// not a claim about every original Steam notification type.
/// </summary>
public sealed class ModernToastScheduler
{
    public const int Capacity = 3;
    public const double CardWidth = 283;
    // Owner screenshot supports 70px for the shown achievement cards only.
    public const double CardHeight = 70;
    public static readonly TimeSpan Entrance = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan Hold = TimeSpan.FromMilliseconds(5000);
    public static readonly TimeSpan Exit = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan Lifetime = Entrance + Hold + Exit;

    private readonly List<UnlockEvent> _wave = [];
    private readonly Queue<UnlockEvent> _waiting = new();
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private TimeSpan _waveStarted;
    private TimeSpan _previous;
    private bool _clockStarted;

    public int ActiveCount => _wave.Count;
    public int WaitingCount => _waiting.Count;
    public bool IsIdle => _wave.Count == 0 && _waiting.Count == 0;
    public bool IsAnimating => _wave.Count != 0 &&
        (_previous - _waveStarted < Entrance ||
         _previous - _waveStarted >= Entrance + Hold);

    public TimeSpan? NextHoldDeadline =>
        _wave.Count > 0 && !IsAnimating
            ? _waveStarted + Entrance + Hold
            : null;

    /// <summary>
    /// Next REQUIRED scheduler transition. The WPF compositor is permitted to
    /// stop sending Rendering callbacks while fullscreen CS:GO is foreground.
    /// Hosts must wake at these boundaries even when no new event arrives.
    /// </summary>
    public TimeSpan? NextTransitionDeadline
    {
        get
        {
            if (_wave.Count == 0) return null;
            var elapsed = _previous - _waveStarted;
            if (elapsed < Entrance) return _waveStarted + Entrance;
            if (elapsed < Entrance + Hold) return _waveStarted + Entrance + Hold;
            if (elapsed < Lifetime) return _waveStarted + Lifetime;
            return _previous; // caller should immediately Advance at overdue boundary
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

    /// <summary>
    /// Called once when the desktop host finishes its short admission batch.
    /// Advances the idle clock without showing a partial wave, so the first
    /// rendered frame really starts at the shared below-taskbar origin.
    /// </summary>
    public void AlignPendingWaveStart(TimeSpan now)
    {
        if (_wave.Count != 0 || _waiting.Count == 0)
            throw new InvalidOperationException("Only a not-yet-admitted wave may be aligned.");
        if (now < TimeSpan.Zero || (_clockStarted && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now));
        _clockStarted = true;
        _previous = now;
    }

    public IReadOnlyList<ModernToastFrame> Advance(TimeSpan now)
    {
        if (now < TimeSpan.Zero || (_clockStarted && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now), "Time must be monotonic.");

        // Hosts must advance their monotonic clock BEFORE enqueueing after
        // idle; that lets the new wave begin now, not when the process launched.
        var cursor = _clockStarted ? _previous : TimeSpan.Zero;
        _clockStarted = true;

        // Resolve a slow callback at exact transition deadlines; a backlog
        // never extends the lifetime of already expired notifications.
        while (true)
        {
            if (_wave.Count == 0 && _waiting.Count > 0)
            {
                _waveStarted = cursor;
                while (_wave.Count < Capacity && _waiting.Count > 0)
                    _wave.Add(_waiting.Dequeue());
            }
            if (_wave.Count == 0) break;

            var deadline = _waveStarted + Lifetime;
            if (deadline > now) break;
            _wave.Clear();
            cursor = deadline;
        }

        _previous = now;
        if (_wave.Count == 0) return [];

        var elapsed = now - _waveStarted;
        var stage = elapsed < Entrance ? ModernToastStage.Entering :
            elapsed < Entrance + Hold ? ModernToastStage.Showing :
            ModernToastStage.Exiting;
        // Phase fraction is a *visibility* fraction. Every member moves
        // from y=0 (completely below work-area bottom) to its assigned slot,
        // then returns to the SAME origin. There is no independent entry point.
        var fraction = stage switch
        {
            ModernToastStage.Entering =>
                Math.Clamp(elapsed.TotalMilliseconds / Entrance.TotalMilliseconds, 0, 1),
            ModernToastStage.Exiting =>
                Math.Clamp((Lifetime - elapsed).TotalMilliseconds / Exit.TotalMilliseconds, 0, 1),
            _ => 1d
        };
        var count = _wave.Count;
        var frames = new List<ModernToastFrame>(count);
        // Oldest at top, newest at bottom. Render top-to-bottom; bottom window
        // is created last so it naturally overlaps the stack as cards emerge.
        for (var i = 0; i < count; i++)
        {
            var slot = count - i - 1;
            frames.Add(new ModernToastFrame(_wave[i], slot, count, stage,
                -(slot + 1) * CardHeight * fraction, fraction));
        }
        return frames;
    }

    public void Clear()
    {
        _wave.Clear();
        _waiting.Clear();
        _known.Clear();
        _clockStarted = false;
        _waveStarted = TimeSpan.Zero;
        _previous = TimeSpan.Zero;
    }
}

public enum ModernToastStage { Entering, Showing, Exiting }

/// <param name="BottomRelativeY">Card top relative to work-area bottom,
/// including wave interpolation; 0 is the common fully hidden origin.</param>
/// <param name="WaveSize">Number of simultaneously active cards in this wave.</param>
/// <param name="Progress">Visibility interpolation 0..1 (not a progress achievement value).</param>
public sealed record ModernToastFrame(
    UnlockEvent Event,
    int Slot,
    int WaveSize,
    ModernToastStage Stage,
    double BottomRelativeY,
    double Progress);

/// <summary>
/// Owner-observed dynamic Steam-logo watermark. Placement models ONE shared
/// decoration across the occupied stack; each toast displays its corresponding
/// clipped segment. Geometry is a nonproprietary approximation awaiting the
/// original hashed CSS positions and runtime images.
/// </summary>
public static class ModernWatermarkLayout
{
    public const double LogoHeight = 170;

    public static double TopInsideCard(int waveSize, int slot)
    {
        if (waveSize is < 1 or > ModernToastScheduler.Capacity)
            throw new ArgumentOutOfRangeException(nameof(waveSize));
        if (slot < 0 || slot >= waveSize)
            throw new ArgumentOutOfRangeException(nameof(slot));
        var logoTop = (waveSize * ModernToastScheduler.CardHeight - LogoHeight) / 2.0;
        var cardTopInWave = (waveSize - 1 - slot) * ModernToastScheduler.CardHeight;
        return logoTop - cardTopInWave;
    }
}
