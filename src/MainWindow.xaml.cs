using System.Windows;
using WhatsUpWithMyPC.ViewModels;

namespace WhatsUpWithMyPC;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Optional: Hide when minimized (tray icon will remain)
            // Hide();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        e.Cancel = true;
        Hide();

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ShowTrayNotification("WhatsUpWithMyPC is still running in the system tray.");
        }
    }
}
