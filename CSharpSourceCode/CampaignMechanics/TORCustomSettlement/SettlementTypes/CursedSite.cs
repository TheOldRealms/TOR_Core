using SandBox;
using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class CursedSite : BaseSettlementType, IDisposable
    {
        private Settlement _settlement;
        private bool _isMarkerShown = false;
        private GameEntity _markerEntity;
        private Decal _markerDecal;
        public ReligionObject Religion { get; private set; }

        public override void OnInit(Settlement settlement, ReligionObject religion = null)
        {
            _settlement = settlement;
            Religion = religion;
            InformationManager.OnShowTooltip += OnShowTooltip;
            InformationManager.OnHideTooltip += OnHideTooltip;
        }

        private void OnShowTooltip(Type type, object[] args)
        {
            if(type == typeof(Settlement) && ScreenManager.TopScreen is MapScreen)
            {
                var settlement = args[0] as Settlement;
                if(settlement == _settlement)
                {
                    ShowAreaMarker(true);
                }
            }
        }

        private void OnHideTooltip()
        {
            if(_isMarkerShown) ShowAreaMarker(false);
        }

        private void ShowAreaMarker(bool shouldShow)
        {
            if (_markerEntity == null) CreateVisuals();
            if(_markerEntity != null)
            {
                MatrixFrame frame = _settlement.Party.Visuals.GetGlobalFrame();
                frame.Scale(new Vec3(32, 32, 1));
                _markerEntity.SetGlobalFrame(frame);
                _markerDecal.SetFactor1Linear(4281663744U);
                _markerEntity.SetVisibilityExcludeParents(shouldShow);
            }
            _isMarkerShown = shouldShow;
        }

        private void CreateVisuals()
        {
            MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
            _markerEntity = GameEntity.CreateEmpty(mapScene.Scene, true);
            _markerEntity.Name = "CursedSiteMarker";
            _markerDecal = Decal.CreateDecal();
            if(_markerDecal != null && _markerEntity != null)
            {
                Material resource = Material.GetFromResource("decal_city_circle_a");
                _markerDecal.SetMaterial(resource);
                mapScene.Scene.AddDecalInstance(_markerDecal, "editor_set", false);
                _markerEntity.AddComponent(_markerDecal);
            }
        }

        public void Dispose()
        {
            InformationManager.OnShowTooltip -= OnShowTooltip;
            InformationManager.OnHideTooltip -= OnHideTooltip;
            _settlement = null;
            _markerDecal = null;
            _markerEntity = null;
        }
    }
}
