using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Extensions;



namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Pure "town POI" descriptor — holds the identity, menu placement config, and access
    /// rule for one TOR point of interest (Wizard Hall, Witch Hunter Lodge, future
    /// Runehall / Engineer Hall / Chapel / Grove / Herdstone / Crypt / …).
    ///
    /// <para>Not a <c>CampaignBehaviorBase</c>. Shared lifecycle work plus the
    /// <c>LocationId → SettlementPOI</c> lookup live on
    /// <see cref="SettlementPOIManagerCampaignBehavior"/>, which discovers subclasses by
    /// reflection on session launch — adding a new POI is just a new subclass file.</para>
    ///
    /// <para>Denial reasons use convention-based lookup via <see cref="R"/> against
    /// <c>tor_poi_&lt;shortName&gt;.&lt;field&gt;</c> in <c>tor_strings.xml</c>.</para>
    /// </summary>
    public abstract class SettlementPOI
    {
        // ------- Identity (abstract)

        /// <summary>Location StringId this POI owns (e.g. <c>tor_wizardhall</c>). Must start with <c>tor_</c>.</summary>
        public abstract string LocationId { get; }

        /// <summary>Game menu the POI's "visit" option is registered on (e.g. <c>town_artisan</c>).</summary>
        public abstract string ParentMenuId { get; }

        /// <summary>Menu-option id in <see cref="ParentMenuId"/> that the POI pins itself against.</summary>
        public abstract string AnchorEntryId { get; }

        /// <summary>True = pin above the anchor; false = pin below (right-after).</summary>
        public virtual bool AnchorAbove => true;

        /// <summary>
        /// Where this POI's "visit" option appears. Default is one attachment matching
        /// the legacy <see cref="ParentMenuId"/> / <see cref="AnchorEntryId"/> /
        /// <see cref="AnchorAbove"/> properties — so existing town POIs don't change.
        ///
        /// Override to attach the same POI to multiple menus (e.g. both the town
        /// artisan submenu AND the castle courtyard submenu). The manager renders
        /// one menu option per attachment; <see cref="AppliesTo"/> + <see cref="CheckAccess"/>
        /// still gate per-settlement, so entries only show where the POI actually exists.
        /// </summary>
        public virtual IEnumerable<ParentMenuAttachment> MenuAttachments =>
            new[] { new ParentMenuAttachment(ParentMenuId, AnchorEntryId, AnchorAbove) };

        /// <summary>Localization id for the "Visit …" menu label.</summary>
        public abstract string EnterLabelId { get; }

        /// <summary>English fallback for the menu label when <see cref="EnterLabelId"/> is not registered.</summary>
        public abstract string EnterLabelDefault { get; }

        /// <summary>
        /// Scene-launch mode for this POI.
        /// <para><b>False (default)</b> — vanilla <c>PlayerEncounter.LocationEncounter.CreateAndOpenMissionController</c>
        /// path. Works on towns, castles, villages, hideouts: anywhere vanilla populates a
        /// <c>LocationEncounter</c> on entry. Supports LocationCharacters (NPCs attached to
        /// the location in XML / spawned by behaviors).</para>
        /// <para><b>True</b> — direct <c>MissionState.OpenNew</c> path. Used for TOR custom
        /// settlements (shrines, Oak of Ages, chaos portals, …) whose component types the
        /// engine's encounter machinery doesn't recognize. The scene loads, but LocationCharacters
        /// aren't auto-spawned — any NPC placement must happen via a MissionBehavior.</para>
        /// </summary>
        public virtual bool UsesMissionLaunch => false;

        // ------- Access rule (the main hook)

        /// <summary>
        /// Return <c>true</c> if the hero may enter this POI from <paramref name="s"/>, else
        /// <c>false</c> with <paramref name="denialReason"/> set via <see cref="R"/>.
        /// </summary>
        public abstract bool CheckAccess(Settlement s, Hero h, out TextObject denialReason);

        /// <summary>
        /// Discovery hook — placeholder for the future per-settlement discovery feature.
        /// Returns <c>true</c> today; override on a per-POI basis to consult a tracker.
        /// </summary>
        protected virtual bool IsDiscovered(Settlement settlement) => true;

        /// <summary>
        /// Text rendered on the settlement nameplate when this POI is discovered. For
        /// sprite icons, return the Gauntlet inline-image syntax
        /// <c>&lt;img src="sprite_name"&gt;</c> — the nameplate's TextWidget parses the
        /// tag and renders the sprite inline (same pattern as
        /// <see cref="CustomResource.GetCustomResourceIconAsText"/>). For POIs without
        /// an art asset yet, a short letter (e.g. "H") also works. Default empty = no
        /// visible icon when discovered.
        ///
        /// <para>POI-level static form — same icon for every settlement that has this
        /// POI. Override <see cref="MapIconTextFor"/> instead if you need per-settlement
        /// variation (e.g. "lodge has a bounty offer available right now").</para>
        /// </summary>
        public virtual string MapIconText => "";

        /// <summary>
        /// Per-settlement variant of <see cref="MapIconText"/>. Default forwards to the
        /// static form so existing POIs don't change. Override when the icon reflects
        /// per-settlement runtime state. The nameplate VM calls this when rebuilding
        /// its slot text; refreshes fire via
        /// <see cref="SettlementPOIManagerCampaignBehavior.IconsChanged"/>.
        /// </summary>
        public virtual string MapIconTextFor(Settlement s) => MapIconText;

        /// <summary>
        /// Does this POI exist in <paramref name="s"/>? Scene presence is the sole gate —
        /// a POI "exists" at a settlement when the Location row has at least one scene
        /// attached (via XML on load, or <see cref="SettlementPOIManagerCampaignBehavior.EstablishAt"/>
        /// at runtime). The town_complex template provides the Location row in every town's
        /// LocationComplex; only scenes are per-settlement.
        /// </summary>
        public bool AppliesTo(Settlement s)
        {
            var loc = s?.LocationComplex?.GetLocationWithId(LocationId);
            return loc != null && loc.GetSceneCount() > 0;
        }

        // ------- Convention-based reason lookup

        /// <summary>POI short name derived from <see cref="LocationId"/> (strips the <c>tor_</c> prefix).</summary>
        protected string PoiShortName =>
            LocationId != null && LocationId.StartsWith("tor_") ? LocationId.Substring(4) : LocationId;

        /// <summary>
        /// Per-instance cache so repeated <see cref="R"/> calls for the same field hand back
        /// the identical <see cref="TextObject"/>. The menu-condition translator in
        /// <see cref="SettlementPOIManagerCampaignBehavior"/> relies on reference equality to decide
        /// hide vs. gray.
        /// </summary>
        private readonly Dictionary<string, TextObject> _reasonCache = new();

        /// <summary>
        /// Localized denial-reason lookup. Resolves <c>tor_poi_&lt;shortName&gt;.&lt;field&gt;</c>
        /// against <c>tor_strings.xml</c>; falls back to <paramref name="fallback"/> if the
        /// string isn't registered. Cached per instance so reference equality works.
        /// </summary>
        protected TextObject R(string field, string fallback)
        {
            if (!_reasonCache.TryGetValue(field, out var cached))
            {
                cached = TORTextHelper.GetTextObject($"tor_poi_{PoiShortName}.{field}", fallback);
                _reasonCache[field] = cached;
            }
            return cached;
        }

        // ------- Hide-eligible sentinels (virtual — override only if the POI needs different text)

        public virtual TextObject HideOnUndiscovered =>
            R("Undiscovered", "You have not yet found this place.");

        public virtual TextObject HideOnSealed =>
            R("Sealed", "This place is sealed.");

        /// <summary>
        /// Register this POI's trainer dialog tree (if any) against the campaign.
        /// Called once per session by <c>SettlementPOIManagerCampaignBehavior</c>.
        /// Default is no-op: POIs without a conversation partner (ROR recruiter,
        /// future scene-only POIs) don't need to override.
        /// </summary>
        public virtual void RegisterDialogs(CampaignGameStarter starter) { }

        /// <summary>
        /// Spawn + place any resident NPC(s) this POI hosts at <paramref name="s"/>.
        /// Called on both session launch and before each mission open so the hero is
        /// always present where expected. Default is no-op — POIs with no resident
        /// (scene-only POIs, recruiter-style POIs) opt out by not overriding.
        /// Hosts of a single NPC override by calling <see cref="PlaceResident"/>;
        /// hosts of multiple NPCs (e.g. a wizard hall with distinct trainer and
        /// enchanter heroes) call it per template.
        /// </summary>
        public virtual void EnsurePlaced(Settlement s) { }

        /// <summary>
        /// Shared resident-placement primitive: get-or-create a hero of
        /// <paramref name="templateId"/> at <paramref name="s"/> via the POI
        /// manager (which handles save-state and orphan adoption), then plant the
        /// LocationCharacter into this POI's location. Idempotent — repeated calls
        /// across session-launch and mission-open are safe.
        /// </summary>
        protected void PlaceResident(Settlement s, string templateId)
        {
            if (string.IsNullOrEmpty(templateId) || !AppliesTo(s)) return;
            var mgr = TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            var hero = mgr?.GetOrCreateHero(s, LocationId, templateId);
            if (hero == null || !s.HeroesWithoutParty.Contains(hero)) return;
            var location = s.LocationComplex.GetLocationWithId(LocationId);
            TrainerPlacement.EnsureAt(s, hero, location);
        }
    }

    /// <summary>
    /// A single parent-menu attachment for a <see cref="SettlementPOI"/>: which menu
    /// the "visit" option is registered on, which existing menu entry it anchors
    /// against, and whether it goes above or below that anchor.
    /// </summary>
    public readonly record struct ParentMenuAttachment(string MenuId, string AnchorEntryId, bool AnchorAbove);
}
