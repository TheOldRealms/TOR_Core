using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    class AlliancesEnabledCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom, Kingdom otherKingdom, out TextObject textObject, bool forcePlayerCharacterCosts = false, bool bypassCosts = false)
        {
            textObject = null;

            return true;
        }
    }
}