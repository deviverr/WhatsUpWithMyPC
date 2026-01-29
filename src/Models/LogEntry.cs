namespace WhatsUpWithMyPC.Models;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }

    public string TimestampFormatted => Timestamp.ToString("HH:mm:ss");

    public string Icon => Level switch
    {
        LogLevel.Info => "\uE946",      // Info icon
        LogLevel.Success => "\uE73E",   // Checkmark
        LogLevel.Warning => "\uE7BA",   // Warning
        LogLevel.Error => "\uE711",     // Error X
        _ => "\uE946"
    };

    public string ColorKey => Level switch
    {
        LogLevel.Info => "TextSecondaryBrush",
        LogLevel.Success => "SuccessBrush",
        LogLevel.Warning => "WarningBrush",
        LogLevel.Error => "ErrorBrush",
        _ => "TextSecondaryBrush"
    };
}
