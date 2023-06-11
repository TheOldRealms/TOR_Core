using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORCareerPerkCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyCareerTickEvents);
        }




        private void DailyCareerTickEvents(MobileParty mobileParty)
        {
            if(mobileParty!=MobileParty.MainParty) return;

            if(!MobileParty.MainParty.LeaderHero.HasAnyCareer()) return;
            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("MasterOfDeadPassive1"))
            {
                var list = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < list.Count; index++)
                {
                    var member = list[index];
                    if (member.Character.IsUndead()&&!member.Character.IsHero)
                    {
                        var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive1");
                        if (choice != null)
                        {
                            mobileParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                        }
                    }
                }
            }
            
            
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}