namespace WukongBench.Infrastructure.Settings;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Line oriented Unreal Engine <c>.ini</c> document. Every line that is not
/// touched keeps its original text, so comments, unknown keys and the exact
/// layout of the benchmark tool's settings file are preserved.
/// </summary>
internal sealed class IniDocument
{
    private static readonly Regex SectionRegex = new(@"^\s*\[(?<name>[^\]]+)\]\s*$", RegexOptions.Compiled);
    private static readonly Regex KeyValueRegex = new(@"^\s*(?<key>[^=;\r\n]+?)\s*=(?<value>.*)$", RegexOptions.Compiled);

    private readonly List<IniLine> _lines;

    private IniDocument(List<IniLine> lines)
    {
        _lines = lines;
    }

    public static IniDocument Parse(string text)
    {
        var lines = new List<IniLine>();
        string currentSection = string.Empty;

        foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var sectionMatch = SectionRegex.Match(raw);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups["name"].Value.Trim();
                lines.Add(IniLine.CreateSection(raw, currentSection));
                continue;
            }

            var keyValueMatch = KeyValueRegex.Match(raw);
            if (keyValueMatch.Success && !raw.TrimStart().StartsWith(';') && !raw.TrimStart().StartsWith('#'))
            {
                lines.Add(IniLine.CreateKeyValue(
                    raw,
                    currentSection,
                    keyValueMatch.Groups["key"].Value.Trim(),
                    keyValueMatch.Groups["value"].Value.Trim()));
                continue;
            }

            lines.Add(IniLine.CreateOther(raw));
        }

        return new IniDocument(lines);
    }

    public string ToText()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _lines.Count; i++)
        {
            builder.Append(_lines[i].ToText());
            if (i < _lines.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    public IEnumerable<(string Section, string Key, string Value)> Entries()
    {
        foreach (var line in _lines)
        {
            if (line.Kind == IniLineKind.KeyValue)
            {
                yield return (line.Section, line.Key!, line.Value!);
            }
        }
    }

    public string? Get(string section, string key)
    {
        foreach (var line in _lines)
        {
            if (line.Kind == IniLineKind.KeyValue &&
                string.Equals(line.Section, section, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return line.Value;
            }
        }

        return null;
    }

    /// <summary>Sets a key, creating the section and/or the key when needed.</summary>
    public void Set(string section, string key, string value)
    {
        var lastSectionLineIndex = -1;
        var sectionFound = false;

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            if (line.Kind == IniLineKind.Section &&
                string.Equals(line.Section, section, StringComparison.OrdinalIgnoreCase))
            {
                sectionFound = true;
                lastSectionLineIndex = i;
                continue;
            }

            if (sectionFound)
            {
                if (line.Kind == IniLineKind.Section)
                {
                    break;
                }

                if (line.Kind == IniLineKind.KeyValue &&
                    string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    line.SetValue(value);
                    return;
                }

                lastSectionLineIndex = i;
            }
        }

        var newLine = IniLine.CreateKeyValue($"{key}={value}", section, key, value);

        if (!sectionFound)
        {
            if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines[^1].Raw))
            {
                _lines.Add(IniLine.CreateOther(string.Empty));
            }

            _lines.Add(IniLine.CreateSection($"[{section}]", section));
            _lines.Add(newLine);
            return;
        }

        _lines.Insert(lastSectionLineIndex + 1, newLine);
    }

    public sealed class IniLine
    {
        private IniLine(IniLineKind kind, string raw, string section, string? key, string? value)
        {
            Kind = kind;
            Raw = raw;
            Section = section;
            Key = key;
            Value = value;
        }

        public IniLineKind Kind { get; }

        public string Raw { get; private set; }

        public string Section { get; }

        public string? Key { get; }

        public string? Value { get; private set; }

        public static IniLine CreateSection(string raw, string section) =>
            new(IniLineKind.Section, raw, section, null, null);

        public static IniLine CreateKeyValue(string raw, string section, string key, string value) =>
            new(IniLineKind.KeyValue, raw, section, key, value);

        public static IniLine CreateOther(string raw) =>
            new(IniLineKind.Other, raw, string.Empty, null, null);

        public void SetValue(string value)
        {
            Value = value;
            Raw = $"{Key}={value}";
        }

        public string ToText() => Kind == IniLineKind.KeyValue ? $"{Key}={Value}" : Raw;
    }

    public enum IniLineKind
    {
        Other,
        Section,
        KeyValue,
    }
}
