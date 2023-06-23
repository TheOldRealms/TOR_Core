using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Quests
{
    public class HuntCultistsQuestCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if(party == MobileParty.MainParty)
            {
                Hero master = settlement.HeroesWithoutParty.FirstOrDefault(x => x.IsBountyMaster());
                if(master != null && master.Issue == null)
                {
                    Campaign.Current.IssueManager.CreateNewIssue(new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnIssueSelected), typeof(HuntCultistsIssue), IssueBase.IssueFrequency.VeryCommon), master);
                }
            }
        }

        private void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnIssueSelected), typeof(HuntCultistsIssue), IssueBase.IssueFrequency.VeryCommon));
            }
        }

        private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            PotentialIssueData potentialIssueData = pid;
            Settlement targetSettlement = TORCommon.FindSettlementsAroundPosition(issueOwner.CurrentSettlement.Position2D, 100f, x => x.IsVillage).GetRandomElementInefficiently();
            if (targetSettlement == null) targetSettlement = Settlement.FindAll(x => x.IsVillage).GetRandomElementInefficiently();
            return new HuntCultistsIssue(issueOwner, targetSettlement);
        }

        private bool ConditionsHold(Hero issueGiver)
        {
            return issueGiver != null && issueGiver.IsBountyMaster();
        }

        public override void SyncData(IDataStore dataStore) { }

        public class HuntCultistsIssue : IssueBase
        {
            [SaveableField(0)]
            private Settlement _targetSettlement;

            public HuntCultistsIssue(Hero issueOwner, Settlement targetSettlement) : base(issueOwner, CampaignTime.DaysFromNow(1f))
            {
                _targetSettlement = targetSettlement;
            }

            protected override int RewardGold => 2500;

            public override TextObject IssueBriefByIssueGiver => new TextObject("{=!}As a matter of fact, I have a lead on a potential cultist. A grave accusation that needs investigating.");

            public override TextObject IssueAcceptByPlayer => new TextObject("{=!}What needs to be done?");

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get
                {
                    TextObject textObject = new TextObject("{=!}I need you to travel to {TARGET_SETTLEMENT}. Investigate the local populace and root out any cultists. On successful completion, the order will pay you {REWARD}{GOLD_ICON}.", null);
                    textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("REWARD", RewardGold);
                    textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    return textObject;
                }
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=!}Consider it done.");

            public override bool IsThereAlternativeSolution => false;

            public override bool IsThereLordSolution => false;

            public override TextObject Title => new TextObject("{=!}A cultist in our midst");

            public override TextObject Description
            {
                get
                {
                    TextObject textObject = new TextObject("{=!}Travel to target settlement and root out any cultist who may be hiding there.", null);
                    textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
                    return textObject;
                }
            }

            public override IssueFrequency GetFrequency() => IssueFrequency.VeryCommon;

            public override bool IssueStayAliveConditions()
            {
                return IssueOwner != null && !IssueOwner.CurrentSettlement.MapFaction.IsAtWarWith(Clan.PlayerClan);
            }

            protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero relationHero, out SkillObject skill)
            {
                flag = PreconditionFlags.None;
                relationHero = issueGiver;
                skill = null;
                IFaction mapfaction = issueGiver.MapFaction;
                if (mapfaction == null) mapfaction = issueGiver.CurrentSettlement?.MapFaction;
                if (mapfaction != null && mapfaction.Leader.GetDominantReligion() != null && mapfaction.Leader.GetDominantReligion().HostileReligions.Contains(Hero.MainHero.GetDominantReligion()))
                {
                    flag |= PreconditionFlags.Relation;
                }
                if (mapfaction != null && mapfaction.IsAtWarWith(Hero.MainHero.MapFaction))
                {
                    flag |= PreconditionFlags.AtWar;
                }
                return flag == PreconditionFlags.None;
            }

            //not used - anywhere, even in native, but abstract implementation requires it
            protected override void CompleteIssueWithTimedOutConsequences() { }

            protected override QuestBase GenerateIssueQuest(string questId)
            {
                return new HuntCultistsQuest("hunt_cultists_quest_" + CampaignTime.Now.ElapsedSecondsUntilNow, IssueOwner, CampaignTime.DaysFromNow(1f), RewardGold, _targetSettlement);
            }

            protected override void OnGameLoad() { }

            protected override void HourlyTick() { }
        }

        public class HuntCultistsQuest : QuestBase
        {
            [SaveableField(1)]
            Settlement _settlement;

            public HuntCultistsQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, Settlement targetSettlement) : base(questId, questGiver, duration, rewardGold)
            {
                _settlement = targetSettlement;
                SetDialogs();
                InitializeQuestOnCreation();
            }

            public override TextObject Title => new TextObject("{=!}A cultist in our midst");

            public override bool IsRemainingTimeHidden => false;

            protected override void HourlyTick() { }

            protected override void InitializeQuestOnGameLoad()
            {
                SetDialogs();
            }

            protected override void SetDialogs()
            {
                OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(new TextObject("{=!}Excellent. Do not underestimate the ruinous powers, unwavering vigilance is required on your quest!", null), null, null).Condition(() => Hero.OneToOneConversationHero == QuestGiver).Consequence(OnQuestAccepted).CloseDialog();
                DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(new TextObject("{=!}It was good doing business with you.", null), null, null).Condition(() => Hero.OneToOneConversationHero == QuestGiver).CloseDialog();
            }

            protected override void OnTimedOut()
            {
                AddLog(new TextObject("{=!}You failed to complete the investigation in time. Any potential cultists are surely in the wind now."));
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, SettlementEntered);
            }

            private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
            {
                return;
            }

            private void OnQuestAccepted()
            {
                StartQuest();
                var acceptLog = new TextObject("{=!}You were tasked to travel to {TARGET_SETTLEMENT} and root out any cultist who may be hiding there.");
                acceptLog.SetTextVariable("TARGET_SETTLEMENT", _settlement.EncyclopediaLinkWithName);
                AddLog(acceptLog);
            }
        }
    }
}
