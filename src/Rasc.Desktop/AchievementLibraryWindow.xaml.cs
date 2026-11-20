using System.Windows;
using System.Windows.Controls;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Read-only, virtualized achievement viewer. Every row is from the curated
/// definitions + last valid GC snapshot; it never writes Steam, GC, or SQLite.
/// </summary>
public partial class AchievementLibraryWindow : Window
{
    private readonly Action _reload;
    private IReadOnlyList<AchievementLibraryDisplayRow> _rows = [];

    public AchievementLibraryWindow(Action reload)
    {
        ArgumentNullException.ThrowIfNull(reload);
        _reload = reload;
        InitializeComponent();
    }

    public void SetSnapshot(AchievementLibrarySnapshot snapshot, string localizationStatus,
        string monitorStatus)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _rows = snapshot.Entries.Select(entry =>
        {
            var state = entry.State switch
            {
                AchievementEarnedState.Earned => "Earned",
                AchievementEarnedState.Locked => "Locked",
                _ => "Unknown"
            };
            return new AchievementLibraryDisplayRow(entry.ApiId,
                entry.DisplayName, entry.Description ?? "Description unavailable",
                state, entry.Rarity?.ToString() ?? "Unclassified",
                entry.State == AchievementEarnedState.Earned
                    ? entry.KnownUnlockTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                      ?? "Time unavailable"
                    : "—");
        }).ToArray();

        SummaryText.Text = snapshot.HasValidSnapshot
            ? $"Earned {snapshot.EarnedCatalogCount} / {snapshot.CatalogCount} · " +
              $"Locked {snapshot.LockedCatalogCount}" +
              (snapshot.ExtraSourceIdCount > 0
                  ? $" · {snapshot.ExtraSourceIdCount} unrecognized GC award(s)"
                  : "")
            : $"{snapshot.CatalogCount} achievements · GC state UNKNOWN until a valid snapshot";
        TextSourceText.Text = localizationStatus;
        SourceStatusText.Text = monitorStatus;
        ApplyFilter();
    }

    public void ShowCatalogError(string error)
    {
        SummaryText.Text = "Achievement definitions could not be loaded.";
        TextSourceText.Text = error;
        SourceStatusText.Text = "Correct the editable definitions file and select Reload files.";
        _rows = [];
        ApplyFilter();
    }

    public void SetSourceStatus(string status) => SourceStatusText.Text = status;

    private void Reload_Click(object sender, RoutedEventArgs e) => _reload();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void StateFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ApplyFilter();

    private void ApplyFilter()
    {
        if (CatalogGrid is null || SearchBox is null || StateFilterBox is null) return;
        string query = SearchBox.Text.Trim();
        string selected = (StateFilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        CatalogGrid.ItemsSource = _rows.Where(row =>
            (selected == "All" || row.StateLabel == selected) &&
            (query.Length == 0 ||
             row.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.ApiId.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    internal sealed record AchievementLibraryDisplayRow(
        string ApiId, string Name, string Description, string StateLabel,
        string RarityLabel, string UnlockedLabel);
}
