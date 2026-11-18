using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ModernStackGeometryTests
{
    private static UnlockEvent E(int n) =>
        new($"stack-{n}", "fixture", $"STK-{n}", 0, DateTimeOffset.UnixEpoch);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CardsShareOneViewportAndHaveZeroIntercardDistanceAtRest(int count)
    {
        var scheduler = new ModernToastScheduler();
        for (var i = 0; i < count; i++) scheduler.Enqueue(E(i));
        scheduler.Advance(TimeSpan.Zero);
        var frames = scheduler.Advance(TimeSpan.FromMilliseconds(301))
            .OrderBy(ModernStackGeometry.TopInViewport).ToArray();

        Assert.Equal(count, frames.Length);
        Assert.Equal(210, ModernStackGeometry.ViewportHeight);
        var expectedStart = ModernStackGeometry.ViewportHeight -
            count * ModernToastScheduler.CardHeight;
        Assert.Equal(expectedStart, ModernStackGeometry.TopInViewport(frames[0]));
        for (var i = 1; i < count; i++)
        {
            var previousBottom = ModernStackGeometry.TopInViewport(frames[i - 1])
                + ModernToastScheduler.CardHeight;
            var currentTop = ModernStackGeometry.TopInViewport(frames[i]);
            Assert.Equal(previousBottom, currentTop);
        }
        Assert.Equal(ModernStackGeometry.ViewportHeight,
            ModernStackGeometry.TopInViewport(frames[^1]) +
            ModernToastScheduler.CardHeight);
    }

    [Theory]
    [InlineData(0.75)]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void CardsHaveIdenticalSharedBoundaryAfterUniformScaling(double scale)
    {
        var scheduler = new ModernToastScheduler();
        for (var i = 0; i < 3; i++) scheduler.Enqueue(E(i));
        scheduler.Advance(TimeSpan.Zero);
        var frames = scheduler.Advance(TimeSpan.FromMilliseconds(300))
            .OrderBy(ModernStackGeometry.TopInViewport).ToArray();
        for (int i = 1; i < frames.Length; i++)
        {
            var priorBottom = (ModernStackGeometry.TopInViewport(frames[i - 1]) +
                ModernToastScheduler.CardHeight) * scale;
            var nextTop = ModernStackGeometry.TopInViewport(frames[i]) * scale;
            Assert.Equal(priorBottom, nextTop, precision: 8);
        }
    }

    [Fact]
    public void AllCardsEnterFromExactSameSharedBottomOrigin()
    {
        var scheduler = new ModernToastScheduler();
        for (int i = 0; i < 3; i++) scheduler.Enqueue(E(i));
        var frames = scheduler.Advance(TimeSpan.Zero);
        Assert.All(frames, f =>
            Assert.Equal(ModernStackGeometry.ViewportHeight,
                ModernStackGeometry.TopInViewport(f)));
    }

    [Fact]
    public void ExistingWaveTimingAndStackingRemainUnchanged()
    {
        var s = new ModernToastScheduler();
        s.Enqueue(E(1));
        s.Enqueue(E(2));
        s.Enqueue(E(3));
        var at0 = s.Advance(TimeSpan.Zero);
        Assert.All(at0, f => Assert.Equal(ModernToastStage.Entering, f.Stage));
        var resting = s.Advance(TimeSpan.FromMilliseconds(301));
        Assert.Equal([0d, 70d, 140d], resting
            .Select(ModernStackGeometry.TopInViewport)
            .OrderBy(x => x).ToArray());
        var exiting = s.Advance(TimeSpan.FromMilliseconds(5450));
        Assert.All(exiting, f => Assert.Equal(ModernToastStage.Exiting, f.Stage));
        Assert.Empty(s.Advance(TimeSpan.FromMilliseconds(5600)));
    }
}
