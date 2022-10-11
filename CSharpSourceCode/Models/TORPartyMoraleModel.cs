using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment;

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
            return result;
        }
    }
}
