using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;

namespace SyncApp.Helpers;

public class StartupManager
{
    private const string AppName = "SyncApp";
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void EnableStartup()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                throw new Exception("Could not determine application path");
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to enable startup: {ex.Message}", ex);
        }
    }

    public static void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key?.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to disable startup: {ex.Message}", ex);
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
}
