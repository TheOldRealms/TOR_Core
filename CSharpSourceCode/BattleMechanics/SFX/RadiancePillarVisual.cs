using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.Utilities;
using SceneEntity = TaleWorlds.Engine.GameEntity;

namespace TOR_Core.BattleMechanics.SFX
{
    // Give the beam its own lifetime so it can fade after the spell ends.
    public class RadiancePillarSpawner : ScriptComponentBehavior
    {
        private const string ColumnPrefab = "radiance_pillar_column";
        private const int MaxOwnerWaitTicks = 30;
        private static bool _missingPrefabLogged;

        private AbilityScript _ability;
        private RadiancePillarColumn _column;
        private float _elapsed;
        private int _waitTicks;
        private bool _spawnAttempted;
        private bool _done;

        public override TickRequirement GetTickRequirement() => _done ? TickRequirement.None : TickRequirement.Tick;

        protected override void OnInit()
        {
            base.OnInit();
            SetScriptComponentToTick(GetTickRequirement());
        }

        protected override void OnTick(float dt)
        {
            if (_done || !(dt > 0f) || float.IsInfinity(dt)) return;
            _elapsed += dt;
            try
            {
                _ability ??= AbilityVisualClock.FindOwner(GameEntity);
                if (_ability == null)
                {
                    if (++_waitTicks > MaxOwnerWaitTicks) Stop();
                    return;
                }

                if (!_spawnAttempted) Spawn();
                if (_column == null || _ability.IsFading)
                {
                    Stop();
                    return;
                }
                _column.KeepAlive();
            }
            catch (Exception e)
            {
                TORCommon.Log("RadiancePillarSpawner stopped: " + e.Message, NLog.LogLevel.Warn);
                Stop();
            }
        }

        private void Spawn()
        {
            _spawnAttempted = true;
            var scene = Mission.Current?.Scene;
            var template = _ability.Ability?.Template;
            if (scene == null || template == null) return;
            if (!SceneEntity.PrefabExists(ColumnPrefab))
            {
                if (!_missingPrefabLogged)
                {
                    _missingPrefabLogged = true;
                    TORCommon.Log("Pillar of Radiance column prefab missing: " + ColumnPrefab, NLog.LogLevel.Warn);
                }
                return;
            }

            var origin = GameEntity.GetGlobalFrame().origin;
            if (!IsFinite(origin)) return;

            var root = SceneEntity.InstantiateWithRestOffset(scene, ColumnPrefab, false,
                new MatrixFrame(Mat3.Identity, origin), 0f, callScriptCallbacks: false);
            if (root == null) return;

            try
            {
                var column = root.GetFirstScriptOfType<RadiancePillarColumn>();
                if (column == null ||
                    !column.Configure(root, template.Duration, template.TickInterval, AbilityVisualClock.Life(_ability, 0f)))
                {
                    root.FadeOut(0.05f, true);
                    return;
                }

                // Put the light on the beam so the ground dims with it.
                if (!template.HasLight && template.LightIntensity > 0f && template.LightRadius > 0f)
                {
                    var light = Light.CreatePointLight(template.LightRadius);
                    light.LightColor = template.LightColorRGB;
                    light.Intensity = template.LightIntensity;
                    light.SetShadowType(Light.ShadowType.NoShadow);
                    light.ShadowEnabled = false;
                    light.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, column.LightHeight));
                    light.SetVisibility(true);
                    if (root.AddLight(light)) column.SetLight(light);
                }

                root.CallScriptCallbacks(true);
                _column = column;
            }
            catch (Exception e)
            {
                TORCommon.Log("Pillar of Radiance column not created: " + e.Message, NLog.LogLevel.Warn);
                _column = null;
                root.FadeOut(0.05f, true);
            }
        }

        protected override void OnRemoved(int removeReason)
        {
            _done = true;
            _column?.Release();
            _column = null;
            _ability = null;
            base.OnRemoved(removeReason);
        }

        private void Stop()
        {
            _column?.Release();
            _column = null;
            _ability = null;
            if (_done) return;
            _done = true;
            SetScriptComponentToTick(TickRequirement.None);
        }

        private static bool IsFinite(Vec3 v) =>
            !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
            !float.IsNaN(v.z) && !float.IsInfinity(v.z);
    }

    public class RadiancePillarColumn : ScriptComponentBehavior
    {
        public float FadeSeconds = 10f;
        public float Height = 84f;
        public float GroundSink = 15f; // Hide the faded end of the beam below ground.
        public float CoreWidth = 5f;
        public float SheathWidth = 14f;
        public float CoreLevel = 0.55f;
        public float SheathLevel = 0.2f;
        public float StrikeSeconds = 0.45f;
        public float FlashSize = 8f;
        public float RingRadius = 8f;
        public float PulseGain = 0.45f;
        public float LightHeight = 3f;
        public float BaseGlowSize = 4.5f;
        public float BaseGlowLevel = 0.45f;
        public float BaseGlowHeight = 1.4f;

        internal const float KeepAliveTimeout = 0.35f;
        internal const float FlashSeconds = 0.55f;
        internal const float RingSeconds = 0.8f;
        internal const float PulseRise = 0.06f;
        internal const float PulseDecay = 0.24f;
        internal const float ImpactRise = 0.04f;
        internal const float ImpactDecay = 0.35f;
        private const float MinLevel = 0.002f;

        private static readonly Vec3 CoreTint = new Vec3(1f, 0.97f, 0.9f);
        private static readonly Vec3 SheathTint = new Vec3(1f, 0.86f, 0.62f);
        private static readonly Vec3 FlashTint = new Vec3(1f, 0.98f, 0.94f);
        private static readonly Vec3 RingTint = new Vec3(1f, 0.92f, 0.75f);

        private sealed class Part
        {
            public SceneEntity Entity;
            public bool Visible = true;
            public float MinZ;
            public float Width = 1f;
            public float Height = 1f;
        }

        private SceneEntity _root;
        private Part _core, _sheath, _flash, _ring;
        private ParticleSystem _motes;
        private Light _light;
        private float _lightBase;

        private readonly List<float> _pulseTimes = new();
        private int _nextPulse;
        private float _lastPulse = float.NegativeInfinity;

        private float _t;
        private float _sinceAlive;
        private bool _alive;
        private bool _releaseRequested;
        private float _fadeStart = -1f;
        private bool _finished;

        internal bool Configure(SceneEntity root, float duration, float tickInterval, float lifeAtSpawn)
        {
            if (root == null) return false;
            _root = root;
            foreach (var child in root.GetChildren())
            {
                switch (child.Name)
                {
                    case "pillar_core": _core = MakePart(child); break;
                    case "pillar_sheath": _sheath = MakePart(child); break;
                    case "pillar_flash": _flash = MakePart(child); break;
                    case "pillar_ring": _ring = MakePart(child); break;
                    case "pillar_motes":
                        if (child.GetComponentCount(SceneEntity.ComponentType.ParticleSystemInstanced) > 0)
                        {
                            _motes = child.GetComponentAtIndex(0, SceneEntity.ComponentType.ParticleSystemInstanced) as ParticleSystem;
                            _motes?.SetDontRemoveFromEntity(true);
                        }
                        break;
                }
            }
            if (_core == null) return false;

            // Brighten the beam on each damage tick.
            if (tickInterval > 0.05f && IsFinite(duration) && IsFinite(lifeAtSpawn))
            {
                for (int k = 1; k <= 64 && k * tickInterval <= duration; k++)
                    _pulseTimes.Add(k * tickInterval - lifeAtSpawn);
            }

            Apply(0f);
            return true;
        }

        internal void SetLight(Light light)
        {
            _light = light;
            _lightBase = light.Intensity;
            light.Intensity = 0f;
        }

        internal void KeepAlive() => _alive = true;

        internal void Release() => _releaseRequested = true;

        public override TickRequirement GetTickRequirement() =>
            _root != null && !_finished ? TickRequirement.Tick : TickRequirement.None;

        protected override void OnInit()
        {
            base.OnInit();
            SetScriptComponentToTick(GetTickRequirement());
        }

        protected override void OnTick(float dt)
        {
            if (_finished || _root == null || !(dt > 0f) || float.IsInfinity(dt)) return;
            try
            {
                _t += dt;
                if (_fadeStart < 0f)
                {
                    if (_alive)
                    {
                        _sinceAlive = 0f;
                        _alive = false;
                    }
                    else
                    {
                        _sinceAlive += dt;
                    }
                    if (_releaseRequested || _sinceAlive > KeepAliveTimeout) _fadeStart = Math.Max(_t, StrikeSeconds);
                }

                while (_nextPulse < _pulseTimes.Count && _t >= _pulseTimes[_nextPulse])
                {
                    float at = _pulseTimes[_nextPulse++];
                    if (_fadeStart < 0f || at <= _fadeStart + 0.1f) _lastPulse = at;
                }

                Apply(_t);
                if (_fadeStart >= 0f && _t >= _fadeStart + FadeSeconds) Finish();
            }
            catch (Exception e)
            {
                TORCommon.Log("RadiancePillarColumn stopped: " + e.Message, NLog.LogLevel.Warn);
                Finish();
            }
        }

        private void Apply(float t)
        {
            float strikeSeconds = Math.Max(0.01f, StrikeSeconds);
            float reach = StrikeReach(t / strikeSeconds);
            float landed = t - strikeSeconds;
            float fade = _fadeStart < 0f ? 1f : FadeLevel((t - _fadeStart) / Math.Max(0.1f, FadeSeconds));
            float impact = Envelope(landed, ImpactRise, ImpactDecay);
            float pulse = Envelope(t - _lastPulse, PulseRise, PulseDecay);
            float breath = 1f + 0.04f * MathF.Sin(t * 4.4f);

            float top = Height;
            float bottom = top - (top + GroundSink) * reach;
            float width = (0.55f + 0.45f * reach * reach) * (1f + 0.5f * impact + 0.15f * pulse) * (0.8f + 0.2f * fade) * breath;
            float level = fade * (1f + 0.35f * impact + PulseGain * pulse);
            float sheathIn = Clamp01(landed / 0.25f);

            PlaceColumn(_core, CoreWidth * width, bottom, top, CoreTint, CoreLevel * level);
            PlaceColumn(_sheath, SheathWidth * width, bottom, top, SheathTint, SheathLevel * level * sheathIn);

            if (landed >= 0f)
            {
                float flashP = landed / FlashSeconds;
                float burst = flashP < 1f ? Square(1f - flashP) : 0f;
                float grow = 1f - Square(1f - Clamp01(flashP / 0.15f));
                float size = BaseGlowSize + (FlashSize * (0.35f + 0.65f * grow) - BaseGlowSize) * burst;
                float baseLevel = BaseGlowLevel * fade * (1f + 0.6f * pulse) * Clamp01(landed / 0.3f);
                PlaceQuad(_flash, size, BaseGlowHeight, FlashTint, Math.Max(burst, baseLevel));
            }
            else
            {
                Hide(_flash);
            }

            float ringP = landed / RingSeconds;
            if (ringP >= 0f && ringP < 1f)
            {
                float grow = 1f - Cube(1f - ringP);
                PlaceQuad(_ring, 2f * RingRadius * (0.15f + 0.85f * grow), 0.1f, RingTint, 0.85f * (1f - ringP) * MathF.Sqrt(1f - ringP));
            }
            else
            {
                Hide(_ring);
            }

            _motes?.SetRuntimeEmissionRateMultiplier(landed < 0f ? 0f : (1f + 2.5f * impact + 1.5f * pulse) * fade * fade);

            if (_light != null)
                _light.Intensity = _lightBase * (landed < 0f ? 0.3f * reach : 1f) * fade * (1f + 0.8f * impact + 0.5f * pulse);
        }

        private void Finish()
        {
            if (_finished) return;
            _finished = true;
            SetScriptComponentToTick(TickRequirement.None);
            try
            {
                _motes?.SetRuntimeEmissionRateMultiplier(0f);
                if (_light != null) _light.Intensity = 0f;
                _root?.FadeOut(0.3f, true);
            }
            catch (Exception e)
            {
                TORCommon.Log("RadiancePillarColumn cleanup: " + e.Message, NLog.LogLevel.Warn);
            }
        }

        protected override void OnRemoved(int removeReason)
        {
            _finished = true;
            _root = null;
            _core = _sheath = _flash = _ring = null;
            _motes = null;
            _light = null;
            base.OnRemoved(removeReason);
        }

        private static Part MakePart(SceneEntity entity)
        {
            var part = new Part { Entity = entity };
            if (entity.MultiMeshComponentCount <= 0) return part;
            var mesh = entity.GetMetaMesh(0);
            if (mesh == null) return part;
            var box = mesh.GetBoundingBox();
            float across = Math.Max(box.max.x - box.min.x, box.max.y - box.min.y);
            float tall = box.max.z - box.min.z;
            if (IsFinite(across) && across > 0.0001f) part.Width = across;
            if (IsFinite(tall) && tall > 0.0001f)
            {
                part.Height = tall;
                part.MinZ = box.min.z;
            }
            return part;
        }

        private static void PlaceColumn(Part part, float width, float bottom, float top, Vec3 tint, float level)
        {
            if (part == null) return;
            float height = top - bottom;
            if (height < 0.02f || width < 0.01f || level <= MinLevel)
            {
                Hide(part);
                return;
            }
            float sxy = width / part.Width;
            float sz = height / part.Height;
            var frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, bottom - part.MinZ * sz));
            frame.Scale(new Vec3(sxy, sxy, sz));
            part.Entity.SetFrame(ref frame);
            Show(part, tint, level);
        }

        private static void PlaceQuad(Part part, float size, float z, Vec3 tint, float level)
        {
            if (part == null) return;
            if (size < 0.01f || level <= MinLevel)
            {
                Hide(part);
                return;
            }
            float s = size / part.Width;
            var frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, z));
            frame.Scale(new Vec3(s, s, 1f));
            part.Entity.SetFrame(ref frame);
            Show(part, tint, level);
        }

        private static void Show(Part part, Vec3 tint, float level)
        {
            if (!part.Visible)
            {
                part.Entity.SetVisibilityExcludeParents(true);
                part.Visible = true;
            }
            part.Entity.SetFactorColor(ToColor(tint * Math.Min(1f, level)));
        }

        private static void Hide(Part part)
        {
            if (part == null || !part.Visible) return;
            part.Entity.SetVisibilityExcludeParents(false);
            part.Visible = false;
        }


        internal static float StrikeReach(float p) => p <= 0f ? 0f : p >= 1f ? 1f : p * p;

        internal static float FadeLevel(float u)
        {
            if (u <= 0f) return 1f;
            if (u >= 1f) return 0f;
            return 1f - u * u * (3f - 2f * u);
        }

        internal static float Envelope(float since, float rise, float decay)
        {
            if (!(since >= 0f) || float.IsInfinity(since)) return 0f;
            if (since < rise) return since / rise;
            return (float)Math.Exp(-(since - rise) / decay);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Square(float v) => v * v;

        private static float Cube(float v) => v * v * v;

        private static uint ToColor(Vec3 rgb) => new Color(Clamp01(rgb.x), Clamp01(rgb.y), Clamp01(rgb.z), 1f).ToUnsignedInteger();

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
