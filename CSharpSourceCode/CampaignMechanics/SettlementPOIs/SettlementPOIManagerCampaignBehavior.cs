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

        // Established POIs: Key "settlementId|locationId" → scene_name attached at this spot.
        // Scene_name is persisted so OnSessionLaunched can re-apply SetSceneName in case
        // Location._sceneNames isn't captured by vanilla's auto-serializer.
        private Dictionary<string, string> _establishedScenes = new();

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
            dataStore.SyncData("_poiEstablishedScenes", ref _establishedScenes);
        }

        private static string MakeEstablishKey(Settlement s, string locationId) =>
            $"{s.StringId}|{locationId}";

        public bool IsEstablished(Settlement s, SettlementPOI poi)
        {
            if (s == null || poi == null) return false;
            return _establishedScenes.ContainsKey(MakeEstablishKey(s, poi.LocationId));
        }

        public void MarkEstablished(Settlement s, SettlementPOI poi, string sceneName)
        {
            if (s == null || poi == null || string.IsNullOrEmpty(sceneName)) return;
            _establishedScenes[MakeEstablishKey(s, poi.LocationId)] = sceneName;
        }

        public void UnmarkEstablished(Settlement s, SettlementPOI poi)
        {
            if (s == null || poi == null) return;
            _establishedScenes.Remove(MakeEstablishKey(s, poi.LocationId));
        }

        /// <summary>
        /// Dynamically establish a POI at a settlement. Attaches the scene to the
        /// template-provided Location row and marks the (settlement, POI) pair as established.
        /// Idempotent — calling repeatedly is harmless.
        /// Returns true if the POI is now established (either newly or already).
        /// </summary>
        public bool EstablishAt(Settlement s, SettlementPOI poi, string sceneName)
        {
            if (s == null || poi == null || string.IsNullOrEmpty(sceneName)) return false;
            var loc = s.LocationComplex?.GetLocationWithId(poi.LocationId);
            if (loc == null) return false;   // template doesn't define this Location in the settlement's complex
            if (loc.GetSceneCount() == 0) loc.SetSceneName(0, sceneName);
            MarkEstablished(s, poi, sceneName);
            // Place any trainers that newly apply.
            foreach (var h in _byLocationId.Values.OfType<ISpellTrainerHost>())  if (ReferenceEquals(h, poi)) h.EnsurePlaced(s);
            foreach (var h in _byLocationId.Values.OfType<IEnchanterHost>())      if (ReferenceEquals(h, poi)) h.EnsurePlaced(s);
            foreach (var h in _byLocationId.Values.OfType<ISkillTrainerHost>())  if (ReferenceEquals(h, poi)) h.EnsurePlaced(s);
            return true;
        }

        private const string XmlEstablishedMarker = "<xml>";

        /// <summary>
        /// Auto-seed any (settlement, POI) pair that already has a scene via XML as
        /// "established." Vanilla re-applies the XML scene on every load, so we only need
        /// a sentinel marker — we don't need to persist the actual scene name here.
        /// For dynamically-established pairs, <see cref="EstablishAt"/> stores the real
        /// scene name instead so OnSessionLaunched can re-apply SetSceneName if vanilla's
        /// Location serializer doesn't carry our runtime-attached scene across save/load.
        /// </summary>
        private void AutoSeedXmlEstablishedPOIs()
        {
            foreach (var poi in _byLocationId.Values)
            {
                if (!poi.RequiresEstablishment) continue;
                foreach (var s in Settlement.All)
                {
                    if (!s.IsTown) continue;
                    var loc = s.LocationComplex?.GetLocationWithId(poi.LocationId);
                    if (loc == null || loc.GetSceneCount() == 0) continue;
                    var key = MakeEstablishKey(s, poi.LocationId);
                    if (_establishedScenes.ContainsKey(key)) continue;
                    _establishedScenes[key] = XmlEstablishedMarker;
                }
            }
        }

        /// <summary>
        /// Re-apply SetSceneName for dynamically-established POIs on load, in case vanilla's
        /// Location serializer doesn't persist runtime-attached _sceneNames. Idempotent and
        /// cheap (no-op when the Location already has the scene).
        /// </summary>
        private void ReapplyDynamicEstablishedScenes()
        {
            foreach (var kvp in _establishedScenes)
            {
                if (kvp.Value == XmlEstablishedMarker) continue;
                var parts = kvp.Key.Split('|');
                if (parts.Length != 2) continue;
                var settlement = Settlement.All.FirstOrDefault(s => s.StringId == parts[0]);
                var loc = settlement?.LocationComplex?.GetLocationWithId(parts[1]);
                if (loc != null && loc.GetSceneCount() == 0)
                    loc.SetSceneName(0, kvp.Value);
            }
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
            foreach (var host in _byLocationId.Values.OfType<ISkillTrainerHost>())
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

            // Establish-state housekeeping. Order matters: re-apply dynamic scenes first
            // (so they count toward auto-seed's "has scene" check), then auto-seed any
            // XML sites that aren't yet recorded.
            ReapplyDynamicEstablishedScenes();
            AutoSeedXmlEstablishedPOIs();

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
