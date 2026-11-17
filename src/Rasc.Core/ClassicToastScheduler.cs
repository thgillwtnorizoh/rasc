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

    private sealed class Card(UnlockEvent evt)
    {
        public UnlockEvent Event { get; } = evt;
        public TimeSpan Expiration { get; set; } = TimeSpan.MaxValue;
    }

    private sealed record Motion(MotionKind Kind, Card Card, int Slot, TimeSpan Start);
    private readonly List<Card> _settled = []; // newest (bottom) at index 0
    private readonly Queue<UnlockEvent> _waiting = new();
    private readonly HashSet<string> _tracked = new(StringComparer.Ordinal);
    private Motion? _motion;
    private bool _clockStarted;
    private TimeSpan _previous;

    public int WaitingCount => _waiting.Count;
    public int ActiveCount => _settled.Count + (_motion?.Kind == MotionKind.Enter ? 1 : 0);
    public bool IsIdle => _settled.Count == 0 && _waiting.Count == 0 && _motion is null;

    public bool Enqueue(UnlockEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (string.IsNullOrWhiteSpace(evt.EventId))
            throw new ArgumentException("Notification event must have a stable ID.", nameof(evt));
        if (!_tracked.Add(evt.EventId)) return false;
        _waiting.Enqueue(evt);
        return true;
    }

    /// <summary>
    /// Advance simulation to now. Large jumps resolve each completed transition at
    /// its actual deadline before evaluating later transitions. Stable FIFO is preserved.
    /// All visible pixels remain opaque; ClipTop/ClipBottom describe occlusion.
    /// </summary>
    public IReadOnlyList<ClassicToastFrame> Advance(TimeSpan now)
    {
        if (now < TimeSpan.Zero || (_clockStarted && now < _previous))
            throw new ArgumentOutOfRangeException(nameof(now), "Time must be monotonic.");

        var cursor = _clockStarted ? _previous : TimeSpan.Zero;
        _clockStarted = true;
        _previous = now;

        for (var transitions = 0; transitions < 100000; transitions++)
        {
            if (_motion is { } motion)
            {
                var end = motion.Start + (motion.Kind == MotionKind.Enter ? SlideIn : SlideOut);
                if (end > now) break;

                if (motion.Kind == MotionKind.Enter)
                {
                    motion.Card.Expiration = end + Display;
                    _settled.Insert(0, motion.Card);
                }
                else
                {
                    _settled.Remove(motion.Card);
                    _tracked.Remove(motion.Card.Event.EventId);
                }
                _motion = null;
                cursor = end;
                continue;
            }

            // After a removal, an already queued replacement has priority over
            // expiring the next survivor. This is a documented ENGINEERING policy.
            if (_waiting.Count > 0 && _settled.Count < Capacity)
            {
                var incoming = new Card(_waiting.Dequeue());
                _motion = new Motion(MotionKind.Enter, incoming, 0, cursor);
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
            frames.Add(Frame(card.Event, i, stage, y, clipBottomScreen));
        }

        if (_motion is { Kind: MotionKind.Enter } entering)
        {
            var y = ViewportHeight - CardHeight * progress;
            frames.Add(Frame(entering.Card.Event, 0, ClassicToastStage.Entering,
                y, ViewportHeight));
        }
        return frames;
    }

    private static ClassicToastFrame Frame(UnlockEvent evt, int slot,
        ClassicToastStage stage, double y, double absoluteClipBottom)
    {
        // Local clip coordinates; if a card descends behind another, the
        // part under the lower card's top edge becomes invisible.
        var top = Math.Clamp(-y, 0, CardHeight);
        var bottom = Math.Clamp(absoluteClipBottom - y, 0, CardHeight);
        return new ClassicToastFrame(evt, slot, stage, y, top, Math.Max(top, bottom));
    }

    public void Clear()
    {
        _waiting.Clear();
        _settled.Clear();
        _tracked.Clear();
        _motion = null;
    }
}

public enum ClassicToastStage { Entering, Visible, Exiting }

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
    public double VisibleHeight => Math.Max(0, ClipBottom - ClipTop);
    public double Opacity => 1.0;
}
