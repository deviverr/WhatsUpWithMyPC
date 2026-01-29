using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WhatsUpWithMyPC.Helpers;
using WhatsUpWithMyPC.Models;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly SystemMonitor _monitor;

    private double _cpuUsage;
    private string _cpuName = "Loading...";
    private double _ramUsed;
    private double _ramTotal;
    private double _ramUsagePercent;
    private string _gpuName = "Loading...";
    private double _gpuMemory;
    private string _downloadSpeed = "0 bps";
    private string _uploadSpeed = "0 bps";
    private string _networkStatus = "Checking...";
    private bool _hasBattery;
    private double _batteryPercent;
    private string _batteryStatus = "";
    private string _uptime = "0:00:00";

    public DashboardViewModel()
    {
        _monitor = new SystemMonitor();
        _monitor.StatsUpdated += OnStatsUpdated;

        Disks = new ObservableCollection<DiskInfo>();

        RunHealthCheckCommand = new RelayCommand(RunHealthCheck);
        RunCleanupCommand = new RelayCommand(RunCleanup);
        RefreshCommand = new RelayCommand(Refresh);

        // Get initial static info
        var cpuInfo = WmiHelper.GetCpuInfo();
        CpuName = cpuInfo.Name;

        var gpuInfo = WmiHelper.GetGpuInfo();
        GpuName = gpuInfo.Name;
        GpuMemory = gpuInfo.DedicatedMemoryGB;

        // Start monitoring
        _monitor.Start();
    }

    public double CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    public string CpuName
    {
        get => _cpuName;
        set => SetProperty(ref _cpuName, value);
    }

    public double RamUsed
    {
        get => _ramUsed;
        set => SetProperty(ref _ramUsed, value);
    }

    public double RamTotal
    {
        get => _ramTotal;
        set => SetProperty(ref _ramTotal, value);
    }

    public double RamUsagePercent
    {
        get => _ramUsagePercent;
        set => SetProperty(ref _ramUsagePercent, value);
    }

    public string GpuName
    {
        get => _gpuName;
        set => SetProperty(ref _gpuName, value);
    }

    public double GpuMemory
    {
        get => _gpuMemory;
        set => SetProperty(ref _gpuMemory, value);
    }

    public ObservableCollection<DiskInfo> Disks { get; }

    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetProperty(ref _downloadSpeed, value);
    }

    public string UploadSpeed
    {
        get => _uploadSpeed;
        set => SetProperty(ref _uploadSpeed, value);
    }

    public string NetworkStatus
    {
        get => _networkStatus;
        set => SetProperty(ref _networkStatus, value);
    }

    public bool HasBattery
    {
        get => _hasBattery;
        set => SetProperty(ref _hasBattery, value);
    }

    public double BatteryPercent
    {
        get => _batteryPercent;
        set => SetProperty(ref _batteryPercent, value);
    }

    public string BatteryStatus
    {
        get => _batteryStatus;
        set => SetProperty(ref _batteryStatus, value);
    }

    public string Uptime
    {
        get => _uptime;
        set => SetProperty(ref _uptime, value);
    }

    public ICommand RunHealthCheckCommand { get; }
    public ICommand RunCleanupCommand { get; }
    public ICommand RefreshCommand { get; }

    private void OnStatsUpdated(object? sender, SystemStats stats)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // CPU
            CpuUsage = stats.Cpu.UsagePercent;

            // RAM
            RamUsed = stats.Memory.UsedGB;
            RamTotal = stats.Memory.TotalGB;
            RamUsagePercent = stats.Memory.UsagePercent;

            // Disks
            Disks.Clear();
            foreach (var disk in stats.Disks)
            {
                Disks.Add(disk);
            }

            // Network
            DownloadSpeed = stats.Network.DownloadSpeedFormatted;
            UploadSpeed = stats.Network.UploadSpeedFormatted;
            NetworkStatus = stats.Network.IsConnected
                ? $"Connected - {stats.Network.AdapterName}"
                : "Disconnected";

            // Battery
            HasBattery = stats.Battery.HasBattery;
            BatteryPercent = stats.Battery.ChargePercent;
            BatteryStatus = stats.Battery.IsCharging ? "Charging" : "On Battery";

            // Uptime
            Uptime = FormatUptime(stats.Uptime);
        });
    }

    private string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    private void RunHealthCheck(object? parameter)
    {
        // Navigate to Health Check view
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.NavigateCommand.Execute("HealthCheck");
        }
    }

    private void RunCleanup(object? parameter)
    {
        // Navigate to Cleanup view
        if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.NavigateCommand.Execute("Cleanup");
        }
    }

    private void Refresh(object? parameter)
    {
        // Force a refresh
        _monitor.Stop();
        _monitor.Start();
    }

    public void Dispose()
    {
        _monitor.StatsUpdated -= OnStatsUpdated;
        _monitor.Dispose();
    }
}
