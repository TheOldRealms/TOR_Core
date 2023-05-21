// Decompiled with JetBrains decompiler
// Type: TaleWorlds.CampaignSystem.CampaignBehaviors.KingdomDecisionProposalBehavior
// Assembly: TaleWorlds.CampaignSystem, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5258A0C3-6337-4AB0-A110-152B92C40F90
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll

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

    // This is a, largely unchanged, copy of KingdomDecisionProposalBehavior
    public class TORKingdomDecisionProposalBehavior : CampaignBehaviorBase
    {
        private List<KingdomDecision> _kingdomDecisionsList;

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
            if ((int)Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < 5.0 || clan.IsEliminated || clan == Clan.PlayerClan || clan.TotalStrength <= 0.0 || clan.IsBanditFaction || clan.Kingdom == null || clan.Influence < 100.0)
                return;

            float randomFloat = MBRandom.RandomFloat;
            float num = MathF.Min(0.33f, (float)(1.0 / (((Kingdom)clan.MapFaction).Clans.Count(x => x.Influence > 100.0) + 2.0)))
                        * (clan.Kingdom != Hero.MainHero.MapFaction || Hero.MainHero.Clan.IsUnderMercenaryService ? 1f : (clan.Kingdom.Leader == Hero.MainHero ? 0.5f : 0.75f));

            DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
            KingdomDecision kingdomDecision = null;

            if (randomFloat < (double)num && clan.Influence > (double)diplomacyModel.GetInfluenceCostOfProposingPeace())
            {
                kingdomDecision = GetRandomPeaceDecision(clan);
            }
            else if (randomFloat < num * 2.0 && clan.Influence > (double)diplomacyModel.GetInfluenceCostOfProposingWar(clan.Kingdom))
            {
                kingdomDecision = GetRandomWarDecision(clan);
            }
            else if (randomFloat < num * 2.5 && clan.Influence > (double)(diplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal() * 4))
            {
                kingdomDecision = GetRandomPolicyDecision(clan);
            }
            else if (randomFloat < num * 3.0 && clan.Influence > 700.0)
            {
                kingdomDecision = GetRandomAnnexationDecision(clan);
            }

            if (kingdomDecision != null)
            {
                if (_kingdomDecisionsList == null)
                    _kingdomDecisionsList = new List<KingdomDecision>();
                bool flag1 = false;
                if (kingdomDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision)kingdomDecision).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
                {
                    foreach (KingdomDecision kingdomDecisions in _kingdomDecisionsList)
                    {
                        CampaignTime triggerTime;
                        if (kingdomDecisions is MakePeaceKingdomDecision && kingdomDecisions.Kingdom == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision)kingdomDecisions).FactionToMakePeaceWith == clan.Kingdom)
                        {
                            triggerTime = kingdomDecisions.TriggerTime;
                            if (triggerTime.IsFuture)
                                flag1 = true;
                        }
                        if (kingdomDecisions is MakePeaceKingdomDecision && kingdomDecisions.Kingdom == clan.Kingdom && ((MakePeaceKingdomDecision)kingdomDecisions).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
                        {
                            triggerTime = kingdomDecisions.TriggerTime;
                            if (triggerTime.IsFuture)
                                flag1 = true;
                        }
                    }
                }
                if (flag1)
                    return;
                bool flag2 = false;
                foreach (KingdomDecision kingdomDecisions in _kingdomDecisionsList)
                {
                    if (kingdomDecisions is DeclareWarDecision declareWarDecision1 && kingdomDecision is DeclareWarDecision declareWarDecision2 && declareWarDecision1.FactionToDeclareWarOn == declareWarDecision2.FactionToDeclareWarOn && declareWarDecision1.ProposerClan.MapFaction == declareWarDecision2.ProposerClan.MapFaction)
                        flag2 = true;
                    else if (kingdomDecisions is MakePeaceKingdomDecision peaceKingdomDecision1 && kingdomDecision is MakePeaceKingdomDecision peaceKingdomDecision2 && peaceKingdomDecision1.FactionToMakePeaceWith == peaceKingdomDecision2.FactionToMakePeaceWith && peaceKingdomDecision1.ProposerClan.MapFaction == peaceKingdomDecision2.ProposerClan.MapFaction)
                        flag2 = true;
                }
                if (flag2)
                    return;
                _kingdomDecisionsList.Add(kingdomDecision);
                KingdomElection kingdomElection = new KingdomElection(kingdomDecision);
                clan.Kingdom.AddDecision(kingdomDecision);
            }
            else
                UpdateKingdomDecisions(clan.Kingdom);
        }

        private void HourlyTick()
        {
            if (Clan.PlayerClan.Kingdom == null)
                return;
            UpdateKingdomDecisions(Clan.PlayerClan.Kingdom);
        }

        private void DailyTick()
        {
            if (_kingdomDecisionsList == null)
                return;
            int count = _kingdomDecisionsList.Count;
            int num = 0;
            for (int index = 0; index < count; ++index)
            {
                if (_kingdomDecisionsList[index - num].TriggerTime.ElapsedDaysUntilNow > 15.0)
                {
                    _kingdomDecisionsList.RemoveAt(index - num);
                    ++num;
                }
            }
        }

        public void UpdateKingdomDecisions(Kingdom kingdom)
        {
            List<KingdomDecision> kingdomDecisionList1 = new List<KingdomDecision>();
            List<KingdomDecision> kingdomDecisionList2 = new List<KingdomDecision>();
            foreach (KingdomDecision unresolvedDecision in kingdom.UnresolvedDecisions)
            {
                if (unresolvedDecision.ShouldBeCancelled())
                    kingdomDecisionList1.Add(unresolvedDecision);
                else if (unresolvedDecision.TriggerTime.IsPast && !unresolvedDecision.NeedsPlayerResolution)
                    kingdomDecisionList2.Add(unresolvedDecision);
            }
            foreach (KingdomDecision kingdomDecision in kingdomDecisionList1)
            {
                kingdom.RemoveDecision(kingdomDecision);
                bool isPlayerInvolved = kingdomDecision.DetermineChooser().Leader.IsHumanPlayerCharacter || kingdomDecision.DetermineSupporters().Any(x => x.IsPlayer);
                CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(kingdomDecision, isPlayerInvolved);
            }
            foreach (KingdomDecision decision in kingdomDecisionList2)
                new KingdomElection(decision).StartElectionWithoutPlayer();
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
            if (!side1Faction.IsKingdomFaction || !side2Faction.IsKingdomFaction)
                return;
            UpdateKingdomDecisions((Kingdom)side1Faction);
            UpdateKingdomDecisions((Kingdom)side2Faction);
        }

        private KingdomDecision GetRandomWarDecision(Clan clan)
        {
            KingdomDecision randomWarDecision = null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault(x => x is DeclareWarDecision) != null) return null;
            
            Kingdom elementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(x => x != kingdom && !x.IsAtWarWith(kingdom) && x.GetStanceWith(kingdom).PeaceDeclarationDate.ElapsedDaysUntilNow > 20.0);
            
            if (elementWithPredicate != null && ConsiderWar(clan, kingdom, elementWithPredicate))
                randomWarDecision = new TORDeclareWarDecision(clan, elementWithPredicate);
            return randomWarDecision;
        }
        
        private bool ConsiderWar(Clan clan, Kingdom kingdom, IFaction otherFaction)
        {
            double influenceCost = (double)Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(kingdom) / 2;
            if (clan.Influence < influenceCost) return false;
            
            TORDeclareWarDecision decision = new TORDeclareWarDecision(clan, otherFaction);
            float support = decision.CalculateSupport(clan);
            return support > 50.0 && MBRandom.RandomFloat < 1.4 * GetKingdomSupportForDecision(decision) - 0.55;
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
        
        private bool ConsiderPeace(Clan clan, Clan otherClan, Kingdom kingdom, IFaction otherFaction, out MakePeaceKingdomDecision decision)
        {
            decision = null;
            int ofProposingPeace = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace();
            if (clan.Influence < (double)ofProposingPeace)
                return false;
            int num1 = new PeaceBarterable(clan.Leader, kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction(otherFaction);
            int num2 = -num1;
            int num3;
            if (clan.MapFaction == Hero.MainHero.MapFaction && otherFaction is Kingdom)
            {
                foreach (Clan clan1 in ((Kingdom)otherFaction).Clans)
                {
                    if (clan1.Leader != clan1.MapFaction.Leader)
                    {
                        int valueForFaction = new PeaceBarterable(clan1.Leader, kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction(clan1);
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
                PeaceBarterable peaceBarterable = new PeaceBarterable(clan.MapFaction.Leader, kingdom, otherFaction, CampaignTime.Years(1f));
                int num5 = peaceBarterable.GetValueForFaction(clan.MapFaction);
                int num6 = 0;
                int num7 = 1;
                if (clan.MapFaction is Kingdom)
                {
                    foreach (Clan clan2 in ((Kingdom)clan.MapFaction).Clans)
                    {
                        if (clan2.Leader != clan2.MapFaction.Leader)
                        {
                            int valueForFaction = peaceBarterable.GetValueForFaction(clan2);
                            if (valueForFaction < num5)
                                num5 = valueForFaction;
                            num6 += valueForFaction;
                            ++num7;
                        }
                    }
                }
                int num8 = (int)(0.6499999761581421 * (num6 / (float)num7) + 0.3499999940395355 * num5);
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
            if (decision.CalculateSupport(clan) <= 5.0 || MBRandom.RandomFloat >= 2.0 * (GetKingdomSupportForDecision(decision) - (double)num4))
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

        private KingdomDecision GetRandomPolicyDecision(Clan clan)
        {
            KingdomDecision randomPolicyDecision = null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault(x => x is KingdomPolicyDecision) != null)
                return null;
            if (clan.Influence < 200.0)
                return null;
            PolicyObject randomElement = PolicyObject.All.GetRandomElement();
            bool flag = kingdom.ActivePolicies.Contains(randomElement);
            if (ConsiderPolicy(clan, kingdom, randomElement, flag))
                randomPolicyDecision = new KingdomPolicyDecision(clan, randomElement, flag);
            return randomPolicyDecision;
        }

        private bool ConsiderPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
        {
            int proposalAndDisavowal = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal();
            if (clan.Influence < (double)proposalAndDisavowal)
                return false;
            KingdomPolicyDecision decision = new KingdomPolicyDecision(clan, policy, invert);
            return decision.CalculateSupport(clan) > 50.0 && MBRandom.RandomFloat < GetKingdomSupportForDecision(decision) - 0.55;
        }

        private KingdomDecision GetRandomAnnexationDecision(Clan clan)
        {
            KingdomDecision annexationDecision = null;
            Kingdom kingdom = clan.Kingdom;
            if (kingdom.UnresolvedDecisions.FirstOrDefault(x => x is KingdomPolicyDecision) != null)
                return null;
            if (clan.Influence < 300.0)
                return null;
            Clan randomElement1 = kingdom.Clans.GetRandomElement();
            if (randomElement1 != null && randomElement1 != clan && randomElement1.GetRelationWithClan(clan) < -25)
            {
                if (randomElement1.Fiefs.Count == 0)
                    return null;
                Town randomElement2 = randomElement1.Fiefs.GetRandomElement();
                if (ConsiderAnnex(clan, kingdom, randomElement1, randomElement2))
                    annexationDecision = new SettlementClaimantPreliminaryDecision(clan, randomElement2.Settlement);
            }
            return annexationDecision;
        }

        private bool ConsiderAnnex(Clan clan, Kingdom kingdom, Clan targetClan, Town targetSettlement)
        {
            int costOfAnnexation = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(kingdom);
            if (clan.Influence < (double)costOfAnnexation)
                return false;
            SettlementClaimantPreliminaryDecision decision = new SettlementClaimantPreliminaryDecision(clan, targetSettlement.Settlement);
            return decision.CalculateSupport(clan) > 50.0 && MBRandom.RandomFloat < GetKingdomSupportForDecision(decision) - 0.6;
        }

        private float GetKingdomSupportForDecision(KingdomDecision decision) => new KingdomElection(decision).GetLikelihoodForOutcome(0);

        private void SessionLaunched(CampaignGameStarter starter)
        {
        }

        public override void SyncData(IDataStore dataStore) => dataStore.SyncData("_kingdomDecisionsList", ref _kingdomDecisionsList);
    }
}
