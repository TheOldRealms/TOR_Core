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
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.Ink
{
    public class InkStoryCampaignBehavior : CampaignBehaviorBase
    {
        private InkBookMapView _inkView;
        private MapScreen _mapScreen;
        private CampaignTimeControlMode _cachedSpeed = CampaignTimeControlMode.StoppablePlay;
        public override void RegisterEvents() { }

        public void OpenStory(InkStory story) 
        {
            if(ScreenManager.TopScreen is MapScreen)
            {
                _mapScreen = ScreenManager.TopScreen as MapScreen;
                _cachedSpeed = Campaign.Current.TimeControlMode;
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                _inkView = (InkBookMapView)_mapScreen.AddMapView<InkBookMapView>();
                if(_inkView != null) _inkView.OpenStory(story);
            }
        }

        public void CloseStory()
        {
            _inkView.CloseStory();
            _mapScreen.RemoveMapView(_inkView);
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
            Layer = new GauntletLayer(4399) { IsFocusLayer = true };
            _layer = Layer as GauntletLayer;
            _movie = _layer.LoadMovie("InkStory", _vm);
            Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            MapScreen.AddLayer(Layer);
            ScreenManager.TrySetFocus(Layer);
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
            Layer.InputRestrictions.ResetInputRestrictions();
            MapScreen.RemoveLayer(Layer);
            ScreenManager.TryLoseFocus(Layer);
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
