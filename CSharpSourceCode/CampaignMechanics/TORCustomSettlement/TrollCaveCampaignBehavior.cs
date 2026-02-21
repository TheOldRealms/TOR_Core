using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class TrollCaveCampaignBehavior : CampaignBehaviorBase
    {
        private const string TrollClanId = "troll_clan_1";
        private const int CooldownDays = 28; // 4 weeks

        [SaveableField(0)]
        private Dictionary<string, CampaignTime> _caveClearedTime = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, EnforceWarWithTrolls);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            // Check if this was a troll cave battle
            if (!mapEvent.IsPlayerMapEvent) return;

            // Find troll defender party in the battle
            foreach (var party in mapEvent.DefenderSide.Parties)
            {
                if (party.Party?.MobileParty?.PartyComponent is TrollCaveDefenderPartyComponent defenderComponent)
                {
                    var settlement = defenderComponent.HomeSettlement;

                    // Player won if they were on the winning side
                    bool playerWon = mapEvent.WinningSide == mapEvent.PlayerSide;

                    if (playerWon && settlement != null)
                    {
                        // Victory - set cooldown, vanilla handles loot/prisoners
                        SetCaveCleared(settlement);
                    }
                    // On defeat, TrollCaveMissionController.HandleForcedDefeatEnd handles everything

                    // Destroy the temporary defender party
                    TrollCaveDefenderPartyComponent.DestroyDefenderParty(party.Party?.MobileParty);
                    break;
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_caveClearedTime", ref _caveClearedTime);
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement.SettlementComponent is not TrollCaveComponent trollCave)
                return;

            // Check if cooldown has expired and reactivate
            if (!trollCave.IsActive && _caveClearedTime.TryGetValue(settlement.StringId, out var clearedTime))
            {
                if (CampaignTime.Now.ToDays - clearedTime.ToDays >= CooldownDays)
                {
                    trollCave.IsActive = true;
                    _caveClearedTime.Remove(settlement.StringId);
                }
            }
        }

        public void SetCaveCleared(Settlement settlement)
        {
            if (settlement.SettlementComponent is TrollCaveComponent trollCave)
            {
                trollCave.IsActive = false;
                _caveClearedTime[settlement.StringId] = CampaignTime.Now;
            }
        }

        public bool IsCaveOnCooldown(Settlement settlement)
        {
            if (settlement == null) return false;
            return _caveClearedTime.ContainsKey(settlement.StringId);
        }

        public int GetCooldownDaysRemaining(Settlement settlement)
        {
            if (_caveClearedTime.TryGetValue(settlement.StringId, out var clearedTime))
            {
                int elapsed = (int)(CampaignTime.Now.ToDays - clearedTime.ToDays);
                return CooldownDays - elapsed;
            }
            return 0;
        }

        private void EnforceWarWithTrolls(CampaignGameStarter starter)
        {
            Clan trollClan = Clan.FindFirst(x => x.StringId == TrollClanId);
            if (trollClan == null) return;

            List<Kingdom> allKingdoms = Kingdom.All.ToList();
            List<Clan> allClans = Clan.NonBanditFactions.Where(x => x.StringId != TrollClanId).ToList();

            // Set troll clan as enemy of all kingdoms
            foreach (var kingdom in allKingdoms)
            {
                if (!trollClan.IsAtWarWith(kingdom))
                {
                    FactionManager.DeclareWar(trollClan, kingdom);
                }
            }

            // Set troll clan as enemy of all non-troll clans
            foreach (var clan in allClans)
            {
                if (!trollClan.IsAtWarWith(clan))
                {
                    FactionManager.DeclareWar(trollClan, clan);
                }
            }
        }
    }
}
