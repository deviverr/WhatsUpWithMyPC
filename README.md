# WUWMP - WhatsUpWithMyPC

A lightweight Windows PC health analyzer, cleaner, and repair utility. Designed to be minimal, fast, and effective.

![Windows 10/11](https://img.shields.io/badge/Windows-10%2F11-blue)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-2.0-orange)

## What's New in v2.0

- **Activity Log Panel**: Real-time logs showing what the app is doing
- **Performance Optimizer**: Tweak Windows settings for better performance
- **Startup Manager**: Full control over startup programs with impact analysis
- **Hardware Diagnostics**: Test disk health, RAM, and CPU stress tests
- **Enhanced Dashboard**: More stats including display info, process counts
- **Improved UI**: Better visual feedback and status indicators

## Features

### Dashboard
Real-time system monitoring with live statistics:
- **CPU**: Usage percentage, processor name, core count
- **RAM**: Used/Total memory, usage percentage
- **GPU**: Graphics card name, VRAM
- **Disk**: Drive usage, free space for all drives
- **Network**: Upload/download speeds, connection status
- **Battery**: Charge level, charging status (laptops)
- **Display**: Resolution, refresh rate, DPI scaling
- **System**: Process count, thread count, handle count
- **Uptime**: System uptime display

### Health Check
Comprehensive system diagnostics that scans for:
- Disk health and low space warnings
- High memory usage and memory leaks
- System errors from Windows Event Log
- Excessive startup programs
- Windows service issues
- Security status (Defender, Firewall)
- Large temporary file accumulation

### Cleanup
Free up disk space by removing:
- Windows Temp files
- User Temp files
- Browser cache (Chrome, Edge, Firefox)
- Windows Update cache
- Thumbnail cache
- Recycle Bin contents
- Old log files

Preview sizes before cleaning, with selective cleanup options.

### Performance Optimizer (NEW in v2.0)
Optimize Windows for better performance:

| Category | Options |
|----------|---------|
| Visual Effects | Disable animations, transparency, shadows |
| Power Plan | Balanced, High Performance, Power Saver |
| Memory | Clear standby list, working sets |
| Gaming | Disable Xbox DVR, enable Game Mode |

One-click presets:
- **Maximum Performance**: All optimizations enabled
- **Balanced**: Sensible defaults
- **Gaming Mode**: Optimized for gaming

### Startup Manager (NEW in v2.0)
Full control over startup programs:
- View all startup items (Registry + Startup folder)
- Impact rating (High/Medium/Low)
- Publisher and file path information
- Enable/Disable toggle for each item
- Refresh list to see changes

### Hardware Diagnostics (NEW in v2.0)
Test and diagnose hardware health:
- **Disk Health**: SMART status check for all drives
- **Memory Test**: RAM stress test with pattern verification
- **CPU Stress Test**: Multi-core load test
- **System Info**: Detailed hardware specifications

### Quick Fixes
One-click solutions for common problems:

| Problem | Solution |
|---------|----------|
| PC Won't Shut Down | Kills hanging processes, resets power config |
| Fast Battery Drain | Optimizes power settings, identifies power-hungry apps |
| Random Restarts | Analyzes crash logs, disables auto-restart on BSOD |
| RAM Full | Clears standby memory, shows memory hogs |
| Disk Full | Finds large files, quick cleanup |
| Slow Startup | Lists and manages startup programs |
| High CPU Usage | Identifies CPU-intensive processes |
| Network Issues | Resets network stack, flushes DNS |

### Activity Log (NEW in v2.0)
Collapsible panel showing real-time operation logs:
- Timestamped entries
- Color-coded by type (Info, Success, Warning, Error)
- Clear button to reset log
- Auto-scroll to latest entries

### System Tray
- Minimize to system tray for background monitoring
- Live stats in tooltip (CPU, RAM, Disk)
- Quick access menu to all features
- Notification alerts for issues
- Runs in background with minimal resource usage

## Screenshots

```
+-------------------------------------------+
|  WhatsUpWithMyPC v2.0           [_][#][X] |
+--------+----------------------------------+
| MONITOR    |                              |
|  Dashboard |    System Dashboard          |
| ANALYSIS   |                              |
|  Health    |   CPU: 15%    RAM: 8.2 GB    |
|  Cleanup   |   GPU: NVIDIA  Disk: 45%     |
| OPTIMIZE   |   Display: 1920x1080@144Hz   |
|  Optimizer |                              |
|  Startup   |                              |
| TOOLS      |                              |
|  Hardware  |                              |
|  Fixes     |                              |
+--------+----------------------------------+
| > Activity Log                    [Clear] |
| [12:34] i Scanning system...              |
| [12:35] * Found 234 MB temp files         |
+-------------------------------------------+
| CPU: 15%  RAM: 8.2/16GB  Disk: 45%       |
+-------------------------------------------+
```

## Requirements

- Windows 10 or Windows 11 (x64)
- .NET 8.0 Runtime (included in self-contained build)
- ~50MB disk space

## Installation

### Option 1: Download Release
1. Download the latest release from [Releases](../../releases)
2. Extract `WhatsUpWithMyPC.exe`
3. Run - no installation required!

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/WhatsUpWithMyPC.git
cd WhatsUpWithMyPC

# Build release version
dotnet publish src/WhatsUpWithMyPC.csproj -c Release

# Output: src/bin/Release/net8.0-windows/win-x64/publish/WhatsUpWithMyPC.exe
```

## Usage

1. **Launch**: Double-click `WhatsUpWithMyPC.exe`
2. **Dashboard**: View real-time system statistics
3. **Health Check**: Click "Run Full Scan" to check for issues
4. **Cleanup**: Scan for and remove unnecessary files
5. **Optimizer**: Tweak performance settings
6. **Startup**: Manage startup programs
7. **Hardware**: Run diagnostic tests
8. **Quick Fixes**: One-click solutions for common problems
9. **Activity Log**: Click to expand/collapse the log panel
10. **Minimize to Tray**: Click the tray icon or close button to minimize

### Tips
- Run as Administrator for full functionality (some fixes require elevated privileges)
- The app continues monitoring in the system tray when minimized
- Right-click the tray icon for quick access to features
- Check the Activity Log to see what operations are running

## Performance

WhatsUpWithMyPC is designed to be lightweight:

| Metric | Target | Typical |
|--------|--------|---------|
| Memory (idle) | < 50 MB | ~40 MB |
| Memory (scanning) | < 100 MB | ~70 MB |
| CPU (idle) | < 1% | ~0.5% |
| CPU (monitoring) | < 5% | ~2% |
| Startup time | < 2 sec | ~1 sec |
| Executable size | < 20 MB | ~15 MB |

## Building

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 (optional)

### Build Commands
```bash
# Debug build
dotnet build src/WhatsUpWithMyPC.csproj

# Release build
dotnet build src/WhatsUpWithMyPC.csproj -c Release

# Publish single-file executable
dotnet publish src/WhatsUpWithMyPC.csproj -c Release -r win-x64 --self-contained

# Run
dotnet run --project src/WhatsUpWithMyPC.csproj
```

## Project Structure

```
WhatsUpWithMyPC/
├── src/
│   ├── Models/           # Data models
│   ├── ViewModels/       # MVVM view models
│   ├── Views/            # WPF user controls
│   │   ├── DashboardView.xaml
│   │   ├── HealthCheckView.xaml
│   │   ├── CleanupView.xaml
│   │   ├── OptimizerView.xaml      # NEW
│   │   ├── StartupManagerView.xaml # NEW
│   │   ├── DiagnosticsView.xaml    # NEW
│   │   └── FixesView.xaml
│   ├── Services/         # Business logic
│   │   ├── LogService.cs           # NEW
│   │   └── ...
│   ├── Helpers/          # Utility classes
│   ├── Themes/           # UI styles
│   └── Assets/           # Icons, resources
├── WhatsUpWithMyPC.sln
├── README.md
└── .gitignore
```

## Technology Stack

- **Framework**: .NET 8.0 (LTS)
- **UI**: WPF (Windows Presentation Foundation)
- **Architecture**: MVVM (Model-View-ViewModel)
- **APIs**: WMI, Performance Counters, Windows API

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

### v2.0.0
- Added Activity Log panel with real-time operation logging
- Added Performance Optimizer with visual effects, power plans, and gaming optimizations
- Added Startup Manager with impact ratings and enable/disable controls
- Added Hardware Diagnostics with SMART, RAM test, and CPU stress test
- Enhanced Dashboard with display info and process counts
- Improved navigation with categorized sections
- Added logging to all operations for better transparency

### v1.0.0
- Initial release
- Dashboard with real-time monitoring
- Health Check diagnostics
- Cleanup utility
- Quick Fixes for common problems
- System tray integration

## Acknowledgments

- Built with WPF and .NET 8
- Icons from Segoe MDL2 Assets
- Inspired by the need for a simple, no-bloat PC utility

---

**Note**: This application is provided as-is. Always backup important data before running system cleanup or fix operations.
