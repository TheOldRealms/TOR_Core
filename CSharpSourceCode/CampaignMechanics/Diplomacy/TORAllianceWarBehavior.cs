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
            if (!faction1.IsKingdomFaction || !faction2.IsKingdomFaction) return;

            var attacker = (Kingdom)faction1;
            var defender = (Kingdom)faction2;

            // Get all allies of the defender
            var defenderAllies = defender.AlliedKingdoms.ToList();

            foreach (var ally in defenderAllies)
            {
                if (ally == attacker) continue; // Shouldn't happen, but safety check
                if (ally.IsAtWarWith(attacker)) continue; // Already at war

                // Total War style: ally must join or break alliance
                bool willJoin = ShouldAllyJoinWar(ally, defender, attacker);

                if (willJoin)
                {
                    // Join the war - this is an alliance war, not offensive
                    MarkAsAllianceWar(ally, attacker);

                    // Declare war
                    DeclareWarAction.ApplyByKingdomDecision(ally, attacker);

                    // Notify player if involved
                    if (ally == Clan.PlayerClan?.Kingdom)
                    {
                        var message = new TextObject("{=TOR_Alliance_Joined}Your kingdom has joined the war against {ENEMY} to honor your alliance with {ALLY}.");
                        message.SetTextVariable("ENEMY", attacker.Name);
                        message.SetTextVariable("ALLY", defender.Name);
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Yellow));
                    }
                }
                else
                {
                    // Break the alliance
                    var allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
                    allianceBehavior?.EndAlliance(ally, defender);

                    // Notify player if involved
                    if (ally == Clan.PlayerClan?.Kingdom)
                    {
                        var message = new TextObject("{=TOR_Alliance_Broken}Your alliance with {ALLY} has ended - you refused to join the war against {ENEMY}.");
                        message.SetTextVariable("ENEMY", attacker.Name);
                        message.SetTextVariable("ALLY", defender.Name);
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Red));
                    }
                }
            }

            // Also handle allies of the attacker (they may be called to join offensive war)
            var attackerAllies = attacker.AlliedKingdoms.ToList();
            foreach (var ally in attackerAllies)
            {
                if (ally == defender) continue;
                if (ally.IsAtWarWith(defender)) continue;

                // Offensive alliance call - less obligatory, but still considered
                bool willJoin = ShouldAllyJoinOffensiveWar(ally, attacker, defender);

                if (willJoin)
                {
                    MarkAsAllianceWar(ally, defender);
                    DeclareWarAction.ApplyByKingdomDecision(ally, defender);
                }
                // Note: Not breaking alliance for refusing offensive war call
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

        private void MarkAsAllianceWar(Kingdom kingdom, Kingdom enemy)
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
