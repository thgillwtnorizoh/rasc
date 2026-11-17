namespace Rasc.Core;

/// <summary>
/// Pure screen-space integer pixel clipping for the one-window Classic stack.
/// No gameplay/window dependencies. Logical card spacing remains exactly ZERO.
/// Prevents independent card origin/clip rounding at non-integer Windows DPI.
/// </summary>
public static class ClassicToastPixelGrid
{
    public static ClassicToastPixelSpan Map(ClassicToastFrame frame, double dpiScale)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!double.IsFinite(dpiScale) || dpiScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpiScale));

        static int Snap(double v) =>
            checked((int)Math.Round(v, MidpointRounding.AwayFromZero));

        var viewportBottom = Snap(ClassicToastScheduler.ViewportHeight * dpiScale);
        var origin = Snap(frame.Y * dpiScale);
        var top = Math.Clamp(Snap((frame.Y + frame.ClipTop) * dpiScale), 0, viewportBottom);
        var bottom = Math.Clamp(Snap((frame.Y + frame.ClipBottom) * dpiScale), top, viewportBottom);
        return new ClassicToastPixelSpan(origin, top, bottom);
    }
}

/// <param name="OriginY">Physical screen-pixel top of the card, including clipped area.</param>
/// <param name="VisibleTop">Absolute viewport physical pixel of the upper clip edge.</param>
/// <param name="VisibleBottom">Absolute viewport physical pixel of the lower clip edge.</param>
public readonly record struct ClassicToastPixelSpan(int OriginY, int VisibleTop, int VisibleBottom)
{
    public int VisibleHeight => VisibleBottom - VisibleTop;
}
