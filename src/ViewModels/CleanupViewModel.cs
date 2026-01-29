using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsUpWithMyPC.Models;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class CleanupViewModel : ViewModelBase
{
    private readonly CleanupService _cleanupService;
    private CancellationTokenSource? _cts;

    private bool _isWorking;
    private string _statusMessage = "Ready";
    private int _progress;
    private bool _showResult;
    private CleanupResult? _result;

    public CleanupViewModel()
    {
        _cleanupService = new CleanupService();
        _cleanupService.StatusChanged += (s, status) => StatusMessage = status;
        _cleanupService.ProgressChanged += (s, progress) => Progress = progress;

        CleanupItems = new ObservableCollection<CleanupItem>();

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => CanScan);
        CleanCommand = new AsyncRelayCommand(CleanAsync, () => HasSelectedItems);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
    }

    public ObservableCollection<CleanupItem> CleanupItems { get; }

    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            if (SetProperty(ref _isWorking, value))
            {
                OnPropertyChanged(nameof(CanScan));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public bool ShowResult
    {
        get => _showResult;
        set => SetProperty(ref _showResult, value);
    }

    public CleanupResult? Result
    {
        get => _result;
        set => SetProperty(ref _result, value);
    }

    public bool HasItems => CleanupItems.Count > 0;
    public bool NoItemsFound => !IsWorking && CleanupItems.Count == 0 && !ShowResult;
    public bool CanScan => !IsWorking;
    public bool HasSelectedItems => CleanupItems.Any(i => i.IsSelected);
    public int SelectedCount => CleanupItems.Count(i => i.IsSelected);

    public string TotalSelectedSize
    {
        get
        {
            var totalBytes = CleanupItems.Where(i => i.IsSelected).Sum(i => i.SizeBytes);
            if (totalBytes >= 1_073_741_824) return $"{totalBytes / 1_073_741_824.0:F2} GB";
            if (totalBytes >= 1_048_576) return $"{totalBytes / 1_048_576.0:F2} MB";
            if (totalBytes >= 1_024) return $"{totalBytes / 1_024.0:F2} KB";
            return $"{totalBytes} B";
        }
    }

    public ICommand ScanCommand { get; }
    public ICommand CleanCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }

    private async Task ScanAsync()
    {
        IsWorking = true;
        ShowResult = false;
        CleanupItems.Clear();
        StatusMessage = "Scanning for cleanup items...";

        _cts = new CancellationTokenSource();

        try
        {
            var items = await _cleanupService.ScanForCleanupAsync(_cts.Token);

            foreach (var item in items.OrderByDescending(i => i.SizeBytes))
            {
                CleanupItems.Add(item);
            }

            StatusMessage = $"Found {items.Count} items to clean";
            NotifyPropertiesChanged();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task CleanAsync()
    {
        IsWorking = true;
        ShowResult = false;
        StatusMessage = "Cleaning...";

        _cts = new CancellationTokenSource();

        try
        {
            Result = await _cleanupService.CleanupAsync(CleanupItems.ToList(), _cts.Token);
            ShowResult = true;
            CleanupItems.Clear();
            StatusMessage = "Cleanup complete!";
            NotifyPropertiesChanged();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cleanup cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cleanup failed: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void SelectAll(object? parameter)
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = true;
        }
        NotifyPropertiesChanged();
    }

    private void DeselectAll(object? parameter)
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = false;
        }
        NotifyPropertiesChanged();
    }

    private void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(NoItemsFound));
        OnPropertyChanged(nameof(HasSelectedItems));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(TotalSelectedSize));
    }
}
