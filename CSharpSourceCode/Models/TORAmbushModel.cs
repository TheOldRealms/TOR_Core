using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

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

        public virtual float CalculateAmbushSuccessChance(MobileParty attacker, MobileParty target, CampaignTime stealthEnteredAt)
        {
            if (attacker == null || target == null) return MinChance;

            float chance = BaseAmbushChance;
            chance += ScoutingDeltaBonus(attacker, target);
            chance += TerrainBonus(attacker);
            chance += ForceRatioBonus(attacker, target);
            chance += StealthDurationBonus(stealthEnteredAt);
            return MBMath.ClampFloat(chance, MinChance, MaxChance);
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
