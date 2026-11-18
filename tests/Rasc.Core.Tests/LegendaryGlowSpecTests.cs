using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class LegendaryGlowSpecTests
{
    [Fact]
    public void CodePenReferenceHas48IconAndThree10UnitNestedExpansions()
    {
        Assert.Equal(48, LegendaryGlowSpec.IconSize);
        Assert.Equal(68, LegendaryGlowSpec.OuterSize);
        Assert.Equal(88, LegendaryGlowSpec.MiddleSize);
        Assert.Equal(108, LegendaryGlowSpec.LightSize);
        Assert.Equal(10, LegendaryGlowSpec.MaskPadding);
    }

    [Fact]
    public void GoldLegendaryRadialStopsMatchReviewedCodePen()
    {
        LegendaryGlowSpec.Validate();
        var s = LegendaryGlowSpec.RadialStops;
        Assert.Equal(new double[]{0,.06,.10,.26,.35,.40,.60,.82,1},
            s.Select(x=>x.Offset).ToArray());
        Assert.Equal(new byte[]{45,0,45,255,45,255,45,255,45},
            s.Select(x=>x.Alpha).ToArray());
        Assert.Equal((byte)201, s[0].Green);
        Assert.Equal((byte)109, s[0].Blue);
        Assert.Equal((byte)184, s[3].Green);
        Assert.Equal((byte)78, s[3].Blue);
        Assert.All(s, stop => Assert.Equal((byte)255, stop.Red));
    }

    [Fact]
    public void MaskAndLightRotateInOppositeDirectionsAtSourcePeriods()
    {
        Assert.Equal(TimeSpan.FromSeconds(18), LegendaryGlowSpec.MaskReverse);
        Assert.Equal(TimeSpan.FromSeconds(6), LegendaryGlowSpec.LightForward);
        Assert.Equal(.6, LegendaryGlowSpec.NearShadowOpacity);
        Assert.Equal(.4, LegendaryGlowSpec.FarShadowOpacity);
        Assert.Equal(2, LegendaryGlowSpec.NearShadowBlur);
        Assert.Equal(16, LegendaryGlowSpec.FarShadowBlur);
        Assert.Equal(1, LegendaryGlowSpec.ShadowSpread);
        Assert.Equal(0.5, LegendaryGlowSpec.CssBlurSigmaFactor);
    }
}
