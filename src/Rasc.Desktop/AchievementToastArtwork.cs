using System.IO;
using System.Windows.Media.Imaging;

namespace Rasc.Desktop;

/// <summary>Decode already hash-verified local JPEG bytes, without loading network files or saving assets.</summary>
internal static class AchievementToastArtwork
{
    internal static BitmapSource? Decode(byte[]? jpeg)
    {
        if (jpeg is null || jpeg.Length == 0) return null;
        try
        {
            using var input = new MemoryStream(jpeg, writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 64;
            bitmap.StreamSource = input;
            bitmap.EndInit();
            if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0) return null;
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or
            InvalidOperationException or ArgumentException or FileFormatException)
        {
            return null;
        }
    }
}
