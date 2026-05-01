using System.Collections.Generic;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.AI.CivilianMissionAI;
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
        public static void DamageAgents(IEnumerable<Agent> agents, int minDamage, int maxDamage = -1, Agent damager = null, TargetType targetType = TargetType.All, TriggeredEffectTemplate triggeredeffectTemplate = null, DamageType damageType = DamageType.Physical, bool hasShockWave = false, Vec3 impactPosition = default, AbilityTemplate originSpellTemplate = null, int castId = -1)
        {
            if (agents == null || damager == null) return;

            // Delegate to TORAbilityModel for aggregate damage handling
            if (Game.Current.GameType is Campaign)
            {
                var abilityModel = Campaign.Current.Models.GetAbilityModel();
                if (abilityModel != null)
                {
                    abilityModel.ApplySpellDamageToAgents(
                        agents,
                        minDamage,
                        maxDamage,
                        damager,
                        damageType,
                        originSpellTemplate,
                        triggeredeffectTemplate,
                        hasShockWave,
                        impactPosition,
                        castId);
                    return;
                }
            }

            // Fallback for non-campaign mode
            var mission = Mission.Current;

            foreach (var agent in agents)
            {
                if (agent == null || !agent.IsHuman ||  !agent.IsActive() || agent.Health < 1f || agent.IsFadingOut())
                {
                    continue;
                }

                if (mission != null && mission.FindAgentWithIndex(agent.Index) != agent)
                {
                    continue;
                }

                var baseDamage = maxDamage < minDamage ? minDamage : MBRandom.RandomInt(minDamage, maxDamage);
                if (baseDamage < 0) continue;

                if (impactPosition != default && hasShockWave && triggeredeffectTemplate != null)
                {
                    var distance = agent.Position.Distance(impactPosition);
                    baseDamage = (int)((triggeredeffectTemplate.Radius - distance) / triggeredeffectTemplate.Radius * baseDamage);
                }

                agent.ApplyDamage(baseDamage, impactPosition, damager, doBlow: true, hasShockWave: hasShockWave, originatesFromAbility: originSpellTemplate != null);
            }
        }

        public static void HealAgents(IEnumerable<Agent> agents, int minHeal, int maxHeal = -1, Agent healer = null, TargetType targetType = TargetType.Friendly, AbilityTemplate originSpellTemplate = null, int castId = -1)
        {
            if (agents == null) return;

            // Delegate to TORAbilityModel for aggregate healing handling
            if (Game.Current.GameType is Campaign)
            {
                var abilityModel = Campaign.Current.Models.GetAbilityModel();
                if (abilityModel != null)
                {
                    abilityModel.ApplySpellHealingToAgents(
                        agents,
                        minHeal,
                        maxHeal,
                        healer,
                        originSpellTemplate,
                        castId);
                    return;
                }
            }

            // Fallback for non-campaign mode
            var logic = Mission.Current?.GetMissionBehavior<AbilitySystem.AbilityManagerMissionLogic>();

            var mission = Mission.Current;

            foreach (var agent in agents)
            {
                if (agent == null || !agent.IsActive() || agent.Health <= 0f || agent.IsFadingOut())
                {
                    continue;
                }

                if (mission != null && mission.FindAgentWithIndex(agent.Index) != agent)
                {
                    continue;
                }
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

                // Book healing to session if we have a valid castId
                if (castId >= 0 && logic != null)
                {
                    logic.BookSpellHealing(castId, agent, amount);
                }

                if (CareerHelper.IsValidCareerMissionInteractionBetweenAgents(healer, agent))
                {
                    CareerHelper.ApplyCareerAbilityCharge(amount, ChargeType.Healed, AttackTypeMask.Spell, healer, agent);
                }
            }
        }

        public static void ApplyStatusEffectToAgents(IEnumerable<Agent> agents, string effectId, Agent applierAgent, float duration = 5, bool append = true, bool isMutated = false, int castId = -1)
        {
            if (agents == null)
            {
                return;
            }

            var mission = Mission.Current;

            foreach (var agent in agents)
            {
                if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Health < 1f || agent.IsFadingOut())
                {
                    continue;
                }
                if (mission != null && mission.FindAgentWithIndex(agent.Index) != agent)
                {
                    continue;
                }

                agent.ApplyStatusEffect(effectId, applierAgent, duration, append, isMutated, false, castId);
            }
        }

        /// <summary>
        /// Transitions enemy agents from civilian/passive state to hostile combat state.
        /// Used for quest scenarios where dialog triggers combat (like native hideout boss fights).
        /// </summary>
        public static void MakeEnemyAgentsHostile()
        {
            if (Mission.Current == null) return;

            // Collect enemy agents
            var enemyAgents = new List<Agent>();

            foreach (var agent in Mission.Current.Agents)
            {
                if (!agent.IsActive() || !agent.IsHuman) continue;

                if (agent.Team == Mission.Current.PlayerEnemyTeam)
                {
                    enemyAgents.Add(agent);
                }
            }

            Mission.Current.SetMissionMode(MissionMode.Battle, false);

            // Set teams as enemies (critical for AI to know who to attack)
            Mission.Current.PlayerTeam.SetIsEnemyOf(Mission.Current.PlayerEnemyTeam, true);

            // Force all enemy agents into fight mode
            foreach (var agent in enemyAgents)
            {
                ForceAgentForFight(agent);
            }

            // Set formation charge orders (like native hideout does)
            if (Mission.Current.PlayerEnemyTeam?.MasterOrderController != null)
            {
                Mission.Current.PlayerEnemyTeam.MasterOrderController.SelectAllFormations();
                Mission.Current.PlayerEnemyTeam.MasterOrderController.SetOrder(OrderType.Charge);
            }
        }

        /// <summary>
        /// Forces an agent into fight mode by removing civilian behaviors and setting combat state.
        /// </summary>
        private static void ForceAgentForFight(Agent agent)
        {
            var agentFlags = agent.GetAgentFlags();
            agent.SetAgentFlags((agentFlags | AgentFlag.CanAttack) & ~AgentFlag.CanRetreat);

            // Remove civilian behavior groups that interfere with combat by setting movement targets
            var agentNavigator = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
            if (agentNavigator != null)
            {
                agentNavigator.RemoveBehaviorGroup<TORDailyBehaviorGroup>();
                agentNavigator.ClearTarget();
            }

            agent.SetWatchState(Agent.WatchState.Alarmed);
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);

            if (Agent.Main != null && agent.Team != Agent.Main.Team)
            {
                agent.SetTargetAgent(Agent.Main);
            }
        }
    }
}