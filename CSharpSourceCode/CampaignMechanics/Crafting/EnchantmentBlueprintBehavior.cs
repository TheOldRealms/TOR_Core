using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// The campaign-wide store of enchantment blueprints the player has learned — the only
    /// place blueprint knowledge lives. Read and written through
    /// <see cref="EnchantmentBlueprints"/> rather than directly.
    ///
    /// There is no migration path off the retired per-hero lists: a save written before
    /// blueprints were centralised starts empty here, and in fact will not load cleanly at
    /// all now that <c>HeroExtendedInfo</c>'s slot 10 is gone. A new campaign is required.
    ///
    /// Persisted as a <see cref="List{T}"/> rather than a <see cref="HashSet{T}"/> because
    /// nothing in this codebase syncs a HashSet through <see cref="IDataStore"/> and the
    /// save system's container support for one is unverified. <see cref="_index"/> is the
    /// runtime lookup, rebuilt from the list on load.
    /// </summary>
    public class EnchantmentBlueprintBehavior : CampaignBehaviorBase
    {
        private List<string> _knownBlueprints = [];
        private readonly HashSet<string> _index = [];

        public static EnchantmentBlueprintBehavior Instance =>
            Campaign.Current?.GetCampaignBehavior<EnchantmentBlueprintBehavior>();

        public IReadOnlyCollection<string> Known => _index;

        /// <summary>
        /// Set-backed membership test. Exists so callers do not reach for LINQ's
        /// <c>Contains</c> on <see cref="Known"/>, which would degrade the hash lookup to a
        /// linear scan - this is called per trait inside UI population loops.
        /// </summary>
        internal bool Contains(string blueprintId) => _index.Contains(blueprintId);

        /// <summary>
        /// Nothing to register: the store is written directly by
        /// <see cref="EnchantmentBlueprints.Learn"/> rather than by listening for the
        /// learned event, which it raises itself afterwards.
        /// </summary>
        public override void RegisterEvents() { }

        /// <summary>
        /// Adds <paramref name="blueprintId"/> to the store. Returns false if it was already
        /// present, so <see cref="EnchantmentBlueprints.Learn"/> can tell a real grant from a
        /// repeat and avoid raising the learned event twice.
        /// </summary>
        internal bool Record(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return false;
            if (!_index.Add(blueprintId)) return false;
            _knownBlueprints.Add(blueprintId);
            return true;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_knownBlueprints", ref _knownBlueprints);
            _knownBlueprints ??= [];
            RebuildIndex();
        }

        private void RebuildIndex()
        {
            _index.Clear();
            foreach (var blueprintId in _knownBlueprints)
            {
                if (!string.IsNullOrEmpty(blueprintId)) _index.Add(blueprintId);
            }
        }
    }
}
