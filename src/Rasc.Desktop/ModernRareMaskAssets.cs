using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rasc.Desktop;

/// <summary>
/// Exact 64x64 alpha masks supplied by the owner from the approved CodePen
/// gold LegendaryAchievement reference. Loaded as embedded WPF resources,
/// with no runtime network dependency or Steam file modification.
///
/// Note: the user-supplied masks visibly match the CodePen embedded payloads.
/// Attribution to a specific original Valve CLIENT BUILD is not independently
/// established. The repository/asset-distribution rights gate remains OPEN.
/// </summary>
internal static class ModernRareMaskAssets
{
    private static readonly Lazy<BitmapSource> SquareLazy =
        new(() => Load("achievement_square_mask"));
    private static readonly Lazy<BitmapSource> StarLazy =
        new(() => Load("achievement_star_mask"));

    internal static BitmapSource Square => SquareLazy.Value;
    internal static BitmapSource Star => StarLazy.Value;

    private static BitmapSource Load(string name)
    {
        var path = new Uri(
            $"pack://application:,,,/Rasc.Desktop;component/Assets/SteamRareMasks/{name}.png",
            UriKind.Absolute);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = path;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.EndInit();

        if (bitmap.PixelWidth != 64 || bitmap.PixelHeight != 64)
            throw new InvalidDataException($"Steam glow mask {name} is not 64x64.");

        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// WPF OpacityMask uses the decoded PNG alpha channel. CSS uses
    /// mask-image/mask-size:100%: scale the original alpha to the full
    /// outer 68x68 square or rotating 88x88 star container.
    /// </summary>
    internal static ImageBrush AlphaBrush(BitmapSource source, double maskExtent)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!double.IsFinite(maskExtent) || maskExtent <= 0)
            throw new ArgumentOutOfRangeException(nameof(maskExtent));
        // CSS mask-size:100% is relative to its 68px/88px element box,
        // not the potentially larger bounds of painted rotating children.
        var brush = new ImageBrush(source)
        {
            Stretch = Stretch.Fill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
            TileMode = TileMode.None,
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, source.PixelWidth, source.PixelHeight),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, maskExtent, maskExtent)
        };
        brush.Freeze();
        return brush;
    }
}
