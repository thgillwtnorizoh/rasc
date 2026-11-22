using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class EventStoreTests
{
    private const string Source = "CSGO/CSGO_GC/ACHIEVEMENTS.TXT";
    private static readonly DateTimeOffset Now =
        DateTimeOffset.FromUnixTimeSeconds(1795260000);
    private static GcSnapshot Snapshot(params (string id, uint stamp)[] a) =>
        new(new Dictionary<string, int>(), new Dictionary<string, float>(),
            a.ToDictionary(x => x.id, x => x.stamp, StringComparer.Ordinal));
    private static GcJournalUnlock Award(string session, ulong seq,
        string id, uint stamp) => new(session, seq, id, stamp, Now);

    [Fact]
    public void FirstSnapshotIsSilentAndFutureLegacyDiffsAreOrdered()
    {
        using var store = new UnlockEventStore();
        Assert.Empty(store.AcceptSnapshot(Source, Snapshot(("A", 1)), Now));
        Assert.Empty(store.GetPending(Source));
        Assert.Empty(store.GetRecent(Source));
        var events = store.AcceptSnapshot(Source,
            Snapshot(("A", 1), ("C", 3), ("B", 2)), Now);
        Assert.Equal(new[] { "B", "C" }, events.Select(x => x.AchievementId));
        Assert.Equal(new[] { "B", "C" },
            store.GetPending(Source).Select(x => x.AchievementId));
        foreach (var evt in events) store.MarkDelivered(evt.EventId, Now);
        Assert.Empty(store.GetPending(Source));
    }

    [Fact]
    public void NewMonitoringSessionHasNoSavedPendingEventsOrHistory()
    {
        using (var first = new UnlockEventStore())
        {
            first.AcceptSnapshot(Source, Snapshot(), Now);
            Assert.Single(first.AcceptSnapshot(Source, Snapshot(("OLD", 4)), Now));
            Assert.Single(first.GetPending(Source));
        }
        using var restarted = new UnlockEventStore();
        Assert.Empty(restarted.GetPending(Source));
        Assert.Empty(restarted.GetRecent(Source));
        Assert.Empty(restarted.AcceptSnapshot(Source, Snapshot(("OLD", 4)), Now));
        Assert.Empty(restarted.GetPending(Source));
    }

    [Fact]
    public void InitialJournalRowsAreSilentlyAcknowledgedThenOnlyNewRowsNotify()
    {
        using var store = new UnlockEventStore();
        var oldRows = new[] { Award("s1", 1, "OLD", 1),
            Award("s1", 2, "OLDER", 2) };
        Assert.Empty(store.AcceptJournal(Source, oldRows, Now,
            baselineExisting: true));
        Assert.Empty(store.GetRecent(Source));
        var newRows = oldRows.Append(Award("s1", 3, "NEW", 3)).ToArray();
        Assert.Equal("NEW",
            Assert.Single(store.AcceptJournal(Source, newRows, Now)).AchievementId);
        Assert.Empty(store.AcceptJournal(Source, newRows, Now));
        Assert.Single(store.GetRecent(Source));
        // In the same RASC session, a new GC match may truncate the journal,
        // but the writer's sequence stays monotonic and old records never replay.
        Assert.Equal("SECOND_MATCH",
            Assert.Single(store.AcceptJournal(Source,
                [Award("s1", 4, "SECOND_MATCH", 4)], Now)).AchievementId);
        Assert.Equal(2, store.GetRecent(Source).Count);
    }

    [Fact]
    public void JournalIdentityIsSourceSessionSequenceNotUnlockTimestamp()
    {
        using var store = new UnlockEventStore();
        var a = Assert.Single(store.AcceptJournal(Source,
            [Award("process-one", 7, "A", 30)], Now));
        Assert.Empty(store.AcceptJournal(Source,
            [Award("process-one", 7, "B", 30)], Now));
        var b = Assert.Single(store.AcceptJournal(Source,
            [Award("process-two", 1, "A", 30)], Now));
        Assert.NotEqual(a.EventId, b.EventId);
        Assert.Equal(2, store.GetRecent(Source).Count);
    }

    [Fact]
    public void EarnedStateIsKnownOnlyAfterValidatedSnapshot()
    {
        using var store = new UnlockEventStore();
        store.AcceptJournal(Source, [Award("live", 1, "LIVE", 1)], Now);
        Assert.False(store.GetCurrentState(Source).HasValidSnapshot);
        Assert.Empty(store.AcceptSnapshot(Source,
            Snapshot(("SAVED", 12)), Now, recordNewUnlocks: false));
        var next = Assert.Single(store.AcceptJournal(Source,
            [Award("live", 2, "NEW", 22)], Now));
        Assert.Equal("NEW", next.AchievementId);
        var state = store.GetCurrentState(Source);
        Assert.True(state.HasValidSnapshot);
        Assert.Equal(2, state.Achievements.Count);
        Assert.Contains("SAVED", state.Achievements.Keys);
        Assert.Contains("NEW", state.Achievements.Keys);
    }

    [Fact]
    public void BadJournalDoesNotCreateSyntheticAwardFromSnapshotBaseline()
    {
        using var store = new UnlockEventStore();
        Assert.Empty(store.AcceptSnapshot(Source,
            Snapshot(("A", 1)), Now, recordNewUnlocks: false));
        Assert.Empty(store.AcceptSnapshot(Source,
            Snapshot(("A", 1), ("B", 2)), Now, recordNewUnlocks: false));
        Assert.Empty(store.GetRecent(Source));
        Assert.Equal("B", Assert.Single(store.AcceptJournal(Source,
            [Award("live", 8, "B", 2)], Now)).AchievementId);
        Assert.Empty(store.AcceptJournal(Source,
            [Award("live", 8, "B", 2)], Now));
    }

    [Fact]
    public void SourceIsolationAndFallbackTimestampDeduplication()
    {
        using var store = new UnlockEventStore();
        store.AcceptSnapshot("one", Snapshot(), Now);
        store.AcceptSnapshot("two", Snapshot(), Now);
        Assert.Single(store.AcceptSnapshot("two", Snapshot(("A", 3)), Now));
        Assert.Empty(store.GetRecent("one"));
        store.AcceptSnapshot("two", Snapshot(), Now);
        Assert.Empty(store.AcceptSnapshot("two", Snapshot(("A", 3)), Now));
        Assert.Single(store.GetRecent("two"));
    }
}
