using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
                if (host.Content is not Canvas canvas || canvas.Children.Count != count)
                    throw new InvalidOperationException("Modern cards are not on one canvas.");
                var top = canvas.Children.Cast<UIElement>()
                    .Select(Canvas.GetTop).OrderBy(y => y).ToArray();
                for (var i = 0; i < count; i++)
                {
                    var expected = ModernStackGeometry.ViewportHeight -
                        count * ModernToastScheduler.CardHeight +
                        i * ModernToastScheduler.CardHeight;
                    if (Math.Abs(top[i] - expected) > 0.0001)
                        throw new InvalidOperationException(
                            $"Seam in Modern canvas at card {i}: {top[i]} != {expected}");
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
