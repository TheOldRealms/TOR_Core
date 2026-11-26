using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CampaignMechanics.Ambush.UI
{
    /// <summary>
    /// Map overlay for the ambush preparation bar. Mirrors the vanilla siege-overlay pattern.
    /// </summary>
    public class AmbushPrepOverlayMapView : MapView
    {
        private GauntletLayer _layer;
        private AmbushPrepOverlayVM _dataSource;
        private GauntletMovieIdentifier _movie;

        protected override void CreateLayout()
        {
            base.CreateLayout();
            // Reuse the nameplate layer for correct z-order with party nameplates.
            var basicView = MapScreen.GetMapView<GauntletMapBasicView>();
            Layer = (ScreenLayer)basicView.GauntletNameplateLayer;
            _layer = Layer as GauntletLayer;
            _dataSource = new AmbushPrepOverlayVM();
            _movie = _layer.LoadMovie("AmbushPrepOverlay", _dataSource);
        }

        protected override void OnMapScreenUpdate(float dt)
        {
            using var _profilerScope = TOR_Core.Utilities.Profiling.TORCampaignProfiler.Scope("Map.AmbushPrepOverlay");
            base.OnMapScreenUpdate(dt);
            _dataSource?.Update();
        }

        protected override void OnFinalize()
        {
            if (_layer != null && _movie != null)
                _layer.ReleaseMovie(_movie);
            _movie = null;
            _dataSource = null;
            Layer = null;
            _layer = null;
            base.OnFinalize();
        }
    }
}
