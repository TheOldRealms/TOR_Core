using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORFactionDiscontinuationCampaignBehavior : CampaignBehaviorBase
    {
        private const float SurvivalDurationForIndependentClanInWeeks = 4f;
        private Dictionary<string, double> _independentClans = [];
        private List<string> _kingdomsPendingDiscontinuation = [];

        // Special faction pairings - these factions should merge with each other
        private readonly Dictionary<string, string> specialPairings = new()
        {
            { TORConstants.Cultures.EONIR, TORConstants.Cultures.ASRAI },
            { TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR },
            { TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.SYLVANIA },
            { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON }
        };


        public override void RegisterEvents()
        {
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
        }

        public void OnSettlementOwnerChanged(
            Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (_independentClans.ContainsKey(newOwner.Clan.StringId))
            {
                _independentClans.Remove(newOwner.Clan.StringId);
            }
            if (CanClanBeDiscontinued(oldOwner.Clan))
            {
                AddIndependentClan(oldOwner.Clan);
            }
            Kingdom kingdom = oldOwner.Clan.Kingdom;
            if (kingdom != null && CanKingdomBeDiscontinued(kingdom))
            {
                QueueKingdomDiscontinuation(kingdom);
            }
        }

        public void OnClanChangedKingdom(
            Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
        {
            if (newKingdom == null)
            {
                if (CanClanBeDiscontinued(clan))
                {
                    AddIndependentClan(clan);
                }
            }
            else if (_independentClans.ContainsKey(clan.StringId))
            {
                _independentClans.Remove(clan.StringId);
            }
            if (clan == Clan.PlayerClan && oldKingdom != null && CanKingdomBeDiscontinued(oldKingdom))
            {
                QueueKingdomDiscontinuation(oldKingdom);
            }
        }
        private void DailyTick()
        {
            foreach (var kingdomId in _kingdomsPendingDiscontinuation.ToList())
            {
                _kingdomsPendingDiscontinuation.Remove(kingdomId);

                var kingdom = Kingdom.All.FirstOrDefault(x => x.StringId == kingdomId);
                if (kingdom != null && CanKingdomBeDiscontinued(kingdom))
                {
                    DiscontinueKingdom(kingdom);
                }
            }
        }

        private void QueueKingdomDiscontinuation(Kingdom kingdom)
        {
            if (!_kingdomsPendingDiscontinuation.Contains(kingdom.StringId))
            {
                _kingdomsPendingDiscontinuation.Add(kingdom.StringId);
            }
        }

        private void DailyTickClan(Clan clan)
        {
            if (!_independentClans.ContainsKey(clan.StringId))
            {
                return;
            }

            //If all nobles executed, remove clan.
            if (!clan.Heroes.WhereQ(x => x.IsLord).Any())
            {
                DiscontinueClan(clan);
                return;
            }

            //30% chance to avoid  further code.
            if (MBRandom.RandomFloat >= 0.3f)
            {
                if (_independentClans[clan.StringId] < CampaignTime.Now.ToWeeks) { DiscontinueClan(clan); }
                return;
            }

            var candidateKingdoms = GetCandidateKingdomsForJoiningClan(clan);
            if (candidateKingdoms.Any())
            {
                var targetKingdom = candidateKingdoms.MinBy(x => x.Heroes.Count);
                if (targetKingdom != null && targetKingdom.Heroes.Count > 0)
                {
                    float chance = 10f / ((float)targetKingdom.Heroes.Count * 2);

                    if (MBRandom.RandomFloat < chance)
                    {
                        ChangeKingdomAction.ApplyByJoinToKingdom(clan, targetKingdom);
                        return;
                    }
                }
            }
            if (_independentClans[clan.StringId] < CampaignTime.Now.ToWeeks) { DiscontinueClan(clan); }
        }

        private IEnumerable<Kingdom> GetCandidateKingdomsForJoiningClan(Clan clan)
        {
            var clanCulture = clan.Culture;

            if (specialPairings.TryGetValue(clanCulture.StringId, out string pairedCulture))
            {
                //permits fragmented cultures between kingdoms as well as a player kingdom for viable candidates
                return Kingdom.All.WhereQ(x => !x.IsEliminated && (x.Culture == clan.Culture || x.Culture.StringId == pairedCulture));
            }

            return Kingdom.All.WhereQ(x => !x.IsEliminated && x.Culture == clan.Culture);
        }



        private bool CanKingdomBeDiscontinued(Kingdom kingdom)
        {
            bool flag = !kingdom.IsEliminated
                        && kingdom != Clan.PlayerClan.Kingdom
                        && kingdom.Settlements.IsEmpty();
            if (flag)
            {
                CampaignEventDispatcher.Instance.CanKingdomBeDiscontinued(kingdom, ref flag);
            }
            return flag;
        }

        private void DiscontinueKingdom(Kingdom kingdom)
        {
            foreach (Clan clan in new List<Clan>(kingdom.Clans))
            {
                FinalizeMapEvents(clan);
                ChangeKingdomAction.ApplyByLeaveByKingdomDestruction(clan, true);
            }
            kingdom.RulingClan = null;
            DestroyKingdomAction.Apply(kingdom);
        }

        private void FinalizeMapEvents(Clan clan)
        {
            foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents.ToList())
            {
                if (warPartyComponent?.MobileParty?.MapEvent != null)
                {
                    warPartyComponent.MobileParty.MapEvent.FinalizeEvent();
                }
                if (warPartyComponent?.MobileParty?.SiegeEvent != null)
                {
                    warPartyComponent.MobileParty.SiegeEvent.FinalizeSiegeEvent();
                }
            }
            foreach (Settlement settlement in clan.Settlements.ToList())
            {
                if (settlement?.Party?.MapEvent != null)
                {
                    settlement.Party.MapEvent.FinalizeEvent();
                }
                if (settlement?.Party?.SiegeEvent != null)
                {
                    settlement.Party.SiegeEvent.FinalizeSiegeEvent();
                }
            }
        }

        private bool CanClanBeDiscontinued(Clan clan) => clan.Kingdom == null
                && !clan.IsRebelClan
                && !clan.IsBanditFaction
                && !clan.IsMinorFaction
                && clan != Clan.PlayerClan
                && clan.Settlements.IsEmpty();

        private void DiscontinueClan(Clan clan)
        {
            DestroyClanAction.Apply(clan);
            _independentClans.Remove(clan.StringId);
        }

        private void AddIndependentClan(Clan clan)
        {
            if (!_independentClans.ContainsKey(clan.StringId))
            {
                _independentClans.Add(clan.StringId, CampaignTime.WeeksFromNow(SurvivalDurationForIndependentClanInWeeks).ToWeeks);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_independentClans", ref _independentClans);
            dataStore.SyncData("_kingdomsPendingDiscontinuation", ref _kingdomsPendingDiscontinuation);
        }
    }
}