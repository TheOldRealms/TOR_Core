using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Aggression;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    public class ReligiouslyAlignedCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom, Kingdom otherKingdom, out TextObject textObject, bool forcePlayerCharacterCosts = false, bool bypassCosts = false)
        {
            textObject = null;

            var religionScore = ReligiousAggressionCalculator.CalculateReligionMultiplier(kingdom, otherKingdom);
            if (religionScore < 0)
            {
                textObject = GameTexts.FindText("str_diplomacy_alliance_notReligiouslyAligned_text");
            }

            return religionScore >= 0;
        }
    }
}
