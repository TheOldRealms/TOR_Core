using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    /// <summary>Town food model with Unarmed governor perk, culture bonuses, and greenskin/karak food modifiers.</summary>
    public class TORSettlementFoodModel : DefaultSettlementFoodModel
    {
        /// <summary>Daily food change with TOR culture, guild, and perk bonuses layered onto the vanilla base.</summary>
        public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
        {
            base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);
            var explainedNumber = base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);

            // Unarmed perk: food bonus in besieged governed towns.
            if (town.IsUnderSiege && town.Governor != null)
            {
                TORSkillPerkHelper.ApplyPerkPassives(town.Governor, ref explainedNumber,
                    SkillEffectType.GovernorBesiegedFood);
            }


            if (town.StringId == "town_comp_LL1")
            {
                explainedNumber.Add(40, new TextObject("Elven Metropolis"));
            }

            if (town.OwnerClan.IsCastleFaction() && town.IsCastle && !town.IsUnderSiege)
            {
                if (explainedNumber.ResultNumber < 100)
                {
                    explainedNumber.LimitMin(0);
                    explainedNumber.Add(100);
                }
            }

            if (town.Settlement.IsDwarfKarak())
            {
                explainedNumber.Add(75, new TextObject("Dwarf Karak"));
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI)
                {
                    if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_3))
                    {
                        explainedNumber.Add(25f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_2))
                    {
                        explainedNumber.Add(15f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_1))
                    {
                        explainedNumber.Add(10f, new TextObject("Brewers Guild"));
                    }
                }
            }

            if (town.Settlement.Owner == Hero.MainHero && town.Settlement.IsGreenskinCamp())
            {
                var shinies = town.Settlement.Stash.FirstOrDefaultQ(x => x.EquipmentElement.Item.StringId == "tor_gs_gold_pile").Amount;
                explainedNumber.Add(shinies / 20, new TextObject("Shiny Pile"));
            }

            // Squigherds bonus for greenskin-owned towns
            if (town.Settlement.OwnerClan?.Culture?.StringId == TORConstants.Cultures.GREENSKIN && town.Settlement.IsGreenskinCamp())
            {
                explainedNumber.Add(60, new TextObject("Squigherds"));
            }

            return explainedNumber;
        }


    }
}