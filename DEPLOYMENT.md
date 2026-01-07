# Deployment Guide

## Prerequisites

Before deploying SyncApp, ensure the following are installed on the target machine:

### Required Software
- **Windows 10 or later** (64-bit recommended)
- **.NET 8.0 Runtime** - Download from [Microsoft .NET Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server or SQL Server Express** (for source database)

### Network Requirements
- Access to local SQL Server instance
- Network connectivity to cloud SQL Server (typically over HTTPS/1433)
- Firewall rules allowing SQL Server connections

## Deployment Options

### Option 1: Using WiX Installer (Recommended)

1. **Build the installer** (requires WiX Toolset v4+):
   ```powershell
   cd Installer
   dotnet build --configuration Release
   ```

2. **Run the installer**:
   - Locate the generated `.msi` file in `Installer/bin/Release/`
   - Double-click to run the installer
   - Follow the installation wizard
   - The app will be installed to `C:\Program Files\SyncApp\`
   - Desktop and Start Menu shortcuts will be created

3. **First Run**:
   - Launch SyncApp from the desktop shortcut
   - Complete the configuration wizard (see Configuration Guide below)

### Option 2: Portable Deployment

1. **Build the application**:
   ```powershell
   # Run the build script
   .\build.ps1
   ```
   
   Or manually:
   ```powershell
   dotnet publish SyncApp/SyncApp.csproj -c Release -o ./publish
   ```

2. **Copy files**:
   - Copy all files from `./Package/` or `./publish/` folder to target machine
   - Extract to desired location (e.g., `C:\SyncApp\`)

3. **First Run**:
   - Run `SyncApp.exe` from the extracted folder
   - Complete the configuration wizard

### Option 3: Deploy from Source

1. **Clone the repository**:
   ```bash
   git clone https://github.com/accounts1-alt/sync-app.git
   cd sync-app
   ```

2. **Build**:
   ```powershell
   dotnet build SyncApp.sln --configuration Release
   ```

3. **Run**:
   ```powershell
   dotnet run --project SyncApp/SyncApp.csproj
   ```

## Configuration Guide

### Initial Setup

1. **Launch Application**: Start SyncApp for the first time

2. **Configure Source Database**:
   - Click **"Detect"** to auto-discover local SQL Server instances
   - Or manually enter server name (e.g., `localhost`, `.\SQLEXPRESS`, `SERVER01`)
   - Click **"Load DBs"** to retrieve available databases
   - Select your source database
   - Choose authentication method:
     - **Windows Authentication** (recommended for local)
     - **SQL Server Authentication** (enter username/password)
   - Click **"Test Connection"** to verify

3. **Configure Cloud Database**:
   - Enter cloud server IP or hostname (e.g., `sql.example.com`)
   - Enter database name
   - Enter SQL credentials (username and password)
   - Click **"Test Connection"** to verify

4. **Select Tables**:
   - Click **"Load Tables"** to retrieve available tables
   - Check the tables you want to synchronize
   - At least one table must be selected

5. **Finalize Configuration**:
   - (Optional) Check **"Run on Windows Startup"** to auto-start the app
   - Click **"Save Configuration"**
   - Click **"Minimize to Tray"** to run in background

### Running as a Background Service

After configuration, SyncApp runs in the system tray and automatically syncs data every 15 minutes.

**System Tray Features**:
- **Double-click** the tray icon to open configuration
- **Right-click** for menu:
  - Open Configuration
  - Status
  - Exit

**Auto-Start Configuration**:
- If "Run on Windows Startup" was checked, the app will automatically start when Windows boots
- Registry entry is created at: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`

## Sync Behavior

### How Syncing Works

1. **Scheduled Execution**: Sync runs automatically every 15 minutes
2. **Incremental Sync**: 
   - If tables have `ModifiedDate`, `CreatedDate`, or `LastModified` columns, only changed records are synced
   - Falls back to full table sync if timestamp columns don't exist
3. **Table Creation**: If tables don't exist in cloud database, they are automatically created
4. **Bulk Copy**: Uses SQL Server Bulk Copy for efficient data transfer

### Sync Process Details

For each selected table:
1. Connect to source database
2. Check if table exists in cloud database (create if not)
3. Detect timestamp columns for incremental sync
4. Query changed/new records
5. Bulk copy to cloud database

## Security Considerations

### Credentials Storage
- Configuration is stored in: `%APPDATA%\SyncApp\config.json`
- **Passwords are stored in plain text** - secure this file appropriately
- Recommended: Use Windows file permissions to restrict access

### Best Practices
1. **Use Windows Authentication** when possible for local database
2. **Use SQL Server Authentication** with strong passwords for cloud
3. **Limit database user permissions** to only required tables
4. **Use VPN or SSL/TLS** for cloud database connections
5. **Regularly rotate passwords**
6. **Monitor sync logs** for unusual activity

### Firewall Configuration
- Ensure port **1433** (default SQL Server port) is open for cloud connections
- Configure Windows Firewall to allow SyncApp.exe

## Troubleshooting

### Application Won't Start
- Verify .NET 8.0 Runtime is installed
- Check Windows Event Viewer for errors
- Try running as Administrator

### Connection Failures
- Verify SQL Server is running: `services.msc` → SQL Server service
- Test connectivity: `telnet <server> 1433`
- Check firewall rules
- Verify credentials are correct
- Ensure SQL Server allows remote connections

### Sync Not Working
- Check that configuration is saved
- Verify at least one table is selected
- Check network connectivity to cloud server
- Review logs (if configured)
- Verify user has permissions to create tables and insert data in cloud database

### Performance Issues
- Reduce number of selected tables
- Ensure timestamp columns exist for incremental sync
- Check network bandwidth
- Monitor SQL Server performance
- Consider adding indexes to timestamp columns

## Uninstallation

### If Installed via MSI
1. Open **Settings** → **Apps** → **Installed apps**
2. Find "SyncApp"
3. Click **Uninstall**
4. Follow the wizard

### If Portable Deployment
1. Stop the application (Exit from system tray)
2. Delete the application folder
3. Remove registry entry (if auto-start was enabled):
   - Run `regedit`
   - Navigate to: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
   - Delete the "SyncApp" value
4. Delete configuration: `%APPDATA%\SyncApp\`

## Support

For issues, questions, or feature requests, please:
- Check the README.md for general information
- Review this deployment guide
- Check the GitHub repository for updates
- Submit an issue on GitHub
