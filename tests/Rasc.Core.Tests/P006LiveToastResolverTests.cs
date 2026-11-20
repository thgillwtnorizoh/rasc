using System.Security.Cryptography;
using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class P006LiveToastResolverTests
{
    private const string English = """
        "lang" { "Tokens" {
            "UNSTOPPABLE_FORCE_NAME" "Synthetic Four Kills"
            "UNSTOPPABLE_FORCE_DESC" "Four in one round, synthetic fixture."
            "KILL_ENEMY_HIGH_NAME" "Synthetic Grinding Award"
            "KILL_ENEMY_HIGH_DESC" "A long-term requirement, synthetic fixture."
        }}
        """;

    [Fact]
    public void ApprovedCuratedRarityAndCoherentLocalTextAreUsedForRealGcId()
    {
        var catalog = CuratedAchievementCatalog.LoadDefault();
        var language = CsgoAchievementLocalization.Parse(English);
        var actual = AchievementToastResolver.Resolve(
            "UNSTOPPABLE_FORCE", catalog, language, null);
        Assert.Equal("Synthetic Four Kills", actual.Name);
        Assert.Equal("Four in one round, synthetic fixture.", actual.Description);
        Assert.True(actual.IsRare);
        Assert.Null(actual.IconJpeg);
        var longTerm = AchievementToastResolver.Resolve(
            "KILL_ENEMY_HIGH", catalog, language, null);
        Assert.True(longTerm.IsRare); // series-capstone exception at 14/25
    }

    [Fact]
    public void UnknownIdNeverGetsFictionalRarityOrDescription()
    {
        var catalog = CuratedAchievementCatalog.LoadDefault();
        var localized = CsgoAchievementLocalization.Parse(English);
        var unknown = AchievementToastResolver.Resolve(
            "NOT_IN_HISTORICAL_CATALOG", catalog, localized, null);
        Assert.Equal("NOT_IN_HISTORICAL_CATALOG", unknown.Name);
        Assert.Equal("", unknown.Description);
        Assert.False(unknown.IsRare);
        Assert.Null(unknown.IconJpeg);
    }

    [Fact]
    public void MissingOriginalTextRetainsIdAndApprovedClassification()
    {
        var result = AchievementToastResolver.Resolve(
            "UNSTOPPABLE_FORCE", CuratedAchievementCatalog.LoadDefault(),
            null, null);
        Assert.Equal("UNSTOPPABLE_FORCE", result.Name);
        Assert.Equal("", result.Description);
        Assert.True(result.IsRare);
    }

    [Fact]
    public void OriginalJpegMustMatchExpectedHashAndUsesPanoramaThenScaleform()
    {
        string root = Path.Combine(Path.GetTempPath(),
            "rasc-jpeg-" + Guid.NewGuid().ToString("N"));
        string panorama = Path.Combine(root, "csgo", "materials", "panorama",
            "images", "icons", "achievements");
        string scaleform = Path.Combine(root, "csgo", "resource", "flash",
            "images", "achievements");
        byte[] exact = [0xff, 0xd8, 0x01, 0x02, 0xff, 0xd9];
        byte[] wrong = [0xff, 0xd8, 0x03, 0x04, 0xff, 0xd9];
        var expected = Convert.ToHexString(SHA256.HashData(exact));
        var definition = new CuratedAchievementDefinition(
            "TEST_ICON", "test_icon.jpg", expected,
            3, 4, 3, 4, 4, false, null);
        try
        {
            Directory.CreateDirectory(panorama);
            Directory.CreateDirectory(scaleform);
            Assert.Null(CsgoAchievementIconReader.ReadVerified(root, definition));
            File.WriteAllBytes(Path.Combine(panorama, "test_icon.jpg"), exact);
            Assert.Equal(exact, CsgoAchievementIconReader.ReadVerified(root, definition));
            File.WriteAllBytes(Path.Combine(panorama, "test_icon.jpg"), wrong);
            Assert.Null(CsgoAchievementIconReader.ReadVerified(root, definition));
            File.WriteAllBytes(Path.Combine(scaleform, "test_icon.jpg"), exact);
            Assert.Equal(exact, CsgoAchievementIconReader.ReadVerified(root, definition));
            Assert.Equal(exact, CsgoAchievementIconReader.ReadVerified(
                Path.Combine(root, "csgo"), definition));
            Assert.Equal(wrong, File.ReadAllBytes(Path.Combine(panorama, "test_icon.jpg")));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void OversizedOrInvalidArtworkNeverBecomesAnIcon()
    {
        string root = Path.Combine(Path.GetTempPath(),
            "rasc-bad-jpeg-" + Guid.NewGuid().ToString("N"));
        string folder = Path.Combine(root, "csgo", "materials", "panorama",
            "images", "icons", "achievements");
        var exact = new byte[] { 0xff, 0xd8, 0x03, 0xff, 0xd9 };
        var def = new CuratedAchievementDefinition(
            "TEST_BAD", "bad.jpg", Convert.ToHexString(SHA256.HashData(exact)),
            0, 0, 0, 0, 0, false, null);
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "bad.jpg"), exact);
            Assert.Equal(exact, CsgoAchievementIconReader.ReadVerified(root, def));
            File.WriteAllBytes(Path.Combine(folder, "bad.jpg"),
                new byte[CsgoAchievementIconReader.MaxJpegBytes + 1]);
            Assert.Null(CsgoAchievementIconReader.ReadVerified(root, def));
            File.WriteAllBytes(Path.Combine(folder, "bad.jpg"), [0x00, 0xd8, 0x03, 0xff, 0xd9]);
            Assert.Null(CsgoAchievementIconReader.ReadVerified(root, def));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }
}
