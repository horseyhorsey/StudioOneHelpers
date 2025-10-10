using System.Text.Json;
using Blazored.LocalStorage;
using StudioOneHelpers.Models;

namespace StudioOneHelpers.Services;

public class StickerLayoutService
{
    private readonly ILocalStorageService _localStorage;

    public StickerLayoutService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>Save sticker layout to localStorage</summary>
    /// <param name="buttons">List of controller buttons</param>
    /// <param name="gridRows">Number of rows in grid</param>
    /// <param name="gridColumns">Number of columns in grid</param>
    /// <returns>Task</returns>
    public async Task SaveLayoutToStorageAsync(List<ControllerButton> buttons, int gridRows, int gridColumns)
    {
        var layoutData = new
        {
            Buttons = buttons,
            GridRows = gridRows,
            GridColumns = gridColumns,
            LastModified = DateTime.Now
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(layoutData, options);
        
        await _localStorage.SetItemAsync("StickerLayout", jsonString);
    }

    /// <summary>Load sticker layout from localStorage</summary>
    /// <returns>Tuple of buttons list, rows, and columns</returns>
    public async Task<(List<ControllerButton> buttons, int rows, int columns)> LoadLayoutFromStorageAsync()
    {
        try
        {
            var json = await _localStorage.GetItemAsync<string>("StickerLayout") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var layoutData = JsonSerializer.Deserialize<dynamic>(json);
                if (layoutData != null)
                {
                    // Parse the dynamic object to extract data
                    var jsonDoc = JsonDocument.Parse(json);
                    var root = jsonDoc.RootElement;
                    
                    var buttons = new List<ControllerButton>();
                    if (root.TryGetProperty("Buttons", out var buttonsElement))
                    {
                        buttons = JsonSerializer.Deserialize<List<ControllerButton>>(buttonsElement.GetRawText()) ?? new List<ControllerButton>();
                    }
                    
                    var rows = root.TryGetProperty("GridRows", out var rowsElement) ? rowsElement.GetInt32() : 4;
                    var columns = root.TryGetProperty("GridColumns", out var columnsElement) ? columnsElement.GetInt32() : 4;
                    
                    return (buttons, rows, columns);
                }
            }
        }
        catch (Exception)
        {
            // Return default layout if deserialization fails
        }
        
        return (CreateDefaultLayout(4, 4), 4, 4);
    }

    /// <summary>Create default blank layout</summary>
    /// <param name="rows">Number of rows</param>
    /// <param name="columns">Number of columns</param>
    /// <returns>List of empty controller buttons</returns>
    public List<ControllerButton> CreateDefaultLayout(int rows, int columns)
    {
        var buttons = new List<ControllerButton>();
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                buttons.Add(new ControllerButton
                {
                    Row = row,
                    Column = col,
                    ButtonLabel = $"{row + 1}-{col + 1}"
                });
            }
        }
        
        return buttons;
    }

    /// <summary>Get available colors for buttons</summary>
    /// <returns>Dictionary of color names and hex values</returns>
    public Dictionary<string, string> GetAvailableColors()
    {
        return new Dictionary<string, string>
        {
            { "Green", "#4CAF50" },
            { "Red", "#F44336" },
            { "Pink", "#E91E63" },
            { "Blue", "#2196F3" },
            { "Yellow", "#FFEB3B" },
            { "Orange", "#FF9800" },
            { "Purple", "#9C27B0" },
            { "Teal", "#009688" }
        };
    }

    /// <summary>Check if layout data exists in localStorage</summary>
    /// <returns>True if layout exists, false otherwise</returns>
    public async Task<bool> HasLayoutDataAsync()
    {
        var layoutData = await _localStorage.GetItemAsync<string>("StickerLayout");
        return !string.IsNullOrEmpty(layoutData);
    }

    /// <summary>Save button size preferences to localStorage</summary>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height</param>
    /// <param name="unit">Unit of measurement (mm or cm)</param>
    /// <returns>Task</returns>
    public async Task SaveButtonSizePreferencesAsync(double width, double height, string unit)
    {
        var sizeData = new
        {
            Width = width,
            Height = height,
            Unit = unit,
            LastModified = DateTime.Now
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(sizeData, options);
        
        await _localStorage.SetItemAsync("StickerButtonSize", jsonString);
    }

    /// <summary>Load button size preferences from localStorage</summary>
    /// <returns>Tuple of width, height, and unit</returns>
    public async Task<(double width, double height, string unit)> LoadButtonSizePreferencesAsync()
    {
        try
        {
            var json = await _localStorage.GetItemAsync<string>("StickerButtonSize") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;
                
                var width = root.TryGetProperty("Width", out var widthElement) ? widthElement.GetDouble() : 20.0;
                var height = root.TryGetProperty("Height", out var heightElement) ? heightElement.GetDouble() : 15.0;
                var unit = root.TryGetProperty("Unit", out var unitElement) ? unitElement.GetString() ?? "mm" : "mm";
                
                return (width, height, unit);
            }
        }
        catch (Exception)
        {
            // Return default values if deserialization fails
        }
        
        return (20.0, 15.0, "mm"); // Default: 20mm x 15mm
    }
}
