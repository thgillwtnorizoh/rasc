using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public class ParserTests
{
    public const string Baseline = """
        "version" "1"
        "integer_stats"
        {
          "total_kills" "250"
          "total_damage_done" "25601"
        }
        "float_stats"
        {
          "fractional_example" "1065353216"
        }
        "achievements"
        {
          "KILL_ENEMY_LAST_BULLET" "1794738859"
        }
        """;

    [Fact] public void ReadsGt610UnsignedAchievementAndFloatBits()
    {
        var snap = GcKeyValuesParser.Parse(Baseline);
        Assert.Equal(250, snap.IntegerStats["total_kills"]);
        Assert.Equal(25601, snap.IntegerStats["total_damage_done"]);
        Assert.Equal(1f, snap.FloatStats["fractional_example"]);
        Assert.Equal((uint)1794738859, snap.Achievements["KILL_ENEMY_LAST_BULLET"]);
    }

    [Fact] public void AcceptsBomAndLineComments()
    {
        var result = GcKeyValuesParser.Parse("\uFEFF// saved\n" + Baseline);
        Assert.Single(result.Achievements);
    }

    [Fact] public void EmptyOptionalSectionsAreAllowed()
    {
        var snap = GcKeyValuesParser.Parse("\"version\" \"1\"");
        Assert.Empty(snap.Achievements);
        Assert.Empty(snap.IntegerStats);
        Assert.Empty(snap.FloatStats);
    }

    [Theory]
    [InlineData("\"version\" \"2\"")]
    [InlineData("\"version\" \"x\"")]
    [InlineData("\"achievements\" {\"A\" \"1\"}")]
    [InlineData("\"version\" \"1\" \"version\" \"1\"")]
    [InlineData("\"version\" \"1\" \"achievements\" {\"A\" \"1\" \"A\" \"2\"}")]
    [InlineData("\"version\" \"1\" \"achievements\" {\"A\" \"-1\"}")]
    [InlineData("\"version\" \"1\" \"achievements\" {\"A\" \"4294967296\"}")]
    [InlineData("\"version\" \"1\" \"achievements\" {\"A\" \"1\"")]
    [InlineData("\"version\" \"1\" }")]
    public void RejectsMalformedOrAmbiguousSnapshots(string text) =>
        Assert.Throws<FormatException>(() => GcKeyValuesParser.Parse(text));

    [Fact] public void RejectsInvalidNames()
    {
        var tooLong = new string('x', 128);
        Assert.Throws<FormatException>(() =>
            GcKeyValuesParser.Parse($"\"version\" \"1\" \"achievements\" {{\"{tooLong}\" \"7\"}}"));
    }
}
