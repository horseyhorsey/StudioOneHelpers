using StudioOneHelpers.Pages;

namespace StudioOneHelpers.Services
{
    public class NavigationService
    {
        private Home? _homeComponent;

        public void RegisterHomeComponent(Home homeComponent)
        {
            _homeComponent = homeComponent;
        }

        public void NavigateToHome()
        {
            _homeComponent?.NavigateToDashboard();
        }

        public void NavigateToCommands()
        {
            _homeComponent?.NavigateToCommands();
        }

        public void NavigateToPlugins()
        {
            _homeComponent?.NavigateToPlugins();
        }

        public void NavigateToPresetCategory(string category)
        {
            _homeComponent?.NavigateToPresetCategory(category);
        }

        public async Task NavigateToStickers()
        {
            if (_homeComponent != null)
            {
                await _homeComponent.NavigateToStickers();
            }
        }

        public void NavigateToGuide()
        {
            _homeComponent?.NavigateToGuide();
        }
    }
}
