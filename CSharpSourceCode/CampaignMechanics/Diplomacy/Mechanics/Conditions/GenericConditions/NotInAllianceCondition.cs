using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.GenericConditions
{
    internal sealed class NotInAllianceCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom,
                                   Kingdom otherKingdom,
                                   out TextObject textObject,
                                   bool forcePlayerCharacterCosts = false,
                                   bool bypassCosts = false)
        {
            textObject = null;

            var alreadyInAlliance = FactionManager.IsAlliedWithFaction(kingdom, otherKingdom);

            if (alreadyInAlliance)
                textObject = GameTexts.FindText("str_diplomacy_cannotBeInAlliance_text");

            return !alreadyInAlliance;
        }
    }
}