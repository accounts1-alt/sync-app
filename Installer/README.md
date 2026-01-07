# Windows Installer

This directory contains WiX Toolset configuration for creating a Windows Installer (.msi) package.

## Important Note

**WiX Toolset v4 only works on Windows.** This installer project cannot be built on Linux or macOS.

## Building on Windows

1. **Install WiX Toolset v4**:
   ```powershell
   dotnet tool install --global wix
   ```

2. **Build the installer**:
   ```powershell
   dotnet build Installer.wixproj --configuration Release
   ```

3. **Output**:
   - The MSI installer will be in `bin/Release/`

## Alternative: Portable Deployment

If you cannot build the WiX installer, use the portable deployment option instead:

```powershell
# Build portable version
dotnet publish ../SyncApp/SyncApp.csproj -c Release -o ./publish

# Or use the build script
..\build.ps1
```

## Installer Features

When built, the installer provides:
- Installation to Program Files
- Desktop shortcut
- Start Menu shortcut
- Automatic uninstallation support
- Windows Installer database tracking

## Files

- `Installer.wixproj` - WiX project file
- `Product.wxs` - Main installer definition (WiX source)

## For Development

The installer project is excluded from the main solution file when building on non-Windows platforms. On Windows, you can add it back:

```powershell
dotnet sln add Installer/Installer.wixproj
```
