using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncApp.Helpers;
using SyncApp.Models;

namespace SyncApp.Services;

public class DataSyncService : BackgroundService
{
    private readonly ILogger<DataSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(15);

    public DataSyncService(ILogger<DataSyncService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Sync Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = ConfigurationManager.LoadConfiguration();
                
                if (config != null && config.IsConfigured && config.SelectedTables.Count > 0)
                {
                    _logger.LogInformation("Starting sync cycle at {time}", DateTimeOffset.Now);
                    await PerformSync(config);
                    
                    config.LastSyncTime = DateTime.UtcNow;
                    ConfigurationManager.SaveConfiguration(config);
                    
                    _logger.LogInformation("Sync cycle completed at {time}", DateTimeOffset.Now);
                }
                else
                {
                    _logger.LogInformation("No configuration found or no tables selected, skipping sync");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync cycle");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }

        _logger.LogInformation("Data Sync Service stopped");
    }

    private async Task PerformSync(SyncConfiguration config)
    {
        var sourceConnString = SqlServerService.BuildConnectionString(
            config.SourceServer,
            config.SourceDatabase,
            config.SourceUsername,
            config.SourcePassword,
            config.SourceUseWindowsAuth
        );

        var cloudConnString = SqlServerService.BuildConnectionString(
            config.CloudServer,
            config.CloudDatabase,
            config.CloudUsername,
            config.CloudPassword,
            config.CloudUseWindowsAuth
        );

        foreach (var tableName in config.SelectedTables)
        {
            try
            {
                await SyncTable(sourceConnString, cloudConnString, tableName, config.LastSyncTime);
                _logger.LogInformation("Synced table: {tableName}", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing table {tableName}", tableName);
            }
        }
    }

    private async Task SyncTable(string sourceConnString, string cloudConnString, string tableName, DateTime? lastSyncTime)
    {
        using var sourceConn = new SqlConnection(sourceConnString);
        using var cloudConn = new SqlConnection(cloudConnString);
        
        await sourceConn.OpenAsync();
        await cloudConn.OpenAsync();

        // Ensure table exists in cloud database
        await EnsureTableExists(sourceConn, cloudConn, tableName);

        // For incremental sync, we look for a timestamp/modified column
        // If not available, we do a full sync
        var hasTimestampColumn = await HasTimestampColumn(sourceConn, tableName);
        
        string query;
        if (hasTimestampColumn && lastSyncTime.HasValue)
        {
            // Incremental sync based on ModifiedDate or similar column
            query = $@"
                SELECT * FROM {tableName}
                WHERE ModifiedDate > @LastSyncTime 
                   OR CreatedDate > @LastSyncTime
                   OR (ModifiedDate IS NULL AND CreatedDate > @LastSyncTime)";
        }
        else
        {
            // Full sync - get all data
            query = $"SELECT * FROM {tableName}";
        }

        using var sourceCmd = new SqlCommand(query, sourceConn);
        if (hasTimestampColumn && lastSyncTime.HasValue)
        {
            sourceCmd.Parameters.AddWithValue("@LastSyncTime", lastSyncTime.Value);
        }

        using var reader = await sourceCmd.ExecuteReaderAsync();
        var dataTable = new DataTable();
        dataTable.Load(reader);

        // Bulk insert or merge data into cloud database
        if (dataTable.Rows.Count > 0)
        {
            await BulkInsertData(cloudConn, tableName, dataTable);
        }
    }

    private async Task<bool> HasTimestampColumn(SqlConnection connection, string tableName)
    {
        var query = $@"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = '{tableName.Replace("'", "''")}' 
            AND (COLUMN_NAME = 'ModifiedDate' OR COLUMN_NAME = 'CreatedDate' OR COLUMN_NAME = 'LastModified')";

        using var cmd = new SqlCommand(query, connection);
        var result = await cmd.ExecuteScalarAsync();
        var count = result != null ? Convert.ToInt32(result) : 0;
        return count > 0;
    }

    private async Task EnsureTableExists(SqlConnection sourceConn, SqlConnection cloudConn, string tableName)
    {
        // Get table schema from source
        var schemaQuery = $@"
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{tableName.Replace("'", "''")}'
            ORDER BY ORDINAL_POSITION";

        using var schemaCmd = new SqlCommand(schemaQuery, sourceConn);
        using var reader = await schemaCmd.ExecuteReaderAsync();
        
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
            var isNullable = reader.GetString(3);

            var columnDef = $"[{columnName}] {dataType}";
            if (maxLength.HasValue && (dataType.ToLower() == "varchar" || dataType.ToLower() == "nvarchar" || dataType.ToLower() == "char" || dataType.ToLower() == "nchar"))
            {
                columnDef += maxLength.Value == -1 ? "(MAX)" : $"({maxLength.Value})";
            }
            columnDef += isNullable == "YES" ? " NULL" : " NOT NULL";
            
            columns.Add(columnDef);
        }
        reader.Close();

        if (columns.Count > 0)
        {
            // Check if table exists in cloud
            var checkTableQuery = $@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = '{tableName.Replace("'", "''")}'";

            using var checkCmd = new SqlCommand(checkTableQuery, cloudConn);
            var result = await checkCmd.ExecuteScalarAsync();
            var tableExists = result != null && Convert.ToInt32(result) > 0;

            if (!tableExists)
            {
                // Create table in cloud database
                var createTableQuery = $@"
                    CREATE TABLE [{tableName}] (
                        {string.Join(",\n                        ", columns)}
                    )";

                using var createCmd = new SqlCommand(createTableQuery, cloudConn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task BulkInsertData(SqlConnection connection, string tableName, DataTable dataTable)
    {
        using var bulkCopy = new SqlBulkCopy(connection);
        bulkCopy.DestinationTableName = tableName;
        bulkCopy.BulkCopyTimeout = 300; // 5 minutes timeout
        
        // Map columns
        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable);
    }
}
