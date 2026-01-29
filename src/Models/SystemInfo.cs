namespace WhatsUpWithMyPC.Models;

public class CpuInfo
{
    public string Name { get; set; } = string.Empty;
    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }
    public double UsagePercent { get; set; }
    public double Temperature { get; set; }
}

public class MemoryInfo
{
    public ulong TotalBytes { get; set; }
    public ulong UsedBytes { get; set; }
    public ulong AvailableBytes { get; set; }
    public double UsagePercent { get; set; }

    public double TotalGB => TotalBytes / 1024.0 / 1024.0 / 1024.0;
    public double UsedGB => UsedBytes / 1024.0 / 1024.0 / 1024.0;
    public double AvailableGB => AvailableBytes / 1024.0 / 1024.0 / 1024.0;
}

public class DiskInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public ulong TotalBytes { get; set; }
    public ulong FreeBytes { get; set; }
    public ulong UsedBytes => TotalBytes - FreeBytes;
    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    public double ReadSpeed { get; set; }
    public double WriteSpeed { get; set; }

    public double TotalGB => TotalBytes / 1024.0 / 1024.0 / 1024.0;
    public double FreeGB => FreeBytes / 1024.0 / 1024.0 / 1024.0;
    public double UsedGB => UsedBytes / 1024.0 / 1024.0 / 1024.0;
}

public class GpuInfo
{
    public string Name { get; set; } = string.Empty;
    public ulong DedicatedMemoryBytes { get; set; }
    public double UsagePercent { get; set; }
    public double Temperature { get; set; }

    public double DedicatedMemoryGB => DedicatedMemoryBytes / 1024.0 / 1024.0 / 1024.0;
}

public class BatteryInfo
{
    public bool HasBattery { get; set; }
    public double ChargePercent { get; set; }
    public bool IsCharging { get; set; }
    public string PowerMode { get; set; } = string.Empty;
    public TimeSpan EstimatedRuntime { get; set; }
    public string HealthStatus { get; set; } = "Unknown";
}

public class NetworkInfo
{
    public string AdapterName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public double DownloadSpeedBps { get; set; }
    public double UploadSpeedBps { get; set; }

    public string DownloadSpeedFormatted => FormatSpeed(DownloadSpeedBps);
    public string UploadSpeedFormatted => FormatSpeed(UploadSpeedBps);

    private static string FormatSpeed(double bps)
    {
        if (bps >= 1_000_000_000) return $"{bps / 1_000_000_000:F1} Gbps";
        if (bps >= 1_000_000) return $"{bps / 1_000_000:F1} Mbps";
        if (bps >= 1_000) return $"{bps / 1_000:F1} Kbps";
        return $"{bps:F0} bps";
    }
}

public class SystemStats
{
    public CpuInfo Cpu { get; set; } = new();
    public MemoryInfo Memory { get; set; } = new();
    public List<DiskInfo> Disks { get; set; } = new();
    public GpuInfo Gpu { get; set; } = new();
    public BatteryInfo Battery { get; set; } = new();
    public NetworkInfo Network { get; set; } = new();
    public TimeSpan Uptime { get; set; }
    public DateTime LastUpdated { get; set; }
}
