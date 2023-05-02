using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class HerdStone : ISettlementType
    {
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;
        public string BattleSceneName => "TOR_chaos_portal_001_forceatmo";
        public bool IsActive { get; set; } = true;
        public bool IsRaidingPartySpawner => true;
        public bool IsBattleUnderway { get; set; }
        public string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public void OnInit(Settlement settlement, ReligionObject religion)
        {
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
        }

        public void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("ungor_party");
            Clan beastmenClan = Clan.FindFirst(x => x.StringId == "beastmen_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(_settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var raidingParty = RaidingPartyComponent.CreateRaidingParty("beastmen_clan_1_party_" + _component.RaidingPartyCount + 1, _settlement, "Ungor Raiders", template, beastmenClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, find);
            ((RaidingPartyComponent)raidingParty.PartyComponent).Target = find;
        }
    }
}
