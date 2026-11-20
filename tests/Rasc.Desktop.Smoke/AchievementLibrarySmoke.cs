using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Rasc.Core;
using Rasc.Desktop;

namespace Rasc.Desktop.Smoke;

/// <summary>
/// Actual Windows WPF tree smoke for the 167-entry earned-state library. This
/// uses synthetic GC award snapshots and synthetic localized text, never a
/// player's Steam/game data or a real in-game award.
/// </summary>
internal static class AchievementLibrarySmoke
{
    [STAThread]
    public static int Run()
    {
        int invokedReload = 0;
        Exception? failure = null;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.DispatcherUnhandledException += (_, e) =>
        {
            failure = e.Exception;
            e.Handled = true; // TEST HARNESS ONLY; production never swallows crashes
            app.Shutdown();
        };

        try
        {
            var catalog = CuratedAchievementCatalog.LoadDefault();
            const string syntheticText = """
                "lang" { "Tokens" {
                    "UNSTOPPABLE_FORCE_NAME" "Synthetic Four-Kill"
                    "UNSTOPPABLE_FORCE_DESC" "Synthetic gameplay description."
                }}
                """;
            var language = CsgoAchievementLocalization.Parse(syntheticText);
            var gc = new GcCurrentAchievementState(true,
                new Dictionary<string, uint>(StringComparer.Ordinal)
                {
                    ["UNSTOPPABLE_FORCE"] = 1790000000,
                    ["KILL_WITH_OWN_GUN"] = 0,
                    ["NOT_IN_CURATED_CATALOG"] = 1790000010
                });
            var projection = AchievementLibraryProjection.Create(catalog, language, gc);
            var window = new AchievementLibraryWindow(() => invokedReload++);
            app.MainWindow = window;
            window.SetSnapshot(projection, "Synthetic local fixture", "Monitoring synthetic GC.");
            window.Show();
            var timer = new DispatcherTimer(DispatcherPriority.ContextIdle, app.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                try
                {
                    var grid = (DataGrid?)window.FindName("CatalogGrid")
                        ?? throw new InvalidOperationException("Achievement DataGrid missing.");
                    var search = (TextBox?)window.FindName("SearchBox")
                        ?? throw new InvalidOperationException("Achievement search missing.");
                    var filter = (ComboBox?)window.FindName("StateFilterBox")
                        ?? throw new InvalidOperationException("Earned-state filter missing.");
                    var summary = (TextBlock?)window.FindName("SummaryText")
                        ?? throw new InvalidOperationException("Achievement count missing.");

                    if (grid.Items.Count != 168 || !summary.Text.Contains("Earned 2 / 167",
                            StringComparison.Ordinal))
                        throw new InvalidOperationException("Incorrect full catalog earned count.");
                    if (grid.Items.Cast<object>().Count(x =>
                            x.ToString()?.Contains("Synthetic Four-Kill",
                                StringComparison.Ordinal) == true) != 1)
                        throw new InvalidOperationException("Synthetic local name not bound.");

                    search.Text = "UNSTOPPABLE_FORCE";
                    if (grid.Items.Count != 1)
                        throw new InvalidOperationException("Achievement ID search did not filter.");
                    search.Clear();
                    filter.SelectedIndex = 1; // Earned
                    if (grid.Items.Count != 3)
                        throw new InvalidOperationException("Earned filter must include unknown-ID GC award.");
                    filter.SelectedIndex = 2; // Locked
                    if (grid.Items.Count != 165)
                        throw new InvalidOperationException("Locked filter did not preserve valid baseline.");

                    // Source loss never fabricates a reset: explicitly display the
                    // previous committed state and a truthful source warning.
                    window.SetSourceStatus("Missing: showing last valid GC snapshot.");
                    var status = (TextBlock?)window.FindName("SourceStatusText");
                    if (status?.Text.StartsWith("Missing:", StringComparison.Ordinal) != true)
                        throw new InvalidOperationException("Missing-source warning not displayed.");
                    var reload = FindButton(window, "Reload files");
                    reload.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    if (invokedReload != 1)
                        throw new InvalidOperationException("Reload callback did not fire exactly once.");

                    Console.WriteLine("PASS P006 real WPF library smoke: rows, search, earned/locked and stale status.");
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    app.Shutdown();
                }
            };
            timer.Start();
            app.Run();
            if (failure is not null)
            {
                Console.Error.WriteLine(failure);
                return 1;
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static Button FindButton(AchievementLibraryWindow window, string label)
    {
        // Source-owned WPF visual tree search; no production test hooks required.
        var root = (Grid)window.Content;
        var panel = root.Children.OfType<DockPanel>().Single();
        return panel.Children.OfType<Button>().Single(button =>
            string.Equals(button.Content?.ToString(), label, StringComparison.Ordinal));
    }
}
