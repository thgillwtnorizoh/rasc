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

    [Fact]
    public void BurstUsesAuthoritativeUnlockTimestampThenStableTieBreakAndPersistsDeliveryOrder()
    {
        using var store = new UnlockEventStore(NewDatabase());
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(), Now));
        var first = store.AcceptSnapshot(Source,
            Snapshot(("Z_FIRST", 50), ("A_LATEST", 52), ("B_SECOND", 51),
                     ("X_UNKNOWN_TIME", 0), ("C_TIE", 51)), Now);
        var expected = new[] { "Z_FIRST", "B_SECOND", "C_TIE", "A_LATEST", "X_UNKNOWN_TIME" };
        Assert.Equal(expected, first.Select(e => e.AchievementId));
        // Hash-based event IDs MUST NOT reorder simultaneous GC awards.
        Assert.Equal(expected, store.GetPending(Source).Select(e => e.AchievementId));
        Assert.Equal(expected.Reverse(), store.GetRecent(Source).Select(e => e.AchievementId));
        foreach (var evt in first) store.MarkDelivered(evt.EventId, Now);
        Assert.Empty(store.GetPending(Source));
        Assert.Empty(store.AcceptSnapshot(Source,
            Snapshot(("X_UNKNOWN_TIME", 0), ("C_TIE", 51), ("A_LATEST", 52),
                     ("B_SECOND", 51), ("Z_FIRST", 50)), Now));
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
    [Fact]
    public void JournalEventIdentityIsStableAcrossReadsAndRestarts()
    {
        var db = NewDatabase();
        var incoming = new[]
        {
            new GcJournalUnlock("session-a", 11, "A", 10, Now),
            new GcJournalUnlock("session-a", 12, "B", 10, Now)
        };
        string firstId;
        using (var store = new UnlockEventStore(db))
        {
            Assert.Empty(store.AcceptSnapshot(Source, Snapshot(), Now, recordNewUnlocks: false));
            var events = store.AcceptJournal(Source, incoming, Now);
            Assert.Equal(new[] { "A", "B" }, events.Select(e => e.AchievementId));
            firstId = events[0].EventId;
            Assert.Empty(store.AcceptJournal(Source, incoming, Now));
            Assert.Equal(2, store.GetRecent(Source).Count);
            Assert.True(store.GetCurrentState(Source).Achievements.ContainsKey("A"));
            store.MarkDelivered(firstId, Now);
        }
        using (var reopened = new UnlockEventStore(db))
        {
            Assert.Empty(reopened.AcceptJournal(Source, incoming, Now));
            var fresh = Assert.Single(reopened.AcceptJournal(Source,
                [new GcJournalUnlock("session-b", 1, "A", 10, Now)], Now));
            Assert.NotEqual(firstId, fresh.EventId);
            Assert.Equal(3, reopened.GetRecent(Source).Count);
            Assert.Equal(2, reopened.GetPending(Source).Count);
        }
    }

    [Fact]
    public void SilentJournalSnapshotUpdateCannotGeneratePhantomFallbackAwards()
    {
        using var store = new UnlockEventStore(NewDatabase());
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("OLD", 1)), Now,
            recordNewUnlocks: false));
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("OLD", 1), ("B", 2)), Now,
            recordNewUnlocks: false));
        Assert.Single(store.AcceptJournal(Source,
            [new GcJournalUnlock("one", 4, "B", 2, Now)], Now));
        // A missing journal is the only fallback case. The same achievement
        // must not be re-awarded by the snapshot path afterward.
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("OLD", 1), ("B", 2)), Now));
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(), Now));
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("OLD", 1), ("B", 2)), Now,
            recordNewUnlocks: false));
        Assert.Single(store.GetRecent(Source));
    }

}
