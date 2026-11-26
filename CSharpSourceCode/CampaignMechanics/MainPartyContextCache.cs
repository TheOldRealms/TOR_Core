using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics;

/// <summary>
/// Hourly-refreshed snapshot of what sits near <see cref="MobileParty.MainParty"/> on the
/// campaign map. Callsites that previously called <see cref="TORCommon.FindNearestSettlement"/>
/// inside predicates or per-frame VM updates can read these cached references instead.
/// </summary>
/// <remarks>
/// Not serialised; rebuilt on session launch and at the top of every hourly tick inside
/// <see cref="CustomEvents.CustomEventsCampaignBehavior"/>. For tighter per-callsite
/// radii than the cache's scan window, use the <c>GetNearby*</c> helpers which apply an
/// additional squared-distance check against the cached candidate.
/// </remarks>
public static class MainPartyContextCache
{
    /// <summary>
    /// Scan radius used when refreshing the cache. Chosen to cover the widest radius
    /// any TOR callsite uses (<c>FindNearestSettlement(MainParty, 150f, ...)</c> in the
    /// InkStory variants). Smaller-radius callsites re-check distance via
    /// <see cref="GetNearbyTown"/> / <see cref="GetNearbySettlement"/> /
    /// <see cref="GetNearbyCursedSite"/>.
    /// </summary>
    private const float ScanRadius = 150f;

    public static Settlement NearestSettlement { get; private set; }
    public static Settlement NearestTown { get; private set; }
    public static Settlement NearestCursedSite { get; private set; }

    public static string NearestTownCultureId => NearestTown?.Culture?.StringId;

    /// <summary>
    /// Recomputes all cached references against the current <see cref="MobileParty.MainParty"/>
    /// position. Called at session launch and at the top of each hourly tick.
    /// </summary>
    public static void Refresh()
    {
        var main = MobileParty.MainParty;
        if (main == null)
        {
            NearestSettlement = null;
            NearestTown = null;
            NearestCursedSite = null;
            return;
        }
        NearestSettlement = TORCommon.FindNearestSettlement(main, ScanRadius);
        NearestTown = TORCommon.FindNearestSettlement(main, ScanRadius, x => x.IsTown);
        NearestCursedSite = TORCommon.FindNearestSettlement(main, ScanRadius,
            x => x.SettlementComponent is CursedSiteComponent);
    }

    /// <summary>
    /// Returns the cached nearest town if it is within <paramref name="radius"/> of the
    /// main party right now, otherwise null. Uses squared Euclidean distance to the
    /// town's gate position.
    /// </summary>
    public static Settlement GetNearbyTown(float radius) => WithinRadius(NearestTown, radius);

    /// <summary>
    /// Returns the cached nearest settlement (any kind) if it is within
    /// <paramref name="radius"/> of the main party right now, otherwise null.
    /// </summary>
    public static Settlement GetNearbySettlement(float radius) => WithinRadius(NearestSettlement, radius);

    /// <summary>
    /// Returns the cached nearest cursed site if it is within <paramref name="radius"/>
    /// of the main party right now, otherwise null.
    /// </summary>
    public static Settlement GetNearbyCursedSite(float radius) => WithinRadius(NearestCursedSite, radius);

    /// <summary>
    /// Shared squared-distance gate for the Get-nearby helpers.
    /// </summary>
    private static Settlement WithinRadius(Settlement cached, float radius)
    {
        if (cached == null) return null;
        var main = MobileParty.MainParty;
        if (main == null) return null;
        return main.Position.DistanceSquared(cached.GatePosition) < radius * radius ? cached : null;
    }
}
