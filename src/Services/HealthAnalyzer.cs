using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.ServiceProcess;
using WhatsUpWithMyPC.Helpers;
using WhatsUpWithMyPC.Models;

namespace WhatsUpWithMyPC.Services;

public class HealthAnalyzer
{
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<int>? ProgressChanged;

    public async Task<HealthReport> RunFullScanAsync(CancellationToken cancellationToken = default)
    {
        var report = new HealthReport
        {
            ScanStarted = DateTime.Now
        };

        var totalSteps = 7;
        var currentStep = 0;

        // Step 1: Check disk health
        ReportProgress("Checking disk health...", ++currentStep, totalSteps);
        await Task.Run(() => CheckDiskHealth(report), cancellationToken);

        // Step 2: Check memory usage
        ReportProgress("Analyzing memory usage...", ++currentStep, totalSteps);
        await Task.Run(() => CheckMemoryUsage(report), cancellationToken);

        // Step 3: Check system errors
        ReportProgress("Scanning system event logs...", ++currentStep, totalSteps);
        await Task.Run(() => CheckSystemErrors(report), cancellationToken);

        // Step 4: Check startup programs
        ReportProgress("Analyzing startup programs...", ++currentStep, totalSteps);
        await Task.Run(() => CheckStartupPrograms(report), cancellationToken);

        // Step 5: Check Windows services
        ReportProgress("Checking Windows services...", ++currentStep, totalSteps);
        await Task.Run(() => CheckWindowsServices(report), cancellationToken);

        // Step 6: Check security status
        ReportProgress("Checking security status...", ++currentStep, totalSteps);
        await Task.Run(() => CheckSecurityStatus(report), cancellationToken);

        // Step 7: Check for large temp files
        ReportProgress("Scanning temporary files...", ++currentStep, totalSteps);
        await Task.Run(() => CheckTempFiles(report), cancellationToken);

        report.ScanCompleted = DateTime.Now;
        ReportProgress("Scan complete", totalSteps, totalSteps);

        return report;
    }

    private void ReportProgress(string status, int step, int totalSteps)
    {
        StatusChanged?.Invoke(this, status);
        ProgressChanged?.Invoke(this, (int)((double)step / totalSteps * 100));
    }

    private void CheckDiskHealth(HealthReport report)
    {
        var disks = WmiHelper.GetDiskInfo();

        foreach (var disk in disks)
        {
            // Check for low disk space
            if (disk.UsagePercent > 90)
            {
                report.Issues.Add(new HealthIssue
                {
                    Title = $"Low disk space on {disk.DriveLetter}",
                    Description = $"Drive {disk.DriveLetter} is {disk.UsagePercent:F0}% full. Only {disk.FreeGB:F1} GB remaining.",
                    Severity = disk.UsagePercent > 95 ? IssueSeverity.Critical : IssueSeverity.Warning,
                    Category = IssueCategory.Disk,
                    CanAutoFix = true,
                    FixAction = "RunCleanup"
                });
            }
            else if (disk.UsagePercent > 80)
            {
                report.Issues.Add(new HealthIssue
                {
                    Title = $"Disk space getting low on {disk.DriveLetter}",
                    Description = $"Drive {disk.DriveLetter} is {disk.UsagePercent:F0}% full. Consider cleaning up.",
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.Disk,
                    CanAutoFix = true,
                    FixAction = "RunCleanup"
                });
            }
        }
    }

    private void CheckMemoryUsage(HealthReport report)
    {
        var memory = WmiHelper.GetMemoryInfo();

        if (memory.UsagePercent > 90)
        {
            report.Issues.Add(new HealthIssue
            {
                Title = "Very high memory usage",
                Description = $"RAM is {memory.UsagePercent:F0}% utilized ({memory.UsedGB:F1} / {memory.TotalGB:F1} GB). System may be slow.",
                Severity = IssueSeverity.Error,
                Category = IssueCategory.Memory,
                CanAutoFix = true,
                FixAction = "ClearMemory"
            });
        }
        else if (memory.UsagePercent > 80)
        {
            report.Issues.Add(new HealthIssue
            {
                Title = "High memory usage",
                Description = $"RAM is {memory.UsagePercent:F0}% utilized. Consider closing some applications.",
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Memory,
                CanAutoFix = true,
                FixAction = "ClearMemory"
            });
        }

        // Check for memory hogs
        var topProcesses = ProcessHelper.GetTopMemoryProcesses(5);
        var threshold = memory.TotalBytes * 0.2; // 20% of total RAM

        foreach (var proc in topProcesses)
        {
            if ((ulong)proc.MemoryBytes > threshold)
            {
                report.Issues.Add(new HealthIssue
                {
                    Title = $"High memory usage: {proc.Name}",
                    Description = $"{proc.Name} is using {proc.MemoryFormatted} of RAM.",
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.Memory,
                    CanAutoFix = false
                });
            }
        }
    }

    private void CheckSystemErrors(HealthReport report)
    {
        try
        {
            var recentErrors = new List<(string Source, string Message, DateTime Time)>();

            using var eventLog = new EventLog("System");
            var entries = eventLog.Entries.Cast<EventLogEntry>()
                .Where(e => e.TimeGenerated > DateTime.Now.AddDays(-1))
                .Where(e => e.EntryType == EventLogEntryType.Error || e.EntryType == EventLogEntryType.Warning)
                .OrderByDescending(e => e.TimeGenerated)
                .Take(10);

            var errorCount = 0;
            var warningCount = 0;

            foreach (var entry in entries)
            {
                if (entry.EntryType == EventLogEntryType.Error)
                    errorCount++;
                else
                    warningCount++;
            }

            if (errorCount > 5)
            {
                report.Issues.Add(new HealthIssue
                {
                    Title = "Multiple system errors detected",
                    Description = $"Found {errorCount} errors in the system event log in the last 24 hours.",
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.System,
                    CanAutoFix = false
                });
            }
        }
        catch
        {
            // Event log access might require admin
        }
    }

    private void CheckStartupPrograms(HealthReport report)
    {
        var startupItems = ProcessHelper.GetStartupItems();

        if (startupItems.Count > 10)
        {
            report.Issues.Add(new HealthIssue
            {
                Title = "Many startup programs",
                Description = $"Found {startupItems.Count} programs set to run at startup. This may slow down boot time.",
                Severity = IssueSeverity.Info,
                Category = IssueCategory.Startup,
                CanAutoFix = false,
                FixAction = "ManageStartup"
            });
        }
    }

    private void CheckWindowsServices(HealthReport report)
    {
        try
        {
            // Check if Windows Update service is running
            var wuauserv = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == "wuauserv");

            if (wuauserv != null && wuauserv.Status == ServiceControllerStatus.Stopped)
            {
                report.Issues.Add(new HealthIssue
                {
                    Title = "Windows Update service is stopped",
                    Description = "The Windows Update service is not running. You may not receive important security updates.",
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Update,
                    CanAutoFix = false
                });
            }

            // Check Windows Defender
            var defender = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == "WinDefend");

            if (defender != null && defender.Status == ServiceControllerStatus.Stopped)
            {
                report.Issues.Add(new HealthIssue
                {
                    Title = "Windows Defender is not running",
                    Description = "Windows Defender Antivirus service is stopped. Your system may be vulnerable.",
                    Severity = IssueSeverity.Error,
                    Category = IssueCategory.Security,
                    CanAutoFix = false
                });
            }
        }
        catch
        {
            // Service access might require admin
        }
    }

    private void CheckSecurityStatus(HealthReport report)
    {
        // Check if running as admin (generally not recommended for normal use)
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        if (principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            report.Issues.Add(new HealthIssue
            {
                Title = "Running with administrator privileges",
                Description = "This application is running as administrator. This is only needed for certain fix operations.",
                Severity = IssueSeverity.Info,
                Category = IssueCategory.Security,
                CanAutoFix = false
            });
        }
    }

    private void CheckTempFiles(HealthReport report)
    {
        long totalTempSize = 0;

        var tempPaths = new[]
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };

        foreach (var path in tempPaths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    totalTempSize += GetDirectorySize(path);
                }
                catch
                {
                    // Access denied for some folders
                }
            }
        }

        var sizeGB = totalTempSize / 1024.0 / 1024.0 / 1024.0;

        if (sizeGB > 1)
        {
            report.Issues.Add(new HealthIssue
            {
                Title = "Large temporary files",
                Description = $"Found {sizeGB:F2} GB of temporary files that can be cleaned up.",
                Severity = IssueSeverity.Info,
                Category = IssueCategory.Disk,
                CanAutoFix = true,
                FixAction = "RunCleanup"
            });
        }
    }

    private long GetDirectorySize(string path)
    {
        long size = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip inaccessible files
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                try
                {
                    size += GetDirectorySize(dir);
                }
                catch
                {
                    // Skip inaccessible directories
                }
            }
        }
        catch
        {
            // Skip if we can't enumerate
        }

        return size;
    }
}
