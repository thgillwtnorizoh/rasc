using System.Security.Cryptography;

namespace Rasc.Core;

/// <summary>
/// Source 1 owner-local achievement JPEGs only. The CSV supplies a strictly
/// validated basename and expected SHA-256, never an arbitrary file path.
/// Original assets are neither embedded nor copied into the RASC package.
/// </summary>
public static class CsgoAchievementIconReader
{
    public const int MaxJpegBytes = 1_048_576;

    public static byte[]? ReadVerified(string gameRoot, CuratedAchievementDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameRoot);
        ArgumentNullException.ThrowIfNull(definition);
        string root = Path.GetFullPath(gameRoot);
        string contentRoot = Path.GetFileName(Path.TrimEndingDirectorySeparator(root))
            .Equals("csgo", StringComparison.OrdinalIgnoreCase)
            ? root : Path.Combine(root, "csgo");

        // Prefer owner-observed Panorama path, fallback to matching Scaleform.
        string[] relativeDirectories =
        [
            Path.Combine("materials", "panorama", "images", "icons", "achievements"),
            Path.Combine("resource", "flash", "images", "achievements")
        ];
        foreach (var directory in relativeDirectories)
        {
            string path = Path.Combine(contentRoot, directory, definition.IconFilename);
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                if (stream.Length is < 4 or > MaxJpegBytes) continue;
                byte[] bytes = new byte[(int)stream.Length];
                stream.ReadExactly(bytes);
                if (bytes[0] != 0xff || bytes[1] != 0xd8 ||
                    bytes[^2] != 0xff || bytes[^1] != 0xd9)
                    continue;
                if (Convert.ToHexString(SHA256.HashData(bytes))
                    .Equals(definition.IconSha256, StringComparison.OrdinalIgnoreCase))
                    return bytes;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A missing/busy/invalid copy cannot authorize a different icon.
                // The alternative exact-basename location can still qualify.
            }
        }
        return null;
    }
}

/// <summary>
/// Live toast content assembled from independently sourced GC ID, the owner's
/// editable RASC rarity policy, local Source 1 localization and verified art.
/// This content is never an achievement award rule.
/// </summary>
public sealed record ResolvedAchievementToast(
    string ApiId,
    string Name,
    string Description,
    bool IsRare,
    byte[]? IconJpeg);

public static class AchievementToastResolver
{
    public static ResolvedAchievementToast Resolve(
        string achievementId, CuratedAchievementCatalog? definitions,
        CsgoAchievementLocalization? text, string? gameRoot,
        bool loadArtwork = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementId);
        var definition = definitions?.Find(achievementId);
        var localized = text?.Find(achievementId);
        byte[]? icon = null;
        if (loadArtwork && definition is not null && !string.IsNullOrWhiteSpace(gameRoot))
            icon = CsgoAchievementIconReader.ReadVerified(gameRoot, definition);

        return new ResolvedAchievementToast(
            achievementId,
            localized?.Name ?? achievementId,
            localized?.Description ?? string.Empty,
            definition?.Rarity == CuratedRarity.Rare,
            icon);
    }
}
