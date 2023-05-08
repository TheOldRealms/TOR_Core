using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class ChaosPortal : BaseSettlementType
    {
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;
        public string BattleSceneName => "TOR_chaos_portal_001_forceatmo";
        public bool IsBattleUnderway { get; set; }
        public string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public override void OnInit(Settlement settlement, ReligionObject religion)
        {
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
            IsRaidingPartySpawner = true;
        }

        public override void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_patrol");
            Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(_settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var chaosRaidingParty = RaidingPartyComponent.CreateRaidingParty("chaos_clan_1_party_" + _component.RaidingPartyCount + 1, _settlement, "Chaos Raiders", template, chaosClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(chaosRaidingParty, find);
            ((RaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = find;
        }
    }
}
