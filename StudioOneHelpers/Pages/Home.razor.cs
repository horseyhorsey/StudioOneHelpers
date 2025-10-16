using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using StudioOneHelpers.Services;

namespace StudioOneHelpers.Pages
{
    public partial class Home
    {

        [SupplyParameterFromQuery]
        [Parameter] public string? View { get; set; }

        // View state management
        public enum ViewType { Dashboard, Commands, Plugins, Presets, Guide, Stickers }
        public ViewType CurrentView { get; set; } = ViewType.Dashboard;
        public string? SelectedPresetCategory { get; set; }

        // File input references
        private InputFile? commandsFileInput;
        private InputFile? pluginsFileInput;
        private InputFile? presetsFileInput;

        // Data status indicators
        private string? commandsDataStatus;
        private string? pluginsDataStatus;
        

        // Loading states
        private bool isCommandsLoading = false;
        private bool isPluginsLoading = false;
        private bool isPresetsLoading = false;

        private string? presetsDataStatus;
        private List<string> availableCategories = new();

        protected override async Task OnInitializedAsync()
        {
            NavigationService.RegisterHomeComponent(this);
            await CheckDataStatus();
        }

        protected override Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(View))
            {
                switch (View.ToLower())
                {
                    case "dashboard":
                        CurrentView = ViewType.Dashboard;
                        break;
                    case "commands":
                        CurrentView = ViewType.Commands;
                        break;
                    case "plugins":
                        CurrentView = ViewType.Plugins;
                        break;
                    case "guide":
                        CurrentView = ViewType.Guide;
                        break;
                        // Add other views as needed
                }

                StateHasChanged();
            }
            return base.OnParametersSetAsync();
        }

        public void NavigateToPresetCategory(string category)
        {
            if (presetsDataStatus == null)
            {
                Snackbar.Add("No presets data available. Please import your DataStore.db file first.", Severity.Warning);
                return;
            }

            if (category == "All")
            {
                // Show all available categories - navigate to the first available category
                if (availableCategories.Any())
                {
                    SelectedPresetCategory = availableCategories.First();
                    CurrentView = ViewType.Presets;
                }
            }
            else
            {
                SelectedPresetCategory = category;
                CurrentView = ViewType.Presets;
            }
            StateHasChanged();
        }

        private async Task CheckDataStatus()
        {
            // Check commands data
            commandsDataStatus = await CommandsService.HasCommandsDataAsync() ?
                await CommandsService.GetCommandsImportTimeAsync() : null;

            // Check plugins data
            var pluginsData = await LocalStorage.GetItemAsync<string>("PluginsData");
            pluginsDataStatus = !string.IsNullOrEmpty(pluginsData) ?
                await GetDataImportTime("PluginsData") : null;

            // Check presets data - use the same logic as DatabaseProcessingService
            availableCategories = await DatabaseService.LoadAvailableCategoriesFromStorageAsync();
            presetsDataStatus = availableCategories.Any() ?
                await GetDataImportTime("PresetData") : null;
        }

        private async Task<string> GetDataImportTime(string key)
        {
            var importTime = await LocalStorage.GetItemAsync<string>($"{key}_ImportTime");
            if (!string.IsNullOrEmpty(importTime))
            {
                if (DateTime.TryParse(importTime, out var dateTime))
                {
                    return dateTime.ToString("MMM dd, yyyy HH:mm");
                }
            }
            return "Unknown";
        }

        public void NavigateToCommands()
        {
            if (commandsDataStatus == null)
            {
                Snackbar.Add("No commands data available. Please import your ShortcutsExport.html file first.", Severity.Warning);
                return;
            }
            CurrentView = ViewType.Commands;
            StateHasChanged();
        }

        public void NavigateToPlugins()
        {
            if (pluginsDataStatus == null)
            {
                Snackbar.Add("No plugins data available. Please import your Plugins-en.settings file first.", Severity.Warning);
                return;
            }
            CurrentView = ViewType.Plugins;
            StateHasChanged();
        }

        public void NavigateToStickers()
        {
            CurrentView = ViewType.Stickers;
            StateHasChanged();
        }

        public void NavigateToDashboard()
        {
            CurrentView = ViewType.Dashboard;
            StateHasChanged();
        }

        public void NavigateToGuide()
        {
            CurrentView = ViewType.Guide;
            StateHasChanged();
        }

        // Commands import handlers
        private async Task HandleCommandsImport()
        {
            await JSRuntime.InvokeVoidAsync("clickFileInput", "commandsFileInput");
        }

        private async Task OnCommandsFileSelected(InputFileChangeEventArgs e)
        {
            if (e.File == null) return;

            isCommandsLoading = true;
            StateHasChanged();

            try
            {
                using var stream = e.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var reader = new StreamReader(stream);
                var htmlContent = await reader.ReadToEndAsync();

                // Process the HTML content to extract commands
                var commands = await CommandsService.ProcessCommandsAsync(htmlContent);

                // Save to storage
                await CommandsService.SaveCommandsToStorageAsync(commands);

                Snackbar.Add($"Commands data imported successfully! Found {commands.Count} commands.", Severity.Success);
                await CheckDataStatus();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error importing commands: {ex.Message}", Severity.Error);
            }
            finally
            {
                isCommandsLoading = false;
                StateHasChanged();
            }
        }

        // Plugins import handlers
        private async Task HandlePluginsImport()
        {
            await JSRuntime.InvokeVoidAsync("clickFileInput", "pluginsFileInput");
        }

        private async Task OnPluginsFileSelected(InputFileChangeEventArgs e)
        {
            if (e.File == null) return;

            isPluginsLoading = true;
            StateHasChanged();

            try
            {
                using var stream = e.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var reader = new StreamReader(stream);
                var xmlContent = await reader.ReadToEndAsync();

                // Process plugins using the service
                var plugins = await PluginService.ProcessPluginsAsync(xmlContent);
                await LocalStorage.SetItemAsync("PluginsData_ImportTime", DateTime.Now.ToString());

                Snackbar.Add($"Plugins data imported successfully! Found {plugins.Count} plugins.", Severity.Success);
                await CheckDataStatus();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error importing plugins: {ex.Message}", Severity.Error);
            }
            finally
            {
                isPluginsLoading = false;
                StateHasChanged();
            }
        }

        // Presets import handlers
        private async Task HandlePresetsImport()
        {
            await JSRuntime.InvokeVoidAsync("clickFileInput", "presetsFileInput");
        }

        private async Task OnPresetsFileSelected(InputFileChangeEventArgs e)
        {
            if (e.File == null) return;

            isPresetsLoading = true;
            StateHasChanged();

            try
            {
                using var stream = e.File.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB limit for database files
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Process database using the service
                var presets = await DatabaseService.ProcessDatabaseAsync(fileBytes);
                await LocalStorage.SetItemAsync("PresetData_ImportTime", DateTime.Now.ToString());

                Snackbar.Add($"Presets data imported successfully! Found {presets.Count} presets.", Severity.Success);
                await CheckDataStatus();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error importing presets: {ex.Message}", Severity.Error);
            }
            finally
            {
                isPresetsLoading = false;
                StateHasChanged();
            }
        }


        private async Task ClearAllData()
        {
            try
            {
                // Clear all localStorage data
                await JSRuntime.InvokeVoidAsync("clearAllLocalStorageData");

                // Reset status indicators
                commandsDataStatus = null;
                pluginsDataStatus = null;                

                Snackbar.Add("All imported data has been cleared successfully!", Severity.Success);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error clearing data: {ex.Message}", Severity.Error);
            }
        }
    }
}