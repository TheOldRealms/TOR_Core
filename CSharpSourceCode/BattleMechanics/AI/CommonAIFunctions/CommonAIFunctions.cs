using System;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
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
                    var querySystemClosestEnemyFormation = target.Formation.QuerySystem.ClosestEnemyFormation;
                    if (querySystemClosestEnemyFormation == null)
                    {
                        return float.MaxValue;
                    }

                    return target.GetPositionPrioritizeCalculated().AsVec2.Distance(querySystemClosestEnemyFormation.AveragePosition);
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
            return target => provider.Invoke().Distance(target.GetPosition());
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
            return target => target.Formation.QuerySystem.CurrentVelocity.Length;
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

        public static Agent GetRandomAgent(Formation targetFormation)
        {
            var medianAgent = targetFormation?.GetMedianAgent(true, false, targetFormation.GetAveragePositionOfUnits(true, false));

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
            float distanceE;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(from, to, out distanceE, out GameEntity entity);
            }
            return distanceE > atLeast;
        }
    }
}
