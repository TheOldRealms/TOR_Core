using System;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using static TaleWorlds.MountAndBlade.SkinVoiceManager;

namespace TOR_Core.BattleMechanics.Morale
{
    public class BattleShoutsMissionLogic : MissionLogic
    {
        private OrderController _playerOrderController;
        private bool _battleEnded;
        private bool _deploymentFinished;

        public override void OnTeamDeployed(Team team)
        {
            if (team.IsPlayerTeam && team.IsPlayerGeneral)
            {
                _playerOrderController = team.PlayerOrderController;
                _playerOrderController.OnOrderIssued += OnOrderIssued;
                //_playerOrderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;
            }
        }

        public override void OnDeploymentFinished()
        {
            _deploymentFinished = true;
        }

        public override void OnBattleEnded()
        {
            if(_playerOrderController != null)
            {
                _playerOrderController.OnOrderIssued -= OnOrderIssued;
                //_playerOrderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
            }
            _battleEnded = true;
        }

        private void OnSelectedFormationsChanged()
        {
            if (_battleEnded || !_deploymentFinished) return;
            foreach (var formation in _playerOrderController.SelectedFormations)
            {
                formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent != null && 
                    agent.IsActive() && 
                    agent.IsHuman && 
                    !agent.IsUndead() && 
                    !agent.IsTreeSpirit() && 
                    agent.CurrentlyUsedGameObject == null && 
                    agent.GetComponent<AgentVoiceComponent>() is AgentVoiceComponent component)
                    {
                        component.SetWantsToPlayVoiceWithDelay(VoiceType.HorseStop, 0.5f);
                    }
                });
            }
        }
        
        private void OnOrderIssued(OrderType orderType, TaleWorlds.Library.MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
        {
            if (_battleEnded || !_deploymentFinished) return;

            foreach (var formation in appliedFormations)
            {
                var repeaterAgent = formation.Captain ?? formation.GetMedianAgent(true, true, formation.SmoothedAverageUnitPosition);
                if (repeaterAgent != null &&
                    repeaterAgent.IsActive() &&
                    repeaterAgent.IsHuman &&
                    !repeaterAgent.IsUndead() &&
                    !repeaterAgent.IsTreeSpirit() &&
                    repeaterAgent.CurrentlyUsedGameObject == null &&
                    repeaterAgent.GetComponent<AgentVoiceComponent>() is AgentVoiceComponent component)
                {
                    component.SetWantsToPlayVoiceWithDelay(GetVoiceType(orderType), 0.5f);
                }

                formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent != null &&
                    agent != repeaterAgent &&
                    agent.IsActive() &&
                    agent.IsHuman &&
                    !agent.IsUndead() &&
                    !agent.IsTreeSpirit() &&
                    agent.CurrentlyUsedGameObject == null &&
                    agent.GetComponent<AgentVoiceComponent>() is AgentVoiceComponent component)
                    {
                        component.SetWantsToPlayVoiceWithDelay((orderType == OrderType.Charge || orderType == OrderType.ChargeWithTarget) ? VoiceType.Yell : VoiceType.HorseStop, 2f);
                    }
                });
            }
        }
        

        private SkinVoiceType GetVoiceType(OrderType orderType)
        {
            return orderType switch
            {
                OrderType.None => VoiceType.Idle,
                OrderType.Move => VoiceType.Move,
                OrderType.MoveToLineSegment => VoiceType.Move,
                OrderType.MoveToLineSegmentWithHorizontalLayout => VoiceType.Move,
                OrderType.Charge => VoiceType.Charge,
                OrderType.ChargeWithTarget => VoiceType.Charge,
                OrderType.StandYourGround => VoiceType.Focus,
                OrderType.FollowMe => VoiceType.Follow,
                OrderType.FollowEntity => VoiceType.Follow,
                OrderType.GuardMe => VoiceType.Follow,
                OrderType.Retreat => VoiceType.Retreat,
                OrderType.AdvanceTenPaces => VoiceType.Advance,
                OrderType.FallBackTenPaces => VoiceType.FallBack,
                OrderType.Advance => VoiceType.Advance,
                OrderType.FallBack => VoiceType.FallBack,
                OrderType.LookAtEnemy => VoiceType.FaceEnemy,
                OrderType.LookAtDirection => VoiceType.FaceDirection,
                OrderType.ArrangementLine => VoiceType.FormLine,
                OrderType.ArrangementCloseOrder => VoiceType.FormShieldWall,
                OrderType.ArrangementLoose => VoiceType.FormLoose,
                OrderType.ArrangementCircular => VoiceType.FormCircle,
                OrderType.ArrangementSchiltron => VoiceType.FormSkein,
                OrderType.ArrangementVee => VoiceType.FormSkein,
                OrderType.ArrangementColumn => VoiceType.FormColumn,
                OrderType.ArrangementScatter => VoiceType.FormScatter,
                OrderType.FormCustom => VoiceType.Move,
                OrderType.FormDeep => VoiceType.Move,
                OrderType.FormWide => VoiceType.Move,
                OrderType.FormWider => VoiceType.Move,
                OrderType.CohesionHigh => VoiceType.Move,
                OrderType.CohesionMedium => VoiceType.Move,
                OrderType.CohesionLow => VoiceType.Move,
                OrderType.HoldFire => VoiceType.HoldFire,
                OrderType.FireAtWill => VoiceType.FireAtWill,
                OrderType.RideFree => VoiceType.Charge,
                OrderType.Mount => VoiceType.Mount,
                OrderType.Dismount => VoiceType.Dismount,
                OrderType.AIControlOn => VoiceType.Follow,
                OrderType.AIControlOff => VoiceType.Stop,
                OrderType.Transfer => VoiceType.Move,
                OrderType.Use => VoiceType.Advance,
                OrderType.AttackEntity => VoiceType.AttackGate,
                OrderType.PointDefence => VoiceType.FormLine,
                OrderType.Count => VoiceType.Idle,
                _ => VoiceType.Idle,
            };
        }

    }
}
