using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    class TimeElapsedSinceAllianceFormedCondition : IDiplomacyCondition
    {
        private static readonly TextObject _TTooSoon = GameTexts.FindText("str_diplomacy_alliance_notLongEnoughAllianceToBreak_text");

        public bool ApplyCondition(Kingdom kingdom, Kingdom otherKingdom, out TextObject textObject, bool forcePlayerCharacterCosts = false, bool bypassCosts = false)
        {
            textObject = null;
            var hasEnoughTimeElapsed = !TORCooldownManager.HasBreakAllianceCooldown(kingdom, otherKingdom, out var elapsedDaysUntilNow);
            if (!hasEnoughTimeElapsed)
            {
                textObject = _TTooSoon.CopyTextObject();
                textObject.SetTextVariable("ELAPSED_DAYS", (float) Math.Floor(elapsedDaysUntilNow));
                textObject.SetTextVariable("REQUIRED_DAYS", 42);
            }

            return hasEnoughTimeElapsed;
        }
    }
}