using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rasc.Core;
using System.Windows.Threading;
using Rasc.Desktop;

namespace Rasc.Desktop.Smoke;

/// <summary>
/// Automated developer WPF UI smoke, not proof of screen pixels or CS:GO
/// exclusive-fullscreen support. Uses presentation-only fixtures and checks
/// that Modern -> Classic never overlaps HWND lifetimes or crashes on exit.
/// A real Windows GUI compositor is required; CI failures remain evidence.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 1 || args[0] is not ("classic-only" or "modern-classic" or "modern-progress" or "modern-idle-classic" or "modern-stack-two" or "modern-rare"))
        {
            Console.Error.WriteLine("Usage: Rasc.Desktop.Smoke classic-only|modern-classic|modern-progress|modern-idle-classic|modern-stack-two|modern-rare");
            return 2;
        }
        var scenario = args[0];
        Exception? fault = null;
        bool failed = false;
        bool modernObserved = false;
        bool classicObserved = false;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.DispatcherUnhandledException += (_, e) =>
        {
            fault = e.Exception;
            failed = true;
            e.Handled = true; // only in this TEST HARNESS; real RASC doesn't swallow.
            app.Shutdown();
        };

        try
        {
            var window = new MainWindow();
            app.MainWindow = window;
            window.Show();
            var profile = (ComboBox?)window.FindName("ProfileBox")
                ?? throw new InvalidOperationException("Missing profile selector.");
            var preview = (Button?)window.FindName("PreviewUnlockButton")
                ?? throw new InvalidOperationException("Missing unlock fixture button.");
            var wave = (Button?)window.FindName("PreviewModernWaveButton")
                ?? throw new InvalidOperationException("Missing wave fixture button.");
            var progress = (Button?)window.FindName("PreviewProgressButton")
                ?? throw new InvalidOperationException("Missing progress fixture button.");
            var rare = (Button?)window.FindName("PreviewRareButton")
                ?? throw new InvalidOperationException("Missing rare glow fixture button.");
            var waveCount = (ComboBox?)window.FindName("PreviewWaveCountBox")
                ?? throw new InvalidOperationException("Missing wave count selector.");

            void Select(string name)
            {
                var match = profile.Items.OfType<ComboBoxItem>()
                    .Single(item => (string?)item.Tag == name);
                profile.SelectedItem = match;
                if ((string?)((ComboBoxItem)profile.SelectedItem).Tag != name)
                    throw new InvalidOperationException("Profile switch did not commit: " + name);
            }

            static void Click(Button button) =>
                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            int Count(string name) => app.Windows.Cast<Window>()
                .Count(w => w.IsVisible && w.GetType().Name == name);
            int modernWindowCount() => Count("ModernToastWindow");
            // C03-C03 deliberately uses ONE HWND for N Modern cards. Tag
            // exposes presentation-only child count to the smoke harness.
            int modernCount() => app.Windows.Cast<Window>()
                .Where(w => w.IsVisible && w.GetType().Name == "ModernToastWindow")
                .Sum(w => w.Tag is int count ? count : 0);
            int classicCount() => Count("ClassicToastStackWindow");

            void VerifyOneSeamlessSurface(int count)
            {
                if (modernWindowCount() != 1 || modernCount() != count)
                    throw new InvalidOperationException(
                        $"Expected one Modern HWND with {count} cards.");
                var host = app.Windows.Cast<Window>()
                    .Single(w => w.IsVisible && w.GetType().Name == "ModernToastWindow");
                if (host.Content is not Canvas canvas)
                    throw new InvalidOperationException("Modern host is not a Canvas.");

                var cards = canvas.Children.Cast<UIElement>()
                    .Where(c => c.GetType().Name == "ModernToastCard").ToArray();
                if (cards.Length != count || canvas.Children.Count != count + 1)
                    throw new InvalidOperationException(
                        $"Expected {count} cards plus ONE shared wave decoration.");

                var decor = canvas.Children.OfType<Canvas>().Single();
                if (decor.Children.Count != 2 ||
                    decor.Children[0] is not Border backdrop ||
                    backdrop.Background is not RadialGradientBrush)
                    throw new InvalidOperationException(
                        "One continuous gradient and shared watermark required.");
                var expectedTop = ModernStackGeometry.ViewportHeight -
                    count * ModernToastScheduler.CardHeight;
                if (Math.Abs(Canvas.GetTop(decor) - expectedTop) > .0001 ||
                    Math.Abs(decor.Height - count * ModernToastScheduler.CardHeight) > .0001)
                    throw new InvalidOperationException(
                        "Wave background does not cover whole occupied stack.");

                var tops = cards.Select(Canvas.GetTop).OrderBy(y => y).ToArray();
                for (var i = 0; i < count; i++)
                {
                    var expected = expectedTop + i * ModernToastScheduler.CardHeight;
                    if (Math.Abs(tops[i] - expected) > .0001)
                        throw new InvalidOperationException(
                            $"Modern card {i} top {tops[i]} != {expected}");
                }

                foreach (var card in cards)
                {
                    if (card is not UserControl view ||
                        view.Content is not Canvas contents ||
                        !ReferenceEquals(contents.Background, Brushes.Transparent))
                        throw new InvalidOperationException(
                            "Per-card gradients reintroduced a raster boundary.");
                }

                // Render the REAL WPF visual tree at fractional DPI, not just
                // the geometry. Test only internal boundaries, not the top of
                // a wave, which is meant to reveal transparency above it.
                canvas.UpdateLayout();
                foreach (var dpiFactor in new[]{1.0, 1.25, 1.5})
                {
                    var pxWidth = (int)Math.Ceiling(ModernToastScheduler.CardWidth * dpiFactor);
                    var pxHeight = (int)Math.Ceiling(ModernStackGeometry.ViewportHeight * dpiFactor);
                    var bitmap = new RenderTargetBitmap(
                        pxWidth, pxHeight, 96 * dpiFactor, 96 * dpiFactor,
                        PixelFormats.Pbgra32);
                    bitmap.Render(canvas);
                    var stride = pxWidth * 4;
                    var pixels = new byte[stride * pxHeight];
                    bitmap.CopyPixels(pixels, stride, 0);
                    for (var n = 1; n < count; n++)
                    {
                        var boundary = (int)Math.Round(
                            (expectedTop + n * ModernToastScheduler.CardHeight) * dpiFactor);
                        // x≈2 now legitimately intersects the moving
                        // rare-glow halo of a card that begins at this
                        // boundary. Assert the background in two regions
                        // OUTSIDE icons (x<69), labels and watermark.
                        // Do not classify intended gold light as a seam.
                        foreach (var designX in new[] { 112, 279 })
                        {
                            var sampleX = Math.Min(pxWidth - 2,
                                (int)Math.Round(designX * dpiFactor));
                            for (var y = boundary - 2; y <= boundary + 2; y++)
                            {
                                var a = (y * pxWidth + sampleX) * 4;
                                var b = ((y - 1) * pxWidth + sampleX) * 4;
                                if (pixels[a + 3] < 250 || pixels[b + 3] < 250)
                                    throw new InvalidOperationException(
                                        $"Transparent gap at {count} cards, {dpiFactor}x, x={designX}, y={y}");
                                for (var channel = 0; channel < 3; channel++)
                                    if (Math.Abs(pixels[a + channel] - pixels[b + channel]) > 8)
                                        throw new InvalidOperationException(
                                            $"Background seam at {count} cards, {dpiFactor}x, x={designX}, y={y}");
                            }
                        }
                    }
                }
            }

            static IEnumerable<DependencyObject> Descendants(DependencyObject ancestor)
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(ancestor); i++)
                {
                    var next = VisualTreeHelper.GetChild(ancestor, i);
                    yield return next;
                    foreach (var child in Descendants(next))
                        yield return child;
                }
            }

            byte[] SnapshotRareCard()
            {
                var host = app.Windows.Cast<Window>()
                    .Single(w => w.IsVisible &&
                        w.GetType().Name == "ModernToastWindow");
                if (host.Content is not Canvas canvas)
                    throw new InvalidOperationException("Modern rare host lacks viewport.");
                var card = canvas.Children.Cast<UIElement>()
                    .Single(c => c.GetType().Name == "ModernToastCard");

                var masked = Descendants(card)
                    .OfType<Canvas>().Where(c =>
                        c.Tag is "SteamRareGlowRoot" or "SteamRareGlowRotatingMask")
                    .ToArray();
                if (masked.Length != 2 ||
                    masked.Any(c => c.OpacityMask is not ImageBrush mask ||
                        mask.ImageSource is not BitmapSource bitmap ||
                        bitmap.PixelWidth != 64 || bitmap.PixelHeight != 64))
                    throw new InvalidOperationException(
                        "Source PNG opacity masks not installed on rare glow layers.");

                var wrapper = Descendants(card).OfType<Canvas>()
                    .Single(c => (string?)c.Tag == "LegendaryIconWrapper");
                if (wrapper.Width != LegendaryGlowSpec.IconSize ||
                    wrapper.Height != LegendaryGlowSpec.IconSize ||
                    wrapper.ClipToBounds ||
                    Canvas.GetLeft(wrapper) != 10 ||
                    Canvas.GetTop(wrapper) != 11)
                    throw new InvalidOperationException(
                        "CodePen icon wrapper dimensions, position or overflow changed.");
                var rootElement = masked.Single(c =>
                    (string?)c.Tag == "SteamRareGlowRoot");
                // A CSS mask clips the content to its *mask box*.
                // WPF OpacityMask by itself does not clip overflow from
                // nested rotated children; a real WPF clip is required.
                if (!rootElement.ClipToBounds)
                    throw new InvalidOperationException(
                        "WPF root mask box must clip rotating descendants.");

                var shadows = Descendants(wrapper).OfType<Image>()
                    .Where(i => (string?)i.Tag == "CodePenDualBoxShadow").ToArray();
                if (shadows.Length != 1 || shadows[0].Source is not BitmapSource src ||
                    src.PixelWidth != 128 || src.PixelHeight != 128)
                    throw new InvalidOperationException(
                        "CSS dual shadow must use one 128px transparent raster.");
                if (Descendants(wrapper).OfType<UIElement>().Any(element =>
                        element.Effect is System.Windows.Media.Effects.DropShadowEffect))
                    throw new InvalidOperationException(
                        "Regression: opaque WPF DropShadowEffect plates reintroduced.");

                var shadowPixels = new byte[128 * 128 * 4];
                src.CopyPixels(shadowPixels, 128 * 4, 0);
                int ShadowAlpha(int x) => shadowPixels[(64 * 128 + x) * 4 + 3];
                // Two separated blur radii must produce a smooth falloff
                // outside the CSS-spread 48px square; a hard gold border
                // fails this test even if its overall glow is bright.
                if (!(ShadowAlpha(36) > ShadowAlpha(24) &&
                      ShadowAlpha(24) > ShadowAlpha(8) &&
                      ShadowAlpha(8) <= 2))
                    throw new InvalidOperationException(
                        "Two-shadow CSS blur/spread profile is not smooth.");
                Console.WriteLine(
                    $"CODEPEN SHADOW alpha x36={ShadowAlpha(36)}, " +
                    $"x24={ShadowAlpha(24)}, x8={ShadowAlpha(8)}");

                foreach (var component in masked)
                {
                    var png = (BitmapSource)((ImageBrush)component.OpacityMask!).ImageSource;
                    var pixelsSource = new FormatConvertedBitmap(
                        png, PixelFormats.Bgra32, null, 0);
                    var raw = new byte[64 * 64 * 4];
                    pixelsSource.CopyPixels(raw, 64 * 4, 0);
                    var alpha = Enumerable.Range(0, 64 * 64)
                        .Select(i => raw[i * 4 + 3]).ToArray();
                    Console.WriteLine(
                        $"RARE MASK {component.Tag}: alpha {alpha.Min()}..{alpha.Max()}, " +
                        $"nonzero={alpha.Count(a => a > 0)}, " +
                        $"partial={alpha.Count(a => a is > 0 and < 255)}, " +
                        $"center={alpha[(32 * 64 + 32)]}");
                    if (alpha.Max() == 0 || alpha.Distinct().Count() < 2)
                        throw new InvalidOperationException(
                            $"Mask {component.Tag} has no usable alpha image.");
                }

                // WPF RenderTargetBitmap of an already-positioned child
                // UserControl returned ALL TRANSPARENT pixels, including
                // the icon itself (false-negative in first smoke attempt).
                // Render the same complete Modern viewport that the earlier
                // owner-qualified seam tests used, then crop the bottom
                // 70-unit card into LOCAL coordinates before measuring gold.
                // The backdrop is opaque, but nonrare background is blue
                // gray (R < B); pixels with R > B are gold light. The
                // fixed yellow star in the icon interior is excluded.
                var cardElement = (FrameworkElement)card;
                canvas.UpdateLayout();
                var raster = new RenderTargetBitmap(283,
                    (int)ModernStackGeometry.ViewportHeight, 96, 96,
                    PixelFormats.Pbgra32);
                raster.Render(canvas);
                var full = new byte[283 * (int)ModernStackGeometry.ViewportHeight * 4];
                raster.CopyPixels(full, 283 * 4, 0);
                var bytes = new byte[283 * 70 * 4];
                var top = (int)Canvas.GetTop((UIElement)card);
                if (top != 140)
                    throw new InvalidOperationException(
                        $"Rare preview not resting in bottom slot: {top}.");
                for (var y = 0; y < 70; y++)
                    Buffer.BlockCopy(full, (top + y) * 283 * 4,
                        bytes, y * 283 * 4, 283 * 4);
                var glowing = 0;
                for (var y = 0; y < 70; y++)
                for (var x = 0; x < 68; x++)
                {
                    if (x is >= 10 and < 58 && y is >= 11 and < 59)
                        continue;
                    var p = (y * 283 + x) * 4;
                    // PixelFormats.Pbgra32 is BGRA *premultiplied*.
                    var blue = bytes[p];
                    var green = bytes[p + 1];
                    var red = bytes[p + 2];
                    var alpha = bytes[p + 3];
                    if (alpha > 16 && red > blue + 10 && green > blue + 3)
                        glowing++;
                }

                // Diagnostic separation: card not rendered at all versus
                // a truly invisible rare halo. These are NOT the same defect.
                var insideMaxAlpha = 0;
                var outsideMaxAlpha = 0;
                var outsideNonzero = 0;
                var outsideWarm = 0;
                var maxRedMinusBlue = int.MinValue;
                for (var y = 0; y < 70; y++)
                for (var x = 0; x < 68; x++)
                {
                    var p = (y * 283 + x) * 4;
                    var inside = x is >= 10 and < 58 && y is >= 11 and < 59;
                    var a = (int)bytes[p + 3];
                    if (inside) { insideMaxAlpha = Math.Max(insideMaxAlpha, a); continue; }
                    outsideMaxAlpha = Math.Max(outsideMaxAlpha, a);
                    if (a > 0) outsideNonzero++;
                    var delta = (int)bytes[p + 2] - bytes[p];
                    maxRedMinusBlue = Math.Max(maxRedMinusBlue, delta);
                    if (delta > 3) outsideWarm++;
                }
                var root = masked.Single(c => (string?)c.Tag == "SteamRareGlowRoot");
                var mask = masked.Single(c =>
                    (string?)c.Tag == "SteamRareGlowRotatingMask");
                Console.WriteLine(
                    $"RARE RASTER: card size {cardElement.ActualWidth}x{cardElement.ActualHeight}, " +
                    $"root={root.ActualWidth}x{root.ActualHeight}, " +
                    $"inner={mask.ActualWidth}x{mask.ActualHeight}, " +
                    $"root visible={root.IsVisible}, inner visible={mask.IsVisible}; " +
                    $"inside-max-alpha={insideMaxAlpha}, outside-max-alpha={outsideMaxAlpha}, " +
                    $"outside-nonzero={outsideNonzero}, " +
                    $"outside-warm={outsideWarm}, max(R-B)={maxRedMinusBlue}, glowing={glowing}");
                foreach (var (x, y) in new[]{(0,35),(5,35),(8,11),(59,30),(64,35),(30,5),(30,65),(30,30)})
                {
                    var p = (y * 283 + x) * 4;
                    Console.WriteLine($"RARE PIXEL ({x},{y}): " +
                        $"BGRA={bytes[p]},{bytes[p+1]},{bytes[p+2]},{bytes[p+3]}");
                }

                var bright = 0;
                for (var y = 0; y < 70; y++)
                for (var x = 0; x < 68; x++)
                {
                    if (x is >= 10 and < 58 && y is >= 11 and < 59)
                        continue;
                    var p = (y * 283 + x) * 4;
                    if (bytes[p + 2] >= 160 &&
                        bytes[p + 2] > bytes[p] + 10)
                        bright++;
                }
                Console.WriteLine($"CODEPEN COMPARISON: bright gold pixels = {bright}, " +
                    $"tinted halo pixels = {glowing}");
                // The masked 68px root ends at x=68. A CSS 16px blurred
                // shadow may leave a FAINT tail after that, but intense
                // rotating-gold rays cannot reach the title's x>=83 area.
                // This guards the original C06 WPF overflow bug seen in
                // actual exported bitmap evidence, not an arbitrary fit.
                var spilledRays = 0;
                for (var y = 0; y < 70; y++)
                for (var x = 83; x <= 110; x++)
                {
                    var p = (y * 283 + x) * 4;
                    var blue = bytes[p];
                    var green = bytes[p + 1];
                    var red = bytes[p + 2];
                    if (red >= 145 && red > green + 10 &&
                        green > blue + 10)
                        spilledRays++;
                }
                Console.WriteLine($"CODEPEN MASK BOX: spill pixels beyond x83={spilledRays}");
                if (spilledRays != 0)
                    throw new InvalidOperationException(
                        $"WPF root mask leaked {spilledRays} bright rays into text.");
                if (glowing < 16)
                    throw new InvalidOperationException(
                        $"Rare gold is not visible outside the icon ({glowing} luminous pixels).");
                return bytes;
            }

            void SaveRareFrame(byte[] bytes, string fileName)
            {
                var directory = Environment.GetEnvironmentVariable(
                    "RASC_SMOKE_FRAME_DIR");
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                var frame = BitmapSource.Create(283, 70, 96, 96,
                    PixelFormats.Pbgra32, null, bytes, 283 * 4);
                frame.Freeze();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(frame));
                using var file = File.Create(Path.Combine(directory, fileName));
                encoder.Save(file);
            }

            var clock = Stopwatch.StartNew();
            var stage = 0;
            byte[]? rareFirstFrame = null;
            var interval = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };

            void Fail(string reason)
            {
                failed = true;
                Console.Error.WriteLine("SMOKE FAILED: " + reason);
                interval.Stop();
                app.Shutdown();
            }

            interval.Tick += (_, _) =>
            {
                var elapsed = clock.Elapsed.TotalMilliseconds;
                try
                {
                    if (scenario is "classic-only" or "modern-idle-classic")
                    {
                        if (stage == 0)
                        {
                            // A second discriminating fixture: switching the
                            // selector without ever displaying Modern first.
                            if (scenario == "modern-idle-classic") Select("Modern");
                            Select("Classic");
                            Click(preview);
                            stage++;
                        }
                        else if (stage == 1 && elapsed > 850)
                        {
                            if (classicCount() == 0) { Fail("Classic never appeared."); return; }
                            classicObserved = true;
                            if (modernCount() != 0) { Fail("Unexpected Modern HWND."); return; }
                            stage++;
                        }
                        else if (stage == 2 && elapsed > 8050)
                        {
                            if (classicCount() != 0) { Fail("Classic did not exit cleanly."); return; }
                            interval.Stop();
                            app.Shutdown();
                        }
                    }
                    else if (scenario == "modern-rare")
                    {
                        if (stage == 0)
                        {
                            Select("Modern");
                            Click(rare);
                            stage++;
                        }
                        else if (stage == 1 && elapsed > 900)
                        {
                            if (modernWindowCount() != 1 || modernCount() != 1)
                            {
                                Fail("Rare notification did not appear.");
                                return;
                            }
                            rareFirstFrame = SnapshotRareCard();
                            SaveRareFrame(rareFirstFrame, "c06-rare-frame-0900ms.png");
                            modernObserved = true;
                            stage++;
                        }
                        else if (stage == 2 && elapsed > 2000)
                        {
                            var nextFrame = SnapshotRareCard();
                            SaveRareFrame(nextFrame, "c06-rare-frame-2000ms.png");
                            if (rareFirstFrame is null)
                                throw new InvalidOperationException(
                                    "Missing first rare-glow animation frame.");

                            // A static golden outline cannot satisfy this
                            // check: require actual moving halo pixels.
                            var changed = 0;
                            for (var y = 0; y < 70; y++)
                            for (var x = 0; x < 68; x++)
                            {
                                if (x is >= 10 and < 58 && y is >= 11 and < 59)
                                    continue;
                                var p = (y * 283 + x) * 4;
                                if (Math.Abs(nextFrame[p] - rareFirstFrame[p]) > 4 ||
                                    Math.Abs(nextFrame[p + 1] - rareFirstFrame[p + 1]) > 4 ||
                                    Math.Abs(nextFrame[p + 2] - rareFirstFrame[p + 2]) > 4)
                                    changed++;
                            }
                            if (changed < 12)
                            {
                                Fail($"Rare halo is static or invisible: only {changed} moving pixels.");
                                return;
                            }
                            stage++;
                        }
                        else if (stage == 3 && elapsed > 6550)
                        {
                            if (modernCount() != 0)
                            {
                                Fail("Rare notification did not exit cleanly.");
                                return;
                            }
                            interval.Stop();
                            app.Shutdown();
                        }
                    }
                    else if (scenario is "modern-progress" or "modern-stack-two")
                    {
                        if (stage == 0)
                        {
                            Select("Modern");
                            if (scenario == "modern-stack-two")
                            {
                                waveCount.SelectedItem = waveCount.Items.OfType<ComboBoxItem>()
                                    .Single(item => (string?)item.Tag == "2");
                                Click(wave);
                            }
                            else Click(progress);
                            stage++;
                        }
                        else if (stage == 1 && elapsed > 850)
                        {
                            VerifyOneSeamlessSurface(scenario == "modern-stack-two" ? 2 : 1);
                            modernObserved = true;
                            stage++;
                        }
                        else if (stage == 2 && elapsed > 6450)
                        {
                            if (modernCount() != 0) { Fail("Progress card remained after exit."); return; }
                            interval.Stop();
                            app.Shutdown();
                        }
                    }
                    else
                    {
                        if (stage == 0)
                        {
                            Select("Modern");
                            Click(wave); // 3 cards, including gold rare-glow.
                            stage++;
                        }
                        else if (stage == 1 && elapsed > 500)
                        {
                            VerifyOneSeamlessSurface(3);
                            modernObserved = true;
                            Select("Classic");
                            Click(preview);
                            stage++;
                        }
                        else if (stage == 2 && elapsed > 1100)
                        {
                            if (modernCount() != 3 || modernWindowCount() != 1 ||
                                classicCount() != 0)
                            {
                                Fail("Mixed Modern/Classic windows overlapped.");
                                return;
                            }
                            stage++;
                        }
                        else if (stage == 3 && elapsed > 6700)
                        {
                            if (modernCount() != 0 || classicCount() != 1)
                            {
                                Fail("Classic didn't begin after Modern exited.");
                                return;
                            }
                            classicObserved = true;
                            stage++;
                        }
                        else if (stage == 4 && elapsed > 14800)
                        {
                            if (modernCount() != 0 || classicCount() != 0)
                            {
                                Fail("Toast windows still active after expiry.");
                                return;
                            }
                            interval.Stop();
                            app.Shutdown();
                        }
                    }
                }
                catch (Exception error)
                {
                    fault = error;
                    Fail(error.ToString());
                }
            };
            interval.Start();
            app.Run();
            if (fault is not null) Console.Error.WriteLine(fault);
            // An idle Modern->Classic switch MUST NOT create any Modern
            // HWND. Require Modern observation only for scenarios that
            // actually request a Modern presentation.
            bool requireClassic =
                scenario is not ("modern-progress" or "modern-stack-two" or "modern-rare");
            bool requireModern =
                scenario is "modern-classic" or "modern-progress" or "modern-stack-two" or "modern-rare";
            if ((requireClassic && !classicObserved) ||
                (requireModern && !modernObserved))
            {
                failed = true;
                Console.Error.WriteLine("Required visual HWND observations not reached.");
            }
            Console.WriteLine(failed
                ? $"FAILED WPF scenario: {scenario}"
                : $"PASS WPF scenario: {scenario}; real HWNDs cycled without unhandled exceptions.");
            return failed ? 1 : 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
    }
}
