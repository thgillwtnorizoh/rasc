namespace Rasc.Core;

/// <summary>
/// All Modern cards occupy ONE 283x210 viewport with a shared bottom origin.
/// These placements are in logical design units before one uniform viewport
/// scale is applied. This prevents per-card HWND rounding and 1-2 pixel
/// inter-window compositor seams on 100%, fractional and mixed-DPI desktops.
/// </summary>
public static class ModernStackGeometry
{
    public const double ViewportHeight =
        ModernToastScheduler.CardHeight * ModernToastScheduler.Capacity;

    public static double TopInViewport(ModernToastFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Slot < 0 || frame.Slot >= ModernToastScheduler.Capacity)
            throw new ArgumentOutOfRangeException(nameof(frame));
        if (!double.IsFinite(frame.BottomRelativeY))
            throw new ArgumentOutOfRangeException(nameof(frame));
        return ViewportHeight + frame.BottomRelativeY;
    }
}
