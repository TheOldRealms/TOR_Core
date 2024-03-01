using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.GenericConditions
{
    class AtPeaceCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom, Kingdom otherKingdom, out TextObject textObject, bool forcePlayerCharacterCosts = false, bool bypassCosts = false)
        {
            textObject = null;
            var atWar = FactionManager.IsAtWarAgainstFaction(kingdom, otherKingdom);
            if (atWar)
            {
                textObject = GameTexts.FindText("str_diplomacy_cannotBeAtWar_text");
            }
            return !atWar;
        }
    }
}