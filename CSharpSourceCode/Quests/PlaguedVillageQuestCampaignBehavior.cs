using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TOR_Core.Ink;

namespace TOR_Core.Quests
{
    public class PlaguedVillageQuestCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party == MobileParty.MainParty)
            {
                Hero master = settlement.HeroesWithoutParty.FirstOrDefault(x => x.IsBountyMaster());
                int rng = MBRandom.RandomInt(0, 100);
                if (master != null && master.Issue == null && rng < TORConstants.BOUNTY_QUEST_CHANCE)
                {
                    Campaign.Current.IssueManager.CreateNewIssue(new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnIssueSelected), typeof(PlaguedVillageIssue), IssueBase.IssueFrequency.VeryCommon), master);
                }
            }
        }

        private void OnCheckForIssue(Hero hero)
        {
            if (ConditionsHold(hero))
            {
                Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(new PotentialIssueData.StartIssueDelegate(OnIssueSelected), typeof(PlaguedVillageIssue), IssueBase.IssueFrequency.VeryCommon));
            }
        }

        private IssueBase OnIssueSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            PotentialIssueData potentialIssueData = pid;
            Settlement targetSettlement = TORCommon.FindSettlementsAroundPosition(issueOwner.CurrentSettlement.Position2D, 100f, x => x.IsVillage && x.Culture == issueOwner.Culture && !x.IsRaided && !x.IsUnderRaid).GetRandomElementInefficiently();
            if (targetSettlement == null) targetSettlement = Settlement.FindAll(x => x.IsVillage && x.Culture == issueOwner.Culture && !x.IsRaided && !x.IsUnderRaid).GetRandomElementInefficiently();
            if (targetSettlement == null) targetSettlement = Settlement.FindAll(x => x.IsVillage && !x.IsRaided && !x.IsUnderRaid).GetRandomElementInefficiently();
            return new PlaguedVillageIssue(issueOwner, targetSettlement);
        }

        private bool ConditionsHold(Hero issueGiver)
        {
            return issueGiver != null && issueGiver.IsBountyMaster();
        }

        public override void SyncData(IDataStore dataStore) { }

        public class PlaguedVillageIssue : IssueBase
        {
            [SaveableField(0)]
            private Settlement _targetSettlement;

            public PlaguedVillageIssue(Hero issueOwner, Settlement targetSettlement) : base(issueOwner, CampaignTime.DaysFromNow(30f))
            {
                _targetSettlement = targetSettlement;
            }

            protected override int RewardGold => 2500;

            public override TextObject IssueBriefByIssueGiver => new TextObject("{=!}As a matter of fact, I have a lead on potential cultist activity. A village is struck by a terrible, unnatural plague. A grave matter that needs investigating.");

            public override TextObject IssueAcceptByPlayer => new TextObject("{=!}What needs to be done?");

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get
                {
                    TextObject textObject = new TextObject("{=!}I need you to travel to {TARGET_SETTLEMENT}. Investigate the situation and find root cause of the plague. On successful completion, the order will pay you {REWARD}{GOLD_ICON}.", null);
                    textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("REWARD", RewardGold);
                    textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    return textObject;
                }
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer => new TextObject("{=!}Consider it done.");

            public override bool IsThereAlternativeSolution => false;

            public override bool IsThereLordSolution => false;

            public override TextObject Title => new TextObject("{=!}The plague ridden village");

            public override TextObject Description
            {
                get
                {
                    TextObject textObject = new TextObject("{=!}Travel to target settlement and find the cause of the unnatural epidemic that plagues the village.", null);
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
                return new PlaguedVillageQuest("plagued_village_quest_" + CampaignTime.Now.ElapsedSecondsUntilNow, IssueOwner, CampaignTime.DaysFromNow(30f), RewardGold, _targetSettlement);
            }

            protected override void OnGameLoad() { }

            protected override void HourlyTick() { }
        }

        public class PlaguedVillageQuest : QuestBase
        {
            [SaveableField(1)]
            Settlement _settlement;
            [SaveableField(2)]
            bool _storyPlayed;
            [SaveableField(3)]
            bool _dealtWithCultists;

            public PlaguedVillageQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, Settlement targetSettlement) : base(questId, questGiver, duration, rewardGold)
            {
                _settlement = targetSettlement;
                SetDialogs();
                InitializeQuestOnCreation();
            }

            public override TextObject Title => new TextObject("{=!}The plague ridden village");

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
                AddLog(new TextObject("{=!}You failed to complete the investigation in time."));
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, SettlementEntered);
            }

            private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
            {
                if (party == MobileParty.MainParty && settlement == _settlement && !_storyPlayed)
                {
                    if (settlement.IsUnderRaid || settlement.IsRaided)
                    {
                        InquiryData data = new InquiryData("Village Raided", "The village is raided, no chance to find any cultists now. Come back when the village is repopulated.", true, false, "OK", null, () => InformationManager.HideInquiry(), null);
                        InformationManager.ShowInquiry(data);
                    }
                    else InkStoryManager.OpenStory("NurgleCultists", AfterStory);
                }
            }

            private void AfterStory(InkStory story)
            {
                _storyPlayed = true;
                bool.TryParse(story.GetVariable("DealtWithCultists"), out _dealtWithCultists);
                if (_dealtWithCultists)
                {
                    AddLog(new TextObject("{=!}You were successful in lifting the plague."));
                    CompleteQuestWithSuccess();
                }
                else CompleteQuestWithFail(new TextObject("{=!}You abandoned the mission."));
            }

            private void OnQuestAccepted()
            {
                StartQuest();
                this.QuestDueTime = CampaignTime.Now + CampaignTime.Days(20);
                var acceptLog = new TextObject("{=!}You were tasked to travel to {TARGET_SETTLEMENT} and investigate the epidemic that plagues it.");
                acceptLog.SetTextVariable("TARGET_SETTLEMENT", _settlement.EncyclopediaLinkWithName);
                AddLog(acceptLog);
            }

            protected override void OnCompleteWithSuccess()
            {
                GiveGoldAction.ApplyForQuestBetweenCharacters(null, Hero.MainHero, RewardGold);
            }
        }
    }
}
