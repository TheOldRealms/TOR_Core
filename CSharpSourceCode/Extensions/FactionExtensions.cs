using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.Utilities;

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
                if (kingdom != faction && faction.IsAtWarWith(kingdom) )
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
        /// Note: This will trigger native Call to War decisions for existing wars.
        /// Use SetAllianceClean() to avoid that behavior.
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

        /// <summary>
        /// Gets a list of enemies for a kingdom.
        /// </summary>
        private static List<Kingdom> GetCurrentEnemies(Kingdom kingdom)
        {
            return Kingdom.All
                .Where(k => k != kingdom && kingdom.IsAtWarWith(k))
                .ToList();
        }

        /// <summary>
        /// Removes all pending Call to War decisions from a kingdom.
        /// </summary>
        private static void RemoveCallToWarDecisions(Kingdom kingdom)
        {
            if (kingdom?.UnresolvedDecisions == null) return;

            var callToWarDecisions = kingdom.UnresolvedDecisions
                .Where(d => d.GetType().Name.Contains("CallToWar"))
                .ToList();

            foreach (var decision in callToWarDecisions)
            {
                kingdom.RemoveDecision(decision);
            }
        }

        /// <summary>
        /// Makes a kingdom join all existing wars of their new ally.
        /// These wars are marked as alliance wars so they don't count toward offensive war limits.
        /// </summary>
        private static void JoinAllyWars(Kingdom kingdom, Kingdom ally, List<Kingdom> allyEnemies)
        {
            var allianceWarBehavior = Campaign.Current?.GetCampaignBehavior<TORAllianceWarBehavior>();

            foreach (var enemy in allyEnemies)
            {
                // Skip if already at war with this enemy
                if (kingdom.IsAtWarWith(enemy)) continue;

                // Skip Chaos - those wars are eternal and handled separately
                if (enemy.Culture?.StringId == TORConstants.Cultures.CHAOS) continue;

                // Skip if the new ally would be fighting themselves (edge case)
                if (enemy == kingdom) continue;

                // Mark as alliance war (doesn't count toward offensive war limit)
                allianceWarBehavior?.MarkAsAllianceWar(kingdom, enemy);

                // Declare war
                DeclareWarAction.ApplyByKingdomDecision(kingdom, enemy);
            }
        }
    }
}