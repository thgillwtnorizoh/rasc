namespace Rasc.Core;

/// <summary>
/// Live GC monitor with NO persisted replay database. The first observed
/// journal or snapshot is a silent baseline. When a journal exists, ONLY new
/// native unlock records produce popup events. Every GC match_start marker
/// triggers one achievements.txt baseline refresh for current earned state.
/// The legacy achievements.txt delta monitor is used only while the journal
/// is absent (never for malformed/unavailable journal data).
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
    private bool _firstPass = true;
    private bool _fallbackActive;
    private bool _journalActive;
    private string? _lastMatchStartIdentity;

    public event EventHandler<UnlockEvent>? UnlockDetected;
    public event EventHandler<IReadOnlyList<UnlockEvent>>? UnlockBatchDetected;
    public event EventHandler<GcCurrentAchievementState>? CurrentStateChanged;
    public event EventHandler<string>? StatusChanged;
    /// <summary>Full exception for local diagnostics; status UI remains sanitized.</summary>
    public event EventHandler<Exception>? InternalError;

    public AchievementMonitor(GcFileSource source, UnlockEventStore store, TimeSpan? interval = null)
    {
        _source = source;
        _store = store;
        _interval = interval ?? TimeSpan.FromMilliseconds(250);
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
            _watcher.Filters.Clear();
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

    private void OnChanged(object sender, FileSystemEventArgs e) => Wake();
    private void OnRenamed(object sender, RenamedEventArgs e) => Wake();
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        StatusChanged?.Invoke(this, "File watcher error; periodic reconciliation continues.");
        Wake();
    }
    private void Wake()
    {
        try { _wake.Release(); }
        catch (SemaphoreFullException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var journal = await _source.ReadJournalAsync(ct);
                var now = DateTimeOffset.UtcNow;
                if (journal.Status == ReadStatus.Valid)
                {
                    var latestStart = journal.MatchStarts.LastOrDefault();
                    var markerIdentity = latestStart is null ? null
                        : latestStart.Session + ":" + latestStart.Sequence;
                    var newMatch = markerIdentity is not null &&
                        !string.Equals(_lastMatchStartIdentity, markerIdentity,
                            StringComparison.Ordinal);
                    var initialJournal = !_journalActive && _firstPass;
                    // Snapshot only at initial attach, at a new match marker,
                    // or when transitioning from fallback to journal.
                    if (initialJournal || newMatch || !_journalActive)
                    {
                        var snapshot = await _source.ReadAsync(ct);
                        if (snapshot.Status == ReadStatus.Valid && snapshot.Snapshot is not null)
                            _store.AcceptSnapshot(_source.SourceKey, snapshot.Snapshot,
                                now, recordNewUnlocks: false);
                        else
                            StatusChanged?.Invoke(this, $"Earned-state baseline {snapshot.Status}: {snapshot.Detail}");
                    }
                    // An already-populated journal at application start is
                    // history, not a batch of fresh toasts. No saved pending
                    // events can be recovered because there is no database.
                    _store.AcceptJournal(_source.SourceKey, journal.Unlocks, now,
                        baselineExisting: initialJournal);
                    _lastMatchStartIdentity = markerIdentity ?? _lastMatchStartIdentity;
                    _journalActive = true;
                    _fallbackActive = false;
                    _firstPass = false;
                    PublishStateAndEvents(ct);
                    StatusChanged?.Invoke(this,
                        $"Monitoring achievement_log — {journal.Unlocks.Count} recorded unlock(s).");
                }
                else if (journal.Status == ReadStatus.Missing)
                {
                    var snapshot = await _source.ReadAsync(ct);
                    if (snapshot.Status == ReadStatus.Valid && snapshot.Snapshot is not null)
                    {
                        // On entering fallback, silently establish its own
                        // earned baseline. Subsequent source deltas are live.
                        _store.AcceptSnapshot(_source.SourceKey, snapshot.Snapshot,
                            now, recordNewUnlocks: _fallbackActive);
                        _fallbackActive = true;
                        _journalActive = false;
                        _firstPass = false;
                        PublishStateAndEvents(ct);
                        StatusChanged?.Invoke(this,
                            $"Monitoring achievements.txt fallback — {snapshot.Snapshot.Achievements.Count} unlocked.");
                    }
                    else
                    {
                        _firstPass = false;
                        StatusChanged?.Invoke(this, $"{snapshot.Status}: {snapshot.Detail}");
                    }
                }
                else
                {
                    // Corrupt/unavailable journal never silently enables the
                    // snapshot fallback, which would fabricate historical toasts.
                    StatusChanged?.Invoke(this, $"Journal {journal.Status}: {journal.Detail}");
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                InternalError?.Invoke(this, ex);
                StatusChanged?.Invoke(this, $"Monitoring error: {ex.Message}");
            }
            try
            {
                await _wake.WaitAsync(_interval, ct);
                while (_wake.Wait(0)) { }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }

    private void PublishStateAndEvents(CancellationToken ct)
    {
        var committed = _store.GetCurrentState(_source.SourceKey);
        if (committed.HasValidSnapshot &&
            (_lastSignaledState is null || !SameAchievements(_lastSignaledState, committed)))
        {
            _lastSignaledState = committed;
            CurrentStateChanged?.Invoke(this, committed);
        }
        // Keep one ordered UI burst; do not alter the working popup scheduler.
        var pending = _store.GetPending(_source.SourceKey);
        if (!ct.IsCancellationRequested && pending.Count > 0)
            UnlockBatchDetected?.Invoke(this, pending);
        foreach (var evt in pending)
        {
            if (ct.IsCancellationRequested) break;
            UnlockDetected?.Invoke(this, evt);
            _store.MarkDelivered(evt.EventId, DateTimeOffset.UtcNow);
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
