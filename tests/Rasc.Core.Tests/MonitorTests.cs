using Rasc.Core;
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
}
