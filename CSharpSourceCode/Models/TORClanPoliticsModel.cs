using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORClanPoliticsModel : DefaultClanPoliticsModel
    {
        private const int INFLUENCE_TRESHHOLD = 550;

        public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
        {
            var result = base.CalculateInfluenceChange(clan, includeDescriptions);
            PerkHelper.AddPerkBonusForCharacter(TORPerks.Faith.Blessed, clan.Leader.CharacterObject, true, ref result);
            if(clan != Clan.PlayerClan && clan.Influence < INFLUENCE_TRESHHOLD)
            {
                result.Add(clan.Tier * 15f, new("AI Bonus"));
            }
            return result;
        }
    }
}
