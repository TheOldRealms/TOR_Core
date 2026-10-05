using Helpers;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Framework;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Adds "Visit the Witch Hunter Lodge" to the main town menu for the seven Empire towns
    /// that host a chapterhouse (see <see cref="HallLocations.IsWitchHunterLodgeSettlement"/>).
    /// Race-locked to humans; no career gate; no NPC population (empty scene — a POI the
    /// player walks into without a resident trainer).
    ///
    /// <para>Access rules live in
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel"/>.
    /// This behavior only owns menu presentation and the first-visit banner.</para>
    /// </summary>
    public class WitchHunterLodgeCampaignBehavior : CampaignBehaviorBase
    {
        private const string LocationId = HallLocations.WitchHunterLodgeLocationId;

        /// <summary>
        /// (settlement.StringId:poiId) pairs for which the player has already seen the
        /// first-visit denial banner. Persisted so the banner fires at most once per town.
        /// </summary>
        private HashSet<string> _firstVisitSeen = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_firstVisitSeen", ref _firstVisitSeen);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Registered under "town_artisan" alongside Wizard Hall — all faction-hall POIs
            // cluster together in one submenu rather than scattering across the main town
            // menu. Final position pinned via RearrangeTownMenus in OnAfterSessionLaunched.
            starter.AddGameMenuOption(
                menuId: "town_artisan",
                optionId: LocationId,
                optionText: TORTextHelper.GetTextForNative(
                    "tor_poi_witchhunter_lodge.EnterLabel", "Visit the Witch Hunter Lodge"),
                condition: LodgeMenuCondition,
                consequence: LodgeMenuConsequence,
                isLeave: false);
        }

        /// <summary>
        /// Pinned directly above the submenu's "Leave" option so the entry can never end up
        /// below Leave regardless of the order in which hall behaviors' OnAfterSessionLaunched
        /// handlers fire (anchoring against another hall's option would be order-dependent).
        /// </summary>
        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            var artisanMenu = Campaign.Current.GameMenuManager.GetGameMenu("town_artisan");
            TORSettlementMenuHelpers.RearrangeTownMenus(artisanMenu, LocationId, "town_artisan_leave", above: true);
        }

        private bool LodgeMenuCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            bool canEnter = Campaign.Current.Models.SettlementAccessModel
                .CanMainHeroAccessLocation(Settlement.CurrentSettlement, LocationId,
                                           out bool disableOption, out TextObject disabledText);

            // Hide-vs-gray translator. Reference equality works on the static-readonly
            // sentinel fields in WitchHunterLodgeReasons.
            if (!canEnter && (ReferenceEquals(disabledText, WitchHunterLodgeReasons.Undiscovered)
                              || ReferenceEquals(disabledText, WitchHunterLodgeReasons.Sealed)))
                return MenuHelper.SetOptionProperties(args, false, false, null);

            return MenuHelper.SetOptionProperties(args, canEnter, disableOption, disabledText);
        }

        private void LodgeMenuConsequence(MenuCallbackArgs args)
        {
            // Defensive re-check: vanilla's GameMenu.RunMenuOptionConsequence doesn't gate on
            // IsEnabled, so a grayed option can still fire via UI edge cases. The condition
            // sets IsEnabled correctly; this is the belt-and-braces lock.
            if (!Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(
                    Settlement.CurrentSettlement, LocationId, out _, out _))
                return;

            var manager = Campaign.Current.GameMenuManager;
            manager.NextLocation = LocationComplex.Current.GetLocationWithId(LocationId);
            manager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(manager.NextLocation);
            manager.NextLocation = null;
            manager.PreviousLocation = null;
        }

        /// <summary>
        /// Tier 2 notification: show the denial reason once per town on first entry where
        /// the lodge exists but the main hero is blocked.
        /// </summary>
        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty || settlement == null || !settlement.IsTown)
                return;

            var key = settlement.StringId + ":" + LocationId;
            if (_firstVisitSeen.Contains(key))
                return;

            bool canEnter = Campaign.Current.Models.SettlementAccessModel
                .CanMainHeroAccessLocation(settlement, LocationId,
                                           out _, out TextObject disabledText);

            if (canEnter) return;

            // Same hide-condition as the menu translator — don't surface never-should-see text.
            if (ReferenceEquals(disabledText, WitchHunterLodgeReasons.Sealed)
                || ReferenceEquals(disabledText, WitchHunterLodgeReasons.Undiscovered))
                return;

            MBInformationManager.AddQuickInformation(disabledText);
            _firstVisitSeen.Add(key);
        }
    }
}
