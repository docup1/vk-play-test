namespace WukongBench.Domain.Abstractions;


/// <summary>
/// Reads and writes the Unreal Engine user settings file of the benchmark tool
/// (<c>...\Saved\Config\Windows\GameUserSettings.ini</c>). Implementations must
/// preserve every key they do not modify.
/// </summary>
public interface IGraphicsSettingsStore
{
    /// <summary>Full path of the ini file that is being manipulated.</summary>
    string SettingsFilePath { get; }

    /// <summary><c>true</c> when the file already exists on disk.</summary>
    bool Exists { get; }

    /// <summary>Returns every key/value currently present in the file.</summary>
    IReadOnlyList<GraphicsSettingValue> ReadAll();

    /// <summary>Applies a profile (add or overwrite the listed keys only).</summary>
    void Apply(GraphicsSettingsProfile profile);

    /// <summary>Creates a byte-for-byte backup of the current file.</summary>
    bool TryBackup(string backupFilePath, out string message);

    /// <summary>Restores a previously created backup.</summary>
    bool TryRestore(string backupFilePath, out string message);
}

/// <summary>
/// Creates a settings store for a concrete installation. The path of the ini
/// file depends on the located installation, therefore the store cannot be
/// resolved before the installation is known.
/// </summary>
public interface IGraphicsSettingsStoreFactory
{
    IGraphicsSettingsStore Create(SteamInstallation installation);
}
