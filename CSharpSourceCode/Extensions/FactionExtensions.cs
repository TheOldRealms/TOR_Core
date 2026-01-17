using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TOR_Core.CampaignMechanics.Diplomacy;

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
        /// Creates an alliance between two kingdoms WITHOUT triggering native Call to War decisions
        /// for existing wars. Instead, creates VoluntaryCallToWarDecision for each existing war,
        /// allowing allies to optionally join without obligation.
        /// This is the preferred method for TOR diplomacy.
        /// </summary>
        public static void SetAllianceClean(this IFaction factionA, IFaction factionB)
        {
            if (factionA is not Kingdom kingdom1 || factionB is not Kingdom kingdom2)
            {
                return;
            }

            var allianceBehavior = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (allianceBehavior == null) return;

            // Collect existing wars BEFORE creating alliance
            var kingdom1Wars = GetCurrentEnemies(kingdom1);
            var kingdom2Wars = GetCurrentEnemies(kingdom2);

            // Start the alliance (this will create native Call to War decisions)
            allianceBehavior.StartAlliance(kingdom1, kingdom2);

            // Remove native Call to War decisions
            RemoveCallToWarDecisions(kingdom1);
            RemoveCallToWarDecisions(kingdom2);

            // Create voluntary call to war decisions for existing wars
            CreateVoluntaryCallToWarDecisions(kingdom1, kingdom2, kingdom2Wars);
            CreateVoluntaryCallToWarDecisions(kingdom2, kingdom1, kingdom1Wars);
        }

        /// <summary>
        /// Gets a list of enemy kingdom IDs for a kingdom.
        /// </summary>
        private static List<Kingdom> GetCurrentEnemies(Kingdom kingdom)
        {
            return Kingdom.All
                .Where(k => k != kingdom && kingdom.IsAtWarWith(k))
                .ToList();
        }

        /// <summary>
        /// Removes all pending Call to War decisions from a kingdom.
        /// These are created by native StartAlliance() and we want to replace them with our own.
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
        /// Creates VoluntaryCallToWarDecision for each existing war that the ally was in.
        /// </summary>
        private static void CreateVoluntaryCallToWarDecisions(Kingdom kingdom, Kingdom requestingAlly, List<Kingdom> existingEnemies)
        {
            if (kingdom?.RulingClan == null) return;

            foreach (var enemy in existingEnemies)
            {
                // Skip if already at war with this enemy
                if (kingdom.IsAtWarWith(enemy)) continue;

                // Skip Chaos - those wars are eternal and automatic
                if (enemy.Culture?.StringId == TOR_Core.Utilities.TORConstants.Cultures.CHAOS) continue;

                // Create voluntary decision
                var decision = new VoluntaryCallToWarDecision(kingdom.RulingClan, requestingAlly, enemy);

                if (decision.IsAllowed())
                {
                    // For player kingdom, add as decision popup
                    // For AI, resolve immediately
                    if (kingdom == Clan.PlayerClan?.Kingdom)
                    {
                        kingdom.AddDecision(decision, false); // Not enforced - player can ignore
                    }
                    else
                    {
                        ResolveVoluntaryDecisionForAI(kingdom, decision);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves VoluntaryCallToWarDecision for AI kingdoms.
        /// </summary>
        private static void ResolveVoluntaryDecisionForAI(Kingdom kingdom, VoluntaryCallToWarDecision decision)
        {
            float joinSupport = 0f;
            float declineSupport = 0f;

            foreach (var clan in kingdom.Clans)
            {
                if (clan.IsUnderMercenaryService) continue;

                float clanSupport = decision.CalculateJoinWarSupportPublic(clan);
                float weight = 1f + clan.Tier * 0.2f;
                if (clan == kingdom.RulingClan) weight *= 2f;

                if (clanSupport > 0)
                    joinSupport += clanSupport * weight;
                else
                    declineSupport += -clanSupport * weight;
            }

            // AI is less eager to join existing wars - requires clear advantage
            bool shouldJoin = joinSupport > declineSupport * 1.2f;

            if (shouldJoin)
            {
                var allianceWarBehavior = Campaign.Current?.GetCampaignBehavior<TORAllianceWarBehavior>();
                allianceWarBehavior?.MarkAsAllianceWar(kingdom, decision.Enemy);
                TaleWorlds.CampaignSystem.Actions.DeclareWarAction.ApplyByKingdomDecision(kingdom, decision.Enemy);
            }
        }
    }
}