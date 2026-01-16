using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.RaidingParties;
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

                explainedNumber.Add(saving, TORTextHelper.GetTextObject("tor_undead_food_saving_text", "Saving from undead troops"));
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
                        explainedNumber.Add(totalAdditionalFood, TORTextHelper.GetTextObject("tor_greenskin_appetite", "Greenskin appetite"));
                    }
                }
            }



            if (party.LeaderHero == Hero.MainHero && Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (Hero.MainHero.HasAttribute("Waaagh0"))
                {
                    explainedNumber.AddFactor(-0.6f, TORTextHelper.GetTextObject("tor_greenskin_internal_fightin_text", "Internal Fightin'"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh1"))
                {
                    explainedNumber.AddFactor(-0.3f, TORTextHelper.GetTextObject("tor_greenskin_petty_squabblin_text", "Petty Squabblin'"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh2"))
                {
                    explainedNumber.AddFactor(0.25f, TORTextHelper.GetTextObject("tor_greenskin_ere_we_go_text", "'Ere We Go!"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh3"))
                {
                    explainedNumber.AddFactor(1.0f, TORTextHelper.GetTextObject("tor_greenskin_waaagh_text", "WAAAGH!!!!"));
                }
            }

            explainedNumber.LimitMax(0);//food consumption is a negative number
            return explainedNumber;
        }


        private void AddCareerSpecificFoodPerks(ref ExplainedNumber values, MobileParty party)
        {
        }


        public override bool DoesPartyConsumeFood(MobileParty mobileParty)
        {
            var value = base.DoesPartyConsumeFood(mobileParty);

            //Sly : Raiding parties will stop starving once they've been away from their spawn settlement after ~40 days (they receive 2 food per party member on spawn).
            if (mobileParty.PartyComponent is RaidingPartyComponent)
            {
                return false;
            }

            //Sly : both chaos revolts and brasskeep will have no food consumption to skip their AI needing to find settlements for replenishing.
            if (mobileParty.Party.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                return false;
            }

            //Sly : rogue engineer party won't starve. This will need to be finessed as more quests are added.
            if (mobileParty.IsCurrentlyUsedByAQuest)
            {
                return true;
            }

            if (MobileParty.MainParty == mobileParty && Hero.MainHero.IsEnlisted())
            {
                return false;
            }

            return value;
        }
    }


}