using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class AchievementProgressTrackerTests
{
    private static AchievementProgressTracker FixtureTracker() =>
        new(AchievementProgressCatalog.Parse(
            AchievementProgressCatalog.Header + "\n" +
            "KILL_ENEMY_LOW,SCALAR_CANDIDATE,total_kills,100,VERIFIED:fixture,CSGO-legacy,YES"));

    private static GcSnapshot Sample(int totalKills, params string[] earned) =>
        new(new Dictionary<string, int> { ["total_kills"] = totalKills },
            new Dictionary<string, float>(),
            earned.ToDictionary(id => id, _ => (uint)1, StringComparer.Ordinal));

    [Fact]
    public void Crosses25_50_75OnlyWithPriorValidCounterAndNoDuplicates()
    {
        var tracker = FixtureTracker();
        var now = DateTimeOffset.UnixEpoch;
        Assert.Empty(tracker.Observe("source", Sample(24), now)); // silent baseline
        var one = Assert.Single(tracker.Observe("source", Sample(25), now));
        Assert.Equal(25, one.Percent);
        Assert.Equal(25, one.Current);
        Assert.True(one.Event.IsProgress);
        Assert.Equal(25, one.Event.MilestonePercent);
        Assert.Empty(tracker.Observe("source", Sample(25), now));
        Assert.Empty(tracker.Observe("source", Sample(49), now));
        Assert.Equal(50, Assert.Single(tracker.Observe("source", Sample(50), now)).Percent);
        Assert.Equal(75, Assert.Single(tracker.Observe("source", Sample(75), now)).Percent);
        Assert.Empty(tracker.Observe("source", Sample(80), now));
        Assert.Empty(tracker.Observe("source", Sample(100), now)); // unlock only
    }

    [Fact]
    public void SkippedMilestonesEmitHighestOnlyAndDoNotReappearAfterRollback()
    {
        var tracker = FixtureTracker();
        var now = DateTimeOffset.UnixEpoch;
        tracker.Observe("src", Sample(5), now);
        Assert.Equal(75, Assert.Single(tracker.Observe("src", Sample(80), now)).Percent);
        Assert.Empty(tracker.Observe("src", Sample(3), now)); // reset
        Assert.Empty(tracker.Observe("src", Sample(26), now)); // 25 cannot follow 75
        Assert.Empty(tracker.Observe("src", Sample(50), now)); // 50 cannot follow 75
        Assert.Empty(tracker.Observe("src", Sample(76), now)); // 75 already emitted
    }

    [Fact]
    public void SourceIsolationAndAcceptedUnlockSuppressAllFutureProgress()
    {
        var tracker = FixtureTracker();
        var now = DateTimeOffset.UnixEpoch;
        tracker.Observe("one", Sample(0), now);
        tracker.Observe("two", Sample(10), now);
        Assert.Equal(25, Assert.Single(tracker.Observe("one", Sample(25), now)).Percent);
        tracker.AcceptUnlock("one", "KILL_ENEMY_LOW");
        Assert.Empty(tracker.Observe("one", Sample(51), now));
        Assert.Equal(50, Assert.Single(tracker.Observe("two", Sample(50), now)).Percent);
        Assert.Empty(tracker.Observe("three", Sample(50, "KILL_ENEMY_LOW"), now));
        Assert.Empty(tracker.Observe("three", Sample(75), now));
    }

    [Fact]
    public void UnverifiedCatalogCanNeverGenerateProgress()
    {
        var tracker = new AchievementProgressTracker(
            AchievementProgressCatalog.LoadDefault());
        var now = DateTimeOffset.UnixEpoch;
        Assert.Empty(tracker.Observe("src", Sample(0), now));
        Assert.Empty(tracker.Observe("src", Sample(90), now));
    }

    [Fact]
    public void MissingAndNegativeCounterDoNotEstablishBaseline()
    {
        var tracker = FixtureTracker();
        var now = DateTimeOffset.UnixEpoch;
        var missing = new GcSnapshot(new Dictionary<string, int>(),
            new Dictionary<string, float>(), new Dictionary<string, uint>());
        Assert.Empty(tracker.Observe("src", missing, now));
        Assert.Empty(tracker.Observe("src", Sample(-4), now));
        Assert.Empty(tracker.Observe("src", Sample(30), now));
        Assert.Equal(50, Assert.Single(tracker.Observe("src", Sample(50), now)).Percent);
    }
}
