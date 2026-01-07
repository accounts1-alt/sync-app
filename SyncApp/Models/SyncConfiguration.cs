using System;
using System.Collections.Generic;

namespace SyncApp.Models;

public class SyncConfiguration
{
    public string SourceServer { get; set; } = string.Empty;
    public string SourceDatabase { get; set; } = string.Empty;
    public string SourceUsername { get; set; } = string.Empty;
    public string SourcePassword { get; set; } = string.Empty;
    public bool SourceUseWindowsAuth { get; set; }
    
    public string CloudServer { get; set; } = string.Empty;
    public string CloudDatabase { get; set; } = string.Empty;
    public string CloudUsername { get; set; } = string.Empty;
    public string CloudPassword { get; set; } = string.Empty;
    public bool CloudUseWindowsAuth { get; set; }
    
    public List<string> SelectedTables { get; set; } = new List<string>();
    public bool IsConfigured { get; set; }
    public DateTime? LastSyncTime { get; set; }
}

public class TableInfo
{
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public bool IsSelected { get; set; }
}
