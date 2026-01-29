using WhatsUpWithMyPC.Models;
using WhatsUpWithMyPC.Services;

namespace WhatsUpWithMyPC.Services;

public class CleanupService
{
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<int>? ProgressChanged;

    public async Task<List<CleanupItem>> ScanForCleanupAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();
        LogService.Instance.Info("Starting cleanup scan...", "Cleanup");

        await Task.Run(() =>
        {
            // Windows Temp folder
            LogService.Instance.Info("Scanning Windows Temp folder...", "Cleanup");
            var windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            var winTempItem = ScanFolder("Windows Temp", windowsTemp, CleanupCategory.TempFiles, true);
            items.Add(winTempItem);
            if (winTempItem.SizeBytes > 0)
                LogService.Instance.Success($"Found {winTempItem.SizeFormatted} in Windows Temp", "Cleanup");

            // User Temp folder
            LogService.Instance.Info("Scanning User Temp folder...", "Cleanup");
            var userTemp = Path.GetTempPath();
            var userTempItem = ScanFolder("User Temp", userTemp, CleanupCategory.TempFiles, false);
            items.Add(userTempItem);
            if (userTempItem.SizeBytes > 0)
                LogService.Instance.Success($"Found {userTempItem.SizeFormatted} in User Temp", "Cleanup");

            // Browser caches
            LogService.Instance.Info("Scanning browser caches...", "Cleanup");
            var browserItems = ScanBrowserCaches();
            items.AddRange(browserItems);
            foreach (var browser in browserItems.Where(b => b.SizeBytes > 0))
                LogService.Instance.Success($"Found {browser.SizeFormatted} in {browser.Name}", "Cleanup");

            // Windows Update cache
            var updateCache = @"C:\Windows\SoftwareDistribution\Download";
            if (Directory.Exists(updateCache))
            {
                LogService.Instance.Info("Scanning Windows Update cache...", "Cleanup");
                var updateItem = ScanFolder("Windows Update Cache", updateCache, CleanupCategory.UpdateCache, true);
                items.Add(updateItem);
                if (updateItem.SizeBytes > 0)
                    LogService.Instance.Success($"Found {updateItem.SizeFormatted} in Update Cache", "Cleanup");
            }

            // Thumbnail cache
            var thumbCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\Explorer");
            if (Directory.Exists(thumbCache))
            {
                LogService.Instance.Info("Scanning thumbnail cache...", "Cleanup");
                var thumbItem = ScanFolder("Thumbnail Cache", thumbCache, CleanupCategory.Thumbnails, false, "thumbcache*.db");
                items.Add(thumbItem);
                if (thumbItem.SizeBytes > 0)
                    LogService.Instance.Success($"Found {thumbItem.SizeFormatted} in Thumbnails", "Cleanup");
            }

            // Recycle Bin
            LogService.Instance.Info("Scanning Recycle Bin...", "Cleanup");
            var recycleItem = GetRecycleBinInfo();
            items.Add(recycleItem);
            if (recycleItem.SizeBytes > 0)
                LogService.Instance.Success($"Found {recycleItem.SizeFormatted} in Recycle Bin", "Cleanup");

            // Windows log files
            var logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs");
            if (Directory.Exists(logsFolder))
            {
                LogService.Instance.Info("Scanning Windows logs...", "Cleanup");
                var logsItem = ScanFolder("Windows Logs", logsFolder, CleanupCategory.LogFiles, true);
                items.Add(logsItem);
                if (logsItem.SizeBytes > 0)
                    LogService.Instance.Success($"Found {logsItem.SizeFormatted} in Windows Logs", "Cleanup");
            }

        }, cancellationToken);

        var cleanableItems = items.Where(i => i.SizeBytes > 0).ToList();
        var totalSize = cleanableItems.Sum(i => i.SizeBytes);
        LogService.Instance.Success($"Scan complete: {cleanableItems.Count} locations, {FormatSize(totalSize)} recoverable", "Cleanup");
        return cleanableItems;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0 / 1024.0:F1} GB";
        if (bytes >= 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    private CleanupItem ScanFolder(string name, string path, CleanupCategory category, bool requiresAdmin, string pattern = "*")
    {
        var item = new CleanupItem
        {
            Name = name,
            Path = path,
            Category = category,
            RequiresAdmin = requiresAdmin,
            IsSelected = false
        };

        try
        {
            if (!Directory.Exists(path)) return item;

            var files = Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var info = new FileInfo(file);
                    item.SizeBytes += info.Length;
                    item.FileCount++;
                }
                catch
                {
                    // Skip inaccessible files
                }
            }

            item.Description = $"{item.FileCount:N0} files, {item.SizeFormatted}";
        }
        catch
        {
            item.Description = "Unable to scan (access denied)";
        }

        return item;
    }

    private List<CleanupItem> ScanBrowserCaches()
    {
        var items = new List<CleanupItem>();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Chrome
        var chromeCache = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache");
        if (Directory.Exists(chromeCache))
        {
            items.Add(ScanFolder("Chrome Cache", chromeCache, CleanupCategory.BrowserCache, false));
        }

        // Edge
        var edgeCache = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache");
        if (Directory.Exists(edgeCache))
        {
            items.Add(ScanFolder("Edge Cache", edgeCache, CleanupCategory.BrowserCache, false));
        }

        // Firefox
        var firefoxProfiles = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
        if (Directory.Exists(firefoxProfiles))
        {
            try
            {
                foreach (var profile in Directory.GetDirectories(firefoxProfiles))
                {
                    var cache2 = Path.Combine(profile, "cache2");
                    if (Directory.Exists(cache2))
                    {
                        items.Add(ScanFolder("Firefox Cache", cache2, CleanupCategory.BrowserCache, false));
                        break; // Usually only one profile
                    }
                }
            }
            catch
            {
                // Skip if access denied
            }
        }

        return items;
    }

    private CleanupItem GetRecycleBinInfo()
    {
        var item = new CleanupItem
        {
            Name = "Recycle Bin",
            Category = CleanupCategory.RecycleBin,
            RequiresAdmin = false,
            IsSelected = false
        };

        try
        {
            // Get recycle bin for all drives
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed) continue;

                var recyclePath = Path.Combine(drive.Name, "$Recycle.Bin");
                if (Directory.Exists(recyclePath))
                {
                    try
                    {
                        foreach (var userDir in Directory.GetDirectories(recyclePath))
                        {
                            foreach (var file in Directory.EnumerateFiles(userDir, "*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    item.SizeBytes += new FileInfo(file).Length;
                                    item.FileCount++;
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }

            item.Description = $"{item.FileCount:N0} items, {item.SizeFormatted}";
        }
        catch
        {
            item.Description = "Unable to scan";
        }

        return item;
    }

    public async Task<CleanupResult> CleanupAsync(List<CleanupItem> items, CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult();
        var selectedItems = items.Where(i => i.IsSelected).ToList();
        var totalItems = selectedItems.Count;
        var currentItem = 0;

        LogService.Instance.Info($"Starting cleanup of {totalItems} locations...", "Cleanup");

        foreach (var item in selectedItems)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogService.Instance.Warning("Cleanup cancelled by user", "Cleanup");
                break;
            }

            currentItem++;
            StatusChanged?.Invoke(this, $"Cleaning {item.Name}...");
            ProgressChanged?.Invoke(this, (int)((double)currentItem / totalItems * 100));
            LogService.Instance.Info($"Cleaning {item.Name}...", "Cleanup");

            try
            {
                if (item.Category == CleanupCategory.RecycleBin)
                {
                    await CleanRecycleBinAsync();
                    result.TotalBytesFreed += item.SizeBytes;
                    LogService.Instance.Success($"Emptied Recycle Bin ({item.SizeFormatted})", "Cleanup");
                }
                else
                {
                    var cleaned = await CleanFolderAsync(item.Path, cancellationToken);
                    result.TotalBytesFreed += cleaned.bytes;
                    result.FilesDeleted += cleaned.files;
                    result.FoldersDeleted += cleaned.folders;
                    LogService.Instance.Success($"Cleaned {item.Name}: {cleaned.files} files, {FormatSize(cleaned.bytes)}", "Cleanup");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.Name}: {ex.Message}");
                LogService.Instance.Error($"Failed to clean {item.Name}: {ex.Message}", "Cleanup");
            }
        }

        StatusChanged?.Invoke(this, "Cleanup complete");
        ProgressChanged?.Invoke(this, 100);
        LogService.Instance.Success($"Cleanup complete: {result.FilesDeleted} files deleted, {FormatSize(result.TotalBytesFreed)} freed", "Cleanup");

        return result;
    }

    private async Task<(long bytes, int files, int folders)> CleanFolderAsync(string path, CancellationToken cancellationToken)
    {
        long bytes = 0;
        int files = 0;
        int folders = 0;

        await Task.Run(() =>
        {
            if (!Directory.Exists(path)) return;

            // Delete files
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var info = new FileInfo(file);
                    var size = info.Length;
                    info.Delete();
                    bytes += size;
                    files++;
                }
                catch
                {
                    // Skip locked files
                }
            }

            // Delete empty folders
            foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                                         .OrderByDescending(d => d.Length))
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                        folders++;
                    }
                }
                catch
                {
                    // Skip if not empty or access denied
                }
            }
        }, cancellationToken);

        return (bytes, files, folders);
    }

    private Task CleanRecycleBinAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                // Use SHEmptyRecycleBin via shell32
                NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null,
                    NativeMethods.SHERB_NOCONFIRMATION |
                    NativeMethods.SHERB_NOPROGRESSUI |
                    NativeMethods.SHERB_NOSOUND);
            }
            catch
            {
                // May fail without admin
            }
        });
    }
}

internal static class NativeMethods
{
    public const int SHERB_NOCONFIRMATION = 0x00000001;
    public const int SHERB_NOPROGRESSUI = 0x00000002;
    public const int SHERB_NOSOUND = 0x00000004;

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    public static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, int dwFlags);
}
