using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterMapView : MapView
    {
        private WaaaghMeterGlobalLayer _globalLayer;

        protected override void CreateLayout()
        {
            base.CreateLayout();

            // Create and initialize the global layer (like vanilla MapBar)
            _globalLayer = new WaaaghMeterGlobalLayer();
            _globalLayer.Initialize();

            // Add as global layer so it receives mouse events properly for tooltips
            ScreenManager.AddGlobalLayer(_globalLayer, false);
        }

        protected override void OnFinalize()
        {
            if (_globalLayer != null)
            {
                _globalLayer.OnFinalize();
                ScreenManager.RemoveGlobalLayer(_globalLayer);
                _globalLayer = null;
            }

            base.OnFinalize();
        }
    }
}