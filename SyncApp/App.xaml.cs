using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncApp.Services;
using SyncApp.Helpers;
using System.Windows.Forms;
using System.Drawing;
using Application = System.Windows.Application;

namespace SyncApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build host with background service
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<DataSyncService>();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                });
            })
            .Build();

        // Start the background service
        await _host.StartAsync();

        // Create tray icon
        SetupTrayIcon();

        // Show main window only if not configured yet
        var config = ConfigurationManager.LoadConfiguration();
        if (config == null || !config.IsConfigured)
        {
            ShowMainWindow();
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Sync App - Running"
        };

        var contextMenu = new ContextMenuStrip();
        
        var openMenuItem = new ToolStripMenuItem("Open Configuration");
        openMenuItem.Click += (s, e) => ShowMainWindow();
        contextMenu.Items.Add(openMenuItem);

        var statusMenuItem = new ToolStripMenuItem("Status: Running");
        statusMenuItem.Enabled = false;
        contextMenu.Items.Add(statusMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitMenuItem = new ToolStripMenuItem("Exit");
        exitMenuItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitMenuItem);

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closed += (s, e) => _mainWindow = null;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private async void ExitApplication()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Shutdown();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

