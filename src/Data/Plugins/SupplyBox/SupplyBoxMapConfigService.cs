using System.Text.Json;
using CS2ZombiePlague.Di;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace CS2ZombiePlague.Data.Plugins.SupplyBox;

public sealed class SupplyBoxMapConfigService
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    private const string PluginFolderName = "SupplyBox";
    private string? _configFileName;

    public MapSupplyBoxEntityConfig? MapConfig { get; private set; }
    public List<SupplyBoxEntityConfig>? SupplyBoxesData { get; private set; }

    public void LoadConfig(string mapName)
    {
        _configFileName = mapName + ".json";

        var configPath = GetConfigPath();
        EnsureDirectoryForFile(configPath);

        if (!File.Exists(configPath))
        {
            MapConfig = new MapSupplyBoxEntityConfig();
            SupplyBoxesData = MapConfig.SupplyBoxes;
            SaveConfig();
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<MapSupplyBoxEntityConfig>(json, _jsonOptions) ??
                         new MapSupplyBoxEntityConfig();
            MapConfig = config;
            SupplyBoxesData = config.SupplyBoxes;
        }
        catch (Exception ex)
        {
            _core.Logger.LogError($"Error loading map config '{configPath}': {ex}");
        }
    }

    public void SaveConfig()
    {
        var configPath = GetConfigPath();
        EnsureDirectoryForFile(configPath);

        try
        {
            var json = JsonSerializer.Serialize(MapConfig, _jsonOptions);
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            _core.Logger.LogError($"Error save map config: {ex.Message}");
        }
    }

    private string GetConfigPath()
    {
        return Path.Combine(_core.PluginPath, PluginFolderName, _configFileName);
    }

    private void EnsureDirectoryForFile(string configPath)
    {
        var dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}