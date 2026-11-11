using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Unified bounty quest for both Minor and Major tiers across all four
    /// objective types. The tier is held in a <see cref="SaveableField"/>
    /// (<see cref="_tier"/>) and dispatched inside <see cref="MarkFailurePending"/>;
    /// the per-objective counter lives in the switch inside
    /// <see cref="OnBattleEnd"/> (plus <see cref="OnHideoutCompleted"/> and
    /// <see cref="OnChapterhousePrisonersDonated"/> for the other two event shapes).
    ///
    /// <para>Deliberately a single concrete class — earlier commits on this
    /// branch split it per-objective and per-tier before the user pointed out
    /// those were data differences, not behavioural ones. Adding a new
    /// objective type is a new case in <see cref="OnBattleEnd"/> plus an enum
    /// value; adding a new tier is a case in <see cref="MarkFailurePending"/>
    /// plus an enum value. No new class, no new save id.</para>
    /// </summary>
    public class BountyQuest : QuestBase
    {
        [SaveableField(0)] protected string _templateId;
        [SaveableField(1)] protected int _targetCount;
        [SaveableField(2)] protected int _rewardGold;
        [SaveableField(3)] protected int _rewardPrestige;
        [SaveableField(4)] protected int _progress;
        [SaveableField(5)] protected CampaignTime _acceptedAt;
        [SaveableField(6)] protected bool _readyForTurnIn;
        [SaveableField(7)] protected BountyTier _tier;   // defaults to Minor (0) on old saves — correct

        public BountyTemplate Template => BountyTemplates.Get(_templateId);
        public BountyTier Tier => _tier;
        public int TargetCount => _targetCount;
        public int Progress => _progress;
        public int RewardPrestige => _rewardPrestige;
        public CampaignTime AcceptedAt => _acceptedAt;
        public bool IsReadyForTurnIn => _readyForTurnIn;

        // Bounded progress log for the quest panel ("N/M targets eliminated").
        // Created on quest accept (AddDiscreteLog), updated on each BumpProgressBy
        // via UpdateQuestTaskStage. Not saved: rebuilt in InitializeQuestOnGameLoad
        // so the panel entry survives save/load.
        private TaleWorlds.CampaignSystem.JournalLog _progressLog;

        public bool IsVoluntaryAbandonAvailable =>
            (CampaignTime.Now - _acceptedAt).ToDays >= 3f;

        public BountyQuest(
            string questId, Hero questGiver, CampaignTime duration,
            string templateId, BountyTier tier,
            int targetCount, int rewardGold, int rewardPrestige)
            : base(questId, questGiver, duration, rewardGold)
        {
            _templateId = templateId;
            _tier = tier;
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

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            // Re-seed the progress log entry on save/load so the quest panel
            // keeps showing the N/M counter.
            EnsureProgressLog();
        }

        protected override void SetDialogs()
        {
            OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100)
                .NpcLine(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_offer_dialog",
                    "The Order is watching. See it done."))
                .Condition(() => Hero.OneToOneConversationHero == QuestGiver)
                .Consequence(OnQuestAccepted)
                .CloseDialog();

            DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100)
                .NpcLine(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_discuss_dialog",
                    "There are still heads to be taken. The ledger does not close itself."))
                .Condition(() => Hero.OneToOneConversationHero == QuestGiver)
                .CloseDialog();
        }

        protected override void OnTimedOut()
        {
            if (_readyForTurnIn)
            {
                AddLog(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_late_turnin_log",
                    "You were late to collect, but the Order honours the kills."));
                CompleteQuestWithSuccess();
                return;
            }

            AddLog(TORTextHelper.GetTextObject(
                "tor_wh_bounty_timeout_log",
                "The Order will note your failure to see this through. You will need to beg forgiveness at the chapterhouse before taking another hunt of this kind."));
            CompleteQuestWithFail();
            MarkFailurePending();
        }

        public void TurnInAndComplete()
        {
            if (!_readyForTurnIn) return;
            AddLog(TORTextHelper.GetTextObject(
                "tor_wh_bounty_turnedin_log",
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
                "tor_wh_bounty_started_log",
                "The Order expects {COUNT} {TARGET} dealt with by your hand before the sand runs out.");
            var t = Template;
            acceptLog.SetTextVariable("COUNT", _targetCount);
            acceptLog.SetTextVariable("TARGET", t.TargetLabel);
            AddLog(acceptLog);
            EnsureProgressLog();
        }

        /// <summary>Dispatches the failure flag to the correct tier counter on the behavior.</summary>
        private void MarkFailurePending()
        {
            var inq = Campaign.Current?.GetCampaignBehavior<WitchHunterInquisitionBehavior>();
            if (inq == null) return;
            switch (_tier)
            {
                case BountyTier.Minor: inq.MarkMinorFailurePending(); break;
                case BountyTier.Major: inq.MarkMajorFailurePending(); break;
            }
        }

        // ---- Event wiring (same for all objective types; the handlers below branch)

        protected override void RegisterEvents()
        {
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
            CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutCompleted);
        }

        protected override void OnFinalize()
        {
            CampaignEvents.OnPlayerBattleEndEvent.ClearListeners(this);
            CampaignEvents.OnHideoutBattleCompletedEvent.ClearListeners(this);
        }

        private void OnBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;
            if (!mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            var template = Template;
            if (template.IsEmpty || template.TargetMatches == null) return;

            var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
            if (defeatedSide == null) return;

            switch (template.ObjectiveType)
            {
                case BountyObjectiveType.KillTroops:
                    BumpProgressBy(CountMatchingKills(defeatedSide, template));
                    break;
                case BountyObjectiveType.DefeatParties:
                    BumpProgressBy(CountMatchingParties(defeatedSide, template));
                    break;
            }
        }

        private void OnHideoutCompleted(BattleSideEnum battleSide, HideoutEventComponent eventComponent, HideoutEventComponent.HideoutBattleEndState endState)
        {
            var template = Template;
            if (template.ObjectiveType != BountyObjectiveType.ClearHideout) return;
            if (template.IsEmpty) return;

            var mapEvent = eventComponent?.MapEvent;
            if (mapEvent == null) return;
            if (!mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            // Prefer matching the hideout settlement directly. Defender rosters
            // use vanilla bandit cultures (sea_raiders, forest_bandits), not
            // TOR cultures, so a troop-level predicate never matches a real
            // hideout. Fall back to TargetMatches on the defeated roster for
            // any template that doesn't declare a hideout-level predicate.
            bool matched;
            if (template.HideoutMatches != null)
            {
                matched = template.HideoutMatches(mapEvent.MapEventSettlement);
            }
            else if (template.TargetMatches != null)
            {
                var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
                if (defeatedSide == null) return;
                matched = defeatedSide.Parties.Any(p => p.Troops.Any(t => template.TargetMatches(t.Troop)));
            }
            else
            {
                return;
            }

            if (matched) BumpProgressBy(1);
        }

        /// <summary>
        /// Pushed by <see cref="WitchHunterInquisitionBehavior.OnPrisonersSold"/>
        /// whenever the player uses the chapterhouse's Prisoners turn-in. Only capture-
        /// type bounties care; others no-op via the ObjectiveType check.
        /// </summary>
        public void OnChapterhousePrisonersDonated(FlattenedTroopRoster roster)
        {
            var template = Template;
            if (template.ObjectiveType != BountyObjectiveType.CaptureTroops) return;
            if (template.IsEmpty || template.TargetMatches == null) return;
            if (roster == null) return;

            int captured = 0;
            foreach (var elem in roster)
            {
                if (elem.Troop == null || elem.Troop.IsHero) continue;
                if (template.TargetMatches(elem.Troop)) captured++;
            }
            BumpProgressBy(captured);
        }

        // ---- Progress helpers

        protected void BumpProgressBy(int amount)
        {
            if (amount <= 0) return;
            _progress += amount;
            EnsureProgressLog();
            if (_progressLog != null)
                UpdateQuestTaskStage(_progressLog, System.Math.Min(_progress, _targetCount));
            if (_progress >= _targetCount && !_readyForTurnIn)
            {
                _readyForTurnIn = true;
                AddLog(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_ready_log",
                    "All targets eliminated. Return to any Witch Hunter Chapterhouse to collect your due."));
            }
        }

        /// <summary>
        /// Ensure the quest panel has a discrete-progress entry (shows up as
        /// "N / M targetLabel" with a progress bar). Idempotent via a cached
        /// reference. Re-seeded on save/load from <see cref="InitializeQuestOnGameLoad"/>.
        /// </summary>
        private void EnsureProgressLog()
        {
            if (_progressLog != null) return;
            var template = Template;
            if (template.IsEmpty) return;
            // Empty journal-entry text on purpose: the panel's task-name row
            // ("Norscan hideouts 2/2") is enough, and a free-form entry above
            // it was just visual noise.
            _progressLog = AddDiscreteLog(
                new TextObject(string.Empty),
                new TextObject(template.TargetLabel),
                System.Math.Min(_progress, _targetCount),
                _targetCount);
        }

        private static int CountMatchingKills(MapEventSide defeatedSide, BountyTemplate template)
        {
            int killed = 0;
            foreach (var party in defeatedSide.Parties)
                foreach (var elem in party.Troops.Where(t => t.IsKilled))
                    if (template.TargetMatches(elem.Troop)) killed++;
            return killed;
        }

        private static int CountMatchingParties(MapEventSide defeatedSide, BountyTemplate template)
        {
            int defeatedParties = 0;
            foreach (var party in defeatedSide.Parties)
                if (party.Troops.Any(t => template.TargetMatches(t.Troop)))
                    defeatedParties++;
            return defeatedParties;
        }
    }
}
