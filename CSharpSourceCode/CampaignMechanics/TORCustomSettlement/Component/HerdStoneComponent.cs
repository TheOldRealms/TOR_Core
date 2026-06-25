using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public class HerdStoneComponent : BaseRaiderSpawnerComponent
{
    public override int BattlePartySize => 400;
    public override string BattleSceneName => "TOR_beastmen_herdstone_001";

    public override List<string> RewardItemIds =>
        [

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
        ];

    public override IFaction MapFaction => Settlement.Owner.Clan;

    public override void SpawnNewParty(out MobileParty party, Settlement initialTarget)
    {
        PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("ungor_party");
        Clan beastmenClan = Clan.FindFirst(x => x.StringId == "beastmen_clan_1");
        var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position.ToVec2(), 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
        var raidingParty = RaidingPartyComponent.CreateRaidingParty("beastmen_clan_1_party_" + RaidingPartyCount + 1, Settlement, TORTextHelper.GetText("tor_beastmen_raiders", "Beastmen Raiders"), template, beastmenClan, MBRandom.RandomInt(75, 99));
        if (find != null)
        {
            SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, initialTarget ?? find, MobileParty.NavigationType.Default, false, false);
            ((RaidingPartyComponent)raidingParty.PartyComponent).Target = initialTarget ?? find;
        }
        else //a target will be attempted after spawn via RaidingPartyComponent.HourlyTickAI
        {
            ((RaidingPartyComponent)raidingParty.PartyComponent).Target = null;
        }

        party = raidingParty;
    }
}