using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;
using TOR_Core.BattleMechanics.Artillery;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.ArtilleryAI
{
    public class FieldSiegeWeaponAI : UsableMachineAIBase
    {
        private readonly BaseFieldSiegeWeapon _fieldSiegeWeapon;
        private Target _target;
        private List<Axis> targetDecisionFunctions;

        public FieldSiegeWeaponAI(BaseFieldSiegeWeapon usableMachine) : base(usableMachine)
        {
            _fieldSiegeWeapon = usableMachine;
            targetDecisionFunctions = CreateTargetingFunctions();
        }

        public override bool HasActionCompleted => base.HasActionCompleted;

        protected override void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
        {
            base.OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
            if (_fieldSiegeWeapon.PilotAgent != null && _fieldSiegeWeapon.PilotAgent.IsAIControlled)
            {
                if (_fieldSiegeWeapon.State == RangedSiegeWeapon.WeaponState.Idle)
                {
                    if (_target != null)
                    {
                        if (_fieldSiegeWeapon.Target != _target)
                        {
                            _fieldSiegeWeapon.SetTarget(_target);
                        }

                        if (_fieldSiegeWeapon.Target != null && _fieldSiegeWeapon.PilotAgent.Formation.FiringOrder.OrderType != OrderType.HoldFire)
                        {
                            var position = GetAdjustedTargetPosition(_fieldSiegeWeapon.Target);
                            if (position != Vec3.Zero && _fieldSiegeWeapon.AimAtThreat(_fieldSiegeWeapon.Target) && _fieldSiegeWeapon.IsTargetInRange(position) && _fieldSiegeWeapon.IsSafeToFire())
                            {
                                _fieldSiegeWeapon.AiRequestsShoot();
                                _target = null;
                            }

                            if (!_fieldSiegeWeapon.IsSafeToFire()) //Since safe to fi
                            {
                                _target = null;
                            }
                        }
                    }
                    else
                    {
                        _fieldSiegeWeapon.ClearTarget();
                        _target = FindNewTarget();
                    }
                }
            }
        }

        private Vec3 GetAdjustedTargetPosition(Target target)
        {
            if (target?.Formation == null) return Vec3.Zero;

            var targetAgent = target.SelectedWorldPosition == Vec3.Zero ? CommonAIFunctions.CommonAIFunctions.GetRandomAgent(target.Formation) : target.Agent;

            if (targetAgent == null) return Vec3.Zero;
            target.Agent = targetAgent;

            Vec3 velocity = target.Formation.QuerySystem.CurrentVelocity.ToVec3();
            float time = _fieldSiegeWeapon.GetEstimatedCurrentFlightTime();

            target.SelectedWorldPosition = target.Position + velocity * time;
            return target.SelectedWorldPosition;
        }

        private Target FindNewTarget()
        {
            var findNewTarget = GetAllThreats()
                .FindAll(target => target.Formation == null || target.Formation.GetCountOfUnitsWithCondition(x => x.IsActive()) > 0)
                .ToList();
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
                if (targetFormation.UtilityValue != -1f && _fieldSiegeWeapon.IsTargetInRange(targetFormation.GetPosition()))
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
            return from f in (from t in Mission.Current.Teams where t.Side.GetOppositeSide() == _fieldSiegeWeapon.Side select t)
                    .SelectMany((Team t) => t.GetFormationsIncludingSpecial())
                where f.CountOfUnits > 0
                select f;
        }

        private List<Axis> CreateTargetingFunctions()
        {
            var targetingFunctions = new List<Axis>();
            targetingFunctions.Add(new Axis(0, 300, x => 0.7f - 3 * (float) Math.Pow(x - 0.3f, 3) + (float) Math.Pow(x, 2), CommonAIDecisionFunctions.DistanceToTarget(() => _fieldSiegeWeapon.GameEntity.GlobalPosition))); // 0.7 - 3(x-0.3)^3 + x^2
            targetingFunctions.Add(new Axis(0, CommonAIDecisionFunctions.CalculateEnemyTotalPower(_fieldSiegeWeapon.Team), x => x, CommonAIDecisionFunctions.FormationPower()));
            targetingFunctions.Add(new Axis(0, 70, x => x, CommonAIDecisionFunctions.UnitCount()));
            targetingFunctions.Add(new Axis(0, 10, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles()));
            return targetingFunctions;
        }

    }
}