using System.Collections.ObjectModel;

namespace Rasc.Core;

/// <summary>
/// Last VALID GC snapshot committed by RASC, scoped to one exact GC source.
/// No source baseline means Unknown, never an assumed locked state.
/// </summary>
public sealed record GcCurrentAchievementState(
    bool HasValidSnapshot,
    IReadOnlyDictionary<string, uint> Achievements)
{
    private static readonly IReadOnlyDictionary<string, uint> NoAchievements =
        new ReadOnlyDictionary<string, uint>(
            new Dictionary<string, uint>(StringComparer.Ordinal));

    public static GcCurrentAchievementState Unknown =>
        new(false, NoAchievements);
}

public enum AchievementEarnedState
{
    Unknown,
    Locked,
    Earned
}

/// <summary>
/// Presentation-only row: no achievement awards, no derived progress, no Steam changes.
/// Rarity is RASC's curated v0.2 policy, never an asserted Steam global rarity.
/// </summary>
public sealed record AchievementLibraryEntry(
    string ApiId,
    string DisplayName,
    string? Description,
    string? Language,
    CuratedRarity? Rarity,
    AchievementEarnedState State,
    uint? RawUnlockUnixSeconds,
    bool OutsideCuratedCatalog)
{
    /// <summary>0 is a source-preserved timestamp with unknown actual award time.</summary>
    public DateTimeOffset? KnownUnlockTime =>
        State == AchievementEarnedState.Earned && RawUnlockUnixSeconds is > 0
            ? DateTimeOffset.FromUnixTimeSeconds(RawUnlockUnixSeconds.Value)
            : null;
}

public sealed record AchievementLibrarySnapshot(
    IReadOnlyList<AchievementLibraryEntry> Entries,
    int CatalogCount,
    int EarnedCatalogCount,
    int ExtraSourceIdCount,
    bool HasValidSnapshot)
{
    public int LockedCatalogCount => HasValidSnapshot ? CatalogCount - EarnedCatalogCount : 0;
    public int UnknownCatalogCount => HasValidSnapshot ? 0 : CatalogCount;
}

/// <summary>
/// Pure, deterministic join of curated definitions, optional local text and source-confirmed
/// current GC achievement state. An extra GC key is visible but never awarded catalog status,
/// classified rare, or mistaken for original historical Steam schema evidence.
/// </summary>
public static class AchievementLibraryProjection
{
    public static AchievementLibrarySnapshot Create(
        CuratedAchievementCatalog definitions,
        CsgoAchievementLocalization? localization,
        GcCurrentAchievementState current)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(current);
        var entries = new List<AchievementLibraryEntry>(definitions.Entries.Count);
        int earnedCount = 0;

        foreach (var definition in definitions.Entries)
        {
            var text = localization?.Find(definition.ApiId);
            bool earned = current.HasValidSnapshot &&
                          current.Achievements.TryGetValue(definition.ApiId, out _);
            var stamp = earned ? current.Achievements[definition.ApiId] : (uint?)null;
            if (earned) earnedCount++;
            entries.Add(new AchievementLibraryEntry(
                definition.ApiId, text?.Name ?? definition.ApiId,
                text?.Description, text?.Language, definition.Rarity,
                !current.HasValidSnapshot ? AchievementEarnedState.Unknown
                    : earned ? AchievementEarnedState.Earned : AchievementEarnedState.Locked,
                stamp, false));
        }

        int extraCount = 0;
        if (current.HasValidSnapshot)
        {
            foreach (var (id, stamp) in current.Achievements
                         .OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (definitions.Find(id) is not null) continue;
                // GC can legitimately report unrecognized identifiers. Keep the raw ID
                // and timestamp while refusing to assert membership or RASC rarity.
                extraCount++;
                entries.Add(new AchievementLibraryEntry(
                    id, id, null, null, null, AchievementEarnedState.Earned,
                    stamp, true));
            }
        }

        return new AchievementLibrarySnapshot(entries.AsReadOnly(),
            definitions.Entries.Count, earnedCount, extraCount, current.HasValidSnapshot);
    }
}
