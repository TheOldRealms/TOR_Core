using System.Collections.Generic;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
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