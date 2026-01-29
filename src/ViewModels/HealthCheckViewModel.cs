using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WhatsUpWithMyPC.Models;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class HealthCheckViewModel : ViewModelBase
{
    private readonly HealthAnalyzer _analyzer;
    private CancellationTokenSource? _cts;

    private bool _isScanning;
    private string _scanStatus = "Ready to scan";
    private int _scanProgress;
    private bool _hasResults;
    private HealthReport? _report;
    private string _scanDuration = "";

    public HealthCheckViewModel()
    {
        _analyzer = new HealthAnalyzer();
        _analyzer.StatusChanged += (s, status) => ScanStatus = status;
        _analyzer.ProgressChanged += (s, progress) => ScanProgress = progress;

        Issues = new ObservableCollection<HealthIssue>();

        RunScanCommand = new AsyncRelayCommand(RunScanAsync, () => CanStartScan);
        FixIssueCommand = new AsyncRelayCommand(FixIssueAsync);
        FixAllCommand = new AsyncRelayCommand(FixAllAsync, () => HasFixableIssues);
    }

    public ObservableCollection<HealthIssue> Issues { get; }

    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(CanStartScan));
            }
        }
    }

    public string ScanStatus
    {
        get => _scanStatus;
        set => SetProperty(ref _scanStatus, value);
    }

    public int ScanProgress
    {
        get => _scanProgress;
        set => SetProperty(ref _scanProgress, value);
    }

    public bool HasResults
    {
        get => _hasResults;
        set => SetProperty(ref _hasResults, value);
    }

    public HealthReport? Report
    {
        get => _report;
        set
        {
            if (SetProperty(ref _report, value))
            {
                OnPropertyChanged(nameof(HasCritical));
                OnPropertyChanged(nameof(HasErrors));
                OnPropertyChanged(nameof(HasWarnings));
                OnPropertyChanged(nameof(HasInfo));
                OnPropertyChanged(nameof(HasFixableIssues));
                OnPropertyChanged(nameof(NoIssuesFound));
            }
        }
    }

    public string ScanDuration
    {
        get => _scanDuration;
        set => SetProperty(ref _scanDuration, value);
    }

    public bool CanStartScan => !IsScanning;
    public bool HasCritical => Report?.CriticalCount > 0;
    public bool HasErrors => Report?.ErrorCount > 0;
    public bool HasWarnings => Report?.WarningCount > 0;
    public bool HasInfo => Report?.InfoCount > 0;
    public bool HasFixableIssues => Issues.Any(i => i.CanAutoFix);
    public bool NoIssuesFound => HasResults && Report?.TotalIssues == 0;

    public ICommand RunScanCommand { get; }
    public ICommand FixIssueCommand { get; }
    public ICommand FixAllCommand { get; }

    private async Task RunScanAsync()
    {
        IsScanning = true;
        HasResults = false;
        Issues.Clear();

        _cts = new CancellationTokenSource();

        try
        {
            Report = await _analyzer.RunFullScanAsync(_cts.Token);

            foreach (var issue in Report.Issues)
            {
                Issues.Add(issue);
            }

            ScanDuration = $"{(Report.ScanCompleted - Report.ScanStarted).TotalSeconds:F1} seconds";
            HasResults = true;
        }
        catch (OperationCanceledException)
        {
            ScanStatus = "Scan cancelled";
        }
        catch (Exception ex)
        {
            ScanStatus = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task FixIssueAsync(object? parameter)
    {
        if (parameter is not HealthIssue issue) return;

        var fixService = new FixService();

        try
        {
            bool success = issue.FixAction switch
            {
                "RunCleanup" => await NavigateToCleanup(),
                "ClearMemory" => await fixService.ClearMemoryAsync(),
                "ManageStartup" => await NavigateToFixes(),
                _ => false
            };

            if (success)
            {
                Issues.Remove(issue);
                if (Report != null)
                {
                    Report.Issues.Remove(issue);
                    OnPropertyChanged(nameof(Report));
                    OnPropertyChanged(nameof(HasCritical));
                    OnPropertyChanged(nameof(HasErrors));
                    OnPropertyChanged(nameof(HasWarnings));
                    OnPropertyChanged(nameof(HasInfo));
                    OnPropertyChanged(nameof(HasFixableIssues));
                    OnPropertyChanged(nameof(NoIssuesFound));
                }
            }
        }
        catch
        {
            MessageBox.Show("Failed to fix the issue. You may need administrator privileges.",
                            "Fix Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
        }
    }

    private async Task FixAllAsync()
    {
        var fixableIssues = Issues.Where(i => i.CanAutoFix).ToList();

        foreach (var issue in fixableIssues)
        {
            await FixIssueAsync(issue);
        }
    }

    private Task<bool> NavigateToCleanup()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.NavigateCommand.Execute("Cleanup");
            }
        });
        return Task.FromResult(true);
    }

    private Task<bool> NavigateToFixes()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.NavigateCommand.Execute("Fixes");
            }
        });
        return Task.FromResult(true);
    }
}
