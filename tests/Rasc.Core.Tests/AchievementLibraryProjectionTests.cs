using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class AchievementLibraryProjectionTests
{
    private const string SourceOne = "GC_SOURCE_ONE";
    private const string SourceTwo = "GC_SOURCE_TWO";
    private static readonly DateTimeOffset When =
        DateTimeOffset.FromUnixTimeSeconds(1790000200);

    private static GcSnapshot Snapshot(params (string id, uint time)[] achievements) =>
        new(new Dictionary<string, int>(), new Dictionary<string, float>(),
            achievements.ToDictionary(x => x.id, x => x.time, StringComparer.Ordinal));

    [Fact]
    public void BeforeValidSourceUnknownIsNotLockedOrZeroEarnedClaim()
    {
        using var store = new UnlockEventStore();
        var unknown = store.GetCurrentState(SourceOne);
        Assert.False(unknown.HasValidSnapshot);
        Assert.Empty(unknown.Achievements);

        var catalog = CuratedAchievementCatalog.LoadDefault();
        var rows = AchievementLibraryProjection.Create(catalog, null, unknown);
        Assert.Equal(167, rows.CatalogCount);
        Assert.Equal(167, rows.UnknownCatalogCount);
        Assert.Equal(0, rows.LockedCatalogCount);
        Assert.Equal(0, rows.EarnedCatalogCount);
        Assert.Equal(0, rows.ExtraSourceIdCount);
        Assert.All(rows.Entries, row =>
        {
            Assert.Equal(AchievementEarnedState.Unknown, row.State);
            Assert.Null(row.RawUnlockUnixSeconds);
        });
        Assert.Equal(CuratedRarity.Rare,
            rows.Entries.Single(row => row.ApiId == "KILL_ENEMY_HIGH").Rarity);
        Assert.Equal(CuratedRarity.Normal,
            rows.Entries.Single(row => row.ApiId == "AVENGE_FRIEND").Rarity);
    }

    [Fact]
    public void ValidEmptyBaselineIsAllLockedWithNoPopupHistory()
    {
        using var store = new UnlockEventStore();
        Assert.Empty(store.AcceptSnapshot(SourceOne, Snapshot(), When));
        var current = store.GetCurrentState(SourceOne);
        Assert.True(current.HasValidSnapshot);
        Assert.Empty(current.Achievements);
        var projection = AchievementLibraryProjection.Create(
            CuratedAchievementCatalog.LoadDefault(), null, current);
        Assert.Equal(167, projection.LockedCatalogCount);
        Assert.Equal(0, projection.UnknownCatalogCount);
        Assert.Empty(store.GetRecent(SourceOne));
        Assert.All(projection.Entries, row =>
            Assert.Equal(AchievementEarnedState.Locked, row.State));
    }

    [Fact]
    public void FirstValidBaselineShowsPreexistingAwardsSilentlyAndPreservesZeroTimestamp()
    {
        using var store = new UnlockEventStore();
        Assert.Empty(store.AcceptSnapshot(SourceOne, Snapshot(
            ("UNSTOPPABLE_FORCE", 1790000000),
            ("KILL_WITH_OWN_GUN", 0)), When));
        var saved = store.GetCurrentState(SourceOne);
        Assert.True(saved.HasValidSnapshot);
        Assert.Equal((uint)0, saved.Achievements["KILL_WITH_OWN_GUN"]);
        Assert.Empty(store.GetPending(SourceOne));
        var snapshot = AchievementLibraryProjection.Create(
            CuratedAchievementCatalog.LoadDefault(), null, saved);
        Assert.Equal(2, snapshot.EarnedCatalogCount);
        Assert.Equal(165, snapshot.LockedCatalogCount);
        var lost = snapshot.Entries.Single(row => row.ApiId == "KILL_WITH_OWN_GUN");
        Assert.Equal(AchievementEarnedState.Earned, lost.State);
        Assert.Equal((uint)0, lost.RawUnlockUnixSeconds);
        Assert.Null(lost.KnownUnlockTime);
        var force = snapshot.Entries.Single(row => row.ApiId == "UNSTOPPABLE_FORCE");
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1790000000), force.KnownUnlockTime);
        Assert.Equal(CuratedRarity.Rare, force.Rarity);
    }

    [Fact]
    public void StatsNeverCreateAnAwardAndExtraGcIdDoesNotBecomeCurated()
    {
        using var store = new UnlockEventStore();
        var fixture = new GcSnapshot(
            new Dictionary<string, int> { ["total_kills"] = 10000 },
            new Dictionary<string, float>(),
            new Dictionary<string, uint> {
                ["UNSTOPPABLE_FORCE"] = 1790000000,
                ["NOT_A_CURATED_ID"] = 1790000100
            });
        Assert.Empty(store.AcceptSnapshot(SourceOne, fixture, When));
        var current = store.GetCurrentState(SourceOne);
        var projection = AchievementLibraryProjection.Create(
            CuratedAchievementCatalog.LoadDefault(), null, current);
        Assert.Equal(1, projection.EarnedCatalogCount);
        Assert.Equal(166, projection.LockedCatalogCount);
        Assert.Equal(1, projection.ExtraSourceIdCount);
        Assert.Equal(168, projection.Entries.Count);
        Assert.Equal(AchievementEarnedState.Locked,
            projection.Entries.Single(row => row.ApiId == "KILL_ENEMY_HIGH").State);
        var extra = projection.Entries.Single(row => row.ApiId == "NOT_A_CURATED_ID");
        Assert.Equal(AchievementEarnedState.Earned, extra.State);
        Assert.True(extra.OutsideCuratedCatalog);
        Assert.Null(extra.Rarity);
        Assert.Equal("NOT_A_CURATED_ID", extra.DisplayName);
        Assert.Null(extra.Description);
    }

    [Fact]
    public void SourceIsolationAndNewMonitoringSessionForgetPriorHistory()
    {
        using (var store = new UnlockEventStore())
        {
            Assert.Empty(store.AcceptSnapshot(SourceOne,
                Snapshot(("UNSTOPPABLE_FORCE", 1790000000)), When));
            Assert.Empty(store.AcceptSnapshot(SourceTwo, Snapshot(), When));
            Assert.Single(store.AcceptSnapshot(SourceOne,
                Snapshot(("UNSTOPPABLE_FORCE", 1790000000),
                         ("IMMOVABLE_OBJECT", 1790000050)), When));
            Assert.Equal(2, store.GetCurrentState(SourceOne).Achievements.Count);
            Assert.True(store.GetCurrentState(SourceTwo).HasValidSnapshot);
            Assert.Empty(store.GetRecent(SourceTwo));
        }
        using var reopened = new UnlockEventStore();
        Assert.False(reopened.GetCurrentState(SourceOne).HasValidSnapshot);
        Assert.False(reopened.GetCurrentState(SourceTwo).HasValidSnapshot);
        Assert.False(reopened.GetCurrentState("THIRD_SOURCE").HasValidSnapshot);
        Assert.Empty(reopened.GetRecent(SourceOne));
    }

    [Fact]
    public void NewSnapshotChangesOnlyItsOwnCurrentStateWithoutReplayingBaseline()
    {
        using var store = new UnlockEventStore();
        store.AcceptSnapshot(SourceOne,
            Snapshot(("UNSTOPPABLE_FORCE", 1790000000)), When);
        Assert.Single(store.AcceptSnapshot(SourceOne,
            Snapshot(("UNSTOPPABLE_FORCE", 1790000000),
                     ("IMMOVABLE_OBJECT", 1790000050)), When));
        var now = store.GetCurrentState(SourceOne);
        var result = AchievementLibraryProjection.Create(
            CuratedAchievementCatalog.LoadDefault(), null, now);
        Assert.Equal(2, result.EarnedCatalogCount);
        Assert.Equal(165, result.LockedCatalogCount);
        Assert.Single(store.GetPending(SourceOne));
        Assert.Empty(store.AcceptSnapshot(SourceOne,
            Snapshot(("UNSTOPPABLE_FORCE", 1790000000),
                     ("IMMOVABLE_OBJECT", 1790000050)), When));
        Assert.Single(store.GetRecent(SourceOne));
    }

    [Fact]
    public void DisplayUsesCompleteLocalTextWithoutChangingRarityOrAwardState()
    {
        const string originalShape = """
            "lang" { "Tokens" {
                "UNSTOPPABLE_FORCE_NAME" "Example Four"
                "UNSTOPPABLE_FORCE_DESC" "Four in one round (synthetic)."
                "KILL_ENEMY_HIGH_NAME" "Example Grind"
                "KILL_ENEMY_HIGH_DESC" "Example cumulative test."
            }}
            """;
        var translations = CsgoAchievementLocalization.Parse(originalShape);
        var snapshot = AchievementLibraryProjection.Create(
            CuratedAchievementCatalog.LoadDefault(), translations,
            GcCurrentAchievementState.Unknown);
        var named = snapshot.Entries.Single(x => x.ApiId == "UNSTOPPABLE_FORCE");
        Assert.Equal("Example Four", named.DisplayName);
        Assert.Equal("Four in one round (synthetic).", named.Description);
        Assert.Equal("english", named.Language);
        Assert.Equal(CuratedRarity.Rare, named.Rarity);
        Assert.Equal(AchievementEarnedState.Unknown, named.State);
        Assert.Equal("AVENGE_FRIEND",
            snapshot.Entries.Single(x => x.ApiId == "AVENGE_FRIEND").DisplayName);
    }

    [Fact]
    public void ClosingAndReopeningSourceDoesNotMakeMissingFileMeanReset()
    {
        using var store = new UnlockEventStore();
        store.AcceptSnapshot(SourceOne,
            Snapshot(("KILL_WITH_OWN_GUN", 0)), When);
        // Missing/invalid/unavailable GCFileSource reads never call AcceptSnapshot.
        var last = store.GetCurrentState(SourceOne);
        Assert.True(last.HasValidSnapshot);
        Assert.Contains("KILL_WITH_OWN_GUN", last.Achievements.Keys);
        var next = store.GetCurrentState(SourceOne);
        Assert.Equal(last.Achievements.Count, next.Achievements.Count);
        Assert.Equal((uint)0, next.Achievements["KILL_WITH_OWN_GUN"]);
    }
}
