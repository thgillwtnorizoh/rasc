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
    private UnlockEventStore? _store;
    private AchievementMonitor? _monitor;
    private bool _canClose;
    private bool _closing;

    public MainWindow()
    {
        InitializeComponent();
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

    private sealed record UnlockRow(string AchievementId, string Observed);
}
