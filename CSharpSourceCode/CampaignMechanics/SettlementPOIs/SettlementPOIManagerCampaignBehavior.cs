using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Framework;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Single campaign-lifecycle entry point for every TOR settlement POI. Also owns the
    /// static <c>LocationId → <see cref="SettlementPOI"/></c> lookup used by
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel"/> and by POI-specific helpers
    /// (e.g. <see cref="WizardHallPOI.GetTrainerLocationId"/>).
    ///
    /// <para>POI classes self-register via <see cref="Register"/> from
    /// <c>SubModule.InitializeGameStarter</c>, before this behavior's event handlers fire.
    /// On session launch each registered POI gets its menu option wired; on
    /// after-session-launch each gets pinned via <see cref="TORSettlementMenuHelpers"/>.</para>
    ///
    /// <para>No per-POI state is persisted — this behavior exists only to drive menu wiring
    /// and expose the registry. Access-rule evaluation happens via
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel"/> dispatching to each POI's
    /// <see cref="SettlementPOI.CheckAccess"/>.</para>
    /// </summary>
    public class SettlementPOIManagerCampaignBehavior : CampaignBehaviorBase
    {
        // ------- Registry (static — survives behavior re-registration across campaign restarts)

        private static readonly Dictionary<string, SettlementPOI> _byLocationId = new();

        public static IEnumerable<SettlementPOI> All => _byLocationId.Values;

        public static void Register(SettlementPOI poi)
        {
            if (poi == null || string.IsNullOrEmpty(poi.LocationId)) return;
            _byLocationId[poi.LocationId] = poi;
        }

        public static SettlementPOI Get(string locationId) =>
            _byLocationId.TryGetValue(locationId, out var poi) ? poi : null;

        /// <summary>Clear the registry — only for test harnesses / reload scenarios.</summary>
        public static void Clear() => _byLocationId.Clear();

        // ------- Lifecycle

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            foreach (var poi in _byLocationId.Values)
            {
                var captured = poi;   // avoid loop-variable capture in the lambdas
                starter.AddGameMenuOption(
                    menuId: captured.ParentMenuId,
                    optionId: captured.LocationId,
                    optionText: TORTextHelper.GetTextForNative(captured.EnterLabelId, captured.EnterLabelDefault),
                    condition: args => MenuCondition(args, captured),
                    consequence: args => MenuConsequence(args, captured),
                    isLeave: false);
            }
        }

        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            foreach (var poi in _byLocationId.Values)
            {
                var menu = Campaign.Current.GameMenuManager.GetGameMenu(poi.ParentMenuId);
                TORSettlementMenuHelpers.RearrangeTownMenus(menu, poi.LocationId, poi.AnchorEntryId, poi.AnchorAbove);
            }
        }

        private static bool MenuCondition(MenuCallbackArgs args, SettlementPOI poi)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            bool canEnter = Campaign.Current.Models.SettlementAccessModel
                .CanMainHeroAccessLocation(Settlement.CurrentSettlement, poi.LocationId,
                                           out bool disableOption, out TextObject disabledText);

            // Hide-vs-gray translator: the two "never-should-see" reasons collapse the option
            // out of the menu entirely rather than showing a disabled entry with tooltip.
            if (!canEnter && (ReferenceEquals(disabledText, poi.HideOnUndiscovered)
                              || ReferenceEquals(disabledText, poi.HideOnSealed)))
                return MenuHelper.SetOptionProperties(args, false, false, null);

            return MenuHelper.SetOptionProperties(args, canEnter, disableOption, disabledText);
        }

        private static void MenuConsequence(MenuCallbackArgs args, SettlementPOI poi)
        {
            // Defensive re-check: vanilla's GameMenu.RunMenuOptionConsequence doesn't gate on
            // IsEnabled, so a grayed option can still fire via UI edge cases. Fail silently.
            if (!Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(
                    Settlement.CurrentSettlement, poi.LocationId, out _, out _))
                return;

            // vanilla lordshall-style transition — triggers LocationCharactersAreReadyToSpawnEvent
            // and the normal exit-back-to-town flow.
            var manager = Campaign.Current.GameMenuManager;
            manager.NextLocation = LocationComplex.Current.GetLocationWithId(poi.LocationId);
            manager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(manager.NextLocation);
            manager.NextLocation = null;
            manager.PreviousLocation = null;
        }
    }
}
