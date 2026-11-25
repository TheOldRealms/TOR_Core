using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.SettlementPOIs;
using TOR_Core.CampaignMechanics.WitchHunter.Bounties;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.WitchHunter
{
    /// <summary>
    /// Player's reputation with the Order of Sigmar. Source of truth for the Witch
    /// Hunter Chapterhouse's rank gates: completed-bounty counters are the stored state,
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

        // Last template picked per tier — used by the picker to avoid rolling
        // the same bounty back-to-back. Persists across save/load so the player
        // can't quit/reload to re-roll to a different template.
        private string _lastMinorTemplateId;
        private string _lastMajorTemplateId;

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

        // Active-bounty count = number of ACCEPTED bounty quests of the matching
        // tier only. Un-accepted issues sitting at the chapterhouse don't fill the cap;
        // the cap is about concurrent commitments. Must filter by IsOngoing —
        // vanilla QuestManager.Quests retains finished quests, so without it
        // every completed Minor keeps occupying its cap slot and the player
        // can't accept a new one even after rank-up.
        public int ActiveMinorBounties() => CountActive(BountyTier.Minor);
        public int ActiveMajorBounties() => CountActive(BountyTier.Major);

        private static int CountActive(BountyTier tier)
        {
            var quests = Campaign.Current?.QuestManager?.Quests;
            if (quests == null) return 0;
            return quests.Count(q => q is BountyQuest bq && bq.Tier == tier && q.IsOngoing);
        }

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

        /// <summary>
        /// Debug/console helper — jumps the counters to the thresholds for a
        /// specific rank and re-applies the attribute flags. Attribute flags
        /// are monotonic (never removed), so downgrading leaves the hero
        /// permanently flagged for the previously-reached ranks — acceptable
        /// for debug use.
        /// </summary>
        public void DebugSetRank(InquisitionRank rank)
        {
            switch (rank)
            {
                case InquisitionRank.Unknown:   _minorCompleted = 0;                       _majorCompleted = 0; break;
                case InquisitionRank.Known:     _minorCompleted = KnownMinorThreshold;     _majorCompleted = 0; break;
                case InquisitionRank.Trusted:   _minorCompleted = TrustedMinorThreshold;   _majorCompleted = 0; break;
                case InquisitionRank.Respected: _minorCompleted = TrustedMinorThreshold;   _majorCompleted = RespectedMajorThreshold; break;
            }
            ApplyRankAttributes();
        }

        // ---- Lifecycle

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonersSold);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
            // Daily tick covers the long game, but a chapterhouse whose inquisitor hasn't
            // been placed yet (first visit on a fresh campaign) is skipped by that
            // tick. SettlementEntered catches the follow-up: same fill logic, now
            // the hero exists. Idempotent: no-op if the slot is already full.
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnDailyTickSettlement(Settlement settlement) => TryFillChapterhouse(settlement);

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero visitor)
        {
            if (party != MobileParty.MainParty) return;
            TryFillChapterhouse(settlement);
        }

        /// <summary>
        /// Fill the chapterhouse inquisitor's offer slot if it is empty and the player
        /// is under the tier cap. Deterministic (no RNG gate) — design brief is
        /// "a chapterhouse is probably worth checking after a week"; the old 25% roll
        /// produced week-long dry spells on unlucky rolls and was the root cause
        /// of PR #46 feedback point 8. Minor rolls first; if it fills the slot,
        /// Major silently skips (single-slot vanilla constraint).
        /// </summary>
        private void TryFillChapterhouse(Settlement settlement)
        {
            if (settlement == null) return;

            var chapterhouse = settlement.LocationComplex?.GetLocationWithId("tor_witchhunter_chapterhouse");
            if (chapterhouse == null || chapterhouse.GetSceneCount() == 0) return;

            var inquisitor = settlement.HeroesWithoutParty.FirstOrDefault(h => h.IsBountyMaster());
            if (inquisitor == null) return;
            if (inquisitor.Issue != null) return;

            TryRollBounty(inquisitor, BountyTier.Minor);
            if (inquisitor.Issue == null) TryRollBounty(inquisitor, BountyTier.Major);
        }

        private void TryRollBounty(Hero inquisitor, BountyTier tier)
        {
            // Per-rank cap gate — don't generate what the player can't accept.
            var cap = tier == BountyTier.Minor ? MinorBountyCap : MajorBountyCap;
            var active = tier == BountyTier.Minor ? ActiveMinorBounties() : ActiveMajorBounties();
            if (cap <= 0 || active >= cap)
            {
                TORCommon.Say($"BountyGen[{tier}] skipped at {inquisitor.CurrentSettlement?.Name}: cap={cap} active={active}");
                return;
            }

            PotentialIssueData.StartIssueDelegate factory =
                tier == BountyTier.Minor
                    ? new PotentialIssueData.StartIssueDelegate(OnMinorBountyIssueSelected)
                    : new PotentialIssueData.StartIssueDelegate(OnMajorBountyIssueSelected);

            Campaign.Current.IssueManager.CreateNewIssue(
                new PotentialIssueData(factory, typeof(BountyIssue), IssueBase.IssueFrequency.VeryCommon),
                inquisitor);

            TORCommon.Say($"BountyGen[{tier}] placed at {inquisitor.CurrentSettlement?.Name} (issue assigned: {inquisitor.Issue != null})");

            // Tell nameplate VMs to re-render — the chapterhouse icon flips to its
            // "offer available" variant when the inquisitor picks up an issue.
            SettlementPOIManagerCampaignBehavior.NotifyIconsChanged();
        }

        private IssueBase OnMinorBountyIssueSelected(in PotentialIssueData pid, Hero issueOwner) => CreateBountyIssue(issueOwner, BountyTier.Minor);
        private IssueBase OnMajorBountyIssueSelected(in PotentialIssueData pid, Hero issueOwner) => CreateBountyIssue(issueOwner, BountyTier.Major);

        private IssueBase CreateBountyIssue(Hero issueOwner, BountyTier tier)
        {
            var avoid = tier == BountyTier.Minor ? _lastMinorTemplateId : _lastMajorTemplateId;
            var template = BountyTemplates.PickRandom(tier, avoid);
            if (template.IsEmpty) return null;

            if (tier == BountyTier.Minor) _lastMinorTemplateId = template.Id;
            else                           _lastMajorTemplateId = template.Id;

            var count    = MBRandom.RandomInt(template.CountMin,    template.CountMax + 1);
            var gold     = MBRandom.RandomInt(template.GoldMin,     template.GoldMax + 1);
            var prestige = MBRandom.RandomInt(template.PrestigeMin, template.PrestigeMax + 1);
            return new BountyIssue(issueOwner, template.Id, tier, count, gold, prestige);
        }

        /// <summary>
        /// Counter bump on successful bounty completion. Fail paths bump nothing —
        /// the pending-failure flag is set from the quest's <c>OnTimedOut</c>
        /// directly against the behavior. Both tiers routed via the issue's
        /// <see cref="BountyIssue.Tier"/>.
        /// </summary>
        private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero relatedHero)
        {
            if (!(issue is BountyIssue bounty)) return;

            // Any state change on a bounty issue (accepted, cancelled, expired, done)
            // means the chapterhouse icon should re-evaluate — "H!" goes back to "H" once
            // the inquisitor isn't carrying an offer anymore.
            SettlementPOIManagerCampaignBehavior.NotifyIconsChanged();

            if (details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess)
            {
                switch (bounty.Tier)
                {
                    case BountyTier.Minor: CompleteMinor(); break;
                    case BountyTier.Major: CompleteMajor(); break;
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_wh_inq_minor_completed", ref _minorCompleted);
            dataStore.SyncData("_wh_inq_major_completed", ref _majorCompleted);
            dataStore.SyncData("_wh_inq_minor_failure", ref _minorFailurePending);
            dataStore.SyncData("_wh_inq_major_failure", ref _majorFailurePending);
            dataStore.SyncData("_wh_inq_last_minor_template", ref _lastMinorTemplateId);
            dataStore.SyncData("_wh_inq_last_major_template", ref _lastMajorTemplateId);
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
        /// vanilla's); checks our chapterhouse-donation flag first and bails out
        /// cleanly otherwise. Rewards are ransom-broker-style gold per troop
        /// with a 2.5× multiplier for heretical categories, plus a small
        /// prestige stipend. Hero prisoners pay a flat high-tier rate and
        /// are released via <see cref="EndCaptivityAction.ApplyByRansom"/>
        /// (matches the Eonir envoy's "release immediately" model —
        /// avoids forcing the player to micro-manage captured heroes).
        /// </summary>
        private void OnPrisonersSold(MobileParty seller, FlattenedTroopRoster roster, Settlement receiving)
        {
            if (!WitchHunterTurnInHelper.IsChapterhouseDonationActive) return;
            if (seller != MobileParty.MainParty) return;

            // Defensive: if we're not actually in a chapterhouse town, bail. Stale flag
            // from a cancelled screen would otherwise fire against the wrong town.
            var chapterhouse = receiving?.LocationComplex?.GetLocationWithId("tor_witchhunter_chapterhouse");
            if (chapterhouse == null || chapterhouse.GetSceneCount() == 0)
            {
                WitchHunterTurnInHelper.EndChapterhouseDonation();
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
                        // Clean release — the envoy / chapterhouse takes care of them. No vanilla
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
                    foreach (var q in quests.OfType<Bounties.BountyQuest>())
                        q.OnChapterhousePrisonersDonated(roster);
                }
            }
            finally
            {
                WitchHunterTurnInHelper.EndChapterhouseDonation();
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
