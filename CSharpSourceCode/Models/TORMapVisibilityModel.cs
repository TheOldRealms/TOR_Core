using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORMapVisibilityModel : DefaultMapVisibilityModel
    {
        public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetPartySpottingRange(party, includeDescriptions);
            if (party.HasPerk(TORPerks.Faith.ForeSight)) PerkHelper.AddPerkBonusForParty(TORPerks.Faith.ForeSight, party, false, ref result);

            if (party.IsMainParty && party.LeaderHero != null && party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref result, PassiveEffectType.PartySpottingRange, true);
            }

            // DwarfBrewers handling
            if (party.IsMainParty && party.LeaderHero == Hero.MainHero)
            {
                if (TORCommon.FindSettlementsAroundPosition(party.Position.ToVec2(), 30, (x) => x.Culture.StringId == TORConstants.Cultures.DAWI).Any())
                {
                    if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_3))
                    {
                        result.AddFactor(0.30f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_2))
                    {
                        result.AddFactor(0.2f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_1))
                    {
                        result.AddFactor(0.1f, new TextObject("Brewers Guild"));
                    }
                }
            }

            return result;
        }
    }
}