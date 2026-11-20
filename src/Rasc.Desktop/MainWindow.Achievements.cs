using System.IO;
using System.Windows;
using Rasc.Core;

namespace Rasc.Desktop;

public partial class MainWindow
{
    private CuratedAchievementCatalog? _liveDefinitions;
    private CsgoAchievementLocalization? _liveLocalization;
    private string? _liveGameRoot;
    private string _liveMetadataStatus = "Achievement metadata not loaded.";

    private void LoadLiveAchievementMetadata(string gcFilePath)
    {
        _liveDefinitions = null;
        _liveLocalization = null;
        _liveGameRoot = InstallationDiscovery.GameRootFromAchievementsPath(gcFilePath);
        try
        {
            _liveDefinitions = CuratedAchievementCatalog.LoadDefault();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            FormatException or InvalidOperationException)
        {
            _liveMetadataStatus = "Editable rarity database invalid: " + ex.Message;
            ShowLiveAchievementStatus();
            return; // no guessed rarity or catalog for live events
        }

        if (_liveGameRoot is null)
        {
            _liveMetadataStatus = "GC game directory missing: raw achievement IDs only.";
            ShowLiveAchievementStatus();
            return;
        }
        var localization = CsgoAchievementLocalization.LoadFromGameRoot(_liveGameRoot);
        _liveLocalization = localization.Catalog;
        _liveMetadataStatus = localization.Status == ReadStatus.Valid &&
            _liveLocalization is { Count: > 0 }
            ? $"Local CS:GO names READY: {_liveLocalization.Count} complete pairs."
            : $"Local achievement names NOT READY ({localization.Status}): " +
              (localization.Detail ?? "No usable achievement name/description pairs.");
        ShowLiveAchievementStatus();
    }

    private void ShowLiveAchievementStatus()
    {
        // Distinct from the rotating monitor status: parsing errors were previously
        // hidden in a tooltip overwritten by polling, while live toasts showed IDs.
        AchievementTextStatusText.Text = _liveMetadataStatus;
        AchievementTextStatusText.ToolTip = _liveGameRoot is null
            ? _liveMetadataStatus
            : Path.Combine(_liveGameRoot, "csgo", "resource", "csgo_english.txt") +
              Environment.NewLine + _liveMetadataStatus;
    }

    private ResolvedAchievementToast ResolveLiveAchievement(UnlockEvent item) =>
        AchievementToastResolver.Resolve(item.AchievementId,
            _liveDefinitions, _liveLocalization, _liveGameRoot);

    private AchievementLibraryWindow? _libraryWindow;
    private CuratedAchievementCatalog? _libraryDefinitions;
    private CsgoAchievementLocalization? _libraryLocalization;
    private string _libraryLocalizationStatus = "Local CS:GO text not loaded.";

    private void BrowseAchievements_Click(object sender, RoutedEventArgs e)
    {
        if (_libraryWindow is null)
        {
            _libraryWindow = new AchievementLibraryWindow(
                () => RefreshAchievementLibrary(reloadSources: true))
            {
                Owner = this
            };
            _libraryWindow.Closed += (_, _) => _libraryWindow = null;
            _libraryWindow.Show();
        }
        else
        {
            _libraryWindow.Activate();
        }
        RefreshAchievementLibrary(reloadSources: true);
    }

    /// <summary>
    /// Reuses the last valid catalog and localization on live GC changes;
    /// manual Reload re-reads the editable CSV and original owner's local text.
    /// The GC source is queried by its exact source key; never mix profiles.
    /// </summary>
    private void RefreshAchievementLibrary(bool reloadSources)
    {
        if (_libraryWindow is null) return;
        try
        {
            if (reloadSources || _libraryDefinitions is null)
            {
                var definitions = CuratedAchievementCatalog.LoadDefault();
                CsgoAchievementLocalization? localization = null;
                string textStatus;

                var root = InstallationDiscovery.GameRootFromAchievementsPath(PathBox.Text);
                if (root is null)
                {
                    textStatus = "Select a valid CS:GO GC path to resolve local names; raw IDs shown.";
                }
                else
                {
                    var read = CsgoAchievementLocalization.LoadFromGameRoot(root);
                    localization = read.Catalog;
                    textStatus = read.Status switch
                    {
                        ReadStatus.Valid =>
                            $"Local English names available: {localization!.Count} complete pairs.",
                        ReadStatus.Missing =>
                            "CS:GO English localization not found; raw IDs shown.",
                        ReadStatus.Invalid =>
                            "CS:GO localization invalid; raw IDs shown. " + read.Detail,
                        _ =>
                            "CS:GO localization unavailable; raw IDs shown. " + read.Detail
                    };
                }

                // Publish as a unit, never mix a new catalog with old text after error.
                _libraryDefinitions = definitions;
                _libraryLocalization = localization;
                _libraryLocalizationStatus = textStatus;
                // An explicit reload is also an owner edit of live metadata.
                // Already queued toasts retain the version captured at arrival.
                if (_monitor is not null && !string.IsNullOrWhiteSpace(PathBox.Text))
                    LoadLiveAchievementMetadata(PathBox.Text);
            }

            _store ??= new UnlockEventStore(Path.Combine(_localDirectory, "history.db"));
            var sourceKey = string.IsNullOrWhiteSpace(PathBox.Text)
                ? null : new GcFileSource(PathBox.Text).SourceKey;
            var state = sourceKey is null
                ? GcCurrentAchievementState.Unknown
                : _store.GetCurrentState(sourceKey);
            var result = AchievementLibraryProjection.Create(
                _libraryDefinitions!, _libraryLocalization, state);

            var status = _monitor is null
                ? "Not currently monitoring. Showing last committed GC snapshot when available."
                : StatusText.Text;
            _libraryWindow.SetSnapshot(result, _libraryLocalizationStatus, status);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            FormatException or ArgumentException or InvalidOperationException or
            Microsoft.Data.Sqlite.SqliteException)
        {
            _libraryDefinitions = null; // never silently accept invalid owner edits
            _libraryLocalization = null;
            _libraryWindow.ShowCatalogError(
                "Catalog unavailable: " + ex.Message);
        }
    }

    private void OnCommittedLibrarySnapshot(GcCurrentAchievementState state)
    {
        // The snapshot is committed before this callback. The persisted store remains
        // the source of truth for the selected GC path, including on monitor restart.
        if (state.HasValidSnapshot && _libraryWindow is not null)
            RefreshAchievementLibrary(reloadSources: false);
    }
}
