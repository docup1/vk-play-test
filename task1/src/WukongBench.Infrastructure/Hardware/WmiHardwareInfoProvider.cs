namespace WukongBench.Infrastructure.Hardware;

using System.Globalization;
using System.Management;
using Microsoft.Win32;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;

/// <summary>
/// Reads the machine characteristics through WMI, with a registry fallback for
/// the video memory (WMI's <c>AdapterRAM</c> is a 32 bit value and therefore
/// saturates at 4 GB).
/// </summary>
public sealed class WmiHardwareInfoProvider : IHardwareInfoProvider
{
    private const string GpuClassKey =
        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    private readonly IPowerSchemeManager _powerSchemeManager;
    private readonly IToolLogger _logger;

    public WmiHardwareInfoProvider(IPowerSchemeManager powerSchemeManager, IToolLogger logger)
    {
        _powerSchemeManager = powerSchemeManager;
        _logger = logger;
    }

    public async Task<HardwareProfile> ReadAsync(CancellationToken cancellationToken)
    {
        var system = ReadSystem();
        var cpu = ReadCpu();
        var gpu = ReadGpu();
        var memory = ReadMemory();
        var powerScheme = await ReadPowerSchemeAsync(cancellationToken);

        var systemWithPower = new SystemInfo(
            system.MachineName,
            system.OperatingSystem,
            system.OsVersion,
            system.OsBuild,
            system.OsArchitecture,
            system.Motherboard,
            system.SystemManufacturer,
            system.SystemModel,
            system.BiosVersion,
            system.PrimaryStorage,
            powerScheme,
            system.IsVirtualMachine);

        return new HardwareProfile(systemWithPower, cpu, gpu, memory);
    }

    private async Task<string> ReadPowerSchemeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var name = await _powerSchemeManager.GetActiveSchemeNameAsync(cancellationToken);
            var guid = await _powerSchemeManager.GetActiveSchemeGuidAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(name) ? guid : $"{name} ({guid})";
        }
        catch (Exception exception)
        {
            _logger.Debug($"Power scheme lookup failed: {exception.Message}");
            return string.Empty;
        }
    }

    private SystemInfo ReadSystem()
    {
        var machineName = Environment.MachineName;
        var os = First("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem");
        var board = First("SELECT Manufacturer, Product FROM Win32_BaseBoard");
        var computer = First("SELECT Manufacturer, Model, TotalPhysicalMemory, HypervisorPresent FROM Win32_ComputerSystem");
        var bios = First("SELECT SMBIOSBIOSVersion FROM Win32_BIOS");

        var manufacturer = Get(computer, "Manufacturer");
        var model = Get(computer, "Model");
        var hypervisor = GetBool(computer, "HypervisorPresent");
        var isVirtual = hypervisor || LooksVirtual(manufacturer) || LooksVirtual(model);

        return new SystemInfo(
            machineName,
            Get(os, "Caption"),
            Get(os, "Version"),
            Get(os, "BuildNumber"),
            Get(os, "OSArchitecture"),
            $"{Get(board, "Manufacturer")} {Get(board, "Product")}".Trim(),
            Get(computer, "Manufacturer"),
            Get(computer, "Model"),
            Get(bios, "SMBIOSBIOSVersion"),
            ReadPrimaryStorage(),
            string.Empty,
            isVirtual ? "yes" : "no");
    }

    private CpuInfo ReadCpu()
    {
        var cpu = First("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, CurrentClockSpeed, DataWidth FROM Win32_Processor");
        var cores = GetInt(cpu, "NumberOfCores");
        var logical = GetInt(cpu, "NumberOfLogicalProcessors");
        if (logical == 0)
        {
            logical = Environment.ProcessorCount;
        }

        return new CpuInfo(
            Get(cpu, "Name"),
            Get(cpu, "Manufacturer"),
            cores,
            logical,
            $"{GetInt(cpu, "MaxClockSpeed")} MHz",
            $"{GetInt(cpu, "CurrentClockSpeed")} MHz",
            $"{GetInt(cpu, "DataWidth")}-bit");
    }

    private GpuInfo ReadGpu()
    {
        var controllers = Query("SELECT Name, AdapterCompatibility, DriverVersion, DriverDate, AdapterRAM, VideoProcessor, VideoModeDescription, CurrentRefreshRate FROM Win32_VideoController")
            .ToArray();

        var pick = controllers
            .OrderByDescending(controller => !IsSoftwareAdapter(Get(controller, "Name")))
            .ThenByDescending(controller => GetLong(controller, "AdapterRAM"))
            .FirstOrDefault();

        if (pick is null)
        {
            return new GpuInfo("unknown", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var adapterRam = GetLong(pick, "AdapterRAM");
        var registryVram = ReadVideoMemoryFromRegistry();
        if (registryVram > adapterRam)
        {
            adapterRam = registryVram;
        }

        var mode = Get(pick, "VideoModeDescription");
        var refresh = GetInt(pick, "CurrentRefreshRate");

        return new GpuInfo(
            Get(pick, "Name"),
            Get(pick, "AdapterCompatibility"),
            Get(pick, "DriverVersion"),
            FormatDriverDate(Get(pick, "DriverDate")),
            adapterRam > 0 ? $"{adapterRam} bytes ({adapterRam / 1024.0 / 1024.0 / 1024.0:0.#} GB)" : "unknown",
            Get(pick, "VideoProcessor"),
            mode,
            refresh > 0 ? refresh.ToString(CultureInfo.InvariantCulture) : string.Empty);
    }

    private MemoryInfo ReadMemory()
    {
        var computer = First("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
        var total = GetLong(computer, "TotalPhysicalMemory");

        var modules = Query("SELECT Capacity, Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory").ToArray();
        var summary = modules.Length == 0
            ? "unknown"
            : string.Join(" + ", modules.Select(module =>
                $"{GetLong(module, "Capacity") / 1024.0 / 1024.0 / 1024.0:0} GB" +
                (string.IsNullOrWhiteSpace(Get(module, "PartNumber")) ? string.Empty : $" {Get(module, "PartNumber").Trim()}") +
                (GetInt(module, "Speed") > 0 ? $" @ {GetInt(module, "Speed")} MHz" : string.Empty)).Distinct());

        var speeds = modules.Select(module => GetInt(module, "Speed")).Where(speed => speed > 0).Distinct().ToArray();
        var pageFile = Query("SELECT AllocatedBaseSize FROM Win32_PageFileUsage").ToArray();
        var pageSummary = pageFile.Length == 0
            ? "unknown"
            : string.Join(", ", pageFile.Select(entry => $"{GetLong(entry, "AllocatedBaseSize")} MB"));

        return new MemoryInfo(
            total > 0 ? $"{total} bytes ({total / 1024.0 / 1024.0 / 1024.0:0.#} GB)" : "unknown",
            summary,
            speeds.Length > 0 ? string.Join("/", speeds) : string.Empty,
            pageSummary);
    }

    private string ReadPrimaryStorage()
    {
        var disk = First("SELECT Model, Size, InterfaceType, MediaType, Index FROM Win32_DiskDrive ORDER BY Index");
        if (disk is null)
        {
            return "unknown";
        }

        var size = GetLong(disk, "Size");
        var model = Get(disk, "Model");
        var media = Get(disk, "MediaType");
        var interfaceType = Get(disk, "InterfaceType");
        var sizeText = size > 0 ? $" ({size / 1024.0 / 1024.0 / 1024.0:0} GB)" : string.Empty;

        return $"{model}{sizeText}, {media}, {interfaceType}".Trim(' ', ',');
    }

    /// <summary>64 bit video memory from the display class registry key (WMI caps at 4 GB).</summary>
    private long ReadVideoMemoryFromRegistry()
    {
        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(GpuClassKey);
            if (classKey is null)
            {
                return 0;
            }

            foreach (var subKeyName in classKey.GetSubKeyNames().OrderBy(name => name, StringComparer.Ordinal))
            {
                using var subKey = classKey.OpenSubKey(subKeyName);
                if (subKey is null)
                {
                    continue;
                }

                if (subKey.GetValue("HardwareInformation.qwMemorySize") is long qw && qw > 0)
                {
                    return qw;
                }

                if (subKey.GetValue("HardwareInformation.MemorySize") is byte[] bytes && bytes.Length == 8)
                {
                    return BitConverter.ToInt64(bytes, 0);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.Debug($"Registry video memory lookup failed: {exception.Message}");
        }

        return 0;
    }

    private ManagementObject? First(string query) => Query(query).FirstOrDefault();

    private IEnumerable<ManagementObject> Query(string wql)
    {
        ManagementObjectSearcher? searcher = null;
        ManagementObjectCollection? collection = null;

        try
        {
            searcher = new ManagementObjectSearcher(wql);
            collection = searcher.Get();
        }
        catch (Exception exception)
        {
            // WMI is unavailable (non Windows host, service down, no rights).
            // The report then shows "unknown" instead of failing the whole run.
            _logger.Debug($"WMI query failed ({wql}): {exception.Message}");
        }

        if (collection is not null)
        {
            foreach (var item in collection)
            {
                if (item is ManagementObject managementObject)
                {
                    yield return managementObject;
                }
            }

            collection.Dispose();
        }

        searcher?.Dispose();
    }

    private static string Get(ManagementObject? target, string property)
    {
        if (target is null)
        {
            return string.Empty;
        }

        try
        {
            return target[property]?.ToString()?.Trim() ?? string.Empty;
        }
        catch (ManagementException)
        {
            return string.Empty;
        }
    }

    private static int GetInt(ManagementObject? target, string property) =>
        int.TryParse(Get(target, property), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static long GetLong(ManagementObject? target, string property) =>
        long.TryParse(Get(target, property), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static bool GetBool(ManagementObject? target, string property) =>
        bool.TryParse(Get(target, property), out var value) && value;

    private static bool IsSoftwareAdapter(string name) =>
        name.Contains("Basic Render", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Remote Display", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Virtual", StringComparison.OrdinalIgnoreCase);

    private static bool LooksVirtual(string value) =>
        value.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("QEMU", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("KVM", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Parallels", StringComparison.OrdinalIgnoreCase);

    private static string FormatDriverDate(string value)
    {
        // WMI returns CIM_DATETIME, e.g. 20240515000000.000000-000
        if (value.Length >= 8 &&
            DateTime.TryParseExact(value[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return value;
    }
}
