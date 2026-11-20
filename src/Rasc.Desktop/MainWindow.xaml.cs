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
    private readonly NotificationPresentationGate _presentationGate;
    private readonly DispatcherTimer _presentationWake;
    private readonly NotificationProfileStore _profileStore;
    private NotificationProfile _profile;
    private bool _profileControlsReady;
    private readonly ClassicPresentationSettingsStore _presentationStore;
    private ClassicPresentationSettings _presentationSettings;
    private bool _presentationControlsReady;
    private readonly NotificationCustomizationStore _customizationStore;
    private readonly NotificationSoundPlayback _soundPlayback = new();
    private NotificationCustomizationSettings _customization;
    private bool _customizationControlsReady;
    private UnlockEventStore? _store;
    private AchievementMonitor? _monitor;
    private bool _canClose;
    private bool _closing;

    public MainWindow()
    {
        InitializeComponent();
        _locationStore = new InstallationLocationStore(
            Path.Combine(_localDirectory, "installation-locations.json"));
        _locations = _locationStore.Load();
        InitializeInstallationDiscovery();
        _presentationStore = new ClassicPresentationSettingsStore(
            Path.Combine(_localDirectory, "appearance.json"));
        _presentationSettings = _presentationStore.Load();
        _customizationStore = new NotificationCustomizationStore(
            Path.Combine(_localDirectory, "notification-customization.json"));
        _customization = _customizationStore.Load();
        _classicToasts = new ClassicToastHost(Dispatcher);
        _modernToasts = new ModernToastHost(Dispatcher);
        _presentationGate = new NotificationPresentationGate(profile => profile switch
        {
            NotificationProfile.Classic => _classicToasts.IsIdle,
            NotificationProfile.Modern => _modernToasts.IsIdle,
            _ => true
        });
        _presentationWake = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(75)
        };
        _presentationWake.Tick += (_, _) =>
        {
            _presentationGate.Pump();
            if (_presentationGate.PendingCount == 0)
                _presentationWake.Stop();
        };
        _classicToasts.SetSettings(_presentationSettings);
        _modernToasts.SetSettings(_presentationSettings);
        _classicToasts.ConfigureCustomization(_customization.Font, PlayNotificationCue);
        _modernToasts.ConfigureCustomization(
            _customization.Font, _customization.ModernTypography, PlayNotificationCue);
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
        SelectNamedChoice(FontChoiceBox, _customization.Font.ToString());
        SelectNamedChoice(ModernTypographyBox, _customization.ModernTypography.ToString());
        SelectNamedChoice(SoundChoiceBox, _customization.Sound.ToString());
        _customizationControlsReady = true;
        UpdateSoundAvailability();
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
        if (dialog.ShowDialog(this) != true) return;
        if (_monitor is not null)
        {
            StatusText.Text = "Stop monitoring before changing the source.";
            return;
        }
        var root = InstallationDiscovery.GameRootFromAchievementsPath(dialog.FileName);
        if (root is null)
        {
            StatusText.Text = "The source must be csgo_gc/achievements.txt next to csgo.exe.";
            return;
        }
        SetManualGcRoot(root);
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!CheckGcPathBeforeMonitoring()) return;
            var source = new GcFileSource(PathBox.Text);
            LoadLiveAchievementMetadata(source.FilePath);
            _store ??= new UnlockEventStore(Path.Combine(_localDirectory, "history.db"));
            _history.Clear();
            foreach (var item in _store.GetRecent(source.SourceKey))
                _history.Add(new UnlockRow(item.AchievementId, item.ObservedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")));

            Directory.CreateDirectory(_localDirectory);
            File.WriteAllText(Path.Combine(_localDirectory, "last-source.txt"), source.FilePath);
            _monitor = new AchievementMonitor(source, _store);
            _monitor.CurrentStateChanged += (_, state) =>
                Dispatcher.BeginInvoke(new Action(() => OnCommittedLibrarySnapshot(state)));
            _monitor.UnlockDetected += (_, item) => Dispatcher.Invoke(() =>
            {
                _history.Insert(0, new UnlockRow(item.AchievementId,
                    item.ObservedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")));
                while (_history.Count > 300) _history.RemoveAt(_history.Count - 1);
                // Rehydrated historical rows do NOT enter the presentation queue.
                // This callback runs only for newly observed/pending delivery events.
                var destination = _profile;
                var content = ResolveLiveAchievement(item);
                if (destination == NotificationProfile.Classic)
                    QueuePresentation(destination, () => _classicToasts.Enqueue(item, content));
                else if (destination == NotificationProfile.Modern)
                    QueuePresentation(destination, () => _modernToasts.Enqueue(item, content));
            });
            _monitor.StatusChanged += (_, message) => Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusText.Text = message;
                _libraryWindow?.SetSourceStatus(message);
            }));
            await _monitor.StartAsync();
            StartButton.IsEnabled = false;
            PathBox.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusText.Text = "Reading source; first-time history will be baselined.";
            StatusText.ToolTip = _liveMetadataStatus;
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
        _libraryWindow?.SetSourceStatus(
            "Monitoring stopped. Showing last successfully committed GC state (if any).");
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
            _presentationWake.Stop();
            _presentationGate.Clear();
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

            _soundPlayback.Dispose();

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

    private static void SelectNamedChoice(
        System.Windows.Controls.ComboBox box, string name)
    {
        foreach (var item in box.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), name, StringComparison.Ordinal))
            {
                box.SelectedItem = item;
                return;
            }
        }
        box.SelectedIndex = 0;
    }

    private void FontChoiceBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_customizationControlsReady) return;
        var tag = (FontChoiceBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();
        if (!Enum.TryParse<PopupFontChoice>(tag, out var choice) || !Enum.IsDefined(choice))
            return;
        UpdateCustomization(_customization with { Font = choice });
        StatusText.Text = PopupFontResolver.IsInstalled(choice)
            ? "Popup font selected: " + PopupFontResolver.UserLabel(choice)
            : "Font not installed on Windows: " + PopupFontResolver.UserLabel(choice)
              + " (a fallback will render; not a valid typography comparison).";
    }

    private void ModernTypographyBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_customizationControlsReady) return;
        var tag = (ModernTypographyBox.SelectedItem as System.Windows.Controls.ComboBoxItem)
            ?.Tag?.ToString();
        if (!Enum.TryParse<ModernTypographyProfile>(tag, out var choice) ||
            !Enum.IsDefined(choice)) return;
        UpdateCustomization(_customization with { ModernTypography = choice });
        StatusText.Text = choice switch
        {
            ModernTypographyProfile.RascAccepted =>
                "Restored accepted RASC Modern typography.",
            ModernTypographyProfile.OwnerObservedMixed =>
                PopupFontResolver.IsInstalled(PopupFontChoice.MotivaSans)
                ? "Experimental mixed face: Motiva Sans Bold name and Windows UI font description. Not original-verified."
                : "Motiva Sans is NOT installed: mixed face would fall back. Comparison is invalid.",
            _ => "Modern source CSS typography selected (" + choice
                + "). Compare Motiva Sans versus Windows sans-serif; Steam runtime parity unverified."
        };
    }

    private void SoundChoiceBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_customizationControlsReady) return;
        var tag = (SoundChoiceBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();
        if (!Enum.TryParse<NotificationSoundChoice>(tag, out var choice) ||
            !Enum.IsDefined(choice)) return;
        UpdateCustomization(_customization with { Sound = choice });
    }

    private void UpdateCustomization(NotificationCustomizationSettings settings)
    {
        var valid = settings.Validated();
        if (valid == _customization) return;
        _customization = valid;
        _classicToasts.ConfigureCustomization(valid.Font, PlayNotificationCue);
        _modernToasts.ConfigureCustomization(
            valid.Font, valid.ModernTypography, PlayNotificationCue);
        if (!_customizationStore.TrySave(valid))
            StatusText.Text = "Popup customizations changed for this session but could not be saved.";
        UpdateSoundAvailability();
    }

    private void UpdateSoundAvailability()
    {
        var choice = _customization.Sound;
        if (choice == NotificationSoundChoice.Off)
        {
            SoundAvailabilityText.Text = "Sound: silent";
            return;
        }
        var path = NotificationSoundFiles.Resolve(_customization,
            _localDirectory, AppContext.BaseDirectory, _steam.Root);
        SoundAvailabilityText.Text = path is not null
            ? _steam.Root is not null && path.Equals(
                Path.Combine(_steam.Root, "steamui", "sounds",
                    NotificationSoundFiles.ExpectedFile(choice) ?? ""),
                StringComparison.OrdinalIgnoreCase)
                ? "Sound: ready (Steam)" : "Sound: ready (local)"
            : choice == NotificationSoundChoice.Custom
                ? "Custom file unavailable"
                : "Preset WAV not installed";
    }

    private void PlayNotificationCue()
    {
        // Invoked by the active host ONLY on actual card/wave entrance;
        // settings changes, holds and animation frames don't retrigger sound.
        if (_customization.Sound == NotificationSoundChoice.Off) return;
        var path = NotificationSoundFiles.Resolve(_customization,
            _localDirectory, AppContext.BaseDirectory, _steam.Root);
        if (path is null)
        {
            UpdateSoundAvailability();
            return;
        }
        if (!_soundPlayback.Play(path))
            StatusText.Text = "Notification sound could not play; check the selected audio codec.";
    }

    private void ImportPresetSound_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Steam PCM audio (*.wav)|*.wav",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select one of the two supplied Steam notification WAV files"
        };
        if (dialog.ShowDialog(this) != true) return;
        var choice = NotificationSoundFiles.IsApprovedPresetFile(
            dialog.FileName, NotificationSoundChoice.DesktopDefault)
            ? NotificationSoundChoice.DesktopDefault
            : NotificationSoundChoice.BigPicture;
        if (!NotificationSoundFiles.TryImportPreset(
            dialog.FileName, _localDirectory, choice, out var message))
        {
            StatusText.Text = message;
            return;
        }
        UpdateCustomization(_customization with { Sound = choice });
        SelectNamedChoice(SoundChoiceBox, choice.ToString());
        UpdateSoundAvailability();
        StatusText.Text = message;
    }

    private void ChooseCustomSound_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Supported audio (*.wav;*.mp3;*.wma)|*.wav;*.mp3;*.wma",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Choose a notification sound (up to 16 MB)"
        };
        if (dialog.ShowDialog(this) != true) return;
        if (!NotificationSoundFiles.IsValidCustomFile(dialog.FileName))
        {
            StatusText.Text = "Custom audio must be a readable WAV, MP3 or WMA file up to 16 MB.";
            return;
        }
        UpdateCustomization(_customization with
        {
            Sound = NotificationSoundChoice.Custom,
            CustomPath = dialog.FileName
        });
        SelectNamedChoice(SoundChoiceBox, NotificationSoundChoice.Custom.ToString());
        UpdateSoundAvailability();
        StatusText.Text = "Custom audio selected: " + Path.GetFileName(dialog.FileName);
    }

    private void TestNotificationSound_Click(object sender, RoutedEventArgs e)
    {
        if (_customization.Sound == NotificationSoundChoice.Off)
        {
            StatusText.Text = "Sound is disabled.";
            return;
        }
        var path = NotificationSoundFiles.Resolve(_customization,
            _localDirectory, AppContext.BaseDirectory, _steam.Root);
        if (path is null)
        {
            StatusText.Text = _customization.Sound == NotificationSoundChoice.Custom
                ? "Choose a readable custom WAV/MP3/WMA first."
                : "Import one of the supplied Steam WAVs first, or extract the private sound pack beside the app.";
            return;
        }
        StatusText.Text = _soundPlayback.Play(path)
            ? "Playing selected sound: " + Path.GetFileName(path)
            : "Could not play selected sound.";
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

    private void QueuePresentation(NotificationProfile profile, Action show)
    {
        _presentationGate.Enqueue(profile, show);
        if (_presentationGate.PendingCount > 0)
        {
            _presentationWake.Start();
            StatusText.Text = "Preview/event queued until the previous notification style finishes.";
        }
        else _presentationWake.Stop();
    }

    private void PreviewClassic_Click(object sender, RoutedEventArgs e)
    {
        switch (_profile)
        {
            case NotificationProfile.Classic:
                QueuePresentation(NotificationProfile.Classic, _classicToasts.PreviewUnlock);
                break;
            case NotificationProfile.Modern:
                QueuePresentation(NotificationProfile.Modern, _modernToasts.PreviewUnlock);
                break;
            case NotificationProfile.Off:
                StatusText.Text = "Popups are disabled.";
                break;
        }
    }

    private void PreviewModernWave_Click(object sender, RoutedEventArgs e)
    {
        if (_profile != NotificationProfile.Modern)
        {
            StatusText.Text = "Select Modern to preview waves.";
            return;
        }
        var item = PreviewWaveCountBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        if (int.TryParse(item?.Tag?.ToString(), out var count))
            QueuePresentation(NotificationProfile.Modern, () => _modernToasts.PreviewWave(count));
    }

    private void PreviewRare_Click(object sender, RoutedEventArgs e)
    {
        if (_profile == NotificationProfile.Modern)
            QueuePresentation(NotificationProfile.Modern, _modernToasts.PreviewRare);
        else StatusText.Text = "Rare preview requires the Modern profile.";
    }

    private void PreviewProgress_Click(object sender, RoutedEventArgs e)
    {
        // Display-only fixture; no fabricated GC callbacks or SQLite updates.
        switch (_profile)
        {
            case NotificationProfile.Classic:
                QueuePresentation(NotificationProfile.Classic, _classicToasts.PreviewProgress);
                break;
            case NotificationProfile.Modern:
                QueuePresentation(NotificationProfile.Modern, _modernToasts.PreviewProgress);
                break;
            case NotificationProfile.Off: StatusText.Text = "Popups are disabled."; break;
        }
    }

    private sealed record UnlockRow(string AchievementId, string Observed);
}
