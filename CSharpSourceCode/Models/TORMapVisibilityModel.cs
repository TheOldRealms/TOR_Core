using Helpers;
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
    public class TORMapVisibilityModel : DefaultMapVisibilityModel
    {
        public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetPartySpottingRange(party, includeDescriptions);
            if(party.HasPerk(TORPerks.Faith.ForeSight)) PerkHelper.AddPerkBonusForParty(TORPerks.Faith.ForeSight, party, false, ref result);
            return result;
        }
    }
}
