using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    /// <summary>
    /// Handles Total War-style alliance behavior where allies must join wars or break the alliance.
    /// Also tracks which wars are "alliance wars" (defensive) vs "offensive wars" for war limit purposes.
    /// </summary>
    public class TORAllianceWarBehavior : CampaignBehaviorBase
    {
        // Track which wars were joined due to alliance obligations (kingdom StringId -> list of enemy kingdom StringIds)
        private Dictionary<string, List<string>> _allianceWars = new();

        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_allianceWars", ref _allianceWars);
        }

        /// <summary>
        /// Checks if a war is an alliance war (defensive) rather than an offensive war.
        /// </summary>
        public bool IsAllianceWar(Kingdom kingdom, Kingdom enemy)
        {
            if (kingdom == null || enemy == null) return false;

            if (_allianceWars.TryGetValue(kingdom.StringId, out var enemies))
            {
                return enemies.Contains(enemy.StringId);
            }
            return false;
        }

        /// <summary>
        /// Gets the number of offensive wars (excluding alliance/defensive wars and Chaos wars).
        /// Chaos wars don't count because they are forced/eternal and cannot be ended through diplomacy.
        /// </summary>
        public int GetOffensiveWarCount(Kingdom kingdom)
        {
            if (kingdom == null) return 0;

            int offensiveWars = 0;

            foreach (var enemy in Kingdom.All)
            {
                if (enemy == kingdom || !kingdom.IsAtWarWith(enemy)) continue;

                // Skip Chaos wars - they're forced and eternal, don't count towards limit
                if (enemy.Culture?.StringId == TORConstants.Cultures.CHAOS) continue;

                // Skip alliance wars
                if (_allianceWars.TryGetValue(kingdom.StringId, out var allianceEnemies) &&
                    allianceEnemies.Contains(enemy.StringId))
                {
                    continue;
                }

                offensiveWars++;
            }

            return offensiveWars;
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            // Debug output
            InformationManager.DisplayMessage(new InformationMessage(
                $"[TOR] OnWarDeclared: {faction1.Name} vs {faction2.Name}", Colors.Cyan));

            if (!faction1.IsKingdomFaction || !faction2.IsKingdomFaction)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[TOR] Skipping - not kingdom factions", Colors.Gray));
                return;
            }

            var attacker = (Kingdom)faction1;
            var defender = (Kingdom)faction2;

            // Get all allies of the defender - they must decide to join or break alliance
            var defenderAllies = defender.AlliedKingdoms.ToList();

            InformationManager.DisplayMessage(new InformationMessage(
                $"[TOR] {defender.Name} has {defenderAllies.Count} allies", Colors.Cyan));

            foreach (var ally in defenderAllies)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[TOR] Checking ally: {ally.Name}", Colors.Cyan));

                if (ally == attacker) continue; // Shouldn't happen, but safety check
                if (ally.IsAtWarWith(attacker))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[TOR] {ally.Name} already at war with {attacker.Name}", Colors.Gray));
                    continue;
                }

                // Create an internal kingdom decision for the ally
                // Total War style: must join or break alliance
                var decision = new HonorAllianceDecision(ally.RulingClan, defender, attacker);

                InformationManager.DisplayMessage(new InformationMessage(
                    $"[TOR] Decision.IsAllowed: {decision.IsAllowed()}, PlayerKingdom: {Clan.PlayerClan?.Kingdom?.Name}", Colors.Cyan));

                // For AI kingdoms, resolve immediately
                // For player kingdom, add as enforced decision requiring player choice
                if (ally == Clan.PlayerClan?.Kingdom)
                {
                    // Player gets to choose - add as enforced decision
                    decision.IsEnforced = true;
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[TOR] Dispatching decision to player kingdom {ally.Name}", Colors.Green));
                    ally.AddDecision(decision, true); // true = ignoreInfluenceCost
                }
                else
                {
                    // AI resolves immediately based on scoring
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[TOR] AI resolving for {ally.Name}", Colors.Yellow));
                    ResolveAIDecision(ally, decision);
                }
            }

            // Also handle allies of the attacker (offensive alliance call)
            // These are less obligatory - don't break alliance for refusing
            var attackerAllies = attacker.AlliedKingdoms.ToList();
            foreach (var ally in attackerAllies)
            {
                if (ally == defender) continue;
                if (ally.IsAtWarWith(defender)) continue;

                // For offensive calls, use the native Call to War system
                // or simply have AI decide without breaking alliance
                if (ally != Clan.PlayerClan?.Kingdom)
                {
                    bool willJoin = ShouldAllyJoinOffensiveWar(ally, attacker, defender);
                    if (willJoin)
                    {
                        MarkAsAllianceWar(ally, defender);
                        DeclareWarAction.ApplyByKingdomDecision(ally, defender);
                    }
                }
                // Note: Not creating decision for offensive calls - less obligatory
            }
        }

        /// <summary>
        /// Resolves the HonorAllianceDecision for AI kingdoms immediately.
        /// </summary>
        private void ResolveAIDecision(Kingdom kingdom, HonorAllianceDecision decision)
        {
            if (!decision.IsAllowed()) return;

            // Calculate total support for joining war
            float joinSupport = 0f;
            float breakSupport = 0f;
            int clanCount = 0;

            foreach (var clan in kingdom.Clans)
            {
                if (clan.IsUnderMercenaryService) continue;

                float clanSupport = decision.CalculateJoinWarSupportPublic(clan);

                // Weight by clan tier/power
                float weight = 1f + clan.Tier * 0.2f;
                if (clan == kingdom.RulingClan) weight *= 2f; // Ruler has more say

                if (clanSupport > 0)
                    joinSupport += clanSupport * weight;
                else
                    breakSupport += -clanSupport * weight;

                clanCount++;
            }

            // Decide based on total support
            bool shouldJoin = joinSupport >= breakSupport;

            // Apply the outcome
            if (shouldJoin)
            {
                MarkAsAllianceWar(kingdom, decision.Attacker);
                DeclareWarAction.ApplyByKingdomDecision(kingdom, decision.Attacker);
            }
            else
            {
                var allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
                allianceBehavior?.EndAlliance(kingdom, decision.AttackedAlly);
            }
        }

        /// <summary>
        /// Determines if an ally should join a defensive war (ally was attacked).
        /// </summary>
        private bool ShouldAllyJoinWar(Kingdom ally, Kingdom attackedAlly, Kingdom attacker)
        {
            // Player kingdom - always join for now (could add popup later)
            if (ally == Clan.PlayerClan?.Kingdom)
            {
                return true; // TODO: Could add player choice popup here
            }

            // Chaos factions don't honor alliances (shouldn't have any, but safety)
            if (ally.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                return false;
            }

            // Check religion - much more likely to join against hostile religions
            var allyReligion = ally.Leader?.GetDominantReligion();
            var attackerReligion = attacker.Leader?.GetDominantReligion();

            if (allyReligion != null && attackerReligion != null)
            {
                // Always join against religious enemies
                if (allyReligion.HostileReligions?.Contains(attackerReligion) == true)
                {
                    return true;
                }
            }

            // Check relative strength - might break alliance if massively outmatched
            float ourStrength = ally.CurrentTotalStrength + attackedAlly.CurrentTotalStrength;
            float enemyStrength = attacker.CurrentTotalStrength;

            // If enemy is 5x stronger than combined alliance, consider breaking
            if (enemyStrength > ourStrength * 5f)
            {
                // Still 30% chance to honor alliance even against overwhelming odds
                return MBRandom.RandomFloat < 0.3f;
            }

            // Default: honor the alliance
            return true;
        }

        /// <summary>
        /// Determines if an ally should join an offensive war (ally declared war).
        /// Less obligatory than defensive.
        /// </summary>
        private bool ShouldAllyJoinOffensiveWar(Kingdom ally, Kingdom attackingAlly, Kingdom target)
        {
            // Player kingdom - don't auto-join offensive wars
            if (ally == Clan.PlayerClan?.Kingdom)
            {
                return false;
            }

            // Chaos factions
            if (ally.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                return false;
            }

            // Check if target is a religious enemy
            var allyReligion = ally.Leader?.GetDominantReligion();
            var targetReligion = target.Leader?.GetDominantReligion();

            if (allyReligion != null && targetReligion != null)
            {
                if (allyReligion.HostileReligions?.Contains(targetReligion) == true)
                {
                    return true; // Gladly join war against religious enemies
                }
            }

            // Less likely to join offensive wars - 40% chance
            return MBRandom.RandomFloat < 0.4f;
        }

        /// <summary>
        /// Marks a war as an alliance war (defensive) so it doesn't count toward offensive war limits.
        /// </summary>
        public void MarkAsAllianceWar(Kingdom kingdom, Kingdom enemy)
        {
            if (!_allianceWars.ContainsKey(kingdom.StringId))
            {
                _allianceWars[kingdom.StringId] = new List<string>();
            }

            if (!_allianceWars[kingdom.StringId].Contains(enemy.StringId))
            {
                _allianceWars[kingdom.StringId].Add(enemy.StringId);
            }
        }

        private void OnPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
        {
            if (!faction1.IsKingdomFaction || !faction2.IsKingdomFaction) return;

            var kingdom1 = (Kingdom)faction1;
            var kingdom2 = (Kingdom)faction2;

            // Remove from alliance war tracking
            RemoveAllianceWarTracking(kingdom1, kingdom2);
            RemoveAllianceWarTracking(kingdom2, kingdom1);
        }

        private void RemoveAllianceWarTracking(Kingdom kingdom, Kingdom enemy)
        {
            if (_allianceWars.TryGetValue(kingdom.StringId, out var enemies))
            {
                enemies.Remove(enemy.StringId);
            }
        }
    }

    /// <summary>
    /// Type definer for save/load of alliance war tracking.
    /// </summary>
    public class TORAllianceWarBehaviorTypeDefiner : SaveableTypeDefiner
    {
        public TORAllianceWarBehaviorTypeDefiner() : base(789_123) { }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, List<string>>));
            ConstructContainerDefinition(typeof(List<string>));
        }
    }
}
