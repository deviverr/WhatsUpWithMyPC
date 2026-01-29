using System.Collections.ObjectModel;
using WhatsUpWithMyPC.Models;

namespace WhatsUpWithMyPC.Services;

public class LogService
{
    private static LogService? _instance;
    private static readonly object _lock = new();

    public static LogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LogService();
                }
            }
            return _instance;
        }
    }

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public int MaxEntries { get; set; } = 500;

    private LogService()
    {
        Log(LogLevel.Info, "WhatsUpWithMyPC started", "App");
    }

    public void Log(LogLevel level, string message, string? source = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var entry = new LogEntry
            {
                Level = level,
                Message = message,
                Source = source,
                Timestamp = DateTime.Now
            };

            Logs.Add(entry);

            // Trim old entries if exceeding max
            while (Logs.Count > MaxEntries)
            {
                Logs.RemoveAt(0);
            }
        });
    }

    public void Info(string message, string? source = null) => Log(LogLevel.Info, message, source);
    public void Success(string message, string? source = null) => Log(LogLevel.Success, message, source);
    public void Warning(string message, string? source = null) => Log(LogLevel.Warning, message, source);
    public void Error(string message, string? source = null) => Log(LogLevel.Error, message, source);

    public void Clear()
    {
        Application.Current?.Dispatcher.Invoke(() => Logs.Clear());
    }
}
