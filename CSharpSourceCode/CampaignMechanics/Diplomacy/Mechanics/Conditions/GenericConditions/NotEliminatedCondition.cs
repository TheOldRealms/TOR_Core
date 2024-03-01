using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.GenericConditions
{
    internal sealed class NotEliminatedCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom,
                                   Kingdom otherKingdom,
                                   out TextObject textObject,
                                   bool forcePlayerCosts = false,
                                   bool bypassCosts = false)
        {
            textObject = null;

            var eliminated = !(kingdom.IsEliminated || otherKingdom.IsEliminated);

            if (!eliminated)
                textObject = GameTexts.FindText("str_diplomacy_factionEliminated_text");

            return eliminated;
        }
    }
}