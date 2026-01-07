# Platform-Specific Build Notes

## Linux/macOS Build Environment

This repository was developed and can be built on Linux/macOS, but with limitations:

### What Works
- ✅ Code compilation and validation
- ✅ NuGet package restoration
- ✅ Building to DLL
- ✅ Unit tests (if added)
- ✅ Code analysis

### What Doesn't Work
- ❌ Generating Windows .exe executable
- ❌ WPF designer (requires Windows)
- ❌ WiX installer build (Windows-only)
- ❌ Running the application (WPF requires Windows)
- ❌ Testing Windows-specific features (tray, registry, etc.)

## Windows Build Environment

When building on Windows, you get full functionality:

### What Works
- ✅ Everything from Linux/macOS
- ✅ **Generates .exe executable**
- ✅ WPF designer in Visual Studio
- ✅ WiX installer build
- ✅ Running and testing the application
- ✅ Code signing
- ✅ Creating single-file executables

### Build on Windows

```powershell
# Standard build
dotnet build SyncApp.sln --configuration Release

# Output includes:
# - SyncApp.exe (Windows executable)
# - SyncApp.dll
# - All dependencies
```

## CI/CD Recommendations

For a complete CI/CD pipeline:

1. **Code Quality Checks** (Linux/macOS/Windows):
   - Lint
   - Compile
   - Unit tests

2. **Build Artifacts** (Windows only):
   - Create release builds
   - Generate installers
   - Sign executables

### GitHub Actions Example

```yaml
name: Build

on: [push, pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore
        run: dotnet restore SyncApp.sln
      - name: Build
        run: dotnet build SyncApp.sln --configuration Release --no-restore

  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Build
        run: dotnet build SyncApp.sln --configuration Release
      - name: Publish
        run: dotnet publish SyncApp/SyncApp.csproj -c Release -o ./publish
      - name: Upload Artifact
        uses: actions/upload-artifact@v3
        with:
          name: SyncApp-Windows
          path: ./publish
```

## Current Build Status

This build was performed on: **Linux**

Therefore:
- DLL and dependencies are built ✅
- EXE is NOT generated (requires Windows build)
- Application can be tested by building on Windows

## For Contributors

If you're developing on:

- **Windows**: Full development experience, use Visual Studio 2022 or VS Code
- **macOS/Linux**: Can edit code, build, and validate, but cannot run or create final executables

We recommend Windows for full-stack development of this WPF application.
