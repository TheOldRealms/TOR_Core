using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

namespace TOR_Core.Utilities
{
    public static class TORMissionHelper
    {
        public static void DamageAgents(IEnumerable<Agent> agents, int minDamage, int maxDamage = -1, Agent damager = null, TargetType targetType = TargetType.All, TriggeredEffectTemplate triggeredeffectTemplate = null, DamageType damageType = DamageType.Physical, bool hasShockWave = false, Vec3 impactPosition = new Vec3(), AbilityTemplate originSpellTemplate = null)
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (triggeredeffectTemplate != null && damager != null)
                        TORSpellBlowHelper.EnqueueSpellBlowInfo(agent.Index, damager.Index, triggeredeffectTemplate.StringID, damageType, originSpellTemplate == null ? string.Empty : originSpellTemplate.StringID);

                    var damage = maxDamage < minDamage ? minDamage : MBRandom.RandomInt(minDamage, maxDamage);
                    agent.ApplyDamage(damage, impactPosition, damager, doBlow: true, hasShockWave: hasShockWave);
                }
            }
        }

        public static void HealAgents(IEnumerable<Agent> agents, int minHeal, int maxHeal = -1, Agent healer = null, TargetType targetType = TargetType.Friendly, AbilityTemplate originSpellTemplate = null)
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (maxHeal < minHeal)
                    {
                        agent.Heal(minHeal);
                    }
                    else
                    {
                        agent.Heal(MBRandom.RandomInt(minHeal, maxHeal));
                    }
                }
            }
        }

        public static void ApplyStatusEffectToAgents(IEnumerable<Agent> agents, string effectId, Agent applierAgent, float multiplier = 1f, TargetType targetType = TargetType.All)
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    agent.ApplyStatusEffect(effectId, applierAgent, multiplier);
                }
            }
        }
    }
}
