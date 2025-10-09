# 🎵 Studio One Helpers

**Professional tools for Studio One users** - A comprehensive web application for managing commands, macros, plugins, and presets from your Studio One digital audio workstation.

## What This App Does

This kushti little helper gives you powerful tools to organize and work with your Studio One data:

### 🎹 Commands & Macros
- **Import** your Studio One keyboard shortcuts from `ShortcutsExport.html`
- **Filter and search** through your commands and macros
- **Generate PDF reports** of your shortcuts for easy reference
- **Organize by sections** (Transport, Edit, View, etc.)

### 🔌 Studio One Plugins
- **Import** plugin data from `Plugins-en.settings` file
- **Browse and filter** your installed VST3 and AudioEffect plugins
- **Search by vendor, name, or category**
- **Generate macros** for plugin management

### 🎛️ Studio One Presets
- **Import** presets from your `DataStore.db` database
- **Explore by category**: `Artist, AudioEffect, AudioSynth, FXChain, MusicEffect, PatternBank, TrackPreset`
- **Search and filter** through thousands of presets
- **Organize by vendor, creator, or subfolder**

## Key Features

- **📱 Progressive Web App** - Works offline, installable on any device
- **💾 Local Storage** - All your data stays private on your device
- **🔍 Advanced Filtering** - Search and filter across all data types
- **📊 PDF Export** - Generate professional reports
- **🎨 Modern UI** - Clean, responsive interface built with MudBlazor
- **⚡ Fast Performance** - Optimized for large datasets

## How to Use

1. **Get your Studio One data**:
   - **Commands**: File → Export → Shortcuts (HTML) in Studio One
   - **Plugins**: Load `Plugins-en.settings` from your Studio One settings folder
   - **Presets**: Load `DataStore.db` from your Studio One data folder

2. **File Locations** (Studio One 6+):
   - **Windows**:
     - **Plugins**: `C:\Users\{USER}\AppData\Roaming\PreSonus\Studio One 7\x64\Plugins-en.settings`
     - **Presets**: `C:\Users\{USER}\AppData\Roaming\PreSonus\Studio One 7\DataStore.db`
   - **macOS**:
     - **Plugins**: `~/Library/Application Support/PreSonus/Studio One 7/x64/Plugins-en.settings`
     - **Presets**: `~/Library/Application Support/PreSonus/Studio One 7/DataStore.db`

   > **Note**: File locations may vary slightly depending on your Studio One version. If you can't find these files in the Studio One 7 folder, check the most recent Studio One version folder in your PreSonus directory.

3. **Import into the app** using the dashboard cards

4. **Explore, filter, and export** your data as needed

## Built With

- **Blazor WebAssembly** - Modern web framework
- **MudBlazor** - Material Design components
- **SQL.js** - Client-side SQLite processing
- **jsPDF** - PDF generation
- **HTML2Canvas** - Screenshot capabilities

*May your shortcuts be swift and your presets be kushti, chav!* 🎵

---

*This README was crafted with the assistance of Dinny Eye, your friendly Romany coding companion. The old ways are the best ways, and good documentation is like a strong wagon - built to last!* 🎵


