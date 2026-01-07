# Build Configuration

## Build Commands

### Standard Build
Build the application for development/testing:
```powershell
dotnet build SyncApp.sln --configuration Release
```

### Portable Build
Create a portable package (requires .NET 8 Runtime on target):
```powershell
dotnet publish SyncApp/SyncApp.csproj -c Release -p:PublishProfile=PortablePublish
```
Output: `SyncApp/bin/Release/net8.0-windows/publish/`

### Standalone Build
Create a self-contained package (includes .NET Runtime):
```powershell
dotnet publish SyncApp/SyncApp.csproj -c Release -p:PublishProfile=StandalonePublish
```
Output: `SyncApp/bin/Release/net8.0-windows/publish-standalone/`

This creates a single EXE file with all dependencies embedded.

### Using the Build Script
A PowerShell script is provided for convenience:
```powershell
.\build.ps1
```

This will build the release version and create a Package folder.

## WiX Installer Build

To build the Windows Installer (.msi):

1. Install WiX Toolset v4:
   ```powershell
   dotnet tool install --global wix
   ```

2. Build the installer:
   ```powershell
   dotnet build Installer/Installer.wixproj --configuration Release
   ```

3. The MSI will be in: `Installer/bin/Release/`

## Build Outputs

### Debug Build
- Path: `SyncApp/bin/Debug/net8.0-windows/`
- Use for: Development and debugging

### Release Build
- Path: `SyncApp/bin/Release/net8.0-windows/`
- Use for: Testing production-ready builds

### Publish (Portable)
- Path: `SyncApp/bin/Release/net8.0-windows/publish/`
- Requires: .NET 8 Runtime on target machine
- Size: Smaller (~10-20 MB)
- Best for: Corporate environments with .NET already installed

### Publish (Standalone)
- Path: `SyncApp/bin/Release/net8.0-windows/publish-standalone/`
- Requires: Nothing (self-contained)
- Size: Larger (~60-80 MB)
- Best for: Distribution to users without .NET

### MSI Installer
- Path: `Installer/bin/Release/`
- Requires: Nothing (includes runtime if needed)
- Best for: Professional installation experience

## Target Frameworks

- **Target Framework**: .NET 8.0 Windows
- **Minimum Windows Version**: Windows 10 (1809 or later)
- **Architecture**: x64 (can be changed to x86 or ARM64)

## Build Requirements

### Development
- .NET 8.0 SDK
- Visual Studio 2022 (optional, for IDE)
- Windows 10 SDK (included with VS)

### CI/CD
The project can be built on:
- GitHub Actions (Windows runner)
- Azure DevOps (Windows agent)
- Any Windows build server with .NET 8 SDK

### Linux/Mac Build Notes
The WPF application requires Windows to build and run. On Linux/Mac:
- Cannot build the full application
- Can build class libraries only
- Consider using Windows VM or remote Windows build server

## Dependencies

All NuGet packages are restored automatically during build:
- Microsoft.Data.SqlClient (5.1.5)
- Microsoft.Extensions.Hosting (8.0.0)
- Microsoft.Extensions.Hosting.WindowsServices (8.0.0)
- Hardcodet.NotifyIcon.Wpf (1.1.0)
- System.Configuration.ConfigurationManager (8.0.0)

## Versioning

To update the version:

1. Edit `SyncApp/SyncApp.csproj`
2. Add version properties:
   ```xml
   <PropertyGroup>
     <Version>1.0.0</Version>
     <FileVersion>1.0.0.0</FileVersion>
     <AssemblyVersion>1.0.0.0</AssemblyVersion>
   </PropertyGroup>
   ```

3. Update installer version in `Installer/Product.wxs`:
   ```xml
   <Package Version="1.0.0.0" ... />
   ```

## Code Signing

For production deployment, sign the executable:

```powershell
# Using signtool (requires code signing certificate)
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com SyncApp.exe
```

## Performance Optimization

The build uses:
- **PublishReadyToRun**: Pre-compiles assemblies for faster startup
- **PublishTrimmed**: (Can be enabled) Removes unused code
- **TieredCompilation**: Optimizes hot paths at runtime

## Troubleshooting Build Issues

### "Windows targeting" Error
Add to .csproj:
```xml
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

### Missing SDK
Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download

### NuGet Restore Fails
Clear cache:
```powershell
dotnet nuget locals all --clear
dotnet restore
```

### WiX Build Fails
Ensure WiX Toolset v4 is installed:
```powershell
dotnet tool list -g
```
