using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterMapView : MapView
    {
        private GauntletLayer _gauntletLayer;
        private WaaaghMeterVM _waaaghMeterVM;
        private GauntletMovieIdentifier _movie;
        private float _timeSinceLastRefresh = 0f;
        private const float RefreshInterval = 1f; // Refresh every second

        protected override void CreateLayout()
        {
            base.CreateLayout();

            InformationManager.DisplayMessage(new InformationMessage("[WaaaghMeterMapView] CreateLayout called", new Color(134, 114, 250)));

            // Create the VM
            _waaaghMeterVM = new WaaaghMeterVM();

            // Create the gauntlet layer
            _gauntletLayer = new GauntletLayer("GauntletLayer",210);

            // Load the WaaaghMeter prefab
            _movie = _gauntletLayer.LoadMovie("WaaaghMeter", _waaaghMeterVM);

            // Add the layer to the screen
            MapScreen.AddLayer(_gauntletLayer);

            InformationManager.DisplayMessage(new InformationMessage($"[WaaaghMeterMapView] Layer added, IsVisible: {_waaaghMeterVM.IsVisible}", new Color(134, 114, 250)));
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);

            // Update the ViewModel periodically (not every frame for performance)
            if (_waaaghMeterVM != null)
            {
                _timeSinceLastRefresh += dt;
                if (_timeSinceLastRefresh >= RefreshInterval)
                {
                    _waaaghMeterVM.RefreshValues();
                    _timeSinceLastRefresh = 0f;
                }
            }
        }

        protected override void OnFinalize()
        {
            InformationManager.DisplayMessage(new InformationMessage("[WaaaghMeterMapView] OnFinalize called", new Color(134, 114, 250)));

            if (_gauntletLayer != null)
            {
                MapScreen.RemoveLayer(_gauntletLayer);
                _gauntletLayer.ReleaseMovie(_movie);
                _movie = null;
                _gauntletLayer = null;
            }

            _waaaghMeterVM?.OnFinalize();
            _waaaghMeterVM = null;

            base.OnFinalize();
        }
    }
}