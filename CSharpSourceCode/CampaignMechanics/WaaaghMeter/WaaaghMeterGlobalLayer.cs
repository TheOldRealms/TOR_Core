using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterGlobalLayer : GlobalLayer
    {
        private WaaaghMeterVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private GauntletMovieIdentifier _movie;
        private float _timeSinceLastRefresh = 0f;
        private const float RefreshInterval = 1f;

        public void Initialize()
        {
            _dataSource = new WaaaghMeterVM();

            // Create gauntlet layer with priority similar to MapBar (202)
            _gauntletLayer = new GauntletLayer("WaaaghMeter", 203);
            Layer = _gauntletLayer;

            // Load the movie
            _movie = _gauntletLayer.LoadMovie("WaaaghMeter", _dataSource);
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (_dataSource != null)
            {
                // Enable input restrictions set to false - allows mouse events but doesn't block input
                // This is the key to making tooltips work on passive HUD elements
                Layer.InputRestrictions.SetInputRestrictions(false);

                // Periodic refresh
                _timeSinceLastRefresh += dt;
                if (_timeSinceLastRefresh >= RefreshInterval)
                {
                    _dataSource.RefreshValues();
                    _timeSinceLastRefresh = 0f;
                }
            }
        }

        public void OnFinalize()
        {
            if (_gauntletLayer != null && _movie != null)
            {
                _gauntletLayer.ReleaseMovie(_movie);
                _movie = null;
            }

            _dataSource?.OnFinalize();
            _dataSource = null;
            _gauntletLayer = null;
        }
    }
}