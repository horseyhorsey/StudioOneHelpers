using System.Xml;
using System.Text.Json;
using Blazored.LocalStorage;
using StudioOneHelpers.Models;

namespace StudioOneHelpers.Services;

public class PluginProcessingService
{
    private readonly ILocalStorageService _localStorage;

    public PluginProcessingService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>Extract Studio One plugins XML data to a plugins list</summary>
    /// <param name="xmlContent"></param>
    /// <returns></returns>
    public async Task<List<PluginItem>> ProcessPluginsAsync(string xmlContent)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        var pluginList = new List<PluginItem>();

        // Find all Section nodes
        var sections = doc.SelectNodes("//Section");
        if (sections != null)
        {
            foreach (XmlNode section in sections)
            {
                var classDescriptions = section.SelectNodes(".//ClassDescription");
                if (classDescriptions != null)
                {
                    foreach (XmlNode classDesc in classDescriptions)
                    {
                        var category = classDesc.Attributes?["category"]?.Value;
                        var name = classDesc.Attributes?["name"]?.Value;
                        var subCategory = classDesc.Attributes?["subCategory"]?.Value;
                        var classId = classDesc.Attributes?["classID"]?.Value;

                        // Get attributes from PersistentAttributes
                        var vendor = "";
                        var version = "";
                        var folder = "";

                        var persistentAttrs = classDesc.SelectSingleNode(".//PersistentAttributes");
                        if (persistentAttrs != null)
                        {
                            var vendorAttr = persistentAttrs.SelectSingleNode(".//Attribute[@id='Class:Vendor']");
                            vendor = vendorAttr?.Attributes?["value"]?.Value ?? "";

                            var versionAttr = persistentAttrs.SelectSingleNode(".//Attribute[@id='Class:Version']");
                            version = versionAttr?.Attributes?["value"]?.Value ?? "";

                            var folderAttr = persistentAttrs.SelectSingleNode(".//Attribute[@id='Class:Folder']");
                            folder = folderAttr?.Attributes?["value"]?.Value ?? "";
                        }

                        // Only add AudioSynth and AudioEffect plugins
                        if (category == "AudioSynth" || category == "AudioEffect")
                        {
                            pluginList.Add(new PluginItem
                            {
                                Category = category,
                                Name = name,
                                Vendor = vendor,
                                Version = version,
                                Folder = folder,
                                SubCategory = subCategory,
                                ClassId = classId
                            });
                        }
                    }
                }
            }
        }

        // Serialize to JSON and save to local storage
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(pluginList, options);
        await _localStorage.SetItemAsync("PluginsData", jsonString);

        return pluginList;
    }

    /// <summary>Load plugins from local storage</summary>
    /// <returns></returns>
    public async Task<List<PluginItem>?> LoadPluginsFromStorageAsync()
    {
        var json = await _localStorage.GetItemAsync<string>("PluginsData") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.Deserialize<List<PluginItem>>(json);
        }
        return null;
    }

    /// <summary>Filter plugins based on criteria</summary>
    /// <param name="plugins"></param>
    /// <param name="showVst3Only"></param>
    /// <param name="searchString"></param>
    /// <param name="activeFilter"></param>
    /// <param name="filterValue"></param>
    /// <returns></returns>
    public List<PluginItem> FilterPlugins(List<PluginItem> plugins, bool showVst3Only, string? searchString, string? activeFilter, string? filterValue)
    {
        var filteredList = showVst3Only 
            ? plugins.Where(x => x.SubCategory?.Contains("VST3") == true)
            : plugins;

        // Apply specific value filter if set (from button clicks)
        if (!string.IsNullOrEmpty(activeFilter) && !string.IsNullOrEmpty(filterValue))
        {
            filteredList = filteredList.Where(x => GetPropertyValue(x, activeFilter) == filterValue);
        }

        // Apply search string filter
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            filteredList = filteredList.Where(x =>
                x.Category?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                x.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                x.Vendor?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                x.Version?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                x.Folder?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                x.ClassId?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true);
        }

        return filteredList.ToList();
    }

    private string? GetPropertyValue(PluginItem item, string propertyName)
    {
        return propertyName switch
        {
            "Name" => item.Name,
            "Vendor" => item.Vendor,
            "Folder" => item.Folder,
            "ClassId" => item.ClassId,
            _ => null
        };
    }
}
