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
        private const float MIN_ALLIES_FOR_TELEPORT = 3; // Minimum allies nearby to consider teleporting
        private const float MAX_TELEPORT_DISTANCE = 80f; // Maximum distance to teleport
        private const float FLANK_OFFSET_DISTANCE = 15f; // Distance to offset from formation center when flanking
        private const float MIN_DISTANCE_FROM_FORMATION = 8f; // Minimum safe distance from enemy formation edge

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

            // Count nearby allies that would be teleported
            var nearbyAllies = Mission.Current.GetNearbyAllyAgents(
                Agent.Position.AsVec2,
                TELEPORT_RADIUS,
                Agent.Team,
                new MBList<Agent>());
            nearbyAllies.Remove(Agent);

            // Don't teleport if too few allies nearby
            if (nearbyAllies.Count < MIN_ALLIES_FOR_TELEPORT)
            {
                return behaviorOptions;
            }

            // Find potential teleport destinations (enemy formations)
            var enemyFormations = Agent.Team.GetEnemyTeams()
                .SelectMany(team => team.GetFormations())
                .Where(f => f.CountOfUnitsWithoutDetachedOnes > 0)
                .ToList();

            foreach (var enemyFormation in enemyFormations)
            {
                var target = CreateTeleportTarget(enemyFormation, nearbyAllies.Count);
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

        private Target CreateTeleportTarget(Formation enemyFormation, int allyCount)
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
            var utility = CalculateTeleportUtility(bestPosition.Value, enemyFormation, allyCount);

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

        private float CalculateTeleportUtility(Vec3 targetPosition, Formation enemyFormation, int allyCount)
        {
            float utility = 0f;

            // Factor 1: Number of allies to teleport (more = better, up to a point)
            var allyFactor = MathF.Min(allyCount / 10f, 0.5f);
            utility += allyFactor;

            // Factor 2: Distance from current position to target (prefer meaningful repositioning)
            var teleportDistance = Agent.Position.Distance(targetPosition);
            var distanceFactor = MathF.Clamp(teleportDistance / 50f, 0.1f, 0.4f);
            utility += distanceFactor;

            // Factor 3: Enemy formation power (prefer targeting valuable formations)
            var powerFactor = MathF.Min(enemyFormation.QuerySystem.FormationPower /
                CommonAIDecisionFunctions.CalculateEnemyTotalPower(Agent.Team) * 0.5f, 0.3f);
            utility += powerFactor;

            // Factor 4: Ranged unit ratio (prefer teleporting near ranged units)
            var rangedFactor = enemyFormation.QuerySystem.RangedUnitRatio * 0.3f;
            utility += rangedFactor;

            // Factor 5: Check if we're currently in danger (boost utility if surrounded)
            var localPowerRatio = Agent.Formation?.QuerySystem.LocalPowerRatio ?? 1f;
            if (localPowerRatio < 0.5f) // We're outnumbered
            {
                utility += 0.2f; // Escape factor
            }

            return MathF.Min(utility, 1.0f);
        }

        protected override Target UpdateTarget(Target target)
        {
            // Ensure we have a valid position selected
            if (target.SelectedWorldPosition == Vec3.Zero && target.Formation != null)
            {
                var nearbyAllies = Mission.Current.GetNearbyAllyAgents(
                    Agent.Position.AsVec2, TELEPORT_RADIUS, Agent.Team, new MBList<Agent>());
                var newTarget = CreateTeleportTarget(target.Formation, nearbyAllies.Count - 1);
                if (newTarget != null)
                {
                    target.SelectedWorldPosition = newTarget.SelectedWorldPosition;
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
