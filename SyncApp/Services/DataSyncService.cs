using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Safely quotes a SQL identifier (table or column name) to prevent SQL injection.
    /// Note: Uses a restrictive alphanumeric+underscore pattern for security.
    /// This prevents potential SQL injection through complex Unicode or special characters
    /// while still supporting the vast majority of standard SQL Server table/column names.
    /// If you need to support tables with special characters, spaces, or Unicode names,
    /// consider implementing additional security measures or whitelisting specific tables.
    /// </summary>
    private static string QuoteSqlIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier cannot be null or whitespace", nameof(identifier));
        }

        // If already properly quoted, validate and return
        if (identifier.StartsWith("[") && identifier.EndsWith("]"))
        {
            identifier = identifier.Substring(1, identifier.Length - 2);
        }

        // Split on dot for schema.table notation
        var parts = identifier.Split('.');
        
        // Validate each part separately
        // Using restrictive pattern for security - only alphanumeric and underscore
        // This covers 99% of standard table/column names while preventing injection risks
        foreach (var part in parts)
        {
            var cleanPart = part.Trim('[', ']', ' ');
            
            if (!Regex.IsMatch(cleanPart, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException($"Invalid SQL identifier part: {cleanPart}. Only alphanumeric characters and underscores are allowed for security reasons.", nameof(identifier));
            }
        }

        // Quote each part
        var quotedParts = parts.Select(part =>
        {
            // Remove any existing brackets and trim
            var cleanPart = part.Trim('[', ']', ' ');
            // Quote the identifier (double any existing ] chars for safety)
            return $"[{cleanPart.Replace("]", "]]")}]";
        });

        return string.Join(".", quotedParts);
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

        // For incremental sync, we look for timestamp/modified columns
        var timestampColumns = await GetTimestampColumns(sourceConn, tableName);
        
        // Safely quote the table name to prevent SQL injection
        var quotedTableName = QuoteSqlIdentifier(tableName);
        
        string query;
        if (timestampColumns.Count > 0 && lastSyncTime.HasValue)
        {
            // Build incremental sync query using actual column names found
            // Quote each column name for safety
            var conditions = timestampColumns.Select(col => $"{QuoteSqlIdentifier(col)} > @LastSyncTime");
            var whereClause = string.Join(" OR ", conditions);
            
            query = $@"SELECT * FROM {quotedTableName} WHERE {whereClause}";
        }
        else
        {
            // Full sync - get all data
            query = $"SELECT * FROM {quotedTableName}";
        }

        using var sourceCmd = new SqlCommand(query, sourceConn);
        if (timestampColumns.Count > 0 && lastSyncTime.HasValue)
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

    private async Task<List<string>> GetTimestampColumns(SqlConnection connection, string tableName)
    {
        var parts = tableName.Split('.');
        string schema = parts.Length > 1 ? parts[0] : "dbo";
        string table = parts.Length > 1 ? parts[1] : parts[0];

        var query = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = @Schema
            AND TABLE_NAME = @TableName
            AND (COLUMN_NAME = 'ModifiedDate' OR COLUMN_NAME = 'CreatedDate' OR COLUMN_NAME = 'LastModified')";

        using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@Schema", schema);
        cmd.Parameters.AddWithValue("@TableName", table);
        
        var columns = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
        return columns;
    }

    private async Task EnsureTableExists(SqlConnection sourceConn, SqlConnection cloudConn, string tableName)
    {
        // Parse schema and table name safely
        var parts = tableName.Split('.');
        string schema = parts.Length > 1 ? parts[0] : "dbo";
        string table = parts.Length > 1 ? parts[1] : parts[0];

        // Get table schema from source using parameterized query
        var schemaQuery = @"
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION";

        using var schemaCmd = new SqlCommand(schemaQuery, sourceConn);
        schemaCmd.Parameters.AddWithValue("@Schema", schema);
        schemaCmd.Parameters.AddWithValue("@TableName", table);
        
        using var reader = await schemaCmd.ExecuteReaderAsync();
        
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
            var isNullable = reader.GetString(3);

            // Use QuoteSqlIdentifier for consistency and security
            var quotedColumnName = QuoteSqlIdentifier(columnName);
            var columnDef = $"{quotedColumnName} {dataType}";
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
            // Check if table exists in cloud using parameterized query
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName";

            using var checkCmd = new SqlCommand(checkTableQuery, cloudConn);
            checkCmd.Parameters.AddWithValue("@Schema", schema);
            checkCmd.Parameters.AddWithValue("@TableName", table);
            
            var result = await checkCmd.ExecuteScalarAsync();
            var tableExists = result != null && Convert.ToInt32(result) > 0;

            if (!tableExists)
            {
                // Create table in cloud database using safely quoted identifier
                var quotedTableName = QuoteSqlIdentifier(tableName);
                var createTableQuery = $@"
                    CREATE TABLE {quotedTableName} (
                        {string.Join(",\n                        ", columns)}
                    )";

                using var createCmd = new SqlCommand(createTableQuery, cloudConn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task BulkInsertData(SqlConnection connection, string tableName, DataTable dataTable)
    {
        // SqlBulkCopy.DestinationTableName expects an unquoted identifier
        // The table name from configuration is already validated in QuoteSqlIdentifier
        // but SqlBulkCopy handles quoting internally
        using var bulkCopy = new SqlBulkCopy(connection);
        bulkCopy.DestinationTableName = tableName; // Use unquoted name - SqlBulkCopy handles quoting
        bulkCopy.BulkCopyTimeout = 300; // 5 minutes timeout
        
        // Map columns
        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable);
    }
}
