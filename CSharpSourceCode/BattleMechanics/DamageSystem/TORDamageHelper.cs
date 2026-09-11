using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.DamageSystem
{
    /// <summary>
    /// Static helper class for common damage calculation logic shared between
    /// spell damage (TORAbilityModel) and melee/ranged damage (TORAgentApplyDamageModel).
    /// </summary>
    public static class TORDamageHelper
    {
        /// <summary>
        /// Halves the remaining friendly-fire damage after every other modifier.
        /// Native hits and already-calculated scripted hits call this at their respective final damage boundaries.
        /// </summary>
        public static float ApplyIronbreakerFriendlyFireReduction(Agent attacker, Agent victim, float damage)
        {
            if (Campaign.Current == null ||
                Hero.MainHero?.HasCareerChoice("GromrilArmorPassive4") != true ||
                attacker == null || victim == null || attacker == victim ||
                attacker.Team == null || attacker.Team == Team.Invalid || attacker.Team != victim.Team ||
                victim.Character?.Culture == null || !victim.Character.IsIronbreakerUnit() ||
                !victim.BelongsToMainParty())
            {
                return damage;
            }

            return Math.Max(0f, damage) * 0.5f;
        }

        /// <summary>
        /// Nest Cleansing adds 50 percentage points to the resistance for an explosion's damage type.
        /// Fire is already covered by the status effect and must not receive the same bonus twice.
        /// </summary>
        public static void ApplyNestCleansingExplosionResistance(Agent victim, DamageType damageType, float[] resistancePercentages)
        {
            if (victim != null && damageType != DamageType.Fire && damageType != DamageType.All &&
                victim.HasAttribute("NestCleansing"))
            {
                resistancePercentages[(int)damageType] += 0.5f;
            }
        }

        /// <summary>
        /// Applies Ironbreaker defenses to artillery splash, whose ordinary path is raw damage.
        /// Does not introduce spell amplification or change damage to unprotected agents.
        /// </summary>
        public static float ApplyIronbreakerExplosionDefenses(Agent attacker, Agent victim, float damage)
        {
            if (Campaign.Current == null || attacker == null || victim == null || !victim.IsHuman || damage <= 0f)
            {
                return damage;
            }

            var hasCareerDefense = victim.HasAttribute("Impenetrable") || victim.HasAttribute("NestCleansing");
            if (!hasCareerDefense && victim.BelongsToMainParty())
            {
                var passiveType = victim.IsHero && victim.IsMainAgent ? PassiveEffectType.Resistance : PassiveEffectType.TroopResistance;
                foreach (var choice in CareerHelper.GetCachedChoicesByType(passiveType))
                {
                    if ((choice.StringId == "GromrilArmorPassive1" || choice.StringId == "ShieldwallPassive3") &&
                        choice.Passive.IsValidCombatInteraction(attacker, victim, AttackTypeMask.Ranged))
                    {
                        hasCareerDefense = true;
                        break;
                    }
                }
            }

            if (!hasCareerDefense)
            {
                return damage;
            }

            var damageModel = MissionGameModels.Current?.AgentApplyDamageModel as TORAgentApplyDamageModel;
            if (damageModel == null)
            {
                return damage;
            }

            var resistances = damageModel.CreateAgentPropertyContainer(victim, PropertyMask.Defense, AttackTypeMask.Ranged).ResistancePercentages;
            ApplyCareerPassives(attacker, victim, AttackTypeMask.Ranged, new float[(int)DamageType.All + 1], resistances);
            ApplyNestCleansingExplosionResistance(victim, DamageType.Physical, resistances);

            var physicalFactor = Math.Max(0f, 1f - resistances[(int)DamageType.Physical]);
            var wardFactor = damageModel.CalculateWardSaveFactor(attacker, victim, resistances, attacker.Team == victim.Team);
            return Math.Min(damage, Math.Max(0f, damage * physicalFactor * wardFactor));
        }

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

        /// <summary>
        /// Determines the attack type mask from a Blow.
        /// </summary>
        public static AttackTypeMask DetermineMask(Blow blow)
        {
            if (TORSpellBlowHelper.IsSpellBlow(blow)) return AttackTypeMask.Spell;
            if (blow.IsMissile)
            {
                return AttackTypeMask.Ranged;
            }

            return AttackTypeMask.Melee;
        }

        /// <summary>
        /// Determines the attack type mask from a KillingBlow.
        /// </summary>
        public static AttackTypeMask DetermineMask(KillingBlow blow)
        {
            if (TORSpellBlowHelper.IsSpellBlow(blow)) return AttackTypeMask.Spell;
            if (blow.IsMissile)
            {
                return AttackTypeMask.Ranged;
            }

            return AttackTypeMask.Melee;
        }
    }
}
