using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Ambush;

namespace TOR_Core.Models
{
    public class TORAmbushModel : GameModel
    {
        // Chance clamps — virtual so subclasses can rebalance via override.
        public virtual float BaseAmbushChance => 0.5f;
        public virtual float MinChance => 0.1f;
        public virtual float MaxChance => 0.95f;

        // Factor weights — virtual for the same reason.
        public virtual float ScoutingPerPointBonus => 0.002f;   // ±100 skill delta = ±0.2
        public virtual float ScoutingBonusCap => 0.2f;
        public virtual float StealthDurationPerHour => 0.05f;   // +0.05 per in-game hour
        public virtual float StealthDurationCap => 0.15f;       // capped at +0.15

        public virtual float ConcealingTerrainBonus => 0.15f;   // forest / mountain / canyon / swamp
        public virtual float OpenTerrainPenalty => -0.10f;      // plain / desert / steppe / dune

        public virtual float SmallAttackerBonus => 0.2f;        // attacker roster < 0.5× target
        public virtual float SlightlySmallerAttackerBonus => 0.1f;  // 0.5× ≤ ratio < 1.0×
        public virtual float OverwhelmingAttackerPenalty => -0.1f;  // attacker ≥ 2× target

        // Target proximity: at zero distance = full bonus, scaling to 0 at the stealth radius edge.
        public virtual float MaxProximityBonus => 0.1f;
        public virtual float ProximityRadius => AmbushCampaignBehavior.StealthRadius;

        // Day/night: night favours ambushers.
        public virtual float NightTimeBonus => 0.1f;

        // ---- Detection model (Phase B v2) ----
        //
        // Fires on QuarterHourlyTickEvent per nearby hostile observer while the
        // player is in stealth. Multiplicative composition (vs the additive
        // CalculateAmbushSuccessChance above) because this is a per-tick "escape
        // notice" roll called many times, not a one-shot strike outcome.
        public virtual float BaseDetectionChancePerTick => 0.05f;
        public virtual float MinDetectionChance => 0f;
        public virtual float MaxDetectionChance => 0.80f;
        public virtual float DetectionScanRadius => 10f;

        // Distance multiplier: closer = higher. Lerped from AdjacentDistanceMultiplier
        // at distance 0 to FarDistanceMultiplier at the scan-radius edge.
        public virtual float AdjacentDistanceMultiplier => 2.5f;
        public virtual float FarDistanceMultiplier => 0.4f;

        public virtual float DetectionScoutingPerPointBonus => 0.004f;   // observer scouting bias
        public virtual float DetectionScoutingBonusCap => 0.5f;

        public virtual float ConcealingTerrainMultiplier => 0.5f;        // forest / mountain / canyon / swamp
        public virtual float OpenTerrainMultiplier => 1.5f;              // plain / desert / steppe / dune

        public virtual float NightDetectionMultiplier => 0.5f;

        public virtual float StealthDurationDetectionPerHour => 0.1f;    // multiplicative +10% per hour
        public virtual float StealthDurationDetectionCap => 0.5f;        // capped at +50%

        // Filter: which parties count as "observers" that can even attempt a detection
        // roll against a stealthed target. Default is hostile parties only — a friendly
        // caravan or allied lord riding past shouldn't blow your cover. Overrides can
        // widen this (e.g., bandit parties always count) or narrow it (e.g., only parties
        // with a sufficiently high Scouting leader).
        public virtual bool CanDetect(MobileParty observer, MobileParty target)
        {
            if (observer == null || target == null || observer == target) return false;
            if (observer.MapFaction == null || target.MapFaction == null) return false;
            return FactionManager.IsAtWarAgainstFaction(observer.MapFaction, target.MapFaction);
        }

        public virtual bool TryDetect(MobileParty observer, MobileParty target,
                                       CampaignTime stealthEnteredAt, out string reason)
        {
            reason = null;
            if (!CanDetect(observer, target)) return false;

            float distance = observer.Position.ToVec2().Distance(target.Position.ToVec2());
            if (distance > DetectionScanRadius) return false;

            float chance = CalculateDetectionChancePerTick(observer, target, stealthEnteredAt);
            if (MBRandom.RandomFloat >= chance) return false;

            reason = BuildDetectionReason(observer, target, chance);
            return true;
        }

        public virtual float CalculateDetectionChancePerTick(MobileParty observer, MobileParty target, CampaignTime stealthEnteredAt)
        {
            float chance = BaseDetectionChancePerTick
                * DistanceDetectionMultiplier(observer, target)
                * ScoutingDetectionMultiplier(observer)
                * TerrainDetectionMultiplier(target)
                * NightDetectionMultiplierValue()
                * StealthDurationDetectionMultiplier(stealthEnteredAt);
            return MBMath.ClampFloat(chance, MinDetectionChance, MaxDetectionChance);
        }

        protected virtual float DistanceDetectionMultiplier(MobileParty observer, MobileParty target)
        {
            float d = observer.Position.ToVec2().Distance(target.Position.ToVec2());
            if (d <= 0.1f) return AdjacentDistanceMultiplier;
            if (d >= DetectionScanRadius) return FarDistanceMultiplier;
            return MBMath.Lerp(AdjacentDistanceMultiplier, FarDistanceMultiplier, d / DetectionScanRadius);
        }

        protected virtual float ScoutingDetectionMultiplier(MobileParty observer)
        {
            int s = observer.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return 1f + MBMath.ClampFloat(s * DetectionScoutingPerPointBonus, 0f, DetectionScoutingBonusCap);
        }

        protected virtual float TerrainDetectionMultiplier(MobileParty target)
        {
            var face = target.CurrentNavigationFace;
            if (!face.IsValid()) return 1f;
            var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
            switch (terrain)
            {
                case TerrainType.Forest:
                case TerrainType.Mountain:
                case TerrainType.Canyon:
                case TerrainType.Swamp:
                    return ConcealingTerrainMultiplier;
                case TerrainType.Plain:
                case TerrainType.Desert:
                case TerrainType.Steppe:
                case TerrainType.Dune:
                    return OpenTerrainMultiplier;
                default:
                    return 1f;
            }
        }

        protected virtual float NightDetectionMultiplierValue()
        {
            if (Campaign.Current == null) return 1f;
            return Campaign.Current.IsNight ? NightDetectionMultiplier : 1f;
        }

        protected virtual float StealthDurationDetectionMultiplier(CampaignTime enteredAt)
        {
            if (enteredAt == CampaignTime.Zero) return 1f;
            float hours = (float)(CampaignTime.Now - enteredAt).ToHours;
            return 1f + MBMath.ClampFloat(hours * StealthDurationDetectionPerHour, 0f, StealthDurationDetectionCap);
        }

        protected virtual string BuildDetectionReason(MobileParty observer, MobileParty target, float chance)
        {
            string who = observer.LeaderHero?.Name?.ToString() ?? observer.Name?.ToString() ?? "a hostile party";
            return "You've been spotted by " + who + ".";
        }

        public virtual float CalculateAmbushSuccessChance(MobileParty attacker, MobileParty target, CampaignTime stealthEnteredAt)
        {
            if (attacker == null || target == null) return MinChance;

            float chance = BaseAmbushChance;
            chance += ScoutingDeltaBonus(attacker, target);
            chance += TerrainBonus(attacker);
            chance += ForceRatioBonus(attacker, target);
            chance += StealthDurationBonus(stealthEnteredAt);
            chance += TargetProximityBonus(attacker, target);
            chance += NightTimeBonusValue();
            return MBMath.ClampFloat(chance, MinChance, MaxChance);
        }

        protected virtual float TargetProximityBonus(MobileParty attacker, MobileParty target)
        {
            if (ProximityRadius <= 0f) return 0f;
            float distance = attacker.Position.ToVec2().Distance(target.Position.ToVec2());
            if (distance >= ProximityRadius) return 0f;
            // Linear fade from full bonus at distance 0 to zero at ProximityRadius.
            float t = 1f - (distance / ProximityRadius);
            return MaxProximityBonus * t;
        }

        protected virtual float NightTimeBonusValue()
        {
            if (Campaign.Current == null) return 0f;
            return Campaign.Current.IsNight ? NightTimeBonus : 0f;
        }

        protected virtual float ScoutingDeltaBonus(MobileParty attacker, MobileParty target)
        {
            int a = attacker.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int t = target.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return MBMath.ClampFloat((a - t) * ScoutingPerPointBonus, -ScoutingBonusCap, ScoutingBonusCap);
        }

        protected virtual float TerrainBonus(MobileParty attacker)
        {
            var face = attacker.CurrentNavigationFace;
            if (!face.IsValid()) return 0f;

            var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
            switch (terrain)
            {
                case TerrainType.Forest:
                case TerrainType.Mountain:
                case TerrainType.Canyon:
                case TerrainType.Swamp:
                    return ConcealingTerrainBonus;
                case TerrainType.Plain:
                case TerrainType.Desert:
                case TerrainType.Steppe:
                case TerrainType.Dune:
                    return OpenTerrainPenalty;
                default:
                    return 0f;
            }
        }

        protected virtual float ForceRatioBonus(MobileParty attacker, MobileParty target)
        {
            int a = attacker.MemberRoster.TotalManCount;
            int t = target.MemberRoster.TotalManCount;
            if (t == 0) return 0f;

            float ratio = (float)a / t;
            if (ratio < 0.5f) return SmallAttackerBonus;
            if (ratio < 1.0f) return SlightlySmallerAttackerBonus;
            if (ratio < 2.0f) return 0f;
            return OverwhelmingAttackerPenalty;
        }

        protected virtual float StealthDurationBonus(CampaignTime enteredAt)
        {
            if (enteredAt == CampaignTime.Zero) return 0f;
            float hours = (float)(CampaignTime.Now - enteredAt).ToHours;
            return MBMath.ClampFloat(hours * StealthDurationPerHour, 0f, StealthDurationCap);
        }
    }
}
