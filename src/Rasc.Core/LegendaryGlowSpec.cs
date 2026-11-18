namespace Rasc.Core;

/// <summary>
/// Presentation-only "Steam Colors" gold LegendaryAchievement reference from
/// https://codepen.io/Dataminer/pen/pooWzpo (retrieved 2026-11-18).
/// This is a behavioral/color reference, NOT the original Valve toast CSS.
/// The CodePen's 64x64 embedded mask PNGs are NOT redistributed: WPF must
/// reconstruct comparable masks from original geometric primitives.
/// </summary>
public static class LegendaryGlowSpec
{
    public const double IconSize = 48;
    public const double MaskPadding = 10;
    public const double OuterSize = IconSize + 2 * MaskPadding; // 68
    public const double MiddleSize = OuterSize + 2 * MaskPadding; // 88
    public const double LightSize = MiddleSize + 2 * MaskPadding; // 108
    public const double NearShadowBlur = 2;
    public const double FarShadowBlur = 16;
    public const double NearShadowOpacity = 0.6;
    public const double FarShadowOpacity = 0.4;
    public static readonly TimeSpan MaskReverse = TimeSpan.FromSeconds(18);
    public static readonly TimeSpan LightForward = TimeSpan.FromSeconds(6);

    public readonly record struct Stop(double Offset, byte Alpha, byte Red, byte Green, byte Blue);

    public static IReadOnlyList<Stop> RadialStops { get; } =
        Array.AsReadOnly(new Stop[]
        {
            new(0.00, 45, 255, 201, 109),
            new(0.06, 0, 255, 201, 109),
            new(0.10, 45, 255, 201, 109),
            new(0.26, 255, 255, 184, 78),
            new(0.35, 45, 255, 201, 109),
            new(0.40, 255, 255, 184, 78),
            new(0.60, 45, 255, 201, 109),
            new(0.82, 255, 255, 184, 78),
            new(1.00, 45, 255, 201, 109)
        });

    public static void Validate()
    {
        if (RadialStops.Count != 9 || RadialStops[0].Offset != 0 ||
            RadialStops[^1].Offset != 1)
            throw new InvalidOperationException("Legendary glow stops missing.");
        for (var i = 1; i < RadialStops.Count; i++)
            if (RadialStops[i].Offset <= RadialStops[i - 1].Offset)
                throw new InvalidOperationException("Legendary glow stops must be strictly ordered.");
    }
}
