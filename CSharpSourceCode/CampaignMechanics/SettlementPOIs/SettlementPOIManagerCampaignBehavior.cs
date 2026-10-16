using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.Missions;
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

        // Runtime-attached scenes (via EstablishAt). Vanilla's Location serializer does
        // NOT persist _sceneNames mutated at runtime, so we remember them here and re-apply
        // SetSceneName on OnSessionLaunched after load. XML-defined scenes are re-applied
        // by vanilla's XML loader on each session and don't need to be in this dict.
        // Key: "settlementId|locationId" → sceneName.
        private Dictionary<string, string> _dynamicScenes = new();

        // Per-settlement discovered POI set — "has the player been here and seen this POI?"
        // Undiscovered POIs show as "?" on the map nameplate; discovered ones show their
        // specific MapIconText. Discovery fires on OnSettlementEnteredEvent (reveal all
        // applicable POIs at once per the "visit the town → learn what's in it" model).
        // Key: settlementId → list of discovered POI LocationIds.
        private Dictionary<string, List<string>> _discovered = new();

        // ------- Lifecycle

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_poiResidents", ref _residents);
            dataStore.SyncData("_poiDynamicScenes", ref _dynamicScenes);
            dataStore.SyncData("_poiDiscovered", ref _discovered);
        }

        /// <summary>
        /// Fires whenever discovery state changes for any (settlement, POI) pair. Nameplate
        /// VMs subscribe to this so they can refresh their displayed slot state — VMs are
        /// cached per settlement by the Harmony patch, so they never re-construct and would
        /// otherwise stay stale with the discovery=false values they were born with.
        /// </summary>
        public static event Action DiscoveryChanged;

        public bool IsDiscovered(Settlement s, SettlementPOI poi)
        {
            if (s == null || poi == null) return false;
            return _discovered.TryGetValue(s.StringId, out var ids) && ids.Contains(poi.LocationId);
        }

        public void MarkDiscovered(Settlement s, SettlementPOI poi)
        {
            if (s == null || poi == null) return;
            if (!_discovered.TryGetValue(s.StringId, out var ids))
                _discovered[s.StringId] = ids = new List<string>();
            if (ids.Contains(poi.LocationId)) return;
            ids.Add(poi.LocationId);
            DiscoveryChanged?.Invoke();
        }

        public void UnmarkDiscovered(Settlement s, SettlementPOI poi)
        {
            if (s == null || poi == null) return;
            if (!_discovered.TryGetValue(s.StringId, out var ids)) return;
            if (!ids.Remove(poi.LocationId)) return;
            DiscoveryChanged?.Invoke();
        }

        /// <summary>
        /// Reveal every applicable POI at the settlement. Called on OnSettlementEnteredEvent
        /// so visiting a town unlocks its full icon set in one step. Idempotent — safe to
        /// call repeatedly.
        /// </summary>
        public void MarkAllApplicableDiscovered(Settlement s)
        {
            if (s == null) return;
            bool changed = false;
            foreach (var poi in _byLocationId.Values)
            {
                if (!poi.AppliesTo(s)) continue;
                if (!_discovered.TryGetValue(s.StringId, out var ids))
                    _discovered[s.StringId] = ids = new List<string>();
                if (ids.Contains(poi.LocationId)) continue;
                ids.Add(poi.LocationId);
                changed = true;
            }
            if (changed) DiscoveryChanged?.Invoke();
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            // Only the player's own visits reveal POIs — AI parties entering doesn't count.
            if (party != MobileParty.MainParty) return;
            MarkAllApplicableDiscovered(settlement);
        }

        /// <summary>
        /// Attach a scene to the template-provided Location row at this settlement and
        /// immediately place any trainers that newly apply. The town_complex template
        /// provides the Location row in every town's complex, so this is a no-reflection
        /// operation. Idempotent: no-op if the Location already has a scene.
        /// Returns true if the POI applies at the settlement after the call.
        /// </summary>
        public bool EstablishAt(Settlement s, SettlementPOI poi, string sceneName)
        {
            if (s == null || poi == null || string.IsNullOrEmpty(sceneName)) return false;
            var loc = s.LocationComplex?.GetLocationWithId(poi.LocationId);
            if (loc == null) return false;   // template doesn't define this Location in the settlement's complex
            if (loc.GetSceneCount() == 0) loc.SetSceneName(0, sceneName);
            _dynamicScenes[$"{s.StringId}|{poi.LocationId}"] = sceneName;
            // Place any trainers that newly apply at this settlement.
            foreach (var h in _byLocationId.Values.OfType<ISpellTrainerHost>())  if (ReferenceEquals(h, poi)) h.EnsurePlaced(s);
            foreach (var h in _byLocationId.Values.OfType<IEnchanterHost>())      if (ReferenceEquals(h, poi)) h.EnsurePlaced(s);
            foreach (var h in _byLocationId.Values.OfType<ISkillTrainerHost>())  if (ReferenceEquals(h, poi)) h.EnsurePlaced(s);
            return poi.AppliesTo(s);
        }

        /// <summary>
        /// Re-apply SetSceneName for POIs established at runtime, since vanilla's Location
        /// serializer doesn't carry mutated _sceneNames across save/load. Idempotent — no-op
        /// when the Location already has a scene (e.g. from XML).
        /// </summary>
        private void ReapplyDynamicScenes()
        {
            foreach (var kvp in _dynamicScenes)
            {
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

            // Re-apply any runtime-attached scenes before the trainer sweep, so AppliesTo
            // reflects post-load reality for POIs established in a previous session.
            ReapplyDynamicScenes();

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

            var settlement = Settlement.CurrentSettlement;

            // Dual-path: TOR custom settlement components (ShrineComponent, OakOfAgesComponent,
            // raiding sites, …) never get a vanilla LocationEncounter because the engine's
            // encounter machinery only recognises Town/Castle/Village/Hideout. For those,
            // launch the scene directly via MissionState.OpenNew (same path TOR uses for
            // arena/brawl/duel). Town-type POIs keep the LocationEncounter route.
            if (poi.UsesMissionLaunch)
            {
                OpenPoiViaMission(poi, settlement);
                return;
            }

            if (PlayerEncounter.LocationEncounter == null || LocationComplex.Current == null)
            {
                // Town-mode POI without a live LocationEncounter — bail cleanly rather than
                // NRE. Shouldn't happen on a vanilla town but defensively keep the guard.
                return;
            }

            // vanilla lordshall-style transition — triggers LocationCharactersAreReadyToSpawnEvent
            // and the normal exit-back-to-town flow.
            var manager = Campaign.Current.GameMenuManager;
            manager.NextLocation = LocationComplex.Current.GetLocationWithId(poi.LocationId);
            manager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(manager.NextLocation);
            manager.NextLocation = null;
            manager.PreviousLocation = null;
        }

        /// <summary>
        /// Resolve the POI's scene + trainer hero and hand off to the mission launcher.
        /// Scene plumbing lives in <see cref="TorMissionManager.OpenPoiVisitMission"/>
        /// so this stays an orchestration layer.
        /// </summary>
        private static void OpenPoiViaMission(SettlementPOI poi, Settlement settlement)
        {
            var location = settlement?.LocationComplex?.GetLocationWithId(poi.LocationId);
            if (location == null) return;

            string sceneName = location.GetSceneName(1);
            if (string.IsNullOrEmpty(sceneName)) sceneName = location.GetSceneName(0);
            if (string.IsNullOrEmpty(sceneName)) return;

            // If the POI is a trainer host, resolve the hero so the mission can spawn
            // them as a visible civilian agent.
            Hero trainer = (poi is ISpellTrainerHost host) ? host.GetOrCreateTrainer(settlement) : null;

            TorMissionManager.OpenPoiVisitMission(sceneName, poi.ParentMenuId, trainer);
        }
    }
}
