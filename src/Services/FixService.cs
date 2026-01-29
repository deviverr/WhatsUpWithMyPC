using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using WhatsUpWithMyPC.Helpers;

namespace WhatsUpWithMyPC.Services;

public class FixService
{
    public event EventHandler<string>? StatusChanged;

    #region Shutdown Issues

    public async Task<bool> FixShutdownIssuesAsync()
    {
        StatusChanged?.Invoke(this, "Attempting to fix shutdown issues...");

        var success = true;

        // Kill hung processes
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c taskkill /f /im \"Not Responding\" 2>nul",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }
        catch
        {
            success = false;
        }

        // Reset power configuration
        try
        {
            await RunCommandAsync("powercfg", "/restoredefaultschemes");
        }
        catch
        {
            success = false;
        }

        StatusChanged?.Invoke(this, success ? "Shutdown issues fixed" : "Some fixes failed");
        return success;
    }

    #endregion

    #region Battery Drain

    public async Task<List<string>> AnalyzeBatteryDrainAsync()
    {
        var suggestions = new List<string>();

        // Get power-hungry processes
        var processes = ProcessHelper.GetTopCpuProcesses(5);
        foreach (var proc in processes)
        {
            if (proc.CpuTime.TotalMinutes > 10)
            {
                suggestions.Add($"High CPU usage: {proc.Name} (using significant CPU time)");
            }
        }

        // Check power plan
        try
        {
            var powerPlan = await RunCommandAsync("powercfg", "/getactivescheme");
            if (powerPlan.Contains("High performance"))
            {
                suggestions.Add("You are using 'High Performance' power plan - consider switching to 'Balanced'");
            }
        }
        catch { }

        // Check screen brightness (conceptual - would need additional API)
        suggestions.Add("Consider reducing screen brightness");
        suggestions.Add("Disable Bluetooth and WiFi when not in use");

        return suggestions;
    }

    public async Task<bool> OptimizeBatteryAsync()
    {
        StatusChanged?.Invoke(this, "Optimizing power settings...");

        try
        {
            // Set to Balanced power plan
            await RunCommandAsync("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");

            // Enable battery saver threshold
            await RunCommandAsync("powercfg", "/setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD 20");

            StatusChanged?.Invoke(this, "Power settings optimized");
            return true;
        }
        catch
        {
            StatusChanged?.Invoke(this, "Failed to optimize power settings");
            return false;
        }
    }

    #endregion

    #region Random Restarts

    public async Task<List<string>> DiagnoseRandomRestartsAsync()
    {
        var issues = new List<string>();

        StatusChanged?.Invoke(this, "Analyzing system for restart causes...");

        // Check event logs for BSOD
        try
        {
            using var eventLog = new EventLog("System");
            var bugChecks = eventLog.Entries.Cast<EventLogEntry>()
                .Where(e => e.TimeGenerated > DateTime.Now.AddDays(-7))
                .Where(e => e.Source == "Microsoft-Windows-WER-SystemErrorReporting" ||
                           e.Source == "BugCheck")
                .Take(5);

            foreach (var entry in bugChecks)
            {
                issues.Add($"System crash detected on {entry.TimeGenerated:g}");
            }
        }
        catch { }

        // Check if auto-restart on BSOD is enabled
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl");
            if (key != null)
            {
                var autoReboot = key.GetValue("AutoReboot");
                if (autoReboot != null && Convert.ToInt32(autoReboot) == 1)
                {
                    issues.Add("Automatic restart on system failure is ENABLED - this hides BSOD errors");
                }
            }
        }
        catch { }

        // Check temperature (if possible)
        issues.Add("Check your system temperature - overheating can cause random restarts");
        issues.Add("Check your power supply - unstable power can cause restarts");

        return issues;
    }

    public async Task<bool> DisableAutoRestartOnBSODAsync()
    {
        StatusChanged?.Invoke(this, "Disabling automatic restart on system failure...");

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl", writable: true);
            if (key != null)
            {
                key.SetValue("AutoReboot", 0, RegistryValueKind.DWord);
                StatusChanged?.Invoke(this, "Automatic restart disabled - you will now see BSOD errors");
                return true;
            }
        }
        catch { }

        StatusChanged?.Invoke(this, "Failed - requires administrator privileges");
        return false;
    }

    #endregion

    #region RAM Issues

    public async Task<bool> ClearMemoryAsync()
    {
        StatusChanged?.Invoke(this, "Clearing memory...");

        try
        {
            // Clear working set
            ProcessHelper.ClearStandbyMemory();

            // Run memory diagnostic tool suggestion
            StatusChanged?.Invoke(this, "Memory cleared. For deeper analysis, run Windows Memory Diagnostic.");
            return true;
        }
        catch
        {
            StatusChanged?.Invoke(this, "Failed to clear memory");
            return false;
        }
    }

    public List<ProcessInfo> GetMemoryHogs()
    {
        return ProcessHelper.GetTopMemoryProcesses(10);
    }

    #endregion

    #region Disk Full

    public List<(string Path, long Size)> FindLargeFiles(string driveLetter, int topCount = 20)
    {
        var largeFiles = new List<(string Path, long Size)>();

        StatusChanged?.Invoke(this, "Scanning for large files...");

        try
        {
            var userFolders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            foreach (var folder in userFolders)
            {
                if (!Directory.Exists(folder)) continue;

                try
                {
                    var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                        .Select(f =>
                        {
                            try { return (Path: f, Size: new FileInfo(f).Length); }
                            catch { return (Path: f, Size: 0L); }
                        })
                        .Where(f => f.Size > 100_000_000) // > 100MB
                        .OrderByDescending(f => f.Size);

                    largeFiles.AddRange(files);
                }
                catch { }
            }
        }
        catch { }

        StatusChanged?.Invoke(this, $"Found {largeFiles.Count} large files");
        return largeFiles.OrderByDescending(f => f.Size).Take(topCount).ToList();
    }

    #endregion

    #region Slow Startup

    public List<StartupItem> GetStartupItems()
    {
        return ProcessHelper.GetStartupItems();
    }

    public bool DisableStartupItem(StartupItem item)
    {
        StatusChanged?.Invoke(this, $"Disabling {item.Name}...");
        var result = ProcessHelper.DisableStartupItem(item);
        StatusChanged?.Invoke(this, result ? $"Disabled {item.Name}" : $"Failed to disable {item.Name}");
        return result;
    }

    #endregion

    #region Network Issues

    public async Task<bool> ResetNetworkStackAsync()
    {
        StatusChanged?.Invoke(this, "Resetting network stack...");

        try
        {
            // Flush DNS
            await RunCommandAsync("ipconfig", "/flushdns");

            // Reset Winsock
            await RunCommandAsync("netsh", "winsock reset");

            // Reset IP stack
            await RunCommandAsync("netsh", "int ip reset");

            StatusChanged?.Invoke(this, "Network stack reset. A restart may be required.");
            return true;
        }
        catch
        {
            StatusChanged?.Invoke(this, "Failed to reset network stack");
            return false;
        }
    }

    public async Task<bool> FlushDnsAsync()
    {
        StatusChanged?.Invoke(this, "Flushing DNS cache...");

        try
        {
            await RunCommandAsync("ipconfig", "/flushdns");
            StatusChanged?.Invoke(this, "DNS cache flushed");
            return true;
        }
        catch
        {
            StatusChanged?.Invoke(this, "Failed to flush DNS");
            return false;
        }
    }

    #endregion

    #region High CPU

    public List<ProcessInfo> GetCpuHogs()
    {
        return ProcessHelper.GetTopCpuProcesses(10);
    }

    public bool KillProcess(int processId)
    {
        return ProcessHelper.KillProcess(processId);
    }

    #endregion

    #region Utility Methods

    private async Task<string> RunCommandAsync(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    #endregion
}
