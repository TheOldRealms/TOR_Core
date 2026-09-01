using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class AnimationTriggerMissionLogic : MissionLogic
    {
        Dictionary<Agent, MBList<AnimationTriggerTuple>> _trackedAgents = new Dictionary<Agent, MBList<AnimationTriggerTuple>>();

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!Mission.Current.HasMissionBehavior<BattleSpawnLogic>() && Game.Current.GameType is Campaign)
            {
                //special mission types like troll caves don't have a spawn logic but should still permit tracking and usage of animation triggers
                if (!Mission.Current.HasMissionBehavior<AbilityManagerMissionLogic>() || !Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>().IsCastingMission())
                {
                    return;
                }
            }


            if (agent.IsHuman)
            {
                if (!agent.IsHero && agent.Character != null && agent.Character.HasAttribute(CharacterAttributes.HAS_ANIMATION_TRIGGERED_EFFECTS))
                {
                    _trackedAgents.Add(agent, new MBList<AnimationTriggerTuple>());
                }
                else if (agent.IsHero && agent.GetHero() != null && agent.GetHero().HasAttribute(CharacterAttributes.HAS_ANIMATION_TRIGGERED_EFFECTS))
                {
                    _trackedAgents.Add(agent, new MBList<AnimationTriggerTuple>());
                }
            }

        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            _trackedAgents.Remove(affectedAgent);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            _trackedAgents.Remove(affectedAgent);
        }

        public override void OnMissionTick(float dt)
        {
            foreach (var entry in _trackedAgents.ToMBList())
            {
                var agent = entry.Key;
                if (!agent.IsActive() || agent.IsFadingOut()) continue;

                var anim = agent.GetCurrentAction(1);
                if (AnimationTriggerManager.HasTriggersForAction(anim.GetName()))
                {
                    if (!_trackedAgents[agent].AnyQ(x => x.ActionName == anim.GetName()))
                    {
                        _trackedAgents[agent].Add(new AnimationTriggerTuple { ActionName = anim.GetName(), HasTriggered = false });
                        continue; // we only want 1 aoe trigger at a time?
                    }
                    else
                    {
                        var tuple = entry.Value.FirstOrDefaultQ(x => x.ActionName == anim.GetName());
                        var trigger = AnimationTriggerManager.GetTriggerForAction(tuple.ActionName);
                        var progress = agent.GetCurrentActionProgress(1);
                        if (!tuple.HasTriggered && progress > trigger.AnimationPercent)
                        {
                            _trackedAgents[agent].FirstOrDefaultQ(x => x.ActionName == anim.GetName()).HasTriggered = true;

                            Vec3 triggerPosition = agent.Position;
                            var visuals = agent.AgentVisuals;
                            if (visuals != null && visuals.IsValid())
                            {
                                var skeleton = visuals.GetSkeleton();
                                if (skeleton != null)
                                {
                                    // still prefer the exact bone when visuals are usable
                                    var frame = visuals.GetGlobalFrame();
                                    var bone = skeleton.GetBoneEntitialFrame(trigger.BoneIndex);
                                    triggerPosition = frame.TransformToParent(bone.origin);
                                }
                            }

                            TriggerEffect(trigger.TriggeredEffectId, triggerPosition, agent);
                        }
                        else if (tuple.HasTriggered && progress < trigger.AnimationPercent)
                        {
                            _trackedAgents[agent].FirstOrDefaultQ(x => x.ActionName == anim.GetName()).HasTriggered = false;
                        }
                    }
                }
            }
        }

        private void TriggerEffect(string effectName, Vec3 position, Agent agent)
        {
            var effect = TriggeredEffectManager.CreateNew(effectName);
            effect.Trigger(position, Vec3.Up, agent);
        }

        private void ApplySplashDamage(Agent affector, Vec3 position, float explosionRadius, int explosionDamage, float damageVariance)
        {
            MBList<Agent> list = new MBList<Agent>();
            var nearbyEnemies = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, explosionRadius, Mission.PlayerTeam, list).ToArray();
            for (int i = 0; i < nearbyEnemies.Length; i++)
            {
                var agent = nearbyEnemies[i];
                var distance = agent.Position.Distance(position);
                if (distance <= explosionRadius)
                {
                    var baseDamage = explosionDamage * MBRandom.RandomFloatRanged(1 - damageVariance, 1 + damageVariance);
                    var damage = (explosionRadius - distance) / explosionRadius * baseDamage;
                    agent.ApplyDamage((int)damage, position, affector, doBlow: true, hasShockWave: true, originatesFromAbility: false);
                }
            }
        }
    }

    public class AnimationTriggerTuple
    {
        public string ActionName { get; set; }
        public bool HasTriggered { get; set; }
    }
}