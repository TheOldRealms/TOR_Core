using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.BattleMechanics.DualWield
{
    /// <summary>
    /// Berserk-skill-scaled bonus curves for dual-wielding. Replaces
    /// Yotthani's flat <c>DualWieldSettings.AttackSpeedBonus</c> /
    /// <c>MovementSpeedBonus</c> / <c>OffHandDamageMultiplier</c> for the main
    /// hero; NPCs continue to read the Yotthani flats scaled by
    /// <c>NpcBonusShare</c>. Modelled after vanilla One-Handed's
    /// "Personal: Weapon Speed" level-scaling: more Berserk = more paired-
    /// weapons advantage, without touching the agent's One-Handed proficiency.
    /// </summary>
    internal static class BerserkBonusModel
    {
        /// <summary>
        /// Skill value at which the main hero matches Yotthani's flat default
        /// (1.0 multiplier). Below this they scale down, above they scale up
        /// to <see cref="MaxMultiplier"/>.
        /// </summary>
        private const float ReferenceSkill = 150f;
        private const float MinMultiplier = 0.5f;
        private const float MaxMultiplier = 1.5f;

        /// <summary>
        /// Skill value at which the offhand-damage multiplier reaches 1.0
        /// (fully parity with main-hand). Capped at 1.0 above this.
        /// </summary>
        private const float OffHandParitySkill = 200f;
        private const float OffHandPenaltyAtZero = 0.85f;

        /// <summary>
        /// Swing-speed multiplier bonus to add on top of Yotthani's flat base.
        /// Returns the bonus FRACTION (0.15 = +15% swing speed); caller does
        /// <c>SwingSpeedMultiplier *= 1 + bonus</c>.
        /// </summary>
        public static float SwingSpeedBonus(Agent agent)
        {
            float flat = DualWieldSettings.Get().AttackSpeedBonus;
            return flat * BerserkScale(agent);
        }

        /// <summary>
        /// Movement-speed multiplier bonus (same contract as <see cref="SwingSpeedBonus"/>).
        /// </summary>
        public static float MovementSpeedBonus(Agent agent)
        {
            float flat = DualWieldSettings.Get().MovementSpeedBonus;
            return flat * BerserkScale(agent);
        }

        /// <summary>
        /// Offhand-damage multiplier to apply to blow magnitude. Starts at
        /// <see cref="OffHandPenaltyAtZero"/> for a Berserk-0 hero and climbs
        /// linearly to 1.0 at <see cref="OffHandParitySkill"/>, capped at 1.0
        /// so offhand never exceeds mainhand without an explicit perk. NPCs
        /// read Yotthani's flat default scaled by <c>NpcBonusShare</c>.
        /// </summary>
        public static float OffHandDamageMultiplier(Agent agent)
        {
            if (!IsMainHeroAgent(agent)) return DualWieldSettings.Get().OffHandDamageMultiplier;

            float skill = Hero.MainHero.GetSkillValue(TORSkills.Berserk);
            float progress = MathF.Clamp(skill / OffHandParitySkill, 0f, 1f);
            return OffHandPenaltyAtZero + (1f - OffHandPenaltyAtZero) * progress;
        }

        /// <summary>
        /// Multiplier applied to Yotthani's flat bonuses for the main hero.
        /// At Berserk 0: <see cref="MinMultiplier"/>. At <see cref="ReferenceSkill"/>:
        /// 1.0 (Yotthani's default). Climbing to <see cref="MaxMultiplier"/> at
        /// double the reference. For NPCs returns <c>NpcBonusShare</c>.
        /// </summary>
        private static float BerserkScale(Agent agent)
        {
            if (!IsMainHeroAgent(agent)) return DualWieldSettings.Get().NpcBonusShare;

            float skill = Hero.MainHero.GetSkillValue(TORSkills.Berserk);
            float raw = MinMultiplier + (MaxMultiplier - MinMultiplier) * (skill / (2f * ReferenceSkill));
            return MathF.Clamp(raw, MinMultiplier, MaxMultiplier);
        }

        private static bool IsMainHeroAgent(Agent agent)
            => agent != null && agent.IsMainAgent && Hero.MainHero != null && TORSkills.Berserk != null;
    }
}
