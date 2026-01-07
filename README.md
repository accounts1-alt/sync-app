# sync-app

A Windows desktop application for synchronizing SQL Server database tables to a cloud SQL Server database.

## Features

- **SQL Server Instance Detection**: Automatically detects local SQL Server and SQL Express instances
- **Manual Configuration**: Supports manual server/IP entry with credentials
- **Table Selection**: Browse and select specific tables to sync
- **Background Sync Service**: Runs as a system tray application, syncing data every 15 minutes
- **Incremental Sync**: Tracks changes and syncs only modified data when possible
- **Windows Startup**: Optionally runs on Windows startup
- **System Tray Integration**: Minimizes to system tray for unobtrusive operation

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- SQL Server or SQL Server Express (for source database)
- Access to a cloud SQL Server database (for target/destination)

## Building the Application

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 (optional, for IDE support)

### Build Steps

1. Clone the repository
```bash
git clone https://github.com/accounts1-alt/sync-app.git
cd sync-app
```

2. Build the solution
```bash
dotnet build SyncApp.sln --configuration Release
```

3. Run the application
```bash
dotnet run --project SyncApp/SyncApp.csproj
```

### Creating an Installer

To create an installer using WiX Toolset:

1. Install WiX Toolset v4 or later
2. Build the installer project:
```bash
cd Installer
dotnet build
```

The installer will be generated in the `Installer/bin/Release` folder.

## Using the Application

### First Run Configuration

1. **Source Database Configuration**:
   - Click "Detect" to auto-discover local SQL Server instances
   - Or manually enter server name/IP
   - Select a database from the dropdown or enter manually
   - Choose Windows Authentication or enter SQL credentials
   - Click "Test Connection" to verify

2. **Cloud Database Configuration**:
   - Enter the cloud server IP or hostname
   - Enter the database name
   - Enter SQL credentials (Windows Auth typically not available for cloud)
   - Click "Test Connection" to verify

3. **Table Selection**:
   - Click "Load Tables" to retrieve tables from the source database
   - Check the tables you want to sync
   - At least one table must be selected

4. **Save Configuration**:
   - Optionally check "Run on Windows Startup" to launch the app on system boot
   - Click "Save Configuration" to save settings

5. **Minimize to Tray**:
   - Click "Minimize to Tray" or close the window to run in the background
   - The app will sync selected tables every 15 minutes

### System Tray

- **Double-click** the tray icon to open the configuration window
- **Right-click** for a context menu with options:
  - Open Configuration
  - Exit

## How It Works

### Sync Process

1. The application runs a background service (`DataSyncService`) that executes every 15 minutes
2. For each selected table:
   - Checks if the table exists in the cloud database (creates it if not)
   - Attempts incremental sync based on `ModifiedDate` or `CreatedDate` columns if available
   - Falls back to full table sync if timestamp columns don't exist
   - Uses SQL Server Bulk Copy for efficient data transfer

### Configuration Storage

Configuration is stored in JSON format at:
```
%APPDATA%\SyncApp\config.json
```

### Security Notes

- Passwords are stored in plain text in the configuration file
- Ensure the configuration file has appropriate file system permissions
- Consider using Windows Authentication when possible
- For production use, consider implementing encryption for stored credentials

## Troubleshooting

### Connection Issues
- Verify SQL Server is running and accessible
- Check firewall rules allow SQL Server connections (default port 1433)
- Verify credentials are correct
- Ensure the user has appropriate permissions on both databases

### Sync Issues
- Check the application logs in the Event Viewer (if configured)
- Verify network connectivity to the cloud SQL Server
- Ensure sufficient permissions to create tables and insert data in the cloud database
- Check that source tables have not been modified or deleted

### Startup Issues
- Verify .NET 8.0 Runtime is installed
- Check Windows Event Logs for application errors
- Try running as Administrator if permission issues occur

## Important Security Considerations

⚠️ **CRITICAL**: This application stores database passwords in **plain text** in the configuration file (`%APPDATA%\SyncApp\config.json`). This is a significant security risk.

### Recommendations for Production Use

1. **Use Windows Authentication** whenever possible to avoid storing passwords
2. **File System Permissions**: Ensure the config file has restrictive permissions (user-only access)
3. **Encryption**: For production deployments, consider implementing credential encryption
4. **Secure Connections**: Use VPN or SSL/TLS for connections to cloud databases
5. **Access Control**: Limit database user permissions to only what's necessary
6. **Password Rotation**: Regularly rotate passwords and update configuration
7. **Alternative Solutions**: Consider using Windows Credential Manager or Azure Key Vault for production

This application is intended for demonstration and development purposes. For production use, implement proper credential encryption or use managed identity/integrated authentication solutions.

## License

This project is provided as-is for demonstration purposes.
