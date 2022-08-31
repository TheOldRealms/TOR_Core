using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Utilities
{
    public static class TORMissionHelper
    {
        public static void DamageAgents(IEnumerable<Agent> agents, int minDamage, int maxDamage = -1, Agent damager = null, TargetType targetType = TargetType.All, string spellID = "", DamageType damageType = DamageType.Physical, bool hasShockWave = false, Vec3 impactPosition = new Vec3())
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (spellID != "" && damager != null)
                        TORSpellBlowHelper.EnqueueSpellInfo(agent.Index, damager.Index, spellID, damageType);

                    if (maxDamage < minDamage)
                    {
                        agent.ApplyDamage(minDamage, impactPosition, damager, doBlow: true, hasShockWave: hasShockWave);
                    }
                    else
                    {
                        agent.ApplyDamage(MBRandom.RandomInt(minDamage, maxDamage), impactPosition, damager, doBlow: true, hasShockWave: hasShockWave);
                    }
                }
            }
        }

        public static void HealAgents(IEnumerable<Agent> agents, int minHeal, int maxHeal = -1, Agent healer = null, TargetType targetType = TargetType.Friendly)
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
