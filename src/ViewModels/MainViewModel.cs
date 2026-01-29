using System.Windows;
using System.Windows.Input;
using WhatsUpWithMyPC.Services;
using WhatsUpWithMyPC.Views;

namespace WhatsUpWithMyPC.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly SystemMonitor _systemMonitor;
    private object? _currentView;
    private string _statusMessage = "Ready";
    private double _cpuUsage;
    private string _ramUsage = "0/0 GB";
    private double _diskUsage;

    private bool _isDashboardSelected = true;
    private bool _isHealthCheckSelected;
    private bool _isCleanupSelected;
    private bool _isFixesSelected;

    // Views (lazy loaded)
    private DashboardView? _dashboardView;
    private HealthCheckView? _healthCheckView;
    private CleanupView? _cleanupView;
    private FixesView? _fixesView;

    public MainViewModel()
    {
        _systemMonitor = new SystemMonitor();
        _systemMonitor.StatsUpdated += OnStatsUpdated;

        NavigateCommand = new RelayCommand(Navigate);
        MinimizeToTrayCommand = new RelayCommand(MinimizeToTray);
    }

    public void Initialize()
    {
        // Start with Dashboard
        Navigate("Dashboard");
        _systemMonitor.Start();
    }

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public double CpuUsage
    {
        get => _cpuUsage;
        set => SetProperty(ref _cpuUsage, value);
    }

    public string RamUsage
    {
        get => _ramUsage;
        set => SetProperty(ref _ramUsage, value);
    }

    public double DiskUsage
    {
        get => _diskUsage;
        set => SetProperty(ref _diskUsage, value);
    }

    public bool IsDashboardSelected
    {
        get => _isDashboardSelected;
        set => SetProperty(ref _isDashboardSelected, value);
    }

    public bool IsHealthCheckSelected
    {
        get => _isHealthCheckSelected;
        set => SetProperty(ref _isHealthCheckSelected, value);
    }

    public bool IsCleanupSelected
    {
        get => _isCleanupSelected;
        set => SetProperty(ref _isCleanupSelected, value);
    }

    public bool IsFixesSelected
    {
        get => _isFixesSelected;
        set => SetProperty(ref _isFixesSelected, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand MinimizeToTrayCommand { get; }

    private void Navigate(object? parameter)
    {
        if (parameter is not string viewName) return;

        IsDashboardSelected = viewName == "Dashboard";
        IsHealthCheckSelected = viewName == "HealthCheck";
        IsCleanupSelected = viewName == "Cleanup";
        IsFixesSelected = viewName == "Fixes";

        CurrentView = viewName switch
        {
            "Dashboard" => _dashboardView ??= new DashboardView(),
            "HealthCheck" => _healthCheckView ??= new HealthCheckView(),
            "Cleanup" => _cleanupView ??= new CleanupView(),
            "Fixes" => _fixesView ??= new FixesView(),
            _ => CurrentView
        };

        StatusMessage = $"Viewing {viewName}";
    }

    private void MinimizeToTray(object? parameter)
    {
        if (Application.Current.MainWindow is Window window)
        {
            window.Hide();
            ShowTrayNotification("WhatsUpWithMyPC minimized to system tray.");
        }
    }

    public void ShowTrayNotification(string message)
    {
        // TrayService handles this
        TrayService.Instance?.ShowNotification("WhatsUpWithMyPC", message);
    }

    private void OnStatsUpdated(object? sender, Models.SystemStats stats)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CpuUsage = stats.Cpu.UsagePercent;
            RamUsage = $"{stats.Memory.UsedGB:F1}/{stats.Memory.TotalGB:F1} GB";
            DiskUsage = stats.Disks.FirstOrDefault()?.UsagePercent ?? 0;
        });
    }

    public void Dispose()
    {
        _systemMonitor.StatsUpdated -= OnStatsUpdated;
        _systemMonitor.Dispose();
    }
}
