using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using System.Collections.Generic;
using System.Linq;
using TOR_Core.CampaignMechanics.CompanionRecruitment;
using TOR_Core.CampaignMechanics.WitchHunter;
using TOR_Core.CampaignMechanics.WitchHunter.Bounties;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Witch Hunter Chapterhouse POI — the Order of Sigmar chapterhouses. Reachable from the
    /// <c>town_artisan</c> submenu in whichever Empire settlements provide a scene override
    /// for <c>tor_witchhunter_chapterhouse</c> in <c>tor_settlements.xml</c> (currently Wurtbad,
    /// Leicheberg, Meissen, Bögenhafen, Bechafen, Eisental, Ferlangen). Race-locked to
    /// humans; no career gate. The chapterhouse NPC — currently the relocated BountyMaster —
    /// hosts the hub dialog defined in <see cref="RegisterDialogs"/>.
    /// </summary>
    public class WitchHunterChapterhousePOI : SettlementPOI, ISkillTrainerHost, ICompanionRecruitmentHost
    {
        public override string LocationId => "tor_witchhunter_chapterhouse";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;    // sit directly above "Leave"

        public override string EnterLabelId => "tor_poi_witchhunter_chapterhouse.EnterLabel";
        public override string EnterLabelDefault => "Visit the Witch Hunter Chapterhouse";

        public override string MapIconText => "H";   // text fallback until sprite art lands

        /// <summary>
        /// Per-settlement variant: show "H!" when the chapterhouse's inquisitor is
        /// carrying an unaccepted Minor Bounty offer — visual hint on the map
        /// that there's work waiting. Falls back to the plain "H" otherwise.
        /// </summary>
        public override string MapIconTextFor(Settlement s)
        {
            if (s == null) return MapIconText;
            var inquisitor = s.HeroesWithoutParty.FirstOrDefault(h => h.IsBountyMaster());
            if (inquisitor?.Issue is BountyIssue) return "H!";
            return MapIconText;
        }

        /// <summary>
        /// Template id for the chapterhouse's resident Inquisitor. Historically owned by the now-
        /// retired <c>BountyMasterCampaignBehavior</c>; moving the lifecycle into the POI
        /// framework collapsed that behavior but kept the same hero template so existing
        /// saves adopt their current bounty-master hero into the chapterhouse via
        /// <see cref="SettlementPOIManagerCampaignBehavior.GetOrCreateHero"/>'s orphan scan.
        /// </summary>
        public string InquisitorTemplateId  => "tor_bountymaster_empire_0";
        public string ScoutMasterTemplateId => "tor_scoutmaster_empire_0";

        // Two residents per chapterhouse: the Inquisitor (Crossbow trainer) and
        // the Order Scout (Scouting trainer). One hero → one skill is the
        // convention; multi-skill locations grow by adding more NPCs.
        public override void EnsurePlaced(Settlement s)
        {
            PlaceResident(s, InquisitorTemplateId);
            PlaceResident(s, ScoutMasterTemplateId);
        }

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s))
            {
                denialReason = R("Undiscovered", "You have not yet found this place.");
                return false;
            }
            if (!AppliesTo(s))
            {
                denialReason = R("Sealed", "This place is sealed.");
                return false;
            }
            if (s.IsUnderSiege)
            {
                denialReason = R("SiegeSealed", "The chapterhouse is closed during the siege.");
                return false;
            }
            // Race lock — humans only. No career gate (the chapterhouse welcomes any human visitor).
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
            {
                denialReason = R("Rejected", "The templars turn you away.");
                return false;
            }

            denialReason = null;
            return true;
        }

        /// <summary>
        /// Inquisitor hub dialog. Design doc specifies six branches (plus Leave):
        /// Hunts, Bounties, Turn-ins, Store, Companion Recruitment, Witch Hunter
        /// Exclusive. Hunts and Store are rep-gated behind the <c>INQUISITION_KNOWN</c>
        /// attribute (and store further on <c>INQUISITION_RESPECTED</c> for the full
        /// tier — see T6). Witch Hunter Exclusive is career-gated. Companion
        /// Recruitment is open from Unknown rank per the design.
        /// </summary>
        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            // Greeting — only fires when the player speaks to the chapterhouse's resident
            // bounty-master-turned-Inquisitor inside the chapterhouse location. Guards against
            // the hub appearing in a tavern where the bounty master may still live
            // (Empire towns without a chapterhouse). Also skips when the Inquisitor is
            // carrying a vanilla-offerable issue (Hunt or Bounty) that hasn't been
            // accepted yet — so vanilla's natural start → hero_main_options → issue-intro
            // chain fires and the player sees the quest-offer dialog directly. Once the
            // issue converts to an accepted quest (IssueQuest != null) the greet returns
            // to full duty and our hub (turn-ins, store, etc.) is reachable again.
            obj.AddDialogLine("tor_witchhunter_chapterhouse_greet", "start", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "hub_greet",
                    "What brings you to the Chapterhouse?"),
                () => IsInquisitorPartner() && !InquisitorHasOfferableIssue(), null, 200, null);

            // Scout Master's mini-hub — a separate dialog branch so the Inquisitor
            // keeps his Hunts/Bounties/Turn-ins/Store/etc. tree uncluttered. The
            // training line attaches to this state via SkillTrainerConfig.HubState.
            obj.AddDialogLine("tor_witchhunter_chapterhouse_scout_greet", "start", "tor_witchhunter_chapterhouse_scout_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "scout_greet",
                    "The Order sent you? Fine. What do you need?"),
                IsScoutPartner, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_scout_leave", "tor_witchhunter_chapterhouse_scout_hub", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "scout_leave",
                    "That is all. Good hunting."),
                null, null, 100, null);

            // --- Hunts: two variants with identical player text. When the Inquisitor
            // actually has a Hunt issue, the greet above skips entirely and vanilla's
            // native issue-intro fires at `start` — no player line needed here. So
            // this branch only handles the no-issue and rank-locked cases.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_hunts_none", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_hunts_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                () => IsKnown() && !InquisitorHasHunt(), null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_hunts_locked", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_notrust_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                () => !IsKnown(), null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_bounties_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_bounties",
                    "Are there any bounties the Inquisition needs done?"),
                null, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_turnins", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_turnins_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_turnins",
                    "Lord Inquisitor! I have some things you might be interested in."),
                null, null, 200, null);

            // --- Store: gated by rank; T6 adds the tiered roster inside the stub.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_store", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_store_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_store",
                    "I wish to acquire some Inquisition armaments."),
                IsKnown, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_store_locked", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_notrust_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_store",
                    "I wish to acquire some Inquisition armaments."),
                () => !IsKnown(), null, 200, null);

            // Companion Recruitment player line + NPC replies are registered by
            // CompanionRecruitmentBehavior from the config returned by
            // GetCompanionRecruits() below. Keeps the chapterhouse's dialog file free
            // of per-template companion plumbing.

            // --- Witch Hunter Exclusive: career-gated; design doc explicitly defers the mechanic.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_exclusive", "tor_witchhunter_chapterhouse_hub", "tor_witchhunter_chapterhouse_exclusive_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_exclusive",
                    "What of the Order's business for one of our own?"),
                IsWitchHunterCareer, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_leave", "tor_witchhunter_chapterhouse_hub", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "player_leave",
                    "Have a good day Lord Inquisitor!"),
                null, null, 200, null);

            // --- stub replies
            obj.AddDialogLine("tor_witchhunter_chapterhouse_hunts_todo", "tor_witchhunter_chapterhouse_hunts_stub", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "hunts_todo",
                    "Return soon — I will have hunts to assign."),
                null, null, 200);

            // --- Bounties dispatcher: three independent concerns per the design doc.
            // Pending failure (minor/major) must be excused before new bounties of that
            // tier can be taken. Voluntary abandonment opens an inquiry listing active
            // quests ≥ 3 days old (Phase B wires the real bounty quests; for now the
            // condition returns false, keeping the line hidden). Normal offers run only
            // when the player is under cap and has no pending failure of that tier.
            obj.AddDialogLine("tor_witchhunter_chapterhouse_bounties_hub_npc", "tor_witchhunter_chapterhouse_bounties_stub", "tor_witchhunter_chapterhouse_bounties_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "bounties_hub",
                    "What business will you take up with the Order?"),
                null, null, 200);

            // Excuse — minor
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_excuse_minor", "tor_witchhunter_chapterhouse_bounties_menu", "tor_witchhunter_chapterhouse_bounties_excuse_minor_npc",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "excuse_minor",
                    "I beg forgiveness for the last minor hunt."),
                HasMinorFailurePending, ExcuseMinorFailure, 200, null);
            obj.AddDialogLine("tor_witchhunter_chapterhouse_bounties_excuse_minor_npc", "tor_witchhunter_chapterhouse_bounties_excuse_minor_npc", "tor_witchhunter_chapterhouse_bounties_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "excuse_minor_reply",
                    "Granted. See the next one through to its end."),
                null, null, 200);

            // Excuse — major
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_excuse_major", "tor_witchhunter_chapterhouse_bounties_menu", "tor_witchhunter_chapterhouse_bounties_excuse_major_npc",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "excuse_major",
                    "I beg forgiveness for the last major hunt."),
                HasMajorFailurePending, ExcuseMajorFailure, 200, null);
            obj.AddDialogLine("tor_witchhunter_chapterhouse_bounties_excuse_major_npc", "tor_witchhunter_chapterhouse_bounties_excuse_major_npc", "tor_witchhunter_chapterhouse_bounties_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "excuse_major_reply",
                    "The Order is slow to forgive, but you have paid for it. See the next through."),
                null, null, 200);

            // Turn in a completed bounty — any chapterhouse inquisitor honours it, not just
            // the one who gave it, so the dialog line is identical at every chapterhouse.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_turnin", "tor_witchhunter_chapterhouse_bounties_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "bounties_turnin",
                    "I have come to collect my due. The work is done."),
                HasTurnInBounty, OpenTurnInBountyInquiry, 200, null);

            // Voluntary abandon — hidden until Phase B wires an active-quest check.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_abandon", "tor_witchhunter_chapterhouse_bounties_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "bounties_abandon",
                    "I cannot finish a bounty."),
                HasAbandonableBounty, OpenAbandonBountyInquiry, 200, null);

            // Ask for a new minor bounty. When the Inquisitor actually has a Minor
            // bounty on offer, the greet skip at `start` lets vanilla's native
            // issue-intro fire directly — the player sees the full brief/accept
            // dialog without ever entering our sub-menu. Only the no-offer stub
            // ("Nothing in the ledger right now") lives here.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_ask_minor_stub", "tor_witchhunter_chapterhouse_bounties_menu", "tor_witchhunter_chapterhouse_bounties_offer_minor_npc",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "ask_minor",
                    "Are there minor bounties the Order needs seen to?"),
                () => CanOfferMinor() && !InquisitorHasMinorBounty(), null, 200, null);

            obj.AddDialogLine("tor_witchhunter_chapterhouse_bounties_offer_minor_npc", "tor_witchhunter_chapterhouse_bounties_offer_minor_npc", "tor_witchhunter_chapterhouse_bounties_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "offer_minor_stub",
                    "Nothing in the ledger right now — return in a few days."),
                null, null, 200);

            // Major bounty symmetrical: no-offer stub only; vanilla handles the
            // actual offer flow via greet-skip at `start`.
            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_ask_major_stub", "tor_witchhunter_chapterhouse_bounties_menu", "tor_witchhunter_chapterhouse_bounties_offer_major_npc",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "ask_major",
                    "Are there major bounties the Order needs seen to?"),
                () => CanOfferMajor() && !InquisitorHasMajorBounty(), null, 200, null);

            obj.AddDialogLine("tor_witchhunter_chapterhouse_bounties_offer_major_npc", "tor_witchhunter_chapterhouse_bounties_offer_major_npc", "tor_witchhunter_chapterhouse_bounties_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "offer_major_stub",
                    "The Order's heavier hunts can wait. Return when the days have turned."),
                null, null, 200);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_bounties_back", "tor_witchhunter_chapterhouse_bounties_menu", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "bounties_back",
                    "That is all for now."),
                null, null, 200, null);

            // Shared "not yet trusted" fallback — Hunts and Store both land here below rank.
            obj.AddDialogLine("tor_witchhunter_chapterhouse_notrust", "tor_witchhunter_chapterhouse_notrust_stub", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "notrust",
                    "Return once you have earned our trust."),
                null, null, 200);

            obj.AddDialogLine("tor_witchhunter_chapterhouse_exclusive_todo", "tor_witchhunter_chapterhouse_exclusive_stub", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "exclusive_todo",
                    "The Order has nothing for you yet, brother. Return in time."),
                null, null, 200);

            // --- turn-ins dispatcher
            // NPC pivots the player into the sub-menu with a short line. The materials
            // and prisoners sub-flows live in WitchHunterTurnInHelper and each take over
            // the UI entirely (inquiry popup / donate-to-town screen), so both target
            // close_window as their consequence point.
            obj.AddDialogLine("tor_witchhunter_chapterhouse_turnins_open", "tor_witchhunter_chapterhouse_turnins_stub", "tor_witchhunter_chapterhouse_turnins_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "turnins_open",
                    "What have you brought the Order?"),
                null, null, 200);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_turnins_materials", "tor_witchhunter_chapterhouse_turnins_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "turnins_materials",
                    "Materials for the pyre — scrolls, warpstone."),
                null, WitchHunterTurnInHelper.OpenMaterialTurnIn, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_turnins_prisoners", "tor_witchhunter_chapterhouse_turnins_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "turnins_prisoners",
                    "Prisoners for the Order to deal with."),
                null, WitchHunterTurnInHelper.OpenPrisonerTurnIn, 200, null);

            obj.AddPlayerLine("tor_witchhunter_chapterhouse_turnins_done", "tor_witchhunter_chapterhouse_turnins_menu", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "turnins_done",
                    "That is all for now."),
                null, null, 200, null);

            // --- store (flat roster: witch-hunter kit from the three canonical templates,
            // plus musket-ball + silver-bullet ammo). Opens as trade so the player can both
            // buy and sell gear in one screen.
            obj.AddDialogLine("tor_witchhunter_chapterhouse_store_open", "tor_witchhunter_chapterhouse_store_stub", "tor_witchhunter_chapterhouse_store_reply",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "store_open",
                    "The Order's wares. These will serve you better than tavern-forged scrap."),
                null, OpenInquisitionStore, 200);
            obj.AddDialogLine("tor_witchhunter_chapterhouse_store_reply", "tor_witchhunter_chapterhouse_store_reply", "tor_witchhunter_chapterhouse_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_chapterhouse", "store_reply",
                    "What else can I do for you?"),
                null, null, 200);
        }

        private static bool IsInquisitorPartner()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject == null) return false;
            if (!partner.HeroObject.IsBountyMaster()) return false;

            // Scope strictly to the chapterhouse location — the same hero may also appear in a
            // tavern (Empire towns without a chapterhouse), and the hub must not show there.
            var complex = LocationComplex.Current;
            var loc = complex?.GetLocationOfCharacter(partner.HeroObject);
            return loc != null && loc.StringId == "tor_witchhunter_chapterhouse";
        }

        // Mirror of IsInquisitorPartner, scoped to the Scout Master's template.
        // No BountyMaster attribute check — the Scout isn't one.
        private static bool IsScoutPartner()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject?.Template == null) return false;
            if (partner.HeroObject.Template.StringId != "tor_scoutmaster_empire_0") return false;

            var complex = LocationComplex.Current;
            var loc = complex?.GetLocationOfCharacter(partner.HeroObject);
            return loc != null && loc.StringId == "tor_witchhunter_chapterhouse";
        }

        private static bool InquisitorHasHunt()
        {
            // Hunts covers anything that ISN'T a bounty — HuntCultists, PlaguedVillage,
            // and whatever story-driven issue types land later. Bounties have their own
            // branch, so filter BountyIssue (both tiers) out.
            var issue = CharacterObject.OneToOneConversationCharacter?.HeroObject?.Issue;
            if (issue == null) return false;
            if (issue is BountyIssue) return false;
            return true;
        }

        /// <summary>
        /// True when the Inquisitor is carrying an Issue that vanilla can offer
        /// directly — i.e. the issue hasn't been accepted as a quest yet. Used
        /// by the greet to step aside so vanilla's native start →
        /// hero_main_options → issue-intro chain fires instead of routing the
        /// player into our hub.
        /// </summary>
        private static bool InquisitorHasOfferableIssue()
        {
            var issue = CharacterObject.OneToOneConversationCharacter?.HeroObject?.Issue;
            return issue != null && issue.IssueQuest == null;
        }

        private static bool InquisitorHasMinorBounty() => InquisitorBountyTier() == BountyTier.Minor;
        private static bool InquisitorHasMajorBounty() => InquisitorBountyTier() == BountyTier.Major;

        private static BountyTier? InquisitorBountyTier()
        {
            var issue = CharacterObject.OneToOneConversationCharacter?.HeroObject?.Issue;
            return (issue is BountyIssue b) ? b.Tier : (BountyTier?)null;
        }

        private static bool IsKnown() =>
            Hero.MainHero?.HasAttribute(WitchHunterInquisitionBehavior.AttrKnown) == true;

        private static bool IsWitchHunterCareer() =>
            Hero.MainHero != null && Hero.MainHero.HasCareer(TORCareers.WitchHunter);

        private static WitchHunterInquisitionBehavior Inq() =>
            Campaign.Current?.GetCampaignBehavior<WitchHunterInquisitionBehavior>();

        private static bool HasMinorFailurePending() => Inq()?.MinorFailurePending == true;
        private static bool HasMajorFailurePending() => Inq()?.MajorFailurePending == true;
        private static void ExcuseMinorFailure() => Inq()?.ExcuseMinorFailure();
        private static void ExcuseMajorFailure() => Inq()?.ExcuseMajorFailure();

        private static bool CanOfferMinor()
        {
            var inq = Inq();
            if (inq == null) return false;
            return inq.MinorBountyCap > 0 && !inq.MinorFailurePending && inq.ActiveMinorBounties() < inq.MinorBountyCap;
        }

        private static bool CanOfferMajor()
        {
            var inq = Inq();
            if (inq == null) return false;
            return inq.MajorBountyCap > 0 && !inq.MajorFailurePending && inq.ActiveMajorBounties() < inq.MajorBountyCap;
        }

        private static bool HasAbandonableBounty()
        {
            var quests = Campaign.Current?.QuestManager?.Quests;
            if (quests == null) return false;
            // Phase C: || q is MajorBountyQuest mjq && mjq.IsVoluntaryAbandonAvailable
            return quests.Any(q => q is BountyQuest mq && mq.IsVoluntaryAbandonAvailable);
        }

        private static bool HasTurnInBounty()
        {
            var quests = Campaign.Current?.QuestManager?.Quests;
            if (quests == null) return false;
            // Phase C: || q is MajorBountyQuest mjq && mjq.IsReadyForTurnIn
            return quests.Any(q => q is BountyQuest mq && mq.IsReadyForTurnIn);
        }

        private static void OpenTurnInBountyInquiry()
        {
            var quests = Campaign.Current?.QuestManager?.Quests;
            if (quests == null) return;

            var turnable = quests.OfType<BountyQuest>()
                .Where(q => q.IsReadyForTurnIn)
                .ToList();
            if (turnable.Count == 0) return;

            var elements = turnable
                .Select(q => new InquiryElement(q, q.Title.ToString(), null))
                .ToList();

            var inq = new MultiSelectionInquiryData(
                TORTextHelper.GetText("tor_wh_bounty_turnin_title", "Collect Your Due"),
                TORTextHelper.GetText("tor_wh_bounty_turnin_desc", "Which bounty will you claim payment for? Any chapterhouse will honour it."),
                elements, true, 1, elements.Count,
                TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"),
                TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                picked => TurnInSelectedBounties(picked),
                null);
            MBInformationManager.ShowMultiSelectionInquiry(inq, true);
        }

        private static void TurnInSelectedBounties(List<InquiryElement> picked)
        {
            if (picked == null) return;
            foreach (var el in picked)
                if (el.Identifier is BountyQuest q) q.TurnInAndComplete();
        }

        private static void OpenAbandonBountyInquiry()
        {
            var quests = Campaign.Current?.QuestManager?.Quests;
            if (quests == null) return;

            var abandonable = quests
                .OfType<BountyQuest>()
                .Where(q => q.IsVoluntaryAbandonAvailable)
                .Cast<QuestBase>()
                .ToList();
            if (abandonable.Count == 0) return;

            var elements = abandonable
                .Select(q => new InquiryElement(q, q.Title.ToString(), null))
                .ToList();

            var inq = new MultiSelectionInquiryData(
                TORTextHelper.GetText("tor_wh_bounty_abandon_title", "Which bounty will you give up on?"),
                TORTextHelper.GetText("tor_wh_bounty_abandon_desc", "The Order will note your choice. No reward will follow."),
                elements, true, 1, 1,
                TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"),
                TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                picked => AbandonSelectedBounty(picked),
                null);
            MBInformationManager.ShowMultiSelectionInquiry(inq, true);
        }

        private static void AbandonSelectedBounty(List<InquiryElement> picked)
        {
            if (picked == null || picked.Count == 0) return;
            if (picked[0].Identifier is QuestBase quest) quest.CompleteQuestWithCancel();
        }

        // Two-tier roster per the design: basic unlocks at Known (INQUISITION_KNOWN),
        // full set adds in at Respected (INQUISITION_RESPECTED). "Basic" is the
        // lower-end witch hunter kit — plain hat, boots, gloves, rapier mk1, handgun
        // mk1, standard ball. "Full-tier adds" layer in the heavier armor pieces,
        // the companion-grade hat, upgraded rapier + flintlock pistols, and the
        // thematic silver bullets.
        private static readonly string[] StoreTierBasic = new[]
        {
            "tor_empire_head_hat_witch_hunter_001",
            "tor_empire_arm_gauntlet_witch_hunter_001",
            "tor_empire_leg_boots_witch_hunter_001",
            "tor_empire_weapon_rapier_001",
            "tor_empire_weapon_gun_handgun_003",
            "tor_neutral_weapon_ammo_musket_ball",
            "tor_empire_weapon_crossbow_empire_crossbow_001",
            "bolt_a",
        };

        private static readonly string[] StoreTierFullAdds = new[]
        {
            "tor_empire_head_hat_witch_hunter_002",
            "tor_empire_head_hat_witch_hunter_003",
            "tor_empire_body_armor_witch_hunter_001",
            "tor_empire_shoulder_pauldron_witch_hunter_001",
            "tor_empire_weapon_rapier_002",
            "tor_empire_weapon_gun_flintlock_pistol_007",
            "tor_empire_weapon_gun_flintlock_pistol_008",
            "tor_neutral_weapon_ammo_silver_bullet",
        };

        private static void OpenInquisitionStore()
        {
            var full = Hero.MainHero?.HasAttribute(WitchHunterInquisitionBehavior.AttrRespected) == true;
            var roster = new ItemRoster();
            foreach (var id in StoreTierBasic)
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (item == null) continue;
                roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 4)));
            }
            if (full)
            {
                foreach (var id in StoreTierFullAdds)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                    if (item == null) continue;
                    roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 4)));
                }
            }
            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        // ---- ISkillTrainerHost
        // One config per trainer NPC. Both plug into the chapterhouse hub dialog
        // state so the "arrange training" line appears alongside Hunts,
        // Bounties, etc. TrainerCondition filters by partner.Template so
        // only the Crossbow line shows next to the Inquisitor and only
        // the Scouting line shows next to the Order Scout.
        public IEnumerable<SkillTrainerConfig> GetSkillTrainers()
        {
            yield return new SkillTrainerConfig(
                templateId:   InquisitorTemplateId,
                dialogSuffix: "InquisitorCrossbow",
                skillId:      "Crossbow",
                hubState:     "tor_witchhunter_chapterhouse_hub",
                reintroState: "tor_witchhunter_chapterhouse_hub");

            yield return new SkillTrainerConfig(
                templateId:   ScoutMasterTemplateId,
                dialogSuffix: "OrderScout",
                skillId:      "Scouting",
                hubState:     "tor_witchhunter_chapterhouse_scout_hub",
                reintroState: "tor_witchhunter_chapterhouse_scout_hub");
        }

        // ---- ICompanionRecruitmentHost
        // The Order's Inquisitor keeps one witch-hunter wanderer on offer at
        // a time; the slot refreshes every 60 days per the design doc.
        public IEnumerable<CompanionRecruitmentConfig> GetCompanionRecruits()
        {
            yield return new CompanionRecruitmentConfig(
                candidateTemplateIds: new[] { "tor_wanderer_empire_2" },
                hostTemplateId:       InquisitorTemplateId,
                dialogSuffix:         "WitchHunterChapterhouseHunter",
                hubState:             "tor_witchhunter_chapterhouse_hub",
                reintroState:         "tor_witchhunter_chapterhouse_hub",
                refreshDays:          60);
        }
    }
}
