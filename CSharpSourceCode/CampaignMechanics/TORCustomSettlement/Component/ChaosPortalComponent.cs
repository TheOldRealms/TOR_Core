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