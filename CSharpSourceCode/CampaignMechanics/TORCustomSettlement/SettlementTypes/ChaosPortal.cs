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
using TOR_Core.CampaignMechanics.ChaosRaiding;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class ChaosPortal : ISettlementType
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
            var find = TORCommon.FindSettlementsAroundPosition(_settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var chaosRaidingParty = ChaosRaidingPartyComponent.CreateChaosRaidingParty("chaos_clan_1_party_" + _component.RaidingPartyCount + 1, _settlement, _component, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(chaosRaidingParty, find);
            ((ChaosRaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = find;
        }
    }
}
