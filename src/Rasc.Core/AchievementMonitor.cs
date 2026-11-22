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
    private GcCurrentAchievementState? _lastSignaledState;
    private bool _snapshotObservedThisRun;

    public event EventHandler<UnlockEvent>? UnlockDetected;
    /// <summary>One ordered, source-confirmed reconciliation burst; replaces per-award UI dispatch.</summary>
    public event EventHandler<IReadOnlyList<UnlockEvent>>? UnlockBatchDetected;
    public event EventHandler<GcCurrentAchievementState>? CurrentStateChanged;
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
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = false
            };
            // Avoid opening the snapshot repeatedly on unrelated temp-file
            // writes (including the GC writer's atomic replacement staging).
            _watcher.Filters.Add(Path.GetFileName(_source.FilePath));
            _watcher.Filters.Add(Path.GetFileName(_source.JournalPath));
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
                // Journal existence takes priority even if the file is empty.
                // Invalid/unavailable journal reads fail closed, NOT snapshot fallback.
                var journal = await _source.ReadJournalAsync(ct);
                var read = await _source.ReadAsync(ct);
                var usingJournal = journal.Status == ReadStatus.Valid;
                if (journal.Status is ReadStatus.Invalid or ReadStatus.Unavailable)
                {
                    StatusChanged?.Invoke(this, $"Journal {journal.Status}: {journal.Detail}");
                }
                else if (usingJournal || read.Status == ReadStatus.Valid)
                {
                    var now = DateTimeOffset.UtcNow;
                    if (read.Status == ReadStatus.Valid && read.Snapshot is not null)
                    {
                        // A journal can arrive before the first snapshot. Baseline
                        // that later snapshot silently instead of replaying old
                        // achievements when the journal disappears.
                        _store.AcceptSnapshot(_source.SourceKey, read.Snapshot, now,
                            recordNewUnlocks: !usingJournal && _snapshotObservedThisRun);
                        _snapshotObservedThisRun = true;
                    }
                    if (usingJournal)
                        _store.AcceptJournal(_source.SourceKey, journal.Unlocks, now);

                    // Publish the actual committed state after both sources. Live
                    // unlocks can precede achievements.txt persistence.
                    var committed = _store.GetCurrentState(_source.SourceKey);
                    if (committed.HasValidSnapshot &&
                        (_lastSignaledState is null || !SameAchievements(_lastSignaledState, committed)))
                    {
                        _lastSignaledState = committed;
                        CurrentStateChanged?.Invoke(this, committed);
                    }
                    // Preserve the original single ordered batch and at-least-once
                    // delivery contract for the UI, regardless of source choice.
                    var pending = _store.GetPending(_source.SourceKey);
                    if (!ct.IsCancellationRequested && pending.Count > 0)
                        UnlockBatchDetected?.Invoke(this, pending);
                    foreach (var evt in pending)
                    {
                        if (ct.IsCancellationRequested) break;
                        UnlockDetected?.Invoke(this, evt);
                        _store.MarkDelivered(evt.EventId, DateTimeOffset.UtcNow);
                    }
                    StatusChanged?.Invoke(this, usingJournal
                        ? $"Monitoring achievement_log — {journal.Unlocks.Count} event(s); " +
                          (read.Status == ReadStatus.Valid ? "GC snapshot available." : "awaiting GC snapshot.")
                        : $"Monitoring achievements.txt — {read.Snapshot!.Achievements.Count} unlocked.");
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

    private static bool SameAchievements(GcCurrentAchievementState left,
        GcCurrentAchievementState right) =>
        left.HasValidSnapshot == right.HasValidSnapshot &&
        left.Achievements.Count == right.Achievements.Count &&
        left.Achievements.All(pair =>
            right.Achievements.TryGetValue(pair.Key, out uint value) && value == pair.Value);

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
