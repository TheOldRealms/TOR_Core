using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    internal class HasEnoughScoreCondition : IDiplomacyCondition
    {
        public bool ApplyCondition(Kingdom kingdom, Kingdom otherKingdom, out TextObject textObject, bool forcePlayerCharacterCosts = false, bool bypassCosts = false)
        {
            textObject = null;
            var scoreTooLow = AllianceScoringModel.ScoringModelInstance.GetScore(kingdom, otherKingdom).ResultNumber < AllianceScoringModel.ScoringModelInstance.ScoreThreshold;
            if (scoreTooLow)
            {
                textObject = GameTexts.FindText("str_diplomacy_alliance_notInterested_text");
            }
            return !scoreTooLow;
        }
    }
}