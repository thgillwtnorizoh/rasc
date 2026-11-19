using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Nonmodal, explicit recovery choices. A missing install must not prevent
/// viewing offline previews or other working features. This dialog never
/// downloads, edits Steam, installs a GC or reads achievements on its own.
/// </summary>
internal sealed class InstallationRecoveryWindow : Window
{
    internal InstallationRecoveryWindow(
        Window owner, SteamInstallReport steam, GcInstallReport gc,
        Action chooseSteam, Action chooseGc, Action rescan)
    {
        Owner = owner;
        Title = "RASC — Installation checks";
        Width = 640;
        Height = 440;
        MinWidth = 510;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowActivated = false;
        ShowInTaskbar = false;
        var root = new StackPanel { Margin = new Thickness(22) };
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = root
        };
        Content = scroll;
        root.Children.Add(new TextBlock
        {
            Text = "Local files need attention",
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 9)
        });
        root.Children.Add(Text("RASC only reads local Steam/GT-610 files. It does not repair, download or replace installed files.", 0));

        if (!steam.Ready)
        {
            root.Children.Add(Heading("Steam installation"));
            var problem = !steam.Found
                ? "No Steam installation was found."
                : "Files missing from the selected Steam installation:\n" +
                  string.Join("\n", steam.Missing.Select(s => "• " + s)) +
                  "\n\nSelected folder: " + steam.Root;
            root.Children.Add(Text(problem, 4));
            root.Children.Add(Text("Fix: install/update or repair Steam using Valve's client/installer, then rescan. Or select the correct existing Steam folder.", 8));
            root.Children.Add(Buttons(
                ("Open Steam installer / repair", () => OpenUrl(InstallationLocator.SteamDownloadUrl)),
                ("Set Steam location…", () => { Close(); chooseSteam(); })));
        }

        if (gc.State != GcInstallState.Ready)
        {
            root.Children.Add(Heading("CS:GO Game Coordinator source"));
            var problem = gc.State switch
            {
                GcInstallState.MissingAchievements =>
                    "Found csgo_gc/ and csgo.exe together at:\n" + gc.GameRoot +
                    "\n\nMissing: csgo_gc/achievements.txt. Download GT-610 csgo-gc " +
                    "(or a compatible achievement-enabled version) if necessary. " +
                    "If the GC is already installed, launch CS:GO and play a game " +
                    "to try to generate achievements.txt.",
                GcInstallState.MissingLauncher =>
                    "The selected game folder is incomplete. Both csgo_gc/ and csgo.exe " +
                    "must be in the SAME folder.\n\nChecked: " + gc.GameRoot,
                _ =>
                    "No compatible Game Coordinator installation was found. " +
                    "Install GT-610 csgo-gc (or a compatible achievement-enabled version), " +
                    "or manually select your CS:GO game folder."
            };
            root.Children.Add(Text(problem, 4));
            root.Children.Add(Buttons(
                ("Open GT-610 downloads", () => OpenUrl(InstallationLocator.GcReleasesUrl)),
                ("Set CS:GO location…", () => { Close(); chooseGc(); })));
        }

        root.Children.Add(Buttons(
            ("Rescan locations", () => { Close(); rescan(); }),
            ("Not now", Close)));
    }

    private static TextBlock Heading(string content) => new()
    {
        Text = content,
        FontSize = 15,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 18, 0, 7)
    };

    private static TextBlock Text(string content, int top) => new()
    {
        Text = content,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, top, 0, 3)
    };

    private static WrapPanel Buttons(params (string label, Action action)[] items)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 8, 0, 2) };
        foreach (var (label, action) in items)
        {
            var button = new Button
            {
                Content = label,
                Margin = new Thickness(0, 0, 8, 4),
                Padding = new Thickness(10, 6, 10, 6)
            };
            button.Click += (_, _) => action();
            row.Children.Add(button);
        }
        return row;
    }

    private static void OpenUrl(string url)
    {
        // Two constant official URLs only, never user-provided executable
        // paths or a shell command. ShellExecute opens the default browser.
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
            or InvalidOperationException)
        { MessageBox.Show("Could not open the browser. URL: " + url, "RASC"); }
    }
}
