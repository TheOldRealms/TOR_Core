using System.Collections.Generic;
using Helpers;
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

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Adds the "Visit the Wizard Hall" option to vanilla's "town" game menu and routes the
    /// consequence through the standard LocationEncounter flow so the vanilla Location
    /// machinery (scene load, LocationCharactersAreReadyToSpawnEvent, exit back to town)
    /// works out of the box.
    ///
    /// Access rules live in <see cref="TOR_Core.Models.TORSettlementAccessModel"/> — this
    /// behavior only presents the result and decides hide-vs-gray via the menu-condition
    /// translator described in
    /// <c>Dokumente/TORTasks/town-pois-and-access-model/architecture.md</c>.
    /// </summary>
    public class WizardHallCampaignBehavior : CampaignBehaviorBase
    {
        private const string LocationId = "tor_wizardhall";

        /// <summary>
        /// (settlement.StringId:poiId) pairs for which the player has already seen the
        /// first-visit denial banner. Persisted so the banner fires exactly once per town
        /// across saves, matching how vanilla's "You have arrived at X" one-shots work.
        /// </summary>
        private HashSet<string> _firstVisitSeen = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_firstVisitSeen", ref _firstVisitSeen);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                menuId: "town",
                optionId: LocationId,
                optionText: TORTextHelper.GetTextForNative(
                    "tor_wizardhall_menu_entry", "Visit the Wizard Hall"),
                condition: HallMenuCondition,
                consequence: HallMenuConsequence,
                isLeave: false,
                index: 4);
        }

        private bool HallMenuCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            bool canEnter = Campaign.Current.Models.SettlementAccessModel
                .CanMainHeroAccessLocation(Settlement.CurrentSettlement, LocationId,
                                           out bool disableOption, out TextObject disabledText);

            // Menu-condition translator: hide (vs. gray-with-tooltip) the option when the
            // model's denial reason is one of the "non-spoilery" cases. Reference equality
            // works because Reasons.* are static-readonly fields in the same assembly.
            if (!canEnter && (ReferenceEquals(disabledText, Reasons.Undiscovered)
                              || ReferenceEquals(disabledText, Reasons.NotAHallHere)))
                return MenuHelper.SetOptionProperties(args, false, false, null);

            return MenuHelper.SetOptionProperties(args, canEnter, disableOption, disabledText);
        }

        private void HallMenuConsequence(MenuCallbackArgs args)
        {
            // vanilla lordshall-style transition: swap current Location to the hall via
            // LocationEncounter. Triggers LocationCharactersAreReadyToSpawnEvent, which
            // the NPC-population behavior (task 8) subscribes to.
            var manager = Campaign.Current.GameMenuManager;
            manager.NextLocation = LocationComplex.Current.GetLocationWithId(LocationId);
            manager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(manager.NextLocation);
            manager.NextLocation = null;
            manager.PreviousLocation = null;
        }

        /// <summary>
        /// Tier 2 notification: on the first time the main party enters a town where the hall
        /// exists but the main hero is denied, surface the denial reason as an
        /// <see cref="MBInformationManager.AddQuickInformation"/> banner. Makes the POI
        /// discoverable to players who might never otherwise notice the grayed menu entry.
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

            // Skip if the player is allowed (no reason to notify them).
            if (canEnter) return;

            // Skip the two "non-spoilery" states — the hall isn't here, or hasn't been
            // discovered yet. Same translator rule as the menu-condition hide branch.
            if (ReferenceEquals(disabledText, Reasons.NotAHallHere)
                || ReferenceEquals(disabledText, Reasons.Undiscovered))
                return;

            // Hall exists in this town but main hero is blocked — fire the banner once.
            MBInformationManager.AddQuickInformation(disabledText);
            _firstVisitSeen.Add(key);
        }
    }
}
