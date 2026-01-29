using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WhatsUpWithMyPC.Helpers;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class FixesViewModel : ViewModelBase
{
    private readonly FixService _fixService;

    private string _statusMessage = "";
    private bool _showDetails;
    private string _detailsTitle = "";

    public FixesViewModel()
    {
        _fixService = new FixService();
        _fixService.StatusChanged += (s, msg) => StatusMessage = msg;

        DetailItems = new ObservableCollection<string>();

        FixShutdownCommand = new AsyncRelayCommand(FixShutdownAsync);
        OptimizeBatteryCommand = new AsyncRelayCommand(OptimizeBatteryAsync);
        DiagnoseRestartsCommand = new AsyncRelayCommand(DiagnoseRestartsAsync);
        ClearMemoryCommand = new AsyncRelayCommand(ClearMemoryAsync);
        FindLargeFilesCommand = new AsyncRelayCommand(FindLargeFilesAsync);
        ManageStartupCommand = new RelayCommand(ManageStartup);
        FindCpuHogsCommand = new RelayCommand(FindCpuHogs);
        ResetNetworkCommand = new AsyncRelayCommand(ResetNetworkAsync);
        CloseDetailsCommand = new RelayCommand(_ => ShowDetails = false);
    }

    public ObservableCollection<string> DetailItems { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool ShowDetails
    {
        get => _showDetails;
        set => SetProperty(ref _showDetails, value);
    }

    public string DetailsTitle
    {
        get => _detailsTitle;
        set => SetProperty(ref _detailsTitle, value);
    }

    public ICommand FixShutdownCommand { get; }
    public ICommand OptimizeBatteryCommand { get; }
    public ICommand DiagnoseRestartsCommand { get; }
    public ICommand ClearMemoryCommand { get; }
    public ICommand FindLargeFilesCommand { get; }
    public ICommand ManageStartupCommand { get; }
    public ICommand FindCpuHogsCommand { get; }
    public ICommand ResetNetworkCommand { get; }
    public ICommand CloseDetailsCommand { get; }

    private async Task FixShutdownAsync()
    {
        ClearDetails();
        var success = await _fixService.FixShutdownIssuesAsync();

        if (success)
        {
            StatusMessage = "Shutdown issues have been addressed. Try shutting down your PC now.";
        }
        else
        {
            StatusMessage = "Some fixes require administrator privileges. Try running as admin.";
        }
    }

    private async Task OptimizeBatteryAsync()
    {
        ClearDetails();
        var suggestions = await _fixService.AnalyzeBatteryDrainAsync();

        DetailsTitle = "Battery Optimization Suggestions";
        foreach (var suggestion in suggestions)
        {
            DetailItems.Add(suggestion);
        }
        ShowDetails = true;

        await _fixService.OptimizeBatteryAsync();
    }

    private async Task DiagnoseRestartsAsync()
    {
        ClearDetails();
        var issues = await _fixService.DiagnoseRandomRestartsAsync();

        DetailsTitle = "Restart Diagnosis Results";
        foreach (var issue in issues)
        {
            DetailItems.Add(issue);
        }
        ShowDetails = true;

        // Offer to disable auto-restart
        var result = MessageBox.Show(
            "Would you like to disable automatic restart on system failure? This will show the Blue Screen when crashes occur, helping diagnose the issue.",
            "Disable Auto-Restart?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _fixService.DisableAutoRestartOnBSODAsync();
        }
    }

    private async Task ClearMemoryAsync()
    {
        ClearDetails();
        await _fixService.ClearMemoryAsync();

        // Show memory hogs
        var memoryHogs = _fixService.GetMemoryHogs();

        DetailsTitle = "Top Memory-Using Processes";
        foreach (var proc in memoryHogs)
        {
            DetailItems.Add($"{proc.Name} - {proc.MemoryFormatted}");
        }
        ShowDetails = true;
    }

    private async Task FindLargeFilesAsync()
    {
        ClearDetails();
        StatusMessage = "Scanning for large files...";

        await Task.Run(() =>
        {
            var largeFiles = _fixService.FindLargeFiles("C:", 15);

            Application.Current.Dispatcher.Invoke(() =>
            {
                DetailsTitle = "Large Files Found (>100MB)";
                foreach (var (path, size) in largeFiles)
                {
                    var sizeStr = size >= 1_073_741_824
                        ? $"{size / 1_073_741_824.0:F2} GB"
                        : $"{size / 1_048_576.0:F2} MB";
                    DetailItems.Add($"{sizeStr} - {path}");
                }

                if (DetailItems.Count == 0)
                {
                    DetailItems.Add("No large files found in user folders.");
                }

                ShowDetails = true;
                StatusMessage = "";
            });
        });
    }

    private void ManageStartup(object? parameter)
    {
        ClearDetails();
        var startupItems = _fixService.GetStartupItems();

        DetailsTitle = "Startup Programs";
        foreach (var item in startupItems)
        {
            DetailItems.Add($"{item.Name} ({item.Location})");
        }

        if (DetailItems.Count == 0)
        {
            DetailItems.Add("No startup programs found in registry.");
        }

        DetailItems.Add("");
        DetailItems.Add("Tip: Use Task Manager (Ctrl+Shift+Esc) > Startup tab to manage startup programs.");

        ShowDetails = true;
    }

    private void FindCpuHogs(object? parameter)
    {
        ClearDetails();
        var cpuHogs = _fixService.GetCpuHogs();

        DetailsTitle = "Top CPU-Using Processes";
        foreach (var proc in cpuHogs)
        {
            DetailItems.Add($"{proc.Name} (PID: {proc.Id}) - CPU Time: {proc.CpuTime.TotalMinutes:F1} min");
        }

        DetailItems.Add("");
        DetailItems.Add("Tip: Use Task Manager (Ctrl+Shift+Esc) to end high-CPU processes.");

        ShowDetails = true;
    }

    private async Task ResetNetworkAsync()
    {
        ClearDetails();

        var result = MessageBox.Show(
            "This will reset your network stack and flush DNS. You may need to reconnect to WiFi networks. Continue?",
            "Reset Network?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var flushed = await _fixService.FlushDnsAsync();
            var reset = await _fixService.ResetNetworkStackAsync();

            DetailsTitle = "Network Reset Results";
            DetailItems.Add(flushed ? "DNS cache flushed successfully" : "Failed to flush DNS");
            DetailItems.Add(reset ? "Network stack reset successfully" : "Failed to reset network stack (may require admin)");
            DetailItems.Add("");
            DetailItems.Add("A system restart may be required for changes to take full effect.");
            ShowDetails = true;
        }
    }

    private void ClearDetails()
    {
        DetailItems.Clear();
        ShowDetails = false;
        StatusMessage = "";
    }
}
