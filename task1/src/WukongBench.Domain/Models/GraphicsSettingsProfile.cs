namespace WukongBench.Domain.Models;

/// <summary>
/// One key/value pair destined for an Unreal Engine <c>.ini</c> file.
/// </summary>
public sealed class GraphicsSettingValue
{
    public GraphicsSettingValue(string section, string key, string value, string meaning)
    {
        Section = section;
        Key = key;
        Value = value;
        Meaning = meaning;
    }

    /// <summary>Ini section, for example <c>[/Script/Engine.GameUserSettings]</c> or <c>[ScalabilityGroups]</c>.</summary>
    public string Section { get; }

    /// <summary>Ini key inside the section.</summary>
    public string Key { get; }

    /// <summary>Raw value as written to the file (invariant culture).</summary>
    public string Value { get; }

    /// <summary>Human readable explanation, used by the report and the CLI dump.</summary>
    public string Meaning { get; }
}

/// <summary>
/// One entry of the benchmark tool's own <c>UISettingData</c> tuple, for example
/// <c>("QualityLevel", "5")</c>. This is the channel the in-game graphics menu
/// reads, so it has to be written in addition to the standard UE keys.
/// </summary>
public sealed class UISettingValue
{
    public UISettingValue(string key, string value, string meaning)
    {
        Key = key;
        Value = value;
        Meaning = meaning;
    }

    /// <summary>Name inside <c>UISettingData</c>, e.g. <c>QualityLevel</c>.</summary>
    public string Key { get; }

    /// <summary>Value as it appears in the game settings menu (mostly 1..5, booleans as 0/1).</summary>
    public string Value { get; }

    /// <summary>Human readable explanation.</summary>
    public string Meaning { get; }
}

/// <summary>
/// A named, complete set of graphics settings that will be written to the
/// benchmark tool's user settings file before a pass is started.
/// </summary>
public sealed class GraphicsSettingsProfile
{
    public GraphicsSettingsProfile(
        string name,
        string rationale,
        IReadOnlyList<GraphicsSettingValue> values,
        IReadOnlyList<UISettingValue> uiSettings)
    {
        Name = name;
        Rationale = rationale;
        Values = values;
        UiSettings = uiSettings;
    }

    /// <summary>Short name, e.g. <c>cpu-bound</c>.</summary>
    public string Name { get; }

    /// <summary>Why these values were chosen (printed in the report).</summary>
    public string Rationale { get; }

    /// <summary>Standard ini keys to write (UE sections).</summary>
    public IReadOnlyList<GraphicsSettingValue> Values { get; }

    /// <summary>Entries to merge into the <c>UISettingData</c> tuple of the tool's own section.</summary>
    public IReadOnlyList<UISettingValue> UiSettings { get; }
}
