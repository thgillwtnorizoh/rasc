using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class EventStoreTests
{
    private static GcSnapshot Snapshot(params (string name, uint stamp)[] awards) =>
        new(new Dictionary<string, int>(), new Dictionary<string, float>(),
            awards.ToDictionary(x => x.name, x => x.stamp, StringComparer.Ordinal));

    private static string NewDatabase() =>
        Path.Combine(Path.GetTempPath(), "rasc-test-" + Guid.NewGuid().ToString("N") + ".db");
    private const string Source = "C:\\CSGO\\CSGO_GC\\ACHIEVEMENTS.TXT";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_794_738_859);

    [Fact] public void FirstSnapshotIsNonNotifyingBaseline()
    {
        using var store = new UnlockEventStore(NewDatabase());
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("A", 1), ("B", 2)), Now));
        Assert.Empty(store.GetPending(Source));
        Assert.Empty(store.GetRecent(Source));
    }

    [Fact] public void NewAchievementsAreEmittedOnlyOnce()
    {
        using var store = new UnlockEventStore(NewDatabase());
        store.AcceptSnapshot(Source, Snapshot(("A", 1)), Now);
        var events = store.AcceptSnapshot(Source, Snapshot(("A", 1), ("B", 2), ("C", 3)), Now);
        Assert.Equal(2, events.Count);
        Assert.Equal(new[] { "B", "C" }, events.Select(x => x.AchievementId));
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("A", 1), ("B", 2), ("C", 3)), Now));
        Assert.Equal(2, store.GetPending(Source).Count);
        store.MarkDelivered(events[0].EventId, Now);
        Assert.Single(store.GetPending(Source));
    }

    [Fact] public void RestartPreservesDeduplicationAndPendingDelivery()
    {
        var db = NewDatabase();
        string eventId;
        using (var first = new UnlockEventStore(db))
        {
            first.AcceptSnapshot(Source, Snapshot(("A", 1)), Now);
            eventId = Assert.Single(first.AcceptSnapshot(Source, Snapshot(("A", 1), ("B", 2)), Now)).EventId;
        }
        using (var restored = new UnlockEventStore(db))
        {
            Assert.Equal(eventId, Assert.Single(restored.GetPending(Source)).EventId);
            Assert.Empty(restored.AcceptSnapshot(Source, Snapshot(("A", 1), ("B", 2)), Now));
            restored.MarkDelivered(eventId, Now);
        }
        using var reopened = new UnlockEventStore(db);
        Assert.Empty(reopened.GetPending(Source));
        Assert.Single(reopened.GetRecent(Source));
    }

    [Fact] public void MissingThenReawardSameTimestampDoesNotDuplicate()
    {
        using var store = new UnlockEventStore(NewDatabase());
        store.AcceptSnapshot(Source, Snapshot(), Now);
        var first = Assert.Single(store.AcceptSnapshot(Source, Snapshot(("A", 7)), Now));
        store.AcceptSnapshot(Source, Snapshot(), Now);
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("A", 7)), Now));
        store.AcceptSnapshot(Source, Snapshot(), Now);
        var second = Assert.Single(store.AcceptSnapshot(Source, Snapshot(("A", 8)), Now));
        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact] public void DifferentSourcesHaveIndependentBaselines()
    {
        using var store = new UnlockEventStore(NewDatabase());
        store.AcceptSnapshot("ONE", Snapshot(("A", 1)), Now);
        store.AcceptSnapshot("TWO", Snapshot(("A", 1)), Now);
        Assert.Empty(store.GetRecent("ONE"));
        Assert.Single(store.AcceptSnapshot("TWO", Snapshot(("A", 1), ("B", 2)), Now));
        Assert.Empty(store.GetRecent("ONE"));
    }
}
