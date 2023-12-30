using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Invasions
{
    public class InvasionCampaignBehavior : CampaignBehaviorBase
    {
        private MobileParty _currentInvadingParty;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, HourlyPartyTick);
            CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener(this, OnSiegeCompleted);
        }

        private void OnSiegeCompleted(Settlement settlement, MobileParty attackerParty, bool win, MapEvent.BattleTypes types)
        {
            if(attackerParty == _currentInvadingParty && !win)
            {
                _currentInvadingParty = null;
                KillCharacterAction.ApplyByBattle(attackerParty.LeaderHero, null);
                DestroyPartyAction.Apply(settlement.Party, attackerParty);
            }
        }

        private void HourlyPartyTick(MobileParty party, PartyThinkParams thinkParams)
        {
            if (party.IsInvasionParty())
            {
                var component = (IInvasionParty)party.PartyComponent;
                component.HourlyTickAI(thinkParams);
            }
        }

        private void Tick(float dt)
        {
            //Delete parties without a valid a Target if needed
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            /*
            if (_currentInvadingParty != null) return;
            if (settlement.SettlementComponent is ChaosPortalComponent)
            {
                PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_patrol");
                Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
                CharacterObject leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_bw_cultist_lord_0");
                var leader = HeroCreator.CreateSpecialHero(leaderTemplate, settlement, chaosClan, null, 45);
                chaosClan.UpdateHomeSettlement(settlement);
                leader.BornSettlement = settlement;
                leader.UpdateHomeSettlement();
                _currentInvadingParty = InvasionPartyComponent.CreateInvasionForce("chaos_clan_1_invading_party" + MBRandom.RandomInt(111,111111), settlement, "Chaos Invaders", template, chaosClan, leader ,MBRandom.RandomInt(1200, 1500));
            }
            */
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
