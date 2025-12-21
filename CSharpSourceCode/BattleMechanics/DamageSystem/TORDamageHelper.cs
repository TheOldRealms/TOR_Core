using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.BattleMechanics.DamageSystem
{
    /// <summary>
    /// Static helper class for common damage calculation logic shared between
    /// spell damage (TORAbilityModel) and melee/ranged damage (TORAgentApplyDamageModel).
    /// </summary>
    public static class TORDamageHelper
    {
        /// <summary>
        /// Applies career passives to damage and resistance percentages for both attacker and victim.
        /// </summary>
        public static void ApplyCareerPassives(
            Agent attacker,
            Agent victim,
            AttackTypeMask attackTypeMask,
            float[] additionalDamagePercentages,
            float[] resistancePercentages)
        {
            if (Game.Current.GameType is not Campaign)
                return;

            if (!CareerHelper.IsValidCareerMissionInteractionBetweenAgents(attacker, victim))
                return;

            if (attacker.BelongsToMainParty())
            {
                var careerBonuses = CareerHelper.AddCareerPassivesForDamageValues(attacker, victim, attackTypeMask, PropertyMask.Attack);
                for (var index = 0; index < careerBonuses.Length; index++)
                {
                    additionalDamagePercentages[index] += careerBonuses[index];
                }
            }

            if (victim.BelongsToMainParty())
            {
                var careerBonuses = CareerHelper.AddCareerPassivesForDamageValues(attacker, victim, attackTypeMask, PropertyMask.Defense);
                for (var index = 0; index < careerBonuses.Length; index++)
                {
                    resistancePercentages[index] += careerBonuses[index];
                }
            }
        }

        /// <summary>
        /// Calculates final damage using TOR's damage type system with proportions, amplifications, and resistances.
        /// Used for melee/ranged attacks where damage is split across multiple damage types.
        /// </summary>
        /// <param name="baseDamage">The base damage before TOR modifications</param>
        /// <param name="damageProportions">How damage is split across types (should sum to 1.0)</param>
        /// <param name="damageAmplifications">Attacker's damage amplification per type</param>
        /// <param name="additionalDamagePercentages">Additional damage bonuses per type</param>
        /// <param name="resistancePercentages">Victim's resistance per type</param>
        /// <param name="damageCategories">Output array for damage per category (for display)</param>
        /// <returns>Total damage after all modifications</returns>
        public static float CalculateDamageWithProportions(
            float baseDamage,
            float[] damageProportions,
            float[] damageAmplifications,
            float[] additionalDamagePercentages,
            float[] resistancePercentages,
            out float[] damageCategories)
        {
            damageCategories = new float[(int)DamageType.All + 1];
            float resultDamage = 0;

            for (int i = 0; i < damageCategories.Length - 1; i++)
            {
                damageProportions[i] += additionalDamagePercentages[i];
                damageCategories[i] = baseDamage * damageProportions[i];
                damageCategories[i] += damageCategories[(int)DamageType.All] / (int)DamageType.All;
                if (damageCategories[i] > 0)
                {
                    damageAmplifications[i] -= resistancePercentages[i];
                    damageCategories[i] *= 1 + damageAmplifications[i];
                    resultDamage += damageCategories[i];
                }
            }

            return resultDamage;
        }

        /// <summary>
        /// Calculates final damage for a single damage type (used for spells).
        /// </summary>
        /// <param name="baseDamage">The base damage before modifications</param>
        /// <param name="damageType">The type of damage being dealt</param>
        /// <param name="damageAmplifications">Attacker's damage amplification per type</param>
        /// <param name="additionalDamagePercentages">Additional damage bonuses per type</param>
        /// <param name="resistancePercentages">Victim's resistance per type</param>
        /// <returns>Final damage after amplifications and resistances</returns>
        public static float CalculateSingleTypeDamage(
            float baseDamage,
            DamageType damageType,
            float[] damageAmplifications,
            float[] additionalDamagePercentages,
            float[] resistancePercentages)
        {
            int damageTypeIndex = (int)damageType;

            // Apply amplifications and resistances for this damage type
            damageAmplifications[damageTypeIndex] += additionalDamagePercentages[damageTypeIndex];
            damageAmplifications[damageTypeIndex] -= resistancePercentages[damageTypeIndex];

            return baseDamage * (1 + damageAmplifications[damageTypeIndex]);
        }
    }
}
