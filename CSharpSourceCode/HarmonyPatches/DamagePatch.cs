using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class DamagePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Agent), "HandleBlow")]
        public static bool PreHandleBlow(ref Blow b, Agent __instance)
        {
            Agent attacker = b.OwnerId != -1 ? Mission.Current.FindAgentWithIndex(b.OwnerId) : __instance;
            Agent victim = __instance;

            if (!victim.IsHuman || !attacker.IsHuman)
            {
                return true;
            }

            if (attacker == victim)
            {
                return true;
            }

            bool isSpell = false;
            float[] damageCategories = new float[(int)DamageType.All + 1];
            var attackerPropertyContainer = attacker.GetProperties(PropertyMask.Attack);
            var victimPropertyContainer = victim.GetProperties(PropertyMask.Defense);
            //attack properties;
            var damageProportions = attackerPropertyContainer.DamageProportions;
            var damagePercentages = attackerPropertyContainer.DamagePercentages;
            var additionalDamagePercentages = attackerPropertyContainer.AdditionalDamagePercentages;
            //defense properties
            var resistancePercentages = victimPropertyContainer.ResistancePercentages;

            if (b.StrikeType == StrikeType.Thrust && b.AttackType == AgentAttackType.Kick && b.DamageCalculated && b.BlowFlag.HasFlag(BlowFlags.NoSound) && b.VictimBodyPart == BoneBodyPartType.Chest)
            {
                isSpell = true;
            }

            if (isSpell)
            {
                var spellInfo = TORSpellBlowHelper.GetSpellInfo(victim.Index, attacker.Index);
                int damageType = (int)spellInfo.DamageType;
                damageCategories[damageType] = b.InflictedDamage;
                damagePercentages[damageType] -= resistancePercentages[damageType];
                damageCategories[damageType] *= 1 + damagePercentages[damageType];
                b.InflictedDamage = (int)damageCategories[damageType];
                if(Game.Current.GameType is Campaign)
                {
                    var abilityTemplate = AbilityFactory.GetTemplate(spellInfo.SpellID);
                    if (attacker.IsHero)
                    {
                        var hero = attacker.GetHero();
                        RewardHeroForAbilityDamage(hero, b.InflictedDamage, abilityTemplate);
                    }
                }
                
                if (attacker == Agent.Main || victim == Agent.Main)
                    TORDamageDisplay.DisplaySpellDamageResult(spellInfo.SpellID, spellInfo.DamageType, b.InflictedDamage, damagePercentages[damageType]);                
                return true;
            }

            var resultDamage = 0;
            var highestDamageValue = 0f;
            var highestNonPhysicalDamageType = DamageType.Physical;
            for (int i = 0; i < damageCategories.Length - 1; i++)
            {
                damageProportions[i] += additionalDamagePercentages[i];
                damageCategories[i] = b.InflictedDamage * damageProportions[i];
                damageCategories[i] += damageCategories[(int)DamageType.All] / (int)DamageType.All;
                if (damageCategories[i] > 0)
                {
                    if (damageCategories[i] > highestDamageValue && i != (int)DamageType.Physical)
                    {
                        highestDamageValue = damageCategories[i];
                        highestNonPhysicalDamageType = (DamageType)i;
                    }
                    damagePercentages[i] -= resistancePercentages[i];
                    damageCategories[i] *= 1 + damagePercentages[i];
                    resultDamage += (int)damageCategories[i];
                }
            }

            b.InflictedDamage = resultDamage;
            b.BaseMagnitude = resultDamage;

            if (b.InflictedDamage > 0)
            {
                if (attacker == Agent.Main || victim == Agent.Main)
                    TORDamageDisplay.DisplayDamageResult(resultDamage, damageCategories);
            }
            return true;
        }

        private static void RewardHeroForAbilityDamage(Hero hero, int inflictedDamage, AbilityTemplate template)
        {
            var model = Campaign.Current.Models.GetSpellcraftSkillModel();
            if (model != null)
            {
                if(template != null)
                {
                    var skill = model.GetRelevantSkillForAbility(template);
                    var amount = model.GetSkillXpForAbilityDamage(template, inflictedDamage);
                    hero.AddSkillXp(skill, amount);
                }
            }
        }
    }
}
