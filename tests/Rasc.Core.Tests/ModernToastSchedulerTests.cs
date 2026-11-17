using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ModernToastSchedulerTests
{
    private static UnlockEvent Event(int n) =>
        new($"event-{n}", "fixture", $"ACH_{n}", 0,
            DateTimeOffset.UnixEpoch);

    [Fact]
    public void ConstantsFollowSupportedDesktopReference()
    {
        Assert.Equal(283, ModernToastScheduler.CardWidth);
        Assert.Equal(70, ModernToastScheduler.CardHeight);
        Assert.Equal(TimeSpan.FromMilliseconds(300), ModernToastScheduler.Entrance);
        Assert.Equal(TimeSpan.FromSeconds(5), ModernToastScheduler.Hold);
        Assert.Equal(TimeSpan.FromMilliseconds(300), ModernToastScheduler.Exit);
        Assert.Equal(3, ModernToastScheduler.Capacity);
    }

    [Fact]
    public void IdleClockIsRebasedBeforeReceivingNewEvents()
    {
        var scheduler = new ModernToastScheduler();
        Assert.Empty(scheduler.Advance(TimeSpan.FromMinutes(9)));
        scheduler.Enqueue(Event(1));
        var frames = scheduler.Advance(TimeSpan.FromMinutes(9));
        Assert.Single(frames);
        Assert.Equal(ModernToastStage.Entering, frames[0].Stage);
        Assert.Equal(0, frames[0].Progress);
    }

    [Fact]
    public void EntranceHoldAndExitAreTimeDriven()
    {
        var scheduler = new ModernToastScheduler();
        scheduler.Enqueue(Event(1));
        var first = Assert.Single(scheduler.Advance(TimeSpan.Zero));
        Assert.Equal(ModernToastStage.Entering, first.Stage);
        Assert.Equal(0, first.BottomRelativeY);

        var inFrame = Assert.Single(scheduler.Advance(TimeSpan.FromMilliseconds(150)));
        Assert.Equal(-35, inFrame.BottomRelativeY, 6);
        Assert.Equal(ModernToastStage.Entering, inFrame.Stage);

        var showing = Assert.Single(scheduler.Advance(TimeSpan.FromMilliseconds(300)));
        Assert.Equal(ModernToastStage.Showing, showing.Stage);
        Assert.Equal(-70, showing.BottomRelativeY);

        var exit = Assert.Single(scheduler.Advance(TimeSpan.FromMilliseconds(5450)));
        Assert.Equal(ModernToastStage.Exiting, exit.Stage);
        Assert.Equal(-35, exit.BottomRelativeY, 6);

        Assert.Empty(scheduler.Advance(TimeSpan.FromMilliseconds(5600)));
        Assert.True(scheduler.IsIdle);
    }

    [Fact]
    public void BurstOfSixIsBoundedAndFifo()
    {
        var scheduler = new ModernToastScheduler();
        for (var n = 1; n <= 6; n++) Assert.True(scheduler.Enqueue(Event(n)));
        Assert.Equal(3, scheduler.Advance(TimeSpan.Zero).Count);
        Assert.Equal(3, scheduler.ActiveCount);
        Assert.Equal(3, scheduler.WaitingCount);
        var second = scheduler.Advance(TimeSpan.FromMilliseconds(5600));
        Assert.Equal(new[] { "event-6", "event-5", "event-4" },
            second.Select(f => f.Event.EventId).ToArray());
        Assert.Equal(0, scheduler.WaitingCount);
        Assert.All(second, f => Assert.Equal(ModernToastStage.Entering, f.Stage));
    }

    [Fact]
    public void DuplicateEventIdNeverReplaysDuringSession()
    {
        var scheduler = new ModernToastScheduler();
        Assert.True(scheduler.Enqueue(Event(1)));
        Assert.False(scheduler.Enqueue(Event(1)));
        scheduler.Advance(TimeSpan.FromSeconds(10));
        Assert.False(scheduler.Enqueue(Event(1)));
    }

    [Fact]
    public void LargeClockJumpResolvesQueuedLifetimes()
    {
        var scheduler = new ModernToastScheduler();
        for (var n = 1; n <= 9; n++) scheduler.Enqueue(Event(n));
        Assert.Equal(3, scheduler.Advance(TimeSpan.Zero).Count);
        Assert.Empty(scheduler.Advance(TimeSpan.FromMilliseconds(16800)));
        Assert.True(scheduler.IsIdle);
    }

    [Fact]
    public void RejectsReversedClockAndInvalidEvents()
    {
        var scheduler = new ModernToastScheduler();
        scheduler.Advance(TimeSpan.FromSeconds(1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            scheduler.Advance(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ModernToastScheduler().Advance(TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentException>(() => scheduler.Enqueue(
            Event(1) with { EventId = "" }));
    }

    [Fact]
    public void ClearStopsAllPendingNotifications()
    {
        var scheduler = new ModernToastScheduler();
        for (var n = 1; n <= 6; n++) scheduler.Enqueue(Event(n));
        scheduler.Advance(TimeSpan.Zero);
        scheduler.Clear();
        Assert.True(scheduler.IsIdle);
        Assert.True(scheduler.Enqueue(Event(1)));
        Assert.Single(scheduler.Advance(TimeSpan.Zero));
    }

    [Fact]
    public void HoldDeadlineDoesNotAlterAnimationTime()
    {
        var scheduler = new ModernToastScheduler();
        scheduler.Enqueue(Event(1));
        scheduler.Advance(TimeSpan.FromMilliseconds(300));
        Assert.False(scheduler.IsAnimating);
        Assert.Equal(TimeSpan.FromMilliseconds(5300),
            scheduler.NextHoldDeadline);
        scheduler.Advance(TimeSpan.FromMilliseconds(5300));
        Assert.True(scheduler.IsAnimating);
    }

    [Fact]
    public void SlotsAreDistinctAndNewestAtBottom()
    {
        var scheduler = new ModernToastScheduler();
        scheduler.Enqueue(Event(1));
        scheduler.Enqueue(Event(2));
        scheduler.Enqueue(Event(3));
        var frames = scheduler.Advance(TimeSpan.FromMilliseconds(300));
        Assert.Equal(new[] { "event-3", "event-2", "event-1" },
            frames.Select(f => f.Event.EventId).ToArray());
        Assert.Equal(new[] { -70.0, -140.0, -210.0 },
            frames.Select(f => f.BottomRelativeY).ToArray());
    }
}

public sealed class NotificationProfileStoreTests
{
    [Fact]
    public void SavesAndLoadsSelection()
    {
        var path = Path.Combine(Path.GetTempPath(), "rasc-profile-" +
            Guid.NewGuid().ToString("N"), "profile.txt");
        try
        {
            var store = new NotificationProfileStore(path);
            Assert.Equal(NotificationProfile.Classic, store.Load());
            Assert.True(store.TrySave(NotificationProfile.Modern));
            Assert.Equal(NotificationProfile.Modern, store.Load());
            Assert.True(store.TrySave(NotificationProfile.Off));
            Assert.Equal(NotificationProfile.Off, store.Load());
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void UnknownOrOversizedPreferenceFailsClosedToClassic()
    {
        var path = Path.GetTempFileName();
        try
        {
            var store = new NotificationProfileStore(path);
            File.WriteAllText(path, "not-a-profile");
            Assert.Equal(NotificationProfile.Classic, store.Load());
            File.WriteAllText(path, new string('x', 200));
            Assert.Equal(NotificationProfile.Classic, store.Load());
            File.WriteAllText(path, "999");
            Assert.Equal(NotificationProfile.Classic, store.Load());
            Assert.False(store.TrySave((NotificationProfile)999));
        }
        finally { File.Delete(path); }
    }
}
