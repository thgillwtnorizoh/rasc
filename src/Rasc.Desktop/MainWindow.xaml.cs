using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using Microsoft.Win32;
using Rasc.Core;

namespace Rasc.Desktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<UnlockRow> _history = [];
    private readonly string _localDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RASC");
    private readonly ClassicToastHost _classicToasts;
    private readonly ModernToastHost _modernToasts;
    private readonly NotificationProfileStore _profileStore;
    private NotificationProfile _profile;
    private bool _profileControlsReady;
    private readonly ClassicPresentationSettingsStore _presentationStore;
    private ClassicPresentationSettings _presentationSettings;
    private bool _presentationControlsReady;
    private UnlockEventStore? _store;
    private AchievementMonitor? _monitor;
    private bool _canClose;
    private bool _closing;

    public MainWindow()
    {
        InitializeComponent();
        _presentationStore = new ClassicPresentationSettingsStore(
            Path.Combine(_localDirectory, "appearance.json"));
        _presentationSettings = _presentationStore.Load();
        _classicToasts = new ClassicToastHost(Dispatcher);
        _modernToasts = new ModernToastHost(Dispatcher);
        _classicToasts.SetSettings(_presentationSettings);
        _modernToasts.SetSettings(_presentationSettings);
        _profileStore = new NotificationProfileStore(
            Path.Combine(_localDirectory, "notification-profile.txt"));
        _profile = _profileStore.Load();
        foreach (var candidate in ProfileBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(candidate.Tag?.ToString(), _profile.ToString(), StringComparison.Ordinal))
            {
                ProfileBox.SelectedItem = candidate;
                break;
            }
        }
        _profileControlsReady = true;
        SelectAppearancePreference(AnimationFpsBox, _presentationSettings.AnimationFps);
        SelectAppearancePreference(PopupSizeBox, _presentationSettings.PopupSizePercent);
        _presentationControlsReady = true;
        RecentList.ItemsSource = _history;
        PathBox.Text = LoadLastPath();
        Closing += OnClosing;
        Closed += (_, _) => ShutdownTrace.Mark("Window.Closed");
    }

    private string LoadLastPath()
    {
        try
        {
            var config = Path.Combine(_localDirectory, "last-source.txt");
            return File.Exists(config) ? File.ReadAllText(config).Trim() : "";
        }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "GT-610 achievement statistics (achievements.txt)|achievements.txt|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = "achievements.txt",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true) PathBox.Text = dialog.FileName;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var source = new GcFileSource(PathBox.Text);
            _store ??= new UnlockEventStore(Path.Combine(_localDirectory, "history.db"));
            _history.Clear();
            foreach (var item in _store.GetRecent(source.SourceKey))
                _history.Add(new UnlockRow(item.AchievementId, item.ObservedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")));

            Directory.CreateDirectory(_localDirectory);
            File.WriteAllText(Path.Combine(_localDirectory, "last-source.txt"), source.FilePath);
            _monitor = new AchievementMonitor(source, _store);
            _monitor.UnlockDetected += (_, item) => Dispatcher.Invoke(() =>
            {
                _history.Insert(0, new UnlockRow(item.AchievementId,
                    item.ObservedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")));
                while (_history.Count > 300) _history.RemoveAt(_history.Count - 1);
                // Rehydrated historical rows do NOT enter the presentation queue.
                // This callback runs only for newly observed/pending delivery events.
                switch (_profile)
                {
                    case NotificationProfile.Classic: _classicToasts.Enqueue(item); break;
                    case NotificationProfile.Modern: _modernToasts.Enqueue(item); break;
                    case NotificationProfile.Off: break;
                }
            });
            _monitor.StatusChanged += (_, message) => Dispatcher.BeginInvoke(() => StatusText.Text = message);
            await _monitor.StartAsync();
            StartButton.IsEnabled = false;
            PathBox.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusText.Text = "Reading source; first-time history will be baselined.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not start: " + ex.Message;
            if (_monitor is not null) { await _monitor.DisposeAsync(); _monitor = null; }
        }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        if (_monitor is not null)
        {
            await _monitor.DisposeAsync();
            _monitor = null;
        }
        StartButton.IsEnabled = true;
        PathBox.IsEnabled = true;
        StatusText.Text = "Monitoring stopped.";
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_canClose)
        {
            ShutdownTrace.Mark("Window.Closing (final)");
            return;
        }

        e.Cancel = true;
        if (_closing) return; // coalesce repeated close clicks while shutdown is already running
        _closing = true;
        ShutdownTrace.Begin();
        ShutdownTrace.Mark("Window.Closing (initial)");

        try
        {
            if (_monitor is not null)
            {
                ShutdownTrace.Mark("Monitor.DisposeAsync begin");
                await _monitor.DisposeAsync();
                _monitor = null;
                ShutdownTrace.Mark("Monitor.DisposeAsync complete");
            }
            else ShutdownTrace.Mark("Monitor already stopped");
        }
        catch (Exception ex)
        {
            ShutdownTrace.Mark("Monitor stop error: " + ex.GetType().Name);
        }
        finally
        {
            try
            {
                ShutdownTrace.Mark("ClassicToastHost.Dispose begin");
                _classicToasts.Dispose();
                ShutdownTrace.Mark("ClassicToastHost.Dispose complete");
            }
            catch (Exception ex)
            {
                ShutdownTrace.Mark("ClassicToastHost dispose error: " + ex.GetType().Name);
            }

            try
            {
                ShutdownTrace.Mark("ModernToastHost.Dispose begin");
                _modernToasts.Dispose();
                ShutdownTrace.Mark("ModernToastHost.Dispose complete");
            }
            catch (Exception ex)
            {
                ShutdownTrace.Mark("ModernToastHost dispose error: " + ex.GetType().Name);
            }

            try
            {
                ShutdownTrace.Mark("SQLite.Dispose begin");
                _store?.Dispose();
                _store = null;
                ShutdownTrace.Mark("SQLite.Dispose complete");
            }
            catch (Exception ex)
            {
                ShutdownTrace.Mark("SQLite dispose error: " + ex.GetType().Name);
            }

            _canClose = true;
            ShutdownTrace.Mark("Final close scheduled");
            // Avoid re-entering Window.Close during the initial Closing callback.
            _ = Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.Normal);
        }
    }

    private static void SelectAppearancePreference(
        System.Windows.Controls.ComboBox box, int value)
    {
        foreach (var item in box.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (int.TryParse(item.Tag?.ToString(), out var parsed) && parsed == value)
            {
                box.SelectedItem = item;
                return;
            }
        }
        box.SelectedIndex = -1;
        if (box.IsEditable) box.Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        else box.SelectedIndex = 0;
    }

    private void AnimationFpsBox_LostKeyboardFocus(object sender,
        System.Windows.Input.KeyboardFocusChangedEventArgs e) => CommitTypedAnimationFps();

    private void AnimationFpsBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Return)
        {
            CommitTypedAnimationFps();
            e.Handled = true;
        }
    }

    private void CommitTypedAnimationFps()
    {
        if (!_presentationControlsReady) return;
        var entry = AnimationFpsBox.Text.Trim();
        var selected = AnimationFpsBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (selected?.Content?.ToString() == entry) return; // a selected preset
        if (entry.Equals("Display-synced", StringComparison.OrdinalIgnoreCase))
        {
            UpdatePresentationSettings(_presentationSettings with { AnimationFps = 0 });
            SelectAppearancePreference(AnimationFpsBox, 0);
            return;
        }
        if (int.TryParse(entry, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var fps) &&
            ClassicPresentationSettings.IsValidFps(fps))
        {
            UpdatePresentationSettings(_presentationSettings with { AnimationFps = fps });
            SelectAppearancePreference(AnimationFpsBox, fps);
            return;
        }
        SelectAppearancePreference(AnimationFpsBox, _presentationSettings.AnimationFps);
        StatusText.Text = "Enter Display-synced or a whole FPS number from 15 to 240.";
    }

    private void AnimationFpsBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_presentationControlsReady) return;
        var item = AnimationFpsBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (!int.TryParse(item?.Tag?.ToString(), out var selected)) return;
        UpdatePresentationSettings(_presentationSettings with { AnimationFps = selected });
    }

    private void PopupSizeBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_presentationControlsReady) return;
        var item = PopupSizeBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (!int.TryParse(item?.Tag?.ToString(), out var selected)) return;
        UpdatePresentationSettings(_presentationSettings with { PopupSizePercent = selected });
    }

    private void UpdatePresentationSettings(ClassicPresentationSettings requested)
    {
        var valid = requested.Validated();
        if (valid == _presentationSettings) return;
        _presentationSettings = valid;
        _classicToasts.SetSettings(valid);
        _modernToasts.SetSettings(valid);
        if (!_presentationStore.TrySave(valid))
            StatusText.Text = "Appearance updated for this session; failed to save settings.";
    }

    private void ProfileBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_profileControlsReady) return;
        var selected = (ProfileBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();
        if (!Enum.TryParse<NotificationProfile>(selected, out var profile) ||
            !Enum.IsDefined(profile)) return;
        if (_profile == profile) return;
        _profile = profile;
        if (!_profileStore.TrySave(profile))
            StatusText.Text = "Popup profile changed this session, but could not be saved.";
    }

    private void PreviewClassic_Click(object sender, RoutedEventArgs e)
    {
        switch (_profile)
        {
            case NotificationProfile.Classic: _classicToasts.PreviewUnlock(); break;
            case NotificationProfile.Modern: _modernToasts.PreviewUnlock(); break;
            case NotificationProfile.Off: StatusText.Text = "Popups are disabled."; break;
        }
    }

    private void PreviewProgress_Click(object sender, RoutedEventArgs e)
    {
        // Display-only fixture; no fabricated GC callbacks or SQLite updates.
        switch (_profile)
        {
            case NotificationProfile.Classic: _classicToasts.PreviewProgress(); break;
            case NotificationProfile.Modern: _modernToasts.PreviewProgress(); break;
            case NotificationProfile.Off: StatusText.Text = "Popups are disabled."; break;
        }
    }

    private sealed record UnlockRow(string AchievementId, string Observed);
}
