using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Active Minor Bounty quest. Tracks enemy-troop kills matching the template's
    /// <see cref="BountyTemplate.TargetMatches"/> predicate until <see cref="_targetCount"/>
    /// hits — then auto-completes with gold + prestige reward.
    ///
    /// <para><c>_acceptedAt</c> is read by the lodge's "I cannot finish a bounty"
    /// dispatcher to gate voluntary abandonment behind a ≥ 3-day waiting period
    /// (see <see cref="TOR_Core.CampaignMechanics.SettlementPOIs.WitchHunterLodgePOI"/>).</para>
    /// </summary>
    public class MinorBountyQuest : QuestBase
    {
        [SaveableField(0)] private string _templateId;
        [SaveableField(1)] private int _targetCount;
        [SaveableField(2)] private int _rewardGold;
        [SaveableField(3)] private int _rewardPrestige;
        [SaveableField(4)] private int _progressKills;
        [SaveableField(5)] private CampaignTime _acceptedAt;

        public BountyTemplate Template => BountyTemplates.GetMinor(_templateId);
        public int TargetCount => _targetCount;
        public int ProgressKills => _progressKills;
        public int RewardPrestige => _rewardPrestige;
        public CampaignTime AcceptedAt => _acceptedAt;

        /// <summary>True after the quest has been active long enough for the
        /// player to voluntarily abandon it via the lodge dialog.</summary>
        public bool IsVoluntaryAbandonAvailable =>
            (CampaignTime.Now - _acceptedAt).ToDays >= 3f;

        public MinorBountyQuest(
            string questId, Hero questGiver, CampaignTime duration,
            string templateId, int targetCount, int rewardGold, int rewardPrestige)
            : base(questId, questGiver, duration, rewardGold)
        {
            _templateId = templateId;
            _targetCount = targetCount;
            _rewardGold = rewardGold;
            _rewardPrestige = rewardPrestige;
            _progressKills = 0;
            _acceptedAt = CampaignTime.Now;
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
            AddLog(TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_timeout_log",
                "The Order will note your failure to see this through. You will need to beg forgiveness at the lodge before taking another minor hunt."));
            CompleteQuestWithFail();
            Campaign.Current?.GetCampaignBehavior<WitchHunterInquisitionBehavior>()
                ?.MarkMinorFailurePending();
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
        }

        protected override void OnFinalize()
        {
            CampaignEvents.OnPlayerBattleEndEvent.ClearListeners(this);
        }

        private void OnQuestAccepted()
        {
            StartQuest();
            var acceptLog = TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_started_log",
                "The Order expects {COUNT} {TARGET} dead by your hand before the sand runs out.");
            var t = Template;
            acceptLog.SetTextVariable("COUNT", _targetCount);
            acceptLog.SetTextVariable("TARGET", t.TargetLabel);
            AddLog(acceptLog);
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;
            if (!mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            var template = Template;
            if (template.IsEmpty || template.TargetMatches == null) return;

            var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
            if (defeatedSide == null) return;

            int killed = 0;
            foreach (var party in defeatedSide.Parties)
            {
                foreach (var elem in party.Troops.Where(t => t.IsKilled))
                {
                    if (template.TargetMatches(elem.Troop)) killed++;
                }
            }
            if (killed == 0) return;

            _progressKills += killed;
            var progressLog = TORTextHelper.GetTextObject(
                "tor_wh_bounty_minor_progress_log",
                "Progress: {PROGRESS}/{TARGET_COUNT} {TARGET}.");
            progressLog.SetTextVariable("PROGRESS", System.Math.Min(_progressKills, _targetCount));
            progressLog.SetTextVariable("TARGET_COUNT", _targetCount);
            progressLog.SetTextVariable("TARGET", template.TargetLabel);
            AddLog(progressLog);

            if (_progressKills >= _targetCount)
            {
                AddLog(TORTextHelper.GetTextObject(
                    "tor_wh_bounty_minor_success_log",
                    "The Order's ledger is settled. Return to the lodge for your reward."));
                CompleteQuestWithSuccess();
            }
        }

        protected override void OnCompleteWithSuccess()
        {
            if (_rewardGold > 0)
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _rewardGold);
            if (_rewardPrestige > 0)
                Hero.MainHero.AddCustomResource("Prestige", _rewardPrestige);
        }
    }
}
