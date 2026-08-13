using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Ink;
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
            if (party == MobileParty.MainParty)
            {
                Hero master = settlement.HeroesWithoutParty.FirstOrDefault(x => x.IsBountyMaster());
                int rng = MBRandom.RandomInt(0, 100);
                if (master != null && master.Issue == null && rng < TORConstants.BOUNTY_QUEST_CHANCE)
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
            Settlement targetSettlement = TORCommon.FindSettlementsAroundPosition(issueOwner.CurrentSettlement.Position.ToVec2(), 100f, x => x.IsVillage && x.Culture == issueOwner.Culture && !x.IsRaided && !x.IsUnderRaid).GetRandomElementInefficiently();
            if (targetSettlement == null) targetSettlement = Settlement.FindAll(x => x.IsVillage && x.Culture == issueOwner.Culture && !x.IsRaided && !x.IsUnderRaid).GetRandomElementInefficiently();
            if (targetSettlement == null) targetSettlement = Settlement.FindAll(x => x.IsVillage && !x.IsRaided && !x.IsUnderRaid).GetRandomElementInefficiently();
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

            public HuntCultistsIssue(Hero issueOwner, Settlement targetSettlement) : base(issueOwner, CampaignTime.DaysFromNow(30f))
            {
                _targetSettlement = targetSettlement;
            }

            protected override int RewardGold => 2500;

            public override TextObject IssueBriefByIssueGiver => TORTextHelper.GetTextObject("tor_quest_hunt_cultist_issue_brief", "As a matter of fact, I have a lead on a potential cultist. A grave accusation that needs investigating.");

            public override TextObject IssueAcceptByPlayer => TORTextHelper.GetTextObject("tor_quest_hunt_cultist_issue_accept_player", "What needs to be done?");

            public override TextObject IssueQuestSolutionExplanationByIssueGiver
            {
                get
                {
                    TextObject textObject = TORTextHelper.GetTextObject("tor_quest_hunt_cultist_issue_explanation", "I need you to travel to {TARGET_SETTLEMENT}. Investigate the local populace and root out any cultists. On successful completion, the order will pay you {REWARD}{GOLD_ICON}.");
                    textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("REWARD", RewardGold);
                    textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    return textObject;
                }
            }

            public override TextObject IssueQuestSolutionAcceptByPlayer => TORTextHelper.GetTextObject("tor_quest_hunt_cultist_accept_player", "Consider it done.");

            public override bool IsThereAlternativeSolution => false;

            public override bool IsThereLordSolution => false;

            public override TextObject Title => TORTextHelper.GetTextObject("tor_quest_hunt_cultist_issue_title", "A cultist in our midst");

            public override TextObject Description
            {
                get
                {
                    TextObject textObject = TORTextHelper.GetTextObject("tor_quest_hunt_cultist_description", "Travel to {TARGET_SETTLEMENT} and root out any cultist who may be hiding there.");
                    textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
                    return textObject;
                }
            }

            public override IssueFrequency GetFrequency() => IssueFrequency.VeryCommon;

            public override bool IssueStayAliveConditions()
            {
                var issueOwner = IssueOwner;
                var currentSettlement = issueOwner?.CurrentSettlement;
                var mapFaction = currentSettlement?.MapFaction;

                return mapFaction != null && !mapFaction.IsAtWarWith(Clan.PlayerClan);
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
                return new HuntCultistsQuest("hunt_cultists_quest_" + CampaignTime.Now.ElapsedSecondsUntilNow, IssueOwner, CampaignTime.DaysFromNow(30f), RewardGold, _targetSettlement);
            }

            protected override void OnGameLoad() { }

            protected override void HourlyTick() { }
        }

        public class HuntCultistsQuest : QuestBase
        {
            [SaveableField(1)]
            Settlement _settlement;
            [SaveableField(2)]
            bool _storyPlayed;
            [SaveableField(3)]
            bool _dealtWithCultists;

            private void TrackTarget()
            {
                if (_settlement != null)
                    AddTrackedObject(_settlement);
            }

            public HuntCultistsQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, Settlement targetSettlement) : base(questId, questGiver, duration, rewardGold)
            {
                _settlement = targetSettlement;
                SetDialogs();
                InitializeQuestOnCreation();
            }

            public override TextObject Title => TORTextHelper.GetTextObject("tor_quest_hunt_cultist_quest_title", "A cultist in our midst");

            public override bool IsRemainingTimeHidden => false;

            protected override void HourlyTick() { }

            protected override void InitializeQuestOnGameLoad()
            {
                SetDialogs();
                TrackTarget();
            }

            protected override void SetDialogs()
            {
                OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100).NpcLine(TORTextHelper.GetTextObject("tor_quest_hunt_cultist_offer_dialog", "Excellent. Do not underestimate the ruinous powers, unwavering vigilance is required on your quest!"), null, null).Condition(() => Hero.OneToOneConversationHero == QuestGiver).Consequence(OnQuestAccepted).CloseDialog();
                DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(TORTextHelper.GetTextObject("tor_quest_hunt_cultist_discuss_dialog", "It was good doing business with you."), null, null).Condition(() => Hero.OneToOneConversationHero == QuestGiver).CloseDialog();
                Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 199).NpcLine(TORTextHelper.GetText("tor_khorne_cultist_mission_dialog", "This vessel is mine. Don't interfere with my plans!"))
                    .Condition(() => Mission.Current != null && Mission.Current.SceneName == "TOR_cultist_lair_001" && Mission.Current.Mode != MissionMode.Battle)
                    .PlayerLine(TORTextHelper.GetText("tor_quest_hunt_cultist_prepare_to_die", "Prepare to die!"))
                    .Consequence(TurnHostile).CloseDialog());
            }

            protected override void OnTimedOut()
            {
                AddLog(TORTextHelper.GetTextObject("tor_quest_hunt_cultist_out_of_time_log", "You failed to complete the investigation in time. Any potential cultists are surely in the wind now."));
            }

            protected override void RegisterEvents()
            {
                CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, SettlementEntered);
            }

            protected override void OnFinalize()
            {
                CampaignEvents.AfterSettlementEntered.ClearListeners(this);

                if (_settlement != null)
                    RemoveTrackedObject(_settlement);
            }

            private void TurnHostile()
            {
                TORMissionHelper.MakeEnemyAgentsHostile();
            }

            private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
            {
                if (party == MobileParty.MainParty && settlement == _settlement && !_storyPlayed)
                {
                    if (settlement.IsUnderRaid || settlement.IsRaided)
                    {
                        var titleText = TORTextHelper.GetText("tor_quest_hunt_cultist_village_raided_title", "Village Raided");
                        var messageText = TORTextHelper.GetText("tor_quest_hunt_cultist_village_raided_info", "The village has been raided. Any cultists have likely fled or gone into hiding. Return when the village has been repopulated.");
                        InquiryData data = new InquiryData(titleText, messageText, true, false, "OK", null, () => InformationManager.HideInquiry(), null);
                        InformationManager.ShowInquiry(data);
                    }
                    else InkStoryManager.OpenStory("CultistInOurMidst", AfterStory);
                }
            }

            private void AfterStory(InkStory story)
            {
                _storyPlayed = true;
                bool.TryParse(story.GetVariable("DealtWithCultists"), out _dealtWithCultists);
                if (_dealtWithCultists)
                {
                    AddLog(TORTextHelper.GetTextObject("tor_quest_hunt_cultist_log_updated_success", "You were successful in uncovering the cultists."));
                    CompleteQuestWithSuccess();
                }
                else CompleteQuestWithFail(TORTextHelper.GetTextObject("tor_quest_hunt_cultist_log_updated_fail", "The cultists escaped."));
            }

            private void OnQuestAccepted()
            {
                StartQuest();
                this.QuestDueTime = CampaignTime.Now + CampaignTime.Days(20);

                TrackTarget();

                var acceptLog = TORTextHelper.GetTextObject("tor_quest_hunt_cultist_started", "You were tasked to travel to {TARGET_SETTLEMENT} and root out any cultist who may be hiding there.");
                acceptLog.SetTextVariable("TARGET_SETTLEMENT", _settlement.EncyclopediaLinkWithName);
                AddLog(acceptLog);
            }

            protected override void OnCompleteWithSuccess()
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
            }
        }
    }
}