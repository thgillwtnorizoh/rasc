using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class CuratedAchievementCatalogTests
{
    private static string Row(string id, string factors = "3,4,3,4,4",
        string capstone = "NO", string rarityOverride = "") =>
        $"{id},example_icon.jpg,{new string('A', 64)},{factors},{capstone},{rarityOverride}";

    private static CuratedAchievementCatalog Load(params string[] rows) =>
        CuratedAchievementCatalog.Parse(
            CuratedAchievementCatalog.Header + "\n" + string.Join("\n", rows) + "\n");

    [Fact]
    public void BundledV02PreservesApproved167And63Rare104Normal()
    {
        var catalog = CuratedAchievementCatalog.LoadDefault();
        Assert.Equal(167, catalog.Entries.Count);
        Assert.Equal(167, catalog.Entries.Select(a => a.ApiId).Distinct().Count());
        Assert.Equal(63, catalog.Entries.Count(a => a.Rarity == CuratedRarity.Rare));
        Assert.Equal(104, catalog.Entries.Count(a => a.Rarity == CuratedRarity.Normal));
        Assert.Equal(17, CuratedAchievementCatalog.RareThreshold);
        Assert.All(catalog.Entries, a => Assert.InRange(a.Score, 0, 25));
        Assert.All(catalog.Entries, a => Assert.Null(a.RarityOverride));
    }

    [Fact]
    public void OwnerExamplesAndCapstoneRetainOriginalFactorTotals()
    {
        var catalog = CuratedAchievementCatalog.LoadDefault();

        var god = Assert.IsType<CuratedAchievementDefinition>(catalog.Find("KILL_ENEMY_HIGH"));
        Assert.Equal(14, god.Score);
        Assert.True(god.SeriesCapstone);
        Assert.Equal("series-capstone", god.RarityBasis);
        Assert.Equal(CuratedRarity.Rare, god.Rarity);

        var lost = Assert.IsType<CuratedAchievementDefinition>(catalog.Find("KILL_WITH_OWN_GUN"));
        Assert.Equal(19, lost.Score);
        Assert.Equal(CuratedRarity.Rare, lost.Rarity);
        Assert.Equal(18, catalog.Find("UNSTOPPABLE_FORCE")!.Score);
        Assert.Equal(18, catalog.Find("IMMOVABLE_OBJECT")!.Score);
        Assert.Equal(22, catalog.Find("WIN_ROUNDS_WITHOUT_BUYING")!.Score);
        Assert.Null(catalog.Find("SURVIVED_HEADSHOT_DUE_TO_HELMET"));
        Assert.Null(catalog.Find("UNKNOWN_NOT_IN_CATALOG"));
        Assert.Null(catalog.Find(null));
    }

    [Fact]
    public void ExactlySeventeenIsRareAndSixteenNormal()
    {
        var entries = Load(
            Row("SCORE_16", "3,4,3,4,2"),
            Row("SCORE_17", "4,4,3,4,2"),
            Row("CAPSTONE", "1,1,1,1,1", "YES"));
        Assert.Equal(CuratedRarity.Normal, entries.Find("SCORE_16")!.Rarity);
        Assert.Equal(CuratedRarity.Rare, entries.Find("SCORE_17")!.Rarity);
        Assert.Equal(CuratedRarity.Rare, entries.Find("CAPSTONE")!.Rarity);
        Assert.Equal("series-capstone", entries.Find("CAPSTONE")!.RarityBasis);
    }

    [Fact]
    public void OwnerCanOverrideRarityWithoutDestroyingScoringOrOtherRows()
    {
        var catalog = Load(
            Row("OWNER_NORMAL", rarityOverride: "NORMAL"),
            Row("OWNER_RARE", "1,1,1,1,1", rarityOverride: "RARE"),
            Row("UNALTERED", "1,1,1,1,1"));
        Assert.Equal(18, catalog.Find("OWNER_NORMAL")!.Score);
        Assert.Equal(CuratedRarity.Normal, catalog.Find("OWNER_NORMAL")!.Rarity);
        Assert.Equal("owner-override", catalog.Find("OWNER_NORMAL")!.RarityBasis);
        Assert.Equal(5, catalog.Find("OWNER_RARE")!.Score);
        Assert.Equal(CuratedRarity.Rare, catalog.Find("OWNER_RARE")!.Rarity);
        Assert.Equal(CuratedRarity.Normal, catalog.Find("UNALTERED")!.Rarity);
    }

    [Fact]
    public void StrictCsvRejectsDuplicatesUnsafeNamesAndMalformedValues()
    {
        void Reject(string csv) =>
            Assert.Throws<FormatException>(() => CuratedAchievementCatalog.Parse(csv));
        string head = CuratedAchievementCatalog.Header + "\n";
        Reject(head + Row("DUPLICATE") + "\n" + Row("DUPLICATE"));
        Reject(head + Row("lowercase"));
        Reject(head + Row("VALID_ID").Replace("example_icon.jpg", "../escape.jpg"));
        Reject(head + Row("VALID_ID").Replace(new string('A', 64), "abc"));
        Reject(head + Row("VALID_ID", "6,4,3,4,4"));
        Reject(head + Row("VALID_ID", "-1,4,3,4,4"));
        Reject(head + Row("VALID_ID", capstone: "MAYBE"));
        Reject(head + Row("VALID_ID", rarityOverride: "legendary"));
        Reject("incorrect_header\n" + Row("VALID_ID"));
        Reject(head + Row("VALID_ID") + ",EXTRA");
        Reject(head);
        Reject("");
        Reject(new string('A', 262_145));
    }

    [Fact]
    public void AllowsUtf8BomCommentsAndCrLfButRejectsUnknownOverride()
    {
        string text = "\uFEFF# Owner notes\r\n" + CuratedAchievementCatalog.Header +
            "\r\n\r\n" + Row("SAMPLE_A", rarityOverride: "RARE") + "\r\n";
        var catalog = CuratedAchievementCatalog.Parse(text);
        Assert.Single(catalog.Entries);
        Assert.Equal(CuratedRarity.Rare, catalog.Find("SAMPLE_A")!.Rarity);
    }

    [Fact]
    public void ExternalEditableFileTakesPrecedenceAndMissingExplicitFileIsNotCreated()
    {
        var temporary = Path.Combine(Path.GetTempPath(), "rasc-catalog-" +
            Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            Assert.Throws<FileNotFoundException>(() =>
                CuratedAchievementCatalog.LoadDefault(temporary));
            Assert.False(File.Exists(temporary));
            File.WriteAllText(temporary, CuratedAchievementCatalog.Header + "\n" +
                Row("LOCAL_OVERRIDE", rarityOverride: "NORMAL"));
            var modified = CuratedAchievementCatalog.LoadDefault(temporary);
            Assert.Single(modified.Entries);
            Assert.Equal(temporary, modified.Source);
            Assert.Equal(CuratedRarity.Normal, modified.Find("LOCAL_OVERRIDE")!.Rarity);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    [Fact]
    public void EmbeddedFallbackContainsTheFullOwnerAcceptedCatalog()
    {
        using var stream = typeof(CuratedAchievementCatalog).Assembly.GetManifestResourceStream(
            "Rasc.Core.Data.achievement-definitions.v0.2.csv");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var embedded = CuratedAchievementCatalog.Parse(reader.ReadToEnd(), "embedded-test");
        Assert.Equal(167, embedded.Entries.Count);
        Assert.Equal(63, embedded.Entries.Count(e => e.Rarity == CuratedRarity.Rare));
        Assert.Equal(104, embedded.Entries.Count(e => e.Rarity == CuratedRarity.Normal));
    }

    [Fact]
    public void DoesNotCarryAnyBundledOriginalValveDisplayTextOrAwardLogic()
    {
        var catalog = CuratedAchievementCatalog.LoadDefault();
        Assert.All(catalog.Entries, definition =>
        {
            Assert.Matches("^[a-z0-9_]+\\.jpg$", definition.IconFilename);
            Assert.Matches("^[0-9A-F]{64}$", definition.IconSha256);
            Assert.Matches("^[A-Z][A-Z0-9_]+$", definition.ApiId);
        });
    }
}
