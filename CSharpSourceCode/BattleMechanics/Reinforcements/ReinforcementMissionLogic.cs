using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    public class ReinforcementMissionLogic : MissionLogic
    {
        private static readonly FieldInfo BattleSideSpawnContextsField = typeof(DefaultBattleMissionAgentSpawnLogic).GetField("_battleSideSpawnContexts", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NumberSpawnedTroopsField = typeof(MissionBattleSideSpawnContext).GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AllocatedTroopsField = typeof(MapEventSide).GetField("_allocatedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo MapEventTypeField = typeof(MapEvent).GetField("_mapEventType", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Queue<PendingReinforcementTroop>[] _pendingTroops = { new(), new() };
        private readonly Queue<List<PendingReinforcementTroop>>[] _delayedReinforcementGroups = { new(), new() };
        private readonly List<Vec3>[] _reinforcementSpawnPositions = { new(), new() };
        private readonly int[] _nextReinforcementSpawnPosition = new int[2];
        private readonly bool[] _pendingTroopsKeepSideAlive = new bool[2];
        private readonly Dictionary<Agent, BattleSideEnum> _spawnedReinforcements = new();

        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;
        private MissionBattleSideSpawnContext[] _spawnContexts;
        private MapEvent _battle;
        private ReinforcementBehavior.ReinforcementBattleState _state;
        private bool _initialized;
        private int _nextTroopRank;
        private BattleSideEnum _nextSpawnSide = BattleSideEnum.Defender;

        private sealed class ReinforcementArrivalGroup
        {
            public IReadOnlyList<MapEventParty> JoinedParties { get; }
            public bool ReinforcesPlayer { get; }
            public bool Notified { get; set; }

            public ReinforcementArrivalGroup(IReadOnlyList<MapEventParty> joinedParties, bool reinforcesPlayer)
            {
                JoinedParties = joinedParties;
                ReinforcesPlayer = reinforcesPlayer;
            }
        }

        private readonly struct PendingReinforcementTroop
        {
            public IAgentOriginBase Origin { get; }
            public Vec3 SpawnPosition { get; }
            public ReinforcementArrivalGroup ArrivalGroup { get; }

            public PendingReinforcementTroop(IAgentOriginBase origin, Vec3 spawnPosition, ReinforcementArrivalGroup arrivalGroup = null)
            {
                Origin = origin;
                SpawnPosition = spawnPosition;
                ArrivalGroup = arrivalGroup;
            }
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);

            if (_initialized || oldMissionMode != MissionMode.Deployment || Mission.Mode != MissionMode.Battle)
                return;

            InitializeReinforcements();
        }

        private void InitializeReinforcements()
        {
            _initialized = true;
            _battle = MapEvent.PlayerMapEvent;

            if (_battle == null)
                return;

            if (!_battle.IsFieldBattle && !_battle.IsSiegeAssault)
                return;

            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            _spawnContexts = (MissionBattleSideSpawnContext[])BattleSideSpawnContextsField.GetValue(_spawnLogic);

            if (_battle.IsSiegeAssault)
            {
                CacheReinforcementSpawnPositions(BattleSideEnum.Attacker);
            }
            else
            {
                CacheReinforcementSpawnPositions(BattleSideEnum.Defender);
                CacheReinforcementSpawnPositions(BattleSideEnum.Attacker);
            }

            var behavior = Campaign.Current.GetCampaignBehavior<ReinforcementBehavior>();
            _state = behavior.GetOrCreateBattleState(_battle);
            if (!_state.SnapshotTaken)
                TakeReinforcementSnapshot();
        }

        private void TakeReinforcementSnapshot()
        {
            var playerSide = _battle.PlayerSide;
            var defenderLeaderParty = _battle.DefenderSide.LeaderParty?.MobileParty;
            if (playerSide == BattleSideEnum.Attacker && defenderLeaderParty?.IsBandit == true)
            {
                var banditStrength = defenderLeaderParty.Party.GetCustomStrength(BattleSideEnum.Defender, _battle.SimulationContext);

                // if there is a notable strength difference between bandits and the player, block reinforcements from joining the player side to avoid trivializing the fight
                _state.BlockPlayerReinforcementsAgainstBandits = _battle.StrengthOfSide[(int)playerSide] >= banditStrength * 1.75f;
            }

            var spottingRange = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRange(MobileParty.MainParty).ResultNumber;

            // Keep enemy reach just inside the actual spotting boundary for rounding.
            var enemyRadius = spottingRange * 0.95f;

            // ally distance is slightly larger than enemy spotting range to allow for some leeway in the arrival decision
            var allyRadius = enemyRadius * 1.2f;
            var searchData = MobileParty.StartFindingLocatablesAroundPosition(_battle.Position.ToVec2(), allyRadius);

            for (var party = MobileParty.FindNextLocatable(ref searchData); party != null; party = MobileParty.FindNextLocatable(ref searchData))
            {
                if (!CanBeReinforcementCandidate(party))
                    continue;

                var side = GetJoinableSide(party);
                if (side == BattleSideEnum.None)
                    continue;

                if (_battle.IsSiegeAssault && side != BattleSideEnum.Defender)
                    continue;

                var radius = side == playerSide ? allyRadius : enemyRadius;
                var distance = party.Position.Distance(_battle.Position);
                if (distance > radius)
                    continue;

                if (side != playerSide && (!party.IsVisible || party.Position.Distance(MobileParty.MainParty.Position) > enemyRadius))
                    continue;

                var arrivalFactor = 1f;
                if (_battle.IsSiegeAssault)
                {
                    // Outside siege relief is faster
                    arrivalFactor = party.Army?.LeaderParty == party ? 0.7f : 0.8f;
                }
                else if (party.Army?.LeaderParty == party)
                {
                    arrivalFactor = 0.8f; // coordinated army relief arrives faster
                }

                _state.Candidates.Add(new ReinforcementBehavior.ReinforcementCandidateState
                {
                    Party = party,
                    Side = side,
                    RemainingArrivalTime = CalculateArrivalDelay(distance, party.Speed, arrivalFactor)
                });
            }

            _state.Candidates.Sort((left, right) =>
            {
                var delayComparison = right.RemainingArrivalTime.CompareTo(left.RemainingArrivalTime);
                return delayComparison != 0
                    ? delayComparison
                    : string.CompareOrdinal(right.Party.StringId, left.Party.StringId);
            });
            _state.SnapshotTaken = true;
        }

        private bool CanBeReinforcementCandidate(MobileParty party)
        {
            if (party.MapEvent != null)
                return false;

            if (party.IsGarrison || party.CurrentSettlement != null || party.BesiegerCamp != null)
                return false;

            if (party.Army != null && party.Army.LeaderParty != party && party.Army.DoesLeaderPartyAndAttachedPartiesContain(party))
                return false;

            if (party.IsCustomParty || party.IsCurrentlyUsedByAQuest)
                return false;

            if (party.IsVillager || party.IsBandit)
                return false;

            if (_battle.IsSiegeAssault && party.IsMilitia)
                return false;

            // only armed caravans can reinforce a party
            if (party.IsCaravan && !party.CaravanPartyComponent.IsElite)
                return false;

            return true;
        }

        private BattleSideEnum GetJoinableSide(MobileParty party)
        {
            var canJoinPlayerSide = _battle.CanPartyJoinBattle(party.Party, _battle.PlayerSide);
            var enemySide = _battle.PlayerSide.GetOppositeSide();
            var canJoinEnemySide = _battle.CanPartyJoinBattle(party.Party, enemySide);

            if (canJoinPlayerSide == canJoinEnemySide)
                return BattleSideEnum.None;

            return canJoinPlayerSide ? _battle.PlayerSide : enemySide;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            ReinforcementArrivalNotifier.TickArrivalNotification(dt);

            if (_spawnLogic == null)
                return;

            for (var i = _state.Candidates.Count - 1; i >= 0; i--)
            {
                var candidate = _state.Candidates[i];
                if (candidate.Resolved)
                    continue;

                candidate.RemainingArrivalTime -= dt;
                if (candidate.RemainingArrivalTime > 0f)
                    continue;

                // retreat/re entry cannot reset the timer or reroll the decision for that mapevent
                candidate.Resolved = true;
                if (CanStillJoin(candidate) && ShouldJoinBattle(candidate.Party, candidate.Side))
                    RegisterReinforcingParty(candidate.Party, candidate.Side);
            }

            ReleaseDelayedReinforcements(BattleSideEnum.Defender);
            ReleaseDelayedReinforcements(BattleSideEnum.Attacker);
            if (SpawnNextTroop(_nextSpawnSide))
                _nextSpawnSide = _nextSpawnSide.GetOppositeSide();
            else
                SpawnNextTroop(_nextSpawnSide.GetOppositeSide());
        }

        public bool HasPendingReinforcements(BattleSideEnum side)
        {
            return _state.Candidates.Any(x => !x.Resolved && x.Side == side && CanStillJoin(x)) ||
                   _delayedReinforcementGroups[(int)side].Count > 0 ||
                   _pendingTroops[(int)side].Count > 0;
        }

        private bool CanStillJoin(ReinforcementBehavior.ReinforcementCandidateState candidate)
        {
            var party = candidate.Party;
            if (!party.IsActive || party.MapEvent != null || party.CurrentSettlement != null || party.BesiegerCamp != null)
                return false;

            return _battle.CanPartyJoinBattle(party.Party, candidate.Side);
        }

        private bool ShouldJoinBattle(MobileParty party, BattleSideEnum side)
        {
            if (_state.BlockPlayerReinforcementsAgainstBandits && side == _battle.PlayerSide)
                return false;

            if (party.IsPatrolParty)
                return true;

            if (party.IsCaravan)
                return ShouldArmedCaravanJoin(party, side);

            var decisionHero = party.LeaderHero ?? party.Owner;
            if (decisionHero == null)
                return MBRandom.RandomFloat < 0.5f;

            var reinforcedParty = _battle.GetLeaderParty(side);
            var reinforcedHero = reinforcedParty.LeaderHero ?? reinforcedParty.Owner;
            var relation = reinforcedHero != null ? decisionHero.GetRelation(reinforcedHero) : 0;

            // +/-50 the relationship is strong enough to make the commitment deterministic
            if (relation >= 50)
                return true;
            if (relation <= -50)
                return false;

            // valor answers bad odds
            // calculating prefers safe encounters
            // honor reinforces faction loyalty. not integrated fully yet 
            var valor = decisionHero.GetTraitLevel(DefaultTraits.Valor);
            var calculating = decisionHero.GetTraitLevel(DefaultTraits.Calculating);
            var honor = decisionHero.GetTraitLevel(DefaultTraits.Honor);
            GetBattlePowerAfterJoining(party, side, out var joiningSidePower, out var opposingSidePower);
            var joiningOutmatched = joiningSidePower < opposingSidePower;

            if (valor >= 2 && joiningSidePower < opposingSidePower * 0.75f)
                return true;
            if (valor <= -2 && joiningOutmatched)
                return false;
            if (calculating >= 2 && joiningSidePower > opposingSidePower * 1.25f)
                return true;

            var chance = 0.5f + 0.4f * relation / 50f;
            if (joiningOutmatched)
            {
                chance += valor * 0.08f;
                chance -= calculating * 0.05f;
            }
            else
            {
                chance += calculating * 0.07f;
                chance += valor * 0.02f;
            }

            if (party.MapFaction == reinforcedParty.MapFaction)
                chance += honor * 0.05f;

            return MBRandom.RandomFloat < MathF.Clamp(chance, 0.1f, 0.9f);
        }

        private bool ShouldArmedCaravanJoin(MobileParty party, BattleSideEnum side)
        {
            var owner = party.CaravanPartyComponent.Owner;
            var relation = owner.GetRelation(Hero.MainHero);
            var signedRelation = side == _battle.PlayerSide ? relation : -relation;

            if (signedRelation >= 40)
                return true;
            if (signedRelation <= -40)
                return false;

            var chance = MathF.Clamp(0.5f + 0.4f * signedRelation / 40f, 0.1f, 0.9f);
            return MBRandom.RandomFloat < chance;
        }

        private void GetBattlePowerAfterJoining(MobileParty party, BattleSideEnum side, out float joiningSidePower, out float opposingSidePower)
        {
            joiningSidePower = _battle.GetMapEventSide(side).Parties.Sum(x => x.Party.GetCustomStrength(side, _battle.SimulationContext));
            opposingSidePower = _battle.GetMapEventSide(side.GetOppositeSide()).Parties.Sum(x => x.Party.GetCustomStrength(side.GetOppositeSide(), _battle.SimulationContext));

            joiningSidePower += party.Army?.LeaderParty == party
                ? party.Army.GetCustomStrength(side, _battle.SimulationContext)
                : party.Party.GetCustomStrength(side, _battle.SimulationContext);

        }

        private void RegisterReinforcingParty(MobileParty party, BattleSideEnum side)
        {
            var mapEventSide = _battle.GetMapEventSide(side);
            var partiesBeforeJoin = new HashSet<PartyBase>(_battle.InvolvedParties);
            var sideStrengthBeforeJoin = _battle.StrengthOfSide[(int)side];
            var wasSiegeAssault = _battle.IsSiegeAssault;

            // EventPositionAdder required to move the party in campaign is built at party join time
            party.Position = _battle.Position;
            foreach (var attachedParty in party.AttachedParties)
                attachedParty.Position = _battle.Position;

            party.Party.MapEventSide = mapEventSide;

            var joinedParties = mapEventSide.Parties.Where(x => !partiesBeforeJoin.Contains(x.Party)).ToList();

            if (wasSiegeAssault && _battle.EventType == MapEvent.BattleTypes.SiegeOutside)
            {
                // restoring the context for SiegeOutside and defensive sieges with relief reinforcements
                MapEventTypeField.SetValue(_battle, MapEvent.BattleTypes.Siege);
                var correctAddedStrength = joinedParties.Sum(x => x.Party.GetCustomStrength(side, _battle.SimulationContext));
                _battle.StrengthOfSide[(int)side] = sideStrengthBeforeJoin + correctAddedStrength;
                _battle.AttackerSide.CalculateRenownAndInfluenceValuesOnPartyInvolved(_battle.StrengthOfSide);
                _battle.DefenderSide.CalculateRenownAndInfluenceValuesOnPartyInvolved(_battle.StrengthOfSide);
            }

            var spawnPosition = GetReinforcementSpawnPosition(side);
            var allocatedTroops = (Dictionary<UniqueTroopDescriptor, MapEventParty>)AllocatedTroopsField.GetValue(mapEventSide);
            var arrivalGroup = new ReinforcementArrivalGroup(joinedParties, side == _battle.PlayerSide);
            var reinforcementGroup = new List<PendingReinforcementTroop>();

            foreach (var joinedParty in joinedParties)
            {
                foreach (var rosterElement in joinedParty.Troops)
                {
                    if (rosterElement.State != RosterTroopState.Active)
                        continue;

                    allocatedTroops.Add(rosterElement.Descriptor, joinedParty);

                    var origin = new ReinforcementPartyAgentOrigin(mapEventSide, joinedParty.Party, rosterElement.Troop, rosterElement.Descriptor, _nextTroopRank++);
                    reinforcementGroup.Add(new PendingReinforcementTroop(origin, spawnPosition, arrivalGroup));
                }
            }

            var sideIndex = (int)side;
            var vanillaTroopsRemaining = side == BattleSideEnum.Defender
                ? _spawnLogic.NumberOfRemainingDefenderTroops
                : _spawnLogic.NumberOfRemainingAttackerTroops;

            var fieldReliefStarted = side == BattleSideEnum.Defender
                ? _state.DefenderFieldReliefStarted
                : _state.AttackerFieldReliefStarted;

            var joinCurrentWave = !_battle.IsSiegeAssault && (!fieldReliefStarted || vanillaTroopsRemaining == 0);
            if (!_battle.IsSiegeAssault)
            {
                if (side == BattleSideEnum.Defender)
                    _state.DefenderFieldReliefStarted = true;
                else
                    _state.AttackerFieldReliefStarted = true;
            }

            if (joinCurrentWave)
            {
                foreach (var troop in reinforcementGroup)
                    _pendingTroops[sideIndex].Enqueue(troop);
            }
            else
            {
                _delayedReinforcementGroups[sideIndex].Enqueue(reinforcementGroup);
            }
        }

        public void QueueReinforcementGroup(IEnumerable<IAgentOriginBase> origins, BattleSideEnum side)
        {
            var spawnPosition = GetReinforcementSpawnPosition(side);
            foreach (var origin in origins)
                _pendingTroops[(int)side].Enqueue(new PendingReinforcementTroop(origin, spawnPosition));
        }

        private void ReleaseDelayedReinforcements(BattleSideEnum side)
        {
            var delayedGroups = _delayedReinforcementGroups[(int)side];
            if (delayedGroups.Count == 0)
                return;

            var vanillaTroopsRemaining = side == BattleSideEnum.Defender
                ? _spawnLogic.NumberOfRemainingDefenderTroops
                : _spawnLogic.NumberOfRemainingAttackerTroops;

            if (vanillaTroopsRemaining != 0)
                return;

            while (delayedGroups.Count > 0)
            {
                foreach (var troop in delayedGroups.Dequeue())
                    _pendingTroops[(int)side].Enqueue(troop);
            }
        }

        private void CacheReinforcementSpawnPositions(BattleSideEnum boundarySide)
        {
            var team = boundarySide == BattleSideEnum.Attacker ? Mission.AttackerTeam : Mission.DefenderTeam;
            Mission.GetFormationSpawnFrame(team, FormationClass.Infantry, true, out var formationWorldPosition, out _, true);
            var referencePosition = formationWorldPosition.AsVec2;
            var validSpawnPositions = new List<(float Distance, Vec3 Position)>();

            foreach (var fleePosition in Mission.GetFleePositionsForSide(boundarySide))
            {
                var escapePosition = fleePosition.GetClosestPointToEscape(referencePosition);
                var escapeWorldPosition = new WorldPosition(Mission.Scene, UIntPtr.Zero, escapePosition, false);
                if (Mission.Scene.GetPathDistanceBetweenPositions(ref formationWorldPosition, ref escapeWorldPosition, 0f, out var pathDistance))
                    validSpawnPositions.Add((pathDistance, escapeWorldPosition.GetGroundVec3MT()));
            }

            var spawnPositions = _reinforcementSpawnPositions[(int)boundarySide];
            // flee point closer to the allied side is otherwise unknown to the reinforcing parties
            foreach (var position in validSpawnPositions.OrderBy(x => x.Distance).Take(3))
                spawnPositions.Add(position.Position);

            if (spawnPositions.Count == 0)
                spawnPositions.Add(Mission.GetClosestBoundaryPosition(referencePosition).ToVec3(0f));
        }

        private Vec3 GetReinforcementSpawnPosition(BattleSideEnum side)
        {
            // siege relief forces comes from the attackers side since it is an outside force breaking in
            var boundarySide = _battle.IsSiegeAssault ? BattleSideEnum.Attacker : side;
            var spawnPositions = _reinforcementSpawnPositions[(int)boundarySide];
            var nextPosition = _nextReinforcementSpawnPosition[(int)boundarySide]++;
            return spawnPositions[nextPosition % spawnPositions.Count];
        }

        private bool SpawnNextTroop(BattleSideEnum side)
        {
            var queue = _pendingTroops[(int)side];
            if (queue.Count == 0)
                return false;

            var vanillaTroopsRemaining = side == BattleSideEnum.Defender
                ? _spawnLogic.NumberOfRemainingDefenderTroops
                : _spawnLogic.NumberOfRemainingAttackerTroops;

            if (vanillaTroopsRemaining == 0 && !_pendingTroopsKeepSideAlive[(int)side])
            {
                ChangeActiveTroopCount(side, 1);
                _pendingTroopsKeepSideAlive[(int)side] = true;
            }

            // rechecking shared agent cap for an upcoming reinforcement wave and outer reinforcements
            var nextTroop = queue.Peek();
            var requiredAgentSlots = nextTroop.Origin.Troop.HasMount() ? 2 : 1;
            if (Mission.AllAgents.Count + requiredAgentSlots > DefaultBattleMissionAgentSpawnLogic.MaxNumberOfAgentsForMission)
                return false;

            var isPlayerSide = side == _battle.PlayerSide;
            var team = TaleWorlds.MountAndBlade.Mission.GetAgentTeam(nextTroop.Origin, isPlayerSide);
            var initialDirection = (Mission.GetFormationSpawnPosition(team, FormationClass.Infantry) - nextTroop.SpawnPosition.AsVec2).Normalized();

            // fow now reinforcing a defensive siege spawns with mounts. tbd
            var agent = Mission.SpawnTroop(
                nextTroop.Origin,
                isPlayerSide,
                hasFormation: true,
                spawnWithHorse: true,
                isReinforcement: true,
                formationTroopCount: 1,
                formationTroopIndex: 0,
                isAlarmed: true,
                wieldInitialWeapons: true,
                initialPosition: nextTroop.SpawnPosition,
                initialDirection: initialDirection,
                formationIndex: FormationClass.Unset);

            queue.Dequeue();
            _spawnedReinforcements.Add(agent, side);
            ChangeActiveTroopCount(side, 1);

            if (nextTroop.ArrivalGroup != null && !nextTroop.ArrivalGroup.Notified)
            {
                nextTroop.ArrivalGroup.Notified = true;
                ReinforcementArrivalNotifier.Show(nextTroop.ArrivalGroup.JoinedParties, nextTroop.ArrivalGroup.ReinforcesPlayer);
            }

            if (queue.Count == 0 && _pendingTroopsKeepSideAlive[(int)side])
            {
                ChangeActiveTroopCount(side, -1);
                _pendingTroopsKeepSideAlive[(int)side] = false;
            }

            return true;
        }

        protected override void OnEndMission()
        {
            ReinforcementArrivalNotifier.ResetArrivalNotification();
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnEarlyAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (_spawnedReinforcements.TryGetValue(affectedAgent, out var reinforcementSide))
            {
                _spawnedReinforcements.Remove(affectedAgent);
                ChangeActiveTroopCount(reinforcementSide, -1);
            }
        }

        private void ChangeActiveTroopCount(BattleSideEnum side, int amount)
        {
            var spawnContext = _spawnContexts[(int)side];
            var currentCount = (int)NumberSpawnedTroopsField.GetValue(spawnContext);
            NumberSpawnedTroopsField.SetValue(spawnContext, currentCount + amount);
        }

        private static float CalculateArrivalDelay(float distance, float speed, float arrivalFactor)
        {
            // parties already within 3 map units arrives instantly
            var travelDistance = MathF.Max(0f, distance - 3f);
            var travelDelay = travelDistance / speed * 50f; // campaign travel time -> mission seconds
            return (10f + travelDelay) * arrivalFactor;
        }
    }
}