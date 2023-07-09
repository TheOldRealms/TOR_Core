// Decompiled with JetBrains decompiler
// Type: TaleWorlds.CampaignSystem.CampaignBehaviors.KingdomDecisionProposalBehavior
// Assembly: TaleWorlds.CampaignSystem, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5258A0C3-6337-4AB0-A110-152B92C40F90
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll

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

    // This is a, largely unchanged, copy of KingdomDecisionProposalBehavior
    public class TORKingdomDecisionProposalBehavior : CampaignBehaviorBase
    {
       private const int KingdomDecisionProposalCooldownInDays = 1;
    private const float ClanInterestModifier = 1f;
    private const float DecisionSuccessChanceModifier = 1f;
    private List<KingdomDecision> _kingdomDecisionsList;

    public override void RegisterEvents()
    {
      CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object) this, new Action<CampaignGameStarter>(this.SessionLaunched));
      CampaignEvents.DailyTickClanEvent.AddNonSerializedListener((object) this, new Action<Clan>(this.DailyTickClan));
      CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object) this, new Action(this.HourlyTick));
      CampaignEvents.DailyTickEvent.AddNonSerializedListener((object) this, new Action(this.DailyTick));
      CampaignEvents.MakePeace.AddNonSerializedListener((object) this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceMade));
      CampaignEvents.WarDeclared.AddNonSerializedListener((object) this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
    }

    private void DailyTickClan(Clan clan)
    {
      if ((double) (int) Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < 5.0 || clan.IsEliminated || clan == Clan.PlayerClan || (double) clan.TotalStrength <= 0.0 || clan.IsBanditFaction || clan.Kingdom == null || (double) clan.Influence < 100.0)
        return;
      KingdomDecision kingdomDecision = (KingdomDecision) null;
      float randomFloat = MBRandom.RandomFloat;
      float num = MathF.Min(0.33f, (float) (1.0 / ((double) ((Kingdom) clan.MapFaction).Clans.Count<Clan>((Func<Clan, bool>) (x => (double) x.Influence > 100.0)) + 2.0))) * (clan.Kingdom != Hero.MainHero.MapFaction || Hero.MainHero.Clan.IsUnderMercenaryService ? 1f : (clan.Kingdom.Leader == Hero.MainHero ? 0.5f : 0.75f));
      DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
      if ((double) randomFloat < (double) num && (double) clan.Influence > (double) diplomacyModel.GetInfluenceCostOfProposingPeace(clan))
        kingdomDecision = this.GetRandomPeaceDecision(clan);
      else if ((double) randomFloat < (double) num * 2.0 && (double) clan.Influence > (double) diplomacyModel.GetInfluenceCostOfProposingWar(clan))
        kingdomDecision = this.GetRandomWarDecision(clan);
      else if ((double) randomFloat < (double) num * 2.5 && (double) clan.Influence > (double) (diplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan) * 4))
        kingdomDecision = this.GetRandomPolicyDecision(clan);
      else if ((double) randomFloat < (double) num * 3.0 && (double) clan.Influence > 700.0)
        kingdomDecision = this.GetRandomAnnexationDecision(clan);
      if (kingdomDecision != null)
      {
        if (this._kingdomDecisionsList == null)
          this._kingdomDecisionsList = new List<KingdomDecision>();
        bool flag1 = false;
        if (kingdomDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision) kingdomDecision).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
        {
          foreach (KingdomDecision kingdomDecisions in this._kingdomDecisionsList)
          {
            CampaignTime triggerTime;
            if (kingdomDecisions is MakePeaceKingdomDecision && kingdomDecisions.Kingdom == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision) kingdomDecisions).FactionToMakePeaceWith == clan.Kingdom)
            {
              triggerTime = kingdomDecisions.TriggerTime;
              if (triggerTime.IsFuture)
                flag1 = true;
            }
            if (kingdomDecisions is MakePeaceKingdomDecision && kingdomDecisions.Kingdom == clan.Kingdom && ((MakePeaceKingdomDecision) kingdomDecisions).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
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
        foreach (KingdomDecision kingdomDecisions in this._kingdomDecisionsList)
        {
          if (kingdomDecisions is DeclareWarDecision declareWarDecision1 && kingdomDecision is DeclareWarDecision declareWarDecision2 && declareWarDecision1.FactionToDeclareWarOn == declareWarDecision2.FactionToDeclareWarOn && declareWarDecision1.ProposerClan.MapFaction == declareWarDecision2.ProposerClan.MapFaction)
            flag2 = true;
          else if (kingdomDecisions is MakePeaceKingdomDecision peaceKingdomDecision1 && kingdomDecision is MakePeaceKingdomDecision peaceKingdomDecision2 && peaceKingdomDecision1.FactionToMakePeaceWith == peaceKingdomDecision2.FactionToMakePeaceWith && peaceKingdomDecision1.ProposerClan.MapFaction == peaceKingdomDecision2.ProposerClan.MapFaction)
            flag2 = true;
        }
        if (flag2)
          return;
        this._kingdomDecisionsList.Add(kingdomDecision);
        KingdomElection kingdomElection = new KingdomElection(kingdomDecision);
        clan.Kingdom.AddDecision(kingdomDecision);
      }
      else
        this.UpdateKingdomDecisions(clan.Kingdom);
    }

    private void HourlyTick()
    {
      if (Clan.PlayerClan.Kingdom == null)
        return;
      this.UpdateKingdomDecisions(Clan.PlayerClan.Kingdom);
    }

    private void DailyTick()
    {
      if (this._kingdomDecisionsList == null)
        return;
      int count = this._kingdomDecisionsList.Count;
      int num = 0;
      for (int index = 0; index < count; ++index)
      {
        if ((double) this._kingdomDecisionsList[index - num].TriggerTime.ElapsedDaysUntilNow > 15.0)
        {
          this._kingdomDecisionsList.RemoveAt(index - num);
          ++num;
        }
      }
    }

    public void UpdateKingdomDecisions(Kingdom kingdom)
    {
      List<KingdomDecision> kingdomDecisionList1 = new List<KingdomDecision>();
      List<KingdomDecision> kingdomDecisionList2 = new List<KingdomDecision>();
      foreach (KingdomDecision unresolvedDecision in (List<KingdomDecision>) kingdom.UnresolvedDecisions)
      {
        if (unresolvedDecision.ShouldBeCancelled())
          kingdomDecisionList1.Add(unresolvedDecision);
        else if (unresolvedDecision.TriggerTime.IsPast && !unresolvedDecision.NeedsPlayerResolution)
          kingdomDecisionList2.Add(unresolvedDecision);
      }
      foreach (KingdomDecision kingdomDecision in kingdomDecisionList1)
      {
        kingdom.RemoveDecision(kingdomDecision);
        bool isPlayerInvolved = kingdomDecision.DetermineChooser().Leader.IsHumanPlayerCharacter || kingdomDecision.DetermineSupporters().Any<Supporter>((Func<Supporter, bool>) (x => x.IsPlayer));
        CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(kingdomDecision, isPlayerInvolved);
      }
      foreach (KingdomDecision decision in kingdomDecisionList2)
        new KingdomElection(decision).StartElectionWithoutPlayer();
    }

    private void OnPeaceMade(
      IFaction side1Faction,
      IFaction side2Faction,
      MakePeaceAction.MakePeaceDetail detail)
    {
      this.HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
    }

    private void OnWarDeclared(
      IFaction side1Faction,
      IFaction side2Faction,
      DeclareWarAction.DeclareWarDetail detail)
    {
      this.HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
    }

    private void HandleDiplomaticChangeBetweenFactions(IFaction side1Faction, IFaction side2Faction)
    {
      if (!side1Faction.IsKingdomFaction || !side2Faction.IsKingdomFaction)
        return;
      this.UpdateKingdomDecisions((Kingdom) side1Faction);
      this.UpdateKingdomDecisions((Kingdom) side2Faction);
    }

    private KingdomDecision GetRandomWarDecision(Clan clan)
    {
      KingdomDecision randomWarDecision = null;
      Kingdom kingdom = clan.Kingdom;
      if (kingdom.UnresolvedDecisions.FirstOrDefault(x => x is DeclareWarDecision) != null)
        return null;
      Kingdom elementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(x => !x.IsEliminated && x != kingdom && !x.IsAtWarWith(kingdom) && x.GetStanceWith(kingdom).PeaceDeclarationDate.ElapsedDaysUntilNow > 20.0);
      if (elementWithPredicate != null && ConsiderWar(clan, kingdom, elementWithPredicate))
        randomWarDecision = new TORDeclareWarDecision(clan, elementWithPredicate);
      return randomWarDecision;
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

    private bool ConsiderWar(Clan clan, Kingdom kingdom, IFaction otherFaction)
    {
      int num = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(clan) / 2;
      if (clan.Influence < (double) num)
        return false;
      TORDeclareWarDecision decision = new TORDeclareWarDecision(clan, otherFaction);
      return decision.CalculateSupport(clan) > 50.0 && MBRandom.RandomFloat < 1.399999976158142 * GetKingdomSupportForDecision(decision) - 0.550000011920929;
    }

    private float GetKingdomSupportForWar(Clan clan, Kingdom kingdom, IFaction otherFaction) => new KingdomElection((KingdomDecision) new TORDeclareWarDecision(clan, otherFaction)).GetLikelihoodForSponsor(clan);

    private bool ConsiderPeace(
      Clan clan,
      Clan otherClan,
      Kingdom kingdom,
      IFaction otherFaction,
      out MakePeaceKingdomDecision decision)
    {
      decision = (MakePeaceKingdomDecision) null;
      int ofProposingPeace = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace(clan);
      if ((double) clan.Influence < (double) ofProposingPeace)
        return false;
      int num1 = new PeaceBarterable(clan.Leader, (IFaction) kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction(otherFaction);
      int num2 = -num1;
      int num3;
      if (clan.MapFaction == Hero.MainHero.MapFaction && otherFaction is Kingdom)
      {
        foreach (Clan clan1 in (List<Clan>) ((Kingdom) otherFaction).Clans)
        {
          if (clan1.Leader != clan1.MapFaction.Leader)
          {
            int valueForFaction = new PeaceBarterable(clan1.Leader, (IFaction) kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction((IFaction) clan1);
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
        PeaceBarterable peaceBarterable = new PeaceBarterable(clan.MapFaction.Leader, (IFaction) kingdom, otherFaction, CampaignTime.Years(1f));
        int num5 = peaceBarterable.GetValueForFaction(clan.MapFaction);
        int num6 = 0;
        int num7 = 1;
        if (clan.MapFaction is Kingdom)
        {
          foreach (Clan clan2 in (List<Clan>) ((Kingdom) clan.MapFaction).Clans)
          {
            if (clan2.Leader != clan2.MapFaction.Leader)
            {
              int valueForFaction = peaceBarterable.GetValueForFaction((IFaction) clan2);
              if (valueForFaction < num5)
                num5 = valueForFaction;
              num6 += valueForFaction;
              ++num7;
            }
          }
        }
        int num8 = (int) (0.6499999761581421 * (double) ((float) num6 / (float) num7) + 0.3499999940395355 * (double) num5);
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
      if ((double) decision.CalculateSupport(clan) <= 5.0 || (double) MBRandom.RandomFloat >= 2.0 * ((double) this.GetKingdomSupportForDecision((KingdomDecision) decision) - (double) num4))
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
      int num2 = -new PeaceBarterable(clan.Leader, (IFaction) kingdom, otherFaction, CampaignTime.Years(1f)).GetValueForFaction(otherFaction);
      if (otherFaction is Clan && num2 < 0)
        num2 = 0;
      if (num2 > -5000 && num2 < 5000)
        num2 = 0;
      int dailyTributeForValue = Campaign.Current.Models.DiplomacyModel.GetDailyTributeForValue(num2);
      return new KingdomElection((KingdomDecision) new MakePeaceKingdomDecision(clan, otherFaction, dailyTributeForValue)).GetLikelihoodForSponsor(clan);
    }

    private KingdomDecision GetRandomPolicyDecision(Clan clan)
    {
      KingdomDecision randomPolicyDecision = (KingdomDecision) null;
      Kingdom kingdom = clan.Kingdom;
      if (kingdom.UnresolvedDecisions.FirstOrDefault<KingdomDecision>((Func<KingdomDecision, bool>) (x => x is KingdomPolicyDecision)) != null)
        return (KingdomDecision) null;
      if ((double) clan.Influence < 200.0)
        return (KingdomDecision) null;
      PolicyObject randomElement = PolicyObject.All.GetRandomElement<PolicyObject>();
      bool flag = kingdom.ActivePolicies.Contains(randomElement);
      if (this.ConsiderPolicy(clan, kingdom, randomElement, flag))
        randomPolicyDecision = (KingdomDecision) new KingdomPolicyDecision(clan, randomElement, flag);
      return randomPolicyDecision;
    }

    private bool ConsiderPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
    {
      int proposalAndDisavowal = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
      if ((double) clan.Influence < (double) proposalAndDisavowal)
        return false;
      KingdomPolicyDecision decision = new KingdomPolicyDecision(clan, policy, invert);
      return (double) decision.CalculateSupport(clan) > 50.0 && (double) MBRandom.RandomFloat < (double) this.GetKingdomSupportForDecision((KingdomDecision) decision) - 0.55;
    }

    private float GetKingdomSupportForPolicy(
      Clan clan,
      Kingdom kingdom,
      PolicyObject policy,
      bool invert)
    {
      Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
      return new KingdomElection((KingdomDecision) new KingdomPolicyDecision(clan, policy, invert)).GetLikelihoodForSponsor(clan);
    }

    private KingdomDecision GetRandomAnnexationDecision(Clan clan)
    {
      KingdomDecision annexationDecision = (KingdomDecision) null;
      Kingdom kingdom = clan.Kingdom;
      if (kingdom.UnresolvedDecisions.FirstOrDefault<KingdomDecision>((Func<KingdomDecision, bool>) (x => x is KingdomPolicyDecision)) != null)
        return (KingdomDecision) null;
      if ((double) clan.Influence < 300.0)
        return (KingdomDecision) null;
      Clan randomElement1 = kingdom.Clans.GetRandomElement<Clan>();
      if (randomElement1 != null && randomElement1 != clan && randomElement1.GetRelationWithClan(clan) < -25)
      {
        if (randomElement1.Fiefs.Count == 0)
          return (KingdomDecision) null;
        Town randomElement2 = randomElement1.Fiefs.GetRandomElement<Town>();
        if (this.ConsiderAnnex(clan, randomElement2))
          annexationDecision = (KingdomDecision) new SettlementClaimantPreliminaryDecision(clan, randomElement2.Settlement);
      }
      return annexationDecision;
    }

    private bool ConsiderAnnex(Clan clan, Town targetSettlement)
    {
      int costOfAnnexation = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(clan);
      if ((double) clan.Influence < (double) costOfAnnexation)
        return false;
      SettlementClaimantPreliminaryDecision decision = new SettlementClaimantPreliminaryDecision(clan, targetSettlement.Settlement);
      return (double) decision.CalculateSupport(clan) > 50.0 && (double) MBRandom.RandomFloat < (double) this.GetKingdomSupportForDecision((KingdomDecision) decision) - 0.6;
    }

    private float GetKingdomSupportForDecision(KingdomDecision decision) => new KingdomElection(decision).GetLikelihoodForOutcome(0);

    private void SessionLaunched(CampaignGameStarter starter)
    {
    }

    public override void SyncData(IDataStore dataStore) => dataStore.SyncData<List<KingdomDecision>>("_kingdomDecisionsList", ref this._kingdomDecisionsList);

    private delegate KingdomDecision KingdomDecisionCreatorDelegate(Clan sponsorClan);
    }
}
