using System.Collections.Generic;

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
    }
}
