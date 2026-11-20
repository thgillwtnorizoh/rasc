using System.Globalization;
using System.Text;

namespace Rasc.Core;

/// <summary>
/// Owner-curated RASC rarity metadata. This is NOT Valve's Steam rarity, an original
/// achievement rule, or proof that an API ID was present in a particular Steam schema.
/// </summary>
public enum CuratedRarity { Normal, Rare }

public sealed record CuratedAchievementDefinition(
    string ApiId,
    string IconFilename,
    string IconSha256,
    int Mechanical,
    int Conditions,
    int Consistency,
    int Opportunity,
    int Time,
    bool SeriesCapstone,
    CuratedRarity? RarityOverride)
{
    public int Score => Mechanical + Conditions + Consistency + Opportunity + Time;
    public CuratedRarity Rarity => RarityOverride ??
        (SeriesCapstone || Score >= CuratedAchievementCatalog.RareThreshold
            ? CuratedRarity.Rare : CuratedRarity.Normal);
    public string RarityBasis => RarityOverride.HasValue ? "owner-override"
        : SeriesCapstone && Score < CuratedAchievementCatalog.RareThreshold ? "series-capstone"
        : "five-factor-score";
}

/// <summary>
/// Simple UTF-8 CSV definition store. The shipped file is editable; if it is absent,
/// an embedded copy is used. Invalid edits fail explicitly rather than silently
/// substituting a different catalog. No Valve artwork or localization is bundled.
/// </summary>
public sealed class CuratedAchievementCatalog
{
    public const int RareThreshold = 17;
    public const string RubricVersion = "P006-RARITY-v0.2";
    public const string FileName = "achievement-definitions.v0.2.csv";
    public const string Header =
        "api_id,icon_filename,icon_sha256,m,c,s,o,t,series_capstone,rarity_override";
    private const int MaxCsvCharacters = 262_144;
    private const int MaxRows = 1024;
    private const string ResourceName = "Rasc.Core.Data.achievement-definitions.v0.2.csv";

    private readonly Dictionary<string, CuratedAchievementDefinition> _byId;
    public IReadOnlyList<CuratedAchievementDefinition> Entries { get; }
    public string Source { get; }

    private CuratedAchievementCatalog(List<CuratedAchievementDefinition> entries, string source)
    {
        Entries = entries.AsReadOnly();
        _byId = entries.ToDictionary(e => e.ApiId, StringComparer.Ordinal);
        Source = source;
    }

    /// <summary>
    /// Prefer an explicitly selected CSV or the editable CSV alongside the application.
    /// Only if that file is absent, read the embedded, owner-approved baseline.
    /// This method does not read or write the user's Steam/CS:GO/GC files.
    /// </summary>
    public static CuratedAchievementCatalog LoadDefault(string? csvPath = null)
    {
        string path = csvPath ?? Path.Combine(AppContext.BaseDirectory, "Data", FileName);
        if (File.Exists(path))
        {
            string text = File.ReadAllText(path, new UTF8Encoding(false, true));
            return Parse(text, path);
        }
        if (csvPath is not null)
            throw new FileNotFoundException("Selected achievement definitions CSV does not exist.", path);

        using Stream resource = typeof(CuratedAchievementCatalog).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded RASC achievement catalog is missing.");
        using var reader = new StreamReader(resource, new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: true);
        return Parse(reader.ReadToEnd(), "embedded:" + ResourceName);
    }

    public CuratedAchievementDefinition? Find(string? apiId) =>
        apiId is not null && _byId.TryGetValue(apiId, out var definition)
            ? definition : null;

    public static CuratedAchievementCatalog Parse(string csv, string source = "provided CSV")
    {
        ArgumentNullException.ThrowIfNull(csv);
        if (csv.Length > MaxCsvCharacters)
            throw new FormatException("Achievement CSV exceeds the 256 KiB safety bound.");

        using var reader = new StringReader(csv.TrimStart('\uFEFF'));
        var entries = new List<CuratedAchievementDefinition>();
        var known = new HashSet<string>(StringComparer.Ordinal);
        var headerSeen = false;
        var lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (!headerSeen)
            {
                if (line != Header)
                    throw new FormatException($"Achievement CSV line {lineNumber}: unexpected column header.");
                headerSeen = true;
                continue;
            }
            if (entries.Count >= MaxRows)
                throw new FormatException("Achievement CSV exceeds the 1024-record safety bound.");

            // The schema intentionally contains no free-form text columns:
            // commas and newlines are not legal inside any field.
            var c = line.Split(',');
            if (c.Length != 10 || c.Any(v => v.Contains('"')))
                throw new FormatException($"Achievement CSV line {lineNumber}: expected ten plain fields.");
            for (int i = 0; i < c.Length; i++) c[i] = c[i].Trim();
            string id = c[0], icon = c[1], hash = c[2].ToUpperInvariant();
            if (!ValidId(id))
                throw new FormatException($"Achievement CSV line {lineNumber}: invalid API ID.");
            if (!ValidIcon(icon))
                throw new FormatException($"Achievement CSV line {lineNumber}: unsafe icon filename.");
            if (hash.Length != 64 || !hash.All(IsHex))
                throw new FormatException($"Achievement CSV line {lineNumber}: invalid icon SHA-256.");
            if (!known.Add(id))
                throw new FormatException($"Achievement CSV line {lineNumber}: duplicate API ID {id}.");

            var factors = new int[5];
            for (int i = 0; i < factors.Length; i++)
            {
                if (!int.TryParse(c[i + 3], NumberStyles.None, CultureInfo.InvariantCulture,
                    out factors[i]) || factors[i] is < 0 or > 5)
                    throw new FormatException($"Achievement CSV line {lineNumber}: factor {i + 1} must be 0-5.");
            }
            bool capstone = c[8] switch
            {
                "YES" => true,
                "NO" => false,
                _ => throw new FormatException($"Achievement CSV line {lineNumber}: capstone must be YES or NO.")
            };
            CuratedRarity? rarityOverride = c[9] switch
            {
                "" => null,
                "RARE" => CuratedRarity.Rare,
                "NORMAL" => CuratedRarity.Normal,
                _ => throw new FormatException($"Achievement CSV line {lineNumber}: invalid rarity override.")
            };
            entries.Add(new CuratedAchievementDefinition(id, icon, hash,
                factors[0], factors[1], factors[2], factors[3], factors[4],
                capstone, rarityOverride));
        }
        if (!headerSeen || entries.Count == 0)
            throw new FormatException("Achievement CSV requires its header and at least one definition.");
        return new CuratedAchievementCatalog(entries, source);
    }

    private static bool ValidId(string id) =>
        id.Length is >= 2 and <= 96 && id[0] is >= 'A' and <= 'Z' &&
        id.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');

    private static bool ValidIcon(string name) =>
        name.Length is >= 5 and <= 100 && name.EndsWith(".jpg", StringComparison.Ordinal) &&
        name[..^4].All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    private static bool IsHex(char c) =>
        c is >= '0' and <= '9' or >= 'A' and <= 'F';
}
