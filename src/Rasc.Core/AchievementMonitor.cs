namespace Rasc.Core;

/// <summary>
/// FileSystemWatcher is a low-latency signal only; a bounded periodic pass recovers missed
/// changes and atomic replacements. Invalid and missing source reads NEVER mutate persisted state.
/// Events are replayed until the consumer successfully accepts them (at-least-once dispatch).
/// </summary>
public sealed class AchievementMonitor : IAsyncDisposable
{
    private readonly GcFileSource _source;
    private readonly UnlockEventStore _store;
    private readonly TimeSpan _interval;
    private readonly SemaphoreSlim _wake = new(0, 1);
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _cancel;
    private Task? _runner;

    public event EventHandler<UnlockEvent>? UnlockDetected;
    public event EventHandler<string>? StatusChanged;

    public AchievementMonitor(GcFileSource source, UnlockEventStore store, TimeSpan? interval = null)
    {
        _source = source;
        _store = store;
        _interval = interval ?? TimeSpan.FromSeconds(1);
        if (_interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
    }

    public Task StartAsync()
    {
        if (_runner is not null) throw new InvalidOperationException("Monitor already started.");
        _cancel = new CancellationTokenSource();
        var directory = Path.GetDirectoryName(_source.FilePath)!;
        if (Directory.Exists(directory))
        {
            _watcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(_source.FilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false
            };
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnWatcherError;
            _watcher.EnableRaisingEvents = true;
        }
        _runner = Task.Run(() => RunAsync(_cancel.Token));
        return Task.CompletedTask;
    }

    private void OnChanged(object sender, FileSystemEventArgs args) => Wake();

    private void OnRenamed(object sender, RenamedEventArgs args) => Wake();

    private void OnWatcherError(object sender, ErrorEventArgs args)
    {
        StatusChanged?.Invoke(this, "File watcher error; periodic reconciliation continues.");
        Wake();
    }

    private void Wake()
    {
        try { _wake.Release(); }
        catch (SemaphoreFullException) { /* coalesced: one outstanding wake is sufficient */ }
        catch (ObjectDisposedException) { /* shutting down */ }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var read = await _source.ReadAsync(ct);
                if (read.Status == ReadStatus.Valid && read.Snapshot is not null)
                {
                    _store.AcceptSnapshot(_source.SourceKey, read.Snapshot, DateTimeOffset.UtcNow);
                    foreach (var evt in _store.GetPending(_source.SourceKey))
                    {
                        if (ct.IsCancellationRequested) break;
                        UnlockDetected?.Invoke(this, evt);
                        _store.MarkDelivered(evt.EventId, DateTimeOffset.UtcNow);
                    }
                    StatusChanged?.Invoke(this, $"Monitoring — {read.Snapshot.Achievements.Count} unlocked in current file.");
                }
                else StatusChanged?.Invoke(this, $"{read.Status}: {read.Detail}");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Monitoring error: {ex.Message}");
            }
            try
            {
                await _wake.WaitAsync(_interval, ct);
                while (_wake.Wait(0)) { /* collapse burst */ }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _watcher?.Dispose();
        _watcher = null;
        _cancel?.Cancel();
        if (_runner is not null)
        {
            try { await _runner; }
            catch (OperationCanceledException) { }
        }
        _cancel?.Dispose();
        _wake.Dispose();
    }
}
