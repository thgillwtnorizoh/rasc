using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

/// <summary>
/// Development integration: GC file snapshot -> durable event store -> Classic timeline.
/// Does not qualify WPF's live window appearance.
/// </summary>
public sealed class ClassicToastIntegrationTests
{
    private static GcSnapshot Snapshot(params (string, uint)[] values) =>
        new(new Dictionary<string, int>(), new Dictionary<string, float>(),
            values.ToDictionary(x => x.Item1, x => x.Item2, StringComparer.Ordinal));

    [Fact]
    public void SixNewUnlocksProduceThreeThenThreeWithoutReplayFromHistory()
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
        Assert.Equal(3, first.Count);
        Assert.Equal(new[] { "AWARD_1", "AWARD_2", "AWARD_3" },
            first.Select(x => x.Event.AchievementId));
        var second = queue.Advance(TimeSpan.FromMilliseconds(6900));
        Assert.Equal(new[] { "AWARD_4", "AWARD_5", "AWARD_6" },
            second.Select(x => x.Event.AchievementId));

        // History returns events, but is explicitly NOT a source of presentation enqueue.
        Assert.Equal(6, store.GetRecent(source).Count);
        Assert.Empty(store.GetPending(source));
        Assert.Empty(store.AcceptSnapshot(source,
            Snapshot(new[] { ("EXISTING", (uint)1) }.Concat(newAwards).ToArray()), now));
    }
}
