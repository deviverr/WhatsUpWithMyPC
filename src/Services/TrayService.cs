using System.Drawing;
using System.Windows.Forms;

namespace WhatsUpWithMyPC.Services;

public class TrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;

    public static TrayService? Instance { get; private set; }

    public void Initialize()
    {
        Instance = this;

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Open WhatsUpWithMyPC", null, OnOpen);
        _contextMenu.Items.Add("-");
        _contextMenu.Items.Add("Dashboard", null, (s, e) => NavigateTo("Dashboard"));
        _contextMenu.Items.Add("Health Check", null, (s, e) => NavigateTo("HealthCheck"));
        _contextMenu.Items.Add("Cleanup", null, (s, e) => NavigateTo("Cleanup"));
        _contextMenu.Items.Add("Quick Fixes", null, (s, e) => NavigateTo("Fixes"));
        _contextMenu.Items.Add("-");
        _contextMenu.Items.Add("Exit", null, OnExit);

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "WhatsUpWithMyPC - System Monitor",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += OnOpen;
    }

    private static Icon CreateDefaultIcon()
    {
        // Create a simple icon programmatically
        // In production, you'd load from resources
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.FromArgb(0, 120, 212)); // Windows blue
            using var font = new Font("Segoe UI", 14, System.Drawing.FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            g.DrawString("W", font, brush, 6, 4);
        }

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
    }

    public void UpdateTooltip(string cpuUsage, string ramUsage, string diskUsage)
    {
        if (_notifyIcon != null)
        {
            var text = $"WhatsUpWithMyPC\nCPU: {cpuUsage}\nRAM: {ramUsage}\nDisk: {diskUsage}";
            // NotifyIcon.Text is limited to 127 characters
            _notifyIcon.Text = text.Length > 127 ? text[..127] : text;
        }
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current is App app)
            {
                app.ShowMainWindow();
            }
        });
    }

    private void NavigateTo(string viewName)
    {
        OnOpen(null, EventArgs.Empty);

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow?.DataContext is ViewModels.MainViewModel vm)
            {
                vm.NavigateCommand.Execute(viewName);
            }
        });
    }

    private void OnExit(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current is App app)
            {
                app.ExitApplication();
            }
        });
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;

        Instance = null;
    }
}
