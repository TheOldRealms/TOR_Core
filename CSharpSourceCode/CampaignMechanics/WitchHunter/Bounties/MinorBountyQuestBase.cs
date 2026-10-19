using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Shared infrastructure for all Minor Bounty quests. Each
    /// <see cref="BountyObjectiveType"/> is a separate concrete subclass because the
    /// vanilla event each listens to is different (kill-count vs prisoner-donation
    /// vs hideout-cleared vs party-defeated); forcing them into one parameterised
    /// class would be a switch in every method. The common state (template id,
    /// counts, rewards, _acceptedAt gate, _readyForTurnIn flag) lives here.
    ///
    /// <para>Subclasses implement <see cref="RegisterEvents"/> + <see cref="OnFinalize"/>
    /// to wire the right vanilla event, and call <see cref="BumpProgressBy"/>
    /// whenever that event signals progress.</para>
    /// </summary>
    public abstract class MinorBountyQuestBase : QuestBase
    {
        [SaveableField(0)] protected string _templateId;
        [SaveableField(1)] protected int _targetCount;
        [SaveableField(2)] protected int _rewardGold;
        [SaveableField(3)] protected int _rewardPrestige;
        [SaveableField(4)] protected int _progress;
        [SaveableField(5)] protected CampaignTime _acceptedAt;
        [SaveableField(6)] protected bool _readyForTurnIn;

        public BountyTemplate Template => BountyTemplates.GetMinor(_templateId);
        public int TargetCount => _targetCount;
        public int Progress => _progress;
        public int RewardPrestige => _rewardPrestige;
        public CampaignTime AcceptedAt => _acceptedAt;
        public bool IsReadyForTurnIn => _readyForTurnIn;
        public bool IsVoluntaryAbandonAvailable =>
            (CampaignTime.Now - _acceptedAt).ToDays >= 3f;

        protected MinorBountyQuestBase(
            string questId, Hero questGiver, CampaignTime duration,
            string templateId, int targetCount, int rewardGold, int rewardPrestige)
            : base(questId, questGiver, duration, rewardGold)
        {
            _templateId = templateId;
            _targetCount = targetCount;
            _rewardGold = rewardGold;
            _rewardPrestige = rewardPrestige;
            _progress = 0;
            _acceptedAt = CampaignTime.Now;
            _readyForTurnIn = false;
            SetDialogs();
            InitializeQuestOnCreation();
        }

        public override TextObject Title
        {
            get
            {
                var t = Template;
                var title = TORTextHelper.GetTextObject(t.TitleKey, t.TitleFallback);
                title.SetTextVariable("COUNT", _targetCount);
                title.SetTextVariable("TARGET", t.TargetLabel);
                return title;
            }
        }

        public override bool IsRemainingTimeHidden => false;

        protected override void HourlyTick() { }
        protected override void InitializeQuestOnGameLoad() => SetDialogs();

        protected override void SetDialogs()
        {
            OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100)
                .NpcLine(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_minor_offer_dialog",
                    "The Order is watching. See it done."))
                .Condition(() => Hero.OneToOneConversationHero == QuestGiver)
                .Consequence(OnQuestAccepted)
                .CloseDialog();

            DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100)
                .NpcLine(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_minor_discuss_dialog",
                    "There are still heads to be taken. The ledger does not close itself."))
                .Condition(() => Hero.OneToOneConversationHero == QuestGiver)
                .CloseDialog();
        }

        protected override void OnTimedOut()
        {
            if (_readyForTurnIn)
            {
                AddLog(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_minor_late_turnin_log",
                    "You were late to collect, but the Order honours the kills."));
                CompleteQuestWithSuccess();
                return;
            }

            AddLog(TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_timeout_log",
                "The Order will note your failure to see this through. You will need to beg forgiveness at the lodge before taking another minor hunt."));
            CompleteQuestWithFail();
            Campaign.Current?.GetCampaignBehavior<WitchHunterInquisitionBehavior>()
                ?.MarkMinorFailurePending();
        }

        public void TurnInAndComplete()
        {
            if (!_readyForTurnIn) return;
            AddLog(TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_turnedin_log",
                "The Order's ledger is settled. Reward paid."));
            CompleteQuestWithSuccess();
        }

        protected override void OnCompleteWithSuccess()
        {
            if (_rewardGold > 0)
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
            if (_rewardPrestige > 0)
                Hero.MainHero.AddCustomResource("Prestige", _rewardPrestige);
        }

        protected void OnQuestAccepted()
        {
            StartQuest();
            var acceptLog = TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_started_log",
                "The Order expects {COUNT} {TARGET} dealt with by your hand before the sand runs out.");
            var t = Template;
            acceptLog.SetTextVariable("COUNT", _targetCount);
            acceptLog.SetTextVariable("TARGET", t.TargetLabel);
            AddLog(acceptLog);
        }

        /// <summary>
        /// Pushed by <see cref="WitchHunterInquisitionBehavior.OnPrisonersSold"/>
        /// whenever the player uses the lodge's Prisoners turn-in — the behavior
        /// already fires on the right event with the full donated roster, so
        /// capture-type quests don't need their own subscription (which would
        /// race against the behavior's suppression-flag teardown). Default no-op;
        /// <see cref="MinorBountyCaptureQuest"/> overrides to count matching
        /// donations.
        /// </summary>
        public virtual void OnLodgePrisonersDonated(FlattenedTroopRoster roster) { }

        /// <summary>
        /// Subclasses call this with the increment from whatever event they listened
        /// to (kill count, prisoner count, hideouts cleared, parties defeated).
        /// Caps implicitly at <see cref="_targetCount"/> for the ready-for-turn-in
        /// trigger and emits the progress log.
        /// </summary>
        protected void BumpProgressBy(int amount)
        {
            if (amount <= 0) return;
            _progress += amount;
            AddProgressLog();
            if (_progress >= _targetCount && !_readyForTurnIn)
            {
                _readyForTurnIn = true;
                AddLog(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_minor_ready_log",
                    "All targets eliminated. Return to any Witch Hunter Lodge to collect your due."));
            }
        }

        private void AddProgressLog()
        {
            var template = Template;
            var log = TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_progress_log",
                "Progress: {PROGRESS}/{TARGET_COUNT} {TARGET}.");
            log.SetTextVariable("PROGRESS", System.Math.Min(_progress, _targetCount));
            log.SetTextVariable("TARGET_COUNT", _targetCount);
            log.SetTextVariable("TARGET", template.TargetLabel);
            AddLog(log);
        }
    }
}
