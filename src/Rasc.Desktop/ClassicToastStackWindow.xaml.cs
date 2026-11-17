using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Entire Classic stack is composed in ONE transparent topmost, click-through window.
/// No Steam injection/overlay. Tested target is primary bottom-docked taskbar only.
/// </summary>
public partial class ClassicToastStackWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;

    // Explicit physical-pixel backplates extend each card's original
    // background to its snapped cell boundary. At 125% / 175% DPI, a 94-DIP
    // card is 117.5 / 164.5 physical pixels tall: independently rounding
    // the *next* origin may otherwise expose a half-pixel seam in a 3-stack.
    // Background only is extended; icon, text and the accepted visual
    // dimensions/proportions remain completely unchanged.
    private readonly Dictionary<ClassicToastCard, Rectangle> _backplates = new();
    private static readonly Brush BackplateBrush = CreateBackplateBrush();

    private static Brush CreateBackplateBrush()
    {
        var fill = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(42, 46, 51), 0),
                new GradientStop(Color.FromRgb(18, 26, 42), 0.851),
                new GradientStop(Color.FromRgb(18, 26, 42), 1)
            }
        };
        fill.Freeze();
        return fill;
    }

    public ClassicToastStackWindow()
    {
        InitializeComponent();
    }

    public void PositionOnPrimaryWorkArea()
    {
        var work = SystemParameters.WorkArea;
        Left = work.Right - ClassicToastScheduler.CardWidth;
        Top = work.Bottom - ClassicToastScheduler.ViewportHeight;
    }

    public void Apply(ClassicToastCard card, ClassicToastFrame frame)
    {
        if (!Viewport.Children.Contains(card)) Viewport.Children.Add(card);
        // Snap the moving card to physical pixels, then align its clip in the
        // same physical coordinate system to avoid a one-pixel exposed edge.
        var dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleY;
        if (dpiScale <= 0 || !double.IsFinite(dpiScale)) dpiScale = 1;
        // Shared physical-pixel edge quantization. The upper card's bottom
        // and the lower card's top are projected from the same screen-space
        // seam, rather than separately rounded local translations/clips.
        // This guards against subpixel clipping seams on fractional DPI.
        var px = ClassicToastPixelGrid.Map(frame, dpiScale);
        var y = px.OriginY / dpiScale;
        var clipTop = Math.Clamp((px.VisibleTop - px.OriginY) / dpiScale,
            0, ClassicToastScheduler.CardHeight);
        var clipBottom = Math.Clamp((px.VisibleBottom - px.OriginY) / dpiScale,
            clipTop, ClassicToastScheduler.CardHeight);
        // The original card's 94-DIP painting surface need not reach the
        // next card's snapped physical-pixel origin on fractional DPI. Add
        // an opaque, source-color-matched underlay with pixel-aligned bounds.
        // It only fills the <1 physical pixel edge that WPF can leave clear;
        // it never changes any text/icon geometry or adds visual spacing.
        if (!_backplates.TryGetValue(card, out var backplate))
        {
            backplate = new Rectangle
            {
                Width = ClassicToastScheduler.CardWidth,
                Fill = BackplateBrush,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };
            RenderOptions.SetEdgeMode(backplate, EdgeMode.Aliased);
            _backplates.Add(card, backplate);
            Viewport.Children.Add(backplate);
        }
        // Use the SAME quantized bottom edge for the background and the
        // following card's top. This removes an actual transparent seam
        // without disguising the lower card's intentional 14-DIP top inset.
        var fullBottomPx = (int)Math.Round(
            (frame.Y + ClassicToastScheduler.CardHeight) * dpiScale,
            MidpointRounding.AwayFromZero);
        backplate.Height = Math.Max(0, fullBottomPx - px.OriginY) / dpiScale;
        backplate.Clip = new RectangleGeometry(new Rect(
            0,
            Math.Max(0, (px.VisibleTop - px.OriginY) / dpiScale),
            ClassicToastScheduler.CardWidth,
            Math.Max(0, (px.VisibleBottom - px.VisibleTop) / dpiScale)));
        Canvas.SetLeft(backplate, 0);
        Canvas.SetTop(backplate, y);

        card.Clip = new RectangleGeometry(new Rect(0, clipTop,
            ClassicToastScheduler.CardWidth, clipBottom - clipTop));
        Canvas.SetLeft(card, 0);
        Canvas.SetTop(card, y);
        // Each lower card (and its matching underlay) occludes the card
        // above during downward sink transitions.
        Panel.SetZIndex(backplate, -2 * frame.Slot);
        Panel.SetZIndex(card, -2 * frame.Slot + 1);
    }

    public void Remove(ClassicToastCard card)
    {
        Viewport.Children.Remove(card);
        if (_backplates.Remove(card, out var backplate))
            Viewport.Children.Remove(backplate);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (!OperatingSystem.IsWindows()) return;
        var handle = new WindowInteropHelper(this).Handle;
        var flags = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        _ = SetWindowLongPtr(handle, GwlExStyle, new IntPtr(
            flags | WsExTransparent | WsExToolWindow | WsExNoActivate));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
}
