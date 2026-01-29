using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC;

public partial class App : Application
{
    private TrayService? _trayService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize system tray
        _trayService = new TrayService();
        _trayService.Initialize();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _trayService?.Dispose();
        base.OnExit(e);
    }

    public void ShowMainWindow()
    {
        if (MainWindow == null)
        {
            MainWindow = new MainWindow();
        }
        MainWindow.Show();
        MainWindow.Activate();
    }

    public void ExitApplication()
    {
        _trayService?.Dispose();
        Shutdown();
    }
}
