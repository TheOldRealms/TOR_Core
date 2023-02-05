using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.Ink
{
    public class InkStoryCampaignBehavior : CampaignBehaviorBase
    {
        private InkBookMapView _inkView;
        private CampaignTimeControlMode _cachedSpeed = CampaignTimeControlMode.StoppablePlay;
        public override void RegisterEvents()
        {
            ScreenManager.OnPushScreen += OnPushScreen;
        }

        private void OnPushScreen(ScreenBase pushedScreen)
        {
            if (pushedScreen.GetType() != typeof(MapScreen)) return;
            else
            {
                var mapscreen = pushedScreen as MapScreen;
                if(mapscreen.GetMapView<InkBookMapView>() == null)
                {
                    mapscreen.AddMapView<InkBookMapView>();
                }
                _inkView = mapscreen.GetMapView<InkBookMapView>();
            }
        }

        public void OpenStory(InkStory story) 
        {
            _cachedSpeed = Campaign.Current.TimeControlMode;
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            _inkView.OpenStory(story);
        }

        public void CloseStory()
        {
            _inkView.CloseStory();
            Campaign.Current.TimeControlMode = _cachedSpeed;
        }

        public override void SyncData(IDataStore dataStore) { }
    }

    public class InkBookMapView : MapView
    {
        private InkStoryVM _vm;
        private GauntletLayer _layer;
        private IGauntletMovie _movie;

        protected override void CreateLayout()
        {
            base.CreateLayout();
            _vm = new InkStoryVM();
            GauntletMapBasicView mapView = MapScreen.GetMapView<GauntletMapBasicView>();
            Layer = mapView.GauntletLayer;
            _layer = Layer as GauntletLayer;
            _movie = _layer.LoadMovie("InkStory", _vm);
        }

        protected override void OnMapScreenUpdate(float dt)
        {
            base.OnMapScreenUpdate(dt);
        }

        public void OpenStory(InkStory story)
        {
            _vm.SetStory(story);
            _vm.IsVisible = true;
        }

        public void CloseStory()
        {
            _vm.IsVisible = false;
            _vm.ClearStory();
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
