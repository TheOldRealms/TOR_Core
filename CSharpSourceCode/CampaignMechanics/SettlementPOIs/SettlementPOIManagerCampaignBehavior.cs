using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Framework;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Single campaign-lifecycle entry point for every TOR settlement POI. Also owns the
    /// static <c>LocationId → <see cref="SettlementPOI"/></c> lookup used by
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel"/> and by POI-specific helpers.
    ///
    /// <para>POIs register themselves automatically: on session launch the manager scans the
    /// TOR_Core assembly for every concrete <see cref="SettlementPOI"/> subclass with a
    /// parameterless constructor and instantiates one of each into the registry. Adding a
    /// new POI is just a new subclass file — SubModule.cs needs no changes.</para>
    ///
    /// <para>No per-POI state is persisted — this behavior exists only to drive menu wiring
    /// and expose the registry. Access-rule evaluation happens via
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel"/> dispatching to each POI's
    /// <see cref="SettlementPOI.CheckAccess"/>.</para>
    /// </summary>
    public class SettlementPOIManagerCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly Dictionary<string, SettlementPOI> _byLocationId = new();

        public static IEnumerable<SettlementPOI> All => _byLocationId.Values;

        public static SettlementPOI Get(string locationId) =>
            _byLocationId.TryGetValue(locationId, out var poi) ? poi : null;

        // Central registry of POI residents.
        // Key: "settlementId|locationId" → list of hero StringIds living at that POI in that settlement.
        private Dictionary<string, List<string>> _residents = new();

        // ------- Lifecycle

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_poiResidents", ref _residents);
        }

        public Hero GetOrCreateHero(Settlement s, string locationId, string templateId)
        {
            if (s == null || string.IsNullOrEmpty(locationId) || string.IsNullOrEmpty(templateId)) return null;

            var key = $"{s.StringId}|{locationId}";
            if (!_residents.TryGetValue(key, out var ids))
                _residents[key] = ids = new List<string>();

            // Dedup by template: if a resident of this POI already has this template, reuse.
            // Natural handling of "two role interfaces, same template" (Dawi: spell trainer +
            // enchanter → same template → one hero plays both).
            foreach (var id in ids)
            {
                var h = Hero.FindFirst(x => x.StringId == id);
                if (h != null && h.IsAlive && h.Template?.StringId == templateId) return h;
            }

            var template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);
            if (template == null) return null;

            var hero = HeroCreator.CreateSpecialHero(template, s, null, null, 50);
            hero.SupporterOf = s.OwnerClan;
            hero.Culture = s.Culture;
            // Clone the TextObject so SetTextVariable doesn't mutate the shared template-level
            // instance (which otherwise causes every hero of this template to display the last-set FIRSTNAME).
            var name = template.GetName().CopyTextObject();
            name.SetTextVariable("FIRSTNAME", hero.FirstName);
            hero.SetName(name, hero.FirstName);
            hero.CharacterObject.HiddenInEncyclopedia = true;
            HeroHelper.SpawnHeroForTheFirstTime(hero, s);
            ids.Add(hero.StringId);
            return hero;
        }

        public IEnumerable<Hero> GetResidents(Settlement s, string locationId)
        {
            var key = $"{s.StringId}|{locationId}";
            if (!_residents.TryGetValue(key, out var ids)) yield break;
            foreach (var id in ids)
            {
                var h = Hero.FindFirst(x => x.StringId == id);
                if (h != null) yield return h;
            }
        }

        private void OnBeforeMissionOpened()
        {
            var s = Settlement.CurrentSettlement;
            if (s?.IsTown == true) PlaceTrainersFor(s);
        }

        private void PlaceAllTrainersEverywhere()
        {
            foreach (var s in Settlement.All)
            {
                if (!s.IsTown) continue;
                PlaceTrainersFor(s);
            }
        }

        private void PlaceTrainersFor(Settlement s)
        {
            foreach (var host in _byLocationId.Values.OfType<ISpellTrainerHost>())
                host.EnsurePlaced(s);
            foreach (var host in _byLocationId.Values.OfType<IEnchanterHost>())
                host.EnsurePlaced(s);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            DiscoverAndRegisterAllPOIs();

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

            // Defensive sweep — verify every POI's expected trainer is placed in their
            // applicable settlement, covering both fresh campaigns (replaces OnNewGameCreated)
            // and loaded saves.
            PlaceAllTrainersEverywhere();
        }

        private static void DiscoverAndRegisterAllPOIs()
        {
            if (_byLocationId.Count > 0) return;

            foreach (var t in typeof(SettlementPOI).Assembly.GetTypes()
                                 .Where(t => t.IsSubclassOf(typeof(SettlementPOI)) && !t.IsAbstract))
            {
                var poi = (SettlementPOI)Activator.CreateInstance(t);
                _byLocationId[poi.LocationId] = poi;
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
