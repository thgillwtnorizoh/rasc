using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ModernAchievementWaveQueueTests
{
    private static readonly DateTimeOffset Origin =
        DateTimeOffset.FromUnixTimeMilliseconds(1_790_000_000_000);

    private static UnlockEvent E(int id, int offsetMs) =>
        new("q-" + id, "synthetic-gc", "ACH_" + id, 1790000000,
            Origin.AddMilliseconds(offsetMs));

    [Fact]
    public void ReceiptsWithin800msMakeOneWaveAndBoundaryMakesNext()
    {
        var q = new ModernAchievementWaveQueue();
        Assert.True(q.Enqueue(E(1, 0)));
        Assert.True(q.Enqueue(E(2, 400)));
        Assert.True(q.Enqueue(E(3, 799)));
        Assert.True(q.Enqueue(E(4, 800)));
        Assert.Equal(2, q.PendingWaveCount);
        Assert.Equal(4, q.PendingAchievementCount);
        Assert.Null(q.TakeReady(Origin.AddMilliseconds(799)));
        var first = q.TakeReady(Origin.AddMilliseconds(800));
        Assert.NotNull(first);
        Assert.Equal(new[] { "q-1", "q-2", "q-3" },
            first.Achievements.Select(x => x.EventId));
        Assert.Equal(Origin, first.FirstSignalAt);
        Assert.Equal(Origin.AddMilliseconds(800), first.CollectionEndsAt);
        Assert.Null(q.TakeReady(Origin.AddMilliseconds(1599)));
        Assert.Equal("q-4", Assert.Single(q.TakeReady(
            Origin.AddMilliseconds(1600))!.Achievements).EventId);
        Assert.Equal(0, q.PendingAchievementCount);
    }

    [Fact]
    public void CapacityOverflowRemainsFifoAndNeverMixesLaterSignalCohorts()
    {
        var q = new ModernAchievementWaveQueue();
        for (var i = 1; i <= 8; i++)
            Assert.True(q.Enqueue(E(i, 0)));
        var later = E(9, 3000);
        Assert.True(q.Enqueue(later));
        Assert.Equal(4, q.PendingWaveCount); // first 3, second 3, third 2, fourth 1
    }

    [Fact]
    public void OverflowAndLaterAwardsAreFourSeparateImmutableGroups()
    {
        var q = new ModernAchievementWaveQueue();
        for (var i = 1; i <= 8; i++)
            Assert.True(q.Enqueue(E(i, 0)));
        Assert.True(q.Enqueue(E(9, 3000)));
        Assert.Equal(4, q.PendingWaveCount);
        Assert.Equal(9, q.PendingAchievementCount);
        var first = q.TakeReady(Origin.AddMilliseconds(800))!;
        var second = q.TakeReady(Origin.AddMilliseconds(800))!;
        var third = q.TakeReady(Origin.AddMilliseconds(800))!;
        Assert.Equal(new[] { "q-1", "q-2", "q-3" },
            first.Achievements.Select(x => x.EventId));
        Assert.Equal(new[] { "q-4", "q-5", "q-6" },
            second.Achievements.Select(x => x.EventId));
        Assert.Equal(new[] { "q-7", "q-8" },
            third.Achievements.Select(x => x.EventId));
        Assert.Null(q.TakeReady(Origin.AddMilliseconds(3799)));
        Assert.Equal("q-9", Assert.Single(q.TakeReady(
            Origin.AddMilliseconds(3800))!.Achievements).EventId);
    }

    [Fact]
    public void BusyUiThreadDoesNotRewriteOriginalSourceSignalOrExpirePending()
    {
        var q = new ModernAchievementWaveQueue();
        Assert.True(q.Enqueue(E(1, 0)));
        Assert.True(q.Enqueue(E(2, 600)));
        // Model a 15s dispatcher stall after the GC monitor recorded both.
        var wave = q.TakeReady(Origin.AddSeconds(15))!;
        Assert.Equal(new[] { "q-1", "q-2" },
            wave.Achievements.Select(x => x.EventId));
        Assert.Equal(Origin.AddMilliseconds(800), wave.CollectionEndsAt);
        Assert.Empty(q.TakeReady(Origin.AddMinutes(10))?.Achievements ?? []);
        Assert.True(q.Enqueue(E(3, 21000)));
        Assert.Equal("q-3", Assert.Single(q.TakeReady(
            Origin.AddMinutes(1))!.Achievements).EventId);
    }

    [Fact]
    public void DuplicateAndReversedSourceSignalsDoNotReorderEarlierGroups()
    {
        var q = new ModernAchievementWaveQueue();
        Assert.True(q.Enqueue(E(1, 1000)));
        Assert.False(q.Enqueue(E(1, 1000)));
        Assert.True(q.Enqueue(E(2, 700))); // abnormal clock jump: separate group
        Assert.True(q.Enqueue(E(3, 1100))); // still ordered after second
        Assert.Equal(3, q.PendingWaveCount);
        Assert.Equal(new[] { "q-1", "q-2", "q-3" },
            Enumerable.Range(0, 3)
                .Select(_ => q.TakeReady(Origin.AddSeconds(10))!.Achievements.Single().EventId));
        Assert.Throws<ArgumentException>(() =>
            q.Enqueue(new UnlockEvent("", "x", "y", 0, Origin)));
        q.Clear();
        Assert.Equal(0, q.PendingWaveCount);
        Assert.True(q.Enqueue(E(1, 1000)));
    }
}
