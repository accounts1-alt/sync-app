# Quick Start Guide

Get started with SyncApp in 5 minutes!

## Step 1: Install

### Windows Installer (Recommended)
1. Download the latest `.msi` installer from the releases page
2. Double-click to run
3. Follow the installation wizard
4. Launch from desktop shortcut

### Portable Version
1. Download and extract the ZIP file
2. Run `SyncApp.exe`

## Step 2: Configure Source Database

1. **Detect SQL Server**:
   - Click the **"Detect"** button
   - Select your SQL Server instance from the dropdown
   
2. **Load Database**:
   - Click **"Load DBs"** button
   - Select your database from the dropdown

3. **Authentication**:
   - Keep "Use Windows Authentication" checked (recommended)
   - Or uncheck and enter SQL credentials

4. **Test**:
   - Click **"Test Connection"** to verify it works

## Step 3: Configure Cloud Database

1. **Enter Server Details**:
   - Enter cloud server IP or hostname
   - Enter database name

2. **Enter Credentials**:
   - Uncheck "Use Windows Authentication"
   - Enter username and password

3. **Test**:
   - Click **"Test Connection"** to verify

## Step 4: Select Tables

1. Click **"Load Tables"** button
2. Check the boxes next to tables you want to sync
3. Must select at least one table

## Step 5: Save and Run

1. (Optional) Check **"Run on Windows Startup"** for auto-start
2. Click **"Save Configuration"**
3. Click **"Minimize to Tray"** to run in background

## You're Done! 🎉

The app will now:
- Run in the system tray
- Sync your selected tables every 15 minutes
- Start automatically on Windows boot (if enabled)

## System Tray

- **Double-click** icon = Open configuration
- **Right-click** icon = Menu (Open/Exit)

## Need Help?

See the full [Deployment Guide](DEPLOYMENT.md) or [README](README.md) for detailed information.
