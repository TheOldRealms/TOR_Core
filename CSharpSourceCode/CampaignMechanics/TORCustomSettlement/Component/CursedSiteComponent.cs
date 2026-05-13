using SandBox;
using SandBox.View.Map;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public class CursedSiteComponent : TORBaseSettlementComponent, IDisposable
{

    public static int MIN_TROOP_COUNT = 1;
    public static int MAX_TROOP_COUNT = 4;
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
    
    /*
    public override void OnPartyEntered(MobileParty party)
    {

        if (party == null || party.LeaderHero == null || party == MobileParty.MainParty) return;
        var leaderHero = party.LeaderHero;

        if (leaderHero.IsNecromancer() || leaderHero.IsVampire())
        {
            var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
            if (freeSlots > 0)
            {
                var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
                int raisePower = Math.Max(1, (int)leaderHero.GetExtendedInfo().SpellCastingLevel);
                var count = MBRandom.RandomInt(MIN_TROOP_COUNT, MAX_TROOP_COUNT);
                count *= raisePower;
                if (freeSlots < count) count = freeSlots;
                party.MemberRoster.AddToCounts(troop, count);
                CampaignEventDispatcher.Instance.OnTroopRecruited(party.LeaderHero, Settlement, null, troop, count);

                
                if (_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId))
                {
                    _lastGhostRecruitmentTime[party.LeaderHero.StringId] = (int)CampaignTime.Now.ToDays;
                }
                else
                {
                    _lastGhostRecruitmentTime.Add(party.LeaderHero.StringId, (int)CampaignTime.Now.ToDays);
                }
                
            }
        }

        LeaveSettlementAction.ApplyForParty(party);

        if (party.Army == null || party.Army.LeaderParty == party)//unsure what happens if all of the attached parties in an army are set to start thinking; player-facing issue only as AI armies won't try to visit shrines
        {
            party.SetMoveModeHold();
            party.Ai.SetDoNotMakeNewDecisions(false);
            party.Ai.RethinkAtNextHourlyTick = true;
        }
    }*/

    public void Dispose()
    {
        InformationManager.OnShowTooltip -= OnShowTooltip;
        InformationManager.OnHideTooltip -= OnHideTooltip;
        _markerDecal = null;
        _markerEntity = null;
    }
}
