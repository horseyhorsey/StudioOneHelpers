using Blazored.LocalStorage;
using StudioOneHelpers.Models;
using System.Text.Json;

namespace StudioOneHelpers.Services;

public class PluginLookupService
{
    private readonly ILocalStorageService _localStorage;
    private readonly Dictionary<string, string> _pluginNameCache = new();
    private bool _cacheLoaded = false;

    public PluginLookupService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>Load plugin mappings from localStorage and build cache</summary>
    /// <returns>Dictionary of ClassID to Plugin Name mappings</returns>
    public async Task<Dictionary<string, string>> LoadPluginMappingsAsync()
    {
        try
        {
            _pluginNameCache.Clear();
            
            var plugins = await _localStorage.GetItemAsync<string>("PluginsData");
            if (!string.IsNullOrWhiteSpace(plugins))
            {
                var pluginList = JsonSerializer.Deserialize<List<PluginItem>>(plugins);
                if (pluginList != null)
                {
                    foreach (var plugin in pluginList)
                    {
                        if (!string.IsNullOrEmpty(plugin.ClassId) && !string.IsNullOrEmpty(plugin.Name))
                        {
                            _pluginNameCache[plugin.ClassId] = plugin.Name;
                        }
                    }
                }
            }
            
            _cacheLoaded = true;
            Console.WriteLine($"Loaded {_pluginNameCache.Count} plugin mappings into cache");
            return new Dictionary<string, string>(_pluginNameCache);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading plugin mappings: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>Get plugin name by ClassID from cache</summary>
    /// <param name="classId">The ClassID to look up</param>
    /// <returns>Plugin name if found, null otherwise</returns>
    public string? GetPluginNameByClassId(string? classId)
    {
        if (string.IsNullOrEmpty(classId))
            return null;

        return _pluginNameCache.TryGetValue(classId, out var pluginName) ? pluginName : null;
    }

    /// <summary>Refresh the cache by reloading from localStorage</summary>
    public async Task RefreshCacheAsync()
    {
        await LoadPluginMappingsAsync();
    }

    /// <summary>Check if cache has been loaded</summary>
    public bool IsCacheLoaded => _cacheLoaded;

    /// <summary>Get the number of cached plugin mappings</summary>
    public int CacheCount => _pluginNameCache.Count;

    /// <summary>Get all cached ClassIDs</summary>
    public IEnumerable<string> GetAllClassIds()
    {
        return _pluginNameCache.Keys;
    }

    /// <summary>Get all cached plugin names</summary>
    public IEnumerable<string> GetAllPluginNames()
    {
        return _pluginNameCache.Values;
    }
}
