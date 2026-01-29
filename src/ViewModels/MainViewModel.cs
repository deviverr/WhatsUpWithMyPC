using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsUpWithMyPC.Models;
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
    private int _processCount;
    private bool _isLogVisible = true;

    // Navigation selection states
    private bool _isDashboardSelected = true;
    private bool _isHealthCheckSelected;
    private bool _isCleanupSelected;
    private bool _isFixesSelected;
    private bool _isOptimizerSelected;
    private bool _isStartupSelected;
    private bool _isDiagnosticsSelected;

    // Views (lazy loaded)
    private DashboardView? _dashboardView;
    private HealthCheckView? _healthCheckView;
    private CleanupView? _cleanupView;
    private FixesView? _fixesView;
    private OptimizerView? _optimizerView;
    private StartupManagerView? _startupManagerView;
    private DiagnosticsView? _diagnosticsView;

    public MainViewModel()
    {
        _systemMonitor = new SystemMonitor();
        _systemMonitor.StatsUpdated += OnStatsUpdated;

        // Initialize commands
        NavigateCommand = new RelayCommand(Navigate);
        MinimizeToTrayCommand = new RelayCommand(MinimizeToTray);
        ToggleLogCommand = new RelayCommand(_ => IsLogVisible = !IsLogVisible);
        ClearLogCommand = new RelayCommand(_ => LogService.Instance.Clear());

        // Subscribe to log changes
        LogService.Instance.Logs.CollectionChanged += (s, e) => OnPropertyChanged(nameof(LogCount));
    }

    public void Initialize()
    {
        Navigate("Dashboard");
        _systemMonitor.Start();
        LogService.Instance.Info("System monitoring started", "Monitor");
    }

    #region Properties

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

    public int ProcessCount
    {
        get => _processCount;
        set => SetProperty(ref _processCount, value);
    }

    public bool IsLogVisible
    {
        get => _isLogVisible;
        set => SetProperty(ref _isLogVisible, value);
    }

    public ObservableCollection<LogEntry> LogEntries => LogService.Instance.Logs;

    public int LogCount => LogService.Instance.Logs.Count;

    #endregion

    #region Navigation States

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

    public bool IsOptimizerSelected
    {
        get => _isOptimizerSelected;
        set => SetProperty(ref _isOptimizerSelected, value);
    }

    public bool IsStartupSelected
    {
        get => _isStartupSelected;
        set => SetProperty(ref _isStartupSelected, value);
    }

    public bool IsDiagnosticsSelected
    {
        get => _isDiagnosticsSelected;
        set => SetProperty(ref _isDiagnosticsSelected, value);
    }

    #endregion

    #region Commands

    public ICommand NavigateCommand { get; }
    public ICommand MinimizeToTrayCommand { get; }
    public ICommand ToggleLogCommand { get; }
    public ICommand ClearLogCommand { get; }

    #endregion

    #region Methods

    private void Navigate(object? parameter)
    {
        if (parameter is not string viewName) return;

        // Reset all selection states
        IsDashboardSelected = viewName == "Dashboard";
        IsHealthCheckSelected = viewName == "HealthCheck";
        IsCleanupSelected = viewName == "Cleanup";
        IsFixesSelected = viewName == "Fixes";
        IsOptimizerSelected = viewName == "Optimizer";
        IsStartupSelected = viewName == "Startup";
        IsDiagnosticsSelected = viewName == "Diagnostics";

        // Lazy load and set current view
        CurrentView = viewName switch
        {
            "Dashboard" => _dashboardView ??= new DashboardView(),
            "HealthCheck" => _healthCheckView ??= new HealthCheckView(),
            "Cleanup" => _cleanupView ??= new CleanupView(),
            "Fixes" => _fixesView ??= new FixesView(),
            "Optimizer" => _optimizerView ??= new OptimizerView(),
            "Startup" => _startupManagerView ??= new StartupManagerView(),
            "Diagnostics" => _diagnosticsView ??= new DiagnosticsView(),
            _ => CurrentView
        };

        StatusMessage = viewName switch
        {
            "Dashboard" => "Live system monitoring",
            "HealthCheck" => "Scan your system for issues",
            "Cleanup" => "Free up disk space",
            "Fixes" => "Quick solutions for common problems",
            "Optimizer" => "Optimize Windows performance",
            "Startup" => "Manage startup programs",
            "Diagnostics" => "Hardware diagnostics & tests",
            _ => "Ready"
        };

        LogService.Instance.Info($"Navigated to {viewName}", "Nav");
    }

    private void MinimizeToTray(object? parameter)
    {
        if (Application.Current.MainWindow is System.Windows.Window window)
        {
            window.Hide();
            ShowTrayNotification("WhatsUpWithMyPC minimized to system tray.");
            LogService.Instance.Info("Minimized to system tray", "App");
        }
    }

    public void ShowTrayNotification(string message)
    {
        TrayService.Instance?.ShowNotification("WhatsUpWithMyPC", message);
    }

    private void OnStatsUpdated(object? sender, SystemStats stats)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            CpuUsage = stats.Cpu.UsagePercent;
            RamUsage = $"{stats.Memory.UsedGB:F1}/{stats.Memory.TotalGB:F1} GB";
            DiskUsage = stats.Disks.FirstOrDefault()?.UsagePercent ?? 0;
            ProcessCount = stats.ProcessCount;
        });
    }

    public void Dispose()
    {
        _systemMonitor.StatsUpdated -= OnStatsUpdated;
        _systemMonitor.Dispose();
        LogService.Instance.Info("Application closing", "App");
    }

    #endregion
}
