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

        internal double Scale(double width, double height, int percent) =>
            PopupAnchorGeometry.AutoScale(HeightDips, WidthDips, HeightDips,
                width, height, percent);

        /// <param name="offsetDip">Motion offset of top-left from bottom edge
        /// of target app, in unscaled card DIPs (0 starts below the app).</param>
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
                // Initial WPF placement only. OnSourceInitialized and next
                // composition tick reapply exact native physical placement.
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

    internal static Target Capture()
    {
        // All native rectangles are physical device pixels for a PMv2 WPF app.
        var foreground = GetForegroundWindow();
        var monitor = MonitorFromWindow(foreground != IntPtr.Zero
            ? foreground : GetDesktopWindow(), MonitorDefaultToNearest);

        var mi = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref mi))
            return new Target(new PopupRect(SystemParameters.WorkArea.Left,
                SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Width,
                SystemParameters.WorkArea.Height), 1);

        var monitorRect = Rect(mi.Monitor);
        var work = Rect(mi.Work);
        var windowRect = default(NativeRect);
        var valid = foreground != IntPtr.Zero && IsWindowVisible(foreground)
            && !IsIconic(foreground) && GetWindowRect(foreground, out windowRect);
        if (valid)
        {
            try
            {
                // Prefer visible frame bounds to invisible DWM resize borders.
                if (DwmGetWindowAttribute(foreground, DwmaExtendedFrameBounds,
                    out var visible, Marshal.SizeOf<NativeRect>()) == 0)
                    windowRect = visible;
            }
            catch (DllNotFoundException) { /* WPF fallback to GetWindowRect */ }
            catch (EntryPointNotFoundException) { }
        }

        var geometry = PopupAnchorGeometry.Resolve(
            Rect(windowRect), monitorRect, work, valid);
        uint dx = 96, dy = 96;
        try
        {
            if (GetDpiForMonitor(monitor, MdtEffectiveDpi, out dx, out dy) != 0)
                dx = 96;
        }
        catch (DllNotFoundException) { dx = 96; }
        catch (EntryPointNotFoundException) { dx = 96; }
        var dpi = dx is >= 48 and <= 960 ? dx / 96.0 : 1.0;
        return new Target(geometry, dpi);
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
