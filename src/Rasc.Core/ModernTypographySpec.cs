namespace Rasc.Core;

/// <summary>
/// P004-C02: exact source CSS number/color profiles from SteamTracking's
/// steamui/sp.js+steamui/css/sp.css UI changelist 11229305.
/// JS: achievement text uses AchievementTextSection/Name/Desc.
/// CSS: Name font-size 12px, line-height 16px, weight 400 (BasicUI 500);
/// Desc font-size 12px, line-height 16px, weight 300 (BasicUI 400).
/// The two Steam modes are STATIC CSS contracts, NOT proven Steam runtime
/// computed-font pixel parity. No font binaries or original assets involved.
/// RASC geometry/positioning/text truncation is deliberately unchanged.
/// </summary>
public readonly record struct ModernTypographySpec(
    double NameSize, int NameWeight, string NameColor,
    double DescriptionSize, int DescriptionWeight, string DescriptionColor,
    double LineHeight)
{
    public static ModernTypographySpec For(ModernTypographyProfile profile) => profile switch
    {
        ModernTypographyProfile.RascAccepted =>
            new(12, 600, "#D3D8DF", 11.5, 400, "#8C96A4", double.NaN),
        ModernTypographyProfile.SteamCssDesktop =>
            new(12, 400, "#FFFFFF", 12, 300, "#8B929A", 16),
        ModernTypographyProfile.SteamCssBasicUi =>
            new(12, 500, "#FFFFFF", 12, 400, "#B8BCBF", 16),
        _ => For(ModernTypographyProfile.RascAccepted)
    };
}
