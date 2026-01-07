using System;
using System.IO;
using System.Text.Json;
using SyncApp.Models;

namespace SyncApp.Helpers;

public class ConfigurationManager
{
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SyncApp",
        "config.json"
    );

    public static void SaveConfiguration(SyncConfiguration config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    public static SyncConfiguration? LoadConfiguration()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<SyncConfiguration>(json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load configuration: {ex.Message}", ex);
        }
    }

    public static bool ConfigurationExists()
    {
        return File.Exists(ConfigFilePath);
    }
}
