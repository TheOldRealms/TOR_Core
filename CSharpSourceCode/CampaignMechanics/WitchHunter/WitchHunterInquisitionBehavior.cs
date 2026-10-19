using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.SettlementPOIs;
using TOR_Core.CampaignMechanics.WitchHunter.Bounties;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.WitchHunter
{
    /// <summary>
    /// Player's reputation with the Order of Sigmar. Source of truth for the Witch
    /// Hunter Lodge's rank gates: completed-bounty counters are the stored state,
    /// <see cref="Rank"/> and the per-tier caps are derived, and tier crossings
    /// emit <c>INQUISITION_KNOWN/TRUSTED/RESPECTED</c> hero attributes so cross-
    /// system readers (store gates, hunt gates, future career perks, dialog
    /// visibility) consult <c>HasAttribute</c> without touching this behavior.
    /// Pattern mirrors <see cref="TOR_Core.CampaignMechanics.Menagery.OathGoldBehavior"/>'s
    /// <c>GUILD_RUNESMITH_1/2/3</c> convention.
    ///
    /// <para>Design doc: <c>TORTasks/witchhunter feature/Witch Hunter Chapterhouse.odp</c>.
    /// Implementation plan: <c>TORTasks/witchhunter feature/README.md</c> and
    /// sibling task docs.</para>
    /// </summary>
    public class WitchHunterInquisitionBehavior : CampaignBehaviorBase
    {
        // Attribute ids applied to Hero.MainHero on tier crossings. Readers (store
        // gates, hunt visibility, future career perks) key off these rather than
        // taking a direct dependency on this behavior.
        public const string AttrKnown     = "INQUISITION_KNOWN";
        public const string AttrTrusted   = "INQUISITION_TRUSTED";
        public const string AttrRespected = "INQUISITION_RESPECTED";

        // Design-doc thresholds. Known at 5 minor; Trusted at 5+15=20 minor;
        // Respected additionally requires 5 major (and transitively, Trusted).
        public const int KnownMinorThreshold     = 5;
        public const int TrustedMinorThreshold   = 20;
        public const int RespectedMajorThreshold = 5;

        private int _minorCompleted;
        private int _majorCompleted;
        private bool _minorFailurePending;
        private bool _majorFailurePending;

        // ---- Public read surface

        public int MinorCompleted => _minorCompleted;
        public int MajorCompleted => _majorCompleted;
        public bool MinorFailurePending => _minorFailurePending;
        public bool MajorFailurePending => _majorFailurePending;

        /// <summary>
        /// Rank is monotonic by design. The <c>&amp;&amp; _minorCompleted &gt;=
        /// TrustedMinorThreshold</c> guard on Respected is defensive: in normal
        /// play Majors can't be completed before Trusted (vanilla gating via
        /// <see cref="MajorBountyCap"/>), but debug-granting counters directly
        /// shouldn't jump a player past the ladder.
        /// </summary>
        public InquisitionRank Rank
        {
            get
            {
                if (_minorCompleted >= TrustedMinorThreshold && _majorCompleted >= RespectedMajorThreshold)
                    return InquisitionRank.Respected;
                if (_minorCompleted >= TrustedMinorThreshold) return InquisitionRank.Trusted;
                if (_minorCompleted >= KnownMinorThreshold) return InquisitionRank.Known;
                return InquisitionRank.Unknown;
            }
        }

        public int MinorBountyCap => Rank switch
        {
            InquisitionRank.Unknown   => 1,
            InquisitionRank.Known     => 2,
            InquisitionRank.Trusted   => 2,
            InquisitionRank.Respected => 3,
            _ => 0,
        };

        public int MajorBountyCap => Rank switch
        {
            InquisitionRank.Trusted   => 1,
            InquisitionRank.Respected => 2,
            _ => 0,
        };

        // Active-bounty count = number of ACCEPTED bounty quests only. Un-accepted
        // issues sitting at the lodge don't fill the cap; the cap is about
        // concurrent commitments.
        public int ActiveMinorBounties()
        {
            var quests = Campaign.Current?.QuestManager?.Quests;
            if (quests == null) return 0;
            return quests.Count(q => q is MinorBountyQuestBase);
        }

        public int ActiveMajorBounties() => 0;   // Phase C: MajorBountyQuest

        // ---- Mutators (invoked by dialog consequences and quest OnTimedOut)

        public void CompleteMinor()
        {
            _minorCompleted++;
            ApplyRankAttributes();
        }

        public void CompleteMajor()
        {
            _majorCompleted++;
            ApplyRankAttributes();
        }

        public void MarkMinorFailurePending() => _minorFailurePending = true;
        public void MarkMajorFailurePending() => _majorFailurePending = true;
        public void ExcuseMinorFailure() => _minorFailurePending = false;
        public void ExcuseMajorFailure() => _majorFailurePending = false;

        // ---- Lifecycle

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonersSold);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
        }

        /// <summary>
        /// Roll for a Minor Bounty issue once per in-game day per lodge-containing
        /// settlement. The player no longer re-rolls by bouncing in and out of a
        /// town; offers accumulate across lodges at a predictable cadence.
        /// Vanilla's one-issue-per-hero constraint still applies — each lodge
        /// holds at most one offer, but seven lodges = seven independent slots.
        /// </summary>
        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement == null) return;

            var lodge = settlement.LocationComplex?.GetLocationWithId("tor_witchhunter_lodge");
            if (lodge == null || lodge.GetSceneCount() == 0) return;

            var inquisitor = settlement.HeroesWithoutParty.FirstOrDefault(h => h.IsBountyMaster());
            if (inquisitor == null || inquisitor.Issue != null) return;

            // Don't generate offers the player can never accept (cap gate).
            if (MinorBountyCap <= 0 || ActiveMinorBounties() >= MinorBountyCap) return;

            if (MBRandom.RandomInt(0, 100) >= TORConstants.BOUNTY_QUEST_CHANCE) return;

            Campaign.Current.IssueManager.CreateNewIssue(
                new PotentialIssueData(
                    new PotentialIssueData.StartIssueDelegate(OnMinorBountyIssueSelected),
                    typeof(MinorBountyIssue),
                    IssueBase.IssueFrequency.VeryCommon),
                inquisitor);

            // Tell nameplate VMs to re-render — the lodge icon flips to its
            // "offer available" variant when the inquisitor picks up an issue.
            SettlementPOIManagerCampaignBehavior.NotifyIconsChanged();
        }

        private IssueBase OnMinorBountyIssueSelected(in PotentialIssueData pid, Hero issueOwner)
        {
            var template = BountyTemplates.PickRandomMinor();
            if (template.IsEmpty) return null;
            var count    = MBRandom.RandomInt(template.CountMin,    template.CountMax + 1);
            var gold     = MBRandom.RandomInt(template.GoldMin,     template.GoldMax + 1);
            var prestige = MBRandom.RandomInt(template.PrestigeMin, template.PrestigeMax + 1);
            return new MinorBountyIssue(issueOwner, template.Id, count, gold, prestige);
        }

        /// <summary>
        /// Counter bump on successful bounty completion. Fail paths bump nothing —
        /// the pending-failure flag is set from the quest's <c>OnTimedOut</c>
        /// directly against the behavior.
        /// </summary>
        private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero relatedHero)
        {
            if (issue is MinorBountyIssue)
            {
                // Any state change on a bounty issue (accepted, cancelled, expired, done)
                // means the lodge icon should re-evaluate — "H!" goes back to "H" once
                // the inquisitor isn't carrying an offer anymore.
                SettlementPOIManagerCampaignBehavior.NotifyIconsChanged();
            }
            if (details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess && issue is MinorBountyIssue)
                CompleteMinor();
            // Phase C: else if (issue is MajorBountyIssue) CompleteMajor();
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_wh_inq_minor_completed", ref _minorCompleted);
            dataStore.SyncData("_wh_inq_major_completed", ref _majorCompleted);
            dataStore.SyncData("_wh_inq_minor_failure", ref _minorFailurePending);
            dataStore.SyncData("_wh_inq_major_failure", ref _majorFailurePending);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Re-apply tier attributes on load — covers both existing saves that
            // predate the behavior (counters default to 0, no-op) and the natural
            // case where attributes were already applied in a prior session.
            ApplyRankAttributes();
        }

        /// <summary>
        /// Prisoner-donation callback. Fires on every donation (ours, Eonir's,
        /// vanilla's); checks our lodge-donation flag first and bails out
        /// cleanly otherwise. Rewards are ransom-broker-style gold per troop
        /// with a 2.5× multiplier for heretical categories, plus a small
        /// prestige stipend. Hero prisoners pay a flat high-tier rate and
        /// are released via <see cref="EndCaptivityAction.ApplyByRansom"/>
        /// (matches the Eonir envoy's "release immediately" model —
        /// avoids forcing the player to micro-manage captured heroes).
        /// </summary>
        private void OnPrisonersSold(MobileParty seller, FlattenedTroopRoster roster, Settlement receiving)
        {
            if (!WitchHunterTurnInHelper.IsLodgeDonationActive) return;
            if (seller != MobileParty.MainParty) return;

            // Defensive: if we're not actually in a lodge town, bail. Stale flag
            // from a cancelled screen would otherwise fire against the wrong town.
            var lodge = receiving?.LocationComplex?.GetLocationWithId("tor_witchhunter_lodge");
            if (lodge == null || lodge.GetSceneCount() == 0)
            {
                WitchHunterTurnInHelper.EndLodgeDonation();
                return;
            }

            try
            {
                int gold = 0, prestige = 0;
                foreach (var el in roster)
                {
                    gold     += WitchHunterTurnInHelper.CalculateHereticalGoldFor(el.Troop);
                    prestige += WitchHunterTurnInHelper.CalculateHereticalPrestigeFor(el.Troop);

                    if (el.Troop.IsHero && el.Troop.HeroObject != null && el.Troop.HeroObject.IsAlive)
                    {
                        // Clean release — the envoy / lodge takes care of them. No vanilla
                        // execution path (that's a design non-goal; the Inquisition handles
                        // disposal off-screen).
                        EndCaptivityAction.ApplyByRansom(el.Troop.HeroObject, null);
                    }
                    else
                    {
                        // Remove from the receiving settlement's prison roster so vanilla
                        // doesn't swell the garrison with our handed-off prisoners.
                        receiving.Party.PrisonRoster.RemoveTroop(el.Troop);
                    }
                }
                if (gold > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold);
                if (prestige > 0) Hero.MainHero.AddCustomResource("Prestige", prestige);

                // Push the donation into every active bounty quest so capture-type
                // quests can tick progress. Non-capture subclasses ignore via the
                // base's no-op default. Pushed from here (not subscribed per-quest)
                // to avoid racing the suppression-flag teardown in the finally below.
                var quests = Campaign.Current?.QuestManager?.Quests;
                if (quests != null)
                {
                    foreach (var q in quests.OfType<Bounties.MinorBountyQuestBase>())
                        q.OnLodgePrisonersDonated(roster);
                }
            }
            finally
            {
                WitchHunterTurnInHelper.EndLodgeDonation();
            }
        }

        /// <summary>
        /// Idempotently add the attribute flags for the current rank. Attributes
        /// are monotonic (we never remove them), matching OathGold's guild-tier
        /// attributes.
        /// </summary>
        private void ApplyRankAttributes()
        {
            var hero = Hero.MainHero;
            if (hero == null) return;
            var rank = Rank;
            if (rank >= InquisitionRank.Known     && !hero.HasAttribute(AttrKnown))     hero.AddAttribute(AttrKnown);
            if (rank >= InquisitionRank.Trusted   && !hero.HasAttribute(AttrTrusted))   hero.AddAttribute(AttrTrusted);
            if (rank >= InquisitionRank.Respected && !hero.HasAttribute(AttrRespected)) hero.AddAttribute(AttrRespected);
        }
    }
}
