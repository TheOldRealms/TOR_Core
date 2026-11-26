using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics;

/// <summary>
/// Stateless tier classification for campaign entities based on Euclidean distance to
/// <see cref="MobileParty.MainParty"/>. Used by Harmony prefixes to coarsen or skip
/// vanilla per-entity ticks for distant parties/settlements, and by future TOR code
/// that wants to self-gate.
/// </summary>
/// <remarks>
/// Thresholds derived from campaign-map landmarks:
/// <list type="bullet">
///   <item>Near (≤ 185): Altdorf → Middenheim distance. Player can plausibly see
///   or interact with entities within this range.</item>
///   <item>Mid (≤ 300): roughly "a neighbouring kingdom away". Updates still matter
///   for background AI consistency.</item>
///   <item>Far (> 300): distant from the player. Safe to coarsen or skip
///   updates whose output only matters to AI, not visibility.</item>
/// </list>
/// No cache: a single <c>DistanceSquared</c> compute per call is already in the
/// low-nanoseconds range, and a dictionary lookup would be equal or slower.
/// </remarks>
public static class AiThinkLod
{
    public const float NearRadius = 185f;
    public const float MidRadius = 300f;

    private const float NearRadiusSq = NearRadius * NearRadius;
    private const float MidRadiusSq = MidRadius * MidRadius;

    public enum Tier
    {
        Near,
        Mid,
        Far,
    }

    public static Tier GetTier(MobileParty party)
    {
        if (party == null) return Tier.Far;
        var main = MobileParty.MainParty;
        if (main == null) return Tier.Far;
        return FromDistanceSq(main.Position.DistanceSquared(party.Position));
    }

    public static Tier GetTier(Settlement settlement)
    {
        if (settlement == null) return Tier.Far;
        var main = MobileParty.MainParty;
        if (main == null) return Tier.Far;
        return FromDistanceSq(main.Position.DistanceSquared(settlement.GatePosition));
    }

    private static Tier FromDistanceSq(float distSq)
    {
        if (distSq < NearRadiusSq) return Tier.Near;
        if (distSq < MidRadiusSq) return Tier.Mid;
        return Tier.Far;
    }
}
