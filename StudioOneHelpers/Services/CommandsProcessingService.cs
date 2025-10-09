using System.Text.Json;
using Blazored.LocalStorage;
using HtmlAgilityPack;

namespace StudioOneHelpers.Services;

public class CommandsProcessingService
{
    private readonly ILocalStorageService _localStorage;

    public CommandsProcessingService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>Extract S1 html table data to a commands list</summary>
    /// <param name="s1HtmlExport">The HTML content from Studio One shortcuts export</param>
    /// <returns>List of CommandItem objects</returns>
    public async Task<List<CommandItem>> ProcessCommandsAsync(string s1HtmlExport)
    {
        // Load HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(s1HtmlExport);

        // Dictionary to store data
        var commands = new List<CommandItem>();

        // Find all <h2> nodes
        var headers = doc.DocumentNode.SelectNodes("//h2");
        if (headers != null)
        {
            foreach (var header in headers)
            {
                string sectionName = header.InnerText.Trim();
                // Find the next <table> node after this header
                var table = header.SelectSingleNode("following-sibling::table[1]");
                if (table != null)
                {
                    var trNodes = table.SelectNodes(".//tr");
                    if (trNodes != null)
                    {
                        foreach (var tr in trNodes)
                        {
                            var tds = tr.SelectNodes(".//td");
                            if (tds != null && tds.Count > 0)
                            {
                                string commandName = tds[0].InnerText.Trim();
                                string shortcut = tds.Count > 1 ? tds[1].InnerText.Trim() : "";
                                commands.Add(new CommandItem
                                {
                                    SectionName = sectionName,
                                    CommandName = commandName,
                                    Shortcut = shortcut
                                });
                            }
                        }
                    }
                }
            }
        }

        return commands;
    }

    /// <summary>Save commands data to localStorage with import timestamp</summary>
    /// <param name="commands">List of commands to save</param>
    /// <returns>Task</returns>
    public async Task SaveCommandsToStorageAsync(List<CommandItem> commands)
    {
        // Serialize to JSON
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(commands, options);
        
        // Save to localStorage
        await _localStorage.SetItemAsync("CommandsData", jsonString);
        await _localStorage.SetItemAsync("CommandsData_ImportTime", DateTime.Now.ToString());
    }

    /// <summary>Load commands data from localStorage</summary>
    /// <returns>List of CommandItem objects or empty list if none found</returns>
    public async Task<List<CommandItem>> LoadCommandsFromStorageAsync()
    {
        try
        {
            var json = await _localStorage.GetItemAsync<string>("CommandsData") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(json))
            {
                return JsonSerializer.Deserialize<List<CommandItem>>(json) ?? new List<CommandItem>();
            }
        }
        catch (Exception)
        {
            // Return empty list if deserialization fails
        }
        
        return new List<CommandItem>();
    }

    /// <summary>Get the import timestamp for commands data</summary>
    /// <returns>Formatted timestamp string or "Unknown"</returns>
    public async Task<string> GetCommandsImportTimeAsync()
    {
        var importTime = await _localStorage.GetItemAsync<string>("CommandsData_ImportTime");
        if (!string.IsNullOrEmpty(importTime))
        {
            if (DateTime.TryParse(importTime, out var dateTime))
            {
                return dateTime.ToString("MMM dd, yyyy HH:mm");
            }
        }
        return "Unknown";
    }

    /// <summary>Check if commands data exists in localStorage</summary>
    /// <returns>True if commands data exists, false otherwise</returns>
    public async Task<bool> HasCommandsDataAsync()
    {
        var commandsData = await _localStorage.GetItemAsync<string>("CommandsData");
        return !string.IsNullOrEmpty(commandsData);
    }
}

/// <summary>Command item model for Studio One shortcuts</summary>
public class CommandItem
{
    public string? SectionName { get; set; }
    public string? CommandName { get; set; }
    public string? Shortcut { get; set; }
}
