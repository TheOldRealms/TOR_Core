using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class CygorStepSoundMissionLogic : MissionLogic
    {
        private const float StepDistance = 4f;
        private const float MaxStepSlice = 8f;
        private const float StepShakeRange = 35f;
        private const float StepShakeIntensity = 0.55f;

        private static readonly string[] SoundIds =
        {
            "cygor_step_heavy_01",
            "cygor_step_heavy_02",
            "cygor_step_heavy_03",
            "cygor_step_heavy_04"
        };

        private static readonly string[] ThrowReleaseActions =
        {
            "act_release_heavy_thrown",
            "act_release_stone",
            "act_release_javelin"
        };

        private readonly Dictionary<int, Vec2> _lastPosition = new Dictionary<int, Vec2>();
        private readonly Dictionary<int, float> _travel = new Dictionary<int, float>();
        private readonly HashSet<int> _throwPlayed = new HashSet<int>();
        private readonly int[] _soundIndices = new int[SoundIds.Length];
        private ActionIndexCache[] _throwReleaseActions;
        private int _nextSound;
        private int _throwSoundIndex = -1;
        private bool _soundsResolved;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            ResolveSounds();

            var agents = Mission.Agents;
            if (agents == null) return;

            for (int i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                if (!IsCygor(agent)) continue;

                TryPlayThrowRelease(agent);
                if (!HasAnySound()) continue;

                Vec2 position = agent.Position.AsVec2;
                if (!_lastPosition.TryGetValue(agent.Index, out Vec2 previous))
                {
                    _lastPosition[agent.Index] = position;
                    _travel[agent.Index] = 0f;
                    continue;
                }

                float moved = (float)Math.Sqrt((position - previous).LengthSquared);
                _lastPosition[agent.Index] = position;
                if (moved <= 0.01f || moved > MaxStepSlice) continue;

                float traveled = _travel[agent.Index] + moved;
                if (traveled < StepDistance)
                {
                    _travel[agent.Index] = traveled;
                    continue;
                }

                _travel[agent.Index] = traveled - StepDistance;
                PlayStep(agent.Position);
                CygorCameraShake.Apply(agent.Position, StepShakeRange, StepShakeIntensity);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            if (affectedAgent == null) return;
            _lastPosition.Remove(affectedAgent.Index);
            _travel.Remove(affectedAgent.Index);
            _throwPlayed.Remove(affectedAgent.Index);
        }

        private void ResolveSounds()
        {
            if (_soundsResolved) return;
            _soundsResolved = true;
            for (int i = 0; i < SoundIds.Length; i++)
            {
                _soundIndices[i] = SoundEvent.GetEventIdFromString(SoundIds[i]);
            }

            _throwSoundIndex = SoundEvent.GetEventIdFromString("cygor_throw_whoosh");
            _throwReleaseActions = new ActionIndexCache[ThrowReleaseActions.Length];
            for (int i = 0; i < ThrowReleaseActions.Length; i++)
            {
                _throwReleaseActions[i] = ActionIndexCache.Create(ThrowReleaseActions[i]);
            }
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
            if (!IsCygor(shooterAgent) || _throwPlayed.Contains(shooterAgent.Index)) return;

            ResolveSounds();
            if (_throwSoundIndex < 0) return;

            _throwPlayed.Add(shooterAgent.Index);
            PlaySound(_throwSoundIndex, shooterAgent.Position);
        }

        private void TryPlayThrowRelease(Agent agent)
        {
            if (_throwSoundIndex < 0 || _throwReleaseActions == null) return;

            if (!IsThrowRelease(agent))
            {
                _throwPlayed.Remove(agent.Index);
                return;
            }

            if (_throwPlayed.Contains(agent.Index)) return;

            _throwPlayed.Add(agent.Index);
            PlaySound(_throwSoundIndex, agent.Position);
        }

        private bool IsThrowRelease(Agent agent)
        {
            var upper = agent.GetCurrentAction(1);
            var lower = agent.GetCurrentAction(0);
            for (int i = 0; i < _throwReleaseActions.Length; i++)
            {
                var action = _throwReleaseActions[i];
                if (upper == action || lower == action) return true;
            }

            return false;
        }

        private void PlaySound(int index, Vec3 position)
        {
            if (index < 0) return;
            Mission.MakeSound(
                index,
                position,
                soundCanBePredicted: false,
                isReliable: false,
                relatedAgent1: -1,
                relatedAgent2: -1);
        }

        private bool HasAnySound()
        {
            for (int i = 0; i < _soundIndices.Length; i++)
            {
                if (_soundIndices[i] >= 0) return true;
            }

            return false;
        }

        private void PlayStep(Vec3 position)
        {
            for (int n = 0; n < _soundIndices.Length; n++)
            {
                int index = _soundIndices[_nextSound];
                _nextSound = (_nextSound + 1) % _soundIndices.Length;
                if (index < 0) continue;

                PlaySound(index, position);
                return;
            }
        }

        private static bool IsCygor(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return false;
            if (agent.Monster != null && agent.Monster.StringId == "cygor") return true;
            return agent.Character != null && agent.Character.IsCygor();
        }
    }
}
