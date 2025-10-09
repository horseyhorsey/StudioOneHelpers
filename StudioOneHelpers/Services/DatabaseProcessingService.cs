using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.JSInterop;
using StudioOneHelpers.Models;

namespace StudioOneHelpers.Services;

public class DatabaseProcessingService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;

    public DatabaseProcessingService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    /// <summary>Check localStorage quota and provide user feedback</summary>
    /// <returns>True if storage is available, false if quota exceeded</returns>
    public async Task<bool> CheckStorageQuotaAsync()
    {
        try
        {
            // Try to store a small test item to check quota
            var testData = "quota_test";
            await _localStorage.SetItemAsync("quota_test", testData);
            await _localStorage.RemoveItemAsync("quota_test");
            return true;
        }
        catch (Exception ex) when (ex.Message.Contains("quota") || ex.Message.Contains("QuotaExceededError"))
        {
            return false;
        }
    }

    /// <summary>Extract PresetDescriptor data from SQLite database file</summary>
    /// <param name="fileBytes">The .db file as byte array</param>
    /// <returns>List of PresetDescriptor objects</returns>
    public async Task<List<PresetDescriptor>> ProcessDatabaseAsync(byte[] fileBytes)
    {
        try
        {
            // Use JavaScript-based SQLite processing
            var jsResult = await _jsRuntime.InvokeAsync<object[]>("processSqliteDatabase", fileBytes);
            
            var presetList = new List<PresetDescriptor>();
            var allowedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Artist", "AudioEffect", "AudioSynth", 
                "FXChain", "MusicEffect", "PatternBank", "TrackPreset"
            };
            
            foreach (var item in jsResult)
            {
                var jsonElement = (JsonElement)item;
                var category = jsonElement.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : null;
                
                // Only process presets with allowed categories
                if (!string.IsNullOrEmpty(category) && allowedCategories.Contains(category))
                {
                    var preset = new PresetDescriptor
                    {
                        Category = category,
                        ClassId = jsonElement.TryGetProperty("classId", out var classId) ? classId.GetString() : null,
                        Vendor = jsonElement.TryGetProperty("vendor", out var vendor) ? vendor.GetString() : null,
                        Title = jsonElement.TryGetProperty("title", out var title) ? title.GetString() : null,
                        Creator = jsonElement.TryGetProperty("creator", out var creator) ? creator.GetString() : null,
                        SubFolder = jsonElement.TryGetProperty("subFolder", out var subFolder) ? subFolder.GetString() : null
                    };
                    
                    presetList.Add(preset);
                }
            }
            
            // Group presets by category and save each category separately
            var groupedPresets = presetList.GroupBy(p => p.Category!);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var failedCategories = new List<string>();
            
            Console.WriteLine($"Processing {presetList.Count} presets into {groupedPresets.Count()} categories:");
            foreach (var group in groupedPresets)
            {
                Console.WriteLine($"  - {group.Key}: {group.Count()} presets");
            }
            
            foreach (var group in groupedPresets)
            {
                var categoryPresets = group.ToList();
                string jsonString = JsonSerializer.Serialize(categoryPresets, options);
                
                try
                {
                    await _localStorage.SetItemAsync($"PresetData_{group.Key}", jsonString);
                    Console.WriteLine($"Successfully stored {categoryPresets.Count} presets for category: {group.Key}");
                }
                catch (Exception ex) when (ex.Message.Contains("quota") || ex.Message.Contains("QuotaExceededError"))
                {
                    // If individual category storage fails due to quota, mark for fallback
                    failedCategories.Add(group.Key);
                    Console.WriteLine($"Failed to store {group.Key} separately due to quota: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Log any other errors
                    Console.WriteLine($"Error storing {group.Key}: {ex.Message}");
                    failedCategories.Add(group.Key);
                }
            }
            
            // Only save combined data if individual category storage failed
            if (failedCategories.Any())
            {
                Console.WriteLine($"Some categories failed to store individually, saving combined data as fallback");
                string allPresetsJson = JsonSerializer.Serialize(presetList, options);
                try
                {
                    await _localStorage.SetItemAsync("PresetData", allPresetsJson);
                }
                catch (Exception ex) when (ex.Message.Contains("quota") || ex.Message.Contains("QuotaExceededError"))
                {
                    // If even the combined storage fails, we need to clear some space
                    await ClearOldStorageData();
                    await _localStorage.SetItemAsync("PresetData", allPresetsJson);
                }
            }
            else
            {
                Console.WriteLine("All categories stored successfully - no combined storage needed");
            }
            
            // If some categories failed to store separately, store them in a compressed format
            if (failedCategories.Any())
            {
                await StoreFailedCategoriesAsCompressed(groupedPresets.Where(g => failedCategories.Contains(g.Key)), options);
            }
            
            return presetList;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error processing database: {ex.Message}", ex);
        }
    }

    /// <summary>Load presets from local storage</summary>
    /// <returns>List of PresetDescriptor objects or null if not found</returns>
    public async Task<List<PresetDescriptor>?> LoadPresetsFromStorageAsync()
    {
        var json = await _localStorage.GetItemAsync<string>("PresetData") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.Deserialize<List<PresetDescriptor>>(json);
        }
        return null;
    }

    /// <summary>Load presets for a specific category from local storage with optimized performance</summary>
    /// <param name="category">Category to load</param>
    /// <returns>List of PresetDescriptor objects for the category or null if not found</returns>
    public async Task<List<PresetDescriptor>?> LoadPresetsByCategoryFromStorageAsync(string category)
    {
        // Try to load from individual category storage first (fastest path)
        var json = await _localStorage.GetItemAsync<string>($"PresetData_{category}") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                return JsonSerializer.Deserialize<List<PresetDescriptor>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing category {category}: {ex.Message}");
                // Fall through to other methods
            }
        }
        
        // If individual storage failed, try compressed storage
        var compressedJson = await _localStorage.GetItemAsync<string>("PresetData_Compressed") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(compressedJson))
        {
            try
            {
                var compressedData = JsonSerializer.Deserialize<Dictionary<string, object>>(compressedJson);
                if (compressedData != null && compressedData.ContainsKey(category))
                {
                    // Convert compressed data back to PresetDescriptor objects
                    var categoryData = compressedData[category];
                    var categoryJson = JsonSerializer.Serialize(categoryData);
                    var compressedPresets = JsonSerializer.Deserialize<List<object>>(categoryJson);
                    
                    if (compressedPresets != null)
                    {
                        var presets = new List<PresetDescriptor>();
                        foreach (var preset in compressedPresets)
                        {
                            var presetJson = JsonSerializer.Serialize(preset);
                            var presetData = JsonSerializer.Deserialize<Dictionary<string, object>>(presetJson);
                            
                            if (presetData != null)
                            {
                                presets.Add(new PresetDescriptor
                                {
                                    Category = category,
                                    ClassId = presetData.TryGetValue("ClassId", out var classId) ? classId?.ToString() : null,
                                    Vendor = presetData.TryGetValue("Vendor", out var vendor) ? vendor?.ToString() : null,
                                    Title = presetData.TryGetValue("Title", out var title) ? title?.ToString() : null,
                                    Creator = presetData.TryGetValue("Creator", out var creator) ? creator?.ToString() : null,
                                    SubFolder = presetData.TryGetValue("SubFolder", out var subFolder) ? subFolder?.ToString() : null
                                });
                            }
                        }
                        return presets;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading compressed data for {category}: {ex.Message}");
            }
        }
        
        // Final fallback: load from combined storage and filter (slowest path)
        var allPresets = await LoadPresetsFromStorageAsync();
        if (allPresets != null)
        {
            return allPresets.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        return null;
    }

    /// <summary>Load presets for a specific category with pagination and sorting support for better performance</summary>
    /// <param name="category">Category to load</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchString">Optional search filter</param>
    /// <param name="sortBy">Property name to sort by</param>
    /// <param name="sortDirection">Sort direction (ascending/descending)</param>
    /// <returns>Paginated result with items and total count</returns>
    public async Task<(List<PresetDescriptor> Items, int TotalCount)> LoadPresetsByCategoryPaginatedAsync(string category, int page, int pageSize, string? searchString = null, string? sortBy = null, bool sortAscending = true, string? activeFilter = null, string? filterValue = null)
    {
        var allPresets = await LoadPresetsByCategoryFromStorageAsync(category);
        
        if (allPresets == null || !allPresets.Any())
        {
            return (new List<PresetDescriptor>(), 0);
        }
        
        // Apply search filter if provided
        var filteredPresets = string.IsNullOrWhiteSpace(searchString) && string.IsNullOrWhiteSpace(activeFilter)
            ? allPresets 
            : FilterPresets(allPresets, searchString, activeFilter, filterValue);
        
        // Apply sorting if specified
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            filteredPresets = sortBy.ToLower() switch
            {
                "vendor" => sortAscending 
                    ? filteredPresets.OrderBy(p => p.Vendor).ToList()
                    : filteredPresets.OrderByDescending(p => p.Vendor).ToList(),
                "classid" => sortAscending 
                    ? filteredPresets.OrderBy(p => p.ClassId).ToList()
                    : filteredPresets.OrderByDescending(p => p.ClassId).ToList(),
                "title" => sortAscending 
                    ? filteredPresets.OrderBy(p => p.Title).ToList()
                    : filteredPresets.OrderByDescending(p => p.Title).ToList(),
                "creator" => sortAscending 
                    ? filteredPresets.OrderBy(p => p.Creator).ToList()
                    : filteredPresets.OrderByDescending(p => p.Creator).ToList(),
                "subfolder" => sortAscending 
                    ? filteredPresets.OrderBy(p => p.SubFolder).ToList()
                    : filteredPresets.OrderByDescending(p => p.SubFolder).ToList(),
                _ => filteredPresets
            };
        }
        
        // Apply pagination
        var pagedItems = filteredPresets
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList();
        
        return (pagedItems, filteredPresets.Count);
    }

    /// <summary>Load all available categories from local storage</summary>
    /// <returns>List of available categories</returns>
    public async Task<List<string>> LoadAvailableCategoriesFromStorageAsync()
    {
        var allowedCategories = new[] { "Artist", "AudioEffect", "AudioSynth", "FXChain", "MusicEffect", "PatternBank", "TrackPreset" };
        var availableCategories = new List<string>();
        
        // First, check individual category storage
        foreach (var category in allowedCategories)
        {
            var json = await _localStorage.GetItemAsync<string>($"PresetData_{category}");
            if (!string.IsNullOrWhiteSpace(json))
            {
                availableCategories.Add(category);
            }
        }
        
        // If no individual categories found, check compressed storage
        if (!availableCategories.Any())
        {
            var compressedJson = await _localStorage.GetItemAsync<string>("PresetData_Compressed");
            if (!string.IsNullOrWhiteSpace(compressedJson))
            {
                try
                {
                    var compressedData = JsonSerializer.Deserialize<Dictionary<string, object>>(compressedJson);
                    if (compressedData != null)
                    {
                        availableCategories.AddRange(compressedData.Keys);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading compressed categories: {ex.Message}");
                }
            }
        }
        
        // Final fallback: check combined storage
        if (!availableCategories.Any())
        {
            var allPresets = await LoadPresetsFromStorageAsync();
            if (allPresets != null)
            {
                availableCategories = allPresets
                    .Where(p => !string.IsNullOrEmpty(p.Category))
                    .Select(p => p.Category!)
                    .Distinct()
                    .Where(c => allowedCategories.Contains(c))
                    .ToList();
            }
        }
        
        return availableCategories;
    }

    /// <summary>Split existing PresetData into separate category storage entries</summary>
    public async Task<bool> SplitExistingPresetDataAsync()
    {
        try
        {
            // Load the existing combined data
            var allPresets = await LoadPresetsFromStorageAsync();
            if (allPresets == null || !allPresets.Any())
            {
                Console.WriteLine("No existing PresetData found to split");
                return false;
            }

            Console.WriteLine($"Found {allPresets.Count} presets in combined storage, splitting by category...");

            // Group presets by category
            var groupedPresets = allPresets.GroupBy(p => p.Category!);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var successCount = 0;

            foreach (var group in groupedPresets)
            {
                var categoryPresets = group.ToList();
                string jsonString = JsonSerializer.Serialize(categoryPresets, options);
                
                try
                {
                    await _localStorage.SetItemAsync($"PresetData_{group.Key}", jsonString);
                    Console.WriteLine($"Successfully stored {categoryPresets.Count} presets for category: {group.Key}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error storing {group.Key}: {ex.Message}");
                }
            }

            Console.WriteLine($"Successfully split data into {successCount} categories");
            return successCount > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error splitting existing data: {ex.Message}");
            return false;
        }
    }

    /// <summary>Clear old storage data to free up space</summary>
    private async Task ClearOldStorageData()
    {
        try
        {
            // Clear old category-specific data
            var allowedCategories = new[] { "Artist", "AudioEffect", "AudioSynth", "FXChain", "MusicEffect", "PatternBank", "TrackPreset" };
            foreach (var category in allowedCategories)
            {
                await _localStorage.RemoveItemAsync($"PresetData_{category}");
            }
            
            // Clear any compressed data
            await _localStorage.RemoveItemAsync("PresetData_Compressed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing old storage data: {ex.Message}");
        }
    }

    /// <summary>Store failed categories in a compressed format</summary>
    private async Task StoreFailedCategoriesAsCompressed(IEnumerable<IGrouping<string, PresetDescriptor>> failedGroups, JsonSerializerOptions options)
    {
        try
        {
            var compressedData = new Dictionary<string, object>();
            
            foreach (var group in failedGroups)
            {
                // Store only essential data to reduce size
                var compressedPresets = group.Select(p => new
                {
                    p.ClassId,
                    p.Vendor,
                    p.Title,
                    p.Creator,
                    p.SubFolder
                }).ToList();
                
                compressedData[group.Key] = compressedPresets;
            }
            
            string compressedJson = JsonSerializer.Serialize(compressedData, options);
            await _localStorage.SetItemAsync("PresetData_Compressed", compressedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing compressed data: {ex.Message}");
        }
    }

    /// <summary>Get presets grouped by category</summary>
    /// <param name="presets">List of all presets</param>
    /// <returns>Dictionary with category as key and list of presets as value</returns>
    public Dictionary<string, List<PresetDescriptor>> GetPresetsByCategory(List<PresetDescriptor> presets)
    {
        if (presets == null || !presets.Any())
            return new Dictionary<string, List<PresetDescriptor>>();

        return presets
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .GroupBy(p => p.Category!)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>Get presets for a specific category</summary>
    /// <param name="presets">List of all presets</param>
    /// <param name="category">Category to filter by</param>
    /// <returns>List of presets for the specified category</returns>
    public List<PresetDescriptor> GetPresetsByCategory(List<PresetDescriptor> presets, string category)
    {
        if (presets == null || !presets.Any())
            return new List<PresetDescriptor>();

        return presets
            .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Get all available categories from presets</summary>
    /// <param name="presets">List of all presets</param>
    /// <returns>List of unique categories</returns>
    public List<string> GetAvailableCategories(List<PresetDescriptor> presets)
    {
        if (presets == null || !presets.Any())
            return new List<string>();

        return presets
            .Where(p => !string.IsNullOrEmpty(p.Category))
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    /// <summary>Filter presets based on criteria</summary>
    /// <param name="presets">List of presets to filter</param>
    /// <param name="searchString">Search term to filter by</param>
    /// <param name="activeFilter">Active filter property</param>
    /// <param name="filterValue">Filter value to match</param>
    /// <returns>Filtered list of presets</returns>
    public List<PresetDescriptor> FilterPresets(List<PresetDescriptor> presets, string? searchString, string? activeFilter, string? filterValue)
    {
        if (presets == null || !presets.Any())
            return new List<PresetDescriptor>();

        var filteredList = presets.AsEnumerable();

        // Apply specific value filter if set (from button clicks)
        if (!string.IsNullOrEmpty(activeFilter) && !string.IsNullOrEmpty(filterValue))
        {
            filteredList = filteredList.Where(x => GetPropertyValue(x, activeFilter) == filterValue);
        }

        // Apply search string filter with optimized string comparison
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var searchLower = searchString.ToLowerInvariant();
            filteredList = filteredList.Where(x =>
                (x.Category?.ToLowerInvariant().Contains(searchLower) == true) ||
                (x.ClassId?.ToLowerInvariant().Contains(searchLower) == true) ||
                (x.Vendor?.ToLowerInvariant().Contains(searchLower) == true) ||
                (x.Title?.ToLowerInvariant().Contains(searchLower) == true) ||
                (x.Creator?.ToLowerInvariant().Contains(searchLower) == true) ||
                (x.SubFolder?.ToLowerInvariant().Contains(searchLower) == true));
        }

        return filteredList.ToList();
    }

    private string? GetPropertyValue(PresetDescriptor item, string propertyName)
    {
        return propertyName switch
        {
            "Category" => item.Category,
            "ClassId" => item.ClassId,
            "Vendor" => item.Vendor,
            "Title" => item.Title,
            "Creator" => item.Creator,
            "SubFolder" => item.SubFolder,
            _ => null
        };
    }
}
