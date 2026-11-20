using System.Diagnostics;
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
                    if (ms > 13000)
                        throw new TimeoutException("Late-join WPF smoke never finished.");
                    switch (stage)
                    {
                        case 0 when ms >= 600:
                            AssertCards(1, ["Late Live 1"]);
                            if (cueCount != 1)
                                throw new InvalidOperationException("Initial cue not played once.");
                            stage = 1;
                            break;
                        case 1 when ms >= 1000:
                            host.Enqueue(E(2), Content(2)); // separate GC write
                            stage = 2;
                            break;
                        case 2 when ms >= 1450:
                            host.Enqueue(E(3), Content(3)); // separate GC write
                            stage = 3;
                            break;
                        case 3 when ms >= 1750:
                            host.Enqueue(E(4), Content(4)); // capacity 3, next wave
                            stage = 4;
                            break;
                        case 4 when ms >= 2100:
                            AssertCards(3,
                                ["Late Live 1", "Late Live 2", "Late Live 3"]);
                            if (cueCount != 1)
                                throw new InvalidOperationException(
                                    "Late live joins must not replay the original audio cue.");
                            stage = 5;
                            break;
                        case 5 when ms >= 8400:
                            AssertCards(1, ["Late Live 4"]);
                            if (cueCount != 2)
                                throw new InvalidOperationException(
                                    "Next wave must play exactly one fresh audio cue.");
                            Console.WriteLine(
                                "PASS P006 late WPF: 1+1+1 joins immediately, 4th FIFO wave; " +
                                "two-line/one-line reference layout and sound gate.");
                            stage = 6;
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
            if (failure is not null || stage != 6)
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
