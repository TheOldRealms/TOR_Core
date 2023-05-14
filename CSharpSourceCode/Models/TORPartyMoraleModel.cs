using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyMoraleModel : DefaultPartyMoraleModel
    {
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
        {
            var result = base.GetEffectivePartyMorale(mobileParty, includeDescription);
            if (mobileParty.HasPerk(TORPerks.SpellCraft.StoryTeller))
            {
                result.Add(TORPerks.SpellCraft.StoryTeller.SecondaryBonus, TORPerks.SpellCraft.StoryTeller.Name);
            }
            if (mobileParty.HasBlessing("cult_of_lady"))
            {
                result.Add(20, GameTexts.FindText("tor_religion_blessing_name", "cult_of_lady"));
            }
            return result;
        }
    }
}
