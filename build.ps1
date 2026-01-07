# Build Script for SyncApp
# This script builds the application and creates a portable package

Write-Host "Building SyncApp..." -ForegroundColor Green

# Build the application
dotnet build SyncApp.sln --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create package directory using script location
$packageDir = Join-Path $PSScriptRoot "Package"
if (Test-Path $packageDir) {
    Remove-Item $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $packageDir | Out-Null

# Copy build output
$sourceDir = Join-Path $PSScriptRoot "SyncApp\bin\Release\net8.0-windows"
Copy-Item -Path "$sourceDir\*" -Destination $packageDir -Recurse

Write-Host "Package created in $packageDir" -ForegroundColor Green
Write-Host "You can now create an installer or distribute the contents of the Package folder" -ForegroundColor Cyan
