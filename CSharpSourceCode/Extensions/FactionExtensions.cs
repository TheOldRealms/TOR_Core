using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace TOR_Core.Extensions
{
    public static class FactionExtensions
    {
        /// <summary>
        /// Gets the number of active kingdom wars for a faction.
        /// In 1.3, we iterate over all kingdoms and check IsAtWarWith.
        /// </summary>
        public static int GetNumActiveKingdomWars(this IFaction faction)
        {
            if (faction == null) return 0;

            int count = 0;
            foreach (var kingdom in Kingdom.All)
            {
                if (kingdom != faction && faction.IsAtWarWith(kingdom))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets the sum of enemy kingdom power (including their allies).
        /// </summary>
        public static float GetSumEnemyKingdomPower(this IFaction faction)
        {
            if (faction == null) return 0f;

            float sum = 0f;
            foreach (var kingdom in Kingdom.All)
            {
                if (kingdom != faction && faction.IsAtWarWith(kingdom))
                {
                    sum += kingdom.GetAllianceTotalStrength();
                }
            }
            return sum;
        }

        /// <summary>
        /// Checks if two factions are allied.
        /// In 1.3, alliances are managed through IAllianceCampaignBehavior and Kingdom.IsAllyWith().
        /// </summary>
        public static bool IsAlliedWith(this IFaction faction1, IFaction faction2)
        {
            if (faction1 == null || faction2 == null || faction1 == faction2)
            {
                return false;
            }

            // Alliances only exist between kingdoms in 1.3
            if (faction1 is Kingdom kingdom1 && faction2 is Kingdom kingdom2)
            {
                return kingdom1.IsAllyWith(kingdom2);
            }

            return false;
        }

        /// <summary>
        /// Gets all allied factions for a given faction.
        /// In 1.3, uses Kingdom.AlliedKingdoms.
        /// </summary>
        public static IEnumerable<IFaction> GetAlliedFactions(this IFaction faction)
        {
            if (faction is Kingdom kingdom)
            {
                return kingdom.AlliedKingdoms.Cast<IFaction>();
            }
            return Enumerable.Empty<IFaction>();
        }

        /// <summary>
        /// Gets the total strength of a faction including all its allies.
        /// In 1.3, uses CurrentTotalStrength instead of TotalStrength.
        /// </summary>
        public static float GetAllianceTotalStrength(this IFaction faction)
        {
            if (faction == null) return 0f;

            float totalStrength = faction.CurrentTotalStrength;

            foreach (var ally in faction.GetAlliedFactions())
            {
                totalStrength += ally.CurrentTotalStrength;
            }

            return totalStrength;
        }

        /// <summary>
        /// Creates an alliance between two kingdoms using the 1.3 IAllianceCampaignBehavior.
        /// </summary>
        public static void SetAlliance(this IFaction factionA, IFaction factionB)
        {
            if (factionA is not Kingdom kingdom1 || factionB is not Kingdom kingdom2)
            {
                return;
            }

            var allianceBehavior = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            allianceBehavior?.StartAlliance(kingdom1, kingdom2);
        }

        // Note: Breaking alliances in 1.3 is handled internally by IAllianceCampaignBehavior
        // when diplomatic conditions change. Manual alliance breaking requires further investigation
        // of the exact API.
    }
}