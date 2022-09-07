using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.AgentBehavior.AgentTacticalBehavior;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.Artillery;

namespace TOR_Core.BattleMechanics.AI.AgentBehavior.AgentCastingBehavior
{
    public class ArtilleryPlacementCastingBehavior : AbstractAgentCastingBehavior
    {
        private static Random _random = new Random();

        public ArtilleryPlacementCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template, abilityIndex)
        {
            Hysteresis = 0.1f;
            TacticalBehavior = new AdjacentAoETacticalBehavior(agent, Component, this);
        }

        public override void Execute()
        {
            if (CurrentTarget.TacticalPosition == null) return;

            var castingPosition = ((AdjacentAoETacticalBehavior) TacticalBehavior)?.CastingPosition;
            if (castingPosition.HasValue && Agent.Position.AsVec2.Distance(castingPosition.Value.AsVec2) > 5) return;

            base.Execute();
        }

        protected override Target UpdateTarget(Target target)
        {
            var width = target.TacticalPosition.Width / 1.5f;
            var direction = target.TacticalPosition.Position.GetGroundVec3() - Agent.Team.QuerySystem.AverageEnemyPosition.ToVec3();
            direction /= direction.Length;
            target.SelectedWorldPosition = target.TacticalPosition.Position.GetGroundVec3() + direction.AsVec2.RightVec().ToVec3() * (float) (_random.NextDouble() * width - width / 2);
            return target;
        }

        protected override bool HaveLineOfSightToTarget(Target target)
        {
            var activeEntitiesWithScriptComponentOfType = Mission.Current.GetActiveEntitiesWithScriptComponentOfType<ArtilleryRangedSiegeWeapon>();
            return !activeEntitiesWithScriptComponentOfType.Any(entity => entity.GlobalPosition.Distance(target.SelectedWorldPosition) < 3.5);
        }

        public override List<BehaviorOption> CalculateUtility()
        {
            var behaviorOptions = new List<BehaviorOption>();
            CurrentTarget.UtilityValue = 1.0f;
            behaviorOptions.Add(new BehaviorOption {Target = CurrentTarget, Behavior = this});
            return behaviorOptions;
        }
    }
}