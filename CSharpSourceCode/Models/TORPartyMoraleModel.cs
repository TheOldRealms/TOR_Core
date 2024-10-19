using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartyMoraleModel : DefaultPartyMoraleModel
    {
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
        {
            var result = base.GetEffectivePartyMorale(mobileParty, includeDescription);

            if (mobileParty.IsMainParty)
            {
                AddCareerPassivesForTroopMorale(mobileParty, ref result);
            }
            if (mobileParty.HasPerk(TORPerks.SpellCraft.StoryTeller))
            {
                result.Add(TORPerks.SpellCraft.StoryTeller.SecondaryBonus, TORPerks.SpellCraft.StoryTeller.Name);
            }

            if (mobileParty.LeaderHero == Hero.MainHero&& Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                var chivalryLevel = mobileParty.LeaderHero.GetChivalryLevel();
                var value = 0f;
                if (chivalryLevel == ChivalryLevel.Unknightly)
                {
                    value = -0.75f;
                }
                if (chivalryLevel == ChivalryLevel.Uninspiring)
                {
                    value = -0.5f;
                }
                if (chivalryLevel == ChivalryLevel.Sincere)
                {
                    value = -0.2f;
                }
                if (chivalryLevel == ChivalryLevel.Noteworthy)
                {
                    value = 0f;
                }
                if (chivalryLevel == ChivalryLevel.PureHearted)
                {
                    value = 0.1f;
                }
                if (chivalryLevel == ChivalryLevel.Honourable)
                {
                    value = 0.2f;
                }
                
                result.AddFactor(value,new TextObject(chivalryLevel.ToString()));
            }
            return result;
        }
        
        private void AddCareerPassivesForTroopMorale(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if(party.LeaderHero==null)return;
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.TroopMorale);
            }
        }
    }
}
