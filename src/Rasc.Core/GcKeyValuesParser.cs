using System.Globalization;

namespace Rasc.Core;

/// <summary>
/// Strict read-only parser for GT-610's text KeyValues output. Deliberately rejects malformed,
/// ambiguous and duplicate data rather than manufacturing an empty achievement snapshot.
/// The source writer emits top-level "version", "integer_stats", "float_stats" and
/// "achievements" entries WITHOUT a "user_stats" wrapper.
/// </summary>
public static class GcKeyValuesParser
{
    public static GcSnapshot Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 2_097_152)
            throw new FormatException("GT-610 statistics exceed the 2 MiB safety bound.");
        var pairs = new Reader(text.TrimStart('\uFEFF')).ReadPairs(false, 0);
        if (!pairs.TryGetValue("version", out var ver) || ver is not string version ||
            !uint.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed != 1)
            throw new FormatException("Expected GT-610 local achievement format version 1.");

        return new GcSnapshot(
            ParseSection("integer_stats", static value =>
                int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n)
                    ? n : throw new FormatException("Invalid signed integer statistic."), pairs),
            ParseSection("float_stats", static value =>
            {
                if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var bits))
                    throw new FormatException("Invalid float bit-pattern statistic.");
                return BitConverter.Int32BitsToSingle(unchecked((int)bits));
            }, pairs),
            ParseSection("achievements", static value =>
                uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var n)
                    ? n : throw new FormatException("Invalid achievement timestamp."), pairs));
    }

    private static Dictionary<string, T> ParseSection<T>(
        string name, Func<string, T> parse, Dictionary<string, object> root)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        if (!root.TryGetValue(name, out var section))
            return result;
        if (section is not Dictionary<string, object> values)
            throw new FormatException($"Section {name} must contain an object.");
        foreach (var (key, value) in values)
        {
            if (key.Length is < 1 or > 127 || key.Any(c => c < ' ' || c == '"'))
                throw new FormatException($"Invalid {name} key.");
            if (value is not string str)
                throw new FormatException($"Nested {name} values are unsupported.");
            try { result.Add(key, parse(str)); }
            catch (FormatException ex) { throw new FormatException($"{name}/{key}: {ex.Message}", ex); }
        }
        return result;
    }

    private sealed class Reader(string text)
    {
        private int _pos;

        public Dictionary<string, object> ReadPairs(bool nested, int depth)
        {
            if (depth > 8) throw new FormatException("KeyValues nesting exceeds limit.");
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            while (true)
            {
                SkipTrivia();
                if (_pos >= text.Length)
                {
                    if (nested) throw new FormatException("Missing closing brace.");
                    return values;
                }
                if (text[_pos] == '}')
                {
                    if (!nested) throw new FormatException("Unexpected closing brace.");
                    _pos++;
                    return values;
                }
                var key = ReadQuoted();
                SkipTrivia();
                if (_pos >= text.Length) throw new FormatException("Missing value.");
                object value;
                if (text[_pos] == '"') value = ReadQuoted();
                else if (text[_pos] == '{')
                {
                    _pos++;
                    value = ReadPairs(true, depth + 1);
                }
                else throw new FormatException("Expected quoted value or opening brace.");
                if (!values.TryAdd(key, value))
                    throw new FormatException($"Duplicate KeyValues key '{key}'.");
            }
        }

        private string ReadQuoted()
        {
            if (_pos >= text.Length || text[_pos] != '"')
                throw new FormatException("Expected a quoted KeyValues string.");
            var start = ++_pos;
            while (_pos < text.Length && text[_pos] != '"') _pos++;
            if (_pos >= text.Length) throw new FormatException("Unterminated quoted string.");
            var result = text[start.._pos];
            _pos++;
            return result;
        }

        private void SkipTrivia()
        {
            while (_pos < text.Length)
            {
                if (char.IsWhiteSpace(text[_pos])) { _pos++; continue; }
                if (_pos + 1 < text.Length && text[_pos] == '/' && text[_pos + 1] == '/')
                {
                    _pos += 2;
                    while (_pos < text.Length && text[_pos] != '\n') _pos++;
                    continue;
                }
                break;
            }
        }
    }
}
