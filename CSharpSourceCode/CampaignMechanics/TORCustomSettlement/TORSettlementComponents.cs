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
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
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
                MatrixFrame frame = PartyVisualManager.Current.GetVisualOfParty(Settlement.Party).CircleLocalFrame;
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

    public class ShrineComponent : TORBaseSettlementComponent { }

    public class ChaosPortalComponent : BaseRaiderSpawnerComponent
    {
        public override string BattleSceneName => "TOR_chaos_portal_001_forceatmo";

        public override string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public override void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_lordparty_template");
            Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var chaosRaidingParty = RaidingPartyComponent.CreateRaidingParty("chaos_clan_1_party_" + RaidingPartyCount + 1, Settlement, "{=tor_chaos_raiders_str}Chaos Raiders", template, chaosClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(chaosRaidingParty, find);
            ((RaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = find;
        }
    }

    public class HerdStoneComponent : BaseRaiderSpawnerComponent
    {
        public override string BattleSceneName => "TOR_beastmen_herdstone_001";

        public override string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public override void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("ungor_party");
            Clan beastmenClan = Clan.FindFirst(x => x.StringId == "beastmen_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var raidingParty = RaidingPartyComponent.CreateRaidingParty("beastmen_clan_1_party_" + RaidingPartyCount + 1, Settlement, new TextObject ("{=tor_ungor_raiders_str}Ungor Raiders").ToString(), template, beastmenClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, find);
            ((RaidingPartyComponent)raidingParty.PartyComponent).Target = find;
        }
    }

    public class SlaverCampComponent : BaseRaiderSpawnerComponent
    {
        public override string BattleSceneName => "TOR_beastmen_herdstone_001";

        public override string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public override void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("druchii_slaver_party");
            Clan beastmenClan = Clan.FindFirst(x => x.StringId == "beastmen_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var raidingParty = RaidingPartyComponent.CreateRaidingParty("beastmen_clan_1_party_" + RaidingPartyCount + 1, Settlement, new TextObject("{=tor_dark_elf_slavers}Druchii Slavers").ToString(), template, beastmenClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, find);
            ((RaidingPartyComponent)raidingParty.PartyComponent).Target = find;
        }
    }
}
