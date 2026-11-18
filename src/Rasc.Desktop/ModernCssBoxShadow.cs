using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// One transparent shadow raster for the ONE 48px icon in Dataminer's
/// LegendaryAchievement CodePen. CSS specifies TWO box-shadows on the SAME
/// .fakeIcon (2px and 16px blur, 1px spread), not two independently opaque
/// WPF Borders with two DropShadowEffects as the former C05 renderer did.
///
/// CSS box-shadow blur is represented by a Gaussian convolution of an
/// axis-aligned rectangle expanded by the declared 1px spread. We use
/// sigma=blurRadius/2 as the CSS blur-radius approximation; precise browser
/// antialiasing/gamma can still differ and is tested separately.
/// The near shadow is painted over the far shadow, both BEHIND the icon.
/// No extraneous solid rectangles or guessed brightness adjustments.
/// </summary>
internal static class ModernCssBoxShadow
{
    internal const int SpriteSize = 128;
    internal const int IconOrigin = 40;
    internal const int SourceSize = 48;
    private static readonly Lazy<BitmapSource> ShadowLazy = new(CreateShadow);

    internal static BitmapSource Shadow => ShadowLazy.Value;

    private static BitmapSource CreateShadow()
    {
        var pixels = GeneratePbgra32();
        var source = BitmapSource.Create(SpriteSize, SpriteSize,
            96, 96, PixelFormats.Pbgra32, null, pixels, SpriteSize * 4);
        source.Freeze();
        return source;
    }

    internal static byte[] GeneratePbgra32()
    {
        var near = GaussianShadow(LegendaryGlowSpec.NearShadowBlur);
        var far = GaussianShadow(LegendaryGlowSpec.FarShadowBlur);
        var pixels = new byte[SpriteSize * SpriteSize * 4];

        // Original CSS order: 2px amber appears ABOVE 16px yellow.
        // Blend once into a transparent premultiplied-alpha image.
        for (var i = 0; i < near.Length; ++i)
        {
            var aN = near[i] * LegendaryGlowSpec.NearShadowOpacity;
            var aF = far[i] * LegendaryGlowSpec.FarShadowOpacity;
            var back = aF * (1.0 - aN);
            var alpha = aN + back;

            // Source CSS rgba(255,184,78,.6), rgba(255,217,78,.4).
            // PixelFormats.Pbgra32 stores B,G,R premultiplied by alpha.
            var p = 4 * i;
            pixels[p] = ToByte(78.0 * alpha);
            pixels[p + 1] = ToByte(184.0 * aN + 217.0 * back);
            pixels[p + 2] = ToByte(255.0 * alpha);
            pixels[p + 3] = ToByte(255.0 * alpha);
        }
        return pixels;
    }

    private static double[] GaussianShadow(double cssBlurRadius)
    {
        if (!double.IsFinite(cssBlurRadius) || cssBlurRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(cssBlurRadius));
        if (LegendaryGlowSpec.ShadowSpread != 1)
            throw new InvalidOperationException("Unexpected CSS spread.");

        var n = SpriteSize;
        var baseMask = new double[n * n];
        var left = IconOrigin - (int)LegendaryGlowSpec.ShadowSpread;
        var right = IconOrigin + SourceSize + (int)LegendaryGlowSpec.ShadowSpread;
        for (var y = left; y < right; y++)
        for (var x = left; x < right; x++)
            baseMask[y * n + x] = 1.0;

        // Blur radii 2px and 16px -> sigma 1px and 8px respectively.
        var sigma = cssBlurRadius * LegendaryGlowSpec.CssBlurSigmaFactor;
        var radius = (int)Math.Ceiling(sigma * 4);
        var kernel = new double[2 * radius + 1];
        double sum = 0;
        for (var k = -radius; k <= radius; k++)
        {
            var weight = Math.Exp(-.5 * (k / sigma) * (k / sigma));
            kernel[k + radius] = weight;
            sum += weight;
        }
        for (var k = 0; k < kernel.Length; k++)
            kernel[k] /= sum;

        var horizontal = new double[n * n];
        var output = new double[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            double value = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var sx = x + k;
                if (sx is >= 0 and < SpriteSize)
                    value += baseMask[y * n + sx] * kernel[k + radius];
            }
            horizontal[y * n + x] = value;
        }
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            double value = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var sy = y + k;
                if (sy is >= 0 and < SpriteSize)
                    value += horizontal[sy * n + x] * kernel[k + radius];
            }
            output[y * n + x] = Math.Clamp(value, 0, 1);
        }
        return output;
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);
}
