namespace Rasc.Core;

/// <summary>
/// Bounded 1-3 card Steam-like notification stack. Pre-existing preview wave
/// admission stays immutable (historical P003 behavior). P006-C04 live GC
/// awards may optionally join a still-visible, not-full stack for a bounded
/// window; otherwise FIFO overflow becomes the next wave. This is an
/// owner-requested ENGINEERING correction, not a claimed Steam internal rule.
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
    // A second GC file rewrite may arrive after the first 45ms initial batch.
    // Cap the joining window to avoid indefinitely extending a busy stack.
    public static readonly TimeSpan LiveLateAdmissionWindow = TimeSpan.FromSeconds(3);

    private readonly List<UnlockEvent> _wave = [];
    private readonly List<TimeSpan> _memberStart = [];
    private readonly Queue<UnlockEvent> _waiting = new();
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private TimeSpan _waveStarted;
    private TimeSpan _holdEndsAt;
    private TimeSpan _previous;
    private bool _clockStarted;

    public int ActiveCount => _wave.Count;
    public int WaitingCount => _waiting.Count;
    public bool IsIdle => _wave.Count == 0 && _waiting.Count == 0;
    public bool IsAnimating => _wave.Count != 0 &&
        (_previous < _memberStart[^1] + Entrance ||
         _previous >= _holdEndsAt);

    public TimeSpan? NextHoldDeadline =>
        _wave.Count > 0 && !IsAnimating ? _holdEndsAt : null;

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
            if (_previous < _memberStart[^1] + Entrance)
                return _memberStart[^1] + Entrance;
            if (_previous < _holdEndsAt) return _holdEndsAt;
            if (_previous < _holdEndsAt + Exit) return _holdEndsAt + Exit;
            return _previous; // already overdue; caller advances immediately
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
    /// Opt-in for source-confirmed LIVE awards arriving in separate GC writes
    /// while the first wave is still visible. Does not disturb the already
    /// rendered card, and may extend the common hold so the new card gets its
    /// full entrance + hold. A non-qualifying event must use normal Enqueue.
    /// Preview/scheduler baseline tests intentionally use normal Enqueue.
    /// </summary>
    public bool TryAdmitLiveDuringHold(UnlockEvent evt, TimeSpan now)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (string.IsNullOrWhiteSpace(evt.EventId))
            throw new ArgumentException("Stable event ID required.", nameof(evt));
        if (now < TimeSpan.Zero || (_clockStarted && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now));
        if (_wave.Count == 0 || _wave.Count >= Capacity || _waiting.Count != 0 ||
            now >= _waveStarted + LiveLateAdmissionWindow ||
            now >= _holdEndsAt || _known.Contains(evt.EventId))
            return false;

        _known.Add(evt.EventId);
        _wave.Add(evt);
        _memberStart.Add(now);
        _holdEndsAt = now + Entrance + Hold;
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
                _holdEndsAt = _waveStarted + Entrance + Hold;
                while (_wave.Count < Capacity && _waiting.Count > 0)
                {
                    _wave.Add(_waiting.Dequeue());
                    _memberStart.Add(_waveStarted);
                }
            }
            if (_wave.Count == 0) break;

            var deadline = _holdEndsAt + Exit;
            if (deadline > now) break;
            _wave.Clear();
            _memberStart.Clear();
            cursor = deadline;
        }

        _previous = now;
        if (_wave.Count == 0) return [];

        var exiting = now >= _holdEndsAt;
        var exitFraction = exiting
            ? Math.Clamp((_holdEndsAt + Exit - now).TotalMilliseconds /
                         Exit.TotalMilliseconds, 0, 1)
            : 1d;
        var count = _wave.Count;
        var frames = new ModernToastFrame[count];
        // An added live card rises from below as the existing contiguous stack
        // smoothly shifts upward. Each member retains its original entrance
        // clock, so an existing popup never slides in again or replays a cue.
        double occupiedUnits = 0;
        for (int i = count - 1; i >= 0; i--)
        {
            var age = now - _memberStart[i];
            var entry = Math.Clamp(age.TotalMilliseconds /
                Entrance.TotalMilliseconds, 0, 1);
            occupiedUnits += entry;
            var phase = exiting ? ModernToastStage.Exiting
                : entry < 1 ? ModernToastStage.Entering
                : ModernToastStage.Showing;
            var visibility = exiting ? exitFraction : entry;
            frames[i] = new ModernToastFrame(
                _wave[i], count - i - 1, count, phase,
                -CardHeight * occupiedUnits * exitFraction, visibility);
        }
        return frames;
    }

    public void Clear()
    {
        _wave.Clear();
        _memberStart.Clear();
        _waiting.Clear();
        _known.Clear();
        _clockStarted = false;
        _waveStarted = TimeSpan.Zero;
        _holdEndsAt = TimeSpan.Zero;
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
