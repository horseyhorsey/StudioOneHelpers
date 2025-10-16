namespace StudioOneHelpers.Models;

/// <summary>Model for controller button in sticker layout</summary>
public class ControllerButton
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string? AssignedText { get; set; }
    public string? CustomName { get; set; }
    public string Color { get; set; } = "#4CAF50"; // Default green
    public string Shape { get; set; } = "square"; // "square" or "circle"
    public string? ButtonLabel { get; set; }
    public bool IsAssigned => !string.IsNullOrWhiteSpace(AssignedText);
}
