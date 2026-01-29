namespace WhatsUpWithMyPC.Models;

public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum IssueCategory
{
    System,
    Disk,
    Memory,
    Startup,
    Driver,
    Security,
    Update,
    Performance
}

public class HealthIssue
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
    public IssueCategory Category { get; set; }
    public string? FixAction { get; set; }
    public bool CanAutoFix { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.Now;

    public string SeverityIcon => Severity switch
    {
        IssueSeverity.Info => "\uE946",
        IssueSeverity.Warning => "\uE7BA",
        IssueSeverity.Error => "\uE783",
        IssueSeverity.Critical => "\uE730",
        _ => "\uE946"
    };

    public string SeverityColor => Severity switch
    {
        IssueSeverity.Info => "#0078D4",
        IssueSeverity.Warning => "#FF8C00",
        IssueSeverity.Error => "#E81123",
        IssueSeverity.Critical => "#E81123",
        _ => "#0078D4"
    };
}

public class HealthReport
{
    public List<HealthIssue> Issues { get; set; } = new();
    public DateTime ScanStarted { get; set; }
    public DateTime ScanCompleted { get; set; }
    public int TotalIssues => Issues.Count;
    public int CriticalCount => Issues.Count(i => i.Severity == IssueSeverity.Critical);
    public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
    public int InfoCount => Issues.Count(i => i.Severity == IssueSeverity.Info);

    public string OverallStatus
    {
        get
        {
            if (CriticalCount > 0) return "Critical Issues Found";
            if (ErrorCount > 0) return "Errors Found";
            if (WarningCount > 0) return "Warnings Found";
            if (InfoCount > 0) return "Minor Issues";
            return "System Healthy";
        }
    }

    public string OverallStatusColor
    {
        get
        {
            if (CriticalCount > 0 || ErrorCount > 0) return "#E81123";
            if (WarningCount > 0) return "#FF8C00";
            return "#107C10";
        }
    }
}
