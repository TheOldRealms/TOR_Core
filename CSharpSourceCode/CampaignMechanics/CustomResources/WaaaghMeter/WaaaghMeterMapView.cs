using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterMapView : MapView
    {
        private WaaaghMeterVM _vm;
        private GauntletLayer _layer;
        private GauntletMovieIdentifier _movie;

        protected override void CreateLayout()
        {
            base.CreateLayout();
            _vm = new WaaaghMeterVM();
            GauntletMapBasicView mapView = MapScreen.GetMapView<GauntletMapBasicView>();
            Layer = mapView.GauntletLayer;
            _layer = Layer as GauntletLayer;
            _movie = _layer.LoadMovie("WaaaghMeter", _vm);
        }

        protected override void OnMapScreenUpdate(float dt)
        {
            base.OnMapScreenUpdate(dt);
            _vm.RefreshValues();
        }

        protected override void OnFinalize()
        {
            _vm.OnFinalize();
            _vm = null;
            _layer.ReleaseMovie(_movie);
            _movie = null;
            _layer = null;
            Layer = null;
            base.OnFinalize();
        }
    }
}