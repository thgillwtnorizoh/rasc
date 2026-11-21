using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Rasc.Core;
using Rasc.Desktop;

namespace Rasc.Desktop.Smoke;

/// <summary>
/// Source-independent actual WPF regression: live GC awards delivered in
/// different file reads must join a visible 1-2 card stack promptly. Also
/// verifies Argyle Sap reference 2-line description spacing versus a short
/// one-line toast. All award data is synthetic; no game/Steam modification.
/// </summary>
internal static class P006LateAdmissionSmoke
{
    [STAThread]
    internal static int Run()
    {
        Exception? failure = null;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        ModernToastHost? host = null;
        DispatcherTimer? ticker = null;
        int cueCount = 0;
        int stage = 0;
        app.DispatcherUnhandledException += (_, e) =>
        {
            failure = e.Exception;
            e.Handled = true; // test harness only
            app.Shutdown();
        };

        try
        {
            AssertDescriptionLayout();
            host = new ModernToastHost(app.Dispatcher);
            host.ConfigureCustomization(PopupFontChoice.Original,
                ModernTypographyProfile.RascAccepted, () => cueCount++);
            UnlockEvent E(int n) => new("late-live-" + n, "synthetic-source",
                "SYNTHETIC_" + n, (uint)(1790000200 + n), DateTimeOffset.UtcNow);
            ResolvedAchievementToast Content(int n) => new("SYNTHETIC_" + n,
                "Late Live " + n, "Synthetic condition " + n, false, null);

            host.Enqueue(E(1), Content(1));
            var watch = Stopwatch.StartNew();
            ticker = new DispatcherTimer(DispatcherPriority.Normal, app.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            ticker.Tick += (_, _) =>
            {
                try
                {
                    double ms = watch.Elapsed.TotalMilliseconds;
                    if (ms > 16500)
                        throw new TimeoutException("Immutable Modern waves did not drain.");
                    switch (stage)
                    {
                        case 0 when ms >= 1250:
                            AssertCards(1, ["Late Live 1"]);
                            if (cueCount != 1)
                                throw new InvalidOperationException("First wave should cue once.");
                            stage = 1;
                            break;
                        case 1 when ms >= 1450:
                            host.Enqueue(E(2), Content(2)); // NEW GC source write
                            AssertCards(1, ["Late Live 1"]); // no live join
                            stage = 2;
                            break;
                        case 2 when ms >= 1650:
                            host.Enqueue(E(3), Content(3)); // same 800ms second cohort
                            AssertCards(1, ["Late Live 1"]);
                            stage = 3;
                            break;
                        case 3 when ms >= 2500:
                            host.Enqueue(E(4), Content(4)); // separate third cohort
                            stage = 4;
                            break;
                        case 4 when ms >= 3000:
                            AssertCards(1, ["Late Live 1"]);
                            if (cueCount != 1)
                                throw new InvalidOperationException(
                                    "Later source writes cannot replay/alter active wave.");
                            // Suppress ordinary WPF animation and hold wake sources.
                            // The independent watchdog must still drain sealed
                            // FIFO waves without any more GC award signals.
                            DetachWpfWakeCallbacks(host);
                            stage = 5;
                            break;
                        case 5 when ms >= 7000:
                            AssertCards(2, ["Late Live 2", "Late Live 3"]);
                            if (cueCount != 2)
                                throw new InvalidOperationException(
                                    "Second sealed cohort did not get its own cue.");
                            stage = 6;
                            break;
                        case 6 when ms >= 12500:
                            AssertCards(1, ["Late Live 4"]);
                            if (cueCount != 3)
                                throw new InvalidOperationException(
                                    "Third cohort did not appear without new event.");
                            Console.WriteLine(
                                "PASS P006 immutable WPF waves: first 800ms cohort stays fixed, " +
                                "later 2-card cohort then 1-card cohort, all cues, full durations; " +
                                "Argyle two-line and short-line layout.");
                            stage = 7;
                            ticker.Stop();
                            app.Shutdown();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    failure = ex;
                    ticker.Stop();
                    app.Shutdown();
                }
            };
            ticker.Start();
            app.Run();
            if (failure is not null || stage != 7)
            {
                Console.Error.WriteLine(failure?.ToString() ??
                    "Late-join WPF phases incomplete.");
                return 1;
            }
            return 0;

            void AssertCards(int expected, string[] names)
            {
                var window = app.Windows.OfType<ModernToastWindow>()
                    .SingleOrDefault(w => w.IsVisible)
                    ?? throw new InvalidOperationException("No visible Modern WPF host.");
                if (!Equals(window.Tag, expected))
                    throw new InvalidOperationException(
                        $"Expected {expected} live cards; actual {window.Tag}.");
                var canvas = (Canvas)window.Content;
                var cards = canvas.Children.OfType<ModernToastCard>()
                    .OrderBy(Canvas.GetTop).ToArray();
                if (cards.Length != expected)
                    throw new InvalidOperationException("Wrong Modern card count.");
                for (int i = 0; i < expected; i++)
                {
                    var face = (Canvas)cards[i].Content;
                    if (!face.Children.OfType<TextBlock>()
                        .Any(t => t.Text == names[i]))
                        throw new InvalidOperationException("Wrong late-join FIFO title: " + names[i]);
                }
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            ticker?.Stop();
            host?.Dispose();
        }
    }

    private static void DetachWpfWakeCallbacks(ModernToastHost host)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(ModernToastHost);
        foreach (string prefix in new[] { "Hold", "Anchor" })
        {
            string field = prefix == "Hold" ? "_holdWake" : "_anchorWake";
            var timer = (DispatcherTimer)(type.GetField(field, flags)?.GetValue(host)
                ?? throw new InvalidOperationException(field + " missing."));
            var callback = type.GetMethod("On" + prefix + "Wake", flags)
                ?? throw new InvalidOperationException("On" + prefix + "Wake missing.");
            var handler = (EventHandler)Delegate.CreateDelegate(
                typeof(EventHandler), host, callback);
            timer.Tick -= handler;
            timer.Stop();
        }
        var stop = type.GetMethod("StopRendering", flags)
            ?? throw new InvalidOperationException("StopRendering missing.");
        stop.Invoke(host, null);
    }

    private static void AssertDescriptionLayout()
    {
        static (ModernToastCard Card, TextBlock Name, TextBlock Desc) Create(string desc)
        {
            var card = new ModernToastCard(
                ModernToastContent.Unlock("The Argyle Sap", desc),
                PopupFontChoice.Original, ModernTypographyProfile.RascAccepted);
            var text = ((Canvas)card.Content).Children.OfType<TextBlock>().ToArray();
            return (card, text.Single(x => x.Text == "The Argyle Sap"),
                text.Single(x => x.Text == desc));
        }

        var shortText = Create("Kill an enemy.");
        if (Canvas.GetTop(shortText.Name) != 18 ||
            Canvas.GetTop(shortText.Desc) != 36 ||
            shortText.Desc.Height != 18)
            throw new InvalidOperationException("Short description reference positions changed.");

        var longText = Create(
            "Blow up an Engineer, his sentry gun, and his dispenser with a single blast.");
        if (Canvas.GetTop(longText.Name) != 10 ||
            Canvas.GetTop(longText.Desc) != 26 ||
            longText.Desc.Height != 32 ||
            longText.Desc.TextWrapping != TextWrapping.Wrap ||
            longText.Desc.TextTrimming != TextTrimming.CharacterEllipsis)
            throw new InvalidOperationException("Two-line Argyle Sap reference layout not applied.");

        longText.Card.ApplyFontChoice(PopupFontChoice.SansSerif);
        if (Canvas.GetTop(longText.Desc) != 26 || longText.Desc.Height != 32)
            throw new InvalidOperationException("Two-line layout lost after changing font.");
    }
}
