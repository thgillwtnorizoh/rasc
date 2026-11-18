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
        if (args.Length != 1 || args[0] is not ("classic-only" or "modern-classic" or "modern-progress" or "modern-idle-classic" or "modern-stack-two"))
        {
            Console.Error.WriteLine("Usage: Rasc.Desktop.Smoke classic-only|modern-classic|modern-progress|modern-idle-classic|modern-stack-two");
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
                        var sampleX = Math.Max(1, (int)Math.Round(2 * dpiFactor));
                        for (var y = boundary - 2; y <= boundary + 2; y++)
                        {
                            var a = (y * pxWidth + sampleX) * 4;
                            var b = ((y - 1) * pxWidth + sampleX) * 4;
                            if (pixels[a + 3] < 250 || pixels[b + 3] < 250)
                                throw new InvalidOperationException(
                                    $"Transparent gap at {count} cards, {dpiFactor}x, y={y}");
                            for (var channel = 0; channel < 3; channel++)
                                if (Math.Abs(pixels[a + channel] - pixels[b + channel]) > 8)
                                    throw new InvalidOperationException(
                                        $"Visible gradient seam at {count} cards, {dpiFactor}x, y={y}");
                        }
                    }
                }
            }

            var clock = Stopwatch.StartNew();
            var stage = 0;
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
                scenario is not ("modern-progress" or "modern-stack-two");
            bool requireModern =
                scenario is "modern-classic" or "modern-progress" or "modern-stack-two";
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
