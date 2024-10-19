using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;

namespace TOR_Core.Utilities
{
    public static class TORMissionHelper
    {
        public static void DamageAgents(IEnumerable<Agent> agents, int minDamage, int maxDamage = -1, Agent damager = null, TargetType targetType = TargetType.All, TriggeredEffectTemplate triggeredeffectTemplate = null, DamageType damageType = DamageType.Physical, bool hasShockWave = false, Vec3 impactPosition = default, AbilityTemplate originSpellTemplate = null)
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (agent == null) continue;
                    if (triggeredeffectTemplate != null && damager != null)
                        TORSpellBlowHelper.EnqueueSpellBlowInfo(agent.Index, damager.Index, triggeredeffectTemplate.StringID, damageType, originSpellTemplate == null ? string.Empty : originSpellTemplate.StringID);

                    var damage = maxDamage < minDamage ? minDamage : MBRandom.RandomInt(minDamage, maxDamage);
                    if (damage < 0) continue;
                    if (impactPosition != default && hasShockWave && triggeredeffectTemplate != null)
                    {
                        var distance = agent.Position.Distance(impactPosition);
                        damage = (int)((triggeredeffectTemplate.Radius - distance) / triggeredeffectTemplate.Radius * damage);
                    }
                    agent.ApplyDamage(damage, impactPosition, damager, doBlow: true, hasShockWave: hasShockWave, originatesFromAbility: originSpellTemplate != null);
                }
            }
        }

        public static void HealAgents(IEnumerable<Agent> agents, int minHeal, int maxHeal = -1, Agent healer = null, TargetType targetType = TargetType.Friendly, AbilityTemplate originSpellTemplate = null)
        {
            //ideal place to add also perk effects of skills and careers ?
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    var amount = minHeal;
                    if (maxHeal < minHeal)
                    {
                        agent.Heal(minHeal);
                    }
                    else
                    { 
                        amount = MBRandom.RandomInt(minHeal, maxHeal);
                        agent.Heal(amount);
                    }

                    if (CareerHelper.IsValidCareerMissionInteractionBetweenAgents(healer,agent))
                    {
                        CareerHelper.ApplyCareerAbilityCharge(amount,ChargeType.Healed,AttackTypeMask.Spell,healer,agent);
                    }
                }
            }
        }

        public static void ApplyStatusEffectToAgents(IEnumerable<Agent> agents, string effectId, Agent applierAgent, float duration = 5, bool append = true, bool isMutated = false)
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    agent.ApplyStatusEffect(effectId, applierAgent, duration, append, isMutated);
                }
            }
        }
    }
}
