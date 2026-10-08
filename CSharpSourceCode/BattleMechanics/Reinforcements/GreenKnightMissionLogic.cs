using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    public class GreenKnightMissionLogic : MissionLogic
    {
        private MapEvent _battle;
        private GreenKnightBehavior _behavior;
        private GreenKnightBehavior.GreenKnightBattleState _state;
        private ReinforcementMissionLogic _reinforcementLogic;
        private MissionTimer _arrivalTimer;
        private MissionTimer _cheatArrivalTimer;
        private CharacterObject _greenKnight;
        private CharacterObject _lostSon;
        private bool _queued;
        private int _nextTroopRank;

        public override void AfterStart()
        {
            base.AfterStart();

            var battle = MapEvent.PlayerMapEvent;
            if (battle == null || !battle.IsFieldBattle || battle.IsNavalMapEvent || Mission.SceneName == "TOR_chaos_portal_001_atmo_w_night")
                return;

            _battle = battle;
            _behavior = Campaign.Current.GetCampaignBehavior<GreenKnightBehavior>();
            _state = _behavior.GetOrCreateBattleState(_battle);
            _reinforcementLogic = Mission.GetMissionBehavior<ReinforcementMissionLogic>();
            _arrivalTimer = new MissionTimer(240f); // Green Knight check starts four mission minutes into the fight

            if (!_state.SnapshotTaken)
                TakeOriginalEngagementSnapshot();

            if (_state.BretonniaSide != BattleSideEnum.None)
            {
                _greenKnight = MBObjectManager.Instance.GetObject<CharacterObject>("tor_br_greenknight_mission");
                _lostSon = MBObjectManager.Instance.GetObject<CharacterObject>("tor_br_greenknight_mission");
            }
        }

        public bool ScheduleCheatArrival()
        {
            if (_battle == null || _state.BretonniaSide == BattleSideEnum.None || _queued || _state.Joined || _cheatArrivalTimer != null)
                return false;

            _cheatArrivalTimer = new MissionTimer(20f); // headroom for the player to get into position before the Green Knight arrives
            return true;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_battle == null || _queued || Mission.Mode != MissionMode.Battle || _state.BretonniaSide == BattleSideEnum.None)
                return;

            if (_cheatArrivalTimer != null)
            {
                if (!_cheatArrivalTimer.Check(false))
                    return;

                _cheatArrivalTimer = null;
                _state.Rolled = true;
                _state.RollSucceeded = true;
                _state.ForcedByCheat = true;
                _state.GreenKnightRemaining = 1;
                _state.LostSonsRemaining = 59;
                QueueVirtualForce();
                return;
            }

            if (_state.RollSucceeded)
            {
                if (!_state.Joined && !_state.ForcedByCheat && _behavior.IsOnCooldown)
                    return;

                if (_state.GreenKnightRemaining + _state.LostSonsRemaining > 0)
                    QueueVirtualForce();
                return;
            }

            if (_state.Rolled || !_state.HasNearbyBretonniaSettlement || !_arrivalTimer.Check(false) || _behavior.IsOnCooldown)
                return;

            var bretonniaSide = _battle.GetMapEventSide(_state.BretonniaSide);
            var opposingSide = _battle.GetMapEventSide(_state.BretonniaSide.GetOppositeSide());
            var bretonniaStrength = bretonniaSide.Parties.Sum(x => x.Party.GetCustomStrength(_state.BretonniaSide, _battle.SimulationContext));
            var opposingStrength = opposingSide.Parties.Sum(x => x.Party.GetCustomStrength(_state.BretonniaSide.GetOppositeSide(), _battle.SimulationContext));
            var bretonniaRemaining = bretonniaSide.RecalculateMemberCountOfSide();
            var opposingRemaining = opposingSide.RecalculateMemberCountOfSide();

            // bretonnia must be at least 1.5x down and the enemy still needs 40 troops on the field.
            if (opposingStrength < bretonniaStrength * 1.5f || opposingRemaining < 40)
                return;

            // bretonnia must not be more than 5x weaker while also being down by over 800 troops
            if (opposingStrength > bretonniaStrength * 5f && opposingRemaining - bretonniaRemaining > 800)
                return;

            var chance = 0.05f;
            if (_state.OpposingCultureId == TORConstants.Cultures.GREENSKIN ||
                _state.OpposingCultureId == TORConstants.Cultures.BEASTMEN)
                chance = 0.20f;
            else if (TORConstants.Factions.AllVampire.Contains(_state.OpposingFactionId))
                chance = 0.15f;
            else if (_state.OpposingCultureId == TORConstants.Cultures.EONIR || _state.OpposingCultureId == TORConstants.Cultures.ASRAI)
                chance = 0.10f;

            _state.Rolled = true;
            if (MBRandom.RandomFloat >= chance)
                return;

            _state.RollSucceeded = true;
            _state.GreenKnightRemaining = 1;
            _state.LostSonsRemaining = 59;
            QueueVirtualForce();
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (agent.Origin is not GreenKnightAgentOrigin origin || !origin.IsGreenKnight)
                return;


            if (_behavior.MarkGreenKnightSpawned(_battle))
            {
                var reinforcesPlayer = _state.BretonniaSide == _battle.PlayerSide;
                if (_state.ForcedByCheat)
                    ReinforcementArrivalNotifier.ShowGreenKnightCheat(_lostSon, reinforcesPlayer);
                else
                    ReinforcementArrivalNotifier.ShowGreenKnight(_lostSon, reinforcesPlayer);
            }
        }

        private void TakeOriginalEngagementSnapshot()
        {
            _state.SnapshotTaken = true;
            if (!_state.OriginalSidesCaptured)
                return;

            if (_state.BretonniaSide == BattleSideEnum.None)
                return;

            // only for battles within 30 campaign distance of a bretonnian culture settlement
            var searchData = Settlement.StartFindingLocatablesAroundPosition(_battle.Position.ToVec2(), 30f);
            for (var settlement = Settlement.FindNextLocatable(ref searchData); settlement != null; settlement = Settlement.FindNextLocatable(ref searchData))
            {
                if ((settlement.IsTown || settlement.IsCastle || settlement.IsVillage) && settlement.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    _state.HasNearbyBretonniaSettlement = true;
                    return;
                }
            }
        }

        private void QueueVirtualForce()
        {
            var sideLeader = _battle.GetLeaderParty(_state.BretonniaSide);
            var combatant = new CustomBattleCombatant(_greenKnight.Name, sideLeader.Culture, sideLeader.Banner)
            {
                Side = _state.BretonniaSide
            };
            combatant.SetGeneral(_greenKnight);

            var origins = new List<IAgentOriginBase>(_state.GreenKnightRemaining + _state.LostSonsRemaining);
            if (_state.GreenKnightRemaining > 0)
            {
                combatant.AddCharacter(_greenKnight, _state.GreenKnightRemaining);
                origins.Add(new GreenKnightAgentOrigin(_behavior, _battle, combatant, _greenKnight, true, _nextTroopRank++));
            }

            if (_state.LostSonsRemaining > 0)
            {
                combatant.AddCharacter(_lostSon, _state.LostSonsRemaining);
                for (var i = 0; i < _state.LostSonsRemaining; i++)
                    origins.Add(new GreenKnightAgentOrigin(_behavior, _battle, combatant, _lostSon, false, _nextTroopRank++));
            }

            _reinforcementLogic.QueueReinforcementGroup(origins, _state.BretonniaSide);
            _queued = true;
        }
    }
}
