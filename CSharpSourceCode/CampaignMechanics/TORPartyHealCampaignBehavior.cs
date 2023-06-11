using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORPartyHealCampaignBehavior : PartyHealCampaignBehavior       //Why does that need to be in a campaign behavior? There is a healing model already
    {
        public override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(HealParty));
        }

        private void HealParty(MobileParty party)
        {
            if (party.IsActive && party.MapEvent == null)
            {
                foreach (var troopRoster in party.MemberRoster.GetTroopRoster())
                {
                    if (troopRoster.Character.IsHero && troopRoster.Character.HeroObject.IsVampire())
                    {
                        troopRoster.Character.HeroObject.Heal(troopRoster.Character.HeroObject.HitPoints / 10,false);
                    }
                }
            }
        }
    }
}
