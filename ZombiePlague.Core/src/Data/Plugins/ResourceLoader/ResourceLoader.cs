using System.Text.Json;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Plugins.ResourceLoader;

internal sealed class ResourceLoader(ISwiftlyCore core) : IResourceLoader
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private const string PluginFolderName = "ResourceLoader";
    private const string ConfigFileName = "resources.json";

    private List<PrecacheItem> _resourcesToPrecache = [];

    public void Initialize()
    {
        var config = LoadConfig();
        _resourcesToPrecache = config.PrecacheResources;

        core.Event.OnPrecacheResource += OnPrecacheResources;
    }

    private void OnPrecacheResources(IOnPrecacheResourceEvent @event)
    {
        if (_resourcesToPrecache.Count == 0)
        {
            return;
        }

        foreach (var resource in _resourcesToPrecache)
        {
            if (!resource.Item.IsNullOrEmpty())
            {
                @event.AddItem(resource.Item);
            }
        }
    }
    
    private ResourcePrecacheConfig LoadConfig()
    {
        var configPath = GetConfigPath();
        
        EnsureDirectoryForFile(configPath);

        ResourcePrecacheConfig config = null!;
        
        if (!File.Exists(configPath))
        {
            config = new ResourcePrecacheConfig();
            config.PrecacheResources.Add(GetResourceConfigTemplate());
            
            SaveConfig(config);
        }

        try
        {
            var json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<ResourcePrecacheConfig>(json, _jsonOptions) ??
                         new ResourcePrecacheConfig();
        }
        catch (Exception ex)
        {
            core.Logger.LogError($"Error loading ResourceLoader config '{configPath}': {ex}");
        }
        
        return config;
    }
    
    private void SaveConfig(ResourcePrecacheConfig config)
    {
        var configPath = GetConfigPath();
        EnsureDirectoryForFile(configPath);

        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            core.Logger.LogError($"Error save ResourceLoader config: {ex.Message}");
        }
    }

    private string GetConfigPath()
    {
        return Path.Combine(core.PluginPath, PluginFolderName, ConfigFileName);
    }

    private void EnsureDirectoryForFile(string configPath)
    {
        var dir = Path.GetDirectoryName(configPath);
        
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private PrecacheItem GetResourceConfigTemplate()
    {
        return new PrecacheItem
        {
            Item = ""
        };
    }
}

public sealed class ResourcePrecacheConfig
{
    public List<PrecacheItem> PrecacheResources { get; set; } = [];
}

public sealed class PrecacheItem
{
    public string Item { get; set; }
}