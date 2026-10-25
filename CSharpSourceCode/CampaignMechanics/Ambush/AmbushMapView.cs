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
            Layer = new GauntletLayer("AmbushStealthButton", 100);
            _layer = Layer as GauntletLayer;
            _movie = _layer.LoadMovie("AmbushStealthButton", _vm);
            MapScreen.AddLayer(Layer);
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
            if (_layer != null)
            {
                MapScreen.RemoveLayer(_layer);
                _layer.ReleaseMovie(_movie);
            }
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
