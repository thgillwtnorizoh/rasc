using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ModernStackDecorationLayoutTests
{
    private static UnlockEvent E(int n) => new($"background-{n}",
        "fixture", $"BACKGROUND_{n}", 0, DateTimeOffset.UnixEpoch);

    [Theory]
    [InlineData(1, 140d, 70d)]
    [InlineData(2, 70d, 140d)]
    [InlineData(3, 0d, 210d)]
    public void SettledWavePaintsOneContinuousBackgroundNotSeparateCards(
        int count, double top, double height)
    {
        var scheduler = new ModernToastScheduler();
        for (var i = 0; i < count; i++) scheduler.Enqueue(E(i));
        scheduler.Advance(TimeSpan.Zero);
        var frames = scheduler.Advance(TimeSpan.FromMilliseconds(301));
        var layout = ModernStackDecorationLayout.ForFrames(frames);
        Assert.Equal(top, layout.Top);
        Assert.Equal(height, layout.Height);
        Assert.Equal(ModernStackGeometry.ViewportHeight, layout.Bottom);
        Assert.Equal((height - ModernWatermarkLayout.LogoHeight) / 2,
            layout.WatermarkTop);
        // Every card is drawn over this SAME background brush; none introduces
        // an independent clipped gradient boundary.
        Assert.All(frames, f =>
        {
            var cardTop = ModernStackGeometry.TopInViewport(f);
            Assert.InRange(cardTop, layout.Top, layout.Bottom);
            Assert.InRange(cardTop + ModernToastScheduler.CardHeight,
                layout.Top, layout.Bottom);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EntranceAndExitKeepOccupiedUnionAlignedToSharedOrigin(int count)
    {
        var s = new ModernToastScheduler();
        for (var i = 0; i < count; i++) s.Enqueue(E(i));
        var hidden = ModernStackDecorationLayout.ForFrames(s.Advance(TimeSpan.Zero));
        Assert.Equal(ModernStackGeometry.ViewportHeight, hidden.Top);
        Assert.Equal(70d, hidden.Height);
        var halfway = ModernStackDecorationLayout.ForFrames(
            s.Advance(TimeSpan.FromMilliseconds(150)));
        // At half entrance the bottommost card is still 35 units BELOW
        // the work-area edge. It is the shared WINDOW viewport that clips
        // those pixels, not the content-union background itself.
        Assert.Equal(ModernStackGeometry.ViewportHeight + 35, halfway.Bottom);
        Assert.True(halfway.Top < hidden.Top);
        Assert.True(halfway.Height > 0);
        var departing = ModernStackDecorationLayout.ForFrames(
            s.Advance(TimeSpan.FromMilliseconds(5450)));
        Assert.Equal(ModernStackGeometry.ViewportHeight + 35, departing.Bottom);
        // One-card envelopes retain 70 units while travelling; multi-card
        // envelopes contract as cards converge toward the shared origin.
        Assert.True(departing.Height <= count * ModernToastScheduler.CardHeight);
        Assert.True(departing.Top > ModernStackGeometry.ViewportHeight -
            count * ModernToastScheduler.CardHeight);
    }

    [Theory]
    [InlineData(.75)]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void NoPerCardUnderpaintAtFractionalPixelScale(double scale)
    {
        var s = new ModernToastScheduler();
        for (var i = 0; i < 3; i++) s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        var frames = s.Advance(TimeSpan.FromMilliseconds(300));
        var layout = ModernStackDecorationLayout.ForFrames(frames);
        Assert.Equal(0, layout.Top * scale);
        Assert.Equal(210 * scale, layout.Bottom * scale, 8);
        Assert.Equal(210 * scale, layout.Height * scale, 8);
    }
}
