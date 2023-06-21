using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond.Ranked;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORCareerPerkCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyCareerTickEvents);
            //CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, RaidEvents);
            //CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this,RaidingPartyEvent);
            
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this,RaidingPartyEvent);
            //CampaignEvents.(this,RaidingPartyEvent);
            
            //CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, RaidingPartyEvent);
        }

        private void RaidingPartyEvent(MobileParty mobileParty, ItemRoster itemRoster)
        {
            if (mobileParty != MobileParty.MainParty&& mobileParty.MapEvent.IsRaid) return;

            if(mobileParty==null||mobileParty.LeaderHero != Hero.MainHero) return;
            if(!MobileParty.MainParty.LeaderHero.HasAnyCareer()) return;
                
            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("BladeMasterPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("BladeMasterPassive4");
                if (choice ==null) return;
                var memberList = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (member.Character.IsVampire()&&!member.Character.IsHero)
                    {
                        mobileParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                    }
                    else if (member.Character.IsHero&& member.Character.IsVampire()&& member.Character.IsHero)
                    {
                        var character = member.Character.HeroObject;
                        var skills = new List<SkillObject> { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm, DefaultSkills.Riding, DefaultSkills.Tactics };
                        var targetSkill = skills.GetRandomElement();
                        character.AddSkillXp(targetSkill, choice.GetPassiveValue());
                    }
                    
                }
            }
           
        }
        


        private void DailyCareerTickEvents(MobileParty mobileParty)
        {
            if(mobileParty!=MobileParty.MainParty) return;

            if(!MobileParty.MainParty.LeaderHero.HasAnyCareer()) return;
            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("MasterOfDeadPassive1"))
            {
                var memberList = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
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

            if (choices.Contains("PeerlessWarriorPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("PeerlessWarriorPassive4");
                if (choice != null)
                {
                    var skills = new List<SkillObject> { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm };
                    var targetSkill = skills.GetRandomElement();
                    Hero.MainHero.AddSkillXp(targetSkill, choice.GetPassiveValue()); 
                }
            }

        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}