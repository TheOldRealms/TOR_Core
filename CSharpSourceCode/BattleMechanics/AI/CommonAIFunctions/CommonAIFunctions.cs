using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.CommonAIFunctions
{
    public static class CommonAIDecisionFunctions
    {
        public static Func<Target, float> WindsOfMagicRemainingRatio(Agent behaviorAgent)
        {
            var heroExtendedInfo = behaviorAgent.GetHero()?.GetExtendedInfo();
            return target =>
            {
                if (heroExtendedInfo == null) return 1.0f;
                return heroExtendedInfo.GetCustomResourceValue("WindsOfMagic") / heroExtendedInfo.MaxWindsOfMagic;
            };
        }

        public static Func<Target, float> FormationUnderFire()
        {
            return target => { return target.Formation.QuerySystem.UnderRangedAttackRatio; };
        }

        public static Func<Target, float> FormationCasualties()
        {
            return target => target.Formation.QuerySystem.CasualtyRatio;
        }

        public static Func<Target, float> TargetDistanceToHostiles(Team team = null)
        {
            return target =>
            {
                if (team != null)
                {
                    var distance = target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AverageEnemyPosition);
                    return distance;
                }

                if (target.Formation != null)
                {
                    var querySystemClosestEnemyFormation = target.Formation.CachedClosestEnemyFormation;
                    if (querySystemClosestEnemyFormation == null || querySystemClosestEnemyFormation.Formation == null)
                    {
                        return float.MaxValue;
                    }

                    var targetPoint = target.GetPositionPrioritizeCalculated();
                    if (targetPoint == Vec3.Invalid)
                        return float.MaxValue;

                    return targetPoint.AsVec2.Distance(querySystemClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
                }

                return 0f;
            };
        }

        public static Func<Target, float> TargetDistanceToOwnArmy(Team team = null)
        {
            return target =>
            {
                if (team != null)
                {
                    var distance = target.TacticalPosition.Position.AsVec2.Distance(team.QuerySystem.AveragePosition);
                    return distance;
                }

                return 0f;
            };
        }

        public static Func<Target, float> DistanceToTarget(Func<Vec3> provider)
        {
            return target =>
            {
                var targetPosition = target.GetPosition();
                if (targetPosition == Vec3.Invalid)
                    return float.MaxValue;

                return provider.Invoke().Distance(targetPosition);
            };
        }

        public static Func<Target, float> FormationPower()
        {
            return target => target.Formation.QuerySystem.FormationPower;
        }

        public static Func<Target, float> RangedUnitRatio()
        {
            return target => target.Formation.QuerySystem.RangedUnitRatio;
        }

        public static Func<Target, float> InfantryUnitRatio()
        {
            return target => target.Formation.QuerySystem.InfantryUnitRatio;
        }

        public static Func<Target, float> CavalryUnitRatio()
        {
            return target => target.Formation.QuerySystem.CavalryUnitRatio;
        }

        public static Func<Target, float> Dispersedness()
        {
            return target => target.Formation.UnitSpacing;
        }

        public static Func<Target, float> TargetSpeed()
        {
            return target => target.Formation.CachedCurrentVelocity.Length;
        }

        public static Func<Target, float> BalanceOfPower(Agent agent)
        {
            return target => agent.Team.QuerySystem.TeamPower / (CalculateEnemyTotalPower(agent.Team) + agent.Team.QuerySystem.TeamPower);
        }

        public static Func<Target, float> LocalBalanceOfPower(Agent agent)
        {
            return target => Math.Max(1, agent.Formation.QuerySystem.LocalPowerRatio);
        }

        public static float CalculateEnemyTotalPower(Team chosenTeam)
        {
            float power = 0;
            foreach (var team in Mission.Current.GetEnemyTeamsOf(chosenTeam))
            {
                power += team.QuerySystem.TeamPower;
            }

            return power;
        }

        public static float CalculateTeamTotalPower(Team chosenTeam)
        {
            return chosenTeam.QuerySystem.TeamPower;
        }

        public static Func<Target, float> AssessPositionForArtillery()
        {
            return target =>
            {
                var value = 0.2f;
                if (target.TacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround)
                    value += 0.6f;
                if (target.TacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Cliff)
                    value += 0.6f;
                if (target.TacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
                    value += 0.6f;

                if (target.TacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Opening)
                    value += 0.2f;
                if (target.TacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Forest)
                    value -= 0.1f;
                if (target.TacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.DifficultTerrain)
                    value -= 0.05f;

                return value;
            };
        }


        public static Func<Target, float> PositionHeight()
        {
            return target =>
            {
                return target.TacticalPosition.Position.GetGroundZ();
            };
        }

        public static Func<Target, float> UnitCount()
        {
            return target => target.Formation?.CountOfUnits ?? 1;
        }

        public static Func<Target, float> TargetDistanceToPosition(TacticalPosition position)
        {
            return target => position == null ? 1.0f : target.TacticalPosition.Position.AsVec2.Distance(position.Position.AsVec2);
        }
    }

    public static class CommonAIStateFunctions
    {
        public static bool CanAgentMoveFreely(Agent agent)
        {
            var movementOrder = agent?.Formation?.GetReadonlyMovementOrderReference();
            return movementOrder.HasValue && (movementOrder.Value.OrderType == OrderType.Charge || movementOrder.Value.OrderType == OrderType.ChargeWithTarget || agent?.Formation?.AI?.ActiveBehavior?.GetType().Name.Contains("Skirmish") == true);
        }
    }

    public static class CommonAIFunctions
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Finds the closest agent around a formation's average unit position randomised within the formation's depth and width.
        /// </summary>
        /// <remarks>
        /// Makes use of excludeDetachedUnits = true.
        /// An equivalent of CountOfUnitsWithoutDetachedOnes is used for true, and CountOfUnits for false.
        /// </remarks>
        public static Agent GetRandomAgent(Formation targetFormation)
        {
            var medianAgent = targetFormation?.GetMedianAgent(true, false, targetFormation.GetAveragePositionOfUnits(true, false));
            //Sly : how does the value for GetAveragePositionOfUnits compare with using Formation.CurrentPosition which can make use of the cached position recalculated every 0.1 secs?
            //if the goal here is to find a non-null agent that's near the "middle" of the formation, could this first median agent calculation be skipped and instead formation.CountOfUnitsWithoutDetachedOnes is evaluated to have at least 1 agent in it, then the adjustments are based on formation values before finding the median agent only at the return?
            //for stationary formations, the difference is <1m, I think that's an effect of the width and the number of agents in the rank which leads to a formation position that's in between 2 agents. Because CurrentPosition is recached every 0.1s, it's probably sufficiently close in approximation for any infantry formation; I'm unsure about how fast a cavalry formation can run and what the maximum potential error could be.

            if (medianAgent == null) return null;

            var adjustedPosition = medianAgent.Position;

            var direction = targetFormation.QuerySystem.EstimatedDirection;
            var rightVec = direction.RightVec();

            adjustedPosition += direction.ToVec3() * (float)(_random.NextDouble() * targetFormation.Depth - targetFormation.Depth / 2);
            var widthToTarget = targetFormation.Width * 0.90f;
            adjustedPosition += rightVec.ToVec3() * (float)(_random.NextDouble() * widthToTarget - widthToTarget / 2);

            return targetFormation.GetMedianAgent(true, false, adjustedPosition.AsVec2);
        }

        public static bool HasLineOfSight(Vec3 from, Vec3 to, float atLeast = 70)
        {
            float segmentLength = from.Distance(to);

            float distanceToFirstHit;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Mission.Current.Scene.RayCastForClosestEntityOrTerrain(from, to, out distanceToFirstHit, out _, out _);
            }

            // raycast returns nan when it doesnt hit anything
            if (float.IsNaN(distanceToFirstHit))
                return true;

            const float hitTolerance = 0.3f;
            float minimumClearDistance = Math.Min(atLeast, Math.Max(0f, segmentLength - hitTolerance));

            bool hitAtTargetDistance = Math.Abs(distanceToFirstHit - segmentLength) <= hitTolerance;
            return hitAtTargetDistance && distanceToFirstHit >= minimumClearDistance;
        }
    }
}