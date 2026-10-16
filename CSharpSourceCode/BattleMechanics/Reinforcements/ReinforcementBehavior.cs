using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    public class ReinforcementBehavior : CampaignBehaviorBase
    {
        private Dictionary<MapEvent, ReinforcementBattleState> _battleStates = new();

        public sealed class ReinforcementBattleState
        {
            [SaveableField(0)] public bool SnapshotTaken;
            [SaveableField(1)] public bool BlockPlayerReinforcementsAgainstBandits;
            [SaveableField(2)] public List<ReinforcementCandidateState> Candidates = new();
            [SaveableField(3)] public bool DefenderFieldReliefStarted;
            [SaveableField(4)] public bool AttackerFieldReliefStarted;
        }

        public sealed class ReinforcementCandidateState
        {
            [SaveableField(0)] public MobileParty Party;
            [SaveableField(1)] public BattleSideEnum Side = BattleSideEnum.None;
            [SaveableField(2)] public float RemainingArrivalTime;
            [SaveableField(3)] public bool Resolved;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_reinforcementBattleStates", ref _battleStates);
        }

        public ReinforcementBattleState GetOrCreateBattleState(MapEvent mapEvent)
        {
            if (!_battleStates.TryGetValue(mapEvent, out var state))
            {
                state = new ReinforcementBattleState();
                _battleStates.Add(mapEvent, state);
            }

            return state;
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            _battleStates.Remove(mapEvent);
        }
    }
}
