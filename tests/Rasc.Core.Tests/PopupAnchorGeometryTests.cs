using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class PopupAnchorGeometryTests
{
    private static readonly PopupRect Monitor = new(0, 0, 2560, 1440);
    private static readonly PopupRect Work = new(0, 0, 2560, 1392);

    [Fact]
    public void FullscreenAppAnchorsToPhysicalMonitorNotTaskbarWorkArea()
    {
        Assert.Equal(Monitor, PopupAnchorGeometry.Resolve(
            Monitor, Monitor, Work, true));
        Assert.Equal(Monitor, PopupAnchorGeometry.Resolve(
            new PopupRect(-1, -1, 2562, 1442), Monitor, Work, true));
    }

    [Fact]
    public void WindowedAppAnchorsToVisibleWindowBounds()
    {
        var app = new PopupRect(300, 200, 1280, 800);
        Assert.Equal(app, PopupAnchorGeometry.Resolve(app, Monitor, Work, true));
        var crossingTaskbar = new PopupRect(100, 1100, 900, 300);
        Assert.Equal(new PopupRect(100, 1100, 900, 292),
            PopupAnchorGeometry.Resolve(crossingTaskbar, Monitor, Work, true));
    }

    [Fact]
    public void FallbackPreservesNormalDesktopWorkArea()
    {
        Assert.Equal(Work, PopupAnchorGeometry.Resolve(
            new PopupRect(0, 0, 0, 0), Monitor, Work, true));
        Assert.Equal(Work, PopupAnchorGeometry.Resolve(
            Monitor, Monitor, Work, false));
    }

    [Fact]
    public void ForegroundPlacementHandlesNegativeMonitorCoordinates()
    {
        var mon = new PopupRect(-1920, -100, 1920, 1080);
        var work = new PopupRect(-1920, -100, 1920, 1040);
        var app = new PopupRect(-1600, 25, 1000, 600);
        Assert.Equal(app, PopupAnchorGeometry.Resolve(app, mon, work, true));
        Assert.Equal(mon, PopupAnchorGeometry.Resolve(mon, mon, work, true));
    }

    [Fact]
    public void NativePixelsFollowRightEdgeOnSecondaryMonitorAtFractionalDpi()
    {
        var app = new PopupRect(2560, -120, 1440, 900);
        var frame = PopupAnchorGeometry.Position(app, 1.25, 240, 94, 1.5);
        Assert.Equal(new PopupRect(3550, 604, 450, 176), frame);
        Assert.Equal(app.Right, frame.Right);
        Assert.Equal(app.Bottom, frame.Bottom);
    }

    [Fact]
    public void SharedBelowAppOriginAndFullscreenEdgeHaveDistinctTargets()
    {
        var full = new PopupRect(0, 0, 1920, 1080);
        var windowed = new PopupRect(400, 200, 1000, 700);
        var hidden = PopupAnchorGeometry.Position(full, 1, 283, 70, 1, 0);
        Assert.Equal(1080, hidden.Top); // entirely below the fullscreen app
        Assert.Equal(1920, hidden.Right);
        var fullVisible = PopupAnchorGeometry.Position(full, 1, 283, 70, 1, -70);
        Assert.Equal(1010, fullVisible.Top);
        var windowedVisible = PopupAnchorGeometry.Position(windowed, 1, 283, 70, 1, -70);
        Assert.Equal(830, windowedVisible.Top);
        Assert.Equal(1400, windowedVisible.Right);
    }

    [Fact]
    public void RejectsInvalidNativePlacementGeometry()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopupAnchorGeometry.Position(new PopupRect(0,0,0,0), 1, 240, 94, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopupAnchorGeometry.Position(Monitor, double.NaN, 240, 94, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopupAnchorGeometry.Position(Monitor, 1, 240, 94, 1, double.PositiveInfinity));
    }

    [Fact]
    public void AutoScaleUsesForegroundAppHeightAndFitsViewport()
    {
        Assert.Equal(1.333333333, PopupAnchorGeometry.AutoScale(
            1440, 2560, 1440, 240, 282, 0), 6);
        Assert.Equal(0.625, PopupAnchorGeometry.AutoScale(
            675, 1000, 675, 240, 282, 0), 6);
        Assert.Equal(1.5, PopupAnchorGeometry.AutoScale(
            675, 1000, 675, 240, 282, 150), 6);
        Assert.True(PopupAnchorGeometry.AutoScale(
            100, 150, 100, 283, 210, 0) <= 100d / 210d);
    }
}
