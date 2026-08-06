using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    /// <summary>
    /// Behavior for slow infantry (Dwarfs) - different logic based on attack/defense:
    /// - ATTACKING: Slow steady advance toward enemies
    /// - DEFENDING: Shield wall and hold position, let ranged handle it
    /// </summary>
    public class TORBehaviorAggressiveMelee : BehaviorComponent
    {
        private WorldPosition _defensePosition;
        private bool _positionSet;
        private float _lastAdvanceTime;

        public TORBehaviorAggressiveMelee(Formation formation) : base(formation)
        {
            BehaviorCoherence = 0.8f; // Tight formation
        }

        private bool IsDefending => Formation.Team?.TeamAI?.IsDefenseApplicable ?? false;

        public override void TickOccasionally()
        {
            var closestEnemy = Formation.CachedClosestEnemyFormation?.Formation;
            if (closestEnemy == null) return;

            float distanceToEnemy = Formation.CachedAveragePosition.Distance(
                closestEnemy.CachedMedianPosition.AsVec2);

            // If enemy is very close (within 15m), always charge
            if (distanceToEnemy < 15f)
            {
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(closestEnemy);
                Formation.SetMovementOrder(CurrentOrder);
                Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                return;
            }

            // Set initial position if not set
            if (!_positionSet)
            {
                _defensePosition = Formation.CachedMedianPosition;
                _positionSet = true;
                _lastAdvanceTime = Mission.Current.CurrentTime;
            }

            if (IsDefending)
            {
                // DEFENDING: Hold position with shield wall, let thunderers handle ranged
                CurrentOrder = MovementOrder.MovementOrderMove(_defensePosition);
                Formation.SetMovementOrder(CurrentOrder);
                Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);

                if (Formation.QuerySystem.HasShield)
                    Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                else
                    Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
            }
            else
            {
                // ATTACKING: Slow steady advance toward enemy
                var directionToEnemy = (closestEnemy.CachedMedianPosition.AsVec2 -
                    Formation.CachedAveragePosition).Normalized();

                // Advance 10m every 3 seconds (slow but relentless)
                float timeSinceLastAdvance = Mission.Current.CurrentTime - _lastAdvanceTime;
                if (timeSinceLastAdvance > 3.0f && distanceToEnemy > 20f)
                {
                    _defensePosition.SetVec2(_defensePosition.AsVec2 + directionToEnemy * 10f);
                    _lastAdvanceTime = Mission.Current.CurrentTime;
                }

                CurrentOrder = MovementOrder.MovementOrderMove(_defensePosition);
                Formation.SetMovementOrder(CurrentOrder);
                Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);

                // Use line formation while advancing, shield wall when stopped
                if (distanceToEnemy > 30f)
                    Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                else if (Formation.QuerySystem.HasShield)
                    Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
            }
        }

        protected override void OnBehaviorActivatedAux()
        {
            _positionSet = false;
            _lastAdvanceTime = Mission.Current.CurrentTime;
        }

        protected override float GetAiWeight()
        {
            // Only activate for infantry formations
            if (!Formation.QuerySystem.IsInfantryFormation)
                return 0f;

            var culture = TORCultureBattleSettings.GetTeamCulture(Formation.Team);
            var personality = TORCultureBattleSettings.GetPersonality(culture);

            if (!personality.PreferStandAndFight)
                return 0f;

            // High weight for cultures that prefer standing ground
            return 2.0f;
        }
    }
}
