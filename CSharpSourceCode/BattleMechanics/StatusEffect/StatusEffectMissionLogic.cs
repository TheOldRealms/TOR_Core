using Ink.Parsed;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectMissionLogic : MissionLogic
    {
        private Queue<Agent> _unprocessedAgents = new Queue<Agent>();

        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsHuman)
            {
                StatusEffectComponent effectComponent = new StatusEffectComponent(agent);
                agent.AddComponent(effectComponent);
            }

            _unprocessedAgents.Enqueue(agent);
        }

        public override void OnAgentMount(Agent agent)
        {
            StatusEffectComponent effectComponent = agent.GetComponent<StatusEffectComponent>();
            effectComponent.SynchronizeBaseValues(true);
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (!IsValidAttributeKill(affectedAgent, affectorAgent, agentState))
            {
                return;
            }

            ApplyTheHungerOnKill(affectorAgent);
            ApplyFrenzyOnKill(affectorAgent);
        }

        private static bool IsValidAttributeKill(Agent killedAgent, Agent killerAgent, AgentState agentState)
        {
            if (killedAgent == null || killerAgent == null)
            {
                return false;
            }

            if (killedAgent == killerAgent)
            {
                return false;
            }

            if (!killedAgent.IsHuman || !killerAgent.IsHuman)
            {
                return false;
            }

            if (!killerAgent.IsActive())
            {
                return false;
            }

            if (killedAgent.Team == killerAgent.Team)
            {
                return false;
            }

            return agentState == AgentState.Killed || agentState == AgentState.Unconscious;
        }

        private static void ApplyTheHungerOnKill(Agent killerAgent)
        {
            if (!killerAgent.HasTheHunger())
            {
                return;
            }

            var healPercentOfMaxHealth = 0.15f;
            killerAgent.Heal(killerAgent.HealthLimit * healPercentOfMaxHealth);
        }

        private static void ApplyFrenzyOnKill(Agent killerAgent)
        {
            if (!killerAgent.HasFrenzy())
            {
                return;
            }

            var statusEffectComponent = killerAgent.GetComponent<StatusEffectComponent>();
            if (statusEffectComponent == null)
            {
                return;
            }

            var frenzyMovementEffectId = "trait_frenzy_movement_speed";
            var frenzyAttackSpeedEffectId = "trait_frenzy_attack_speed";
            var maxFrenzyStacks = 5;
            var frenzyStackDuration = 40f;

            if (statusEffectComponent.GetActiveEffectCount(frenzyAttackSpeedEffectId) >= maxFrenzyStacks)
            {
                return;
            }

            killerAgent.ApplyStatusEffect(frenzyMovementEffectId, killerAgent, frenzyStackDuration, false, false, true);
            killerAgent.ApplyStatusEffect(frenzyAttackSpeedEffectId, killerAgent, frenzyStackDuration, false, false, true);
        }

        public override void OnMissionTick(float dt)
        {
            foreach (var agent in Mission.Current.AllAgents)//Sly : I tried .Agents (active agents only, ie. ones not knocked out) to reduce the number of agents being checked and it lead to errors from the enumerable source being modified during use - probably because an agent getting killed will immediately remove them from the list which has a relatively high chance of happening, especially during spells.
                                                            //using AllAgents is generally less error prone because it's only modified during reinforcement waves, but if there's still instability here then it may be worth locally storing the agent list before iterating to prevent the enumerable source modified crash.
            {
                if (agent == null)
                {
                    TORCommon.Log("StatusEffectMissionLogic : null agent checked for status - probably dead and removed from the list due to reinforcements.", LogLevel.Warn);
                    continue;
                }

                var statusEffectComponent = agent.GetComponent<StatusEffectComponent>(); // bakilsin
                if (statusEffectComponent?.NeedsStatusEffectTick == true)
                {
                    statusEffectComponent.OnTick(dt);
                }

            }

            while (_unprocessedAgents.Count > 0)
            {
                var queueAgent = _unprocessedAgents.Dequeue();
                CheckUnitForAddingPermanentEffects(queueAgent);
            }
        }


        private void CheckUnitForAddingPermanentEffects(Agent agent)
        {
            if (agent?.Character == null) return;

            switch (agent.GetRegenerationTier())
            {
                case 3:
                    CareerHelper.AddDefaultPermanentMissionEffect(agent, "regeneration3");
                    break;
                case 2:
                    CareerHelper.AddDefaultPermanentMissionEffect(agent, "regeneration2");
                    break;
                case 1:
                    CareerHelper.AddDefaultPermanentMissionEffect(agent, "regeneration");
                    break;
            }

            if (agent.WieldedWeapon.IsEmpty) return;

            if (agent.GetOriginMobileParty()?.HasBlessing("cult_of_loec") == true)
            {
                CareerHelper.AddDefaultPermanentMissionEffect(agent, "loec_blessing_mvs");
                CareerHelper.AddDefaultPermanentMissionEffect(agent, "loec_blessing_ats");
            }

            if (!agent.BelongsToMainParty()) return;

            //a player can only have one career so don't bother checking more once it finds what it needs
            var mainHero = Hero.MainHero;
            if (mainHero.HasCareer(TORCareers.ImperialMagister))
            {
                CareerHelper.PowerstoneEffectAssignment(agent);
                return;
            }

            if (mainHero.HasCareer(TORCareers.KnightOldWorld))
            {
                CareerHelper.PuritySealAssignment(agent);
                return;
            }

            if (mainHero.HasCareer(TORCareers.Runelord))
            {
                CareerHelper.UnitRuneAssignment(agent);
                return;
            }

            if (mainHero.HasCareer(TORCareers.OrcBoss))
            {
                CareerHelper.ExtorsionAssignment(agent);
                return;
            }

            if (mainHero.HasCareer(TORCareers.Waywatcher))
            {
                var partyExtendedInfo = mainHero.PartyBelongedTo.GetPartyInfo();

                var infos = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == agent.Character.StringId);
                if (infos.Key != null) //Sly : dictionaries in c# don't take null keys, if you try to fetch null from them it will crash. I think this check is in case FirstOrDefault gave a null default.
                {
                    foreach (var id in infos.Value)
                    {
                        CareerHelper.AddDefaultPermanentMissionEffect(agent, id);
                    }

                    // HailOfArrowsPassive4: Troops with Swiftshiver Shards also get reload speed bonus
                    if (mainHero.HasCareerChoice("HailOfArrowsPassive4") && infos.Value.Contains("apply_swift_shiver_trait"))
                    {
                        CareerHelper.AddDefaultPermanentMissionEffect(agent, "swiftshiver_reload_bonus");
                    }
                }
                return;
            }
        }
    }
}