using System.Text;
using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class CsgoAchievementLocalizationTests
{
    private const string English = """
        // Synthetic structure modeled on CS:GO's resource/csgo_english.txt (no Valve text)
        "lang"
        {
            "Language" "English"
            "Tokens"
            {
                "KILL_ENEMY_HIGH_NAME" "Example High Kill Award"
                "KILL_ENEMY_HIGH_DESC" "An example cumulative requirement."
                "KILL_WITH_OWN_GUN_NAME" "Example Recovered Weapon"
                "KILL_WITH_OWN_GUN_DESC" "An example situation-specific requirement."
                "UNSTOPPABLE_FORCE_NAME" "Example Multi-Kill"
                "UNSTOPPABLE_FORCE_DESC" "An example single-round requirement."
                "IMMOVABLE_OBJECT_NAME" "Example Survivor"
                "IMMOVABLE_OBJECT_DESC" "An example survivor requirement."
                "WIN_ROUNDS_WITHOUT_BUYING_NAME" "Example Streak"
                "WIN_ROUNDS_WITHOUT_BUYING_DESC" "An example streak requirement."
                "BOMB_DEFUSE_NEEDED_KIT_NAME" "Example Cut Content"
                "BOMB_DEFUSE_NEEDED_KIT_DESC" "This is outside the reviewed 167-entry catalog."
                "PARTIAL_NAME" "No matching description"
            }
        }
        """;

    private const string Spanish = """
        "lang"
        {
            "Language" "Spanish"
            "Tokens"
            {
                "KILL_ENEMY_HIGH_NAME" "Prueba de logro"
                "KILL_ENEMY_HIGH_DESC" "Prueba de requisito"
                "KILL_WITH_OWN_GUN_NAME" "Entrada incompleta"
            }
        }
        """;

    private sealed class Sandbox : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(),
            "rasc-localization-" + Guid.NewGuid().ToString("N"));
        public string ContentRoot => Path.Combine(Root, "csgo");
        public string Resource => Path.Combine(ContentRoot, "resource");
        public Sandbox() => Directory.CreateDirectory(Resource);
        public void WriteEnglish(byte[] bytes) =>
            File.WriteAllBytes(Path.Combine(Resource, "csgo_english.txt"), bytes);
        public void WriteOther(string language, byte[] bytes) =>
            File.WriteAllBytes(Path.Combine(Resource, "csgo_" + language + ".txt"), bytes);
        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }

    [Fact]
    public void JoinsAll167WithoutPromotingCutTextOrInventingMissingStrings()
    {
        var definitions = CuratedAchievementCatalog.LoadDefault();
        var language = CsgoAchievementLocalization.Parse(English);
        var all = CsgoAchievementDefinitionJoin.Join(definitions, language);
        Assert.Equal(167, all.Count);
        Assert.Equal(definitions.Entries.Count, all.Select(x => x.Definition.ApiId).Distinct().Count());
        Assert.Equal("Example High Kill Award",
            all.Single(x => x.Definition.ApiId == "KILL_ENEMY_HIGH").Name);
        Assert.Equal(CuratedRarity.Rare,
            all.Single(x => x.Definition.ApiId == "KILL_ENEMY_HIGH").Definition.Rarity);
        Assert.Null(all.Single(x => x.Definition.ApiId == "AVENGE_FRIEND").Name);
        Assert.False(all.Single(x => x.Definition.ApiId == "AVENGE_FRIEND").HasLocalText);
        Assert.True(all.Single(x => x.Definition.ApiId == "IMMOVABLE_OBJECT").HasLocalText);
        Assert.NotNull(language.Find("BOMB_DEFUSE_NEEDED_KIT"));
        Assert.DoesNotContain(all, x => x.Definition.ApiId == "BOMB_DEFUSE_NEEDED_KIT");
        Assert.Null(language.Find("PARTIAL"));
        Assert.Null(language.Find("UNKNOWN_ID"));
        Assert.Null(language.Find(null));
    }

    [Fact]
    public void PreferredLocaleUsesOnlyCompletePairsAndFallsBackToEnglish()
    {
        var localized = CsgoAchievementLocalization.Parse(
            English, Spanish, "spanish", "local-english", "local-spanish");
        var translated = localized.Find("KILL_ENEMY_HIGH")!;
        Assert.Equal("Prueba de logro", translated.Name);
        Assert.Equal("Prueba de requisito", translated.Description);
        Assert.Equal("spanish", translated.Language);
        var partial = localized.Find("KILL_WITH_OWN_GUN")!;
        Assert.Equal("Example Recovered Weapon", partial.Name);
        Assert.Equal("english", partial.Language);
        Assert.Equal("local-spanish", localized.PreferredSource);
        Assert.False(localized.PreferredFileMissing);
    }

    [Fact]
    public void MissingPreferredLocaleIsExplicitAndEnglishIsStillAvailable()
    {
        var loaded = CsgoAchievementLocalization.Parse(English, language: "spanish");
        Assert.True(loaded.PreferredFileMissing);
        Assert.Equal("english", loaded.Find("UNSTOPPABLE_FORCE")!.Language);
    }

    [Theory]
    [InlineData("utf16le")]
    [InlineData("utf16be")]
    [InlineData("utf8")]
    public void LoadsGameLocalFileReadOnlyWithSupportedEncodings(string encoding)
    {
        using var t = new Sandbox();
        var bytes = encoding switch
        {
            "utf16le" => new UnicodeEncoding(false, true, true).GetPreamble()
                .Concat(new UnicodeEncoding(false, false, true).GetBytes(English)).ToArray(),
            "utf16be" => new UnicodeEncoding(true, true, true).GetPreamble()
                .Concat(new UnicodeEncoding(true, false, true).GetBytes(English)).ToArray(),
            _ => new UTF8Encoding(true, true).GetPreamble()
                .Concat(new UTF8Encoding(false, true).GetBytes(English)).ToArray()
        };
        t.WriteEnglish(bytes);
        var first = CsgoAchievementLocalization.LoadFromGameRoot(t.Root);
        var second = CsgoAchievementLocalization.LoadFromGameRoot(t.ContentRoot);
        Assert.Equal(ReadStatus.Valid, first.Status);
        Assert.Equal(ReadStatus.Valid, second.Status);
        Assert.Equal("Example Multi-Kill", first.Catalog!.Find("UNSTOPPABLE_FORCE")!.Name);
        Assert.Equal(first.Catalog!.Count, second.Catalog!.Count);
        Assert.True(File.ReadAllBytes(Path.Combine(t.Resource, "csgo_english.txt")).SequenceEqual(bytes));
    }

    [Fact]
    public void MissingAndCorruptSourcesNeverBecomeEmptyOrFalseNames()
    {
        using var t = new Sandbox();
        var missing = CsgoAchievementLocalization.LoadFromGameRoot(t.Root);
        Assert.Equal(ReadStatus.Missing, missing.Status);
        Assert.Null(missing.Catalog);
        t.WriteEnglish([0xff, 0xfe, 0x41]); // incomplete UTF-16LE scalar
        var invalid = CsgoAchievementLocalization.LoadFromGameRoot(t.Root);
        Assert.Equal(ReadStatus.Invalid, invalid.Status);
        Assert.Null(invalid.Catalog);
        t.WriteEnglish(Encoding.UTF8.GetBytes("\"lang\" {\"Language\" \"English\"}"));
        Assert.Equal(ReadStatus.Invalid,
            CsgoAchievementLocalization.LoadFromGameRoot(t.Root).Status);
    }

    [Fact]
    public void OptionalMissingLanguageFallsBackButPresentMalformedLanguageIsInvalid()
    {
        using var t = new Sandbox();
        t.WriteEnglish(new UTF8Encoding(false, true).GetBytes(English));
        var fallback = CsgoAchievementLocalization.LoadFromGameRoot(t.Root, "spanish");
        Assert.Equal(ReadStatus.Valid, fallback.Status);
        Assert.True(fallback.Catalog!.PreferredFileMissing);
        t.WriteOther("spanish", Encoding.UTF8.GetBytes("\"lang\" {\"Tokens\" {\"MISSING_DESC_NAME\" \"Oops\""));
        var damaged = CsgoAchievementLocalization.LoadFromGameRoot(t.Root, "spanish");
        Assert.Equal(ReadStatus.Invalid, damaged.Status);
        Assert.Null(damaged.Catalog);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("spanish.txt")]
    [InlineData("SPANISH")]
    [InlineData("a")]
    public void RejectsPathTraversalOrUnsupportedLocaleIdentifiers(string language)
    {
        Assert.Throws<ArgumentException>(() =>
            CsgoAchievementLocalization.Parse(English, language: language));
        Assert.Throws<ArgumentException>(() =>
            CsgoAchievementLocalization.LoadFromGameRoot("C:\\games\\csgo", language));
    }

    [Fact]
    public void IgnoresUnrelatedTokensCommentsAndConditionalAnnotations()
    {
        var input = """
            #base "custom.txt"
            "lang"
            {
                "Tokens"
                {
                    /* source-style block comment */
                    "KILL_ENEMY_HIGH_NAME" "Example // not a comment"
                    "KILL_ENEMY_HIGH_DESC" "Example with \"quote\" and slash \\ mark"
                    "KILL_ENEMY_HIGH_DESC" "Example with \"quote\" and slash \\ mark"
                    "DISABLED_NAME" "unrelated" [!$XBOX]
                    "inner" { "NESTED_NAME" "Should not be loaded" }
                }
            }
            """;
        var parsed = CsgoAchievementLocalization.Parse(input);
        Assert.Equal("Example // not a comment", parsed.Find("KILL_ENEMY_HIGH")!.Name);
        Assert.Equal("Example with \"quote\" and slash \\ mark",
            parsed.Find("KILL_ENEMY_HIGH")!.Description);
        Assert.Null(parsed.Find("NESTED"));
        Assert.Null(parsed.Find("DISABLED"));
    }

    [Fact]
    public void ValidatesDuplicateConflictsUnterminatedValuesAndMissingSection()
    {
        static void Reject(string input) =>
            Assert.Throws<FormatException>(() => CsgoAchievementLocalization.Parse(input));
        Reject("\"lang\" {\"Tokens\" {\"KILL_ENEMY_HIGH_NAME\" \"A\" \"KILL_ENEMY_HIGH_NAME\" \"B\"}}");
        Reject("\"lang\" {\"Tokens\" {\"KILL_ENEMY_HIGH_NAME\" \"No close}");
        Reject("\"lang\" {\"Tokens\" {\"KILL_ENEMY_HIGH_NAME\" \"A\" ");
        Reject("\"lang\" {\"Language\" \"English\"}");
        Reject("\"lang\" {\"Tokens\" { /* no end ");
        Reject(new string('x', 4_194_305));
    }

    [Fact]
    public void GcFixtureIsAuthoritativeOnlyForAwardsItActuallyContains()
    {
        // Work 1 synthetic GT-610 v1 fixture, NOT an observation of a real user.
        // Unlock times here are synthetic Unix seconds.
        const string gc = """
            "version" "1"
            "integer_stats" { "total_kills" "10000" }
            "float_stats" {}
            "achievements"
            {
                "UNSTOPPABLE_FORCE" "1790000000"
                "KILL_WITH_OWN_GUN" "1790000060"
                "NOT_IN_CATALOG" "1790000100"
            }
            """;
        var snapshot = GcKeyValuesParser.Parse(gc);
        var definitions = CuratedAchievementCatalog.LoadDefault();
        var joined = CsgoAchievementDefinitionJoin.Join(
            definitions, CsgoAchievementLocalization.Parse(English));
        Assert.Equal(3, snapshot.Achievements.Count);
        Assert.Equal(2, snapshot.Achievements.Keys.Count(key => definitions.Find(key) is not null));
        Assert.Equal((uint)1790000000, snapshot.Achievements["UNSTOPPABLE_FORCE"]);
        Assert.DoesNotContain("KILL_ENEMY_HIGH", snapshot.Achievements.Keys);
        // total_kills=10000 does NOT imply KILL_ENEMY_HIGH was awarded.
        Assert.Equal(10000, snapshot.IntegerStats["total_kills"]);
        Assert.Contains(joined, x => x.Definition.ApiId == "KILL_ENEMY_HIGH" && x.HasLocalText);
        Assert.DoesNotContain(joined, x => x.Definition.ApiId == "NOT_IN_CATALOG");
    }
}
