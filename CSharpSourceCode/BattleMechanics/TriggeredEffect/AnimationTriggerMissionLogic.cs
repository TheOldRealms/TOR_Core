using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class AnimationTriggerMissionLogic : MissionLogic
    {
        Dictionary<Agent, MBList<AnimationTriggerTuple>> _trackedAgents = new Dictionary<Agent, MBList<AnimationTriggerTuple>>();

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!Mission.Current.HasMissionBehavior<BattleSpawnLogic>() && Game.Current.GameType is Campaign) return;
            if (agent.IsHuman)
            {
                if (!agent.IsHero && agent.Character != null && agent.Character.HasAttribute("HasAnimationTriggeredEffects"))
                {
                    _trackedAgents.Add(agent, new MBList<AnimationTriggerTuple>());
                }
                else if(agent.IsHero && agent.GetHero() != null && agent.GetHero().HasAttribute("HasAnimationTriggeredEffects"))
                {
                    _trackedAgents.Add(agent, new MBList<AnimationTriggerTuple>());
                }
            }
            
        }

        public override void OnMissionTick(float dt)
        {
            foreach (var entry in _trackedAgents.ToMBList()) 
            {
                var agent = entry.Key;
                if (!agent.IsActive()) continue;
                var anim = agent.GetCurrentAction(1);
                if (AnimationTriggerManager.HasTriggersForAction(anim.Name))
                {
                    if (!_trackedAgents[agent].AnyQ(x => x.ActionName == anim.Name))
                    {
                        _trackedAgents[agent].Add(new AnimationTriggerTuple { ActionName = anim.Name, HasTriggered = false});
                        return;
                    }
                    else
                    {
                        var tuple = entry.Value.FirstOrDefaultQ(x => x.ActionName == anim.Name);
                        var trigger = AnimationTriggerManager.GetTriggerForAction(tuple.ActionName);
                        var progress = agent.GetCurrentActionProgress(1);
                        if (!tuple.HasTriggered && progress > trigger.AnimationPercent)
                        {
                            _trackedAgents[agent].FirstOrDefaultQ(x => x.ActionName == anim.Name).HasTriggered = true;
                            var frame = entry.Key.AgentVisuals.GetGlobalFrame();
                            var bone = entry.Key.AgentVisuals.GetSkeleton().GetBoneEntitialFrame(trigger.BoneIndex);
                            var pos = frame.TransformToParent(bone.origin);
                            TriggerEffect(trigger.TriggeredEffectId, pos, agent);

                        }
                        else if(tuple.HasTriggered && progress < trigger.AnimationPercent)
                        {
                            _trackedAgents[agent].FirstOrDefaultQ(x => x.ActionName == anim.Name).HasTriggered = false;
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
