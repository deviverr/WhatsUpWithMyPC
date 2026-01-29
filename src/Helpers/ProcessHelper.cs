using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WhatsUpWithMyPC.Helpers;

public static class ProcessHelper
{
    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

    public static List<ProcessInfo> GetTopMemoryProcesses(int count = 10)
    {
        var processes = new List<ProcessInfo>();

        try
        {
            var allProcesses = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .Take(count);

            foreach (var process in allProcesses)
            {
                try
                {
                    processes.Add(new ProcessInfo
                    {
                        Id = process.Id,
                        Name = process.ProcessName,
                        MemoryBytes = process.WorkingSet64,
                        CpuTime = process.TotalProcessorTime
                    });
                }
                catch
                {
                    // Skip processes we can't access
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return processes;
    }

    public static List<ProcessInfo> GetTopCpuProcesses(int count = 10)
    {
        var processes = new List<ProcessInfo>();

        try
        {
            var allProcesses = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .OrderByDescending(p =>
                {
                    try { return p.TotalProcessorTime.TotalMilliseconds; }
                    catch { return 0; }
                })
                .Take(count);

            foreach (var process in allProcesses)
            {
                try
                {
                    processes.Add(new ProcessInfo
                    {
                        Id = process.Id,
                        Name = process.ProcessName,
                        MemoryBytes = process.WorkingSet64,
                        CpuTime = process.TotalProcessorTime
                    });
                }
                catch
                {
                    // Skip processes we can't access
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return processes;
    }

    public static bool KillProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ClearStandbyMemory()
    {
        try
        {
            // Clear working set of current process
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
        }
        catch
        {
            // Requires elevated privileges
        }
    }

    public static List<StartupItem> GetStartupItems()
    {
        var items = new List<StartupItem>();

        try
        {
            // Check Run registry key
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            if (key != null)
            {
                foreach (var name in key.GetValueNames())
                {
                    items.Add(new StartupItem
                    {
                        Name = name,
                        Path = key.GetValue(name)?.ToString() ?? "",
                        Location = "Registry (Current User)",
                        IsEnabled = true
                    });
                }
            }

            // Check LocalMachine Run key
            using var lmKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            if (lmKey != null)
            {
                foreach (var name in lmKey.GetValueNames())
                {
                    items.Add(new StartupItem
                    {
                        Name = name,
                        Path = lmKey.GetValue(name)?.ToString() ?? "",
                        Location = "Registry (Local Machine)",
                        IsEnabled = true
                    });
                }
            }
        }
        catch
        {
            // Return what we have
        }

        return items;
    }

    public static bool DisableStartupItem(StartupItem item)
    {
        try
        {
            var keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            var root = item.Location.Contains("Local Machine")
                ? Microsoft.Win32.Registry.LocalMachine
                : Microsoft.Win32.Registry.CurrentUser;

            using var key = root.OpenSubKey(keyPath, writable: true);
            if (key != null)
            {
                key.DeleteValue(item.Name, throwOnMissingValue: false);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }
}

public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long MemoryBytes { get; set; }
    public TimeSpan CpuTime { get; set; }

    public string MemoryFormatted
    {
        get
        {
            if (MemoryBytes >= 1_073_741_824) return $"{MemoryBytes / 1_073_741_824.0:F1} GB";
            if (MemoryBytes >= 1_048_576) return $"{MemoryBytes / 1_048_576.0:F1} MB";
            if (MemoryBytes >= 1_024) return $"{MemoryBytes / 1_024.0:F1} KB";
            return $"{MemoryBytes} B";
        }
    }
}

public class StartupItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
