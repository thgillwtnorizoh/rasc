using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
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
        card.Clip = new RectangleGeometry(new Rect(0, clipTop,
            ClassicToastScheduler.CardWidth, clipBottom - clipTop));
        Canvas.SetLeft(card, 0);
        Canvas.SetTop(card, y);
        Panel.SetZIndex(card, -frame.Slot); // lower card always occludes higher
    }

    public void Remove(ClassicToastCard card) => Viewport.Children.Remove(card);

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
