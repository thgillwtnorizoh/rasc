namespace Rasc.Core;

/// <summary>
/// Source-derived Classic notification duration/capacity with OWNER-TARGET slide and
/// occlusion geometry. This is NOT an original Steam runtime interpolator.
/// Each transition is serialized and uses a caller-supplied monotonic clock.
/// Cards are ordered newest at the bottom; all coordinates are relative to the
/// lower-right anchored 240x(3*94) logical host viewport.
/// </summary>
public sealed class ClassicToastScheduler
{
    public const int Capacity = 3;
    public const double CardWidth = 240;
    public const double CardHeight = 94;
    public const double ViewportHeight = CardHeight * Capacity;

    // May 2023 resource names FadeIn/Out, but owner's target shows slides without alpha.
    public static readonly TimeSpan SlideIn = TimeSpan.FromMilliseconds(450);
    public static readonly TimeSpan Display = TimeSpan.FromSeconds(6);
    public static readonly TimeSpan SlideOut = TimeSpan.FromMilliseconds(450);

    private enum MotionKind { Enter, Exit }

    // Complete presentation data lives with the single scheduler-owned card,
    // never in a second host dictionary with a separate retirement lifecycle.
    private sealed class Card(UnlockEvent evt, ClassicToastContent content)
    {
        public UnlockEvent Event { get; } = evt;
        public ClassicToastContent Content { get; } = content;
        public TimeSpan Expiration { get; set; } = TimeSpan.MaxValue;
    }

    private sealed record Motion(MotionKind Kind, Card Card, int Slot, TimeSpan Start);
    private readonly List<Card> _settled = []; // newest (bottom) at index 0
    private readonly Queue<Card> _waiting = new();
    private readonly HashSet<string> _tracked = new(StringComparer.Ordinal);
    private Motion? _motion;
    private bool _clockStarted;
    private TimeSpan _previous;

    public int WaitingCount => _waiting.Count;
    public int ActiveCount => _settled.Count + (_motion?.Kind == MotionKind.Enter ? 1 : 0);
    public bool IsIdle => _settled.Count == 0 && _waiting.Count == 0 && _motion is null;

    /// <summary>True only during a 450ms slide; WPF only needs per-frame updates then.</summary>
    public bool IsAnimating => _motion is not null;

    /// <summary>
    /// Future monotonic clock deadline when the next held toast becomes eligible to
    /// exit. Returns null during animations and when idle. The presentation host
    /// can sleep without a high-rate dispatcher timer during the six-second hold.
    /// </summary>
    public TimeSpan? NextHoldDeadline =>
        _motion is null && _settled.Count != 0
            ? _settled.Min(card => card.Expiration)
            : null;

    public bool Enqueue(UnlockEvent evt) =>
        Enqueue(evt, new ClassicToastContent("Achievement Unlocked!", evt.AchievementId));

    public bool Enqueue(UnlockEvent evt, ClassicToastContent content)
    {
        ArgumentNullException.ThrowIfNull(evt);
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(evt.EventId))
            throw new ArgumentException("Notification event must have a stable ID.", nameof(evt));
        if (!_tracked.Add(evt.EventId)) return false;
        _waiting.Enqueue(new Card(evt, content));
        return true;
    }

    /// <summary>
    /// Advance simulation to now. Large jumps resolve each completed transition at
    /// its actual deadline before evaluating later transitions. Stable FIFO is preserved.
    /// All visible pixels remain opaque; ClipTop/ClipBottom describe occlusion.
    /// </summary>
    public IReadOnlyList<ClassicToastFrame> Advance(TimeSpan now) =>
        AdvancePresentation(now).Frames;

    /// <summary>
    /// Return a complete read-only presentation transaction: content, geometry,
    /// timeline state and once-only transitions. WPF cannot mutate the scheduler
    /// or look up a card that may already have retired.
    /// </summary>
    public ClassicToastPresentationSnapshot AdvancePresentation(TimeSpan now)
    {
        if (now < TimeSpan.Zero || (_clockStarted && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now), "Time must be monotonic.");

        var transitions = new List<ClassicToastTransition>();
        var cursor = _clockStarted ? _previous : TimeSpan.Zero;
        _clockStarted = true;
        _previous = now;

        for (var transitionCount = 0; transitionCount < 100000; transitionCount++)
        {
            if (_motion is { } motion)
            {
                var end = motion.Start + (motion.Kind == MotionKind.Enter ? SlideIn : SlideOut);
                if (end > now) break;

                if (motion.Kind == MotionKind.Enter)
                {
                    motion.Card.Expiration = end + Display;
                    _settled.Insert(0, motion.Card);
                    transitions.Add(new ClassicToastTransition(motion.Card.Event,
                        ClassicToastTransitionKind.Settled, motion.Slot));
                }
                else
                {
                    _settled.Remove(motion.Card);
                    _tracked.Remove(motion.Card.Event.EventId);
                    transitions.Add(new ClassicToastTransition(motion.Card.Event,
                        ClassicToastTransitionKind.Retired, motion.Slot));
                }
                _motion = null;
                cursor = end;
                continue;
            }

            // After a removal, an already queued replacement has priority over
            // expiring the next survivor. This is a documented ENGINEERING policy.
            if (_waiting.Count > 0 && _settled.Count < Capacity)
            {
                var incoming = _waiting.Dequeue();
                _motion = new Motion(MotionKind.Enter, incoming, 0, cursor);
                transitions.Add(new ClassicToastTransition(incoming.Event,
                    ClassicToastTransitionKind.Started, 0));
                continue;
            }

            // Each card holds for 6 seconds after its own entrance completes.
            // Oldest-first expiration gives a deterministic removal order.
            var expired = -1;
            for (var i = _settled.Count - 1; i >= 0; i--)
            {
                if (_settled[i].Expiration <= now)
                {
                    expired = i;
                    break;
                }
            }
            if (expired >= 0)
            {
                var card = _settled[expired];
                var start = card.Expiration > cursor ? card.Expiration : cursor;
                _motion = new Motion(MotionKind.Exit, card, expired, start);
                transitions.Add(new ClassicToastTransition(card.Event,
                    ClassicToastTransitionKind.ExitStarted, expired));
                continue;
            }
            break;
        }

        var frames = new List<ClassicToastFrame>(_settled.Count + 1);
        var progress = _motion is { } m
            ? Math.Clamp((now - m.Start).TotalMilliseconds /
                (m.Kind == MotionKind.Enter ? SlideIn.TotalMilliseconds : SlideOut.TotalMilliseconds), 0, 1)
            : 0;

        // Render top to bottom so the lower card can cover a descending upper card.
        for (var i = _settled.Count - 1; i >= 0; i--)
        {
            var card = _settled[i];
            var y = ViewportHeight - (i + 1) * CardHeight;
            var stage = ClassicToastStage.Visible;
            var clipBottomScreen = ViewportHeight;

            if (_motion is { Kind: MotionKind.Enter })
                y -= CardHeight * progress; // all older cards pushed upward

            if (_motion is { Kind: MotionKind.Exit } exit)
            {
                if (i >= exit.Slot)
                {
                    y += CardHeight * progress; // exiting + survivors above compact down
                    if (i == exit.Slot)
                    {
                        stage = ClassicToastStage.Exiting;
                        // Bottom exits behind taskbar. Upper exits are masked by
                        // the card immediately below (settled slot i-1).
                        clipBottomScreen = i == 0
                            ? ViewportHeight : ViewportHeight - i * CardHeight;
                    }
                }
            }
            frames.Add(Frame(card, i, stage, y, clipBottomScreen));
        }

        if (_motion is { Kind: MotionKind.Enter } entering)
        {
            var y = ViewportHeight - CardHeight * progress;
            frames.Add(Frame(entering.Card, 0, ClassicToastStage.Entering,
                y, ViewportHeight));
        }
        return new ClassicToastPresentationSnapshot(
            frames.AsReadOnly(), transitions.AsReadOnly(),
            IsAnimating, IsIdle, NextHoldDeadline);
    }

    private static ClassicToastFrame Frame(Card card, int slot,
        ClassicToastStage stage, double y, double absoluteClipBottom)
    {
        // Local clip coordinates; if a card descends behind another, the
        // part under the lower card's top edge becomes invisible.
        var top = Math.Clamp(-y, 0, CardHeight);
        var bottom = Math.Clamp(absoluteClipBottom - y, 0, CardHeight);
        return new ClassicToastFrame(card.Event, slot, stage, y, top,
            Math.Max(top, bottom)) { Content = card.Content };
    }

    public void Clear()
    {
        _waiting.Clear();
        _settled.Clear();
        _tracked.Clear();
        _motion = null;
    }
}

/// <summary>
/// Value data captured once at admission. Callers cannot mutate the stored icon
/// bytes, and the content remains alive as long as a returned frame is alive,
/// even when its scheduler card is later retired.
/// </summary>
public sealed class ClassicToastContent
{
    private readonly byte[]? _icon;
    public string Title { get; }
    public string Description { get; }

    public ClassicToastContent(string title, string description, byte[]? iconJpeg = null)
    {
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        _icon = iconJpeg?.ToArray();
    }

    public byte[]? CopyIconJpeg() => _icon?.ToArray();
}

public enum ClassicToastStage { Entering, Visible, Exiting }
public enum ClassicToastTransitionKind { Started, Settled, ExitStarted, Retired }

public sealed record ClassicToastTransition(
    UnlockEvent Event, ClassicToastTransitionKind Kind, int Slot);

public sealed record ClassicToastPresentationSnapshot(
    IReadOnlyList<ClassicToastFrame> Frames,
    IReadOnlyList<ClassicToastTransition> Transitions,
    bool IsAnimating,
    bool IsIdle,
    TimeSpan? NextHoldDeadline);

/// <param name="Y">Card top in the shared viewport (0 = top, 3H = taskbar edge).</param>
/// <param name="ClipTop">Visible local top edge, inclusive.</param>
/// <param name="ClipBottom">Visible local bottom edge, exclusive.</param>
public sealed record ClassicToastFrame(
    UnlockEvent Event,
    int Slot,
    ClassicToastStage Stage,
    double Y,
    double ClipTop,
    double ClipBottom)
{
    // Full source-backed content, not an ID that requires a mutable WPF lookup.
    // Nullable only to retain compatibility for existing geometry-only fixtures.
    public ClassicToastContent? Content { get; init; }
    public double VisibleHeight => Math.Max(0, ClipBottom - ClipTop);
    public double Opacity => 1.0;
}
