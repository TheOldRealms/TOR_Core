using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    public class GreenKnightBehavior : CampaignBehaviorBase
    {
        private Dictionary<MapEvent, GreenKnightBattleState> _battleStates = new();
        private CampaignTime _cooldownUntil = CampaignTime.Zero;

        public sealed class GreenKnightBattleState
        {
            [SaveableField(0)] public bool SnapshotTaken;
            [SaveableField(1)] public BattleSideEnum BretonniaSide = BattleSideEnum.None;
            [SaveableField(2)] public string OpposingFactionId;
            [SaveableField(3)] public string OpposingCultureId;
            [SaveableField(4)] public bool HasNearbyBretonniaSettlement;
            [SaveableField(5)] public bool Rolled;
            [SaveableField(6)] public bool RollSucceeded;
            [SaveableField(7)] public bool Joined;
            [SaveableField(8)] public int GreenKnightRemaining;
            [SaveableField(9)] public int LostSonsRemaining;
            [SaveableField(10)] public bool OriginalSidesCaptured;
            [SaveableField(11)] public bool ForcedByCheat;
        }

        public bool IsOnCooldown => _cooldownUntil.IsFuture;

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_greenKnightBattleStates", ref _battleStates);
            dataStore.SyncData("_greenKnightCooldownUntil", ref _cooldownUntil);
        }

        public GreenKnightBattleState GetOrCreateBattleState(MapEvent mapEvent)
        {
            if (!_battleStates.TryGetValue(mapEvent, out var state))
            {
                state = new GreenKnightBattleState();
                _battleStates.Add(mapEvent, state);
            }

            return state;
        }

        public void CaptureOriginalSides(MapEvent mapEvent, PartyBase attacker, PartyBase defender)
        {
            var state = GetOrCreateBattleState(mapEvent);
            state.OriginalSidesCaptured = true;

            if (attacker.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                state.BretonniaSide = BattleSideEnum.Attacker;
                state.OpposingFactionId = defender.MapFaction.StringId;
                state.OpposingCultureId = defender.Culture.StringId;
            }
            else if (defender.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                state.BretonniaSide = BattleSideEnum.Defender;
                state.OpposingFactionId = attacker.MapFaction.StringId;
                state.OpposingCultureId = attacker.Culture.StringId;
            }
        }

        public bool MarkGreenKnightSpawned(MapEvent mapEvent)
        {
            var state = _battleStates[mapEvent];
            if (state.Joined)
                return false;

            state.Joined = true;
            _cooldownUntil = CampaignTime.DaysFromNow(20f); // cooldown for green knight reinforcements after a successful spawn
            return true;
        }

        public void RemoveVirtualTroop(MapEvent mapEvent, bool isGreenKnight)
        {
            var state = _battleStates[mapEvent];
            if (isGreenKnight)
                state.GreenKnightRemaining--;
            else
                state.LostSonsRemaining--;
        }

        public bool TryGetActiveVirtualSide(MapEvent mapEvent, out BattleSideEnum side)
        {
            side = BattleSideEnum.None;
            if (MapEvent.PlayerMapEvent != mapEvent || !_battleStates.TryGetValue(mapEvent, out var state))
                return false;

            if ((!state.Joined && IsOnCooldown) || state.GreenKnightRemaining + state.LostSonsRemaining <= 0)
                return false;

            side = state.BretonniaSide;
            return true;
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attacker, PartyBase defender)
        {
            if (mapEvent.IsFieldBattle && !mapEvent.IsNavalMapEvent)
                CaptureOriginalSides(mapEvent, attacker, defender);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            _battleStates.Remove(mapEvent);
        }
    }
}
