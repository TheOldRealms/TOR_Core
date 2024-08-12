using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORCombatXpModel : DefaultCombatXpModel
    {
        public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeWeaponHit)
        {
            SkillObject result = DefaultSkills.Athletics;
            var baseResult = base.GetSkillForWeapon(weapon, isSiegeWeaponHit);
            if (baseResult != null) result = baseResult;
            return result;
        }


        public override void GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase party, int damage, bool isFatal, MissionTypeEnum missionType, out int xpAmount)
        {
            xpAmount = 0;
   
            base.GetXpFromHit(attackerTroop, captain, attackedTroop, party, damage, isFatal, missionType, out xpAmount);

            if(missionType != MissionTypeEnum.Battle) return;
            
            if(party==null || (party != PartyBase.MainParty&&!MobileParty.MainParty.LeaderHero.HasAnyCareer())) return;
            
            ExplainedNumber number = new ExplainedNumber();
            number.Add(xpAmount);
            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (isFatal && attackerTroop.Tier>3&&party.MobileParty == MobileParty.MainParty&&choices.Contains("PeerlessWarriorPassive3"))
            {
                var choice = TORCareerChoices.GetChoice("PeerlessWarriorPassive3");
                if (choice != null)
                {
                    number.AddFactor(choice.GetPassiveValue());
                }
            }
            
            if (isFatal && party.MobileParty == MobileParty.MainParty&&MobileParty.MainParty.HasBlessing("cult_of_ulric"))
            {
                number.AddFactor(0.2f);
            }
            
            xpAmount = (int) number.ResultNumber;
        }


        
        
    }
}
