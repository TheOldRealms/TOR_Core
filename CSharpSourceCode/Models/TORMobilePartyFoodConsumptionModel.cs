using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORMobilePartyFoodConsumptionModel : DefaultMobilePartyFoodConsumptionModel
    {
        public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
        {
            var explainedNumber = base.CalculateDailyBaseFoodConsumptionf(party, includeDescription);
            base.CalculateDailyFoodConsumptionf(party, explainedNumber);
            
            var totalMembers = party.Party.MemberRoster.Sum(item => item.Number);
            var noneatingMemberCount = party.Party.MemberRoster.Sum(item => item.Character.IsUndead() ? item.Number : 0);

            var ratio = (double)noneatingMemberCount / totalMembers;
            float saving = (float)-(ratio * explainedNumber.ResultNumber);
            
            explainedNumber.Add(saving, new TextObject("Saving from undead troops"));
            explainedNumber.LimitMax(0);
            
            if (Hero.MainHero == party.Party.LeaderHero)
            {
                AddCareerSpecificFoodPerks(ref explainedNumber, party);
            }
            
            return explainedNumber;
        }
        
        
        private void AddCareerSpecificFoodPerks(ref ExplainedNumber values, MobileParty party)
        {
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


        public override bool DoesPartyConsumeFood(MobileParty mobileParty)
        {
            var value =  base.DoesPartyConsumeFood(mobileParty);

            if (MobileParty.MainParty== mobileParty && Hero.MainHero.IsEnlisted())
            {
                return false;
            }

            if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.HasAttribute("Brasskeep") &&
                !mobileParty.LeaderHero.Clan.Settlements.AnyQ(x => x.IsTown))
            {
                return false;
            }

            return value;
        }
    }
    
  
}
