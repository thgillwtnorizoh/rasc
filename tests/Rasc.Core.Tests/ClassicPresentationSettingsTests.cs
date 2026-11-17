using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ClassicPresentationSettingsTests
{
    [Fact]
    public void DefaultsAreAutoSyncAndAutoScale()
    {
        var value = new ClassicPresentationSettings();
        Assert.Equal(0, value.AnimationFps);
        Assert.Equal(0, value.PopupSizePercent);
    }

    [Theory]
    [InlineData(-1, -1)]
    [InlineData(999, 999)]
    [InlineData(241, 133)]
    public void InvalidPresetsIndependentlyFallBackToAuto(int fps, int size)
    {
        var value = new ClassicPresentationSettings(fps, size).Validated();
        Assert.Equal(0, value.AnimationFps);
        Assert.Equal(0, value.PopupSizePercent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(165)]
    [InlineData(240)]
    public void EverySupportedFrameRateIsAccepted(int fps)
    {
        Assert.Equal(fps, new ClassicPresentationSettings(fps, 0).Validated().AnimationFps);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(125)]
    [InlineData(150)]
    [InlineData(175)]
    [InlineData(200)]
    [InlineData(250)]
    public void EverySupportedScaleIsAccepted(int size)
    {
        Assert.Equal(size, new ClassicPresentationSettings(0, size).Validated().PopupSizePercent);
    }

    [Fact]
    public void SettingsRoundTripWithoutTouchingAchievementFiles()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-settings-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(folder, "appearance.json");
            var store = new ClassicPresentationSettingsStore(path);
            Assert.Equal(new ClassicPresentationSettings(), store.Load());
            Assert.True(store.TrySave(new ClassicPresentationSettings(120, 150)));
            Assert.Equal(new ClassicPresentationSettings(120, 150), store.Load());
            Assert.True(store.TrySave(new ClassicPresentationSettings(30, 0)));
            Assert.Equal(new ClassicPresentationSettings(30, 0), store.Load());
            Assert.Empty(Directory.EnumerateFiles(folder, "*.tmp"));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void CorruptAndOutOfRangeFilesSafelyFallback()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-corrupt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "appearance.json");
            var store = new ClassicPresentationSettingsStore(path);
            File.WriteAllText(path, "{broken");
            Assert.Equal(new ClassicPresentationSettings(), store.Load());
            File.WriteAllText(path, """{"AnimationFps":60,"PopupSizePercent":999}""");
            Assert.Equal(new ClassicPresentationSettings(60, 0), store.Load());
            File.WriteAllText(path, new string('x', 65537));
            Assert.Equal(new ClassicPresentationSettings(), store.Load());
        }
        finally { Directory.Delete(folder, true); }
    }

    [Theory]
    [InlineData(1080.0, 1.0)]
    [InlineData(1440.0, 1.3333333333333333)]
    [InlineData(2160.0, 2.0)]
    [InlineData(864.0, 1.0)] // 1080px screen at 125% DPI
    [InlineData(1152.0, 1.0666666666666667)] // 1440px screen at 125% DPI
    public void AutoSizeFollowsResolutionWithoutDoubleCountingOsDpi(double screenDip, double expected)
    {
        Assert.Equal(expected,
            ClassicPopupScale.Resolve(screenDip, 3000, 3000, 0), 8);
    }

    [Fact]
    public void AutoAt1440And125PercentDpiRendersApproximately320PhysicalPixelsWide()
    {
        var s = ClassicPopupScale.Resolve(1152, 2500, 1800, 0);
        Assert.Equal(320, ClassicToastScheduler.CardWidth * s * 1.25, 5);
    }

    [Fact]
    public void ManualScaleOverridesAutoAndIsClampedToDesktop()
    {
        Assert.Equal(1.5, ClassicPopupScale.Resolve(1440, 2560, 1380, 150), 5);
        Assert.Equal(1.0, ClassicPopupScale.Resolve(1440, 2560, 1380, 100), 5);
        Assert.Equal(1.0, ClassicPopupScale.Resolve(1440, 240, 282, 250), 5);
        Assert.Equal(0.5, ClassicPopupScale.Resolve(1080, 120, 141, 250), 5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidScreenMetricsNeverProduceNonFiniteScale(double screen)
    {
        Assert.Equal(1, ClassicPopupScale.Resolve(screen, 1920, 1080, 0));
    }

        [Theory]
    [InlineData(15)]
    [InlineData(75)]
    [InlineData(100)]
    [InlineData(123)]
    [InlineData(239)]
    public void CustomIntegerFrameRatesAreAllowed(int fps)
    {
        Assert.True(ClassicPresentationSettings.IsValidFps(fps));
        Assert.Equal(fps, new ClassicPresentationSettings(fps, 0).Validated().AnimationFps);
    }

    [Fact]
    public void CustomFpsPersistsAcrossRestart()
    {
        var folder = Path.Combine(Path.GetTempPath(), "rasc-custom-fps-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(folder, "appearance.json");
            var store = new ClassicPresentationSettingsStore(path);
            Assert.True(store.TrySave(new ClassicPresentationSettings(123, 0)));
            Assert.Equal(123, new ClassicPresentationSettingsStore(path).Load().AnimationFps);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void PhaseAccumulationAvoidsHalfRefreshRateWhenCapping123HzTo120()
    {
        var limiter = new ClassicAnimationFrameGate();
        var accepted = 0;
        for (var ms = 0.0; ms <= 1000; ms += 1000.0 / 123.3)
        {
            if (limiter.ShouldApply(TimeSpan.FromTicks(
                    (long)(ms * TimeSpan.TicksPerMillisecond)), 120))
                accepted++;
        }
        Assert.InRange(accepted, 115, 121);
    }

    [Fact]
    public void GateCapsVisualUpdatesWithoutChangingStopwatchTimeline()
    {
        var limiter = new ClassicAnimationFrameGate();
        Assert.True(limiter.ShouldApply(TimeSpan.Zero, 60));
        Assert.False(limiter.ShouldApply(TimeSpan.FromMilliseconds(8), 60));
        Assert.True(limiter.ShouldApply(TimeSpan.FromMilliseconds(17), 60));
        Assert.False(limiter.ShouldApply(TimeSpan.FromMilliseconds(25), 60));
        Assert.True(limiter.ShouldApply(TimeSpan.FromMilliseconds(34), 60));
        limiter.Reset();
        Assert.True(limiter.ShouldApply(TimeSpan.FromMilliseconds(35), 60));
    }

    [Fact]
    public void GateAllowsEveryRenderingOpportunityWhenUncapped()
    {
        var limiter = new ClassicAnimationFrameGate();
        for (var i = 0; i < 100; i++)
            Assert.True(limiter.ShouldApply(TimeSpan.FromMilliseconds(i), 0));
    }

    [Fact]
    public void GateNeverExceedsRequestedCapForHighFrequencyCallbacks()
    {
        foreach (var fps in new[] { 30, 60, 90, 120, 144, 165, 240 })
        {
            var limiter = new ClassicAnimationFrameGate();
            var accepted = new List<TimeSpan>();
            for (var ticks = 0L; ticks <= TimeSpan.TicksPerSecond; ticks += 40000)
            {
                var instant = TimeSpan.FromTicks(ticks);
                if (limiter.ShouldApply(instant, fps))
                    accepted.Add(instant);
            }
            for (var i = 1; i < accepted.Count; i++)
                Assert.True(accepted[i] > accepted[i - 1]);
            // A phase-accumulated target caps average updates, while
            // allowing slightly uneven *individual* frame intervals.
            Assert.InRange(accepted.Count, 1, fps + 1);
        }
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0, 1.3333333333333333)]
    [InlineData(1.25, 1.0666666666666667)]
    [InlineData(1.25, 1.5)]
    [InlineData(1.5, 2.0)]
    [InlineData(1.75, 2.5)]
    public void ScaledThreeCardStackRemainsPixelContiguous(double windowsDpi, double visualScale)
    {
        var scheduler = new ClassicToastScheduler();
        for (var i = 1; i <= 3; i++)
            Assert.True(scheduler.Enqueue(new UnlockEvent(
                $"scaled-{i}", "test", $"ACH_{i}", (uint)i,
                DateTimeOffset.FromUnixTimeSeconds(i + 100))));
        scheduler.Advance(TimeSpan.Zero);
        var frames = scheduler.Advance(TimeSpan.FromMilliseconds(1350))
            .OrderBy(f => f.Y).ToArray();
        Assert.Equal(3, frames.Length);
        var effectiveDpi = windowsDpi * visualScale;
        for (var i = 0; i < 2; i++)
        {
            var upper = ClassicToastPixelGrid.Map(frames[i], effectiveDpi);
            var lower = ClassicToastPixelGrid.Map(frames[i + 1], effectiveDpi);
            Assert.Equal(upper.VisibleBottom, lower.VisibleTop);
            Assert.Equal(upper.VisibleBottom, lower.OriginY);
        }
    }

    [Fact]
    public void GateRejectsInvalidTargetsAndClockReversal()
    {
        var limiter = new ClassicAnimationFrameGate();
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.ShouldApply(TimeSpan.Zero, 241));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            limiter.ShouldApply(TimeSpan.FromMilliseconds(-1), 60));
        Assert.True(limiter.ShouldApply(TimeSpan.FromSeconds(2), 60));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            limiter.ShouldApply(TimeSpan.FromSeconds(1), 60));
    }
}
