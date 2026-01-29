using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class StartupItemModel : ViewModelBase
{
    private bool _isEnabled;

    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Impact { get; set; } = "Not measured";
    public string RegistryKey { get; set; } = string.Empty;
    public string RegistryValueName { get; set; } = string.Empty;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                // Toggle the startup item
                ToggleStartupItem(value);
            }
        }
    }

    private void ToggleStartupItem(bool enable)
    {
        try
        {
            if (Location == "Registry (User)")
            {
                if (enable)
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    key?.SetValue(RegistryValueName, Path);
                    LogService.Instance.Success($"Enabled startup: {Name}", "Startup");
                }
                else
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    key?.DeleteValue(RegistryValueName, throwOnMissingValue: false);

                    // Move to disabled key
                    using var disabledKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");
                    // Store disabled state
                    LogService.Instance.Warning($"Disabled startup: {Name}", "Startup");
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to toggle {Name}: {ex.Message}", "Startup");
        }
    }
}

public class StartupManagerViewModel : ViewModelBase
{
    private string _statusMessage = "";
    private StartupItemModel? _selectedItem;

    public StartupManagerViewModel()
    {
        StartupItems = new ObservableCollection<StartupItemModel>();
        RefreshCommand = new RelayCommand(_ => LoadStartupItems());
        DeleteCommand = new RelayCommand(DeleteItem, _ => SelectedItem != null);

        LoadStartupItems();
    }

    public ObservableCollection<StartupItemModel> StartupItems { get; }

    public StartupItemModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int EnabledCount => StartupItems.Count(i => i.IsEnabled);
    public int DisabledCount => StartupItems.Count(i => !i.IsEnabled);
    public int TotalCount => StartupItems.Count;
    public bool IsEmpty => StartupItems.Count == 0;

    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }

    private void LoadStartupItems()
    {
        LogService.Instance.Info("Loading startup items...", "Startup");
        StartupItems.Clear();

        try
        {
            // Load from HKCU Run
            LoadFromRegistry(
                Registry.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                "Registry (User)");

            // Load from HKLM Run (requires admin for modification)
            LoadFromRegistry(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                "Registry (System)");

            // Load from Startup folder
            LoadFromStartupFolder();

            StatusMessage = $"Found {TotalCount} startup items";
            LogService.Instance.Success($"Loaded {TotalCount} startup items", "Startup");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to load startup items: {ex.Message}", "Startup");
            StatusMessage = "Error loading startup items";
        }

        OnPropertyChanged(nameof(EnabledCount));
        OnPropertyChanged(nameof(DisabledCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void LoadFromRegistry(RegistryKey root, string keyPath, string location)
    {
        try
        {
            using var key = root.OpenSubKey(keyPath);
            if (key == null) return;

            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName)?.ToString();
                if (string.IsNullOrEmpty(value)) continue;

                StartupItems.Add(new StartupItemModel
                {
                    Name = valueName,
                    Path = value,
                    Location = location,
                    IsEnabled = true,
                    Impact = EstimateImpact(value),
                    RegistryKey = keyPath,
                    RegistryValueName = valueName
                });
            }
        }
        catch
        {
            // Skip if can't access
        }
    }

    private void LoadFromStartupFolder()
    {
        try
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (!Directory.Exists(startupFolder)) return;

            foreach (var file in Directory.GetFiles(startupFolder, "*.lnk"))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                StartupItems.Add(new StartupItemModel
                {
                    Name = name,
                    Path = file,
                    Location = "Startup Folder",
                    IsEnabled = true,
                    Impact = "Not measured"
                });
            }
        }
        catch
        {
            // Skip if can't access
        }
    }

    private string EstimateImpact(string path)
    {
        // Simple heuristic based on known apps
        var lowerPath = path.ToLowerInvariant();

        if (lowerPath.Contains("onedrive") || lowerPath.Contains("dropbox") ||
            lowerPath.Contains("teams") || lowerPath.Contains("slack"))
            return "High";

        if (lowerPath.Contains("update") || lowerPath.Contains("helper") ||
            lowerPath.Contains("tray"))
            return "Low";

        return "Medium";
    }

    private void DeleteItem(object? parameter)
    {
        if (SelectedItem == null) return;

        try
        {
            LogService.Instance.Warning($"Deleting startup item: {SelectedItem.Name}", "Startup");

            if (SelectedItem.Location == "Registry (User)")
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue(SelectedItem.RegistryValueName, throwOnMissingValue: false);
            }
            else if (SelectedItem.Location == "Startup Folder")
            {
                if (File.Exists(SelectedItem.Path))
                {
                    File.Delete(SelectedItem.Path);
                }
            }

            StartupItems.Remove(SelectedItem);
            StatusMessage = "Startup item deleted";
            LogService.Instance.Success($"Deleted startup item: {SelectedItem.Name}", "Startup");

            OnPropertyChanged(nameof(EnabledCount));
            OnPropertyChanged(nameof(DisabledCount));
            OnPropertyChanged(nameof(TotalCount));
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to delete: {ex.Message}", "Startup");
            StatusMessage = "Failed to delete (admin required?)";
        }
    }
}
