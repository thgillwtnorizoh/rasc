using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class ProgressPriorityTests
{
    private static UnlockEvent New(string eventId, string achievementId,
        DateTimeOffset at, bool progress = false) =>
        new(eventId, "source", achievementId, 0, at)
        {
            NotificationKind = progress
                ? AchievementNotificationKind.Progress
                : AchievementNotificationKind.Unlock
        };

    [Fact]
    public void GateDiscardsCrossStyleQueuedProgressButKeepsOtherAwards()
    {
        var idle = new Dictionary<NotificationProfile, bool>
        {
            [NotificationProfile.Modern] = true,
            [NotificationProfile.Classic] = false
        };
        var displayed = new List<string>();
        var gate = new NotificationPresentationGate(profile => idle[profile]);
        gate.Enqueue(NotificationProfile.Modern, () =>
        {
            displayed.Add("active-modern");
            idle[NotificationProfile.Modern] = false;
        });
        Assert.True(gate.EnqueueProgress(NotificationProfile.Classic,
            "BODY_BAGGER", () => displayed.Add("obsolete-75")));
        Assert.True(gate.EnqueueProgress(NotificationProfile.Classic,
            "UNRELATED", () => displayed.Add("other-25")));
        gate.Enqueue(NotificationProfile.Classic, () => displayed.Add("unlock"));
        Assert.Equal(3, gate.PendingCount);
        Assert.Equal(1, gate.AcceptUnlock("BODY_BAGGER"));
        Assert.Equal(2, gate.PendingCount);
        Assert.False(gate.EnqueueProgress(NotificationProfile.Modern,
            "BODY_BAGGER", () => displayed.Add("should-not-appear")));
        idle[NotificationProfile.Modern] = true;
        gate.Pump();
        Assert.Equal(new[] { "active-modern", "other-25", "unlock" }, displayed);
    }

    [Fact]
    public void ReentrantUnlockCancelsProgressBeforeDelivery()
    {
        var displayed = new List<string>();
        var gate = new NotificationPresentationGate(_ => true);
        gate.Enqueue(NotificationProfile.Classic, () =>
        {
            displayed.Add("first");
            Assert.True(gate.EnqueueProgress(NotificationProfile.Classic,
                "A", () => displayed.Add("stale")));
            Assert.True(gate.EnqueueProgress(NotificationProfile.Classic,
                "B", () => displayed.Add("valid")));
            Assert.Equal(1, gate.AcceptUnlock("A"));
        });
        Assert.Equal(new[] { "first", "valid" }, displayed);
    }

    [Fact]
    public void ClassicCancelsWaitingProgressWithoutInterruptingStartedCard()
    {
        var at = DateTimeOffset.UnixEpoch;
        var schedule = new ClassicToastScheduler();
        Assert.True(schedule.Enqueue(New("started-progress", "A", at, true)));
        schedule.Advance(TimeSpan.Zero); // admitted: animation can finish
        Assert.True(schedule.Enqueue(New("cancelled-progress", "A", at, true)));
        Assert.True(schedule.Enqueue(New("other-progress", "B", at, true)));
        Assert.True(schedule.Enqueue(New("unrelated-unlock", "C", at)));
        Assert.Equal(3, schedule.WaitingCount);
        Assert.Equal(new[] { "cancelled-progress" }, schedule.MarkEarned("A"));
        Assert.Equal(2, schedule.WaitingCount);
        Assert.False(schedule.Enqueue(New("later-progress", "A", at, true)));
        var frames = schedule.Advance(TimeSpan.FromMilliseconds(200));
        Assert.Contains(frames, x => x.Event.EventId == "started-progress");
        schedule.Advance(TimeSpan.FromSeconds(40));
        Assert.True(schedule.IsIdle);
        Assert.Equal(0, schedule.WaitingCount);
    }

    [Fact]
    public void ModernCancelsUnpresentedWaveMemberAndPreservesOriginalDeadline()
    {
        var at = DateTimeOffset.UnixEpoch;
        var queue = new ModernAchievementWaveQueue();
        Assert.True(queue.Enqueue(New("award-a", "A", at)));
        Assert.True(queue.Enqueue(New("progress-b", "B", at.AddMilliseconds(100), true)));
        Assert.True(queue.Enqueue(New("award-c", "C", at.AddMilliseconds(200))));
        Assert.True(queue.Enqueue(New("progress-d", "D", at.AddMilliseconds(1000), true)));
        var firstReadyAt = queue.NextReadyAt;
        Assert.Equal(at.AddMilliseconds(800), firstReadyAt);
        Assert.Equal(new[] { "progress-b" }, queue.MarkEarned("B"));
        Assert.Equal(firstReadyAt, queue.NextReadyAt);
        Assert.False(queue.Enqueue(New("later-b", "B", at.AddMilliseconds(1010), true)));
        var wave = queue.TakeReady(at.AddMilliseconds(800));
        Assert.NotNull(wave);
        Assert.Equal(new[] { "award-a", "award-c" },
            wave.Achievements.Select(x => x.EventId));
        var next = queue.TakeReady(at.AddMilliseconds(1800));
        Assert.NotNull(next);
        Assert.Equal("progress-d", Assert.Single(next.Achievements).EventId);
    }

    [Fact]
    public void ModernRemovesEmptyProgressOnlyWaveWithoutDelayingFollowingAward()
    {
        var at = DateTimeOffset.UnixEpoch;
        var queue = new ModernAchievementWaveQueue();
        queue.Enqueue(New("progress-a", "A", at, true));
        queue.Enqueue(New("unlock-b", "B", at.AddMilliseconds(900)));
        Assert.Equal(new[] { "progress-a" }, queue.MarkEarned("A"));
        Assert.Equal(1, queue.PendingWaveCount);
        Assert.Equal(at.AddMilliseconds(1700), queue.NextReadyAt);
        Assert.Null(queue.TakeReady(at.AddMilliseconds(1600)));
        Assert.Equal("unlock-b",
            Assert.Single(queue.TakeReady(at.AddMilliseconds(1700))!.Achievements).EventId);
    }

    [Fact]
    public void NewObservationSessionClearsOldEarnedStateAndOldPendingProgress()
    {
        var at = DateTimeOffset.UnixEpoch;
        var idle = new Dictionary<NotificationProfile, bool>
        {
            [NotificationProfile.Modern] = false,
            [NotificationProfile.Classic] = true
        };
        var gate = new NotificationPresentationGate(profile => idle[profile]);
        gate.Enqueue(NotificationProfile.Modern, () => { });
        Assert.True(gate.EnqueueProgress(NotificationProfile.Classic, "A", () => { }));
        gate.AcceptUnlock("B");
        Assert.Equal(1, gate.BeginProgressSession());
        Assert.False(gate.IsEarned("B"));
        Assert.Equal(0, gate.PendingCount);

        var classic = new ClassicToastScheduler();
        classic.Enqueue(New("first-award", "X", at));
        classic.Advance(TimeSpan.Zero);
        classic.Enqueue(New("old-progress", "B", at, true));
        classic.MarkEarned("A");
        Assert.Equal(new[] { "old-progress" }, classic.BeginProgressSession());
        Assert.True(classic.Enqueue(New("new-progress", "A", at, true)));

        var modern = new ModernAchievementWaveQueue();
        modern.Enqueue(New("old-wave-progress", "A", at, true));
        modern.MarkEarned("B");
        Assert.Equal(new[] { "old-wave-progress" }, modern.BeginProgressSession());
        Assert.True(modern.Enqueue(New("new-wave-progress", "B", at, true)));
    }

    [Fact]
    public void TakingWaveMakesItPresentedAndNoLongerCancelable()
    {
        var at = DateTimeOffset.UnixEpoch;
        var queue = new ModernAchievementWaveQueue();
        queue.Enqueue(New("visible-progress", "A", at, true));
        var alreadyAdmitted = queue.TakeReady(at.AddMilliseconds(800));
        Assert.NotNull(alreadyAdmitted);
        Assert.Empty(queue.MarkEarned("A")); // host owns its visible animation
        Assert.Equal("visible-progress", Assert.Single(alreadyAdmitted.Achievements).EventId);
    }
}
