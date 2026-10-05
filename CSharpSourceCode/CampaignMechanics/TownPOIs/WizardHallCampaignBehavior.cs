using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
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

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

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
    }
}
