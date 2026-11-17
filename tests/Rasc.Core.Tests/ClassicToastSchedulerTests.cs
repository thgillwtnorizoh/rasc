using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public class ClassicToastSchedulerTests
{
    private static UnlockEvent E(int n) =>
        new($"event-{n}", "test-source", $"ACHIEVEMENT_{n}", (uint)(100 + n),
            DateTimeOffset.FromUnixTimeSeconds(100 + n));

    [Fact]
    public void OriginalSourceConstantsArePreserved()
    {
        Assert.Equal(3, ClassicToastScheduler.Capacity);
        Assert.Equal(TimeSpan.FromMilliseconds(450), ClassicToastScheduler.FadeIn);
        Assert.Equal(TimeSpan.FromSeconds(6), ClassicToastScheduler.Visible);
        Assert.Equal(TimeSpan.FromMilliseconds(450), ClassicToastScheduler.FadeOut);
        Assert.Equal(TimeSpan.FromMilliseconds(6900), ClassicToastScheduler.Lifetime);
    }

    [Fact]
    public void EmptyQueueRemainsIdle()
    {
        var s = new ClassicToastScheduler();
        Assert.Empty(s.Advance(TimeSpan.Zero));
        Assert.True(s.IsIdle);
    }

    [Fact]
    public void SingleEventTraversesExactTimeBoundaries()
    {
        var s = new ClassicToastScheduler();
        s.Enqueue(E(1));
        var start = Assert.Single(s.Advance(TimeSpan.Zero));
        Assert.Equal(ClassicToastStage.Entering, start.Stage);
        Assert.Equal(0, start.Opacity);
        Assert.Equal(0, start.Slot);

        var midpoint = Assert.Single(s.Advance(TimeSpan.FromMilliseconds(225)));
        Assert.Equal(0.5, midpoint.Opacity, 4);
        Assert.True(midpoint.VerticalOffsetDips > 0);

        var visible = Assert.Single(s.Advance(TimeSpan.FromMilliseconds(450)));
        Assert.Equal(ClassicToastStage.Visible, visible.Stage);
        Assert.Equal(1, visible.Opacity);
        Assert.Equal(0, visible.VerticalOffsetDips);

        var lastSteady = Assert.Single(s.Advance(TimeSpan.FromMilliseconds(6449)));
        Assert.Equal(ClassicToastStage.Visible, lastSteady.Stage);
        var fade = Assert.Single(s.Advance(TimeSpan.FromMilliseconds(6450)));
        Assert.Equal(ClassicToastStage.Exiting, fade.Stage);
        Assert.Equal(1, fade.Opacity);
        var fading = Assert.Single(s.Advance(TimeSpan.FromMilliseconds(6675)));
        Assert.Equal(0.5, fading.Opacity, 4);

        Assert.Empty(s.Advance(TimeSpan.FromMilliseconds(6900)));
        Assert.True(s.IsIdle);
    }

    [Fact]
    public void SixUnlockBurstUsesThreeSlotsThenFifoOverCapacity()
    {
        var s = new ClassicToastScheduler();
        for (int i = 1; i <= 6; i++) Assert.True(s.Enqueue(E(i)));
        var first = s.Advance(TimeSpan.Zero);
        Assert.Equal(new[] { "event-1", "event-2", "event-3" },
            first.Select(f => f.Event.EventId));
        Assert.Equal(new[] { 0, 1, 2 }, first.Select(f => f.Slot));
        Assert.Equal(3, s.WaitingCount);

        Assert.Equal(3, s.Advance(TimeSpan.FromMilliseconds(6899)).Count);
        var second = s.Advance(TimeSpan.FromMilliseconds(6900));
        Assert.Equal(new[] { "event-4", "event-5", "event-6" },
            second.Select(f => f.Event.EventId));
        Assert.Equal(3, s.ActiveCount);
        Assert.Equal(0, s.WaitingCount);
        Assert.Empty(s.Advance(TimeSpan.FromMilliseconds(13800)));
    }

    [Fact]
    public void ALaterEventDoesNotJumpTheWaitingQueue()
    {
        var s = new ClassicToastScheduler();
        for (int i = 1; i <= 4; i++) s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        Assert.Equal(new[] { "event-1", "event-2", "event-3" },
            s.Advance(TimeSpan.FromSeconds(3)).Select(x => x.Event.EventId));
        s.Enqueue(E(5));
        Assert.Equal(new[] { "event-4", "event-5" },
            s.Advance(TimeSpan.FromMilliseconds(6900))
                .Select(x => x.Event.EventId));
    }

    [Fact]
    public void OnlyAnActiveOrWaitingDuplicateIsSuppressed()
    {
        var s = new ClassicToastScheduler();
        Assert.True(s.Enqueue(E(1)));
        Assert.False(s.Enqueue(E(1)));
        s.Advance(TimeSpan.Zero);
        Assert.False(s.Enqueue(E(1)));
        s.Advance(TimeSpan.FromMilliseconds(6900));
        Assert.True(s.Enqueue(E(1))); // Source ledger, not UI scheduler, owns long-term dedup.
    }

    [Fact]
    public void ClearRemovesCurrentAndWaitingPresentationOnly()
    {
        var s = new ClassicToastScheduler();
        for (int i = 1; i <= 6; i++) s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        s.Clear();
        Assert.True(s.IsIdle);
        Assert.Empty(s.Advance(TimeSpan.FromSeconds(5)));
        Assert.True(s.Enqueue(E(1)));
    }

    [Fact]
    public void RejectsClockReversal()
    {
        var s = new ClassicToastScheduler();
        s.Advance(TimeSpan.FromSeconds(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Advance(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Advance(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void RejectsUnstableEventIds()
    {
        var s = new ClassicToastScheduler();
        Assert.Throws<ArgumentException>(() => s.Enqueue(E(1) with { EventId = "" }));
    }

    [Fact]
    public void AtMostThreeActiveDuringInterleavedArrival()
    {
        var s = new ClassicToastScheduler();
        s.Enqueue(E(1));
        s.Advance(TimeSpan.Zero);
        s.Enqueue(E(2));
        s.Advance(TimeSpan.FromSeconds(2));
        for (int i = 3; i <= 20; i++) s.Enqueue(E(i));
        for (var ms = 2000; ms <= 40000; ms += 250)
        {
            var frames = s.Advance(TimeSpan.FromMilliseconds(ms));
            Assert.InRange(frames.Count, 0, 3);
            Assert.Equal(frames.Select(f => f.Event.EventId).Distinct().Count(), frames.Count);
        }
    }
}
