using TaleWorlds.CampaignSystem;
using TOR_Core.Extensions;

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

        // Active-bounty counts aren't stored — vanilla's IssueManager is the
        // authority. Phase B will wire real Minor/MajorBountyIssue types; until
        // then these return 0.
        public int ActiveMinorBounties() => 0;
        public int ActiveMajorBounties() => 0;

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
