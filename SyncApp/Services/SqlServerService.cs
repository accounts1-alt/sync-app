using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SyncApp.Models;

namespace SyncApp.Services;

public class SqlServerService
{
    public static async Task<List<string>> DetectLocalInstances()
    {
        return await Task.Run(() =>
        {
            var instances = new List<string>();
            
            // Add common local instance names
            instances.Add("(local)");
            instances.Add("localhost");
            instances.Add(".\\SQLEXPRESS");
            instances.Add("(localdb)\\MSSQLLocalDB");
            
            try
            {
                // Try to discover SQL Server instances using SQL Browser
                var enumerator = Microsoft.Data.Sql.SqlDataSourceEnumerator.Instance;
                var dt = enumerator.GetDataSources();
                foreach (DataRow row in dt.Rows)
                {
                    var serverName = row["ServerName"]?.ToString() ?? "";
                    var instanceName = row["InstanceName"]?.ToString() ?? "";
                    
                    if (!string.IsNullOrEmpty(serverName))
                    {
                        var fullName = string.IsNullOrEmpty(instanceName) 
                            ? serverName 
                            : $"{serverName}\\{instanceName}";
                        
                        if (!instances.Contains(fullName))
                        {
                            instances.Add(fullName);
                        }
                    }
                }
            }
            catch
            {
                // If enumeration fails, we still have the default instances
            }
            
            return instances.Distinct().ToList();
        });
    }

    public static async Task<bool> TestConnection(string server, string database, string username, string password, bool useWindowsAuth)
    {
        try
        {
            var connectionString = BuildConnectionString(server, database, username, password, useWindowsAuth);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<string>> GetDatabases(string server, string username, string password, bool useWindowsAuth)
    {
        var databases = new List<string>();
        try
        {
            var connectionString = BuildConnectionString(server, "master", username, password, useWindowsAuth);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve databases: {ex.Message}", ex);
        }
        
        return databases;
    }

    public static async Task<List<TableInfo>> GetTables(string server, string database, string username, string password, bool useWindowsAuth)
    {
        var tables = new List<TableInfo>();
        try
        {
            var connectionString = BuildConnectionString(server, database, username, password, useWindowsAuth);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = @"
                SELECT TABLE_SCHEMA, TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' 
                ORDER BY TABLE_SCHEMA, TABLE_NAME";
            
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(new TableInfo
                {
                    Schema = reader.GetString(0),
                    TableName = reader.GetString(1),
                    IsSelected = false
                });
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve tables: {ex.Message}", ex);
        }
        
        return tables;
    }

    public static string BuildConnectionString(string server, string database, string username, string password, bool useWindowsAuth)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            TrustServerCertificate = true,
            ConnectTimeout = 10
        };

        if (useWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = password;
        }

        return builder.ConnectionString;
    }
}
