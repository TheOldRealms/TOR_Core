using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics
{
    /// <summary>
    /// Scene script ticker that drives <see cref="LevitationVisualLiftMissionView.TickSceneLift"/> inside
    /// <c>rglScene::tick</c>, between <c>tick_animations</c> and <c>rglScene::post_tick</c>, so
    /// <c>AgentRendererSceneController</c> captures the lifted frame for both skinned body and weapons.
    /// </summary>
    public class LevitationVisualLiftScript : ScriptComponentBehavior
    {
        private LevitationVisualLiftMissionView _owner;

        public void Initialize(LevitationVisualLiftMissionView owner)
        {
            _owner = owner;
        }

        public void Detach()
        {
            _owner = null;
        }

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.Tick | base.GetTickRequirement();
        }

        protected override void OnInit()
        {
            base.OnInit();
            SetScriptComponentToTick(GetTickRequirement());
        }

        protected override void OnTick(float dt)
        {
            _owner?.TickSceneLift(dt);
        }

        protected override void OnRemoved(int removeReason)
        {
            _owner?.OnTickerRemoved(this);
            _owner = null;
            base.OnRemoved(removeReason);
        }
    }

    /// <summary>
    /// Applies an extra vertical visual lift (+0.50 m on top of the +0.34 m clips = +0.84 m)
    /// inside scene tick after integrating native Foot-IK at ground height and before
    /// <c>rglScene::post_tick</c> snapshots agent frames for rendering, avoiding the single-foot
    /// ceiling bend glitch while lifting both skinned body meshes and attached weapons together.
    /// </summary>
    [DefaultView]
    public class LevitationVisualLiftMissionView : MissionView
    {
        public const float ExtraLiftHeight = 0.50f;
        private const float RampDuration = 0.55f;
        private const float FastDropDuration = 0.15f;
        private const float MaxBaseZDeviation = 0.60f;

        private sealed class LiftState
        {
            public float LiftProgress;
            public float CurrentLift;
            public bool HasWrittenLift;
            public float LastAppliedLift;
            public float LastWrittenZ;
        }

        private readonly Dictionary<Agent, LiftState> _tracked = new Dictionary<Agent, LiftState>(8);
        private readonly List<Agent> _toRemove = new List<Agent>(8);
        private GameEntity _tickerEntity;
        private LevitationVisualLiftScript _tickerScript;

        private void EnsureTickerEntity()
        {
            if (_tickerEntity != null && _tickerEntity.Pointer != UIntPtr.Zero && _tickerScript != null)
            {
                return;
            }

            if (Mission == null || Mission.Scene == null)
            {
                return;
            }

            GameEntity entity = GameEntity.CreateEmpty(Mission.Scene, false);
            entity.Name = "tor_levitation_visual_lift_ticker";
            entity.CreateAndAddScriptComponent(nameof(LevitationVisualLiftScript), false);
            LevitationVisualLiftScript script = entity.GetFirstScriptOfType<LevitationVisualLiftScript>();
            if (script != null)
            {
                script.Initialize(this);
                entity.CallScriptCallbacks(true);
                _tickerEntity = entity;
                _tickerScript = script;
            }
            else if (entity.Scene != null)
            {
                entity.Remove(0);
            }
        }

        private void CleanupTickerEntity()
        {
            _tickerScript?.Detach();
            GameEntity entity = _tickerEntity;
            _tickerEntity = null;
            _tickerScript = null;

            if (entity != null && entity.Pointer != UIntPtr.Zero && entity.Scene != null)
            {
                entity.Remove(0);
            }
        }

        internal void OnTickerRemoved(LevitationVisualLiftScript script)
        {
            if (ReferenceEquals(_tickerScript, script))
            {
                _tickerScript = null;
                _tickerEntity = null;
            }
        }

        private static bool ShouldLift(Agent agent, out bool isFalling)
        {
            isFalling = false;
            if (agent == null || !agent.IsActive() || agent.Health <= 0f || !agent.IsHuman || agent.HasMount || !agent.HasUsableVisuals())
            {
                return false;
            }

            if (!AbilityManagerMissionLogic.IsWieldingLevitateWeapon(agent))
            {
                return false;
            }

            if (agent.GetCurrentActionType(0) == Agent.ActionCodeType.Fall)
            {
                isFalling = true;
                return false;
            }

            return true;
        }

        private static float SmoothStep(float t)
        {
            t = MathF.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static void ApplySceneLift(Agent agent, LiftState state)
        {
            if (agent == null || !agent.HasUsableVisuals())
            {
                return;
            }

            MBAgentVisuals visuals = agent.AgentVisuals;
            MatrixFrame frame = visuals.GetFrame();
            float curZ = frame.origin.z;

            float baseZ = (state.HasWrittenLift && MathF.Abs(curZ - state.LastWrittenZ) < 0.002f)
                ? (curZ - state.LastAppliedLift)
                : curZ;

            float physZ = agent.Position.z;
            if (MathF.Abs(baseZ - physZ) > MaxBaseZDeviation)
            {
                baseZ = physZ;
            }

            if (MathF.Abs(curZ - baseZ) > 0.0005f)
            {
                frame.origin.z = baseZ;
                visuals.SetFrame(ref frame);
                curZ = baseZ;
            }

            if (state.CurrentLift > 0.0005f)
            {
                visuals.GetSkeleton()?.ForceUpdateBoneFrames();
            }

            float targetZ = baseZ + state.CurrentLift;
            if (MathF.Abs(curZ - targetZ) > 0.0005f)
            {
                frame.origin.z = targetZ;
                visuals.SetFrame(ref frame);
            }

            state.HasWrittenLift = state.CurrentLift > 0.0005f;
            state.LastAppliedLift = state.HasWrittenLift ? state.CurrentLift : 0f;
            state.LastWrittenZ = targetZ;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            EnsureTickerEntity();
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);
            EnsureTickerEntity();
        }

        internal void TickSceneLift(float dt)
        {
            if (Mission == null || Mission.Agents == null)
            {
                return;
            }

            var agents = Mission.Agents;
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];
                if (agent != null && agent.IsHuman && !agent.HasMount && agent.IsActive() && !_tracked.ContainsKey(agent))
                {
                    if (ShouldLift(agent, out _))
                    {
                        _tracked[agent] = new LiftState();
                    }
                }
            }

            if (_tracked.Count == 0)
            {
                return;
            }

            float clampedDt = dt > 0f ? MathF.Min(dt, 0.05f) : 0f;
            _toRemove.Clear();

            foreach (var kvp in _tracked)
            {
                Agent agent = kvp.Key;
                LiftState state = kvp.Value;

                if (agent == null || !agent.IsActive() || agent.Health <= 0f || agent.HasMount)
                {
                    state.LiftProgress = 0f;
                    state.CurrentLift = 0f;
                    if (agent != null && agent.HasUsableVisuals() && state.HasWrittenLift)
                    {
                        ApplySceneLift(agent, state);
                    }
                    _toRemove.Add(agent);
                    continue;
                }

                bool wantLift = ShouldLift(agent, out bool isFalling);
                if (clampedDt > 0f)
                {
                    if (wantLift)
                    {
                        state.LiftProgress = MathF.Min(1f, state.LiftProgress + clampedDt / RampDuration);
                    }
                    else
                    {
                        float dropDur = isFalling ? FastDropDuration : RampDuration;
                        state.LiftProgress = MathF.Max(0f, state.LiftProgress - clampedDt / dropDur);
                    }
                }

                state.CurrentLift = SmoothStep(state.LiftProgress) * ExtraLiftHeight;
                ApplySceneLift(agent, state);

                if (!wantLift && state.LiftProgress <= 0f && !state.HasWrittenLift)
                {
                    _toRemove.Add(agent);
                }
            }

            for (int i = 0; i < _toRemove.Count; i++)
            {
                _tracked.Remove(_toRemove[i]);
            }
            _toRemove.Clear();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (affectedAgent != null && _tracked.TryGetValue(affectedAgent, out var state))
            {
                state.LiftProgress = 0f;
                state.CurrentLift = 0f;
                if (affectedAgent.HasUsableVisuals() && state.HasWrittenLift)
                {
                    ApplySceneLift(affectedAgent, state);
                }
                _tracked.Remove(affectedAgent);
            }
        }

        public override void OnMissionScreenFinalize()
        {
            CleanupTickerEntity();
            _tracked.Clear();
            _toRemove.Clear();
            base.OnMissionScreenFinalize();
        }

        protected override void OnEndMission()
        {
            CleanupTickerEntity();
            _tracked.Clear();
            _toRemove.Clear();
            base.OnEndMission();
        }

        public override void OnRemoveBehavior()
        {
            CleanupTickerEntity();
            _tracked.Clear();
            _toRemove.Clear();
            base.OnRemoveBehavior();
        }
    }
}
