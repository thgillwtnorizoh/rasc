using Rasc.Core;
using System.Diagnostics;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class MonitorTests
{
    private static string Snapshot(params (string id, uint time)[] achievements) =>
        "\"\"".Replace("\"\"", "") + "\"version\" \"1\"\n\"achievements\"\n{\n" +
        string.Join("\n", achievements.Select(x => $"\"{x.id}\" \"{x.time}\"")) + "\n}\n";

    [Fact]
    public async Task InvalidAndMissingFileNeverProduceFalseUnlocks()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-monitor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var source = new GcFileSource(Path.Combine(folder, "achievements.txt"));
        var first = await source.ReadAsync();
        Assert.Equal(ReadStatus.Missing, first.Status);
        await File.WriteAllTextAsync(source.FilePath, "\"version\" \"1\" \"achievements\" {");
        var invalid = await source.ReadAsync();
        Assert.Equal(ReadStatus.Invalid, invalid.Status);
        Assert.Null(invalid.Snapshot);
        await File.WriteAllTextAsync(source.FilePath, Snapshot(("A", 4)));
        var valid = await source.ReadAsync();
        Assert.Equal(ReadStatus.Valid, valid.Status);
        Assert.Single(valid.Snapshot!.Achievements);
    }

    [Fact]
    public async Task AtomicRewriteProducesOneUnlockAndDoesNotAlterGcFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-monitor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "achievements.txt");
        var initial = Snapshot(("A", 1));
        await File.WriteAllTextAsync(path, initial);
        using var store = new UnlockEventStore();
        await using var monitor = new AchievementMonitor(new GcFileSource(path), store,
            TimeSpan.FromMilliseconds(75));
        var baselined = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unlocked = new TaskCompletionSource<UnlockEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.StatusChanged += (_, status) =>
        {
            if (status.Contains("1 unlocked", StringComparison.Ordinal)) baselined.TrySetResult(true);
        };
        monitor.UnlockDetected += (_, evt) => unlocked.TrySetResult(evt);
        await monitor.StartAsync();
        await baselined.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var changed = Snapshot(("A", 1), ("B", 2));
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, changed);
        // The Windows scanner may briefly hold either file during a source
        // replacement. Retry within a bounded 500 ms window; a lasting
        // sharing violation still fails this original atomic-replace test.
        Exception? replacementFailure = null;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                File.Move(temp, path, overwrite: true);
                replacementFailure = null;
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                replacementFailure = ex;
                await Task.Delay(25);
            }
        }
        Assert.Null(replacementFailure);
        var evt = await unlocked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("B", evt.AchievementId);
        Assert.Equal(changed, await File.ReadAllTextAsync(path));
        Assert.Single(store.GetRecent(new GcFileSource(path).SourceKey));
    }
    [Fact]
    public async Task CurrentStateEventRequiresCommittedValidChangeAndNeverResetsOnCorruption()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-current-event-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "achievements.txt");
        await File.WriteAllTextAsync(path, Snapshot(("UNSTOPPABLE_FORCE", 0)));
        var source = new GcFileSource(path);
        using var store = new UnlockEventStore();
        await using var monitor = new AchievementMonitor(
            source, store, TimeSpan.FromMilliseconds(60));

        var first = new TaskCompletionSource<GcCurrentAchievementState>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<GcCurrentAchievementState>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var invalid = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int events = 0;
        monitor.CurrentStateChanged += (_, state) =>
        {
            if (Interlocked.Increment(ref events) == 1)
                first.TrySetResult(state);
            else second.TrySetResult(state);
        };
        monitor.StatusChanged += (_, status) =>
        {
            if (status.StartsWith("Invalid:", StringComparison.Ordinal))
                invalid.TrySetResult(true);
        };
        await monitor.StartAsync();
        var baseline = await first.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(baseline.HasValidSnapshot);
        Assert.Equal((uint)0, baseline.Achievements["UNSTOPPABLE_FORCE"]);
        Assert.Empty(store.GetRecent(source.SourceKey));

        await Task.Delay(250);
        Assert.Equal(1, Volatile.Read(ref events)); // no redundant callbacks each poll

        await File.WriteAllTextAsync(path, "\"version\" \"1\" \"achievements\" {");
        await invalid.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal((uint)0,
            store.GetCurrentState(source.SourceKey).Achievements["UNSTOPPABLE_FORCE"]);
        Assert.Equal(1, Volatile.Read(ref events));

        await File.WriteAllTextAsync(path,
            Snapshot(("UNSTOPPABLE_FORCE", 0), ("IMMOVABLE_OBJECT", 1790000300)));
        var changed = await second.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, changed.Achievements.Count);
        Assert.Equal((uint)1790000300, changed.Achievements["IMMOVABLE_OBJECT"]);
        Assert.Equal(2, Volatile.Read(ref events));
        Assert.Equal("IMMOVABLE_OBJECT",
            Assert.Single(store.GetRecent(source.SourceKey)).AchievementId);
    }

    [Fact]
    public async Task FourGcAwardsInOneSnapshotBecomeOneOrderedUiBatch()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-burst-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "achievements.txt");
        await File.WriteAllTextAsync(path, Snapshot(("BASELINE", 1)));
        using var store = new UnlockEventStore();
        var source = new GcFileSource(path);
        await using var monitor = new AchievementMonitor(
            source, store, TimeSpan.FromMilliseconds(60));
        var ready = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var delivered = new TaskCompletionSource<IReadOnlyList<UnlockEvent>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int batchCount = 0, singleCount = 0;
        monitor.UnlockBatchDetected += (_, batch) =>
        {
            Interlocked.Increment(ref batchCount);
            delivered.TrySetResult(batch.ToArray());
        };
        monitor.UnlockDetected += (_, _) => Interlocked.Increment(ref singleCount);
        monitor.StatusChanged += (_, status) =>
        {
            if (status.Contains("1 unlocked", StringComparison.Ordinal))
                ready.TrySetResult(true);
            if (status.Contains("5 unlocked", StringComparison.Ordinal))
                completed.TrySetResult(true);
        };
        await monitor.StartAsync();
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(store.GetRecent(source.SourceKey)); // silent first snapshot

        var change = Snapshot(("BASELINE", 1),
            ("FOURTH", 44), ("THIRD", 43), ("FIRST", 41), ("SECOND", 42));
        await File.WriteAllTextAsync(path, change);
        var batch = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(new[] { "FIRST", "SECOND", "THIRD", "FOURTH" },
            batch.Select(x => x.AchievementId));
        Assert.Equal(1, Volatile.Read(ref batchCount));
        Assert.Equal(4, Volatile.Read(ref singleCount)); // legacy callbacks preserved
        Assert.Equal(4, store.GetRecent(source.SourceKey).Count);
        Assert.Empty(store.GetPending(source.SourceKey));
        await Task.Delay(200);
        Assert.Equal(1, Volatile.Read(ref batchCount)); // no replay on later poll
    }

    [Fact]
    public async Task StoppingMonitorDoesNotWaitForLongPollingInterval()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-stop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "achievements.txt");
        await File.WriteAllTextAsync(path, Snapshot(("BASELINE", 42)));
        using var store = new UnlockEventStore();
        var source = new GcFileSource(path);
        var monitor = new AchievementMonitor(source, store, TimeSpan.FromMinutes(5));
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.StatusChanged += (_, status) =>
        {
            if (status.Contains("Monitoring", StringComparison.Ordinal))
                started.TrySetResult(true);
        };
        await monitor.StartAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var stopwatch = Stopwatch.StartNew();
        await monitor.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(4));
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Monitor disposal took {stopwatch.Elapsed} rather than canceling its waiting poll.");
    }

    [Fact]
    public async Task PrefersLiveJournalAndFallsBackOnlyWhenFileDisappears()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-journal-monitor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var source = new GcFileSource(Path.Combine(folder, "achievements.txt"));
        await File.WriteAllTextAsync(source.FilePath, Snapshot(("ALREADY", 1)));
        await File.WriteAllTextAsync(source.JournalPath, "");
        using var store = new UnlockEventStore();
        await using var monitor = new AchievementMonitor(
            source, store, TimeSpan.FromMilliseconds(40));

        var ready = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var unlocked = new TaskCompletionSource<UnlockEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int awards = 0;
        monitor.StatusChanged += (_, status) =>
        {
            if (status.StartsWith("Monitoring achievement_log", StringComparison.Ordinal))
                ready.TrySetResult(true);
        };
        monitor.UnlockDetected += (_, award) =>
        {
            Interlocked.Increment(ref awards);
            unlocked.TrySetResult(award);
        };
        await monitor.StartAsync();
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(store.GetRecent(source.SourceKey)); // empty log is not snapshot fallback

        await File.AppendAllTextAsync(source.JournalPath,
            "{\"v\":1,\"session\":\"match1\",\"seq\":1,\"time_ms\":1795260000125," +
            "\"steady_us\":55,\"type\":\"unlock\",\"id\":\"LIVE\"," +
            "\"unlock_seconds\":1795260000}\n");
        Assert.Equal("LIVE",
            (await unlocked.Task.WaitAsync(TimeSpan.FromSeconds(10))).AchievementId);
        await Task.Delay(160);
        Assert.Equal(1, Volatile.Read(ref awards));

        // A fallback snapshot that already contains the live unlock must not
        // generate a second synthetic reward.
        await File.WriteAllTextAsync(source.FilePath,
            Snapshot(("ALREADY", 1), ("LIVE", 1795260000)));
        await File.WriteAllTextAsync(source.JournalPath, ""); // match ended
        await Task.Delay(160);
        Assert.Equal(1, Volatile.Read(ref awards));
        File.Delete(source.JournalPath); // legacy GC fallback
        await Task.Delay(160);
        Assert.Equal(1, Volatile.Read(ref awards));
        Assert.Single(store.GetRecent(source.SourceKey));
    }

    [Fact]
    public async Task MalformedJournalNeverSilentlyFallsBackToSnapshotAwards()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-malformed-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var source = new GcFileSource(Path.Combine(folder, "achievements.txt"));
        await File.WriteAllTextAsync(source.FilePath, Snapshot(("A", 1)));
        await File.WriteAllTextAsync(source.JournalPath, "not-json\n");
        using var store = new UnlockEventStore();
        await using var monitor = new AchievementMonitor(
            source, store, TimeSpan.FromMilliseconds(45));
        var errored = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.StatusChanged += (_, message) =>
        {
            if (message.StartsWith("Journal Invalid:", StringComparison.Ordinal))
                errored.TrySetResult(true);
        };
        await monitor.StartAsync();
        await errored.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(store.GetRecent(source.SourceKey));
        Assert.False(store.GetCurrentState(source.SourceKey).HasValidSnapshot);
    }

    private static string JournalEvent(string type, ulong seq, string fields = "") =>
        "{\"v\":1,\"session\":\"process-A\",\"seq\":" + seq +
        ",\"time_ms\":1795260000125,\"steady_us\":44,\"type\":\"" +
        type + "\"" + fields + "}\n";

    [Fact]
    public async Task ExistingJournalAndDatabaseHistoryNeverReplayOnMonitorStart()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-silent-attach-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var source = new GcFileSource(Path.Combine(folder, "achievements.txt"));
        await File.WriteAllTextAsync(source.FilePath, Snapshot(("OLD", 4)));
        await File.WriteAllTextAsync(source.JournalPath,
            JournalEvent("match_start", 1, ",\"reason\":\"join\"") +
            JournalEvent("unlock", 2, ",\"id\":\"OLD\",\"unlock_seconds\":4"));
        using var store = new UnlockEventStore();
        await using var monitor = new AchievementMonitor(
            source, store, TimeSpan.FromMilliseconds(50));
        var firstRead = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var newlyEarned = new TaskCompletionSource<UnlockEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.StatusChanged += (_, message) =>
        {
            if (message.StartsWith("Monitoring achievement_log", StringComparison.Ordinal))
                firstRead.TrySetResult(true);
        };
        monitor.UnlockDetected += (_, award) => newlyEarned.TrySetResult(award);
        await monitor.StartAsync();
        await firstRead.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(store.GetRecent(source.SourceKey));
        Assert.Empty(store.GetPending(source.SourceKey));

        await File.AppendAllTextAsync(source.JournalPath,
            JournalEvent("unlock", 3, ",\"id\":\"FRESH\",\"unlock_seconds\":5"));
        Assert.Equal("FRESH",
            (await newlyEarned.Task.WaitAsync(TimeSpan.FromSeconds(10))).AchievementId);
        Assert.Single(store.GetRecent(source.SourceKey));
        Assert.False(File.Exists(Path.Combine(folder, "history.db")));
    }

    [Fact]
    public async Task WithJournalEarnedSnapshotIsReadAtMatchStartNotOnEveryPoll()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "rasc-match-baseline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var source = new GcFileSource(Path.Combine(folder, "achievements.txt"));
        await File.WriteAllTextAsync(source.FilePath, Snapshot(("A", 10)));
        await File.WriteAllTextAsync(source.JournalPath, "");
        using var store = new UnlockEventStore();
        await using var monitor = new AchievementMonitor(
            source, store, TimeSpan.FromMilliseconds(50));
        var observed = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var updated = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.StatusChanged += (_, msg) =>
        {
            if (msg.StartsWith("Monitoring achievement_log", StringComparison.Ordinal))
                observed.TrySetResult(true);
        };
        monitor.CurrentStateChanged += (_, state) =>
        {
            if (state.Achievements.ContainsKey("B")) updated.TrySetResult(true);
        };
        await monitor.StartAsync();
        await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Single(store.GetCurrentState(source.SourceKey).Achievements);

        await File.WriteAllTextAsync(source.FilePath,
            Snapshot(("A", 10), ("B", 20)));
        await Task.Delay(220);
        Assert.Single(store.GetCurrentState(source.SourceKey).Achievements);
        Assert.Empty(store.GetRecent(source.SourceKey));

        await File.WriteAllTextAsync(source.JournalPath,
            JournalEvent("match_start", 1, ",\"reason\":\"join\""));
        await updated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, store.GetCurrentState(source.SourceKey).Achievements.Count);
        Assert.Empty(store.GetRecent(source.SourceKey)); // B was previously earned
    }

}
