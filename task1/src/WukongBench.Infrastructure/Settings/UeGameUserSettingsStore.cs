namespace WukongBench.Infrastructure.Settings;

using System.Text;
using Microsoft.Extensions.Options;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;

/// <summary>
/// <see cref="IGraphicsSettingsStore"/> for the benchmark tool's
/// <c>GameUserSettings.ini</c>.
///
/// The file lives inside the Steam install folder
/// (<c>&lt;install&gt;\b1\Saved\Config\Windows\GameUserSettings.ini</c>), not in
/// <c>%LOCALAPPDATA%</c>. Two channels are written: the standard Unreal Engine
/// keys and the tool's own <c>UISettingData</c> tuple. Everything that is not
/// explicitly changed is preserved byte for byte.
/// </summary>
public sealed class UeGameUserSettingsStore : IGraphicsSettingsStore
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IToolLogger _logger;
    private readonly SettingsStoreOptions _options;

    public UeGameUserSettingsStore(SteamInstallation installation, SettingsStoreOptions options, IToolLogger logger)
    {
        _logger = logger;
        _options = options;
        SettingsFilePath = ResolveSettingsPath(installation, options);
    }

    public string SettingsFilePath { get; }

    public bool Exists => File.Exists(SettingsFilePath);

    public IReadOnlyList<GraphicsSettingValue> ReadAll()
    {
        if (!Exists)
        {
            return Array.Empty<GraphicsSettingValue>();
        }

        var document = IniDocument.Parse(ReadText());
        return document.Entries()
            .Select(entry => new GraphicsSettingValue(entry.Section, entry.Key, entry.Value, string.Empty))
            .ToArray();
    }

    public void Apply(GraphicsSettingsProfile profile)
    {
        var document = Exists
            ? IniDocument.Parse(ReadText())
            : IniDocument.Parse(string.Empty);

        foreach (var setting in profile.Values)
        {
            document.Set(setting.Section, setting.Key, setting.Value);
        }

        if (profile.UiSettings.Count > 0)
        {
            var wukongSection = ResolveWukongSection(document);

            // UISettingData may live in a differently named section on other
            // builds, so an existing occurrence anywhere wins over the default.
            var existing = FindSettingDataSection(document) ?? wukongSection;
            var merged = UiSettingData.Merge(document.Get(existing, WukongUiKey), profile.UiSettings);
            document.Set(existing, WukongUiKey, merged);
            _logger.Debug($"UISettingData written into [{existing}].");
        }

        ClearReadOnly();
        File.WriteAllText(SettingsFilePath, document.ToText(), Utf8NoBom);

        if (_options.LockFileDuringRun)
        {
            SetReadOnly();
        }
    }

    public bool TryBackup(string backupFilePath, out string message)
    {
        if (!Exists)
        {
            message = $"No settings file to back up at '{SettingsFilePath}'.";
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(backupFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(SettingsFilePath, backupFilePath, overwrite: true);
            message = $"Original settings backed up to '{backupFilePath}'.";
            return true;
        }
        catch (Exception exception)
        {
            message = $"Could not back up '{SettingsFilePath}': {exception.Message}";
            return false;
        }
    }

    public bool TryRestore(string backupFilePath, out string message)
    {
        if (!File.Exists(backupFilePath))
        {
            message = $"The backup '{backupFilePath}' does not exist.";
            return false;
        }

        try
        {
            ClearReadOnly();
            File.Copy(backupFilePath, SettingsFilePath, overwrite: true);
            message = $"Restored '{SettingsFilePath}'";
            return true;
        }
        catch (Exception exception)
        {
            message = $"Could not restore '{SettingsFilePath}': {exception.Message}";
            return false;
        }
    }

    private const string WukongUiKey = "UISettingData";

    private static string ResolveWukongSection(IniDocument document) => "[/Script/GSGameSettings.GSGameUserSettings]";

    private static string? FindSettingDataSection(IniDocument document) =>
        document.Entries()
            .FirstOrDefault(entry => entry.Key.Equals(WukongUiKey, StringComparison.OrdinalIgnoreCase))
            .Section;

    private string ReadText()
    {
        try
        {
            return File.ReadAllText(SettingsFilePath);
        }
        catch (IOException exception)
        {
            _logger.Warning($"Could not read '{SettingsFilePath}': {exception.Message}");
            return string.Empty;
        }
    }

    private void ClearReadOnly()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var attributes = File.GetAttributes(SettingsFilePath);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(SettingsFilePath, attributes & ~FileAttributes.ReadOnly);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.Debug($"Could not clear the read-only attribute: {exception.Message}");
        }
    }

    private void SetReadOnly()
    {
        try
        {
            var attributes = File.GetAttributes(SettingsFilePath);
            File.SetAttributes(SettingsFilePath, attributes | FileAttributes.ReadOnly);
            _logger.Info("Settings file locked (read-only) for the duration of the run.");
        }
        catch (Exception exception)
        {
            _logger.Warning($"Could not lock the settings file: {exception.Message}");
        }
    }

    private static string ResolveSettingsPath(SteamInstallation installation, SettingsStoreOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SettingsFileOverride))
        {
            return Path.GetFullPath(options.SettingsFileOverride);
        }

        foreach (var candidate in options.SettingsFileCandidates)
        {
            var full = Path.Combine(installation.InstallDirectory, candidate);
            if (File.Exists(full))
            {
                return full;
            }
        }

        var fallback = Path.Combine(installation.InstallDirectory, options.SettingsFileCandidates.First());
        var directory = Path.GetDirectoryName(fallback);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fallback;
    }
}

/// <summary>Creates <see cref="UeGameUserSettingsStore"/> instances.</summary>
public sealed class UeGameUserSettingsStoreFactory : IGraphicsSettingsStoreFactory
{
    private readonly SettingsStoreOptions _options;
    private readonly IToolLogger _logger;

    public UeGameUserSettingsStoreFactory(IOptions<SettingsStoreOptions> options, IToolLogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IGraphicsSettingsStore Create(SteamInstallation installation) =>
        new UeGameUserSettingsStore(installation, _options, _logger);
}
