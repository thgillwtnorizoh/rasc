using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Read-only foreground HWND placement for BOTH presentation profiles.
/// Native screen rectangles and SetWindowPos are in physical pixels to avoid
/// projecting the primary monitor's WPF work-area onto a second display.
/// This does not hook or inject into the foreground process.
/// </summary>
internal static class ForegroundWindowPlacement
{
    internal readonly record struct Target(PopupRect AreaPixels, double DpiScale)
    {
        public double WidthDips => AreaPixels.Width / DpiScale;
        public double HeightDips => AreaPixels.Height / DpiScale;

        internal double ClassicScale(int percent) =>
            ClassicPopupScale.Resolve(HeightDips, WidthDips, HeightDips, percent);

        internal double ModernScale(int percent)
        {
            var desired = percent == 0
                ? Math.Clamp(HeightDips / 1080.0, 1, 2)
                : percent / 100.0;
            var fit = Math.Min(WidthDips / ModernToastScheduler.CardWidth,
                HeightDips / (ModernToastScheduler.CardHeight * ModernToastScheduler.Capacity));
            return Math.Max(0.1, Math.Min(desired, fit));
        }

        internal void Place(Window window, double designWidth, double designHeight,
            double scale, double? offsetDip = null)
        {
            var rect = PopupAnchorGeometry.Position(AreaPixels, DpiScale,
                designWidth, designHeight, scale, offsetDip);
            var x = (int)rect.Left;
            var y = (int)rect.Top;
            var pixelsWide = (int)rect.Width;
            var pixelsHigh = (int)rect.Height;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                window.Left = x / DpiScale;
                window.Top = y / DpiScale;
                return;
            }

            if (GetWindowRect(hwnd, out var current) &&
                current.Left == x && current.Top == y &&
                current.Right - current.Left == pixelsWide &&
                current.Bottom - current.Top == pixelsHigh)
                return;
            _ = SetWindowPos(hwnd, IntPtr.Zero, x, y, pixelsWide, pixelsHigh,
                SwpNoActivate | SwpNoZOrder | SwpNoOwnerZOrder);
        }
    }

    /// <summary>
    /// Returns null for ALL normal/windowed/maximized focused applications,
    /// not their bounds. In that case clients must use their exact pre-C03
    /// SystemParameters.WorkArea positioning and existing Auto scale.
    /// </summary>
    internal static Target? CaptureFullscreen()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || !IsWindowVisible(foreground) ||
            IsIconic(foreground) || !GetWindowRect(foreground, out var windowRect))
            return null;
        var monitor = MonitorFromWindow(foreground, MonitorDefaultToNearest);
        var mi = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref mi))
            return null;

        try
        {
            if (DwmGetWindowAttribute(foreground, DwmaExtendedFrameBounds,
                out var visible, Marshal.SizeOf<NativeRect>()) == 0 &&
                Rect(visible).IsValid)
                windowRect = visible;
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }

        var monitorRect = Rect(mi.Monitor);
        if (!PopupAnchorGeometry.IsFullscreen(Rect(windowRect), monitorRect, true))
            return null;

        uint dpi = 96;
        try
        {
            if (GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dx, out _) == 0)
                dpi = dx;
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }

        return new Target(monitorRect, dpi is >= 48 and <= 960 ? dpi / 96.0 : 1.0);
    }

    private static PopupRect Rect(NativeRect rect) =>
        new(rect.Left, rect.Top,
            rect.Right - rect.Left, rect.Bottom - rect.Top);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }

    private const int MonitorDefaultToNearest = 2;
    private const int DwmaExtendedFrameBounds = 9;
    private const int MdtEffectiveDpi = 0;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd, int flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hmonitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter,
        int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(
        IntPtr hwnd, int attribute, out NativeRect rect, int size);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(
        IntPtr monitor, int type, out uint dpiX, out uint dpiY);
}
