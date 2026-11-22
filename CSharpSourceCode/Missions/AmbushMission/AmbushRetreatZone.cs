using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Scene-authored retreat zone. Scener attaches the script; entity position is the zone centre.
    /// </summary>
    public class AmbushRetreatZone : ScriptComponentBehavior
    {
        /// <summary>
        /// Zone inner radius for the hero. Scener-tunable.
        /// </summary>
        public float Radius = 10f;

        /// <summary>
        /// Multiplier on Radius for the forgiving troop-extraction radius. Default 1.5.
        /// </summary>
        public float TroopExtractionRadiusMultiplier = 1.5f;

        /// <summary>
        /// Draw ground markers during the mission (debug aid).
        /// </summary>
        public bool ShowDebugMarkerInGame = false;

        /// <summary>
        /// Flipped true by the controller when Phase 2 begins; reset on teardown.
        /// </summary>
        public static bool ShowRuntimeMarker { get; set; }

        // Opaque green ARGB; material thins it to an outline.
        private const uint PlayerZoneColor = 0xFF_00_C8_32u;
        // Vanilla hover-ring material.
        private const string RingMaterial = "map_circle_decal";
        // Natural mesh radius; scale factor = Radius / BaseMeshRadius.
        private const float BaseMeshRadius = 1f;

        private TaleWorlds.Engine.GameEntity _decalEntity;
        private Decal _decal;

        private static readonly List<AmbushRetreatZone> _activeZones = new List<AmbushRetreatZone>();

        /// <summary>
        /// Zones currently alive in the running mission's scene.
        /// </summary>
        public static IReadOnlyList<AmbushRetreatZone> ActiveZones => _activeZones;

        /// <summary>
        /// Purge stale references from any prior scene.
        /// </summary>
        public static void ResetRegistry()
        {
            _activeZones.Clear();
            ProceduralFallbackPosition = null;
            ShowRuntimeMarker = false;
        }

        /// <summary>
        /// Procedural fallback position when a scene has no authored zones.
        /// </summary>
        public static Vec3? ProceduralFallbackPosition { get; set; }

        /// <summary>
        /// Radius for the procedural fallback.
        /// </summary>
        public static float ProceduralFallbackRadius { get; set; } = 15f;

        /// <summary>
        /// True if the agent is inside any authored zone or the procedural fallback.
        /// </summary>
        public static bool IsAgentInsideAnyZone(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return false;
            for (int i = 0; i < _activeZones.Count; i++)
            {
                if (_activeZones[i].IsAgentInside(agent)) return true;
            }
            if (ProceduralFallbackPosition.HasValue)
            {
                var pos = ProceduralFallbackPosition.Value;
                var dx = agent.Position.x - pos.x;
                var dy = agent.Position.y - pos.y;
                return (dx * dx + dy * dy) <= ProceduralFallbackRadius * ProceduralFallbackRadius;
            }
            return false;
        }

        /// <summary>
        /// True if the agent is inside the forgiving troop-extraction radius of any zone.
        /// </summary>
        public static bool IsAgentInsideAnyTroopExtractionZone(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return false;
            for (int i = 0; i < _activeZones.Count; i++)
            {
                if (_activeZones[i].IsAgentInsideTroopExtractionZone(agent)) return true;
            }
            if (ProceduralFallbackPosition.HasValue)
            {
                var pos = ProceduralFallbackPosition.Value;
                var dx = agent.Position.x - pos.x;
                var dy = agent.Position.y - pos.y;
                // Fallback uses the per-zone default multiplier.
                const float fallbackMult = 1.5f;
                float r = ProceduralFallbackRadius * fallbackMult;
                return (dx * dx + dy * dy) <= r * r;
            }
            return false;
        }

        protected override void OnInit()
        {
            base.OnInit();
            if (!_activeZones.Contains(this))
                _activeZones.Add(this);
            SetScriptComponentToTick(GetTickRequirement());
        }

        public override TickRequirement GetTickRequirement()
            => base.GetTickRequirement() | TickRequirement.Tick;

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            UpdateDecal(ShowRuntimeMarker || ShowDebugMarkerInGame);
        }

        protected override void OnEditorTick(float dt)
        {
            base.OnEditorTick(dt);
            // Always visible in the editor.
            UpdateDecal(true);
        }

        /// <summary>
        /// Update the ground ring position and tint per tick.
        /// </summary>
        private void UpdateDecal(bool visible)
        {
            if (Scene == null) return;
            EnsureDecalCreated();
            if (_decalEntity == null) return;

            _decalEntity.SetVisibilityExcludeParents(visible);
            if (!visible) return;

            var frame = GameEntity.GetGlobalFrame();
            var scaleVec = Vec3.One * (Radius / BaseMeshRadius);
            frame.Scale(ref scaleVec);
            _decalEntity.SetGlobalFrame(frame);

            // Thin glowing outline profile, same as the campaign strike-range ring.
            _decal.SetFactor1Linear(PlayerZoneColor);
            _decal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
        }

        /// <summary>
        /// Lazily spawn the retreat-zone ring decal entity in the scene.
        /// </summary>
        private void EnsureDecalCreated()
        {
            if (_decalEntity != null) return;
            // Not editor-modifiable so the editor doesn't bake the ring into the scene file.
            _decalEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(Scene,
                isModifiableFromEditor: false, createPhysics: false, callScriptCallbacks: true);
            _decalEntity.Name = "TOR_AmbushRetreatZoneRing";
            _decal = Decal.CreateDecal(null);
            var material = Material.GetFromResource(RingMaterial);
            if (material != null) _decal.SetMaterial(material);
            Scene.AddDecalInstance(_decal, "editor_set", false);
            _decalEntity.AddComponent(_decal);
            _decalEntity.SetVisibilityExcludeParents(false);
        }

        protected override void OnRemoved(int removeReason)
        {
            _activeZones.Remove(this);
            if (_decalEntity != null)
            {
                _decalEntity.Remove(0);
                _decalEntity = null;
            }
            _decal = null;
            base.OnRemoved(removeReason);
        }

        /// <summary>
        /// World position of the zone centre; reads per-call so moving entities work.
        /// </summary>
        public Vec3 Position => GameEntity.GlobalPosition;

        /// <summary>
        /// True if the agent is inside the inner Radius.
        /// </summary>
        public bool IsAgentInside(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return false;
            // Horizontal distance only; zone is a cylinder.
            var zonePos = GameEntity.GlobalPosition;
            var dx = agent.Position.x - zonePos.x;
            var dy = agent.Position.y - zonePos.y;
            return (dx * dx + dy * dy) <= Radius * Radius;
        }

        /// <summary>
        /// True if the agent is inside the troop-extraction radius.
        /// </summary>
        public bool IsAgentInsideTroopExtractionZone(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return false;
            var zonePos = GameEntity.GlobalPosition;
            var dx = agent.Position.x - zonePos.x;
            var dy = agent.Position.y - zonePos.y;
            float r = Radius * TroopExtractionRadiusMultiplier;
            return (dx * dx + dy * dy) <= r * r;
        }
    }
}
