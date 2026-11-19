using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// P003-C03-C03: ONE nonactivating, click-through WPF HWND for a complete
/// Modern wave of one, two, or three cards. The previous per-card HWNDs
/// produced a two-pixel compositing seam in owner screenshots. A single
/// transparent 283x210 viewport preserves the wave's common bottom origin
/// and clips empty slots, with no OS window edges between adjacent cards.
/// </summary>
internal sealed class ModernToastWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExToolWindow = 0x80;
    private const long WsExNoActivate = 0x08000000;
    private readonly Canvas _viewport;
    private readonly Canvas _decorations;
    private readonly Border _backdrop;
    private readonly Canvas _watermark;
    private readonly Dictionary<string, ModernToastCard> _cards =
        new(StringComparer.Ordinal);
    private double _scale = 1;
    private bool _closed;

    internal int ActiveCardCount => _cards.Count;

    internal ModernToastWindow()
    {
        Width = ModernToastScheduler.CardWidth;
        Height = ModernStackGeometry.ViewportHeight;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Focusable = false;
        IsHitTestVisible = false;

        _viewport = new Canvas
        {
            Width = ModernToastScheduler.CardWidth,
            Height = ModernStackGeometry.ViewportHeight,
            Background = Brushes.Transparent,
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        // The background and watermark are wave-level decorations. Earlier
        // versions drew a new radial background and separately clipped Steam
        // logo INSIDE each 70-unit card, leaving thin internal highlight/alpha
        // bands even after moving cards into one HWND. There must be exactly
        // ONE gradient and ONE watermark for the occupied wave.
        _decorations = new Canvas
        {
            Width = ModernToastScheduler.CardWidth,
            Height = 0,
            ClipToBounds = true,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        _backdrop = new Border
        {
            Width = ModernToastScheduler.CardWidth,
            Height = 0,
            Background = ModernToastCard.CreateBackdropGradient(),
            IsHitTestVisible = false
        };
        _watermark = ModernToastCard.CreateWatermark();
        Canvas.SetLeft(_watermark, 143);
        _decorations.Children.Add(_backdrop);
        _decorations.Children.Add(_watermark);
        _viewport.Children.Add(_decorations);

        Content = _viewport;
        Tag = 0;
    }

    /// <summary>
    /// Geometry is in existing C02 design units, then the viewport is scaled
    /// uniformly. Slot 0's card top is y=140 in the fixed-height 210-unit host;
    /// slot 1 is y=70, slot 2 is y=0. All settle without a gap. When entering,
    /// every card starts at y=210 below the shared visible edge.
    /// </summary>
    internal IReadOnlyList<string> Apply(IReadOnlyList<ModernToastFrame> frames,
        IReadOnlyDictionary<string, ModernToastContent> contents,
        double scale, PopupFontChoice fontChoice,
        ModernTypographyChoice typography, Rect desktopWorkArea,
        ForegroundWindowPlacement.Target? fullscreen)
    {
        if (_closed)
            throw new ObjectDisposedException(nameof(ModernToastWindow));
        if (frames.Count is < 1 or > ModernToastScheduler.Capacity)
            throw new ArgumentOutOfRangeException(nameof(frames));
        if (!double.IsFinite(scale) || scale <= 0) scale = 1;

        var currentlyShown = new HashSet<string>(
            frames.Select(f => f.Event.EventId), StringComparer.Ordinal);
        var retired = new List<string>();
        foreach (var id in _cards.Keys.Where(id => !currentlyShown.Contains(id)).ToArray())
        {
            _viewport.Children.Remove(_cards[id]);
            _cards.Remove(id);
            retired.Add(id);
        }

        // The scheduler sends oldest to newest. Adding the bottom card last
        // preserves C02's intended overlapping z-order during entry/exit.
        foreach (var frame in frames)
        {
            var id = frame.Event.EventId;
            if (!_cards.TryGetValue(id, out var card))
            {
                card = new ModernToastCard(contents[id], fontChoice, typography);
                _cards.Add(id, card);
                _viewport.Children.Add(card);
            }

            card.ApplyFontChoice(fontChoice, typography);
            card.Apply(frame);
            Canvas.SetLeft(card, 0);
            Canvas.SetTop(card, ModernStackGeometry.TopInViewport(frame));
        }

        // The moving cards always form one overlapping/contiguous occupied
        // envelope. Paint it as a single surface rather than allowing any
        // per-card background, clip or anti-aliased edge to reveal a seam.
        // Relative geometry is exactly aligned to each child's top, with no
        // arbitrary pixel overlap, border or compensating background stripe.
        var decoration = ModernStackDecorationLayout.ForFrames(frames);
        Canvas.SetTop(_decorations, decoration.Top);
        _decorations.Height = decoration.Height;
        _backdrop.Height = decoration.Height;
        Canvas.SetTop(_watermark, decoration.WatermarkTop);

        Tag = _cards.Count; // presentation-only diagnostic for real-HWND smoke

        if (_scale != scale)
        {
            _scale = scale;
            _viewport.LayoutTransform = new ScaleTransform(scale, scale);
            Width = ModernToastScheduler.CardWidth * scale;
            Height = ModernToastScheduler.CardHeight *
                     ModernToastScheduler.Capacity * scale;
        }

        // Desktop positioning is IDENTICAL to the pre-C03 work-area rule;
        // only a focused fullscreen/borderless window changes the anchor.
        // The full host always ends at target.Bottom. Empty top slots have
        // transparent pixels and never draw into the rest of the desktop.
        if (fullscreen is { } target)
        {
            target.Place(this, ModernToastScheduler.CardWidth,
                ModernStackGeometry.ViewportHeight,
                scale);
        }
        else
        {
            Left = desktopWorkArea.Right - Width;
            Top = desktopWorkArea.Bottom - Height;
        }
        return retired;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (!OperatingSystem.IsWindows()) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        var flags = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        _ = SetWindowLongPtr(hwnd, GwlExStyle,
            new IntPtr(flags | WsExTransparent | WsExToolWindow | WsExNoActivate));

        // WPF Show() materializes the HWND after Apply was called. Reapply
        // native physical-pixel placement only while the window remains live.
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_closed || !IsVisible) return;
            var fullscreen = ForegroundWindowPlacement.CaptureFullscreen();
            if (fullscreen is { } target)
                target.Place(this, ModernToastScheduler.CardWidth,
                    ModernStackGeometry.ViewportHeight,
                    _scale);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        _cards.Clear();
        base.OnClosed(e);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
