using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyWageModel : DefaultPartyWageModel
    {

        public override int GetCharacterWage(CharacterObject character)
        {
            if (character.IsUndead()) return 0;

            switch (character.Tier)
            {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 5;
                case 4:
                    return 8;
                case 5:
                    return 12;
                case 6:
                    return 17;
                case 7:
                    return 23;
                case 8:
                    return 30;
                default:
                    return 40;
            }
        }

        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions = false)
        { 
            var value = base.GetTotalWage(mobileParty, includeDescriptions);


            
            
            for (int index = 0; index < mobileParty.MemberRoster.Count; ++index)
            {
                TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(index);
                if (mobileParty.IsMainParty)
                {
                    var careerID = mobileParty.LeaderHero.GetCareer().StringId;
                    value = AddCareerSpecifWagePerks(value, mobileParty.LeaderHero,careerID, elementCopyAtIndex);
                }
                
                
            }

            return value;
        }

        private ExplainedNumber AddCareerSpecifWagePerks(ExplainedNumber resultValue, Hero hero, string CareerID, TroopRosterElement unit)
        {
            
            //TODO Generalized perks?
            
            if (CareerID == CareerHelper.WARRIORRPRIEST)
            {
                var choices = hero.GetAllCareerChoices();

                if (unit.Character.IsSoldier)
                {
                    if(unit.Character.IsDevotedToCult("cult_of_sigmar")||(!unit.Character.IsReligiousUnit()&&hero.GetAllCareerChoices().Contains("ArchLectorPassive4")))
                    {
                        if (hero.HasCareerChoice("SigmarsProclaimerPassive2"))
                        {
                            var choice = TORCareerChoices.GetChoice("SigmarsProclaimerPassive2");
                            if (choice == null || choice.Passive==null) return resultValue;
                            var effectMagnitude = choice.Passive.EffectMagnitude;
                            if (choice.Passive.InterpretAsPercentage) effectMagnitude /= 100;
                            var value = (unit.Character.TroopWage*unit.Number) * (1-effectMagnitude);
                      
                            resultValue.Add(-value*effectMagnitude, choice.BelongsToGroup.Name);
                        }
                    }
                }
               

            }

            return resultValue;
        }
        
    }
}
