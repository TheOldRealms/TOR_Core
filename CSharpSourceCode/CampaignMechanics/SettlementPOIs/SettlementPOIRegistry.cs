using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Static lookup for every registered <see cref="SettlementPOI"/>. Populated once in
    /// <c>SubModule.InitializeGameStarter</c> (before any campaign event fires).
    ///
    /// <para>Consumers: <see cref="SettlementPOIManagerCampaignBehavior"/> iterates
    /// <see cref="All"/> to wire menu options; <see cref="TOR_Core.Models.TORSettlementAccessModel"/>
    /// calls <see cref="Get"/> to dispatch access-rule questions to the owning POI.</para>
    /// </summary>
    public static class SettlementPOIRegistry
    {
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

        /// <summary>
        /// Returns the Location StringId where in-town trainer behaviors should place their
        /// trainer hero in <paramref name="s"/>. If any registered POI
        /// <see cref="SettlementPOI.HostsTrainer"/> and <see cref="SettlementPOI.AppliesTo"/>
        /// this settlement, that POI's <see cref="SettlementPOI.LocationId"/> is returned.
        /// Falls back to <c>house_1</c> — the vanilla pre-POI location trainer behaviors
        /// targeted before the migration.
        /// </summary>
        public static string GetTrainerLocationId(Settlement s)
        {
            foreach (var poi in _byLocationId.Values)
            {
                if (poi.HostsTrainer && poi.AppliesTo(s))
                    return poi.LocationId;
            }
            return "house_1";
        }
    }
}
