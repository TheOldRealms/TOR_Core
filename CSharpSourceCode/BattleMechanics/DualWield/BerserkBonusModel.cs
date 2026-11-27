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
        /// Swing-speed bonus per Berserk skill point for the main hero. 0.0005
        /// = +15% at the vanilla skill cap (300), matching Yotthani's flat
        /// default. Must stay in sync with <c>TORSkillEffects.BerserkSwingSpeed</c>
        /// so the "Personal: X" tooltip reads the same number the runtime applies.
        /// </summary>
        public const float SwingSpeedPerLevel = 0.0005f;

        /// <summary>
        /// Movement-speed bonus per Berserk skill point. 0.0003 = +9% at 300,
        /// close to Yotthani's 10% flat default. Must stay in sync with
        /// <c>TORSkillEffects.BerserkMovementSpeed</c>.
        /// </summary>
        public const float MovementSpeedPerLevel = 0.0003f;

        /// <summary>
        /// Skill value at which the offhand-damage multiplier reaches 1.0
        /// (parity with main-hand). Capped at 1.0 above.
        /// </summary>
        private const float OffHandParitySkill = 200f;
        private const float OffHandPenaltyAtZero = 0.85f;

        /// <summary>
        /// Swing-speed multiplier bonus. Returns the bonus FRACTION (0.15 =
        /// +15% swing speed); caller does <c>SwingSpeedMultiplier *= 1 + bonus</c>.
        /// Main hero scales linearly with Berserk skill level; NPCs read
        /// Yotthani's flat <c>AttackSpeedBonus</c> scaled by <c>NpcBonusShare</c>.
        /// </summary>
        public static float SwingSpeedBonus(Agent agent)
        {
            if (!IsMainHeroAgent(agent))
                return DualWieldSettings.Get().AttackSpeedBonus * DualWieldSettings.Get().NpcBonusShare;

            return Hero.MainHero.GetSkillValue(TORSkills.Berserk) * SwingSpeedPerLevel;
        }

        /// <summary>
        /// Movement-speed multiplier bonus (same contract as <see cref="SwingSpeedBonus"/>).
        /// </summary>
        public static float MovementSpeedBonus(Agent agent)
        {
            if (!IsMainHeroAgent(agent))
                return DualWieldSettings.Get().MovementSpeedBonus * DualWieldSettings.Get().NpcBonusShare;

            return Hero.MainHero.GetSkillValue(TORSkills.Berserk) * MovementSpeedPerLevel;
        }

        /// <summary>
        /// Offhand-damage multiplier. Starts at <see cref="OffHandPenaltyAtZero"/>
        /// at Berserk 0 and climbs linearly to 1.0 at <see cref="OffHandParitySkill"/>,
        /// capped at 1.0 so offhand never exceeds mainhand without an explicit perk.
        /// NPCs read Yotthani's flat default (0.85).
        /// </summary>
        public static float OffHandDamageMultiplier(Agent agent)
        {
            if (!IsMainHeroAgent(agent)) return DualWieldSettings.Get().OffHandDamageMultiplier;

            float skill = Hero.MainHero.GetSkillValue(TORSkills.Berserk);
            float progress = MathF.Clamp(skill / OffHandParitySkill, 0f, 1f);
            return OffHandPenaltyAtZero + (1f - OffHandPenaltyAtZero) * progress;
        }

        private static bool IsMainHeroAgent(Agent agent)
            => agent != null && agent.IsMainAgent && Hero.MainHero != null && TORSkills.Berserk != null;
    }
}
