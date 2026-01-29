using System.Diagnostics;
using System.Net.NetworkInformation;
using WhatsUpWithMyPC.Helpers;
using WhatsUpWithMyPC.Models;

namespace WhatsUpWithMyPC.Services;

public class SystemMonitor : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly PerformanceCounter _cpuCounter;
    private readonly object _lock = new();
    private bool _isRunning;

    private long _lastNetworkBytesSent;
    private long _lastNetworkBytesReceived;
    private DateTime _lastNetworkCheck = DateTime.Now;

    public event EventHandler<SystemStats>? StatsUpdated;

    public SystemStats CurrentStats { get; private set; } = new();

    public SystemMonitor()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _timer = new System.Threading.Timer(UpdateStats, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start(int intervalMs = 1000)
    {
        if (_isRunning) return;

        _isRunning = true;
        // Initial read to prime the counter
        _cpuCounter.NextValue();
        _timer.Change(500, intervalMs);
    }

    public void Stop()
    {
        _isRunning = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void UpdateStats(object? state)
    {
        if (!_isRunning) return;

        lock (_lock)
        {
            try
            {
                var stats = new SystemStats
                {
                    LastUpdated = DateTime.Now
                };

                // CPU
                var cpuInfo = WmiHelper.GetCpuInfo();
                cpuInfo.UsagePercent = Math.Round(_cpuCounter.NextValue(), 1);
                stats.Cpu = cpuInfo;

                // Memory
                stats.Memory = WmiHelper.GetMemoryInfo();

                // Disks
                stats.Disks = WmiHelper.GetDiskInfo();

                // GPU
                stats.Gpu = WmiHelper.GetGpuInfo();

                // Battery
                stats.Battery = WmiHelper.GetBatteryInfo();

                // Network
                stats.Network = GetNetworkStats();

                // Uptime
                stats.Uptime = WmiHelper.GetSystemUptime();

                CurrentStats = stats;
                StatsUpdated?.Invoke(this, stats);
            }
            catch
            {
                // Silently handle errors to prevent crashes
            }
        }
    }

    private NetworkInfo GetNetworkStats()
    {
        var info = new NetworkInfo();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            if (interfaces.Count == 0)
            {
                info.IsConnected = false;
                return info;
            }

            info.IsConnected = true;
            info.AdapterName = interfaces.First().Name;

            long totalBytesSent = 0;
            long totalBytesReceived = 0;

            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPStatistics();
                totalBytesSent += stats.BytesSent;
                totalBytesReceived += stats.BytesReceived;
            }

            var now = DateTime.Now;
            var elapsed = (now - _lastNetworkCheck).TotalSeconds;

            if (elapsed > 0 && _lastNetworkBytesSent > 0)
            {
                info.UploadSpeedBps = (totalBytesSent - _lastNetworkBytesSent) / elapsed * 8;
                info.DownloadSpeedBps = (totalBytesReceived - _lastNetworkBytesReceived) / elapsed * 8;

                // Clamp negative values (can happen on counter reset)
                if (info.UploadSpeedBps < 0) info.UploadSpeedBps = 0;
                if (info.DownloadSpeedBps < 0) info.DownloadSpeedBps = 0;
            }

            _lastNetworkBytesSent = totalBytesSent;
            _lastNetworkBytesReceived = totalBytesReceived;
            _lastNetworkCheck = now;
        }
        catch
        {
            info.IsConnected = false;
        }

        return info;
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
        _cpuCounter.Dispose();
    }
}
