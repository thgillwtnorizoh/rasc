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
        var db = Path.Combine(folder, "history.db");
        var initial = Snapshot(("A", 1));
        await File.WriteAllTextAsync(path, initial);
        using var store = new UnlockEventStore(db);
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
        File.Move(temp, path, overwrite: true);
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
        using var store = new UnlockEventStore(Path.Combine(folder, "history.db"));
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
    public async Task StoppingMonitorDoesNotWaitForLongPollingInterval()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-stop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "achievements.txt");
        await File.WriteAllTextAsync(path, Snapshot(("BASELINE", 42)));
        using var store = new UnlockEventStore(Path.Combine(folder, "history.db"));
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

}
