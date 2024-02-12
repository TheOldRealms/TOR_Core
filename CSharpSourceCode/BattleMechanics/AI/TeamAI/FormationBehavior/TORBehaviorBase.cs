using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    public struct TORFormationQueryData
    {
        public bool IsFormationFiring;
        public bool IsFormationFightingInMelee;
        public bool IsFormationBeingChargedByCav;
        public bool IsFormationFiredAtBySmallArms;
        public bool IsFormationFiredAtByArtillery;
        public bool IsFormationDispersed;
        public Formation TargetFormation;
    }

    public enum BehaviorType
    {
        CombatBehavior,
        MovementBehavior
    }

    public abstract class TORBehaviorBase : BehaviorComponent
    {
        protected ArrangementOrder CurrentArrangementOrder = ArrangementOrder.ArrangementOrderLine;
        protected FiringOrder CurrentFiringOrder = FiringOrder.FiringOrderFireAtWill;
        protected FormOrder CurrentFormOrder = FormOrder.FormOrderWide;
        protected TORFormationQueryData FormationQueryData = default;
        protected WorldPosition ReformPosition = WorldPosition.Invalid;
        protected readonly BehaviorType BehaviorType;

        protected TORBehaviorBase(Formation formation,  float coherence, BehaviorType behaviorType) : base(formation)
        {
            BehaviorCoherence = coherence;
            CalculateCurrentOrder();
            BehaviorType = behaviorType;
        }

        protected sealed override void CalculateCurrentOrder()
        {
            CalculateFormationQueryData();
            CalculateMovementOrder();
            CalculateFacingOrder();
            CalculateArrangementOrder();
            CalculateFiringOrder();
            CalculateFormOrder();
        }

        private void CalculateFormationQueryData()
        {
            FormationQueryData.IsFormationFightingInMelee = Formation.IsFightingInMelee(0.3f);
            FormationQueryData.IsFormationBeingChargedByCav = Formation.IsFormationBeingChargedByCavalry();
            FormationQueryData.IsFormationDispersed = Formation.IsFormationDispersed(out ReformPosition, GetModifierForDispersedness());
            if (Formation.TargetFormation?.CountOfUnits > 0) FormationQueryData.TargetFormation = Formation.TargetFormation;
            else if (Formation.QuerySystem.ClosestEnemyFormation?.Formation?.CountOfUnits > 0) FormationQueryData.TargetFormation = Formation.QuerySystem.ClosestEnemyFormation.Formation;
            else FormationQueryData.TargetFormation = Formation.Team.QuerySystem.MedianTargetFormation?.Formation;
        }

        protected abstract void CalculateMovementOrder();

        protected abstract void CalculateFacingOrder();

        protected abstract void CalculateArrangementOrder();

        protected abstract void CalculateFiringOrder();

        protected abstract void CalculateFormOrder();

        protected sealed override void OnBehaviorActivatedAux() => SetOrders();

        public sealed override void TickOccasionally() => SetOrders();

        private void SetOrders()
        {
            if(Formation.AI.ActiveBehavior == this)
            {
                CalculateCurrentOrder();
                Formation.SetMovementOrder(CurrentOrder);
                Formation.FacingOrder = CurrentFacingOrder;
                Formation.ArrangementOrder = CurrentArrangementOrder;
                Formation.FiringOrder = CurrentFiringOrder;
                Formation.FormOrder = CurrentFormOrder;
                OnAfterOrdersSet();
            }
        }

        protected virtual void OnAfterOrdersSet() { }

        protected override float GetAiWeight()
        {
            if (BehaviorType == BehaviorType.CombatBehavior && FormationQueryData.TargetFormation == null) return 0;
            return 1f;
        }

        protected virtual float GetModifierForDispersedness()
        {
            switch (Formation.PhysicalClass)
            {
                case FormationClass.Infantry:
                case FormationClass.Ranged:
                case FormationClass.Cavalry:
                case FormationClass.HorseArcher:
                default:
                    return 1f;
            }
        }
    }
}
