namespace Rasc.Core;

/// <summary>
/// Source-independent viewport geometry for one continuous Modern wave
/// background and watermark. All card content remains independent, but their
/// common backdrop covers the entire union of card rectangles with no
/// per-card clipping edge. This is deliberate, NOT a pixel-overlap workaround.
/// </summary>
public static class ModernStackDecorationLayout
{
    public readonly record struct Bounds(double Top, double Height,
        double WatermarkTop)
    {
        public double Bottom => Top + Height;
    }

    public static Bounds ForFrames(IReadOnlyList<ModernToastFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count is < 1 or > ModernToastScheduler.Capacity)
            throw new ArgumentOutOfRangeException(nameof(frames));
        var top = double.PositiveInfinity;
        var bottom = double.NegativeInfinity;
        foreach (var frame in frames)
        {
            var position = ModernStackGeometry.TopInViewport(frame);
            top = Math.Min(top, position);
            bottom = Math.Max(bottom, position + ModernToastScheduler.CardHeight);
        }
        var length = bottom - top;
        if (double.IsNaN(length) || !double.IsFinite(length) || length < 0)
            throw new ArgumentException("Invalid wave geometry.", nameof(frames));
        return new Bounds(top, length, (length - ModernWatermarkLayout.LogoHeight) / 2.0);
    }
}
