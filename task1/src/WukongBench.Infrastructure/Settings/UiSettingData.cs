namespace WukongBench.Infrastructure.Settings;

using System.Text;
using System.Text.RegularExpressions;
using WukongBench.Domain.Models;

/// <summary>
/// Reader/writer for the benchmark tool's own <c>UISettingData</c> tuple, which
/// looks like <c>(("ScreenMode", "1"),("QualityLevel", "5"),...)</c>.
/// </summary>
internal static class UiSettingData
{
    private static readonly Regex EntryRegex = new(
        @"\(\s*""(?<key>[^""]*)""\s*,\s*""(?<value>[^""]*)""\s*\)",
        RegexOptions.Compiled);

    /// <summary>Parses the tuple into an ordered list of key/value pairs.</summary>
    public static List<KeyValuePair<string, string>> Parse(string? value)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (Match match in EntryRegex.Matches(value))
        {
            result.Add(new KeyValuePair<string, string>(
                match.Groups["key"].Value,
                match.Groups["value"].Value));
        }

        return result;
    }

    /// <summary>
    /// Merges the requested values into the tuple: existing keys are updated in
    /// place (their position is kept), unknown keys are preserved and missing
    /// keys are appended.
    /// </summary>
    public static string Merge(string? original, IReadOnlyList<UISettingValue> updates)
    {
        var entries = Parse(original);

        foreach (var update in updates)
        {
            var index = entries.FindIndex(entry =>
                string.Equals(entry.Key, update.Key, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                entries[index] = new KeyValuePair<string, string>(entries[index].Key, update.Value);
            }
            else
            {
                entries.Add(new KeyValuePair<string, string>(update.Key, update.Value));
            }
        }

        var builder = new StringBuilder();
        builder.Append('(');
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append($"(\"{entries[i].Key}\", \"{entries[i].Value}\")");
        }

        builder.Append(')');
        return builder.ToString();
    }
}
