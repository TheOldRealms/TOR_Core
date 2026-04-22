using SandBox;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using System;
using System.Collections.Generic;
using System.Xml;
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
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
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

    //Sly : OnPartyEntered can be implemented as an override here to handle blessing on Ai parties directly rather than making use of the SettlementEntered events
    public class ShrineComponent : TORBaseSettlementComponent
    {
        public override IFaction MapFaction => Settlement.Owner.Clan;
    }

    public class OakOfAgesComponent : TORBaseSettlementComponent
    {
        public override IFaction MapFaction => Settlement.Owner.Clan;
    }

    public class WorldRootsComponent : TORBaseSettlementComponent
    {
        public override IFaction MapFaction => Settlement.Owner.Clan;
    }

    public class TrollCaveComponent : BaseRaiderSpawnerComponent
    {
        private string _battleSceneName;

        public override int BattlePartySize => 200;
        public override string BattleSceneName => string.IsNullOrEmpty(_battleSceneName) ? "TOR_troll_hideout_01" : _battleSceneName;

        public override List<string> RewardItemIds =>
        [

            // not really necessary. doesnt do anything. Though I am not in the mood to change the heritage here.
            "tor_orc_weapon_2h_axe_001",
            "tor_orc_weapon_2h_axe_002",
            "tor_orc_weapon_1h_axe_001",
            "tor_orc_weapon_1h_axe_002",
        ];

        public override IFaction MapFaction => Settlement.Owner.Clan;

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            if (node.Attributes["battle_scene"] != null)
            {
                _battleSceneName = node.Attributes["battle_scene"].Value;
            }
        }

        public override void SpawnNewParty(out MobileParty party, Settlement initialTarget)
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("troll_party_template");
            Clan trollClan = Clan.FindFirst(x => x.StringId == "troll_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position.ToVec2(), 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var trollRaidingParty = RaidingPartyComponent.CreateRaidingParty("troll_clan_1_party_" + RaidingPartyCount + 1, Settlement, "Troll Raiders", template, trollClan, MBRandom.RandomInt(7, 15));
            if (find != null)
            {
                SetPartyAiAction.GetActionForRaidingSettlement(trollRaidingParty, initialTarget ?? find, MobileParty.NavigationType.Default, false);
                ((RaidingPartyComponent)trollRaidingParty.PartyComponent).Target = initialTarget ?? find;
            }
            else
            {
                ((RaidingPartyComponent)trollRaidingParty.PartyComponent).Target = null;
            }

            party = trollRaidingParty;
        }
    }

    //Sly : I wonder if we could send out large armies from these components that target settlements to capture. Unsure what would happen if the siege was a success; would it be attributed to the chaos clan?
    //OnPartLefty to announce an invasion beginning, then the player can react to it as they wish.
    public class ChaosPortalComponent : BaseRaiderSpawnerComponent
    {
        public override int BattlePartySize => 550;
        public override string BattleSceneName => "TOR_chaos_portal_001_atmo_w_night";

        public override List<string> RewardItemIds =>
        [
            "tor_empire_weapon_sword_runefang_001",

            "tor_chaos_weapon_metal_ud_lance_001",

            "tor_chaos_arm_gauntlet_slaneesh_warrior_001",
            "tor_chaos_arm_gauntlet_khorne_warrior_001",
            "tor_chaos_arm_gauntlet_khorne_warrior_002",
            "tor_chaos_arm_gauntlet_tzeentch_warrior_001",
            "tor_chaos_arm_gauntlet_hallow",
            "tor_chaos_arm_bracers_marauder_001",

            "tor_chaos_leg_boots_slaneesh_warrior_001",
            "tor_chaos_leg_boots_nurgle_knight_001",
            "tor_chaos_leg_boots_khorne_warrior_001",
            "tor_chaos_leg_boots_khorne_warrior_002",
            "tor_chaos_leg_boots_tzeentch_warrior_001",
            "tor_chaos_leg_boots_hallow",
            "tor_chaos_leg_boots_marauder_001",
            "tor_chaos_leg_boots_chaos_warrior_001",

            "tor_chaos_body_armor_slaneesh_warrior_001",
            "tor_chaos_body_armor_khorne_warrior_001",
            "tor_chaos_body_armor_khorne_warrior_002",
            "tor_chaos_body_armor_tzeentch_warrior_001",
            "tor_chaos_body_armor_nurgle_knight_001",
            "tor_chaos_body_robe_cultist_001",
            "tor_chaos_body_armor_marauder_001",
            "tor_chaos_body_armor_marauder_002",
            "tor_chaos_body_armor_chaos_warrior_001",

            "tor_chaos_shoulder_pauldron_nurgle_001",
            "tor_chaos_shoulder_pauldron_nurgle_002",
            "tor_chaos_shoulder_pauldron_slaneesh_warrior_001",
            "tor_chaos_shoulder_pauldron_chosen_001",
            "tor_chaos_shoulder_pauldron_khorne_warrior_001",
            "tor_chaos_shoulder_pauldron_khorne_warrior_002",
            "tor_chaos_shoulder_pauldron_tzeentch_warrior_001",
            "tor_chaos_shoulder_cape_marauder_001",
            "tor_chaos_shoulder_fur_marauder_001",
            "tor_chaos_shoulder_cape_chaos_warrior_001",

            "tor_learn_dw_master_rune_swiftness",
            "tor_learn_dw_master_rune_preservation",
            "tor_learn_dw_master_rune_alaric",
            "tor_learn_dw_master_rune_skalf",
            "tor_learn_dw_master_rune_skaldour",
            "tor_learn_dw_master_rune_adamant",
        ];

        public override IFaction MapFaction => Settlement.Owner.Clan;

        public override void SpawnNewParty(out MobileParty party, Settlement initialTarget)
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_lordparty_template");
            Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position.ToVec2(), 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var targetPartySize = MBRandom.RandomInt(75, 99);
            if (MBRandom.RandomInt(4) == 0)
            {
                targetPartySize *= 2;//20% chance for doubled party size
            }

            var chaosRaidingParty = RaidingPartyComponent.CreateRaidingParty("chaos_clan_1_party_" + RaidingPartyCount + 1, Settlement, TORTextHelper.GetText("tor_chaos_raiders", "Chaos Raiders"), template, chaosClan, targetPartySize);
            if (find != null)
            {
                SetPartyAiAction.GetActionForRaidingSettlement(chaosRaidingParty, initialTarget ?? find, MobileParty.NavigationType.Default, false);
                ((RaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = initialTarget ?? find;
            }
            else //a target will be attempted after spawn via RaidingPartyComponent.HourlyTickAI
            {
                ((RaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = null;
            }

            party = chaosRaidingParty;
        }
    }

    public class HerdStoneComponent : BaseRaiderSpawnerComponent
    {
        public override int BattlePartySize => 400;
        public override string BattleSceneName => "TOR_beastmen_herdstone_001";

        public override List<string> RewardItemIds =>
            new()
            {

                "tor_vc_blood_keep_grandmaster_helm",

                "tor_empire_weapon_mace_holy_hammer",

                "tor_vc_shoulder_pauldron_vlad",
                "tor_vc_shoulder_pauldron_red_duke",
                "tor_vc_shoulder_pauldron_blood_dragon_knight_001",
                "tor_vc_arm_gauntlet_vlad",
                "tor_vc_arm_gauntlet_red_duke",
                "tor_vc_arm_gauntlet_blood_dragon_knight_001",
                "tor_empire_weapon_scythe_morr_001",
                "tor_vc_body_armor_red_duke",
                "tor_vc_body_armor_vlad",
                "tor_vc_body_armor_blood_dragon_knight_001",
                "tor_vc_leg_boots_vlad",
                "tor_vc_leg_boots_blood_dragon_knight_001",
                "tor_vc_leg_boots_red_duke",
                "tor_he_weapon_2h_sword_001",
                "tor_vc_weapon_thaxe_black_axe_of_krell",
                "tor_empire_weapon_sword_runefang_001",
                "tor_vc_weapon_sword_khopesh_001",

                "tor_learn_dw_master_rune_flight",
                "tor_learn_dw_master_rune_gromril"
            };

        public override IFaction MapFaction => Settlement.Owner.Clan;

        public override void SpawnNewParty(out MobileParty party, Settlement initialTarget)
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("ungor_party");
            Clan beastmenClan = Clan.FindFirst(x => x.StringId == "beastmen_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position.ToVec2(), 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var raidingParty = RaidingPartyComponent.CreateRaidingParty("beastmen_clan_1_party_" + RaidingPartyCount + 1, Settlement, TORTextHelper.GetText("tor_beastmen_raiders", "Beastmen Raiders"), template, beastmenClan, MBRandom.RandomInt(75, 99));
            if (find != null)
            {
                SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, initialTarget ?? find, MobileParty.NavigationType.Default, false);
                ((RaidingPartyComponent)raidingParty.PartyComponent).Target = initialTarget ?? find;
            }
            else //a target will be attempted after spawn via RaidingPartyComponent.HourlyTickAI
            {
                ((RaidingPartyComponent)raidingParty.PartyComponent).Target = null;
            }

            party = raidingParty;
        }
    }

    public class SlaverCampComponent : BaseRaiderSpawnerComponent
    {
        public override int BattlePartySize => 400;
        public override string BattleSceneName => "TOR_slaver_bay_001";

        public override List<string> RewardItemIds =>
        [
            "tor_he_head_helm_phoenix_001",
            "tor_he_head_helm_whitelion_001",

            "tor_he_shoulder_cape_phoenix_001",
            "tor_he_shoulder_cape_whitelion_001",
            "tor_he_body_armour_phoenix_001",
            "tor_he_body_armour_whitelion_001",

            "tor_he_arm_bracers_phoenix_001",
            "tor_he_leg_boots_phoenix_001",

            "tor_he_leg_boots_whitelion_001",
            "tor_he_arm_bracers_whitelion_001",

            "tor_he_weapon_2h_axe_whitelion_001",

            "tor_he_weapon_halberd_phoenix_001",

            "tor_vc_weapon_sword_khopesh_001",

            "tor_learn_dw_master_rune_breaking",
            "tor_learn_dw_master_rune_steel",
        ];

        public override IFaction MapFaction => Settlement.Owner.Clan;

        public override void SpawnNewParty(out MobileParty party, Settlement initialTarget)
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("druchii_slaver_party");
            Clan clan = Clan.FindFirst(x => x.StringId == "druchii_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position.ToVec2(), 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var raidingParty = RaidingPartyComponent.CreateRaidingParty("druchii_clan_1_party_" + RaidingPartyCount + 1, Settlement, TORTextHelper.GetText("tor_dark_elf_slavers", "Druchii Slavers"), template, clan, MBRandom.RandomInt(75, 99));
            if (find != null)
            {
                SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, initialTarget ?? find, MobileParty.NavigationType.Default, false);
                ((RaidingPartyComponent)raidingParty.PartyComponent).Target = initialTarget ?? find;
            }
            else //a target will be attempted after spawn via RaidingPartyComponent.HourlyTickAI
            {
                ((RaidingPartyComponent)raidingParty.PartyComponent).Target = null;
            }

            party = raidingParty;
        }
    }
}