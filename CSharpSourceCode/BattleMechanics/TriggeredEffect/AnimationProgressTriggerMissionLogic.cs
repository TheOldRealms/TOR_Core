using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class AnimationProgressTriggerMissionLogic : MissionLogic
    {
        private const float BoulderShakeRange = 60f;
        private const float BoulderShakeIntensity = 1f;
        private const float BoulderImpactForce = 1f;
        private const string BoulderImpactEvent = "event:/mission/combat/impact/boulder_stone/";

        private static readonly HashSet<string> GroundMaterials = new HashSet<string>
        {
            "adobe", "default", "grass", "mud", "path", "sand", "soil", "stone", "straw"
        };

        private readonly HashSet<long> _fired = new HashSet<long>();
        private readonly Dictionary<long, float> _lastProgress = new Dictionary<long, float>();
        private List<(AnimationProgressTrigger trigger, int raceInt, ActionIndexCache[] actions)> _compiled;

        public override void AfterStart()
        {
            base.AfterStart();
            _fired.Clear();
            _lastProgress.Clear();
            _compiled = new List<(AnimationProgressTrigger, int, ActionIndexCache[])>();
            foreach (var trigger in AnimationProgressTriggerManager.Instance.Triggers)
            {
                int raceInt = TaleWorlds.Core.FaceGen.GetRaceOrDefault(trigger.RaceId);
                var actions = trigger.ActionIdList.Select(id => ActionIndexCache.Create(id)).ToArray();
                _compiled.Add((trigger, raceInt, actions));
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_compiled == null || _compiled.Count == 0) return;

            var agents = Mission.Agents;
            if (agents == null) return;

            for (int a = 0; a < agents.Count; a++)
            {
                var agent = agents[a];
                if (agent == null || !agent.IsActive()) continue;

                for (int i = 0; i < _compiled.Count; i++)
                {
                    var c = _compiled[i];
                    if (agent.Character == null || agent.Character.Race != c.raceInt) continue;

                    long key = ((long)agent.Index << 8) | (uint)i;

                    int matchedChannel = -1;
                    var action1 = agent.GetCurrentAction(1);
                    if (c.actions.Any(act => act == action1))
                    {
                        matchedChannel = 1;
                    }
                    else
                    {
                        var action0 = agent.GetCurrentAction(0);
                        if (c.actions.Any(act => act == action0))
                        {
                            matchedChannel = 0;
                        }
                    }

                    if (matchedChannel == -1)
                    {
                        if (!_fired.Contains(key)
                            && c.trigger.MinProgressOnInterrupt >= 0f
                            && _lastProgress.TryGetValue(key, out var lp)
                            && lp >= c.trigger.MinProgressOnInterrupt)
                        {
                            _fired.Add(key);
                            FireTrigger(agent, c.trigger);
                        }
                        _fired.Remove(key);
                        _lastProgress.Remove(key);
                        continue;
                    }

                    if (_fired.Contains(key)) continue;

                    float progress = agent.GetCurrentActionProgress(matchedChannel);
                    _lastProgress[key] = progress;

                    if (progress < c.trigger.TriggerProgress) continue;

                    _fired.Add(key);
                    FireTrigger(agent, c.trigger);
                }
            }
        }

        public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            base.OnMeleeHit(attacker, victim, isCanceled, collisionData);
            if (attacker == null || _compiled == null) return;

            for (int i = 0; i < _compiled.Count; i++)
            {
                var c = _compiled[i];
                if (c.trigger.MinProgressOnInterrupt < 0f) continue;
                if (attacker.Character == null || attacker.Character.Race != c.raceInt) continue;

                var a1 = attacker.GetCurrentAction(1);
                var a0 = attacker.GetCurrentAction(0);
                if (!c.actions.Any(x => x == a1 || x == a0)) continue;

                long key = ((long)attacker.Index << 8) | (uint)i;
                if (_fired.Contains(key)) continue;

                _fired.Add(key);
                FireTrigger(attacker, c.trigger);
            }
        }

        private void FireTrigger(Agent agent, AnimationProgressTrigger trigger)
        {
            var pos = GetBoneGlobalPosition(agent, trigger.BoneIndex);
            var effect = TriggeredEffectManager.CreateNew(trigger.TriggeredEffectId);
            effect?.Trigger(pos, Vec3.Up, agent);
            if (string.Equals(trigger.RaceId, "cygor", StringComparison.Ordinal))
            {
                PlayBoulderImpact(agent, pos);
                CygorCameraShake.Apply(agent.Position, BoulderShakeRange, BoulderShakeIntensity);
            }
        }

        private void PlayBoulderImpact(Agent agent, Vec3 position)
        {
            int soundIndex = SoundEvent.GetEventIdFromString(BoulderImpactEvent + GroundMaterial(agent));
            if (soundIndex < 0)
            {
                soundIndex = SoundEvent.GetEventIdFromString(BoulderImpactEvent + "default");
            }
            if (soundIndex < 0) return;

            var parameter = new SoundEventParameter("Force", BoulderImpactForce);
            Mission.MakeSound(
                soundIndex,
                position,
                soundCanBePredicted: false,
                isReliable: false,
                relatedAgent1: -1,
                relatedAgent2: -1,
                ref parameter);
        }

        private static string GroundMaterial(Agent agent)
        {
            int index = agent.GetGroundMaterialForCollisionEffect();
            if (index < 0 || index >= PhysicsMaterial.GetMaterialCount()) return "default";

            string name = PhysicsMaterial.GetNameAtIndex(index);
            if (string.IsNullOrEmpty(name) || !GroundMaterials.Contains(name)) return "default";
            return name;
        }

        private static Vec3 GetBoneGlobalPosition(Agent agent, sbyte bone)
        {
            var visuals = agent.AgentVisuals;
            if (visuals == null) return agent.Position;

            try
            {
                var localFrame = visuals.GetBoneEntitialFrame(bone, false);
                var globalFrame = visuals.GetGlobalFrame();
                var worldFrame = globalFrame.TransformToParent(localFrame);
                var pos = worldFrame.origin;

                float h = 0f;
                if (Mission.Current?.Scene != null && Mission.Current.Scene.GetHeightAtPoint(pos.AsVec2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref h))
                {
                    pos.z = h;
                }
                return pos;
            }
            catch
            {
                return agent.Position;
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            if (affectedAgent != null)
            {
                _fired.RemoveWhere(k => (k >> 8) == affectedAgent.Index);
                var keysToRemove = _lastProgress.Keys.Where(k => (k >> 8) == affectedAgent.Index).ToList();
                foreach (var k in keysToRemove)
                {
                    _lastProgress.Remove(k);
                }
            }
        }
    }
}
