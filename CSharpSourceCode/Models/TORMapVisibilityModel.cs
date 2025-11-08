using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if(party.HasPerk(TORPerks.Faith.ForeSight)) PerkHelper.AddPerkBonusForParty(TORPerks.Faith.ForeSight, party, false, ref result);
            
            
            if (party.IsMainParty)
            {
                if (party.LeaderHero.HasAnyCareer())
                {
                    AddCareerSpecificSpottingRangePerks(ref result, party.LeaderHero, party);
                }

                if (party.LeaderHero == Hero.MainHero)
                {
                    if (TORCommon.FindSettlementsAroundPosition(party.Position.ToVec2(), 30, (x) => x.Culture.StringId == TORConstants.Cultures.DAWI).Any())
                    {
                        if (Hero.MainHero.HasAttribute("DwarfBrewersIII"))
                        {
                            result.AddFactor(0.30f, new TextObject("Brewers Guild"));
                        } 
                        else if (Hero.MainHero.HasAttribute("DwarfBrewersII"))
                        { 
                            result.AddFactor(0.2f, new TextObject("Brewers Guild"));
                        } else if (Hero.MainHero.HasAttribute("DwarfBrewersI"))
                        {
                            result.AddFactor(0.1f, new TextObject("Brewers Guild"));
                        }
                    }
                }
            }
            
            return result;
        }
        
        private void AddCareerSpecificSpottingRangePerks(ref ExplainedNumber number, Hero mainHero, MobileParty party)
        {
            var choices = mainHero.GetAllCareerChoices();


            if (choices.Contains("VividVisionsPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("VividVisionsPassive4");
                number.AddFactor(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
            }
            
            if (choices.Contains("WitchSightPassive1"))
            {
                var choice = TORCareerChoices.GetChoice("WitchSightPassive1");
                number.AddFactor(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
            }
        }
    }
}
