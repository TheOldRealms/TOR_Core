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
            CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.KingdomCreation));
        }

        /// <summary>
        /// Add player created kingdom's to the _lastDecisionTime dictionary immediately.
        /// </summary>
        private void KingdomCreation(Kingdom kingdom)
        {
            if (!_lastDecisionTime.TryGetValue(kingdom.StringId, out _))
            {
                _lastDecisionTime.Add(kingdom.StringId, CampaignTime.Now);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            Kingdom.All.ForEach(k => _lastDecisionTime[k.StringId] = CampaignTime.Zero);
        }

        private void DailyTickClan(Clan clan)
        {
            if (!IsEligibleForDecisionMaking(clan)) return;

            if (Campaign.Current?.Models?.DiplomacyModel is not TORDiplomacyModel model) return;

            var kingdom = clan.Kingdom;
            Kingdom peaceCandidate = null;
            Kingdom warCandidate;

            // Emergency peace check - bypass normal cooldowns
            if (!kingdom.UnresolvedDecisions.AnyQ(x => x is MakePeaceKingdomDecision) && ConsiderEmergencyPeace(kingdom))
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
                if (_lastDecisionTime[clan.Kingdom.StringId].ElapsedDaysUntilNow < _minDaysBetweenDecisions) return;

                // Calculate candidates only if there isn't already another clan that has proposed a decision
                // Priority: Peace > Alliance > Trade Agreement > War
                KingdomDecision decision = null;

                // Check for peace
                if (clan.Influence > model.GetInfluenceCostOfProposingPeace(clan) + _influenceReserveToKeep &&
                    !kingdom.UnresolvedDecisions.AnyQ(x => x is MakePeaceKingdomDecision))
                {
                    peaceCandidate = model.GetPeaceDeclarationTargetCandidate(kingdom);
                    if (peaceCandidate != null)
                    {
                        decision = new MakePeaceKingdomDecision(clan, peaceCandidate);
                    }
                }

                // Note: Alliance and Trade Agreement decisions are handled by the base game's IAllianceCampaignBehavior in 1.3
                // We focus on war and peace decisions here

                // Check for war if no peace decision
                if (decision == null &&
                    clan.Influence > model.GetInfluenceCostOfProposingWar(clan) + _influenceReserveToKeep &&
                    !kingdom.UnresolvedDecisions.AnyQ(x => x is DeclareWarDecision))
                {
                    warCandidate = model.GetWarDeclarationTargetCandidate(kingdom);
                    if (warCandidate != null)
                    {
                        decision = new DeclareWarDecision(clan, warCandidate);
                    }
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
            if (kingdom.GetSumEnemyKingdomPower() > kingdom.GetAllianceTotalStrength() * _outnumberRatioForEmergencyPeace) return true;
            return false;
        }

        private bool IsEligibleForDecisionMaking(Clan clan)
        {
            return CampaignTime.Now.ToDays > 5f &&
                    !clan.IsEliminated &&
                    !clan.IsBanditFaction &&
                    clan != Clan.PlayerClan &&
                    clan.CurrentTotalStrength > 0f &&
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
            // Only handle decision updates, not alliance war/peace syncing
            // Alliance war handling is done by TORAllianceWarBehavior
            HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
        }

        private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
        {
            // Only handle decision updates, not alliance war syncing
            // Alliance war handling (HonorAllianceDecision) is done by TORAllianceWarBehavior
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
                else if (kingdomDecision.TriggerTime.IsPast && Clan.PlayerClan.IsUnderMercenaryService)
                {
                    electionList.Add(kingdomDecision);
                }
            }
            foreach (KingdomDecision decisionToCancel in cancelList)
            {
                bool isPlayerInvolved;
                if (!decisionToCancel.DetermineChooser().Leader.IsHumanPlayerCharacter)
                {
                    isPlayerInvolved = decisionToCancel.DetermineSupporters().Any((Supporter x) => x.IsPlayer);
                }
                else
                {
                    isPlayerInvolved = true;
                }
                kingdom.RemoveDecision(decisionToCancel);
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

        /// <summary>
        /// Updates war/peace status for all allies of a kingdom when diplomatic status changes.
        /// </summary>
        public static void UpdateWarPeaceForAlliance(IFaction kingdom)
        {
            var allKingdoms = Kingdom.All.Where(k => !k.IsEliminated);
            var allAllies = kingdom.GetAlliedFactions().ToList();

            foreach (var ally in allAllies)
            {
                foreach (var otherKingdom in allKingdoms.Where(k => k != kingdom && k != ally))
                {
                    if (kingdom.GetStanceWith(otherKingdom).IsAtWar != ally.GetStanceWith(otherKingdom).IsAtWar)
                    {
                        if (kingdom.IsAtWarWith(otherKingdom))
                        {
                            ally.SetAllyTriggered(true);
                            DeclareWarAction.ApplyByKingdomDecision(ally, otherKingdom);
                        }
                        else
                        {
                            ally.SetAllyTriggered(true);
                            // In 1.3, ApplyByKingdomDecision requires tribute parameters (dailyTribute, dailyTributeInstallments)
                            // Alliance-triggered peace has no tribute
                            MakePeaceAction.ApplyByKingdomDecision(ally, otherKingdom, 0, 0);
                        }
                    }
                }
            }
        }
    }
}