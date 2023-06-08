using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORMobilePartyFoodConsumptionModel : DefaultMobilePartyFoodConsumptionModel
    {
        public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
        {
            var eatingMemberRoster = party.Party.MemberRoster.GetTroopRoster().WhereQ(x => !x.Character.IsUndead());
            int eatingMemberNum = 0;
            foreach(var item in eatingMemberRoster)
            {
                eatingMemberNum += item.Number;
            }

            var eatingPrisonerRoster = party.Party.PrisonRoster.GetTroopRoster().WhereQ(x => !x.Character.IsUndead());
            int eatingPrisonerNum = 0;
            foreach (var item in eatingPrisonerRoster)
            {
                eatingPrisonerNum += item.Number;
            }

            float num = eatingMemberNum + eatingPrisonerNum / 2;

            num = ((num < 1) ? 1 : num);
            float resultNumber = -num / (float)NumberOfMenOnMapToEatOneFood;
            
            
            var number = new ExplainedNumber(resultNumber, includeDescription, null);
            
            if (Hero.MainHero == party.Party.LeaderHero)
            {
                AddCareerSpecificFoodPerks(ref number, party);
            }
       
            return number;
        }
        
        
        private void AddCareerSpecificFoodPerks(ref ExplainedNumber values, MobileParty party)
        {
            //TODO Generalized perks?
            var choices = party.LeaderHero.GetAllCareerChoices();
            
            if (choices.Contains("SigmarsProclaimerPassive3"))
            {
                bool includeRegularTroops = choices.Contains("ArchLectorPassive2");
                var choice = TORCareerChoices.GetChoice("SigmarsProclaimerPassive3");
                var perkValue = AddSigmarsProclaimerPerk(values, party,choice,includeRegularTroops);
                values.Add(perkValue, choice.BelongsToGroup.Name);
            }
            
            
        }

        private float AddSigmarsProclaimerPerk(ExplainedNumber values, MobileParty party, CareerChoiceObject perkChoice, bool includeRegularTroops)
        {
            if(perkChoice==null) return 0;
            var choices = party.LeaderHero.GetAllCareerChoices();
            

            var troops = party.MemberRoster.GetTroopRoster();
            var sigmarRiteTroops = new MBList<TroopRosterElement>();
            
            foreach (var troopRosterElement in troops.Where(troopRosterElement => troopRosterElement.Character.IsSoldier)) //could be all a nice query , doesn't work for whatever reason
            {
                if (!troopRosterElement.Character.UnitBelongsToCult("cult_of_sigmar"))
                {
                    if(troopRosterElement.Character.IsReligiousUnit())
                       continue; 
                    if (includeRegularTroops)
                    {
                        sigmarRiteTroops.Add(troopRosterElement);
                    }
                }
                else
                {
                    sigmarRiteTroops.Add(troopRosterElement);
                }
            }

            var count = sigmarRiteTroops.Sum(x => x.Number);
            if (perkChoice.Passive == null) return 0f;
            var effectMagnitude = perkChoice.Passive.EffectMagnitude;
            if (perkChoice.Passive.InterpretAsPercentage) effectMagnitude /= 100;
            float basefoodConsumptionForRoster =  ((float)count / NumberOfMenOnMapToEatOneFood);
            return basefoodConsumptionForRoster * effectMagnitude;
        }
    }
    
  
}
