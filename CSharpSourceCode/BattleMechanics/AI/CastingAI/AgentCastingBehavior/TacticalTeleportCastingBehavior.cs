using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    /// <summary>
    /// AI behavior for tactical repositioning spells like DaHandUvGork.
    /// Evaluates teleport opportunities based on:
    /// - Number of nearby allies that would be teleported
    /// - Tactical value of destination position
    /// - Proximity to enemy formations for engagement
    /// </summary>
    public class TacticalTeleportCastingBehavior : AbstractAgentCastingBehavior
    {
        private const float TELEPORT_RADIUS = 8f; // Must match TeleportTriggeredScript.TELEPORT_RADIUS
        private const int MIN_MELEE_ALLIES = 3; // Minimum melee allies nearby to consider teleporting
        private const float MIN_POWER_RATIO = 0.5f; // Minimum power ratio vs target formation (0.5 = even fight)
        private const float MIN_POWER_RATIO_LORD = 0.7f; // Lords require safer odds (0.7 = 70% power advantage)
        private const float MAX_TELEPORT_DISTANCE = 80f; // Maximum distance to teleport
        private const float FLANK_OFFSET_DISTANCE = 5f; // Distance to offset from formation center when flanking (reduced for aggressive positioning)
        private const float MIN_DISTANCE_FROM_FORMATION = 3f; // Minimum safe distance from enemy formation edge (spawn almost into them)

        public TacticalTeleportCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
            : base(agent, template, abilityIndex)
        {
            Hysteresis = 0.15f;
        }

        public override List<BehaviorOption> CalculateUtility()
        {
            var behaviorOptions = new List<BehaviorOption>();

            // Check basic ability availability
            var ability = Agent.GetAbility(AbilityIndex);
            if (ability.IsOnCooldown() || !ability.CanCast(Agent, out _))
            {
                return behaviorOptions;
            }

            // Get nearby allies that would be teleported
            var nearbyAllies = Mission.Current.GetNearbyAllyAgents(
                Agent.Position.AsVec2,
                TELEPORT_RADIUS,
                Agent.Team,
                new MBList<Agent>());
            nearbyAllies.Remove(Agent);

            // Analyze ally composition - count MELEE allies specifically (not ranged, not mounted)
            var meleeAllies = nearbyAllies.Where(a =>
                a != null &&
                a.IsActive() &&
                !a.HasMount &&
                !a.IsRangedCached // Use cached ranged check for performance
            ).ToList();

            // Don't teleport if too few melee allies
            if (meleeAllies.Count < MIN_MELEE_ALLIES)
            {
                return behaviorOptions;
            }

            // Calculate total power of allies we'd be teleporting (including the caster)
            var teleportingPower = CalculateAllyGroupPower(meleeAllies) + (Agent.Character?.GetPower() ?? 20f);

            // Find potential teleport destinations (enemy formations)
            // COMPLETELY EXCLUDE mounted formations (cavalry immunity)
            var enemyFormations = Agent.Team.GetEnemyTeams()
                .SelectMany(team => team.GetFormations())
                .Where(f => f.CountOfUnitsWithoutDetachedOnes > 0)
                .Where(f => f.QuerySystem.CavalryUnitRatio < 0.5f) // Skip formations with 50%+ cavalry
                .ToList();

            // Lords are more cautious - require safer odds
            var isLord = Agent.IsHero;
            var requiredPowerRatio = isLord ? MIN_POWER_RATIO_LORD : MIN_POWER_RATIO;

            foreach (var enemyFormation in enemyFormations)
            {
                // Check if we have enough power to engage this formation
                // Regular units: 0.5 = even fight
                // Lords: 0.7 = safe bet (70% power advantage)
                var enemyPower = enemyFormation.QuerySystem.FormationPower;
                var powerRatio = teleportingPower / MathF.Max(enemyPower, 1f);

                // Skip if we don't meet minimum power requirement
                if (powerRatio < requiredPowerRatio)
                {
                    continue;
                }

                var target = CreateTeleportTarget(enemyFormation, meleeAllies, powerRatio);
                if (target != null)
                {
                    behaviorOptions.Add(new BehaviorOption
                    {
                        Target = target,
                        Behavior = this,
                        UtilityValue = target.UtilityValue
                    });
                }
            }

            return behaviorOptions;
        }

        private float CalculateAllyGroupPower(List<Agent> allies)
        {
            float totalPower = 0f;
            foreach (var ally in allies)
            {
                if (ally?.Character != null)
                {
                    totalPower += ally.Character.GetPower();
                }
            }
            return totalPower;
        }

        private Target CreateTeleportTarget(Formation enemyFormation, List<Agent> meleeAllies, float powerRatio)
        {
            var formationCenter = enemyFormation.GetAveragePositionOfUnits(true, false).ToVec3();
            var formationDirection = enemyFormation.QuerySystem.EstimatedDirection;

            // Check distance to formation - must be within teleport range
            var distanceToFormation = Agent.Position.Distance(formationCenter);
            if (distanceToFormation > MAX_TELEPORT_DISTANCE)
            {
                return null;
            }

            // Calculate perpendicular vector for flanking (90 degrees from formation direction)
            var rightVector = new Vec2(-formationDirection.y, formationDirection.x);

            // Calculate formation half-width for positioning
            var formationHalfWidth = enemyFormation.Width / 2f;

            // Try multiple flanking positions: rear-left, rear-right, rear-center
            Vec3? bestPosition = null;
            float bestScore = 0f;

            var candidateOffsets = new[]
            {
                // Behind and to the right
                new Vec2(-formationDirection.x * FLANK_OFFSET_DISTANCE + rightVector.x * (formationHalfWidth + MIN_DISTANCE_FROM_FORMATION),
                         -formationDirection.y * FLANK_OFFSET_DISTANCE + rightVector.y * (formationHalfWidth + MIN_DISTANCE_FROM_FORMATION)),
                // Behind and to the left
                new Vec2(-formationDirection.x * FLANK_OFFSET_DISTANCE - rightVector.x * (formationHalfWidth + MIN_DISTANCE_FROM_FORMATION),
                         -formationDirection.y * FLANK_OFFSET_DISTANCE - rightVector.y * (formationHalfWidth + MIN_DISTANCE_FROM_FORMATION)),
                // Directly behind
                new Vec2(-formationDirection.x * (FLANK_OFFSET_DISTANCE + MIN_DISTANCE_FROM_FORMATION),
                         -formationDirection.y * (FLANK_OFFSET_DISTANCE + MIN_DISTANCE_FROM_FORMATION))
            };

            foreach (var offset in candidateOffsets)
            {
                var candidatePos2D = formationCenter.AsVec2 + offset;

                // Get valid ground height
                float groundHeight = 0f;
                Mission.Current.Scene.GetHeightAtPoint(candidatePos2D,
                    BodyFlags.CommonCollisionExcludeFlagsForCombat,
                    ref groundHeight);

                var candidatePos3D = new Vec3(candidatePos2D.x, candidatePos2D.y, groundHeight);

                // Validate position: not too far, has valid ground, good tactical value
                if (IsValidTeleportPosition(candidatePos3D, formationCenter))
                {
                    float score = ScoreFlankingPosition(candidatePos3D, formationCenter, formationDirection);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPosition = candidatePos3D;
                    }
                }
            }

            if (bestPosition == null)
            {
                return null;
            }

            // Calculate utility score
            var utility = CalculateTeleportUtility(bestPosition.Value, enemyFormation, meleeAllies.Count, powerRatio);

            return new Target
            {
                Formation = enemyFormation,
                SelectedWorldPosition = bestPosition.Value,
                UtilityValue = utility
            };
        }

        private bool IsValidTeleportPosition(Vec3 position, Vec3 formationCenter)
        {
            // Check if position is too far from agent
            if (Agent.Position.Distance(position) > MAX_TELEPORT_DISTANCE)
            {
                return false;
            }

            // Check if height difference is reasonable (not a cliff)
      

            return true;
        }

        private float ScoreFlankingPosition(Vec3 position, Vec3 formationCenter, Vec2 formationDirection)
        {
            float score = 1.0f;

            // Prefer positions that are actually behind the enemy
            var toPosition = (position.AsVec2 - formationCenter.AsVec2).Normalized();
            var dotProduct = Vec2.DotProduct(toPosition, -formationDirection);
            score *= MathF.Max(0.5f, dotProduct); // Penalty if not behind

            return score;
        }

        private float CalculateTeleportUtility(Vec3 targetPosition, Formation enemyFormation, int meleeAllyCount, float powerRatio)
        {
            float utility = 0f;

            // Factor 1: Power ratio (higher is better, we already met minimum)
            // Normalize: 0.5 -> 0.2, 1.0 -> 0.3, 2.0 -> 0.4
            var powerFactor = MathF.Clamp((powerRatio - 0.5f) / 1.5f * 0.2f + 0.2f, 0.2f, 0.4f);
            utility += powerFactor;

            // Factor 2: Number of melee allies to teleport (more = better, up to a point)
            var allyFactor = MathF.Min(meleeAllyCount / 10f, 0.25f);
            utility += allyFactor;

            // Factor 3: STRONGLY prefer ranged formations (easier targets for melee)
            var rangedFactor = enemyFormation.QuerySystem.RangedUnitRatio * 0.4f; // Boosted from 0.3 to 0.4
            utility += rangedFactor;

            // Factor 4: Formation state considerations
            // Boost utility for formations that are:
            // - Already engaged (easier to disrupt)
            // - Under missile fire (already weakened)
            // - Retreating/moving (easier to flank)
            var formationStateBonus = 0f;

            // Check if formation is under pressure
            if (enemyFormation.QuerySystem.UnderRangedAttackRatio > 0.3f)
            {
                formationStateBonus += 0.15f; // Formation is being shot at
            }

            // Check if formation is moving (easier to flank)
            if (enemyFormation.QuerySystem.MovementSpeedMaximum > 0.5f)
            {
                formationStateBonus += 0.1f; // Formation is moving
            }

            // Check if formation is already in melee
            if (enemyFormation.QuerySystem.MakingRangedAttackRatio < 0.5f &&
                enemyFormation.QuerySystem.InfantryUnitRatio > 0.5f)
            {
                formationStateBonus += 0.1f; // Infantry formation in melee
            }

            utility += formationStateBonus;

            // Factor 5: Teleport distance (prefer meaningful repositioning, not too close)
            var teleportDistance = Agent.Position.Distance(targetPosition);
            var distanceFactor = MathF.Clamp(teleportDistance / 60f, 0.05f, 0.15f);
            utility += distanceFactor;

            return MathF.Min(utility, 1.0f);
        }

        protected override Target UpdateTarget(Target target)
        {
            // Recalculate position if needed (formation may have moved)
            if (target.Formation != null)
            {
                // Get current melee allies
                var nearbyAllies = Mission.Current.GetNearbyAllyAgents(
                    Agent.Position.AsVec2, TELEPORT_RADIUS, Agent.Team, new MBList<Agent>());
                nearbyAllies.Remove(Agent);

                var meleeAllies = nearbyAllies.Where(a =>
                    a != null && a.IsActive() && !a.HasMount && !a.IsRangedCached
                ).ToList();

                if (meleeAllies.Count >= MIN_MELEE_ALLIES)
                {
                    var teleportingPower = CalculateAllyGroupPower(meleeAllies) + (Agent.Character?.GetPower() ?? 20f);
                    var enemyPower = target.Formation.QuerySystem.FormationPower;
                    var powerRatio = teleportingPower / MathF.Max(enemyPower, 1f);

                    // Lords require safer odds
                    var isLord = Agent.IsHero;
                    var requiredPowerRatio = isLord ? MIN_POWER_RATIO_LORD : MIN_POWER_RATIO;

                    if (powerRatio >= requiredPowerRatio)
                    {
                        var newTarget = CreateTeleportTarget(target.Formation, meleeAllies, powerRatio);
                        if (newTarget != null)
                        {
                            target.SelectedWorldPosition = newTarget.SelectedWorldPosition;
                            target.UtilityValue = newTarget.UtilityValue;
                        }
                    }
                }
            }
            return target;
        }

        protected override bool HaveLineOfSightToTarget(Target target)
        {
            if (target.Formation == null)
            {
                return false;
            }

            // Must be able to see the enemy formation to teleport near it
            var formationCenter = target.Formation.GetAveragePositionOfUnits(true, false).ToVec3();
            formationCenter.z += 1.5f; // Check at chest height

            var agentEyePosition = Agent.Position + new Vec3(z: Agent.GetEyeGlobalHeight());

            // Raycast to formation center to check line of sight
            float distance;

            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Mission.Current.Scene.RayCastForClosestEntityOrTerrain(
                    agentEyePosition,
                    formationCenter,
                    out distance,
                    out _,  // closestPoint - not needed for LOS check
                    out _,  // collisionNormal - not needed
                    0.3f);
            }

            // Calculate expected distance
            var expectedDistance = agentEyePosition.Distance(formationCenter);

            // If raycast hit something much closer than the formation, we don't have LOS
            // Allow some tolerance for formations being partially visible
            return float.IsNaN(distance) || MathF.Abs(distance - expectedDistance) < 5f;
        }
    }
}
