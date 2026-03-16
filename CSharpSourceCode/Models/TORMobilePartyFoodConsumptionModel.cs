using System;
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
        //MobileParty.FoodChange calls BaseFoodConsumption (member count) first, then FoodConsumption (perk effects) on it.
        public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
        {
            //almost identical to base from 1.3.15, prisoners removed though, specifically for undead parties who don't stock enough food to handle feeding them.
            int troopCount = party.Party.NumberOfAllMembers;
			troopCount = Math.Max(1, troopCount);//there exist divisions that blindly take in consumption values for the denominator - the AI will crash if food consumption hits 0. For the player, unknown.
			return new ExplainedNumber(-(float)troopCount / (float)base.NumberOfMenOnMapToEatOneFood, includeDescription, null);
        }

        public override ExplainedNumber CalculateDailyFoodConsumptionf(MobileParty party, ExplainedNumber consumption)
		{
            if (!Campaign.Current.Models.MobilePartyFoodConsumptionModel.DoesPartyConsumeFood(party))
            {
                return consumption;//in case this gets hit for parties that don't need this value, it will skip heavier calculations
            }

            base.CalculateDailyFoodConsumptionf (party, consumption);
			
            if (party.Party.Culture.StringId == TORConstants.Cultures.SYLVANIA || party.Party.Culture.StringId == TORConstants.Cultures.MOUSILLON)
            {
                var totalMembers = party.Party.NumberOfAllMembers;
                var noneatingMemberCount = party.Party.MemberRoster.Sum(item => item.Character.IsUndead() ? item.Number : 0);
                var ratio = (double)noneatingMemberCount / totalMembers;
                float saving = (float)-(ratio * consumption.ResultNumber);

                consumption.Add(saving, TORTextHelper.GetTextObject("tor_undead_food_saving_text", "Saving from undead troops"));
            }

            if (party != MobileParty.MainParty) return consumption;


            if (Hero.MainHero == party.LeaderHero)
            {
                AddCareerSpecificFoodPerks(ref consumption, party);
            }


            // Apply greenskin-specific food consumption rates
            if (party.IsMainParty && party.Party.Culture.StringId == TORConstants.Cultures.GREENSKIN)       //Main party only - npcs are dumb sadly
            {
                var totalMembers = party.Party.MemberRoster.Sum(item => item.Number);
                if (totalMembers > 0)
                {
                    var eliteOrcCount = party.Party.MemberRoster.Sum(item => item.Character.IsOrc() && item.Character.IsEliteTroop() ? item.Number : 0);
                    var regularOrcCount = party.Party.MemberRoster.Sum(item => item.Character.IsOrc() && !item.Character.IsEliteTroop() ? item.Number : 0);
                    var trollCount = party.Party.MemberRoster.Sum(item => item.Character.IsTroll() ? item.Number : 0);

                    // Calculate additional food consumption:
                    // Goblins eat 1x (normal), Orcs eat 2x (1x extra), Elite Orcs eat 4x (3x extra), Trolls eat 10x (9x extra)
                    float baseFoodPerTroop = consumption.ResultNumber / totalMembers;
                    float additionalRegularOrcFood = regularOrcCount * baseFoodPerTroop * 0.5f; // 1x extra (double total = 1.5x)
                    float additionalEliteOrcFood = eliteOrcCount * baseFoodPerTroop * 2.0f; // 3x extra (quadruple total = 4x)
                    float additionalTrollFood = trollCount * baseFoodPerTroop * 7.0f; // 7x extra (8x total)

                    float totalAdditionalFood = additionalRegularOrcFood + additionalEliteOrcFood + additionalTrollFood;

                    if (totalAdditionalFood != 0)
                    {
                        consumption.Add(totalAdditionalFood, TORTextHelper.GetTextObject("tor_greenskin_appetite", "Greenskin appetite"));
                    }
                }
            }



            if (party.LeaderHero == Hero.MainHero && Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (Hero.MainHero.HasAttribute("Waaagh0"))
                {
                    consumption.AddFactor(-0.6f, TORTextHelper.GetTextObject("tor_greenskin_internal_fightin_text", "Internal Fightin'"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh1"))
                {
                    consumption.AddFactor(-0.3f, TORTextHelper.GetTextObject("tor_greenskin_petty_squabblin_text", "Petty Squabblin'"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh2"))
                {
                    consumption.AddFactor(0.25f, TORTextHelper.GetTextObject("tor_greenskin_ere_we_go_text", "'Ere We Go!"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh3"))
                {
                    consumption.AddFactor(1.0f, TORTextHelper.GetTextObject("tor_greenskin_waaagh_text", "WAAAGH!!!!"));
                }
            }

            //there exist divisions that blindly take in consumption values for the denominator - the AI will crash if food consumption hits 0. For the player, unknown.
            consumption.LimitMax(-0.01f);//food consumption is a negative number
            return consumption;
		}

        private void AddCareerSpecificFoodPerks(ref ExplainedNumber values, MobileParty party)
        {
        }


        public override bool DoesPartyConsumeFood(MobileParty mobileParty)
        {
            var value = base.DoesPartyConsumeFood(mobileParty);

            //Sly : Raiding parties would stop starving once they've been away from their spawn settlement after ~40 days (they receive 2 food per party member on spawn).
            if (mobileParty.IsRaidingParty())
            {
                return false;
            }

            //Sly : both chaos revolts and brasskeep will have no food consumption to skip their AI needing to find settlements for replenishing.
            if (mobileParty.Party.MapFaction?.Culture.StringId == TORConstants.Cultures.CHAOS)//Party.Culture doesn't null protect against MapFaction
            {
                return false;
            }

            //Sly : greenskin stronghold factions have no food consumption for parties as they have no direct way to gain food.
            if (mobileParty.Party.MapFaction?.Culture.StringId == TORConstants.Cultures.GREENSKIN && (mobileParty.Party.MapFaction.StringId == TORConstants.Factions.BLACK_PIT || mobileParty.Party.MapFaction.StringId == TORConstants.Factions.REAVAZ))
            {
                return false;
            }

            //Sly : rogue engineer party won't starve. This will need to be finessed as more quests are added.
            if (mobileParty.IsCurrentlyUsedByAQuest)
            {
                return false;
            }

            if (MobileParty.MainParty == mobileParty && Hero.MainHero.IsEnlisted())
            {
                return false;
            }

            return value;
        }
    }


}