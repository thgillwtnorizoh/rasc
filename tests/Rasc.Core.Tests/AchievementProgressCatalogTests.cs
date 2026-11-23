using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class AchievementProgressCatalogTests
{
    [Fact]
    public void ShippedMappingHasComplete167IdCoverageAndNoLiveProgress()
    {
        var catalog = AchievementProgressCatalog.LoadDefault();
        Assert.Equal(167, catalog.Entries.Count);
        Assert.Equal(86, catalog.Entries.Count(x =>
            x.Classification == ProgressEligibility.ScalarCandidate));
        Assert.Equal(9, catalog.Entries.Count(x =>
            x.Classification == ProgressEligibility.CompositeCandidate));
        Assert.Equal(72, catalog.Entries.Count(x =>
            x.Classification == ProgressEligibility.UnlockOnly));
        Assert.Empty(catalog.Entries.Where(x => x.IsLiveEligible));
        Assert.Equal("total_kills", catalog.Find("KILL_ENEMY_LOW")!.StatId);
        Assert.Equal("TRACE_ONLY", catalog.Find("KILL_ENEMY_LOW")!.Evidence);
        Assert.False(catalog.Find("KILL_ENEMY_LOW")!.LiveEnabled);
        Assert.Equal(ProgressEligibility.CompositeCandidate,
            catalog.Find("META_WEAPONMASTER")!.Classification);
        Assert.Equal(ProgressEligibility.UnlockOnly,
            catalog.Find("HEADSHOTS_IN_ROUND")!.Classification);
    }

    [Theory]
    [InlineData("A,SCALAR_CANDIDATE,total_kills,100,TRACE_ONLY,CSGO-legacy,YES")]
    [InlineData("A,COMPOSITE_CANDIDATE,total_kills,100,VERIFIED:fixture,CSGO-legacy,YES")]
    [InlineData("A,SCALAR_CANDIDATE,total_kills,,VERIFIED:fixture,CSGO-legacy,YES")]
    [InlineData("A,SCALAR_CANDIDATE,total_kills,100,VERIFIED:fixture,,YES")]
    [InlineData("A,SCALAR_CANDIDATE,,100,VERIFIED:fixture,CSGO-legacy,YES")]
    public void CannotEnableIncompleteOrUnverifiedMapping(string row)
    {
        Assert.Throws<FormatException>(() =>
            AchievementProgressCatalog.Parse(
                AchievementProgressCatalog.Header + "\n" + row));
    }

    [Fact]
    public void ExplicitCompleteMappingIsEligibleForFixtures()
    {
        var mappings = AchievementProgressCatalog.Parse(
            AchievementProgressCatalog.Header + "\n" +
            "KILL_ENEMY_LOW,SCALAR_CANDIDATE,total_kills,100,VERIFIED:test-fixture,CSGO-legacy,YES");
        Assert.True(Assert.Single(mappings.Entries).IsLiveEligible);
    }
}
