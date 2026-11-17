namespace Rasc.Core;

/// <summary>
/// Non-Windows math for foreground-app positioning. Inputs/outputs share one
/// coordinate system (native pixels or WPF DIPs); no DPI conversion here.
/// </summary>
public readonly record struct PopupRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public bool IsValid => double.IsFinite(Left) && double.IsFinite(Top)
        && double.IsFinite(Width) && double.IsFinite(Height)
        && Width > 0 && Height > 0;
}

public static class PopupAnchorGeometry
{
    /// <summary>
    /// A true borderless/fullscreen window occupies its monitor, including the
    /// taskbar strip. Otherwise constrain its visible bounds to the usable
    /// monitor area, so popups never overrun a normal desktop taskbar.
    /// </summary>
    public static PopupRect Resolve(PopupRect foreground, PopupRect monitor,
        PopupRect workArea, bool foregroundValid)
    {
        if (!monitor.IsValid || !workArea.IsValid) throw new ArgumentException("Monitor rectangles must be valid.");
        if (!foregroundValid || !foreground.IsValid) return workArea;
        // Allow a 2-unit tolerance for rounded operating-system boundaries.
        var fullscreen = foreground.Left <= monitor.Left + 2
            && foreground.Top <= monitor.Top + 2
            && foreground.Right >= monitor.Right - 2
            && foreground.Bottom >= monitor.Bottom - 2;
        if (fullscreen) return monitor;
        var left = Math.Max(foreground.Left, workArea.Left);
        var top = Math.Max(foreground.Top, workArea.Top);
        var right = Math.Min(foreground.Right, workArea.Right);
        var bottom = Math.Min(foreground.Bottom, workArea.Bottom);
        return right > left && bottom > top
            ? new PopupRect(left, top, right - left, bottom - top)
            : workArea;
    }

    /// <summary>
    /// Compute the native physical rectangle for a toast, regardless of which
    /// monitor hosts it. Null bottomRelativeDip docks the entire window to the
    /// lower-right corner; other values animate from bottom = 0 (hidden).
    /// </summary>
    public static PopupRect Position(PopupRect areaPixels, double dpiScale,
        double designWidthDip, double designHeightDip, double contentScale,
        double? bottomRelativeDip = null)
    {
        if (!areaPixels.IsValid || !double.IsFinite(dpiScale) || dpiScale <= 0 ||
            !double.IsFinite(designWidthDip) || designWidthDip <= 0 ||
            !double.IsFinite(designHeightDip) || designHeightDip <= 0 ||
            !double.IsFinite(contentScale) || contentScale <= 0 ||
            (bottomRelativeDip.HasValue && !double.IsFinite(bottomRelativeDip.Value)))
            throw new ArgumentOutOfRangeException(nameof(areaPixels));
        var width = Math.Max(1, Math.Round(designWidthDip * contentScale * dpiScale));
        var height = Math.Max(1, Math.Round(designHeightDip * contentScale * dpiScale));
        var x = Math.Round(areaPixels.Right) - width;
        var y = bottomRelativeDip.HasValue
            ? Math.Round(areaPixels.Bottom + bottomRelativeDip.Value * contentScale * dpiScale)
            : Math.Round(areaPixels.Bottom) - height;
        return new PopupRect(x, y, width, height);
    }

    /// <summary>Scale relative to foreground content height (1080p baseline).</summary>
    public static double AutoScale(double targetHeight, double width, double height,
        double originalWidth, double originalHeight, int requestedPercent)
    {
        if (!double.IsFinite(targetHeight) || targetHeight <= 0 ||
            !double.IsFinite(width) || width <= 0 ||
            !double.IsFinite(height) || height <= 0)
            return 1;
        if (!double.IsFinite(originalWidth) || originalWidth <= 0 ||
            !double.IsFinite(originalHeight) || originalHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalWidth));
        var desired = requestedPercent == 0
            ? Math.Clamp(targetHeight / 1080.0, 0.5, 2.0)
            : requestedPercent / 100.0;
        var fit = Math.Min(width / originalWidth, height / originalHeight);
        // Tiny windows are allowed to scale below 50% when essential to fit.
        return Math.Min(desired, fit);
    }
}
