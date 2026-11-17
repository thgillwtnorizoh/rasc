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
/// No Steam injection/overlay. P003-C03 tracks active foreground app bounds;
/// mixed-DPI/true-exclusive-fullscreen runtime qualification remains pending.
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
    private int _popupSizePercent;
    private double _contentScale = 1;
    private HwndSource? _hwndSource;
    private const int WmDpiChanged = 0x02E0;
    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;

    public event EventHandler? DisplayMetricsChanged;
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

    public void SetPopupSizePercent(int requestedPercent)
    {
        _popupSizePercent = ClassicPresentationSettings.SupportedSizes.Contains(requestedPercent)
            ? requestedPercent : 0;
        PositionOnForegroundApp();
    }

    /// <summary>
    /// C03-C01 correction: preserve the accepted pre-C03 desktop anchor,
    /// size and work-area behavior UNLESS a focused window covers the entire
    /// monitor (exclusive/borderless fullscreen). Never anchor inside
    /// ordinary foreground applications.
    /// </summary>
    public void PositionOnForegroundApp()
    {
        var fullscreen = ForegroundWindowPlacement.CaptureFullscreen();
        var work = SystemParameters.WorkArea;
        var proposedScale = fullscreen is { } target
            ? target.ClassicScale(_popupSizePercent)
            : ClassicPopupScale.Resolve(
                SystemParameters.PrimaryScreenHeight,
                work.Width, work.Height, _popupSizePercent);

        if (Math.Abs(_contentScale - proposedScale) > 1e-9)
        {
            _contentScale = proposedScale;
            Viewport.LayoutTransform = new ScaleTransform(_contentScale, _contentScale);
        }

        // These are the old exact Classic dimensions, including its
        // approved three-card viewport and clipping semantics.
        Width = ClassicToastScheduler.CardWidth * _contentScale;
        Height = ClassicToastScheduler.ViewportHeight * _contentScale;
        if (fullscreen is { } fs)
        {
            fs.Place(this, ClassicToastScheduler.CardWidth,
                ClassicToastScheduler.ViewportHeight, _contentScale);
        }
        else
        {
            Left = work.Right - Width;
            Top = work.Bottom - Height;
        }
    }

    public void Apply(ClassicToastCard card, ClassicToastFrame frame)
    {
        if (!Viewport.Children.Contains(card)) Viewport.Children.Add(card);
        // Snap the moving card to physical pixels, then align its clip in the
        // same physical coordinate system to avoid a one-pixel exposed edge.
        // Card coordinates are unscaled 240x282 design units. WPF's
        // LayoutTransform multiplies by the user's size setting; physical
        // pixel snapping must use the PRODUCT of UI scale and Windows DPI.
        var systemDpi = VisualTreeHelper.GetDpi(this).DpiScaleY;
        if (systemDpi <= 0 || !double.IsFinite(systemDpi)) systemDpi = 1;
        var dpiScale = systemDpi * _contentScale;
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
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(OnWindowMessage);
        // Initial sizing may predate the actual HWND.
        _ = Dispatcher.BeginInvoke(new Action(PositionOnForegroundApp),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam,
        IntPtr lParam, ref bool handled)
    {
        if (msg is WmDpiChanged or WmDisplayChange or WmSettingChange)
        {
            // WPF processes the DPI change first. Reanchor afterwards using
            // current primary work-area and logical display dimensions.
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsLoaded) return;
                PositionOnForegroundApp();
                DisplayMetricsChanged?.Invoke(this, EventArgs.Empty);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        _hwndSource?.RemoveHook(OnWindowMessage);
        _hwndSource = null;
        base.OnClosed(e);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
}
