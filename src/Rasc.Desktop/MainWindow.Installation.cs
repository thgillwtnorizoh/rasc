using System.IO;
using System.Windows;
using Microsoft.Win32;
using Rasc.Core;

namespace Rasc.Desktop;

public partial class MainWindow
{
    private readonly InstallationLocationStore _locationStore;
    private InstallationLocations _locations;
    private SteamInstallReport _steam = new(null, false, []);
    private GcInstallReport _gc = new(null, GcInstallState.NoInstallation, false);
    private InstallationRecoveryWindow? _installationRecovery;

    private void InitializeInstallationDiscovery()
    {
        Loaded += (_, _) => RefreshInstallations(showRecovery: true);
    }

    private void SetManualGcRoot(string path)
    {
        _locations = _locations with { GcGameRootOverride = path };
        SaveLocationOverride();
        RefreshInstallations(showRecovery: true);
    }

    private void ChooseGcLocation_Click(object sender, RoutedEventArgs e) => ChooseGcLocation();

    private void ChooseGcLocation()
    {
        if (_monitor is not null)
        {
            StatusText.Text = "Stop monitoring before changing Game Coordinator source.";
            return;
        }
        var dialog = new OpenFolderDialog
        {
            Title = "Select the CS:GO game folder containing both csgo.exe and csgo_gc/",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        var path = dialog.FolderName;
        if (Path.GetFileName(path).Equals("csgo_gc", StringComparison.OrdinalIgnoreCase))
            path = Path.GetDirectoryName(path) ?? path;
        SetManualGcRoot(path);
    }

    private void ChooseSteamLocation_Click(object sender, RoutedEventArgs e) => ChooseSteamLocation();

    private void ChooseSteamLocation()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Steam installation root (steam.exe and steamui/)",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        _locations = _locations with { SteamRootOverride = dialog.FolderName };
        SaveLocationOverride();
        RefreshInstallations(showRecovery: true);
    }

    private void RescanInstallations_Click(object sender, RoutedEventArgs e) =>
        RefreshInstallations(showRecovery: true);

    private void ResetManualLocations_Click(object sender, RoutedEventArgs e)
    {
        if (_monitor is not null)
        {
            StatusText.Text = "Stop monitoring before switching GC source.";
            return;
        }
        _locations = new InstallationLocations();
        SaveLocationOverride();
        RefreshInstallations(showRecovery: true);
    }

    private void SaveLocationOverride()
    {
        if (!_locationStore.TrySave(_locations))
            StatusText.Text = "Location chosen, but preference saving failed.";
    }

    private void RefreshInstallations(bool showRecovery)
    {
        _steam = InstallationLocator.FindSteam(_locations.SteamRootOverride);
        _gc = InstallationLocator.FindGc(_steam, _locations.GcGameRootOverride, LoadLastPath());

        SteamPathText.Text = !_steam.Found
            ? "Not found"
            : _steam.Ready ? _steam.Root! : "Missing files: " + _steam.Root;
        GcStatusText.Text = _gc.State switch
        {
            GcInstallState.Ready => "Ready: " + _gc.GameRoot,
            GcInstallState.MissingAchievements => "Missing achievements.txt",
            GcInstallState.MissingLauncher => "Incomplete game folder",
            _ => "Not found"
        };
        SteamPathText.ToolTip = _steam.Root ?? "No Steam installation detected.";
        GcStatusText.ToolTip = _gc.GameRoot ?? "No compatible CS:GO GC source detected.";

        // Source monitor owns its original watcher and source-specific ledger;
        // do not hot-swap its location while it is running.
        if (_monitor is null)
            PathBox.Text = _gc.AchievementsPath ?? "";
        UpdateSoundAvailability();
        if (showRecovery && (!_steam.Ready || _gc.State != GcInstallState.Ready))
        {
            _installationRecovery?.Close();
            var dlg = new InstallationRecoveryWindow(
                this, _steam, _gc, ChooseSteamLocation, ChooseGcLocation,
                () => RefreshInstallations(showRecovery: true));
            _installationRecovery = dlg;
            dlg.Closed += (_, _) =>
            {
                if (ReferenceEquals(_installationRecovery, dlg))
                    _installationRecovery = null;
            };
            dlg.Show(); // nonmodal: previews still work if GC is not installed
        }
        else if (showRecovery)
        {
            _installationRecovery?.Close();
            _installationRecovery = null;
            StatusText.Text = "Steam and GT-610 files found. Read-only monitoring is available.";
        }
    }

    private bool CheckGcPathBeforeMonitoring()
    {
        var root = InstallationDiscovery.GameRootFromAchievementsPath(PathBox.Text);
        var current = root is null
            ? new GcInstallReport(null, GcInstallState.NoInstallation, false)
            : InstallationDiscovery.ExamineGc(root);
        if (current.State == GcInstallState.Ready &&
            string.Equals(current.AchievementsPath, PathBox.Text.Trim(),
                StringComparison.OrdinalIgnoreCase))
            return true;

        _gc = current;
        StatusText.Text = current.State switch
        {
            GcInstallState.MissingAchievements =>
                "achievements.txt missing. Install achievement-capable GT-610 or play a game to generate it.",
            GcInstallState.MissingLauncher =>
                "csgo.exe and csgo_gc/ must exist in the same game folder.",
            _ => "No valid GC source selected. Choose the CS:GO game folder."
        };
        _installationRecovery?.Close();
        var dlg = new InstallationRecoveryWindow(this, _steam, current,
            ChooseSteamLocation, ChooseGcLocation,
            () => RefreshInstallations(showRecovery: true));
        _installationRecovery = dlg;
        dlg.Closed += (_, _) =>
        {
            if (ReferenceEquals(_installationRecovery, dlg))
                _installationRecovery = null;
        };
        dlg.Show();
        return false;
    }
}
