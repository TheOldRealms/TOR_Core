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
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORMobilePartyFoodConsumptionModel : DefaultMobilePartyFoodConsumptionModel
    {
        public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
        {
            var explainedNumber = base.CalculateDailyBaseFoodConsumptionf(party, includeDescription);
            base.CalculateDailyFoodConsumptionf(party, explainedNumber);

            if (party.Party.Culture.StringId == TORConstants.Cultures.SYLVANIA || party.Party.Culture.StringId == TORConstants.Cultures.MOUSILLON)
            {

                var totalMembers = party.Party.MemberRoster.Sum(item => item.Number);
                var noneatingMemberCount = party.Party.MemberRoster.Sum(item => item.Character.IsUndead() ? item.Number : 0);
                var ratio = (double)noneatingMemberCount / totalMembers;
                float saving = (float)-(ratio * explainedNumber.ResultNumber);

                explainedNumber.Add(saving, new TextObject("Saving from undead troops"));
            }

            if (party != MobileParty.MainParty) return explainedNumber;


            if (Hero.MainHero == party.LeaderHero)
            {
                AddCareerSpecificFoodPerks(ref explainedNumber, party);
            }


            // Apply greenskin-specific food consumption rates
            if (party.Party.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                var totalMembers = party.Party.MemberRoster.Sum(item => item.Number);
                if (totalMembers > 0)
                {
                    var eliteOrcCount = party.Party.MemberRoster.Sum(item => item.Character.IsOrc() && item.Character.IsEliteTroop() ? item.Number : 0);
                    var regularOrcCount = party.Party.MemberRoster.Sum(item => item.Character.IsOrc() && !item.Character.IsEliteTroop() ? item.Number : 0);
                    var trollCount = party.Party.MemberRoster.Sum(item => item.Character.IsTroll() ? item.Number : 0);
                    var goblinCount = party.Party.MemberRoster.Sum(item => item.Character.IsGoblin() ? item.Number : 0);

                    // Calculate additional food consumption:
                    // Goblins eat 1x (normal), Orcs eat 2x (1x extra), Elite Orcs eat 4x (3x extra), Trolls eat 10x (9x extra)
                    float baseFoodPerTroop = explainedNumber.ResultNumber / totalMembers;
                    float additionalRegularOrcFood = regularOrcCount * baseFoodPerTroop * 1.0f; // 1x extra (double total = 2x)
                    float additionalEliteOrcFood = eliteOrcCount * baseFoodPerTroop * 3.0f; // 3x extra (quadruple total = 4x)
                    float additionalTrollFood = trollCount * baseFoodPerTroop * 9.0f; // 9x extra (10x total)

                    float totalAdditionalFood = additionalRegularOrcFood + additionalEliteOrcFood + additionalTrollFood;

                    if (totalAdditionalFood != 0)
                    {
                        explainedNumber.Add(totalAdditionalFood, new TextObject("{=str_tor_greenskin_appetite}Greenskin appetite"));
                    }
                }
            }



            if (party.LeaderHero == Hero.MainHero && Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (Hero.MainHero.HasAttribute("Wargh1"))
                {
                    explainedNumber.AddFactor(-0.6f, new TextObject("Internal Fightin'"));
                }
                else if (Hero.MainHero.HasAttribute("Wargh2"))
                {
                    explainedNumber.AddFactor(-0.3f, new TextObject("Petty Squabblin'"));
                }
                else if (Hero.MainHero.HasAttribute("Wargh3"))
                {
                    explainedNumber.AddFactor(0.25f, new TextObject("'Ere We Go!"));
                }
                else if (Hero.MainHero.HasAttribute("Wargh4"))
                {
                    explainedNumber.AddFactor(1.0f, new TextObject("WAAAGH!!!!"));
                }
            }

            explainedNumber.LimitMax(0);//food consumption is a negative number
            return explainedNumber;
        }


        private void AddCareerSpecificFoodPerks(ref ExplainedNumber values, MobileParty party)
        {
            var choices = party.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("SigmarsProclaimerPassive3"))
            {
                bool includeRegularTroops = choices.Contains("ArchLectorPassive2");
                var choice = TORCareerChoices.GetChoice("SigmarsProclaimerPassive3");
                var perkValue = AddSigmarsProclaimerPerk(values, party, choice, includeRegularTroops);
                values.Add(perkValue, choice.BelongsToGroup.Name);
            }


        }

        private float AddSigmarsProclaimerPerk(ExplainedNumber values, MobileParty party, CareerChoiceObject perkChoice, bool includeRegularTroops)
        {
            if (perkChoice == null) return 0;
            var choices = party.LeaderHero.GetAllCareerChoices();


            var troops = party.MemberRoster.GetTroopRoster();
            var sigmarRiteTroops = new MBList<TroopRosterElement>();

            foreach (var troopRosterElement in troops.Where(troopRosterElement => troopRosterElement.Character.IsSoldier)) //could be all a nice query , doesn't work for whatever reason
            {
                if (!troopRosterElement.Character.UnitBelongsToCult("cult_of_sigmar"))
                {
                    if (troopRosterElement.Character.IsReligiousUnit())
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
            float basefoodConsumptionForRoster = ((float)count / NumberOfMenOnMapToEatOneFood);
            return basefoodConsumptionForRoster * effectMagnitude;
        }


        public override bool DoesPartyConsumeFood(MobileParty mobileParty)
        {
            var value = base.DoesPartyConsumeFood(mobileParty);

            if (MobileParty.MainParty == mobileParty && Hero.MainHero.IsEnlisted())
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