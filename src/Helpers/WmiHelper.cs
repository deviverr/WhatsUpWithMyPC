using System.Management;
using WhatsUpWithMyPC.Models;

namespace WhatsUpWithMyPC.Helpers;

public static class WmiHelper
{
    public static CpuInfo GetCpuInfo()
    {
        var info = new CpuInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                info.Name = obj["Name"]?.ToString() ?? "Unknown";
                info.CoreCount = Convert.ToInt32(obj["NumberOfCores"]);
                info.ThreadCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                break;
            }
        }
        catch
        {
            info.Name = "Unknown CPU";
        }

        return info;
    }

    public static MemoryInfo GetMemoryInfo()
    {
        var info = new MemoryInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                // Values are in KB
                var totalKb = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                var freeKb = Convert.ToUInt64(obj["FreePhysicalMemory"]);

                info.TotalBytes = totalKb * 1024;
                info.AvailableBytes = freeKb * 1024;
                info.UsedBytes = info.TotalBytes - info.AvailableBytes;
                info.UsagePercent = info.TotalBytes > 0 ? (double)info.UsedBytes / info.TotalBytes * 100 : 0;
                break;
            }
        }
        catch
        {
            // Default values on error
        }

        return info;
    }

    public static List<DiskInfo> GetDiskInfo()
    {
        var disks = new List<DiskInfo>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    disks.Add(new DiskInfo
                    {
                        DriveLetter = drive.Name.TrimEnd('\\'),
                        Label = drive.VolumeLabel,
                        DriveType = drive.DriveType.ToString(),
                        TotalBytes = (ulong)drive.TotalSize,
                        FreeBytes = (ulong)drive.AvailableFreeSpace
                    });
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return disks;
    }

    public static GpuInfo GetGpuInfo()
    {
        var info = new GpuInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                info.Name = obj["Name"]?.ToString() ?? "Unknown";
                var ram = obj["AdapterRAM"];
                if (ram != null)
                {
                    info.DedicatedMemoryBytes = Convert.ToUInt64(ram);
                }
                break;
            }
        }
        catch
        {
            info.Name = "Unknown GPU";
        }

        return info;
    }

    public static BatteryInfo GetBatteryInfo()
    {
        var info = new BatteryInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining, BatteryStatus FROM Win32_Battery");
            var batteries = searcher.Get();

            if (batteries.Count == 0)
            {
                info.HasBattery = false;
                return info;
            }

            foreach (var obj in batteries)
            {
                info.HasBattery = true;
                info.ChargePercent = Convert.ToDouble(obj["EstimatedChargeRemaining"]);

                var status = Convert.ToInt32(obj["BatteryStatus"]);
                info.IsCharging = status == 2; // 2 = AC Power, charging

                break;
            }

            // Get power plan
            using var powerSearcher = new ManagementObjectSearcher(
                @"root\cimv2\power",
                "SELECT ElementName FROM Win32_PowerPlan WHERE IsActive = True");
            foreach (var obj in powerSearcher.Get())
            {
                info.PowerMode = obj["ElementName"]?.ToString() ?? "Unknown";
                break;
            }
        }
        catch
        {
            info.HasBattery = false;
        }

        return info;
    }

    public static TimeSpan GetSystemUptime()
    {
        try
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount64);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    public static string GetWindowsVersion()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                return $"{obj["Caption"]} ({obj["Version"]})";
            }
        }
        catch
        {
        }

        return "Unknown Windows Version";
    }

    public static DisplayInfo GetDisplayInfo()
    {
        var info = new DisplayInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate, CurrentBitsPerPixel FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                info.Name = obj["Name"]?.ToString() ?? "Unknown Display";
                info.Width = Convert.ToInt32(obj["CurrentHorizontalResolution"] ?? 0);
                info.Height = Convert.ToInt32(obj["CurrentVerticalResolution"] ?? 0);
                info.RefreshRate = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0);
                info.BitsPerPixel = Convert.ToInt32(obj["CurrentBitsPerPixel"] ?? 0);
                break;
            }

            // Get DPI scaling
            try
            {
                using var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
                info.ScalingPercent = Math.Round(graphics.DpiX / 96.0 * 100);
            }
            catch
            {
                info.ScalingPercent = 100;
            }
        }
        catch
        {
            info.Name = "Unknown Display";
        }

        return info;
    }
}
