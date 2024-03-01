using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    internal class TimeElapsedSinceLastWarCondition : IDiplomacyCondition
    {
        private static readonly TextObject _TTooSoon = GameTexts.FindText("str_diplomacy_alliance_atWarTooRecently_text");
        private const double MinimumTimeFromLastWar = 30.0;

        public bool ApplyCondition(Kingdom kingdom, Kingdom otherKingdom, out TextObject textObject, bool forcePlayerCharacterCosts = false, bool bypassCosts = false)
        {
            textObject = null;
            var gameActionLogs = Campaign.Current.LogEntryHistory.GameActionLogs;

            var lastPeaceTime = CampaignTime.Never;
            foreach (var logEntry in gameActionLogs)
            {
                if (logEntry is MakePeaceLogEntry entry
                    && ((entry.Faction1 == kingdom.MapFaction && entry.Faction2 == otherKingdom.MapFaction) || (entry.Faction1 == otherKingdom.MapFaction && entry.Faction2 == kingdom.MapFaction)))
                {
                    lastPeaceTime = entry.GameTime;
                    break;
                }
            }

            var daysSinceLastWar = CampaignTime.Now.ToDays - lastPeaceTime.ToDays;
            var hasEnoughTimeElapsed = lastPeaceTime == CampaignTime.Never || daysSinceLastWar > MinimumTimeFromLastWar;
            if (!hasEnoughTimeElapsed)
            {
                textObject = _TTooSoon.CopyTextObject();
                textObject.SetTextVariable("ELAPSED_DAYS", (int) daysSinceLastWar);
                textObject.SetTextVariable("REQUIRED_DAYS", (int) MinimumTimeFromLastWar);
            }
            return hasEnoughTimeElapsed;
        }
    }
}