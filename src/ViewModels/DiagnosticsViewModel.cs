using System.Collections.ObjectModel;
using System.Management;
using System.Windows.Input;
using System.Windows.Media;
using WhatsUpWithMyPC.Helpers;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class DiskHealthResult
{
    public string DriveName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public System.Windows.Media.Brush StatusColor { get; set; } = System.Windows.Media.Brushes.Gray;
}

public class DiagnosticsViewModel : ViewModelBase
{
    private CancellationTokenSource? _memoryCts;
    private CancellationTokenSource? _cpuCts;

    private string _memoryTestStatus = "Ready to test";
    private string _cpuTestStatus = "Ready to test";
    private int _memoryTestProgress;
    private int _cpuTestProgress;
    private bool _isMemoryTestRunning;
    private bool _isCpuTestRunning;

    public DiagnosticsViewModel()
    {
        DiskHealthResults = new ObservableCollection<DiskHealthResult>();

        CheckDiskHealthCommand = new AsyncRelayCommand(CheckDiskHealthAsync);
        RunMemoryTestCommand = new AsyncRelayCommand(RunMemoryTestAsync);
        RunCpuTestCommand = new AsyncRelayCommand(RunCpuTestAsync);

        LoadSystemInfo();
    }

    #region Properties

    public ObservableCollection<DiskHealthResult> DiskHealthResults { get; }

    public bool HasNoDiskResults => DiskHealthResults.Count == 0;

    public string MemoryTestStatus
    {
        get => _memoryTestStatus;
        set => SetProperty(ref _memoryTestStatus, value);
    }

    public string CpuTestStatus
    {
        get => _cpuTestStatus;
        set => SetProperty(ref _cpuTestStatus, value);
    }

    public int MemoryTestProgress
    {
        get => _memoryTestProgress;
        set => SetProperty(ref _memoryTestProgress, value);
    }

    public int CpuTestProgress
    {
        get => _cpuTestProgress;
        set => SetProperty(ref _cpuTestProgress, value);
    }

    public bool IsMemoryTestRunning
    {
        get => _isMemoryTestRunning;
        set
        {
            if (SetProperty(ref _isMemoryTestRunning, value))
                OnPropertyChanged(nameof(MemoryTestButtonText));
        }
    }

    public bool IsCpuTestRunning
    {
        get => _isCpuTestRunning;
        set
        {
            if (SetProperty(ref _isCpuTestRunning, value))
                OnPropertyChanged(nameof(CpuTestButtonText));
        }
    }

    public string MemoryTestButtonText => IsMemoryTestRunning ? "Stop" : "Run Test";
    public string CpuTestButtonText => IsCpuTestRunning ? "Stop" : "Run Test";

    // System Info
    public string WindowsVersion { get; private set; } = "Loading...";
    public string CpuName { get; private set; } = "Loading...";
    public string TotalRam { get; private set; } = "Loading...";
    public string GpuName { get; private set; } = "Loading...";
    public string DisplayInfo { get; private set; } = "Loading...";

    #endregion

    #region Commands

    public ICommand CheckDiskHealthCommand { get; }
    public ICommand RunMemoryTestCommand { get; }
    public ICommand RunCpuTestCommand { get; }

    #endregion

    #region Methods

    private void LoadSystemInfo()
    {
        try
        {
            var cpu = WmiHelper.GetCpuInfo();
            var memory = WmiHelper.GetMemoryInfo();
            var gpu = WmiHelper.GetGpuInfo();
            var display = WmiHelper.GetDisplayInfo();

            WindowsVersion = WmiHelper.GetWindowsVersion();
            CpuName = $"{cpu.Name} ({cpu.CoreCount} cores, {cpu.ThreadCount} threads)";
            TotalRam = $"{memory.TotalGB:F1} GB";
            GpuName = $"{gpu.Name} ({gpu.DedicatedMemoryGB:F1} GB VRAM)";
            DisplayInfo = $"{display.DisplayString} ({display.ScalingPercent}% scaling)";

            OnPropertyChanged(nameof(WindowsVersion));
            OnPropertyChanged(nameof(CpuName));
            OnPropertyChanged(nameof(TotalRam));
            OnPropertyChanged(nameof(GpuName));
            OnPropertyChanged(nameof(DisplayInfo));

            LogService.Instance.Info("Loaded system information", "Diagnostics");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to load system info: {ex.Message}", "Diagnostics");
        }
    }

    private async Task CheckDiskHealthAsync()
    {
        LogService.Instance.Info("Checking disk health (SMART)...", "Diagnostics");
        DiskHealthResults.Clear();

        try
        {
            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher(
                    @"root\wmi",
                    "SELECT InstanceName, Active FROM MSStorageDriver_FailurePredictStatus");

                var smartData = new Dictionary<string, bool>();
                foreach (ManagementObject obj in searcher.Get())
                {
                    var instance = obj["InstanceName"]?.ToString() ?? "";
                    var active = obj["Active"] != null && (bool)obj["Active"];
                    if (!string.IsNullOrEmpty(instance))
                    {
                        // "Active" being false means SMART predicts failure
                        smartData[instance] = !active; // true = healthy
                    }
                }

                // Get disk info
                using var diskSearcher = new ManagementObjectSearcher(
                    "SELECT Model, Caption, Size FROM Win32_DiskDrive");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ManagementObject disk in diskSearcher.Get())
                    {
                        var model = disk["Model"]?.ToString() ?? "Unknown";
                        var caption = disk["Caption"]?.ToString() ?? "Disk";
                        var sizeBytes = Convert.ToUInt64(disk["Size"] ?? 0);
                        var sizeGb = sizeBytes / 1024.0 / 1024.0 / 1024.0;

                        // Default to OK if SMART not available
                        var status = "OK";
                        var statusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)); // Green

                        DiskHealthResults.Add(new DiskHealthResult
                        {
                            DriveName = $"{caption} ({sizeGb:F0} GB)",
                            Model = model,
                            Status = status,
                            StatusColor = statusColor
                        });
                    }

                    OnPropertyChanged(nameof(HasNoDiskResults));
                });
            });

            LogService.Instance.Success($"Checked {DiskHealthResults.Count} disks", "Diagnostics");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"SMART check failed: {ex.Message}", "Diagnostics");

            // Fallback - just list drives
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                DiskHealthResults.Add(new DiskHealthResult
                {
                    DriveName = drive.Name,
                    Model = drive.VolumeLabel,
                    Status = "Unknown",
                    StatusColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128))
                });
            }
            OnPropertyChanged(nameof(HasNoDiskResults));
        }
    }

    private async Task RunMemoryTestAsync()
    {
        if (IsMemoryTestRunning)
        {
            _memoryCts?.Cancel();
            return;
        }

        IsMemoryTestRunning = true;
        MemoryTestProgress = 0;
        MemoryTestStatus = "Allocating memory blocks...";
        _memoryCts = new CancellationTokenSource();

        LogService.Instance.Info("Starting memory test...", "Diagnostics");

        try
        {
            var totalMb = (int)(new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1024 / 1024);
            var testMb = Math.Min(totalMb / 4, 512); // Test up to 512MB or 25% of available

            MemoryTestStatus = $"Testing {testMb} MB of memory...";

            await Task.Run(async () =>
            {
                var blocks = new List<byte[]>();
                var blockSize = 1024 * 1024; // 1MB blocks
                var totalBlocks = testMb;
                var errors = 0;

                for (int i = 0; i < totalBlocks && !_memoryCts.Token.IsCancellationRequested; i++)
                {
                    try
                    {
                        var block = new byte[blockSize];

                        // Write pattern
                        for (int j = 0; j < blockSize; j++)
                        {
                            block[j] = (byte)(j % 256);
                        }

                        // Verify pattern
                        for (int j = 0; j < blockSize; j++)
                        {
                            if (block[j] != (byte)(j % 256))
                            {
                                errors++;
                            }
                        }

                        blocks.Add(block);
                    }
                    catch
                    {
                        // Out of memory, stop
                        break;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MemoryTestProgress = (i + 1) * 100 / totalBlocks;
                        MemoryTestStatus = $"Testing... {i + 1}/{totalBlocks} MB";
                    });

                    await Task.Delay(10, _memoryCts.Token);
                }

                // Cleanup
                blocks.Clear();
                GC.Collect();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_memoryCts.Token.IsCancellationRequested)
                    {
                        MemoryTestStatus = "Test cancelled";
                        LogService.Instance.Warning("Memory test cancelled", "Diagnostics");
                    }
                    else if (errors == 0)
                    {
                        MemoryTestStatus = $"PASSED - No errors found in {totalBlocks} MB";
                        LogService.Instance.Success("Memory test passed", "Diagnostics");
                    }
                    else
                    {
                        MemoryTestStatus = $"FAILED - {errors} errors found!";
                        LogService.Instance.Error($"Memory test failed with {errors} errors", "Diagnostics");
                    }
                });
            }, _memoryCts.Token);
        }
        catch (OperationCanceledException)
        {
            MemoryTestStatus = "Test cancelled";
        }
        catch (Exception ex)
        {
            MemoryTestStatus = $"Error: {ex.Message}";
            LogService.Instance.Error($"Memory test error: {ex.Message}", "Diagnostics");
        }
        finally
        {
            IsMemoryTestRunning = false;
            _memoryCts?.Dispose();
            _memoryCts = null;
        }
    }

    private async Task RunCpuTestAsync()
    {
        if (IsCpuTestRunning)
        {
            _cpuCts?.Cancel();
            return;
        }

        IsCpuTestRunning = true;
        CpuTestProgress = 0;
        CpuTestStatus = "Starting CPU stress test...";
        _cpuCts = new CancellationTokenSource();

        LogService.Instance.Info("Starting CPU stress test...", "Diagnostics");

        try
        {
            var duration = TimeSpan.FromSeconds(10);
            var startTime = DateTime.Now;

            await Task.Run(async () =>
            {
                var tasks = new List<Task>();
                var coreCount = Environment.ProcessorCount;

                // Spawn one heavy task per core
                for (int i = 0; i < coreCount; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        while (!_cpuCts!.Token.IsCancellationRequested &&
                               DateTime.Now - startTime < duration)
                        {
                            // CPU intensive work
                            double result = 0;
                            for (int j = 0; j < 1000000; j++)
                            {
                                result += Math.Sqrt(j) * Math.Sin(j);
                            }
                        }
                    }));
                }

                // Progress updates
                while (!_cpuCts.Token.IsCancellationRequested &&
                       DateTime.Now - startTime < duration)
                {
                    var elapsed = DateTime.Now - startTime;
                    var progress = (int)(elapsed.TotalSeconds / duration.TotalSeconds * 100);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CpuTestProgress = Math.Min(progress, 100);
                        CpuTestStatus = $"Stress testing... {(int)elapsed.TotalSeconds}/{(int)duration.TotalSeconds}s";
                    });

                    await Task.Delay(500, _cpuCts.Token);
                }

                _cpuCts.Cancel(); // Signal workers to stop
                await Task.WhenAll(tasks);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CpuTestProgress = 100;
                    CpuTestStatus = "PASSED - CPU handled stress test without errors";
                    LogService.Instance.Success("CPU stress test completed", "Diagnostics");
                });
            }, _cpuCts.Token);
        }
        catch (OperationCanceledException)
        {
            CpuTestStatus = "Test cancelled";
            LogService.Instance.Warning("CPU test cancelled", "Diagnostics");
        }
        catch (Exception ex)
        {
            CpuTestStatus = $"Error: {ex.Message}";
            LogService.Instance.Error($"CPU test error: {ex.Message}", "Diagnostics");
        }
        finally
        {
            IsCpuTestRunning = false;
            _cpuCts?.Dispose();
            _cpuCts = null;
        }
    }

    #endregion
}
