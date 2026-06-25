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