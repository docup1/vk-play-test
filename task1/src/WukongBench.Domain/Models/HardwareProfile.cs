namespace WukongBench.Domain.Models;

/// <summary>CPU characteristics.</summary>
public sealed class CpuInfo
{
    public CpuInfo(
        string name,
        string manufacturer,
        int physicalCores,
        int logicalProcessors,
        string maxClockMhz,
        string currentClockMhz,
        string architecture)
    {
        Name = name;
        Manufacturer = manufacturer;
        PhysicalCores = physicalCores;
        LogicalProcessors = logicalProcessors;
        MaxClockMhz = maxClockMhz;
        CurrentClockMhz = currentClockMhz;
        Architecture = architecture;
    }

    public string Name { get; }
    public string Manufacturer { get; }
    public int PhysicalCores { get; }
    public int LogicalProcessors { get; }
    public string MaxClockMhz { get; }
    public string CurrentClockMhz { get; }
    public string Architecture { get; }
}

/// <summary>GPU characteristics.</summary>
public sealed class GpuInfo
{
    public GpuInfo(
        string name,
        string vendor,
        string driverVersion,
        string driverDate,
        string adapterRamBytes,
        string videoProcessor,
        string currentResolution,
        string currentRefreshRateHz)
    {
        Name = name;
        Vendor = vendor;
        DriverVersion = driverVersion;
        DriverDate = driverDate;
        AdapterRamBytes = adapterRamBytes;
        VideoProcessor = videoProcessor;
        CurrentResolution = currentResolution;
        CurrentRefreshRateHz = currentRefreshRateHz;
    }

    public string Name { get; }
    public string Vendor { get; }
    public string DriverVersion { get; }
    public string DriverDate { get; }
    public string AdapterRamBytes { get; }
    public string VideoProcessor { get; }
    public string CurrentResolution { get; }
    public string CurrentRefreshRateHz { get; }
}

/// <summary>RAM characteristics.</summary>
public sealed class MemoryInfo
{
    public MemoryInfo(string totalBytes, string modulesSummary, string speedMhz, string pageFileSummary)
    {
        TotalBytes = totalBytes;
        ModulesSummary = modulesSummary;
        SpeedMhz = speedMhz;
        PageFileSummary = pageFileSummary;
    }

    public string TotalBytes { get; }
    public string ModulesSummary { get; }
    public string SpeedMhz { get; }
    public string PageFileSummary { get; }
}

/// <summary>Machine / OS characteristics.</summary>
public sealed class SystemInfo
{
    public SystemInfo(
        string machineName,
        string operatingSystem,
        string osVersion,
        string osBuild,
        string osArchitecture,
        string motherboard,
        string systemManufacturer,
        string systemModel,
        string biosVersion,
        string primaryStorage,
        string activePowerScheme,
        string isVirtualMachine)
    {
        MachineName = machineName;
        OperatingSystem = operatingSystem;
        OsVersion = osVersion;
        OsBuild = osBuild;
        OsArchitecture = osArchitecture;
        Motherboard = motherboard;
        SystemManufacturer = systemManufacturer;
        SystemModel = systemModel;
        BiosVersion = biosVersion;
        PrimaryStorage = primaryStorage;
        ActivePowerScheme = activePowerScheme;
        IsVirtualMachine = isVirtualMachine;
    }

    public string MachineName { get; }
    public string OperatingSystem { get; }
    public string OsVersion { get; }
    public string OsBuild { get; }
    public string OsArchitecture { get; }
    public string Motherboard { get; }
    public string SystemManufacturer { get; }
    public string SystemModel { get; }
    public string BiosVersion { get; }
    public string PrimaryStorage { get; }
    public string ActivePowerScheme { get; }
    public string IsVirtualMachine { get; }
}

/// <summary>
/// Everything the report says about the machine that was tested.
/// </summary>
public sealed class HardwareProfile
{
    public HardwareProfile(SystemInfo system, CpuInfo cpu, GpuInfo gpu, MemoryInfo memory)
    {
        System = system;
        Cpu = cpu;
        Gpu = gpu;
        Memory = memory;
    }

    public SystemInfo System { get; }
    public CpuInfo Cpu { get; }
    public GpuInfo Gpu { get; }
    public MemoryInfo Memory { get; }
}
