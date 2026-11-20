namespace Rasc.Core;

/// <summary>
/// Presentation-only threshold policy for a FUTURE source-verified progress mapping.
/// It NEVER derives achievement progress from an unlock flag, description, or rarity.
/// Until Work 1 proves an exact source stat/target pairing, no live progress
/// events are generated and no progress rows are persisted by RASC.
/// </summary>
public static class AchievementProgressMilestonePolicy
{
    public static readonly IReadOnlyList<int> Milestones = [25, 50, 75];

    /// <summary>
    /// Returns the highest 25/50/75 percent boundary newly crossed since a
    /// valid PREVIOUS reading. A jump past several emits one progress toast,
    /// never several stale ones. A completed achievement uses its unlock toast.
    /// Callers must separately establish authorized stat/target provenance and
    /// persist delivered milestones before wiring this to actual notifications.
    /// </summary>
    public static int? HighestNewMilestone(
        int previousCount, int currentCount, int targetCount, bool hasPriorValidCount)
    {
        if (targetCount <= 0 || previousCount < 0 || currentCount < 0)
            throw new ArgumentOutOfRangeException(nameof(targetCount),
                "Progress counters and positive goal must be nonnegative and valid.");
        if (!hasPriorValidCount || currentCount <= previousCount ||
            currentCount >= targetCount)
            return null;

        foreach (int percent in new[] { 75, 50, 25 })
        {
            long required = (long)targetCount * percent;
            if ((long)previousCount * 100 < required &&
                (long)currentCount * 100 >= required)
                return percent;
        }
        return null;
    }
}
