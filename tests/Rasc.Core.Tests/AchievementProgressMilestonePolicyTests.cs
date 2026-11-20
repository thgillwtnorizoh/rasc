using Rasc.Core;
using Xunit;

namespace Rasc.Core.Tests;

public sealed class AchievementProgressMilestonePolicyTests
{
    [Fact]
    public void CandidateCumulativeGoalOnlyCrosses25Then50Then75()
    {
        Assert.Null(AchievementProgressMilestonePolicy.HighestNewMilestone(
            0, 2500, 10000, hasPriorValidCount: false)); // silent initial snapshot
        Assert.Equal(25, AchievementProgressMilestonePolicy.HighestNewMilestone(
            2499, 2500, 10000, hasPriorValidCount: true));
        Assert.Equal(50, AchievementProgressMilestonePolicy.HighestNewMilestone(
            4999, 5000, 10000, hasPriorValidCount: true));
        Assert.Equal(75, AchievementProgressMilestonePolicy.HighestNewMilestone(
            7499, 7500, 10000, hasPriorValidCount: true));
    }

    [Fact]
    public void JumpingMultipleMilestonesShowsHighestOnlyAndCompletionNeverPretendsProgress()
    {
        Assert.Equal(75, AchievementProgressMilestonePolicy.HighestNewMilestone(
            1, 8000, 10000, true));
        Assert.Null(AchievementProgressMilestonePolicy.HighestNewMilestone(
            1, 10000, 10000, true));
        Assert.Null(AchievementProgressMilestonePolicy.HighestNewMilestone(
            8000, 10001, 10000, true));
        Assert.Null(AchievementProgressMilestonePolicy.HighestNewMilestone(
            7500, 7500, 10000, true));
        Assert.Null(AchievementProgressMilestonePolicy.HighestNewMilestone(
            7600, 7500, 10000, true));
    }

    [Fact]
    public void UsesIntegerMathSafelyAndSupportsSmallThresholdGoals()
    {
        Assert.Equal(25, AchievementProgressMilestonePolicy.HighestNewMilestone(
            0, 1, 3, true));
        Assert.Equal(75, AchievementProgressMilestonePolicy.HighestNewMilestone(
            1, 2, 3, true));
        Assert.Null(AchievementProgressMilestonePolicy.HighestNewMilestone(
            int.MaxValue - 1, int.MaxValue, int.MaxValue, true));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AchievementProgressMilestonePolicy.HighestNewMilestone(
                0, 2, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AchievementProgressMilestonePolicy.HighestNewMilestone(
                -1, 2, 10, true));
    }
}
