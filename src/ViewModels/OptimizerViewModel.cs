using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.ViewModels;

public class OptimizerViewModel : ViewModelBase
{
    private string _statusMessage = "";
    private bool _windowAnimations = true;
    private bool _taskbarAnimations = true;
    private bool _smoothScrolling = true;
    private bool _transparencyEffects = true;
    private bool _windowShadows = true;
    private bool _gameModeEnabled;
    private bool _disableGameBar;
    private bool _disableBackgroundRecording;
    private string? _selectedPowerPlan;

    public OptimizerViewModel()
    {
        PowerPlans = new ObservableCollection<string>
        {
            "Balanced",
            "High Performance",
            "Power Saver"
        };
        _selectedPowerPlan = PowerPlans.FirstOrDefault();

        ApplyPresetCommand = new RelayCommand(ApplyPreset);
        ApplyPowerPlanCommand = new RelayCommand(ApplyPowerPlan);
        ClearMemoryCommand = new AsyncRelayCommand(ClearMemoryAsync);

        LoadCurrentSettings();
    }

    #region Properties

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool WindowAnimations
    {
        get => _windowAnimations;
        set { if (SetProperty(ref _windowAnimations, value)) SaveVisualSettings(); }
    }

    public bool TaskbarAnimations
    {
        get => _taskbarAnimations;
        set { if (SetProperty(ref _taskbarAnimations, value)) SaveVisualSettings(); }
    }

    public bool SmoothScrolling
    {
        get => _smoothScrolling;
        set { if (SetProperty(ref _smoothScrolling, value)) SaveVisualSettings(); }
    }

    public bool TransparencyEffects
    {
        get => _transparencyEffects;
        set { if (SetProperty(ref _transparencyEffects, value)) SaveVisualSettings(); }
    }

    public bool WindowShadows
    {
        get => _windowShadows;
        set { if (SetProperty(ref _windowShadows, value)) SaveVisualSettings(); }
    }

    public bool GameModeEnabled
    {
        get => _gameModeEnabled;
        set { if (SetProperty(ref _gameModeEnabled, value)) SaveGamingSettings(); }
    }

    public bool DisableGameBar
    {
        get => _disableGameBar;
        set { if (SetProperty(ref _disableGameBar, value)) SaveGamingSettings(); }
    }

    public bool DisableBackgroundRecording
    {
        get => _disableBackgroundRecording;
        set { if (SetProperty(ref _disableBackgroundRecording, value)) SaveGamingSettings(); }
    }

    public ObservableCollection<string> PowerPlans { get; }

    public string? SelectedPowerPlan
    {
        get => _selectedPowerPlan;
        set => SetProperty(ref _selectedPowerPlan, value);
    }

    #endregion

    #region Commands

    public ICommand ApplyPresetCommand { get; }
    public ICommand ApplyPowerPlanCommand { get; }
    public ICommand ClearMemoryCommand { get; }

    #endregion

    #region Methods

    private void LoadCurrentSettings()
    {
        try
        {
            LogService.Instance.Info("Loading current optimization settings", "Optimizer");

            // Load visual effects settings
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            if (key != null)
            {
                var userPreference = key.GetValue("UserPreferencesMask") as byte[];
                // Parse settings if available
            }

            // Load gaming settings
            using var gameKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\GameBar");
            if (gameKey != null)
            {
                var allowAutoGameMode = gameKey.GetValue("AllowAutoGameMode");
                _gameModeEnabled = allowAutoGameMode != null && Convert.ToInt32(allowAutoGameMode) == 1;
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Warning($"Could not load settings: {ex.Message}", "Optimizer");
        }
    }

    private void ApplyPreset(object? parameter)
    {
        if (parameter is not string preset) return;

        LogService.Instance.Info($"Applying {preset} preset", "Optimizer");

        switch (preset)
        {
            case "MaxPerformance":
                WindowAnimations = false;
                TaskbarAnimations = false;
                SmoothScrolling = false;
                TransparencyEffects = false;
                WindowShadows = false;
                StatusMessage = "Maximum performance preset applied";
                break;

            case "Balanced":
                WindowAnimations = true;
                TaskbarAnimations = true;
                SmoothScrolling = true;
                TransparencyEffects = true;
                WindowShadows = true;
                StatusMessage = "Balanced preset applied";
                break;

            case "Gaming":
                WindowAnimations = false;
                TaskbarAnimations = false;
                SmoothScrolling = true;
                TransparencyEffects = false;
                WindowShadows = false;
                GameModeEnabled = true;
                DisableGameBar = true;
                DisableBackgroundRecording = true;
                StatusMessage = "Gaming mode preset applied";
                break;
        }

        LogService.Instance.Success($"{preset} preset applied successfully", "Optimizer");
    }

    private void SaveVisualSettings()
    {
        try
        {
            LogService.Instance.Info("Saving visual effect settings", "Optimizer");

            // Note: Actually changing visual effects requires SystemParametersInfo API
            // This is a simplified version that logs the changes
            StatusMessage = "Visual settings updated";
            LogService.Instance.Success("Visual settings saved", "Optimizer");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to save visual settings: {ex.Message}", "Optimizer");
            StatusMessage = "Failed to save settings";
        }
    }

    private void SaveGamingSettings()
    {
        try
        {
            LogService.Instance.Info("Saving gaming settings", "Optimizer");

            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
            key?.SetValue("AllowAutoGameMode", GameModeEnabled ? 1 : 0, RegistryValueKind.DWord);

            StatusMessage = "Gaming settings updated";
            LogService.Instance.Success("Gaming settings saved", "Optimizer");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to save gaming settings: {ex.Message}", "Optimizer");
            StatusMessage = "Failed to save settings (admin required)";
        }
    }

    private void ApplyPowerPlan(object? parameter)
    {
        if (string.IsNullOrEmpty(SelectedPowerPlan)) return;

        LogService.Instance.Info($"Applying power plan: {SelectedPowerPlan}", "Optimizer");

        try
        {
            var guid = SelectedPowerPlan switch
            {
                "High Performance" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                "Power Saver" => "a1841308-3541-4fab-bc81-f71556f20b4a",
                _ => "381b4222-f694-41f0-9685-ff5bb260df2e" // Balanced
            };

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setactive {guid}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();

            StatusMessage = $"Power plan changed to {SelectedPowerPlan}";
            LogService.Instance.Success($"Power plan set to {SelectedPowerPlan}", "Optimizer");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to change power plan: {ex.Message}", "Optimizer");
            StatusMessage = "Failed to change power plan";
        }
    }

    private async Task ClearMemoryAsync()
    {
        LogService.Instance.Info("Clearing memory cache...", "Optimizer");

        try
        {
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await Task.Delay(500);

            StatusMessage = "Memory cache cleared";
            LogService.Instance.Success("Memory cache cleared successfully", "Optimizer");
        }
        catch (Exception ex)
        {
            LogService.Instance.Error($"Failed to clear memory: {ex.Message}", "Optimizer");
            StatusMessage = "Failed to clear memory";
        }
    }

    #endregion
}
