using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    public class TORKingdomDecisionsCampaignBehavior : CampaignBehaviorBase
    {
        private List<KingdomDecision> _kingdomDecisionsList = [];
        private float _minDaysBetweenDecisions = 20f;
        private Dictionary<string, CampaignTime> _lastDecisionTime = [];
        private float _influenceReserveToKeep = 300f;
        private float _outnumberRatioForEmergencyPeace = 5f;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            Kingdom.All.ForEach(k=> _lastDecisionTime[k.StringId] = CampaignTime.Zero);
        }

        private void DailyTickClan(Clan clan)
        {
            if (!IsEligibleForDecisionMaking(clan)) return;

            if (Campaign.Current?.Models?.DiplomacyModel is not TORDiplomacyModel model) return;

            var kingdom = clan.Kingdom;
            Kingdom peaceCandidate;
            Kingdom warCandidate;

            if (ConsiderEmergencyPeace(kingdom) && !kingdom.UnresolvedDecisions.AnyQ(x => x is MakePeaceKingdomDecision))
            {
                peaceCandidate = model.GetPeaceDeclarationTargetCandidate(kingdom, true);
                if (peaceCandidate != null && !peaceCandidate.UnresolvedDecisions.AnyQ(x => x is MakePeaceKingdomDecision))
                {
                    var peaceDecision = new MakePeaceKingdomDecision(clan, peaceCandidate, MBRandom.RandomInt(1000, 3000));
                    _kingdomDecisionsList.Add(peaceDecision);
                    clan.Kingdom.AddDecision(peaceDecision, true);
                    _lastDecisionTime[kingdom.StringId] = CampaignTime.Now;
                }
            }
            else
            {
                peaceCandidate = model.GetPeaceDeclarationTargetCandidate(kingdom);
                warCandidate = model.GetWarDeclarationTargetCandidate(kingdom);

                if (_lastDecisionTime[clan.Kingdom.StringId].ElapsedDaysUntilNow < _minDaysBetweenDecisions) return;

                KingdomDecision decision = null;
                if (clan.Influence > model.GetInfluenceCostOfProposingPeace(clan) + _influenceReserveToKeep && peaceCandidate != null && !kingdom.UnresolvedDecisions.AnyQ(x => x is MakePeaceKingdomDecision))
                {
                    decision = new MakePeaceKingdomDecision(clan, peaceCandidate);
                }
                else if (clan.Influence > model.GetInfluenceCostOfProposingWar(clan) + _influenceReserveToKeep && warCandidate != null && !kingdom.UnresolvedDecisions.AnyQ(x => x is DeclareWarDecision))
                {
                    decision = new DeclareWarDecision(clan, warCandidate);
                }

                if (decision != null)
                {
                    _kingdomDecisionsList.Add(decision);
                    clan.Kingdom.AddDecision(decision, false);
                    _lastDecisionTime[kingdom.StringId] = CampaignTime.Now;
                }
            }

            UpdateKingdomDecisions(clan.Kingdom);
        }

        private bool ConsiderEmergencyPeace(Kingdom kingdom)
        {
            if (kingdom.GetSumEnemyKingdomPower() > kingdom.TotalStrength * _outnumberRatioForEmergencyPeace) return true;
            return false;
        }

        private bool IsEligibleForDecisionMaking(Clan clan)
        {
            return Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow > 5f &&
                    !clan.IsEliminated &&
                    !clan.IsBanditFaction &&
                    clan != Clan.PlayerClan &&
                    clan.TotalStrength > 0f &&
                    clan.Kingdom != null &&
                    clan.Influence > 0f &&
                    !clan.IsMinorFaction &&
                    !clan.IsUnderMercenaryService;
        }

        private void HourlyTick()
        {
            if (Clan.PlayerClan.Kingdom != null)
            {
                UpdateKingdomDecisions(Clan.PlayerClan.Kingdom);
            }
        }

        private void DailyTick()
        {
            if (_kingdomDecisionsList != null)
            {
                int count = _kingdomDecisionsList.Count;
                int num = 0;
                for (int i = 0; i < count; i++)
                {
                    if (_kingdomDecisionsList[i - num].TriggerTime.ElapsedDaysUntilNow > 15f)
                    {
                        _kingdomDecisionsList.RemoveAt(i - num);
                        num++;
                    }
                }
            }
        }

        private void OnPeaceMade(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
        {
            HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
        }

        private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
        {
            HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
        }

        private void HandleDiplomaticChangeBetweenFactions(IFaction side1Faction, IFaction side2Faction)
        {
            if (side1Faction.IsKingdomFaction && side2Faction.IsKingdomFaction)
            {
                UpdateKingdomDecisions((Kingdom)side1Faction);
                UpdateKingdomDecisions((Kingdom)side2Faction);
            }
        }

        public void UpdateKingdomDecisions(Kingdom kingdom)
        {
            List<KingdomDecision> cancelList = [];
            List<KingdomDecision> electionList = [];
            foreach (KingdomDecision kingdomDecision in kingdom.UnresolvedDecisions)
            {
                if (kingdomDecision.ShouldBeCancelled())
                {
                    cancelList.Add(kingdomDecision);
                }
                else if (kingdomDecision.TriggerTime.IsPast && !kingdomDecision.NeedsPlayerResolution)
                {
                    electionList.Add(kingdomDecision);
                }
            }
            foreach (KingdomDecision decisionToCancel in cancelList)
            {
                kingdom.RemoveDecision(decisionToCancel);
                bool isPlayerInvolved;
                if (!decisionToCancel.DetermineChooser().Leader.IsHumanPlayerCharacter)
                {
                    isPlayerInvolved = decisionToCancel.DetermineSupporters().Any((Supporter x) => x.IsPlayer);
                }
                else
                {
                    isPlayerInvolved = true;
                }
                CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(decisionToCancel, isPlayerInvolved);
            }
            foreach (KingdomDecision decisionToVote in electionList)
            {
                new KingdomElection(decisionToVote).StartElectionWithoutPlayer();
                _lastDecisionTime[kingdom.StringId] = CampaignTime.Now;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_kingdomDecisionsList", ref _kingdomDecisionsList);
        }
    }
}
