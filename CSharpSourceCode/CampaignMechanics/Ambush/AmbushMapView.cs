using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;

namespace TOR_Core.CampaignMechanics.Ambush
{
    public class AmbushMapView : MapView
    {
        private AmbushButtonVM _vm;
        private GauntletLayer _layer;
        private GauntletMovieIdentifier _movie;

        protected override void CreateLayout()
        {
            base.CreateLayout();
            _vm = new AmbushButtonVM();
            // Reuse the main map's GauntletLayer (same pattern as WaaaghMeterMapView).
            // Creating a fresh GauntletLayer leaves input routing unconfigured, so
            // ButtonWidget clicks never fire. The main map layer already has input
            // wired for the campaign HUD.
            var mapBasicView = MapScreen.GetMapView<GauntletMapBasicView>();
            Layer = mapBasicView.GauntletLayer;
            _layer = Layer as GauntletLayer;
            _movie = _layer.LoadMovie("AmbushStealthButton", _vm);
        }

        protected override void OnMapScreenUpdate(float dt)
        {
            base.OnMapScreenUpdate(dt);
            // OnMapScreenUpdate fires unconditionally from MapScreen.OnFrameTick, so this covers
            // both free-map and in-menu states for input polling AND VM refresh.
            _vm?.Refresh();
            HandleInput();
        }

        protected override void OnFinalize()
        {
            // We don't own the layer — it belongs to GauntletMapBasicView — so only
            // release the movie we loaded onto it; don't remove the layer.
            if (_layer != null)
                _layer.ReleaseMovie(_movie);
            _movie = null;
            _layer = null;
            Layer = null;
            _vm = null;
            base.OnFinalize();
        }

        private static void HandleInput()
        {
            if (!Input.IsKeyReleased(AmbushCampaignBehavior.StealthToggleKey)) return;
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()?.OnStealthHotkeyPressed();
        }
    }
}
