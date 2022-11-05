using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.Artillery;

namespace TOR_Core.BattleMechanics.AI.AgentBehavior.Components
{
    public class ArtilleryAI : UsableMachineAIBase
    {
        private readonly Artillery.ArtilleryRangedSiegeWeapon _artillery;
        private Target _target;
        private List<Axis> targetDecisionFunctions;

        public ArtilleryAI(Artillery.ArtilleryRangedSiegeWeapon usableMachine) : base(usableMachine)
        {
            _artillery = usableMachine;
            targetDecisionFunctions = CreateTargetingFunctions();
        }

        public override bool HasActionCompleted => base.HasActionCompleted;

        protected override void OnTick(Func<Agent, bool> isAgentManagedByThisMachineAI, Team potentialUsersTeam, float dt)
        {
            base.OnTick(isAgentManagedByThisMachineAI, potentialUsersTeam, dt);
            if (_artillery.PilotAgent != null && _artillery.PilotAgent.IsAIControlled)
            {
                if (_artillery.State == RangedSiegeWeapon.WeaponState.Idle)
                {
                    if (_target != null && _target.Formation != null && _target.Formation.GetCountOfUnitsWithCondition(x => x.IsActive()) > 0)
                    {
                        if (_artillery.Target != _target)
                        {
                            _artillery.SetTarget(_target);
                        }

                        if (_artillery.Target != null && _artillery.PilotAgent.Formation.FiringOrder.OrderType != OrderType.HoldFire)
                        {
                            var position = GetAdjustedTargetPosition(_artillery.Target);
                            if(position != Vec3.Zero && _artillery.AimAtTarget(position) && IsTargetInRange(position))
                            {
                                _artillery.AiRequestsShoot();
                                _artillery.Target.Agent = null;
                            }
                        }
                    }
                    else
                    {
                        _artillery.ClearTarget();
                        _target = FindNewTarget();
                    }
                }
            }
        }

        private Vec3 GetAdjustedTargetPosition(Target target)
        {
            if (target?.Formation == null) return Vec3.Zero;

            var targetAgent = target.SelectedWorldPosition == Vec3.Zero ? CommonAIFunctions.GetRandomAgent(target.Formation) : target.Agent;

            if (targetAgent == null) return Vec3.Zero;
            target.Agent = targetAgent;

            Vec3 velocity = target.Formation.QuerySystem.CurrentVelocity.ToVec3();
            float time = (UsableMachine as ArtilleryRangedSiegeWeapon).GetEstimatedCurrentFlightTime();

            target.SelectedWorldPosition = target.Position + velocity * time;
            return target.SelectedWorldPosition;
        }

        private bool IsTargetInRange(Vec3 position)
        {
            var startPos = _artillery.ProjectileEntityCurrentGlobalPosition;
            var diff = position - startPos;
            var maxrange = Ballistics.GetMaximumRange(_artillery.BaseMuzzleVelocity, diff.z);
            diff.z = 0;
            return diff.Length < maxrange;
        }

        private Target FindNewTarget()
        {
            var findNewTarget = GetAllThreats();
            return findNewTarget.Count > 0 ? findNewTarget.MaxBy(target => target.UtilityValue) : null;
        }

        private List<Target> GetAllThreats()
        {
            List<Target> list = new List<Target>();
            /*
            this._potentialTargetUsableMachines.RemoveAll((ITargetable ptum) => ptum is UsableMachine && ((ptum as UsableMachine).IsDestroyed || (ptum as UsableMachine).GameEntity == null));
            list.AddRange(from um in this._potentialTargetUsableMachines
                          select new Threat
                          {
                              WeaponEntity = um,
                              ThreatValue = this.Weapon.ProcessTargetValue(um.GetTargetValue(this.WeaponPositions), um.GetTargetFlags())
                          });
            */
            foreach (Formation formation in GetUnemployedEnemyFormations())
            {
                Target targetFormation = GetTargetValueOfFormation(formation);
                if (targetFormation.UtilityValue != -1f)
                {
                    list.Add(targetFormation);
                }
            }

            return list;
        }

        private Target GetTargetValueOfFormation(Formation formation)
        {
            var target = new Target {Formation = formation};
            target.UtilityValue = targetDecisionFunctions.GeometricMean(target); //ProcessTargetValue(, RangedSiegeWeaponAi.ThreatSeeker.GetTargetFlagsOfFormation());
            return target;
        }

        private IEnumerable<Formation> GetUnemployedEnemyFormations()
        {
            return from f in (from t in Mission.Current.Teams where t.Side.GetOppositeSide() == _artillery.Side select t)
                    .SelectMany((Team t) => t.FormationsIncludingSpecial)
                where f.CountOfUnits > 0
                select f;
        }

        private List<Axis> CreateTargetingFunctions()
        {
            var targetingFunctions = new List<Axis>();
            targetingFunctions.Add(new Axis(0, 300, x => 0.7f - 3 * (float) Math.Pow(x - 0.3f, 3) + (float) Math.Pow(x, 2), CommonAIDecisionFunctions.DistanceToTarget(() => _artillery.GameEntity.GlobalPosition))); // 0.7 - 3(x-0.3)^3 + x^2
            targetingFunctions.Add(new Axis(0, CommonAIDecisionFunctions.CalculateEnemyTotalPower(_artillery.Team), x => x, CommonAIDecisionFunctions.FormationPower()));
            targetingFunctions.Add(new Axis(0, 70, x => x, CommonAIDecisionFunctions.UnitCount()));
            targetingFunctions.Add(new Axis(0, 7, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles()));
            return targetingFunctions;
        }

        public float ProcessTargetValue(float baseValue, TargetFlags flags) //TODO: This is probably not necessary, we can represent it better with the axis. Normalized values are better in these scenarios.
        {
            if (flags.HasAnyFlag(TargetFlags.NotAThreat))
            {
                return -1000f;
            }

            if (flags.HasAnyFlag(TargetFlags.None))
            {
                baseValue *= 1.5f;
            }

            if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
            {
                baseValue *= 2f;
            }

            if (flags.HasAnyFlag(TargetFlags.IsStructure))
            {
                baseValue *= 1.5f;
            }

            if (flags.HasAnyFlag(TargetFlags.IsSmall))
            {
                baseValue *= 0.5f;
            }

            if (flags.HasAnyFlag(TargetFlags.IsMoving))
            {
                baseValue *= 0.8f;
            }

            if (flags.HasAnyFlag(TargetFlags.DebugThreat))
            {
                baseValue *= 10000f;
            }

            return baseValue;
        }
    }
}