using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.GenericConditions
{
    internal sealed class HasAuthorityCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom,
                                   Kingdom otherKingdom,
                                   out TextObject textObject,
                                   bool forcePlayerCosts = false,
                                   bool bypassCosts = false)
        {
            textObject = null;

            var authority = Clan.PlayerClan.Kingdom != kingdom
                || (kingdom?.Leader.IsHumanPlayerCharacter ?? false)
                || !forcePlayerCosts;

            if (!authority)
                textObject = GameTexts.FindText("str_diplomacy_youDontHaveAuthority_text");

            return authority;
        }
    }
}