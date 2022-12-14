using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
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

            float[] damageCategories = new float[(int)DamageType.All + 1];
            var attackerPropertyContainer = attacker.GetProperties(PropertyMask.Attack);
            var victimPropertyContainer = victim.GetProperties(PropertyMask.Defense);
            //attack properties;
            var damageProportions = attackerPropertyContainer.DamageProportions;
            var damagePercentages = attackerPropertyContainer.DamagePercentages;
            var additionalDamagePercentages = attackerPropertyContainer.AdditionalDamagePercentages;
            //defense properties
            var resistancePercentages = victimPropertyContainer.ResistancePercentages;
            var resultDamage = 0;
            var wardSaveFactor = 1f;
            if (Game.Current.GameType is Campaign)
            {
                var model = MissionGameModels.Current.AgentApplyDamageModel;
                if(model != null && model is TORAgentApplyDamageModel)
                {
                    var torModel = model as TORAgentApplyDamageModel;
                    wardSaveFactor = torModel.CalculateWardSaveFactor(victim);
                }
            }

            //calculating spell damage
            if (TORSpellBlowHelper.IsSpellBlow(b))
            {
                var spellInfo = TORSpellBlowHelper.GetSpellBlowInfo(victim.Index, attacker.Index);
                int damageType = (int)spellInfo.DamageType;
                damageCategories[damageType] = b.InflictedDamage;
                damagePercentages[damageType] -= resistancePercentages[damageType];
                damageCategories[damageType] *= 1 + damagePercentages[damageType];
                resultDamage = (int)damageCategories[damageType];
                if(Game.Current.GameType is Campaign)
                {
                    var abilityTemplate = AbilityFactory.GetTemplate(spellInfo.OriginAbilityTemplateId);
                    if (attacker.IsHero && abilityTemplate != null)
                    {
                        var hero = attacker.GetHero();
                        var model = Campaign.Current.Models.GetSpellcraftModel();
                        if (model != null)
                        {
                            resultDamage = (int)(resultDamage * model.GetPerkEffectsOnAbilityDamage(hero.CharacterObject, victim, abilityTemplate));
                            var skill = model.GetRelevantSkillForAbility(abilityTemplate);
                            var amount = model.GetSkillXpForAbilityDamage(abilityTemplate, resultDamage);
                            hero.AddSkillXp(skill, amount);
                        }
                    }
                }
                resultDamage = (int)(resultDamage * wardSaveFactor);
                b.InflictedDamage = resultDamage;
                b.BaseMagnitude = resultDamage;
                if (attacker == Agent.Main || victim == Agent.Main)
                    TORDamageDisplay.DisplaySpellDamageResult(spellInfo.TriggeredEffectId, spellInfo.DamageType, resultDamage, damagePercentages[damageType]);                
                return true;
            }

            //calculating non-spell damage
            for (int i = 0; i < damageCategories.Length - 1; i++)
            {
                damageProportions[i] += additionalDamagePercentages[i];
                damageCategories[i] = b.InflictedDamage * damageProportions[i];
                damageCategories[i] += damageCategories[(int)DamageType.All] / (int)DamageType.All;
                if (damageCategories[i] > 0)
                {
                    damagePercentages[i] -= resistancePercentages[i];
                    damageCategories[i] *= 1 + damagePercentages[i];
                    resultDamage += (int)damageCategories[i];
                }
            }
            resultDamage = (int)(resultDamage * wardSaveFactor);
            b.InflictedDamage = resultDamage;
            b.BaseMagnitude = resultDamage;

            if (b.InflictedDamage > 0)
            {
                if (attacker == Agent.Main || victim == Agent.Main)
                    TORDamageDisplay.DisplayDamageResult(resultDamage, damageCategories);
            }
            return true;
        }
    }
}
