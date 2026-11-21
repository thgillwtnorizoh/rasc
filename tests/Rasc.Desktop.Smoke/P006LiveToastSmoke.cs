using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Rasc.Core;
using Rasc.Desktop;

namespace Rasc.Desktop.Smoke;

/// <summary>
/// Real WPF regression of 3+2 queued live-format notifications without a new
/// event to wake the second wave. Every award/artwork string is synthetic.
/// </summary>
internal static class P006LiveToastSmoke
{
    [STAThread]
    internal static int Run()
    {
        Exception? failure = null;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        ModernToastHost? host = null;
        DispatcherTimer? ticker = null;
        DispatcherTimer? freeze = null;
        app.DispatcherUnhandledException += (_, e) =>
        {
            failure = e.Exception;
            e.Handled = true; // TEST ONLY; production WPF exceptions still crash
            app.Shutdown();
        };

        try
        {
            byte[] art = SyntheticJpeg();
            if (AchievementToastArtwork.Decode(art) is null)
                throw new InvalidOperationException("Synthetic JPEG decoder failed.");
            var classic = new ClassicToastCard("Synthetic Name", "Synthetic description.", art);
            if (classic.FindName("AchievementArtwork") is not Image { Source: not null })
                throw new InvalidOperationException("Classic real-JPEG artwork was not applied.");

            // Regression for owner's screenshot: the live Classic path had
            // mistakenly replaced "Achievement Unlocked!" with the raw API ID
            // and put the long goal description where the name belongs.
            using (var classicHost = new ClassicToastHost(app.Dispatcher))
            {
                var classicEvent = new UnlockEvent("classic-c03-live", "synthetic",
                    "KILL_ENEMY_TEAM", 1790000000, DateTimeOffset.UtcNow);
                classicHost.Enqueue(classicEvent, new ResolvedAchievementToast(
                    "KILL_ENEMY_TEAM", "Synthetic Cleaner",
                    "Synthetic five-kill goal", true, art));
                var cardsField = typeof(ClassicToastHost).GetField(
                    "_cards", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Classic card registry missing.");
                var cards = (Dictionary<string, ClassicToastCard>)cardsField.GetValue(classicHost)!;
                var liveCard = cards.Values.Single();
                if ((liveCard.FindName("Headline") as TextBlock)?.Text != "Achievement Unlocked!" ||
                    (liveCard.FindName("Description") as TextBlock)?.Text != "Synthetic Cleaner")
                    throw new InvalidOperationException("Classic live text mapping is reversed.");
            }

            host = new ModernToastHost(app.Dispatcher);
            var clock = Stopwatch.StartNew();
            for (int i = 1; i <= 5; i++)
            {
                var evt = new UnlockEvent("p006-live-" + i, "synthetic-source",
                    "P006_SYNTHETIC_" + i, (uint)(1790000000 + i), DateTimeOffset.UtcNow);
                var content = new ResolvedAchievementToast(evt.AchievementId,
                    "Synthetic Name " + i, "Synthetic description " + i + ".",
                    i == 1, art);
                host.Enqueue(evt, content);
            }

            int stage = 0;
            ticker = new DispatcherTimer(DispatcherPriority.Normal, app.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            // Remove optional WPF Rendering and anchor callbacks throughout the
            // test; independent deadline timer must still advance all queued waves.
            var stopRender = typeof(ModernToastHost).GetMethod(
                "StopRendering", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Modern renderer stop hook unavailable");
            var anchorField = typeof(ModernToastHost).GetField(
                "_anchorWake", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Modern anchor timer unavailable");
            freeze = new DispatcherTimer(DispatcherPriority.Normal, app.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            freeze.Tick += (_, _) =>
            {
                stopRender.Invoke(host, null);
                ((DispatcherTimer)anchorField.GetValue(host)!).Stop();
            };
            freeze.Start();

            ticker.Tick += (_, _) =>
            {
                try
                {
                    var elapsed = clock.Elapsed.TotalMilliseconds;
                    if (elapsed > 13500)
                        throw new TimeoutException("Three-plus-two wave sequence stalled.");
                    if (stage == 0 && elapsed > 1550)
                    {
                        AssertWave(3, [1, 2, 3], imageRequired: true);
                        stage = 1;
                    }
                    if (stage == 1 && elapsed > 7250)
                    {
                        AssertWave(2, [4, 5], imageRequired: true);
                        stage = 2;
                        Console.WriteLine("PASS P006 sealed Modern waves: 800ms source collection, 3 then 2 FIFO, full presentation clock, text/art/rare, no sixth unlock.");
                        ticker.Stop();
                        freeze.Stop();
                        host.Dispose();
                        app.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    failure = ex;
                    ticker.Stop();
                    freeze.Stop();
                    host?.Dispose();
                    app.Shutdown();
                }
            };
            ticker.Start();
            app.Run();
            return failure is null && stage == 2 ? 0 : ReportFailure();
        }
        catch (Exception ex)
        {
            failure = ex;
            return ReportFailure();
        }
        finally
        {
            ticker?.Stop();
            freeze?.Stop();
            host?.Dispose();
        }

        int ReportFailure()
        {
            Console.Error.WriteLine(failure?.ToString() ?? "Live toast wave did not complete.");
            return 1;
        }

        void AssertWave(int expected, int[] ids, bool imageRequired)
        {
            var window = app.Windows.OfType<ModernToastWindow>()
                .SingleOrDefault(w => w.IsVisible)
                ?? throw new InvalidOperationException("No visible Modern host during queue.");
            if (window.Tag is not int tag || tag != expected)
                throw new InvalidOperationException("Wrong active Modern card count.");
            var view = (Canvas)window.Content;
            var cards = view.Children.OfType<ModernToastCard>()
                .OrderBy(Canvas.GetTop).ToArray();
            if (cards.Length != expected)
                throw new InvalidOperationException("Wave card count mismatch.");
            for (int i = 0; i < cards.Length; i++)
            {
                var face = (Canvas)cards[i].Content;
                string title = "Synthetic Name " + ids[i];
                if (!face.Children.OfType<TextBlock>().Any(t => t.Text == title))
                    throw new InvalidOperationException("FIFO name missing: " + title);
                if (!face.Children.OfType<TextBlock>().Any(t =>
                    t.Text == "Synthetic description " + ids[i] + "."))
                    throw new InvalidOperationException("Description missing: " + title);
                var wrapper = face.Children.OfType<Canvas>().Single(c =>
                    c.Tag is string tag && tag.EndsWith("IconWrapper", StringComparison.Ordinal));
                if (ids[i] == 1 && !Equals(wrapper.Tag, "LegendaryIconWrapper"))
                    throw new InvalidOperationException("Approved Rare variant was not activated.");
                var icon = wrapper.Children.OfType<Border>()
                    .Single(b => Equals(b.Tag, "AchievementIconFace"));
                if (imageRequired && icon.Child is not Image { Source: not null })
                    throw new InvalidOperationException("No real JPEG artwork for " + title);
            }
        }
    }

    private static byte[] SyntheticJpeg()
    {
        byte[] pixels = new byte[64 * 64 * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 60; pixels[i + 1] = 100;
            pixels[i + 2] = 240; pixels[i + 3] = 255;
        }
        var image = BitmapSource.Create(64, 64, 96, 96,
            PixelFormats.Bgra32, null, pixels, 64 * 4);
        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var output = new MemoryStream();
        encoder.Save(output);
        return output.ToArray();
    }
}
