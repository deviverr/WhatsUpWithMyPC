namespace WhatsUpWithMyPC.Models;

public enum CleanupCategory
{
    TempFiles,
    BrowserCache,
    WindowsCleanup,
    RecycleBin,
    LogFiles,
    Thumbnails,
    UpdateCache
}

public class CleanupItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CleanupCategory Category { get; set; }
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public bool IsSelected { get; set; }
    public bool RequiresAdmin { get; set; }

    public string SizeFormatted
    {
        get
        {
            if (SizeBytes >= 1_073_741_824) return $"{SizeBytes / 1_073_741_824.0:F2} GB";
            if (SizeBytes >= 1_048_576) return $"{SizeBytes / 1_048_576.0:F2} MB";
            if (SizeBytes >= 1_024) return $"{SizeBytes / 1_024.0:F2} KB";
            return $"{SizeBytes} B";
        }
    }

    public string CategoryIcon => Category switch
    {
        CleanupCategory.TempFiles => "\uE74C",
        CleanupCategory.BrowserCache => "\uE774",
        CleanupCategory.WindowsCleanup => "\uE770",
        CleanupCategory.RecycleBin => "\uE74D",
        CleanupCategory.LogFiles => "\uE7C3",
        CleanupCategory.Thumbnails => "\uE8B9",
        CleanupCategory.UpdateCache => "\uE777",
        _ => "\uE74C"
    };
}

public class CleanupResult
{
    public long TotalBytesFreed { get; set; }
    public int FilesDeleted { get; set; }
    public int FoldersDeleted { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;

    public string BytesFreedFormatted
    {
        get
        {
            if (TotalBytesFreed >= 1_073_741_824) return $"{TotalBytesFreed / 1_073_741_824.0:F2} GB";
            if (TotalBytesFreed >= 1_048_576) return $"{TotalBytesFreed / 1_048_576.0:F2} MB";
            if (TotalBytesFreed >= 1_024) return $"{TotalBytesFreed / 1_024.0:F2} KB";
            return $"{TotalBytesFreed} B";
        }
    }
}
