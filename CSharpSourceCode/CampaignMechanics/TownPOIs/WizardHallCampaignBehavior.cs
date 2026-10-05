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
using TOR_Core.Framework;

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
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_firstVisitSeen", ref _firstVisitSeen);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Registered under "town_artisan" (not "town") — "Visit the Wizard Hall" sits as a
            // sibling of "Visit the enchanter" inside the artisan district submenu, which is
            // thematically coherent (same Imperial Magister NPC underpins both options).
            // Final position within the submenu is pinned via RearrangeTownMenus in
            // OnAfterSessionLaunched.
            starter.AddGameMenuOption(
                menuId: "town_artisan",
                optionId: LocationId,
                optionText: TORTextHelper.GetTextForNative(
                    "tor_poi_wizardhall.EnterLabel", "Visit the Wizard Hall"),
                condition: HallMenuCondition,
                consequence: HallMenuConsequence,
                isLeave: false);
        }

        /// <summary>
        /// Fires after all behaviors have registered their menu options, so the entry we want
        /// to anchor against ("town_artisan_enchanting") definitely exists. Pins "Visit the
        /// Wizard Hall" directly after "Visit the enchanter" — grouping the two Imperial
        /// Magister entry points.
        /// </summary>
        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            var artisanMenu = Campaign.Current.GameMenuManager.GetGameMenu("town_artisan");
            TORSettlementMenuHelpers.RearrangeTownMenus(artisanMenu, LocationId, "town_artisan_enchanting");
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
            // Defensive re-check: vanilla's GameMenu.RunMenuOptionConsequence (GameMenu.cs:266-
            // 283) doesn't gate on IsEnabled, so a grayed option can still fire the consequence
            // via keyboard shortcuts or UI edge cases. The condition sets IsEnabled correctly,
            // but we also hard-fail here so non-eligible heroes cannot sneak into the hall.
            if (!Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(
                    Settlement.CurrentSettlement, LocationId, out _, out _))
                return;

            // vanilla lordshall-style transition: swap current Location to the hall via
            // LocationEncounter. Triggers LocationCharactersAreReadyToSpawnEvent.
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
