using System.Text;

namespace Rasc.Core;

/// <summary>A complete name/description pair obtained from a local CS:GO resource file.</summary>
public sealed record CsgoAchievementText(string Name, string Description, string Language);

/// <summary>
/// English and optional requested-language achievement text from the player's installed
/// CS:GO resources. No Valve strings are embedded or written to disk by RASC.
/// A localized pair is NOT proof of an originally shipped achievement or unlock rule.
/// </summary>
public sealed class CsgoAchievementLocalization
{
    private readonly Dictionary<string, CsgoAchievementText> _entries;
    public string EnglishSource { get; }
    public string PreferredLanguage { get; }
    public string? PreferredSource { get; }
    public bool PreferredFileMissing { get; }
    public int Count => _entries.Count;

    private CsgoAchievementLocalization(
        Dictionary<string, CsgoAchievementText> entries, string englishSource,
        string language, string? preferredSource, bool preferredFileMissing)
    {
        _entries = entries;
        EnglishSource = englishSource;
        PreferredLanguage = language;
        PreferredSource = preferredSource;
        PreferredFileMissing = preferredFileMissing;
    }

    public CsgoAchievementText? Find(string? apiId) =>
        apiId is not null && _entries.TryGetValue(apiId, out var value) ? value : null;

    /// <summary>Parse source text without touching the installed game (fixture-friendly).</summary>
    public static CsgoAchievementLocalization Parse(
        string englishKeyValues, string? preferredKeyValues = null,
        string language = "english", string englishSource = "provided english fixture",
        string? preferredSource = null)
    {
        ArgumentNullException.ThrowIfNull(englishKeyValues);
        ValidateLanguage(language);
        var english = ReadAchievementTokens(englishKeyValues);
        var selected = language == "english" || preferredKeyValues is null
            ? english : ReadAchievementTokens(preferredKeyValues);

        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in english.Keys) identifiers.Add(BaseId(key));
        foreach (var key in selected.Keys) identifiers.Add(BaseId(key));

        var result = new Dictionary<string, CsgoAchievementText>(StringComparer.Ordinal);
        foreach (string id in identifiers)
        {
            if (language != "english" && preferredKeyValues is not null &&
                TryPair(selected, id, out string localizedName, out string localizedDescription))
            {
                result.Add(id, new CsgoAchievementText(localizedName, localizedDescription, language));
            }
            else if (TryPair(english, id, out string englishName, out string englishDescription))
            {
                result.Add(id, new CsgoAchievementText(englishName, englishDescription, "english"));
            }
        }
        return new CsgoAchievementLocalization(result, englishSource, language,
            language == "english" || preferredKeyValues is null ? null : preferredSource,
            language != "english" && preferredKeyValues is null);
    }

    /// <summary>
    /// Selects exactly the game's resource/csgo_*.txt files under the selected installation.
    /// Returns explicit missing/invalid/unavailable status; never changes the game directory.
    /// gameRoot can be the directory containing csgo.exe or its csgo/ content directory.
    /// </summary>
    public static CsgoLocalizationReadResult LoadFromGameRoot(
        string gameRoot, string language = "english")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameRoot);
        ValidateLanguage(language);
        string resourceFolder;
        try
        {
            string full = Path.GetFullPath(gameRoot);
            resourceFolder = Path.GetFileName(Path.TrimEndingDirectorySeparator(full))
                .Equals("csgo", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(full, "resource")
                : Path.Combine(full, "csgo", "resource");
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new(ReadStatus.Invalid, null, "Invalid selected CS:GO installation path.");
        }

        string englishPath = Path.Combine(resourceFolder, "csgo_english.txt");
        string requestedPath = Path.Combine(resourceFolder, $"csgo_{language}.txt");
        try
        {
            if (!File.Exists(englishPath))
                return new(ReadStatus.Missing, null, "CS:GO English localization file is missing.");
            string english = ReadLocalText(englishPath);
            string? requested = null;
            string? preferredSource = null;
            if (language != "english" && File.Exists(requestedPath))
            {
                requested = ReadLocalText(requestedPath);
                preferredSource = requestedPath;
            }
            var catalog = Parse(english, requested, language, englishPath, preferredSource);
            return new(ReadStatus.Valid, catalog, null);
        }
        catch (Exception ex) when (ex is FormatException or DecoderFallbackException)
        {
            return new(ReadStatus.Invalid, null, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(ReadStatus.Unavailable, null, "Cannot read CS:GO localization: " + ex.GetType().Name);
        }
    }

    private static string ReadLocalText(string path)
    {
        const int maxBytes = 8 * 1024 * 1024;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length < 2 || stream.Length > maxBytes)
            throw new FormatException("CS:GO localization file has an unsupported size.");
        byte[] data = new byte[(int)stream.Length];
        stream.ReadExactly(data);

        // Legacy Source 1 localization uses UTF-16; permit UTF-8 for independently
        // edited/localized copies. Do not guess ANSI encodings.
        if (data.Length >= 2 && data[0] == 0xff && data[1] == 0xfe)
            return new UnicodeEncoding(false, false, true).GetString(data, 2, data.Length - 2);
        if (data.Length >= 2 && data[0] == 0xfe && data[1] == 0xff)
            return new UnicodeEncoding(true, false, true).GetString(data, 2, data.Length - 2);
        int offset = data.Length >= 3 && data[0] == 0xef && data[1] == 0xbb &&
            data[2] == 0xbf ? 3 : 0;
        return new UTF8Encoding(false, true).GetString(data, offset, data.Length - offset);
    }

    private static bool TryPair(Dictionary<string, string> tokens, string id,
        out string name, out string description)
    {
        name = ""; description = "";
        return tokens.TryGetValue(id + "_NAME", out name!) &&
               tokens.TryGetValue(id + "_DESC", out description!) &&
               !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(description);
    }

    private static string BaseId(string key) => key[..key.LastIndexOf('_')];

    private static void ValidateLanguage(string language)
    {
        if (language.Length is < 2 or > 24 ||
            !language.All(c => c is >= 'a' and <= 'z'))
            throw new ArgumentException("Language must be a lowercase ASCII Steam locale name.", nameof(language));
    }

    private static bool AchievementKey(string key) =>
        (key.EndsWith("_NAME", StringComparison.Ordinal) ||
         key.EndsWith("_DESC", StringComparison.Ordinal)) &&
        key.Length is >= 7 and <= 101 &&
        key[0] is >= 'A' and <= 'Z' &&
        key.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');

    private static Dictionary<string, string> ReadAchievementTokens(string source)
    {
        if (source.Length > 4_194_304)
            throw new FormatException("Decoded CS:GO localization exceeds the 4 Mi-character bound.");

        var reader = new KeyValuesTokens(source.TrimStart('\uFEFF'));
        var matched = new Dictionary<string, string>(StringComparer.Ordinal);
        bool found = false;
        KeyValuesToken? token;
        while ((token = reader.Next()) is not null)
        {
            if (token.Value.Kind != TokenKind.Text ||
                !token.Value.Value.Equals("Tokens", StringComparison.OrdinalIgnoreCase))
                continue;
            var next = reader.Peek();
            if (next?.Kind != TokenKind.Open) continue;
            if (found) throw new FormatException("More than one CS:GO Tokens section.");
            found = true;
            reader.Next(); // consume {
            ReadPairs(reader, matched);
        }
        if (!found) throw new FormatException("CS:GO localization contains no Tokens section.");
        return matched;
    }

    private static void ReadPairs(KeyValuesTokens reader, Dictionary<string, string> result)
    {
        KeyValuesToken? key;
        while ((key = reader.Next()) is not null)
        {
            if (key.Value.Kind == TokenKind.Close) return;
            if (key.Value.Kind != TokenKind.Text)
                throw new FormatException("Unexpected brace inside localization Tokens section.");

            // Source KeyValues may append an unquoted [condition] after an entry.
            if (key.Value.Value.StartsWith('[') && key.Value.Value.EndsWith(']'))
                continue;

            var value = reader.Next() ??
                throw new FormatException("Localization token without a value.");
            if (value.Kind == TokenKind.Open)
            {
                SkipNested(reader);
                continue;
            }
            if (value.Kind != TokenKind.Text)
                throw new FormatException("Malformed localization token value.");

            string name = key.Value.Value;
            if (!AchievementKey(name)) continue;
            if (value.Value.Length > 16_384)
                throw new FormatException("Achievement description exceeds safety limit.");
            // Original 2023 CS:GO localization intentionally repeats several
            // achievement _DESC keys with revised 5v5 wording. Source KeyValues
            // locality is ordered: a later definition overrides the earlier one.
            // Rejecting duplicate text made the ENTIRE ~3.2M-character source
            // unavailable and caused live popups to display only raw API IDs.
            result[name] = value.Value;
        }
        throw new FormatException("Unterminated localization Tokens section.");
    }

    private static void SkipNested(KeyValuesTokens reader)
    {
        int depth = 1;
        while (depth > 0)
        {
            var token = reader.Next() ??
                throw new FormatException("Unterminated nested localization section.");
            if (token.Kind == TokenKind.Open) depth++;
            if (token.Kind == TokenKind.Close) depth--;
            if (depth > 16) throw new FormatException("Localization nesting exceeds limit.");
        }
    }

    private enum TokenKind { Text, Open, Close }
    private readonly record struct KeyValuesToken(TokenKind Kind, string Value);

    /// <summary>Small KeyValues lexer: comments, quoted values, braces and common Source escapes.</summary>
    private sealed class KeyValuesTokens(string text)
    {
        private int _position;
        private KeyValuesToken? _peeked;

        public KeyValuesToken? Peek() => _peeked ??= Read();
        public KeyValuesToken? Next()
        {
            if (_peeked is not null)
            {
                var result = _peeked;
                _peeked = null;
                return result;
            }
            return Read();
        }

        private KeyValuesToken? Read()
        {
            while (_position < text.Length)
            {
                if (char.IsWhiteSpace(text[_position])) { _position++; continue; }
                if (_position + 1 < text.Length &&
                    text[_position] == '/' && text[_position + 1] == '/')
                {
                    _position += 2;
                    while (_position < text.Length && text[_position] != '\n') _position++;
                    continue;
                }
                if (_position + 1 < text.Length &&
                    text[_position] == '/' && text[_position + 1] == '*')
                {
                    _position += 2;
                    int end = text.IndexOf("*/", _position, StringComparison.Ordinal);
                    if (end < 0) throw new FormatException("Unterminated KeyValues block comment.");
                    _position = end + 2;
                    continue;
                }
                break;
            }
            if (_position == text.Length) return null;
            char first = text[_position++];
            if (first == '{') return new(TokenKind.Open, "{");
            if (first == '}') return new(TokenKind.Close, "}");
            if (first == '"')
            {
                var value = new StringBuilder();
                while (_position < text.Length)
                {
                    char c = text[_position++];
                    if (c == '"') return new(TokenKind.Text, value.ToString());
                    // Source 1 csgo_english.txt has physical newlines inside
                    // quoted values (22 in the April 2023 archived source).
                    // Preserve them as one normalized line break.
                    if (c is '\r' or '\n')
                    {
                        if (c == '\r' && _position < text.Length &&
                            text[_position] == '\n') _position++;
                        value.Append('\n');
                        continue;
                    }
                    if (c == '\\' && _position < text.Length)
                    {
                        char escaped = text[_position++];
                        switch (escaped)
                        {
                            case '"': value.Append('"'); break;
                            case '\\': value.Append('\\'); break;
                            case 'n': value.Append('\n'); break;
                            case 'r': value.Append('\r'); break;
                            case 't': value.Append('\t'); break;
                            default: value.Append('\\').Append(escaped); break;
                        }
                    }
                    else value.Append(c);
                }
                throw new FormatException("Unterminated quoted KeyValues string.");
            }

            int start = _position - 1;
            while (_position < text.Length && !char.IsWhiteSpace(text[_position]) &&
                   text[_position] != '{' && text[_position] != '}' &&
                   text[_position] != '"')
                _position++;
            return new(TokenKind.Text, text[start.._position]);
        }
    }
}

public sealed record CsgoLocalizationReadResult(
    ReadStatus Status, CsgoAchievementLocalization? Catalog, string? Detail);

/// <summary>
/// Strict read-only join; a missing text pair stays unknown. Never infer award
/// conditions or treat localization-only entries as canonical achievements.
/// </summary>
public sealed record ResolvedAchievementDefinition(
    CuratedAchievementDefinition Definition, string? Name,
    string? Description, string? Language)
{
    public bool HasLocalText => Name is not null && Description is not null;
}

public static class CsgoAchievementDefinitionJoin
{
    public static IReadOnlyList<ResolvedAchievementDefinition> Join(
        CuratedAchievementCatalog definitions, CsgoAchievementLocalization texts)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(texts);
        return definitions.Entries.Select(definition =>
        {
            var text = texts.Find(definition.ApiId);
            return new ResolvedAchievementDefinition(definition,
                text?.Name, text?.Description, text?.Language);
        }).ToArray();
    }
}
