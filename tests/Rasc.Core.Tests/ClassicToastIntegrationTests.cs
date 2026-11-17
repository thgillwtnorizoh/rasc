using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

/// <summary>
/// Development-only integration: GC snapshots -> durable ledger -> Classic slide timeline.
/// Does not establish WPF GUI/runtime visual equivalence.
/// </summary>
public sealed class ClassicToastIntegrationTests
{
    private static GcSnapshot Snapshot(params (string, uint)[] values) =>
        new(new Dictionary<string, int>(), new Dictionary<string, float>(),
            values.ToDictionary(x => x.Item1, x => x.Item2, StringComparer.Ordinal));

    [Fact]
    public void SixNewUnlocksAnimateInOrderWithoutReplayFromRestoredHistory()
    {
        var db = Path.Combine(Path.GetTempPath(), "rasc-classic-" + Guid.NewGuid().ToString("N") + ".db");
        const string source = "TEST_GC";
        var now = DateTimeOffset.FromUnixTimeSeconds(1_794_738_859);
        using var store = new UnlockEventStore(db);
        store.AcceptSnapshot(source, Snapshot(("EXISTING", 1)), now); // silent baseline
        var newAwards = Enumerable.Range(1, 6)
            .Select(i => ($"AWARD_{i}", (uint)(100 + i))).ToArray();
        var events = store.AcceptSnapshot(source,
            Snapshot(new[] { ("EXISTING", (uint)1) }.Concat(newAwards).ToArray()), now);
        Assert.Equal(6, events.Count);

        var queue = new ClassicToastScheduler();
        foreach (var e in events) Assert.True(queue.Enqueue(e));
        foreach (var e in events) store.MarkDelivered(e.EventId, now);
        Assert.Empty(store.GetPending(source));

        var first = queue.Advance(TimeSpan.Zero);
        Assert.Equal("AWARD_1", Assert.Single(first).Event.AchievementId);
        Assert.Equal(ClassicToastStage.Entering, first[0].Stage);

        var allStarts = new List<string>();
        for (var ms = 0; ms <= 35000; ms += 25)
        {
            foreach (var frame in queue.Advance(TimeSpan.FromMilliseconds(ms)))
                if (frame.Stage == ClassicToastStage.Entering &&
                    !allStarts.Contains(frame.Event.AchievementId))
                    allStarts.Add(frame.Event.AchievementId);
        }
        Assert.Equal(new[] { "AWARD_1", "AWARD_2", "AWARD_3", "AWARD_4", "AWARD_5", "AWARD_6" }, allStarts);
        Assert.True(queue.IsIdle);

        // History is intentionally not a source of presentation enqueue.
        Assert.Equal(6, store.GetRecent(source).Count);
        Assert.Empty(store.GetPending(source));
        Assert.Empty(store.AcceptSnapshot(source,
            Snapshot(new[] { ("EXISTING", (uint)1) }.Concat(newAwards).ToArray()), now));
    }
}
