# SyncApp - Project Overview

## What is SyncApp?

SyncApp is a Windows desktop application that automatically synchronizes data from local SQL Server databases to cloud SQL Server databases. It runs as a system tray service and performs incremental syncing every 15 minutes.

## Key Features

### 🔍 SQL Server Discovery
- Automatic detection of local SQL Server and SQL Express instances
- Manual server/IP entry support
- Connection testing before configuration

### 📋 Table Selection
- Browse all tables in source database
- Select specific tables to sync
- Support for multiple schemas

### 🔄 Background Synchronization
- Runs as Windows service in system tray
- Automatic sync every 15 minutes
- Incremental sync based on timestamp columns
- Falls back to full sync when needed
- Automatic table creation in cloud database

### ⚙️ Configuration Management
- Persistent configuration storage
- Support for both Windows and SQL authentication
- Separate credentials for source and cloud databases
- Optional auto-start on Windows boot

### 🖥️ User Interface
- Clean WPF-based configuration window
- System tray integration
- Minimize to tray
- Easy configuration updates

## Architecture

### Technology Stack
- **Framework**: .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **Database**: SQL Server / SQL Server Express
- **Background Processing**: Microsoft.Extensions.Hosting BackgroundService
- **Data Access**: Microsoft.Data.SqlClient
- **System Integration**: Windows Registry, System Tray

### Project Structure
```
SyncApp/
├── Models/
│   └── SyncConfiguration.cs       # Configuration and table info models
├── Services/
│   ├── SqlServerService.cs        # SQL Server operations
│   └── DataSyncService.cs         # Background sync service
├── Helpers/
│   ├── ConfigurationManager.cs    # Config persistence
│   └── StartupManager.cs          # Windows startup integration
├── MainWindow.xaml[.cs]          # Main configuration UI
└── App.xaml[.cs]                 # Application entry point
```

## How It Works

### First Run
1. User launches application
2. Configuration window appears
3. User configures source database (local SQL Server)
4. User configures cloud database (target SQL Server)
5. User selects tables to sync
6. Configuration is saved
7. Application minimizes to system tray

### Background Operation
1. Background service starts
2. Every 15 minutes:
   - Reads configuration
   - For each selected table:
     - Checks if table exists in cloud (creates if not)
     - Detects if incremental sync is possible
     - Queries changed records
     - Bulk copies to cloud database
   - Updates last sync timestamp
3. Logs any errors
4. Repeats

### Incremental Sync Logic
- Looks for columns: `ModifiedDate`, `CreatedDate`, or `LastModified`
- If found: Only syncs records modified since last sync
- If not found: Performs full table sync
- Uses SQL Server Bulk Copy for performance

## Files and Locations

### Application Files
- **Installation**: `C:\Program Files\SyncApp\` (via installer)
- **Portable**: User-specified location
- **Executable**: `SyncApp.exe`

### Configuration
- **Path**: `%APPDATA%\SyncApp\config.json`
- **Format**: JSON
- **Contains**: Database credentials, selected tables, sync settings

### Startup Registry
- **Key**: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- **Value**: `SyncApp`
- **Data**: Path to SyncApp.exe

## Security Considerations

⚠️ **Important Security Notes**:

1. **Credentials Storage**: Passwords are stored in plain text in config.json
2. **Mitigation**: File is in user's AppData folder (user-only access by default)
3. **Recommendations**:
   - Use Windows Authentication when possible
   - Set appropriate file permissions on config.json
   - Use strong, unique passwords
   - Consider implementing encryption in production
   - Use VPN or SSL/TLS for cloud connections

## Performance Characteristics

### Startup Time
- ~2-5 seconds on modern hardware
- Loads configuration
- Starts background service
- Creates system tray icon

### Sync Performance
- Depends on:
  - Number of tables
  - Table sizes
  - Network bandwidth
  - Presence of timestamp columns
- Typical: 1000 rows/second for bulk copy
- Incremental sync much faster than full sync

### Resource Usage
- **Memory**: ~50-100 MB when idle
- **CPU**: <1% when idle, varies during sync
- **Network**: Depends on data volume
- **Disk**: Minimal (configuration file only)

## Limitations

### Current Limitations
1. **One-way sync only**: Source → Cloud (no bidirectional sync)
2. **No conflict resolution**: Last write wins
3. **Table schema changes**: Requires manual intervention
4. **Windows only**: No Linux/macOS support (WPF limitation)
5. **SQL Server only**: No support for other databases
6. **Plain text credentials**: Security risk for sensitive environments

### Scalability
- Suitable for: 
  - Small to medium databases
  - Tables with up to millions of rows
  - Dozens of tables
- Not suitable for:
  - Real-time sync requirements
  - Extremely large databases (TB+)
  - High-frequency updates (consider CDC instead)

## Documentation

### User Documentation
- **[README.md](README.md)** - Overview and basic usage
- **[QUICKSTART.md](QUICKSTART.md)** - 5-minute getting started guide
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Detailed deployment instructions

### Developer Documentation
- **[BUILD.md](BUILD.md)** - Build instructions and configurations
- **[PLATFORM_NOTES.md](PLATFORM_NOTES.md)** - Platform-specific build notes
- **[Installer/README.md](Installer/README.md)** - Installer build guide

## Building and Deployment

### Quick Build (Windows)
```powershell
dotnet build SyncApp.sln --configuration Release
```

### Create Portable Package
```powershell
.\build.ps1
```

### Create Installer (Windows only, requires WiX)
```powershell
dotnet tool install --global wix
cd Installer
dotnet build --configuration Release
```

### CI/CD
GitHub Actions workflow provided in `.github/workflows/build.yml`:
- Validates build on Linux
- Creates releases on Windows
- Generates portable and standalone packages
- Builds installer

## Requirements

### Development
- .NET 8.0 SDK
- Windows 10+ (for full development)
- Visual Studio 2022 or VS Code

### Runtime
- Windows 10 or later
- .NET 8.0 Runtime (or self-contained deployment)
- SQL Server access (source)
- Cloud SQL Server access (target)

## Future Enhancements

Potential improvements:
- [ ] Credential encryption
- [ ] Bidirectional sync
- [ ] Conflict resolution strategies
- [ ] Schema change detection
- [ ] Custom sync schedules
- [ ] Email notifications
- [ ] Detailed logging and monitoring
- [ ] Support for other databases
- [ ] REST API for remote management
- [ ] Multiple sync profiles

## Support

For issues, questions, or contributions:
- **GitHub**: https://github.com/accounts1-alt/sync-app
- **Issues**: Submit via GitHub Issues
- **Documentation**: See docs in repository

## License

This project is provided as-is for demonstration purposes.

---

**Version**: 1.0.0  
**Last Updated**: January 2026  
**Built with**: .NET 8.0, WPF, SQL Server
