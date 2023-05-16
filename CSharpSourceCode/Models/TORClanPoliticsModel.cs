using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORClanPoliticsModel : DefaultClanPoliticsModel
    {
        public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
        {
            var result = base.CalculateInfluenceChange(clan, includeDescriptions);
            PerkHelper.AddPerkBonusForCharacter(TORPerks.Faith.Blessed, clan.Leader.CharacterObject, true, ref result);
            return result;
        }
    }
}
