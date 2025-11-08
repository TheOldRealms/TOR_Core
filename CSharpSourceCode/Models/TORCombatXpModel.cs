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

        public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase party, int damage, bool isFatal, MissionTypeEnum missionType)
        {   
            var xpAmount = base.GetXpFromHit(attackerTroop, captain, attackedTroop, party, damage, isFatal, missionType);

            if(missionType != MissionTypeEnum.Battle) return xpAmount;

            if (party == null || (party != PartyBase.MainParty && !MobileParty.MainParty.LeaderHero.HasAnyCareer())) return xpAmount;
            
            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (isFatal && attackerTroop.Tier>3&&party.MobileParty == MobileParty.MainParty&&choices.Contains("PeerlessWarriorPassive3"))
            {
                var choice = TORCareerChoices.GetChoice("PeerlessWarriorPassive3");
                if (choice != null)
                {
                    xpAmount.AddFactor(choice.GetPassiveValue());
                }
            }
            
            if (isFatal && party.MobileParty == MobileParty.MainParty&&MobileParty.MainParty.HasBlessing("cult_of_ulric"))
            {
                xpAmount.AddFactor(0.2f);
            }

            return xpAmount;
        }


        
        
    }
}
