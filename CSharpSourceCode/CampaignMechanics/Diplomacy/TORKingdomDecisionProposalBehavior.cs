using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Diplomacy
{

    public class TORKingdomDecisionProposalBehavior : CampaignBehaviorBase
    {
        private const int KingdomDecisionProposalCooldownInDays = 1;
        private const float ClanInterestModifier = 1f;
        private const float DecisionSuccessChanceModifier = 1f;
        private List<KingdomDecision> _kingdomDecisionsList;
        private delegate KingdomDecision KingdomDecisionCreatorDelegate(Clan sponsorClan);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, SessionLaunched);
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        }

        private void DailyTickClan(Clan clan)
        {
            if (Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < 5 || 
                clan == null ||
                clan.Leader == null ||
                clan.IsEliminated || 
                clan == Clan.PlayerClan || 
                clan.TotalStrength <= 0 || 
                clan.IsBanditFaction || 
                clan.Kingdom == null || 
                clan.Influence < 100) 
                return;

            KingdomDecision kingdomDecision = null;
            float randomFloat = MBRandom.RandomFloat;
            float num = MathF.Min(0.33f, (float)(1.0f / (((Kingdom)clan.MapFaction).Clans.Count(x => x.Influence > 100) + 2))) * (clan.Kingdom != Hero.MainHero.MapFaction || Hero.MainHero.Clan.IsUnderMercenaryService ? 1f : (clan.Kingdom.Leader == Hero.MainHero ? 0.5f : 0.75f));
            
            DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;

            if (randomFloat < num && clan.Influence > diplomacyModel.GetInfluenceCostOfProposingPeace(clan))
                kingdomDecision = GetRandomPeaceDecision(clan);
            else if (randomFloat < num * 2 && clan.Influence > diplomacyModel.GetInfluenceCostOfProposingWar(clan))
                kingdomDecision = GetRandomWarDecision(clan);
            else if (randomFloat < num * 2.5f && clan.Influence > (diplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan) * 4))
                kingdomDecision = GetRandomPolicyDecision(clan);
            else if (randomFloat < num * 3 && clan.Influence > 700)
                kingdomDecision = GetRandomAnnexationDecision(clan);

            bool playerDecisionPending = false;

            if (kingdomDecision != null)
            {
                if (_kingdomDecisionsList == null) _kingdomDecisionsList = new List<KingdomDecision>();

                if (kingdomDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision)kingdomDecision).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
                {
                    foreach (KingdomDecision decision in _kingdomDecisionsList)
                    {
                        CampaignTime triggerTime;
                        if (decision is MakePeaceKingdomDecision && decision.Kingdom == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision)decision).FactionToMakePeaceWith == clan.Kingdom)
                        {
                            triggerTime = decision.TriggerTime;
                            if (triggerTime.IsFuture)
                                playerDecisionPending = true;
                        }
                        if (decision is MakePeaceKingdomDecision && decision.Kingdom == clan.Kingdom && ((MakePeaceKingdomDecision)decision).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
                        {
                            triggerTime = decision.TriggerTime;
                            if (triggerTime.IsFuture)
                                playerDecisionPending = true;
                        }
                    }
                }
                if (playerDecisionPending) return;

                bool shouldAddDecision = true;
                foreach (KingdomDecision decision in _kingdomDecisionsList)
                {
                    if (decision is DeclareWarDecision declareWarDecision1 && kingdomDecision is DeclareWarDecision declareWarDecision2 && declareWarDecision1.FactionToDeclareWarOn == declareWarDecision2.FactionToDeclareWarOn && declareWarDecision1.ProposerClan.MapFaction == declareWarDecision2.ProposerClan.MapFaction)
                        shouldAddDecision = false;
                    else if (decision is MakePeaceKingdomDecision peaceKingdomDecision1 && kingdomDecision is MakePeaceKingdomDecision peaceKingdomDecision2 && peaceKingdomDecision1.FactionToMakePeaceWith == peaceKingdomDecision2.FactionToMakePeaceWith && peaceKingdomDecision1.ProposerClan.MapFaction == peaceKingdomDecision2.ProposerClan.MapFaction)
                        shouldAddDecision = false;
                }
                if(!shouldAddDecision) return;

                _kingdomDecisionsList.Add(kingdomDecision);
                KingdomElection kingdomElection = new KingdomElection(kingdomDecision);
                clan.Kingdom.AddDecision(kingdomDecision);
            }
            else UpdateKingdomDecisions(clan.Kingdom);
        }

        private void HourlyTick()
        {
            if (Clan.PlayerClan.Kingdom != null) UpdateKingdomDecisions(Clan.PlayerClan.Kingdom);
        }

        private void DailyTick()
        {
            if (_kingdomDecisionsList == null || _kingdomDecisionsList.Count <= 0)
                return;
            int num = 0;
            for (int i = 0; i < _kingdomDecisionsList.Count; ++i)
            {
                if (_kingdomDecisionsList[i - num].TriggerTime.ElapsedDaysUntilNow > 15)
                {
                    _kingdomDecisionsList.RemoveAt(i - num);
                    num++;
                }
            }
        }

        public void UpdateKingdomDecisions(Kingdom kingdom)
        {
            List<KingdomDecision> cancelDecisionList = new List<KingdomDecision>();
            List<KingdomDecision> doDecisionList = new List<KingdomDecision>();
            foreach (KingdomDecision unresolvedDecision in kingdom.UnresolvedDecisions)
            {
                if (unresolvedDecision.ShouldBeCancelled()) cancelDecisionList.Add(unresolvedDecision);

                else if (unresolvedDecision.TriggerTime.IsPast && !unresolvedDecision.NeedsPlayerResolution)
                    doDecisionList.Add(unresolvedDecision);
            }
            foreach (KingdomDecision kingdomDecision in cancelDecisionList)
            {
                kingdom.RemoveDecision(kingdomDecision);
                bool isPlayerInvolved = kingdomDecision.DetermineChooser().Leader.IsHumanPlayerCharacter || kingdomDecision.DetermineSupporters().Any<Supporter>((Func<Supporter, bool>)(x => x.IsPlayer));
                CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(kingdomDecision, isPlayerInvolved);
            }
            foreach (KingdomDecision decision in doDecisionList)
                new KingdomElection(decision).StartElectionWithoutPlayer();
        }

        private void OnPeaceMade(
          IFaction side1Faction,
          IFaction side2Faction,
          MakePeaceAction.MakePeaceDetail detail)
        {
            HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
        }

        private void OnWarDeclared(
          IFaction side1Faction,
          IFaction side2Faction,
          DeclareWarAction.DeclareWarDetail detail)
        {
            HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
        }

        private void HandleDiplomaticChangeBetweenFactions(IFaction side1Faction, IFaction side2Faction)
        {
            if (!side1Faction.IsKingdomFaction || !side2Faction.IsKingdomFaction)
                return;
            UpdateKingdomDecisions((Kingdom)side1Faction);
            UpdateKingdomDecisions((Kingdom)side2Faction);
        }

        private KingdomDecision GetRandomWarDecision(Clan clan)
        {
            KingdomDecision randomWarDecision = null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault(x => x is DeclareWarDecision) != null)
                return null;

            Kingdom targetKingdom = Kingdom.All.GetRandomElementWithPredicate(x => !x.IsEliminated && 
                    x != kingdom && !x.IsAtWarWith(kingdom) && 
                    x.GetStanceWith(kingdom).PeaceDeclarationDate.ElapsedDaysUntilNow > 20.0 && 
                    x.Settlements.Count > 0 && x.RulingClan != null);

            if (targetKingdom != null && ConsiderWar(clan, kingdom, targetKingdom))
                randomWarDecision = new TORDeclareWarDecision(clan, targetKingdom);

            return randomWarDecision;
        }

        private bool ConsiderWar(Clan clan, Kingdom kingdom, IFaction otherFaction)
        {
            int num = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(clan) / 2;
            if (clan.Influence < num) return false;
            TORDeclareWarDecision decision = new TORDeclareWarDecision(clan, otherFaction);
            //wtf are these magic numbers?
            return decision.CalculateSupport(clan) > 50 && MBRandom.RandomFloat < 1.4 * GetKingdomSupportForDecision(decision) - 0.55; 
        }

        private KingdomDecision GetRandomPeaceDecision(Clan clan)
        {
            KingdomDecision randomPeaceDecision = null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault(x => x is MakePeaceKingdomDecision) != null)
                return null;
            Kingdom elementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(x => x.IsAtWarWith(kingdom));
            MakePeaceKingdomDecision decision;
            if (elementWithPredicate != null && ConsiderPeace(clan, elementWithPredicate.RulingClan, kingdom, elementWithPredicate, out decision))
                randomPeaceDecision = decision;
            return randomPeaceDecision;
        }

        

        private float GetKingdomSupportForWar(Clan clan, Kingdom kingdom, IFaction otherFaction) => new KingdomElection((KingdomDecision)new TORDeclareWarDecision(clan, otherFaction)).GetLikelihoodForSponsor(clan);

        private bool ConsiderPeace(
          Clan clan,
          Clan otherClan,
          Kingdom kingdom,
          IFaction otherFaction,
          out MakePeaceKingdomDecision decision)
        {
            decision = (MakePeaceKingdomDecision)null;
            int ofProposingPeace = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace(clan);
            if ((double)clan.Influence < (double)ofProposingPeace)
                return false;
            int num1 = new PeaceBarterable(clan.Leader, (IFaction)kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction(otherFaction);
            int num2 = -num1;
            int num3;
            if (clan.MapFaction == Hero.MainHero.MapFaction && otherFaction is Kingdom)
            {
                foreach (Clan clan1 in (List<Clan>)((Kingdom)otherFaction).Clans)
                {
                    if (clan1.Leader != clan1.MapFaction.Leader)
                    {
                        int valueForFaction = new PeaceBarterable(clan1.Leader, (IFaction)kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction((IFaction)clan1);
                        if (valueForFaction < num1)
                            num1 = valueForFaction;
                    }
                }
                num3 = -num1;
            }
            else
                num3 = num2 + 30000;
            if (otherFaction is Clan && num3 < 0)
                num3 = 0;
            float num4 = 0.5f;
            if (otherFaction == Hero.MainHero.MapFaction)
            {
                PeaceBarterable peaceBarterable = new PeaceBarterable(clan.MapFaction.Leader, (IFaction)kingdom, otherFaction, CampaignTime.Years(1f));
                int num5 = peaceBarterable.GetValueForFaction(clan.MapFaction);
                int num6 = 0;
                int num7 = 1;
                if (clan.MapFaction is Kingdom)
                {
                    foreach (Clan clan2 in (List<Clan>)((Kingdom)clan.MapFaction).Clans)
                    {
                        if (clan2.Leader != clan2.MapFaction.Leader)
                        {
                            int valueForFaction = peaceBarterable.GetValueForFaction((IFaction)clan2);
                            if (valueForFaction < num5)
                                num5 = valueForFaction;
                            num6 += valueForFaction;
                            ++num7;
                        }
                    }
                }
                int num8 = (int)(0.6499999761581421 * (double)((float)num6 / (float)num7) + 0.3499999940395355 * (double)num5);
                if (num8 > num3)
                {
                    num3 = num8;
                    num4 = 0.2f;
                }
            }
            int num9 = num3;
            if (num3 > -5000 && num3 < 5000)
                num3 = 0;
            int dailyTributeForValue1 = Campaign.Current.Models.DiplomacyModel.GetDailyTributeForValue(num3);
            decision = new MakePeaceKingdomDecision(clan, otherFaction, dailyTributeForValue1);
            if ((double)decision.CalculateSupport(clan) <= 5.0 || (double)MBRandom.RandomFloat >= 2.0 * ((double)this.GetKingdomSupportForDecision((KingdomDecision)decision) - (double)num4))
                return false;
            if (otherFaction == Hero.MainHero.MapFaction)
            {
                int num10 = num9 + 15000;
                if (num10 > -5000 && num10 < 5000)
                    num10 = 0;
                int dailyTributeForValue2 = Campaign.Current.Models.DiplomacyModel.GetDailyTributeForValue(num10);
                decision = new MakePeaceKingdomDecision(clan, otherFaction, dailyTributeForValue2);
            }
            return true;
        }

        private float GetKingdomSupportForPeace(
          Clan clan,
          Clan otherClan,
          Kingdom kingdom,
          IFaction otherFaction)
        {
            int num1 = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace(clan) / 2;
            int num2 = -new PeaceBarterable(clan.Leader, (IFaction)kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction(otherFaction);
            if (otherFaction is Clan && num2 < 0)
                num2 = 0;
            if (num2 > -5000 && num2 < 5000)
                num2 = 0;
            int dailyTributeForValue = Campaign.Current.Models.DiplomacyModel.GetDailyTributeForValue(num2);
            return new KingdomElection((KingdomDecision)new MakePeaceKingdomDecision(clan, otherFaction, dailyTributeForValue)).GetLikelihoodForSponsor(clan);
        }

        private KingdomDecision GetRandomPolicyDecision(Clan clan)
        {
            KingdomDecision randomPolicyDecision = (KingdomDecision)null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault<KingdomDecision>((Func<KingdomDecision, bool>)(x => x is KingdomPolicyDecision)) != null)
                return (KingdomDecision)null;
            if ((double)clan.Influence < 200.0)
                return (KingdomDecision)null;
            PolicyObject randomElement = PolicyObject.All.GetRandomElement<PolicyObject>();
            bool flag = kingdom.ActivePolicies.Contains(randomElement);
            if (this.ConsiderPolicy(clan, kingdom, randomElement, flag))
                randomPolicyDecision = (KingdomDecision)new KingdomPolicyDecision(clan, randomElement, flag);
            return randomPolicyDecision;
        }

        private bool ConsiderPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
        {
            int proposalAndDisavowal = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
            if ((double)clan.Influence < (double)proposalAndDisavowal)
                return false;
            KingdomPolicyDecision decision = new KingdomPolicyDecision(clan, policy, invert);
            return (double)decision.CalculateSupport(clan) > 50.0 && (double)MBRandom.RandomFloat < (double)this.GetKingdomSupportForDecision((KingdomDecision)decision) - 0.55;
        }

        private float GetKingdomSupportForPolicy(
          Clan clan,
          Kingdom kingdom,
          PolicyObject policy,
          bool invert)
        {
            Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
            return new KingdomElection((KingdomDecision)new KingdomPolicyDecision(clan, policy, invert)).GetLikelihoodForSponsor(clan);
        }

        private KingdomDecision GetRandomAnnexationDecision(Clan clan)
        {
            KingdomDecision annexationDecision = (KingdomDecision)null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault<KingdomDecision>((Func<KingdomDecision, bool>)(x => x is KingdomPolicyDecision)) != null)
                return (KingdomDecision)null;
            if ((double)clan.Influence < 300.0)
                return (KingdomDecision)null;
            Clan randomElement1 = kingdom.Clans.GetRandomElement<Clan>();
            if (randomElement1 != null && randomElement1 != clan && randomElement1.GetRelationWithClan(clan) < -25)
            {
                if (randomElement1.Fiefs.Count == 0)
                    return (KingdomDecision)null;
                Town randomElement2 = randomElement1.Fiefs.GetRandomElement<Town>();
                if (this.ConsiderAnnex(clan, randomElement2))
                    annexationDecision = (KingdomDecision)new SettlementClaimantPreliminaryDecision(clan, randomElement2.Settlement);
            }
            return annexationDecision;
        }

        private bool ConsiderAnnex(Clan clan, Town targetSettlement)
        {
            int costOfAnnexation = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(clan);
            if ((double)clan.Influence < (double)costOfAnnexation)
                return false;
            SettlementClaimantPreliminaryDecision decision = new SettlementClaimantPreliminaryDecision(clan, targetSettlement.Settlement);
            return (double)decision.CalculateSupport(clan) > 50.0 && (double)MBRandom.RandomFloat < (double)this.GetKingdomSupportForDecision((KingdomDecision)decision) - 0.6;
        }

        private float GetKingdomSupportForDecision(KingdomDecision decision) => new KingdomElection(decision).GetLikelihoodForOutcome(0);

        private void SessionLaunched(CampaignGameStarter starter)
        {
        }

        public override void SyncData(IDataStore dataStore) => dataStore.SyncData("_kingdomDecisionsList", ref _kingdomDecisionsList);
    }
}
