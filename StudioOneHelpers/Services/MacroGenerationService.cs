using System.Xml;
using Microsoft.JSInterop;
using MudBlazor;
using Blazored.LocalStorage;
using StudioOneHelpers.Models;
using System.Text.Json;

namespace StudioOneHelpers.Services;

public class MacroGenerationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ISnackbar _snackbar;
    private readonly ILocalStorageService _localStorage;

    public MacroGenerationService(IJSRuntime jsRuntime, ISnackbar snackbar, ILocalStorageService localStorage)
    {
        _jsRuntime = jsRuntime;
        _snackbar = snackbar;
        _localStorage = localStorage;
    }

    public async Task GenerateAndDownloadMacroAsync(PluginItem plugin, string macroTitle, string groupName, string? description = null, string? selectedPreset = null)
    {
        try
        {
            string xmlContent = await GenerateMacroXmlAsync(plugin, macroTitle, groupName, description, selectedPreset);
            string fileName = $"{macroTitle}.studioonemacro";
            
            // Convert to bytes and trigger download
            var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
            await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));
            
            _snackbar.Add($"Macro '{macroTitle}' created successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error creating macro: {ex.Message}", Severity.Error);
        }
    }

    public async Task GenerateAndDownloadMacroAsync(PresetDescriptor preset, string macroTitle, string groupName, string? description = null, int mode = 0)
    {
        try
        {
            string xmlContent = await GenerateMacroXmlAsync(preset, macroTitle, groupName, description, mode);
            string fileName = $"{macroTitle}.studioonemacro";
            
            // Convert to bytes and trigger download
            var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
            await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));
            
            _snackbar.Add($"Macro '{macroTitle}' created successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error creating macro: {ex.Message}", Severity.Error);
        }
    }

    public async Task GenerateAndDownloadMacroAsync(PresetDescriptor preset, string macroTitle, string groupName, string? description = null)
    {
        try
        {
            string xmlContent = await GenerateMacroXmlAsync(preset, macroTitle, groupName, description);
            string fileName = $"{macroTitle}.studioonemacro";
            
            // Convert to bytes and trigger download
            var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
            await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));
            
            _snackbar.Add($"Macro '{macroTitle}' created successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error creating macro: {ex.Message}", Severity.Error);
        }
    }

    public async Task<string> GenerateMacroXmlAsync(PluginItem plugin, string macroTitle, string groupName, string? description = null, string? selectedPreset = null)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));

        var macro = xmlDoc.CreateElement("Macro");
        macro.SetAttribute("title", macroTitle);
        macro.SetAttribute("group", groupName);
        macro.SetAttribute("description", !string.IsNullOrWhiteSpace(description) ? description : $"Macro for {plugin.Name}");

        if (plugin.Category == "AudioSynth")
        {
            await GenerateAudioSynthMacroAsync(xmlDoc, macro, plugin, groupName, selectedPreset);
        }
        else if (plugin.Category == "AudioEffect")
        {
            await GenerateAudioEffectMacroAsync(xmlDoc, macro, plugin, groupName, selectedPreset);
        }

        xmlDoc.AppendChild(macro);
        return xmlDoc.OuterXml;
    }

    public async Task<string> GenerateMacroXmlAsync(PresetDescriptor preset, string macroTitle, string groupName, string? description = null, int mode = 0)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));

        var macro = xmlDoc.CreateElement("Macro");
        macro.SetAttribute("title", macroTitle);
        macro.SetAttribute("group", groupName);
        macro.SetAttribute("description", !string.IsNullOrWhiteSpace(description) ? description : $"Macro for {preset.Title}");

        if (preset.Category == "FXChain")
        {
            GenerateFXChainMacroAsync(xmlDoc, macro, preset, mode);
        }

        xmlDoc.AppendChild(macro);
        return xmlDoc.OuterXml;
    }

    public async Task<string> GenerateMacroXmlAsync(PresetDescriptor preset, string macroTitle, string groupName, string? description = null)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));

        var macro = xmlDoc.CreateElement("Macro");
        macro.SetAttribute("title", macroTitle);
        macro.SetAttribute("group", groupName);
        macro.SetAttribute("description", !string.IsNullOrWhiteSpace(description) ? description : $"Macro for {preset.Title}");

        if (string.Equals(preset.Category, "AudioEffect", StringComparison.OrdinalIgnoreCase))
        {
            await GenerateAudioEffectPresetMacroAsync(xmlDoc, macro, preset);
        }
        else if (string.Equals(preset.Category, "AudioSynth", StringComparison.OrdinalIgnoreCase))
        {
            await GenerateAudioSynthPresetMacroAsync(xmlDoc, macro, preset);
        }
        else if (string.Equals(preset.Category, "TrackPreset", StringComparison.OrdinalIgnoreCase))
        {
            await GenerateTrackPresetMacroAsync(xmlDoc, macro, preset);
        }

        xmlDoc.AppendChild(macro);
        return xmlDoc.OuterXml;
    }

    private async Task GenerateAudioSynthMacroAsync(XmlDocument xmlDoc, XmlElement macro, PluginItem plugin, string groupName, string? selectedPreset = null)
    {
        // Add Instrument Track
        var addTrackCmd = xmlDoc.CreateElement("CommandElement");
        addTrackCmd.SetAttribute("category", "Track");
        addTrackCmd.SetAttribute("name", "Add Instrument Track");
        
        var nameArg = xmlDoc.CreateElement("CommandArgument");
        nameArg.SetAttribute("name", "Name");
        nameArg.SetAttribute("value", plugin.Name ?? "INSTRUMENT");
        addTrackCmd.AppendChild(nameArg);
        macro.AppendChild(addTrackCmd);

        // Add Instrument to Selected Tracks
        var addInstrumentCmd = xmlDoc.CreateElement("CommandElement");
        addInstrumentCmd.SetAttribute("category", "Track");
        addInstrumentCmd.SetAttribute("name", "Add Instrument to Selected Tracks");
        
        var modeArg = xmlDoc.CreateElement("CommandArgument");
        modeArg.SetAttribute("name", "mode");
        modeArg.SetAttribute("value", "1");
        addInstrumentCmd.AppendChild(modeArg);
        
        var cidArg = xmlDoc.CreateElement("CommandArgument");
        cidArg.SetAttribute("name", "cid");
        cidArg.SetAttribute("value", plugin.ClassId ?? "");
        addInstrumentCmd.AppendChild(cidArg);
        
        // Group name
        var groupArg = xmlDoc.CreateElement("CommandArgument");
        groupArg.SetAttribute("name", "preset");
        groupArg.SetAttribute("value", $"{groupName}/Instruments/{plugin.Name}");
        addInstrumentCmd.AppendChild(groupArg);
        macro.AppendChild(addInstrumentCmd);

        // Load preset if selected
        if (!string.IsNullOrEmpty(selectedPreset))
        {
            var preset = await LoadPresetAsync(selectedPreset, plugin.ClassId, plugin.Category);
            if (preset != null)
            {
                var loadPresetCmd = xmlDoc.CreateElement("CommandElement");
                loadPresetCmd.SetAttribute("category", "Track");
                loadPresetCmd.SetAttribute("name", "Load Preset");
                
                var presetArg = xmlDoc.CreateElement("CommandArgument");
                presetArg.SetAttribute("name", "Preset");
                presetArg.SetAttribute("value", preset.Title ?? "");
                loadPresetCmd.AppendChild(presetArg);
                macro.AppendChild(loadPresetCmd);
            }
        }

        // Show Instrument Editor
        var showEditorCmd = xmlDoc.CreateElement("CommandElement");
        showEditorCmd.SetAttribute("category", "Console");
        showEditorCmd.SetAttribute("name", "Show Instrument Editor");
        
        var stateArg = xmlDoc.CreateElement("CommandArgument");
        stateArg.SetAttribute("name", "State");
        stateArg.SetAttribute("value", "");
        showEditorCmd.AppendChild(stateArg);
        macro.AppendChild(showEditorCmd);
    }

    private async Task GenerateAudioEffectMacroAsync(XmlDocument xmlDoc, XmlElement macro, PluginItem plugin, string groupName, string? selectedPreset = null)
    {
        // Add Insert to Selected Channels
        var addInsertCmd = xmlDoc.CreateElement("CommandElement");
        addInsertCmd.SetAttribute("category", "Track");
        addInsertCmd.SetAttribute("name", "Add Insert to Selected Channels");
        
        var modeArg = xmlDoc.CreateElement("CommandArgument");
        modeArg.SetAttribute("name", "mode");
        modeArg.SetAttribute("value", "1");
        addInsertCmd.AppendChild(modeArg);
        
        var cidArg = xmlDoc.CreateElement("CommandArgument");
        cidArg.SetAttribute("name", "cid");
        cidArg.SetAttribute("value", plugin.ClassId ?? "");
        addInsertCmd.AppendChild(cidArg);
        
        var presetArg = xmlDoc.CreateElement("CommandArgument");
        presetArg.SetAttribute("name", "preset");
        presetArg.SetAttribute("value", "default");
        addInsertCmd.AppendChild(presetArg);
        macro.AppendChild(addInsertCmd);

        // Load preset if selected
        if (!string.IsNullOrEmpty(selectedPreset))
        {
            var preset = await LoadPresetAsync(selectedPreset, plugin.ClassId, plugin.Category);
            if (preset != null)
            {
                var loadPresetCmd = xmlDoc.CreateElement("CommandElement");
                loadPresetCmd.SetAttribute("category", "Track");
                loadPresetCmd.SetAttribute("name", "Load Preset");
                
                var presetTitleArg = xmlDoc.CreateElement("CommandArgument");
                presetTitleArg.SetAttribute("name", "Preset");
                presetTitleArg.SetAttribute("value", preset.Title ?? "");
                loadPresetCmd.AppendChild(presetTitleArg);
                macro.AppendChild(loadPresetCmd);
            }
        }

        // Show Channel Editor
        var showChannelCmd = xmlDoc.CreateElement("CommandElement");
        showChannelCmd.SetAttribute("category", "Console");
        showChannelCmd.SetAttribute("name", "Show Channel Editor");
        
        var stateArg = xmlDoc.CreateElement("CommandArgument");
        stateArg.SetAttribute("name", "State");
        stateArg.SetAttribute("value", "");
        showChannelCmd.AppendChild(stateArg);
        macro.AppendChild(showChannelCmd);
    }

    private void GenerateFXChainMacroAsync(XmlDocument xmlDoc, XmlElement macro, PresetDescriptor preset, int mode)
    {
        // Add Insert to Selected Channels
        var addInsertCmd = xmlDoc.CreateElement("CommandElement");
        addInsertCmd.SetAttribute("category", "Track");
        addInsertCmd.SetAttribute("name", "Add Insert to Selected Channels");
        
        var modeArg = xmlDoc.CreateElement("CommandArgument");
        modeArg.SetAttribute("name", "mode");
        modeArg.SetAttribute("value", mode.ToString());
        addInsertCmd.AppendChild(modeArg);
        
        var cidArg = xmlDoc.CreateElement("CommandArgument");
        cidArg.SetAttribute("name", "cid");
        cidArg.SetAttribute("value", preset.ClassId ?? "");
        addInsertCmd.AppendChild(cidArg);
        
        // Build preset path from SubFolder/Title
        var presetPath = "";
        if (!string.IsNullOrEmpty(preset.SubFolder) && !string.IsNullOrEmpty(preset.Title))
        {
            presetPath = $"{preset.SubFolder}/{preset.Title}";
        }
        else if (!string.IsNullOrEmpty(preset.Title))
        {
            presetPath = preset.Title;
        }
        
        var presetArg = xmlDoc.CreateElement("CommandArgument");
        presetArg.SetAttribute("name", "preset");
        presetArg.SetAttribute("value", presetPath);
        addInsertCmd.AppendChild(presetArg);
        macro.AppendChild(addInsertCmd);
    }

    private async Task<PresetDescriptor?> LoadPresetAsync(string? selectedPreset, string? classId, string? category)
    {
        if (string.IsNullOrEmpty(selectedPreset) || string.IsNullOrEmpty(classId) || string.IsNullOrEmpty(category))
            return null;

        try
        {
            var json = await _localStorage.GetItemAsync<string>("PresetData") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var presets = JsonSerializer.Deserialize<List<PresetDescriptor>>(json);
                return presets?.FirstOrDefault(p => 
                    !string.IsNullOrEmpty(p.Title) && 
                    p.Title.Equals(selectedPreset, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(p.ClassId) && 
                    p.ClassId.Equals(classId, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(p.Category) && 
                    p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception)
        {
            // If there's an error loading presets, just continue without them
        }
        return null;
    }

    private async Task GenerateAudioEffectPresetMacroAsync(XmlDocument xmlDoc, XmlElement macro, PresetDescriptor preset)
    {
        // Add Insert to Selected Channels
        var addInsertCmd = xmlDoc.CreateElement("CommandElement");
        addInsertCmd.SetAttribute("category", "Track");
        addInsertCmd.SetAttribute("name", "Add Insert to Selected Channels");
        
        var modeArg = xmlDoc.CreateElement("CommandArgument");
        modeArg.SetAttribute("name", "mode");
        modeArg.SetAttribute("value", "1");
        addInsertCmd.AppendChild(modeArg);
        
        var cidArg = xmlDoc.CreateElement("CommandArgument");
        cidArg.SetAttribute("name", "cid");
        cidArg.SetAttribute("value", preset.ClassId ?? "");
        addInsertCmd.AppendChild(cidArg);
        
        // Build preset path from SubFolder/Title
        var presetPath = "";
        if (!string.IsNullOrEmpty(preset.SubFolder) && !string.IsNullOrEmpty(preset.Title))
        {
            presetPath = $"{preset.SubFolder}/{preset.Title}";
        }
        else if (!string.IsNullOrEmpty(preset.Title))
        {
            presetPath = preset.Title;
        }
        
        var presetArg = xmlDoc.CreateElement("CommandArgument");
        presetArg.SetAttribute("name", "preset");
        presetArg.SetAttribute("value", presetPath);
        addInsertCmd.AppendChild(presetArg);
        macro.AppendChild(addInsertCmd);

        // Show Channel Editor
        var showChannelCmd = xmlDoc.CreateElement("CommandElement");
        showChannelCmd.SetAttribute("category", "Console");
        showChannelCmd.SetAttribute("name", "Show Channel Editor");
        
        var stateArg = xmlDoc.CreateElement("CommandArgument");
        stateArg.SetAttribute("name", "State");
        stateArg.SetAttribute("value", "");
        showChannelCmd.AppendChild(stateArg);
        macro.AppendChild(showChannelCmd);
    }

    private async Task GenerateAudioSynthPresetMacroAsync(XmlDocument xmlDoc, XmlElement macro, PresetDescriptor preset)
    {
        // Add Instrument Track
        var addTrackCmd = xmlDoc.CreateElement("CommandElement");
        addTrackCmd.SetAttribute("category", "Track");
        addTrackCmd.SetAttribute("name", "Add Instrument Track");
        
        var nameArg = xmlDoc.CreateElement("CommandArgument");
        nameArg.SetAttribute("name", "Name");
        nameArg.SetAttribute("value", preset.Title ?? "INSTRUMENT");
        addTrackCmd.AppendChild(nameArg);
        macro.AppendChild(addTrackCmd);

        // Add Instrument to Selected Tracks
        var addInstrumentCmd = xmlDoc.CreateElement("CommandElement");
        addInstrumentCmd.SetAttribute("category", "Track");
        addInstrumentCmd.SetAttribute("name", "Add Instrument to Selected Tracks");
        
        var modeArg = xmlDoc.CreateElement("CommandArgument");
        modeArg.SetAttribute("name", "mode");
        modeArg.SetAttribute("value", "1");
        addInstrumentCmd.AppendChild(modeArg);
        
        var cidArg = xmlDoc.CreateElement("CommandArgument");
        cidArg.SetAttribute("name", "cid");
        cidArg.SetAttribute("value", preset.ClassId ?? "");
        addInstrumentCmd.AppendChild(cidArg);
        
        // Build preset path from SubFolder/Title
        var presetPath = "";
        if (!string.IsNullOrEmpty(preset.SubFolder) && !string.IsNullOrEmpty(preset.Title))
        {
            presetPath = $"{preset.SubFolder}/{preset.Title}";
        }
        else if (!string.IsNullOrEmpty(preset.Title))
        {
            presetPath = preset.Title;
        }
        
        var presetArg = xmlDoc.CreateElement("CommandArgument");
        presetArg.SetAttribute("name", "preset");
        presetArg.SetAttribute("value", presetPath);
        addInstrumentCmd.AppendChild(presetArg);
        macro.AppendChild(addInstrumentCmd);

        // Show Instrument Editor
        var showEditorCmd = xmlDoc.CreateElement("CommandElement");
        showEditorCmd.SetAttribute("category", "Console");
        showEditorCmd.SetAttribute("name", "Show Instrument Editor");
        
        var stateArg = xmlDoc.CreateElement("CommandArgument");
        stateArg.SetAttribute("name", "State");
        stateArg.SetAttribute("value", "");
        showEditorCmd.AppendChild(stateArg);
        macro.AppendChild(showEditorCmd);
    }

    private async Task GenerateTrackPresetMacroAsync(XmlDocument xmlDoc, XmlElement macro, PresetDescriptor preset)
    {
        // Load Track Preset
        var loadTrackPresetCmd = xmlDoc.CreateElement("CommandElement");
        loadTrackPresetCmd.SetAttribute("category", "Track");
        loadTrackPresetCmd.SetAttribute("name", "Load Track Preset");
        
        var nameArg = xmlDoc.CreateElement("CommandArgument");
        nameArg.SetAttribute("name", "Name");
        nameArg.SetAttribute("value", preset.Title ?? "");
        loadTrackPresetCmd.AppendChild(nameArg);
        macro.AppendChild(loadTrackPresetCmd);
        
        // Debug: Add a comment to see if this method is being called
        var debugComment = xmlDoc.CreateComment($"TrackPreset macro generated for: {preset.Title}");
        macro.AppendChild(debugComment);
    }
}


