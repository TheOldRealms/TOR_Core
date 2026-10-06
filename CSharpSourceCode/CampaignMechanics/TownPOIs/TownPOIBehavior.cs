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
    /// Abstract base class for TOR "town POI" behaviors (Wizard Hall, Witch Hunter Lodge,
    /// future Runehall / Engineer Hall / Chapel / Grove / Herdstone / Crypt / …).
    ///
    /// <para>Each concrete POI only needs to supply its identity (location id, menu label,
    /// parent menu id, anchor entry) and its hide-eligible denial reasons. The access rules
    /// still live in <see cref="TOR_Core.Models.TORSettlementAccessModel"/> and the per-POI
    /// settlement whitelist lives in <see cref="TownPOILocations"/> — a subsequent refactor
    /// will migrate the access predicate and whitelist onto this class so each POI is fully
    /// self-contained.</para>
    ///
    /// <para>Shared infrastructure handled here:
    /// <list type="bullet">
    ///   <item>event wiring (session launch, after-session launch, settlement entered)</item>
    ///   <item>menu-option registration + rearrangement</item>
    ///   <item>menu-condition translator (hide vs. gray-with-tooltip)</item>
    ///   <item>defensive consequence guard + vanilla <see cref="PlayerEncounter"/> transition</item>
    ///   <item>Tier 2 first-visit <see cref="MBInformationManager.AddQuickInformation"/> banner</item>
    ///   <item><see cref="IDataStore"/>-backed per-settlement "seen" state</item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class TownPOIBehavior : CampaignBehaviorBase
    {
        // ------- Per-POI identity (abstract)

        /// <summary>Location StringId this POI owns (e.g. <c>tor_wizardhall</c>). Must start with <c>tor_</c>.</summary>
        protected abstract string LocationId { get; }

        /// <summary>Game menu the POI's "visit" option is registered on (e.g. <c>town_artisan</c>).</summary>
        protected abstract string ParentMenuId { get; }

        /// <summary>Menu-option id in <see cref="ParentMenuId"/> that the POI pins itself against.</summary>
        protected abstract string AnchorEntryId { get; }

        /// <summary>True = pin above the anchor; false = pin below (right-after).</summary>
        protected virtual bool AnchorAbove => true;

        /// <summary>Localization id for the "Visit …" menu label (e.g. <c>tor_poi_wizardhall.EnterLabel</c>).</summary>
        protected abstract string EnterLabelId { get; }

        /// <summary>English fallback for the menu label when <see cref="EnterLabelId"/> is not registered.</summary>
        protected abstract string EnterLabelDefault { get; }

        /// <summary>
        /// Reference-equality sentinel for the "player hasn't discovered this POI yet" denial.
        /// The menu-condition translator hides the option rather than graying it when the
        /// access model returns this exact <see cref="TextObject"/> instance.
        /// </summary>
        protected abstract TextObject HideOnUndiscovered { get; }

        /// <summary>
        /// Reference-equality sentinel for the "this POI doesn't exist in this settlement"
        /// defensive denial. Hidden rather than grayed, same reason as above.
        /// </summary>
        protected abstract TextObject HideOnSealed { get; }

        // ------- State (SyncData)

        /// <summary>
        /// <c>"settlementId:LocationId"</c> keys for which the first-visit denial banner has
        /// already been shown. Key is namespaced by <see cref="LocationId"/> inside
        /// <see cref="SyncData"/> so multiple POI behaviors can coexist in one save.
        /// </summary>
        private HashSet<string> _firstVisitSeen = new();

        // ------- Lifecycle

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Namespace per POI id so each subclass has its own persisted set.
            var key = "_firstVisitSeen_" + LocationId;
            dataStore.SyncData(key, ref _firstVisitSeen);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                menuId: ParentMenuId,
                optionId: LocationId,
                optionText: TORTextHelper.GetTextForNative(EnterLabelId, EnterLabelDefault),
                condition: MenuCondition,
                consequence: MenuConsequence,
                isLeave: false);
        }

        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            var menu = Campaign.Current.GameMenuManager.GetGameMenu(ParentMenuId);
            TORSettlementMenuHelpers.RearrangeTownMenus(menu, LocationId, AnchorEntryId, AnchorAbove);
        }

        // ------- Menu wiring

        private bool MenuCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            bool canEnter = Campaign.Current.Models.SettlementAccessModel
                .CanMainHeroAccessLocation(Settlement.CurrentSettlement, LocationId,
                                           out bool disableOption, out TextObject disabledText);

            // Hide-vs-gray translator: the two "never-should-see" reasons collapse the option
            // out of the menu entirely rather than showing a disabled entry with tooltip.
            if (!canEnter && (ReferenceEquals(disabledText, HideOnUndiscovered)
                              || ReferenceEquals(disabledText, HideOnSealed)))
                return MenuHelper.SetOptionProperties(args, false, false, null);

            return MenuHelper.SetOptionProperties(args, canEnter, disableOption, disabledText);
        }

        private void MenuConsequence(MenuCallbackArgs args)
        {
            // Defensive re-check: vanilla's GameMenu.RunMenuOptionConsequence doesn't gate on
            // IsEnabled, so a grayed option can still fire via UI edge cases. Fail silently.
            if (!Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(
                    Settlement.CurrentSettlement, LocationId, out _, out _))
                return;

            // vanilla lordshall-style transition — triggers LocationCharactersAreReadyToSpawnEvent
            // and the normal exit-back-to-town flow.
            var manager = Campaign.Current.GameMenuManager;
            manager.NextLocation = LocationComplex.Current.GetLocationWithId(LocationId);
            manager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(manager.NextLocation);
            manager.NextLocation = null;
            manager.PreviousLocation = null;
        }

        // ------- Tier 2 notification

        /// <summary>
        /// Shows the model's denial reason as a Quick Information banner on the first time
        /// the main party enters a town where the POI exists but the main hero is blocked.
        /// Skips the two "never-should-see" states (same rule as the menu translator).
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
            if (ReferenceEquals(disabledText, HideOnSealed) || ReferenceEquals(disabledText, HideOnUndiscovered))
                return;

            MBInformationManager.AddQuickInformation(disabledText);
            _firstVisitSeen.Add(key);
        }
    }
}
