using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ModernToastWaveTests
{
    private static UnlockEvent E(int n) =>
        new($"wave-event-{n}", "fixture", $"ACH-{n}", 0, DateTimeOffset.UnixEpoch);

    [Fact]
    public void OneTwoThreeCardsShareExactlyTheSameOffscreenEntryPoint()
    {
        for (var size = 1; size <= 3; size++)
        {
            var schedule = new ModernToastScheduler();
            for (var i = 0; i < size; i++) schedule.Enqueue(E(i));
            var start = schedule.Advance(TimeSpan.Zero);
            Assert.Equal(size, start.Count);
            Assert.All(start, f =>
            {
                Assert.Equal(0, f.BottomRelativeY);
                Assert.Equal(ModernToastStage.Entering, f.Stage);
                Assert.Equal(size, f.WaveSize);
            });
            var half = schedule.Advance(TimeSpan.FromMilliseconds(150));
            for (var i = 0; i < size; i++)
                Assert.Equal(-(half[i].Slot + 1) * 35d, half[i].BottomRelativeY);
            var full = schedule.Advance(TimeSpan.FromMilliseconds(300));
            for (var i = 0; i < size; i++)
                Assert.Equal(-(full[i].Slot + 1) * 70d, full[i].BottomRelativeY);
        }
    }

    [Fact]
    public void BatchedAdmissionStartsAtItsActualShowTime()
    {
        var s = new ModernToastScheduler();
        Assert.Empty(s.Advance(TimeSpan.FromMinutes(7)));
        Assert.True(s.Enqueue(E(17)));
        s.AlignPendingWaveStart(TimeSpan.FromMinutes(7) +
            TimeSpan.FromMilliseconds(45));
        var start = Assert.Single(s.Advance(TimeSpan.FromMinutes(7) +
            TimeSpan.FromMilliseconds(45)));
        Assert.Equal(0, start.BottomRelativeY);
        Assert.Equal(ModernToastStage.Entering, start.Stage);
        Assert.Throws<InvalidOperationException>(() =>
            s.AlignPendingWaveStart(TimeSpan.FromMinutes(8)));
    }

    [Fact]
    public void LateArrivalWaitsForWholeWaveToExit()
    {
        var s = new ModernToastScheduler();
        s.Enqueue(E(1));
        Assert.Single(s.Advance(TimeSpan.Zero));
        s.Advance(TimeSpan.FromSeconds(1));
        s.Enqueue(E(2));
        s.Enqueue(E(3));
        Assert.Single(s.Advance(TimeSpan.FromSeconds(3)));
        Assert.Equal(2, s.WaitingCount);
        var exiting = Assert.Single(s.Advance(TimeSpan.FromMilliseconds(5450)));
        Assert.Equal("wave-event-1", exiting.Event.EventId);
        Assert.Equal(-35d, exiting.BottomRelativeY);
        var next = s.Advance(TimeSpan.FromMilliseconds(5600));
        Assert.Collection(next,
            f => Assert.Equal("wave-event-2", f.Event.EventId),
            f => Assert.Equal("wave-event-3", f.Event.EventId));
        Assert.All(next, f => Assert.Equal(0, f.BottomRelativeY));
    }

    [Fact]
    public void WholeWaveReturnsToCommonOriginOnExit()
    {
        var s = new ModernToastScheduler();
        for (var i = 0; i < 3; i++) s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        var mid = s.Advance(TimeSpan.FromMilliseconds(5450));
        Assert.Equal(3, mid.Count);
        Assert.All(mid, f => Assert.Equal(ModernToastStage.Exiting, f.Stage));
        Assert.Equal(new[] { -105d, -70d, -35d },
            mid.Select(f => f.BottomRelativeY).ToArray());
        Assert.Empty(s.Advance(TimeSpan.FromMilliseconds(5600)));
    }

    [Fact]
    public void DeadlineDrivenWavesFlushAllFiveWithoutAnyLaterUnlock()
    {
        var schedule = new ModernToastScheduler();
        for (int i = 1; i <= 3; i++) Assert.True(schedule.Enqueue(E(i)));
        var first = schedule.Advance(TimeSpan.Zero);
        Assert.Equal(3, first.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(300), schedule.NextTransitionDeadline);
        // Two events arrive during the first wave, then the game goes quiet.
        Assert.True(schedule.Enqueue(E(4)));
        Assert.True(schedule.Enqueue(E(5)));
        schedule.Advance(TimeSpan.FromMilliseconds(300));
        Assert.Equal(TimeSpan.FromMilliseconds(5300), schedule.NextTransitionDeadline);
        schedule.Advance(TimeSpan.FromMilliseconds(5300));
        Assert.Equal(TimeSpan.FromMilliseconds(5600), schedule.NextTransitionDeadline);
        var second = schedule.Advance(TimeSpan.FromMilliseconds(5600));
        Assert.Equal(new[] { "wave-event-4", "wave-event-5" },
            second.Select(frame => frame.Event.EventId));
        Assert.Equal(TimeSpan.FromMilliseconds(5900), schedule.NextTransitionDeadline);
        schedule.Advance(TimeSpan.FromMilliseconds(11200));
        Assert.True(schedule.IsIdle);
        Assert.Null(schedule.NextTransitionDeadline);
    }

    [Fact]
    public void AllWaveBacklogExpiresAtExactDeadlinesAcrossLargeJumps()
    {
        var s = new ModernToastScheduler();
        for (int i = 0; i < 8; i++) s.Enqueue(E(i));
        s.Advance(TimeSpan.Zero);
        var second = s.Advance(TimeSpan.FromMilliseconds(5600));
        Assert.Equal(3, second.Count);
        Assert.All(second, f => Assert.Equal(0, f.BottomRelativeY));
        var third = s.Advance(TimeSpan.FromMilliseconds(11200));
        Assert.Equal(2, third.Count);
        Assert.All(third, f => Assert.Equal(0, f.BottomRelativeY));
        Assert.Empty(s.Advance(TimeSpan.FromMilliseconds(16800)));
    }

    [Fact]
    public void WatermarkMovesWithWaveSizeAndSplitsContinuouslyAcrossCards()
    {
        Assert.NotEqual(ModernWatermarkLayout.TopInsideCard(1, 0),
            ModernWatermarkLayout.TopInsideCard(3, 0));
        for (var count = 2; count <= 3; count++)
        {
            for (var slot = 0; slot < count - 1; slot++)
                Assert.Equal(ModernToastScheduler.CardHeight,
                    ModernWatermarkLayout.TopInsideCard(count, slot + 1) -
                    ModernWatermarkLayout.TopInsideCard(count, slot));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernWatermarkLayout.TopInsideCard(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernWatermarkLayout.TopInsideCard(3, 3));
    }

    [Fact]
    public void UnlockProgressAndRareAreDistinctAndValidated()
    {
        var unlock = ModernToastContent.Unlock("Champion", "Win 100 games.");
        Assert.Equal(ModernToastKind.Unlock, unlock.Kind);
        Assert.False(unlock.IsRare);
        Assert.Equal("", unlock.NumericProgress);
        var rare = ModernToastContent.Unlock("Interstellar",
            "Accumulate one mile of distance.", isRare: true);
        Assert.True(rare.IsRare);
        var progress = ModernToastContent.Progress("Arrest Warrant", 19, 50);
        Assert.Equal(ModernToastKind.Progress, progress.Kind);
        Assert.Equal("(19/50)", progress.NumericProgress);
        Assert.Equal(0.38, progress.ProgressFraction, 6);
        // Recovered Steam UI progress bar uses configured minimum, not
        // simply current/maximum. Text remains the literal current/maximum.
        var threshold = ModernToastContent.Progress("Threshold", 19, 50, minimum: 10);
        Assert.Equal("(19/50)", threshold.NumericProgress);
        Assert.Equal(0.225, threshold.ProgressFraction, 6);
        Assert.Equal(0, ModernToastContent.Progress("Below", 5, 50, minimum: 10).ProgressFraction);
        Assert.Equal(1, ModernToastContent.Progress("Complete", 50, 50, minimum: 10).ProgressFraction);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernToastContent.Progress("Invalid", 19, 50, minimum: 50));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernToastContent.Progress("Invalid", 19, 50, minimum: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernToastContent.Progress("Impossible", -1, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernToastContent.Progress("Impossible", 51, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ModernToastContent.Progress("Impossible", 0, 0));
        Assert.Throws<ArgumentException>(() => ModernToastContent.Unlock("", ""));
    }
}
