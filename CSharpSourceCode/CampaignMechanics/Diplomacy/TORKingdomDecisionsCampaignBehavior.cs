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
        // War/Peace decision settings
        private List<KingdomDecision> _kingdomDecisionsList = [];
        private const float MinDaysBetweenDecisions = 10f;  
        private const float VariationDaysBetweenDecisions = 10f;  //0-10 days
        private const float AgreementConsiderationChance = 0.15f;  //0-10 days
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
            Kingdom.All.ForEach(k => _lastDecisionTime[k.StringId] = CampaignTime.Now);
        }

        private void DailyTickClan(Clan clan)
        {
            if (clan == null || clan.IsEliminated || clan.Kingdom == null)
                return;
            
            if (_lastDecisionTime.Keys.ContainsQ(clan.Kingdom.StringId))
            {
                if (_lastDecisionTime[clan.Kingdom.StringId].ElapsedDaysUntilNow < MinDaysBetweenDecisions + MBRandom.RandomFloatRanged(0,VariationDaysBetweenDecisions))
                {
                    return;
                }
            }
            
            // War/Peace decisions - all eligible clans
            if (!IsEligibleForDecisionMaking(clan)) return;

            // Agreement consideration - only ruling clan, separate timing
            if (clan.Kingdom != Clan.PlayerClan?.Kingdom &&
                ShouldConsiderAgreementsToday(clan.Kingdom))
            {
                if (MBRandom.RandomFloat < 0.5f)
                {
                    ConsiderTradeAgreements(clan);
                }
                else
                {
                    ConsiderAlliances(clan);
                }
            }



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
                if (_lastDecisionTime[clan.Kingdom.StringId].ElapsedDaysUntilNow < MinDaysBetweenDecisions) return;

                // Calculate candidates only if there isn't already another clan that has proposed a decision
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
            if (kingdom.GetTotalEnemyAllianceStrength() > kingdom.GetAllianceTotalStrength() * _outnumberRatioForEmergencyPeace) return true;
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
            // Clean up old decisions
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
                else if (kingdomDecision.TriggerTime.IsPast)
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
            // vanilla fallback? im not sure what was intended here
            foreach (KingdomDecision decisionToVote in electionList)
            {
                if (kingdom == Clan.PlayerClan.Kingdom && !Clan.PlayerClan.IsUnderMercenaryService)
                {
                    new KingdomElection(decisionToVote).StartElection();
                }
                else
                {
                    new KingdomElection(decisionToVote).StartElectionWithoutPlayer();
                }

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
            var allAllies = kingdom.GetAlliedKingdoms().ToList();

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
                            MakePeaceAction.ApplyByKingdomDecision(ally, otherKingdom, 0, 0);
                        }
                    }
                }
            }
        }
        

        private void ConsiderTradeAgreements(Clan consideringClan)
        {
            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel as TORTradeAgreementModel;
            if (tradeModel == null)
                return;
            
            Kingdom kingdom = consideringClan.Kingdom;

            var potentialPartners = tradeModel.GetPotentialTradePartners(kingdom);
            if (!potentialPartners.Any())
                return;

            foreach (var targetKingdom in potentialPartners)
            {
                float score = tradeModel.GetScoreOfStartingTradeAgreement(kingdom, targetKingdom, consideringClan, out _);

                if (MBRandom.RandomFloat * 100f < score)
                {
                    kingdom.AddDecision(new TradeAgreementDecision(consideringClan, targetKingdom), true);
                    _lastDecisionTime[kingdom.StringId] = CampaignTime.Now;
                    return;
                }
            }
        }
        

        private void ConsiderAlliances(Clan consideringClan)
        {
            var allianceModel = Campaign.Current?.Models?.AllianceModel as TORAllianceModel;
            if (allianceModel == null)
                return;
            
            var kingdom = consideringClan.Kingdom;

            var potentialAllies = allianceModel.GetPotentialAlliancePartners(kingdom);
            if (!potentialAllies.Any())
                return;
            
            var alliesByScore = new List<(Kingdom kingdom, float value)>();
            foreach (var targetKingdom in potentialAllies)
            {
                float score = allianceModel.GetScoreOfStartingAlliance(kingdom, targetKingdom, kingdom.RulingClan, out _).ResultNumber;
                alliesByScore.Add((targetKingdom, (int)score));
            }
            
            var bestCandidate = alliesByScore.MaxBy(value => value.value).kingdom;
            var result = alliesByScore.MaxBy(value => value.value).value;
            
            if (result > 50 && MBRandom.RandomFloat * 1000f < result)
            {
                kingdom.AddDecision(new StartAllianceDecision(kingdom.RulingClan, bestCandidate), true);
                _lastDecisionTime[kingdom.StringId] = CampaignTime.Now;
            }
        }

        private bool ShouldConsiderAgreementsToday(Kingdom kingdom)
        {
            if (MBRandom.RandomFloat < AgreementConsiderationChance) return false;
            
            return true;
        }
        
        
        
    }
}