using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SyncApp.Helpers;
using SyncApp.Models;
using SyncApp.Services;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;

namespace SyncApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private List<TableInfo> _tables = new List<TableInfo>();

    public MainWindow()
    {
        InitializeComponent();
        LoadSavedConfiguration();
    }

    private async void DetectServers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Detecting...";
            }

            var instances = await SqlServerService.DetectLocalInstances();
            cmbSourceServer.ItemsSource = instances;
            
            if (instances.Count > 0)
            {
                cmbSourceServer.SelectedIndex = 0;
                MessageBox.Show($"Found {instances.Count} SQL Server instance(s)", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No SQL Server instances detected. You can enter server name manually.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = "Detect";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error detecting servers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadSourceDatabases_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var server = cmbSourceServer.Text;
            if (string.IsNullOrWhiteSpace(server))
            {
                MessageBox.Show("Please enter a server name first", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Loading...";
            }

            var databases = await SqlServerService.GetDatabases(
                server,
                txtSourceUsername.Text,
                txtSourcePassword.Password,
                chkSourceWindowsAuth.IsChecked == true
            );

            cmbSourceDatabase.ItemsSource = databases;
            
            if (databases.Count > 0)
            {
                MessageBox.Show($"Found {databases.Count} database(s)", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = "Load DBs";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading databases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TestSourceConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var server = cmbSourceServer.Text;
            var database = cmbSourceDatabase.Text;

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
            {
                MessageBox.Show("Please enter server and database", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var success = await SqlServerService.TestConnection(
                server,
                database,
                txtSourceUsername.Text,
                txtSourcePassword.Password,
                chkSourceWindowsAuth.IsChecked == true
            );

            if (success)
            {
                MessageBox.Show("Connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Connection failed. Please check your credentials.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error testing connection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TestCloudConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var server = txtCloudServer.Text;
            var database = txtCloudDatabase.Text;

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
            {
                MessageBox.Show("Please enter server and database", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var success = await SqlServerService.TestConnection(
                server,
                database,
                txtCloudUsername.Text,
                txtCloudPassword.Password,
                chkCloudWindowsAuth.IsChecked == true
            );

            if (success)
            {
                MessageBox.Show("Connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Connection failed. Please check your credentials.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error testing connection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadTables_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var server = cmbSourceServer.Text;
            var database = cmbSourceDatabase.Text;

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
            {
                MessageBox.Show("Please select a server and database first", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Loading...";
            }

            _tables = await SqlServerService.GetTables(
                server,
                database,
                txtSourceUsername.Text,
                txtSourcePassword.Password,
                chkSourceWindowsAuth.IsChecked == true
            );

            lvTables.ItemsSource = _tables;

            if (_tables.Count > 0)
            {
                MessageBox.Show($"Found {_tables.Count} table(s)", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = "Load Tables";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tables: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(cmbSourceServer.Text) || string.IsNullOrWhiteSpace(cmbSourceDatabase.Text))
            {
                MessageBox.Show("Please configure source database", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCloudServer.Text) || string.IsNullOrWhiteSpace(txtCloudDatabase.Text))
            {
                MessageBox.Show("Please configure cloud database", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedTables = _tables.Where(t => t.IsSelected).Select(t => $"{t.Schema}.{t.TableName}").ToList();
            if (selectedTables.Count == 0)
            {
                MessageBox.Show("Please select at least one table to sync", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var config = new SyncConfiguration
            {
                SourceServer = cmbSourceServer.Text,
                SourceDatabase = cmbSourceDatabase.Text,
                SourceUsername = txtSourceUsername.Text,
                SourcePassword = txtSourcePassword.Password,
                SourceUseWindowsAuth = chkSourceWindowsAuth.IsChecked == true,
                
                CloudServer = txtCloudServer.Text,
                CloudDatabase = txtCloudDatabase.Text,
                CloudUsername = txtCloudUsername.Text,
                CloudPassword = txtCloudPassword.Password,
                CloudUseWindowsAuth = chkCloudWindowsAuth.IsChecked == true,
                
                SelectedTables = selectedTables,
                IsConfigured = true
            };

            ConfigurationManager.SaveConfiguration(config);

            // Handle startup setting
            if (chkRunOnStartup.IsChecked == true)
            {
                StartupManager.EnableStartup();
            }
            else
            {
                StartupManager.DisableStartup();
            }

            MessageBox.Show("Configuration saved successfully! The sync service will start syncing every 15 minutes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void SourceWindowsAuth_Changed(object sender, RoutedEventArgs e)
    {
        bool useWindowsAuth = chkSourceWindowsAuth.IsChecked == true;
        txtSourceUsername.IsEnabled = !useWindowsAuth;
        txtSourcePassword.IsEnabled = !useWindowsAuth;
    }

    private void CloudWindowsAuth_Changed(object sender, RoutedEventArgs e)
    {
        bool useWindowsAuth = chkCloudWindowsAuth.IsChecked == true;
        txtCloudUsername.IsEnabled = !useWindowsAuth;
        txtCloudPassword.IsEnabled = !useWindowsAuth;
    }

    private void LoadSavedConfiguration()
    {
        try
        {
            var config = ConfigurationManager.LoadConfiguration();
            if (config != null && config.IsConfigured)
            {
                cmbSourceServer.Text = config.SourceServer;
                cmbSourceDatabase.Text = config.SourceDatabase;
                txtSourceUsername.Text = config.SourceUsername;
                txtSourcePassword.Password = config.SourcePassword;
                chkSourceWindowsAuth.IsChecked = config.SourceUseWindowsAuth;

                txtCloudServer.Text = config.CloudServer;
                txtCloudDatabase.Text = config.CloudDatabase;
                txtCloudUsername.Text = config.CloudUsername;
                txtCloudPassword.Password = config.CloudPassword;
                chkCloudWindowsAuth.IsChecked = config.CloudUseWindowsAuth;

                chkRunOnStartup.IsChecked = StartupManager.IsStartupEnabled();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // When closing, minimize to tray instead
        e.Cancel = true;
        Hide();
    }
}