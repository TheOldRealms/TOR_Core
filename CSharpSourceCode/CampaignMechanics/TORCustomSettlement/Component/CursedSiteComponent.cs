using SandBox;
using SandBox.View.Map;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

//Sly : OnPartyEntered can be implemented as an override here to handle wraith recruitment for Ai parties directly rather than making use of the SettlementEntered events
public class CursedSiteComponent : TORBaseSettlementComponent, IDisposable
{
    private int _wardHours = 0;
    private bool _isMarkerShown = false;
    private GameEntity _markerEntity;
    private Decal _markerDecal;
    public int WardHours
    {
        get { return _wardHours; }
        set
        {
            _wardHours = value;
            if (_wardHours == 0) IsActive = true;
        }
    }

    public override IFaction MapFaction => Settlement.Owner.Clan;

    public void HourlyTick() => WardHours = Math.Max(0, WardHours - 1);

    public override void OnInit()
    {
        base.OnInit();
        InformationManager.OnShowTooltip += OnShowTooltip;
        InformationManager.OnHideTooltip += OnHideTooltip;
    }

    private void OnShowTooltip(Type type, object[] args)
    {
        if (type == typeof(Settlement) && ScreenManager.TopScreen is MapScreen)
        {
            var settlement = args[0] as Settlement;
            if (settlement == Settlement)
            {
                ShowAreaMarker(true);
            }
        }
    }

    private void OnHideTooltip()
    {
        if (_isMarkerShown) ShowAreaMarker(false);
    }

    private void ShowAreaMarker(bool shouldShow)
    {
        if (_markerEntity == null) CreateVisuals();
        if (_markerEntity != null)
        {
            var vec3 = Settlement.GetPositionAsVec3();
            vec3.z -= 10;//negative offset is towards the camera, positive is into the map
            MatrixFrame frame = new MatrixFrame(Mat3.Identity, vec3);
            frame.Scale(new Vec3(32, 32, 15));//Sly : the decal is slightly above the settlement's height and thicker (vertically) so it shows up better on uneven terrain
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
        if (_markerDecal != null && _markerEntity != null)
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
        _markerDecal = null;
        _markerEntity = null;
    }
}
