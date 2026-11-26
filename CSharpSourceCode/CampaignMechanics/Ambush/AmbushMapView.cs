using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TOR_Core.CampaignMechanics.Ambush.UI;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Map-screen view that attaches the ambush stealth widget and polls the stealth hotkey.
    /// </summary>
    public class AmbushMapView : MapView
    {
        private AmbushButtonVM _vm;
        private GauntletLayer _layer;
        private GauntletMovieIdentifier _movie;

        protected override void CreateLayout()
        {
            base.CreateLayout();
            _vm = new AmbushButtonVM();
            // Reuse the main map's GauntletLayer; a fresh one lacks input wiring and clicks never fire.
            var mapBasicView = MapScreen.GetMapView<GauntletMapBasicView>();
            Layer = mapBasicView.GauntletLayer;
            _layer = Layer as GauntletLayer;
            _movie = _layer.LoadMovie("AmbushStealthWidget", _vm);
        }

        protected override void OnMapScreenUpdate(float dt)
        {
            using var _profilerScope = TOR_Core.Utilities.Profiling.TORCampaignProfiler.Scope("Map.AmbushButton");
            base.OnMapScreenUpdate(dt);
            _vm?.Refresh();
            HandleInput();
        }

        protected override void OnFinalize()
        {
            // Release the movie but leave the layer; it belongs to GauntletMapBasicView.
            if (_layer != null)
                _layer.ReleaseMovie(_movie);
            _movie = null;
            _layer = null;
            Layer = null;
            _vm = null;
            base.OnFinalize();
        }

        /// <summary>
        /// Fire the stealth-toggle hotkey when the configured key is released.
        /// </summary>
        private static void HandleInput()
        {
            if (!Input.IsKeyReleased(AmbushCampaignBehavior.StealthToggleKey)) return;
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()?.OnStealthHotkeyPressed();
        }
    }
}
