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
        // Track which wars were joined due to alliance obligations
        // Structure: kingdom StringId -> (enemy kingdom StringId -> ally kingdom StringId that caused us to join)
        private Dictionary<string, Dictionary<string, string>> _allianceWars = new();

        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
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

            if (_allianceWars.TryGetValue(kingdom.StringId, out var enemyToAllyMap))
            {
                return enemyToAllyMap.ContainsKey(enemy.StringId);
            }
            return false;
        }

        /// <summary>
        /// Gets which ally caused us to join this war. Returns null if not an alliance war.
        /// </summary>
        public Kingdom GetAllianceWarAlly(Kingdom kingdom, Kingdom enemy)
        {
            if (kingdom == null || enemy == null) return null;

            if (_allianceWars.TryGetValue(kingdom.StringId, out var enemyToAllyMap))
            {
                if (enemyToAllyMap.TryGetValue(enemy.StringId, out var allyId))
                {
                    return Kingdom.All.FirstOrDefault(k => k.StringId == allyId);
                }
            }
            return null;
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
                if (_allianceWars.TryGetValue(kingdom.StringId, out var enemyToAllyMap) &&
                    enemyToAllyMap.ContainsKey(enemy.StringId))
                {
                    continue;
                }

                offensiveWars++;
            }

            return offensiveWars;
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (!faction1.IsKingdomFaction || !faction2.IsKingdomFaction) return;

            var attacker = (Kingdom)faction1;
            var defender = (Kingdom)faction2;

            // Get all allies of the defender - they must decide to join or break alliance
            var defenderAllies = defender.AlliedKingdoms.ToList();

            foreach (var ally in defenderAllies)
            {
                if (ally == attacker) continue; // Shouldn't happen, but safety check
                if (ally.IsAtWarWith(attacker)) continue; // Already at war

                // Create an internal kingdom decision for the ally
                // Total War style: must join or break alliance
                var decision = new HonorAllianceDecision(ally.RulingClan, defender, attacker);

                // For AI kingdoms, resolve immediately
                // For player kingdom, add as enforced decision requiring player choice
                if (ally == Clan.PlayerClan?.Kingdom)
                {
                    // Player gets to choose - add as enforced decision
                    decision.IsEnforced = true;
                    ally.AddDecision(decision, true); // true = ignoreInfluenceCost
                }
                else
                {
                    // AI resolves immediately based on scoring
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
                        MarkAsAllianceWar(ally, defender, attacker);
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
                MarkAsAllianceWar(kingdom, decision.Attacker, decision.AttackedAlly);
                DeclareWarAction.ApplyByKingdomDecision(kingdom, decision.Attacker);
            }
            else
            {
                var allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
                allianceBehavior?.EndAlliance(kingdom, decision.AttackedAlly);
            }
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
            
            var allyReligion = ally.Leader?.GetDominantReligion();
            var targetReligion = target.Leader?.GetDominantReligion();

            if (allyReligion != null && targetReligion != null)
            {
                if (allyReligion.HostileReligions?.Contains(targetReligion) == true)
                {
                    return true; 
                }
            }
            
            return MBRandom.RandomFloat < 0.4f;
        }

        /// <summary>
        /// Marks a war as an alliance war (defensive) so it doesn't count toward offensive war limits.
        /// Tracks which ally caused us to join this war.
        /// </summary>
        public void MarkAsAllianceWar(Kingdom kingdom, Kingdom enemy, Kingdom allyWeJoinedFor)
        {
            if (kingdom == null || enemy == null || allyWeJoinedFor == null) return;

            if (!_allianceWars.ContainsKey(kingdom.StringId))
            {
                _allianceWars[kingdom.StringId] = new Dictionary<string, string>();
            }

            // Only set if not already tracked (don't overwrite existing reason)
            if (!_allianceWars[kingdom.StringId].ContainsKey(enemy.StringId))
            {
                _allianceWars[kingdom.StringId][enemy.StringId] = allyWeJoinedFor.StringId;
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
            if (_allianceWars.TryGetValue(kingdom.StringId, out var enemyToAllyMap))
            {
                enemyToAllyMap.Remove(enemy.StringId);
            }
        }

        /// <summary>
        /// Daily check to orphan or re-parent alliance wars when alliances change.
        /// </summary>
        private void OnDailyTick()
        {
            CleanupAllianceWarTracking();
        }

        /// <summary>
        /// Reviews all tracked alliance wars and updates their status.
        /// Wars are orphaned if no ally is fighting the enemy, or re-parented to a new ally.
        /// </summary>
        private void CleanupAllianceWarTracking()
        {
            var warsToOrphan = new List<(string kingdomId, string enemyId)>();
            var warsToReparent = new List<(string kingdomId, string enemyId, string newAllyId)>();

            foreach (var kvp in _allianceWars)
            {
                var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == kvp.Key);
                if (kingdom == null || kingdom.IsEliminated) continue;

                var (orphans, reparents) = EvaluateKingdomAllianceWars(kingdom, kvp.Value);
                warsToOrphan.AddRange(orphans);
                warsToReparent.AddRange(reparents);
            }

            ApplyOrphanChanges(warsToOrphan);
            ApplyReparentChanges(warsToReparent);
        }

        /// <summary>
        /// Evaluates all alliance wars for a single kingdom.
        /// Returns lists of wars to orphan and wars to re-parent.
        /// </summary>
        private (List<(string kingdomId, string enemyId)> orphans, List<(string kingdomId, string enemyId, string newAllyId)> reparents)
            EvaluateKingdomAllianceWars(Kingdom kingdom, Dictionary<string, string> enemyToAllyMap)
        {
            var orphans = new List<(string kingdomId, string enemyId)>();
            var reparents = new List<(string kingdomId, string enemyId, string newAllyId)>();

            foreach (var enemyAllyPair in enemyToAllyMap)
            {
                var enemy = Kingdom.All.FirstOrDefault(k => k.StringId == enemyAllyPair.Key);
                var trackedAlly = Kingdom.All.FirstOrDefault(k => k.StringId == enemyAllyPair.Value);

                if (ShouldRemoveTracking(kingdom, enemy))
                {
                    orphans.Add((kingdom.StringId, enemyAllyPair.Key));
                    continue;
                }

                if (AllyStillQualifies(kingdom, enemy, trackedAlly))
                    continue;

                var newParentAlly = FindAllyFightingEnemy(kingdom, enemy);
                if (newParentAlly != null)
                    reparents.Add((kingdom.StringId, enemyAllyPair.Key, newParentAlly.StringId));
                else
                    orphans.Add((kingdom.StringId, enemyAllyPair.Key));
            }

            return (orphans, reparents);
        }

        /// <summary>
        /// Checks if tracking should be removed entirely (enemy gone or war ended).
        /// </summary>
        private bool ShouldRemoveTracking(Kingdom kingdom, Kingdom enemy)
        {
            if (enemy == null || enemy.IsEliminated)
                return true;

            if (!kingdom.IsAtWarWith(enemy))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the tracked ally still qualifies (allied with us AND at war with enemy).
        /// </summary>
        private bool AllyStillQualifies(Kingdom kingdom, Kingdom enemy, Kingdom trackedAlly)
        {
            return trackedAlly != null &&
                   !trackedAlly.IsEliminated &&
                   kingdom.IsAllyWith(trackedAlly) &&
                   trackedAlly.IsAtWarWith(enemy);
        }

        /// <summary>
        /// Finds an ally that is currently fighting the specified enemy.
        /// </summary>
        private Kingdom FindAllyFightingEnemy(Kingdom kingdom, Kingdom enemy)
        {
            foreach (var potentialAlly in kingdom.AlliedKingdoms)
            {
                if (potentialAlly.IsAtWarWith(enemy))
                    return potentialAlly;
            }
            return null;
        }

        private void ApplyOrphanChanges(List<(string kingdomId, string enemyId)> warsToOrphan)
        {
            foreach (var (kingdomId, enemyId) in warsToOrphan)
            {
                if (_allianceWars.TryGetValue(kingdomId, out var enemyToAllyMap))
                    enemyToAllyMap.Remove(enemyId);
            }
        }

        private void ApplyReparentChanges(List<(string kingdomId, string enemyId, string newAllyId)> warsToReparent)
        {
            foreach (var (kingdomId, enemyId, newAllyId) in warsToReparent)
            {
                if (_allianceWars.TryGetValue(kingdomId, out var enemyToAllyMap))
                    enemyToAllyMap[enemyId] = newAllyId;
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
            ConstructContainerDefinition(typeof(Dictionary<string, Dictionary<string, string>>));
            ConstructContainerDefinition(typeof(Dictionary<string, string>));
        }
    }
}
