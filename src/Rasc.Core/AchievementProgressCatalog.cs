using System.Globalization;
using System.Text;

namespace Rasc.Core;

public enum ProgressEligibility { ScalarCandidate, CompositeCandidate, UnlockOnly }

/// <summary>
/// A candidate is NOT an original Steam progress event. Only source-verified,
/// explicitly enabled scalar mappings may produce synthetic RASC progress.
/// </summary>
public sealed record AchievementProgressMapping(
    string AchievementId, ProgressEligibility Classification, string StatId,
    int Target, string Evidence, string GameProfile, bool LiveEnabled)
{
    public bool IsLiveEligible =>
        Classification == ProgressEligibility.ScalarCandidate && LiveEnabled &&
        StatId.Length > 0 && Target > 1 &&
        Evidence.StartsWith("VERIFIED:", StringComparison.Ordinal) &&
        GameProfile.Length > 0;
}

/// <summary>
/// Editable progress eligibility separate from curated rarity. Every row must
/// be explicitly valid; runtime fails CLOSED on conflicting/invalid mappings.
/// No bundled row is enabled. Supports a verified owner research override.
/// </summary>
public sealed class AchievementProgressCatalog
{
    public const string FileName = "achievement-progress-mappings.v0.1.csv";
    public const string Header =
        "api_id,classification,stat_id,target,evidence,game_profile,live_enabled";
    private const string EmbeddedName =
        "Rasc.Core.Data.achievement-progress-mappings.v0.1.csv";
    private readonly Dictionary<string, AchievementProgressMapping> _byId;
    public IReadOnlyList<AchievementProgressMapping> Entries { get; }
    public string Source { get; }

    private AchievementProgressCatalog(List<AchievementProgressMapping> entries, string source)
    {
        Entries = entries.AsReadOnly();
        _byId = entries.ToDictionary(x => x.AchievementId, StringComparer.Ordinal);
        Source = source;
    }

    public AchievementProgressMapping? Find(string id) =>
        _byId.TryGetValue(id, out var value) ? value : null;

    public static AchievementProgressCatalog LoadDefault(string? path = null)
    {
        string text;
        string source;
        if (path is not null)
        {
            text = File.ReadAllText(path, new UTF8Encoding(false, true));
            source = path;
        }
        else
        {
            var editable = Path.Combine(AppContext.BaseDirectory, "Data", FileName);
            if (File.Exists(editable))
            {
                text = File.ReadAllText(editable, new UTF8Encoding(false, true));
                source = editable;
            }
            else
            {
                using var stream = typeof(AchievementProgressCatalog).Assembly
                    .GetManifestResourceStream(EmbeddedName) ??
                    throw new InvalidOperationException("Embedded progress catalog missing.");
                using var reader = new StreamReader(stream, new UTF8Encoding(false, true));
                text = reader.ReadToEnd();
                source = "embedded:" + EmbeddedName;
            }
        }

        var result = Parse(text, source);
        // Do not accidentally use a partial or cross-version progress catalog.
        var curated = CuratedAchievementCatalog.LoadDefault();
        if (result.Entries.Count != curated.Entries.Count ||
            result.Entries.Any(x => curated.Find(x.AchievementId) is null))
            throw new FormatException(
                "Progress catalog must exactly match the installed curated 167 IDs.");
        return result;
    }

    public static AchievementProgressCatalog Parse(string text, string source = "fixture")
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 262_144)
            throw new FormatException("Progress mapping CSV exceeds 256 KiB.");
        using var reader = new StringReader(text.TrimStart('\uFEFF'));
        var rows = new List<AchievementProgressMapping>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        bool header = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (!header)
            {
                if (line != Header)
                    throw new FormatException("Invalid progress catalog header.");
                header = true;
                continue;
            }
            if (rows.Count >= 1024)
                throw new FormatException("Progress catalog exceeds 1024 rows.");
            var fields = line.Split(',');
            if (fields.Length != 7 || fields.Any(x => x.Contains('"')))
                throw new FormatException("Progress catalog requires seven plain fields.");
            for (var i = 0; i < fields.Length; i++) fields[i] = fields[i].Trim();
            var id = fields[0];
            if (id.Length is < 2 or > 96 || id[0] is < 'A' or > 'Z' ||
                !id.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_') ||
                !seen.Add(id))
                throw new FormatException("Invalid or duplicate achievement ID: " + id);
            var kind = fields[1] switch
            {
                "SCALAR_CANDIDATE" => ProgressEligibility.ScalarCandidate,
                "COMPOSITE_CANDIDATE" => ProgressEligibility.CompositeCandidate,
                "UNLOCK_ONLY" => ProgressEligibility.UnlockOnly,
                _ => throw new FormatException("Invalid progress class for " + id)
            };
            var stat = fields[2];
            if (stat.Length > 96 || stat.Any(c =>
                !(char.IsAsciiLetterOrDigit(c) || c == '_')))
                throw new FormatException("Invalid stat name for " + id);
            var target = 0;
            if (fields[3].Length > 0 &&
                (!int.TryParse(fields[3], NumberStyles.None,
                    CultureInfo.InvariantCulture, out target) || target < 2))
                throw new FormatException("Invalid goal for " + id);
            var evidence = fields[4];
            var profile = fields[5];
            if (evidence.Length > 256 || profile.Length > 128 ||
                evidence.Contains('\n') || profile.Contains('\n'))
                throw new FormatException("Invalid evidence metadata for " + id);
            var enabled = fields[6] switch
            {
                "YES" => true, "NO" => false,
                _ => throw new FormatException("live_enabled must be YES or NO")
            };
            var mapping = new AchievementProgressMapping(
                id, kind, stat, target, evidence, profile, enabled);
            if (enabled && !mapping.IsLiveEligible)
                throw new FormatException(
                    "Enabled progress requires scalar, exact stat, goal, VERIFIED evidence and game profile: " + id);
            rows.Add(mapping);
        }
        if (!header || rows.Count == 0)
            throw new FormatException("Missing progress mapping header or rows.");
        return new AchievementProgressCatalog(rows, source);
    }
}
