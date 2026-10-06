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
    /// <para>Not a <c>CampaignBehaviorBase</c>: all shared lifecycle work — event wiring,
    /// menu-option registration, menu rearrangement — lives on
    /// <see cref="SettlementPOIManagerCampaignBehavior"/>, which iterates
    /// <see cref="SettlementPOIRegistry"/>. One manager handles any number of POIs, so adding a
    /// new POI is just a new subclass plus one line in SubModule to register it.</para>
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

        /// <summary>Localization id for the "Visit …" menu label.</summary>
        public abstract string EnterLabelId { get; }

        /// <summary>English fallback for the menu label when <see cref="EnterLabelId"/> is not registered.</summary>
        public abstract string EnterLabelDefault { get; }

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
    }
}
