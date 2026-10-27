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

        // Companion Recruitment. Default CampaignTime (= Day 0) is treated as
        // "never recruited" so a fresh campaign starts off the cooldown.
        public const int CompanionRecruitCooldownDays = 60;
        public const string WitchHunterWandererTemplateId = "tor_wanderer_empire_2";
        private CampaignTime _lastCompanionRecruitAt;

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
        // tier only. Un-accepted issues sitting at the lodge don't fill the cap;
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

        // ---- Companion Recruitment

        /// <summary>True iff enough days have passed since the last recruit
        /// (or no recruit yet). Design doc puts this at T1 availability — no
        /// rank gate on top.</summary>
        public bool CanRecruitCompanion
        {
            get
            {
                if (_lastCompanionRecruitAt == default) return true;   // never recruited
                return (CampaignTime.Now - _lastCompanionRecruitAt).ToDays >= CompanionRecruitCooldownDays;
            }
        }

        public int DaysUntilRecruitAvailable
        {
            get
            {
                if (_lastCompanionRecruitAt == default) return 0;
                var days = CompanionRecruitCooldownDays - (CampaignTime.Now - _lastCompanionRecruitAt).ToDays;
                return System.Math.Max(0, (int)System.Math.Ceiling(days));
            }
        }

        /// <summary>
        /// Spawn a Witch Hunter wanderer hero, set them up as a player-clan
        /// companion, and add them to the main party. Returns the new hero via
        /// out-param on success. Fails cleanly (returns false) if the template
        /// is missing or the player is not in a settlement.
        /// </summary>
        public bool TryRecruitCompanionAtLodge(Settlement s, out Hero newCompanion)
        {
            newCompanion = null;
            if (s == null) return false;
            if (!CanRecruitCompanion) return false;

            var template = MBObjectManager.Instance.GetObject<CharacterObject>(WitchHunterWandererTemplateId);
            if (template == null) return false;

            // Spawn in the lodge town; TOR_wanderer_empire_2 is "{FIRSTNAME} the
            // Witch Hunter" (confirmed in tor_charactertemplates.xml). Age is
            // arbitrary — vanilla's companion-spawn code uses the same recipe.
            var age = Campaign.Current.Models.AgeModel.HeroComesOfAge + 5 + MBRandom.RandomInt(12);
            var hero = HeroCreator.CreateSpecialHero(template, s, null, null, age);
            if (hero == null) return false;

            hero.ChangeState(Hero.CharacterStates.Active);
            hero.Clan = Clan.PlayerClan;
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, showNotification: true);

            _lastCompanionRecruitAt = CampaignTime.Now;
            newCompanion = hero;
            return true;
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
        }

        /// <summary>
        /// Roll bounty issues once per in-game day per lodge-containing settlement.
        /// One lodge can only host one offer at a time (vanilla single-slot per
        /// hero); the Minor roll fires first, Major second — if the Minor roll
        /// succeeds, the slot fills and Major silently skips. Across the seven
        /// lodges that's enough variance for both tiers to show up at the right
        /// pace once the player has unlocked Major availability.
        /// </summary>
        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement == null) return;

            var lodge = settlement.LocationComplex?.GetLocationWithId("tor_witchhunter_lodge");
            if (lodge == null || lodge.GetSceneCount() == 0) return;

            var inquisitor = settlement.HeroesWithoutParty.FirstOrDefault(h => h.IsBountyMaster());
            if (inquisitor == null) return;

            TryRollBounty(inquisitor, BountyTier.Minor);
            TryRollBounty(inquisitor, BountyTier.Major);
        }

        private void TryRollBounty(Hero inquisitor, BountyTier tier)
        {
            if (inquisitor.Issue != null) return;   // single slot filled already

            // Per-rank cap gate — don't generate what the player can't accept.
            var cap = tier == BountyTier.Minor ? MinorBountyCap : MajorBountyCap;
            var active = tier == BountyTier.Minor ? ActiveMinorBounties() : ActiveMajorBounties();
            if (cap <= 0 || active >= cap) return;

            if (MBRandom.RandomInt(0, 100) >= TORConstants.BOUNTY_QUEST_CHANCE) return;

            PotentialIssueData.StartIssueDelegate factory =
                tier == BountyTier.Minor
                    ? new PotentialIssueData.StartIssueDelegate(OnMinorBountyIssueSelected)
                    : new PotentialIssueData.StartIssueDelegate(OnMajorBountyIssueSelected);

            Campaign.Current.IssueManager.CreateNewIssue(
                new PotentialIssueData(factory, typeof(BountyIssue), IssueBase.IssueFrequency.VeryCommon),
                inquisitor);

            // Tell nameplate VMs to re-render — the lodge icon flips to its
            // "offer available" variant when the inquisitor picks up an issue.
            SettlementPOIManagerCampaignBehavior.NotifyIconsChanged();
        }

        private IssueBase OnMinorBountyIssueSelected(in PotentialIssueData pid, Hero issueOwner) => CreateBountyIssue(issueOwner, BountyTier.Minor);
        private IssueBase OnMajorBountyIssueSelected(in PotentialIssueData pid, Hero issueOwner) => CreateBountyIssue(issueOwner, BountyTier.Major);

        private static IssueBase CreateBountyIssue(Hero issueOwner, BountyTier tier)
        {
            var template = BountyTemplates.PickRandom(tier);
            if (template.IsEmpty) return null;
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
            // means the lodge icon should re-evaluate — "H!" goes back to "H" once
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
            dataStore.SyncData("_wh_inq_last_companion_recruit", ref _lastCompanionRecruitAt);
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
                    foreach (var q in quests.OfType<Bounties.BountyQuest>())
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
