using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Map overlay for the §19 ambush preparation window. Mirrors the
    /// vanilla <c>GauntletMapSiegeOverlayView</c> pattern:
    ///  - CreateLayout: grab the nameplate Gauntlet layer, build the VM,
    ///    load the prefab movie.
    ///  - OnMapScreenUpdate: tick the VM so the slider fills.
    ///  - OnFinalize: release movie + layer references.
    ///
    /// Prefab is <c>GUI/Prefabs/Map/AmbushPrepOverlay.xml</c>; it reuses
    /// the vanilla preparation-slider sprites so the look matches the
    /// siege-prep bar the player already knows.
    /// </summary>
    public class AmbushPrepOverlayMapView : MapView
    {
        private GauntletLayer _layer;
        private AmbushPrepOverlayVM _dataSource;
        private GauntletMovieIdentifier _movie;

        protected override void CreateLayout()
        {
            base.CreateLayout();
            // Reuse the vanilla nameplate layer — same place vanilla
            // sticks the siege overlay. Keeps our overlay in the right
            // z-order relative to party nameplates etc.
            var basicView = MapScreen.GetMapView<GauntletMapBasicView>();
            Layer = (ScreenLayer)basicView.GauntletNameplateLayer;
            _layer = Layer as GauntletLayer;
            _dataSource = new AmbushPrepOverlayVM();
            _movie = _layer.LoadMovie("AmbushPrepOverlay", _dataSource);
        }

        protected override void OnMapScreenUpdate(float dt)
        {
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
