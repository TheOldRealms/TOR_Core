using System;
using System.Collections.Generic;
using System.Timers;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using static TaleWorlds.MountAndBlade.SkinVoiceManager;

namespace TOR_Core.BattleMechanics.Morale
{
    public class BattleShoutsMissionLogic : MissionLogic
    {
        private OrderController _playerOrderController;
        //in milliseconds
        public const int SelectionResponseDelay = 900;
        public const int OrderRepeatDelay = 900;
        public const int OrderResponseDelay = 900;
        private readonly List<ResponseEvent> _orderResponseEvents = [];
        private readonly List<ResponseEvent> _orderResponseEventsToRemove = [];
        private int _ignoreFirstFewEventsNum = -1;
        private int _ignoreEventCurrentCount = 0;
        private bool _battleEnded;

        public override void OnTeamDeployed(Team team)
        {
            if (team.IsPlayerTeam && team.IsPlayerGeneral)
            {
                _ignoreFirstFewEventsNum = 1;
                _playerOrderController = team.PlayerOrderController;
                _playerOrderController.OnOrderIssued += OnOrderIssued;
                _playerOrderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;
            }
        }

        public override void OnBattleEnded()
        {
            if(_playerOrderController != null)
            {
                _playerOrderController.OnOrderIssued -= OnOrderIssued;
                _playerOrderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
            }
            _battleEnded = true;
            foreach(var responseEvent in _orderResponseEvents)
            {
                responseEvent.CancelEarly();
            }
        }

        public override void OnMissionTick(float dt)
        {
            foreach(var item in _orderResponseEventsToRemove)
            {
                _orderResponseEvents.Remove(item);
            }
            _orderResponseEventsToRemove.Clear();
        }

        private void OnSelectedFormationsChanged()
        {
            if (_battleEnded) return;
            if(_ignoreEventCurrentCount > _ignoreFirstFewEventsNum)
            {
                foreach (var formation in _playerOrderController.SelectedFormations)
                {
                    var responseEvent = new ResponseEvent(OnResponseEventComplete)
                    {
                        RespondingFormation = formation,
                        ResponseEventType = ResponseEventType.Selection,
                        RepeaterAgent = null,
                        OrderVoiceType = VoiceType.HorseStop,
                        OrderType = OrderType.None
                    };
                    if (_orderResponseEvents.AnyQ(x => x.Equals(responseEvent))) responseEvent.CancelEarly();
                    else _orderResponseEvents.Add(responseEvent);
                }
            }
            else
            {
                _ignoreEventCurrentCount++;
            }
        }

        private void OnResponseEventComplete(ResponseEvent responseEvent)
        {
            _orderResponseEventsToRemove.Add(responseEvent);
        }

        private void OnOrderIssued(OrderType orderType, TaleWorlds.Library.MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
        {
            if (_battleEnded) return;
            if (_ignoreEventCurrentCount > _ignoreFirstFewEventsNum)
            {
                foreach (var formation in appliedFormations)
                {
                    var responseEvent = new ResponseEvent(OnResponseEventComplete)
                    {
                        RespondingFormation = formation,
                        ResponseEventType = ResponseEventType.Order,
                        RepeaterAgent = formation.Captain ?? formation.GetMedianAgent(true, true, formation.SmoothedAverageUnitPosition),
                        OrderVoiceType = GetVoiceType(orderType),
                        OrderType = orderType
                    };
                    if (_orderResponseEvents.AnyQ(x => x.Equals(responseEvent))) responseEvent.CancelEarly();
                    else
                    {
                        _orderResponseEvents.Add(responseEvent);
                        foreach (var item in _orderResponseEvents.WhereQ(x => x.ResponseEventType == ResponseEventType.Selection && x.RespondingFormation == responseEvent.RespondingFormation))
                        {
                            item.CancelEarly();
                        }
                    }
                }
            }
            else
            {
                _ignoreEventCurrentCount++;
            }
        }

        private SkinVoiceType GetVoiceType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.None:
                    return VoiceType.Idle;
                case OrderType.Move:
                    return VoiceType.Move;
                case OrderType.MoveToLineSegment:
                    return VoiceType.Move;
                case OrderType.MoveToLineSegmentWithHorizontalLayout:
                    return VoiceType.Move;
                case OrderType.Charge:
                    return VoiceType.Charge;
                case OrderType.ChargeWithTarget:
                    return VoiceType.Charge;
                case OrderType.StandYourGround:
                    return VoiceType.Focus;
                case OrderType.FollowMe:
                    return VoiceType.Follow;
                case OrderType.FollowEntity:
                    return VoiceType.Follow;
                case OrderType.GuardMe:
                    return VoiceType.Follow;
                case OrderType.Retreat:
                    return VoiceType.Retreat;
                case OrderType.AdvanceTenPaces:
                    return VoiceType.Advance;
                case OrderType.FallBackTenPaces:
                    return VoiceType.FallBack;
                case OrderType.Advance:
                    return VoiceType.Advance;
                case OrderType.FallBack:
                    return VoiceType.FallBack;
                case OrderType.LookAtEnemy:
                    return VoiceType.FaceEnemy;
                case OrderType.LookAtDirection:
                    return VoiceType.FaceDirection;
                case OrderType.ArrangementLine:
                    return VoiceType.FormLine;
                case OrderType.ArrangementCloseOrder:
                    return VoiceType.FormShieldWall;
                case OrderType.ArrangementLoose:
                    return VoiceType.FormLoose;
                case OrderType.ArrangementCircular:
                    return VoiceType.FormCircle;
                case OrderType.ArrangementSchiltron:
                    return VoiceType.FormSkein;
                case OrderType.ArrangementVee:
                    return VoiceType.FormSkein;
                case OrderType.ArrangementColumn:
                    return VoiceType.FormColumn;
                case OrderType.ArrangementScatter:
                    return VoiceType.FormScatter;
                case OrderType.FormCustom:
                    return VoiceType.Move;
                case OrderType.FormDeep:
                    return VoiceType.Move;
                case OrderType.FormWide:
                    return VoiceType.Move;
                case OrderType.FormWider:
                    return VoiceType.Move;
                case OrderType.CohesionHigh:
                    return VoiceType.Move;
                case OrderType.CohesionMedium:
                    return VoiceType.Move;
                case OrderType.CohesionLow:
                    return VoiceType.Move;
                case OrderType.HoldFire:
                    return VoiceType.HoldFire;
                case OrderType.FireAtWill:
                    return VoiceType.FireAtWill;
                case OrderType.RideFree:
                    return VoiceType.Charge;
                case OrderType.Mount:
                    return VoiceType.Mount;
                case OrderType.Dismount:
                    return VoiceType.Dismount;
                case OrderType.AIControlOn:
                    return VoiceType.Follow;
                case OrderType.AIControlOff:
                    return VoiceType.Stop;
                case OrderType.Transfer:
                    return VoiceType.Move;
                case OrderType.Use:
                    return VoiceType.Advance;
                case OrderType.AttackEntity:
                    return VoiceType.AttackGate;
                case OrderType.PointDefence:
                    return VoiceType.FormLine;
                case OrderType.Count:
                    return VoiceType.Idle;
                default:
                    return VoiceType.Idle;
            }
        }

        private enum ResponseEventType
        {
            Selection,
            Order
        }

        private class ResponseEvent : IEquatable<ResponseEvent>
        {
            internal Agent RepeaterAgent;
            internal Formation RespondingFormation;
            internal ResponseEventType ResponseEventType;
            internal SkinVoiceType OrderVoiceType;
            internal OrderType OrderType;
            private Timer _timer;
            private bool _selectionResponseTriggered;
            private bool _orderRepeatTriggered;
            private bool _orderResponseTriggered;
            private Action<ResponseEvent> _onFinishedCallback;

            public ResponseEvent(Action<ResponseEvent> onFinishedCallback)
            {
                _onFinishedCallback = onFinishedCallback;
                _timer = ResponseEventType == ResponseEventType.Selection ? new(SelectionResponseDelay) : new(OrderRepeatDelay);
                _timer.AutoReset = false;
                _timer.Elapsed += OnElapsed;
                _timer.Start();
            }

            internal void CancelEarly()
            {
                _timer.Stop();
                _onFinishedCallback?.Invoke(this);
            }

            private void OnElapsed(object sender, ElapsedEventArgs e)
            {
                lock (this)
                {
                    if(ResponseEventType == ResponseEventType.Selection)
                    {
                        if (!_selectionResponseTriggered)
                        {
                            _selectionResponseTriggered = true;
                            RespondingFormation.ApplyActionOnEachAttachedUnit(agent =>
                            {
                                if (agent != null && agent.IsActive() && agent.IsHuman && !agent.IsUndead() && agent.CurrentlyUsedGameObject == null)
                                {
                                    agent.MakeVoice(OrderVoiceType, CombatVoiceNetworkPredictionType.NoPrediction);
                                }
                            });
                            _timer.Stop();
                            _onFinishedCallback?.Invoke(this);
                        }
                    }
                    else if(ResponseEventType == ResponseEventType.Order) 
                    { 
                        if (!_orderRepeatTriggered)
                        {
                            _orderRepeatTriggered = true;
                            if(RepeaterAgent != null && RepeaterAgent.IsActive() && RepeaterAgent.IsAIControlled && RepeaterAgent.IsHuman && !RepeaterAgent.IsUndead())
                            {
                                RepeaterAgent.MakeVoice(OrderVoiceType, CombatVoiceNetworkPredictionType.NoPrediction);
                            }
                            _timer.Stop();
                            _timer.Interval = OrderResponseDelay;
                            _timer.Start();
                        }
                        else if(!_orderResponseTriggered)
                        {
                            _orderResponseTriggered = true;
                            RespondingFormation.ApplyActionOnEachAttachedUnit(agent =>
                            {
                                if (agent != null && agent.IsActive() && agent.IsHuman && !agent.IsUndead() && agent.CurrentlyUsedGameObject == null)
                                {
                                    agent.MakeVoice((OrderType == OrderType.Charge || OrderType == OrderType.ChargeWithTarget) ? VoiceType.Yell : VoiceType.HorseStop, CombatVoiceNetworkPredictionType.NoPrediction);
                                }
                            });
                            _timer.Stop();
                            _onFinishedCallback?.Invoke(this);
                        }
                    }
                    
                }
            }

            public override bool Equals(object obj)
            {
                if(obj is ResponseEvent e) { return Equals(e); } else { return false; }
            }

            public bool Equals(ResponseEvent other)
            {
                return ResponseEventType == other.ResponseEventType &&
                    RespondingFormation == other.RespondingFormation &&
                    OrderType == other.OrderType;
            }

            public override int GetHashCode()
            {
                return ResponseEventType.GetHashCode() + RespondingFormation.GetHashCode() + OrderType.GetHashCode();
            }

            public static bool operator ==(ResponseEvent x, ResponseEvent y)
            {
                return x.Equals(y);
            }

            public static bool operator !=(ResponseEvent x, ResponseEvent y)
            {
                return !(x == y);
            }
        }
    }
}
