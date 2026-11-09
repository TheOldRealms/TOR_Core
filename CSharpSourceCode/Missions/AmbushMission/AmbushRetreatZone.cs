using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Scene-authored retreat zone for the ambush mission. The scener attaches this
    /// script to a GameEntity in the scene; the entity's world position becomes the
    /// zone centre. Any mesh / decal parented to that entity is the player-facing
    /// visual (glow / aura / banner — scener's choice).
    ///
    /// At runtime, <see cref="AmbushEndLogic"/> queries <see cref="ActiveZones"/> to
    /// find all zones in the current mission. On a successful ambush extraction the
    /// player must move the main agent inside <see cref="Radius"/> metres of a zone
    /// before the pressure clock elapses.
    ///
    /// Scenes without any <c>AmbushRetreatZone</c> fall back to a procedural zone
    /// placed by <see cref="AmbushMission"/> code at mission start (map edge opposite
    /// the enemy spawn). So scene authoring is NOT blocking Phase D ship.
    /// </summary>
    public class AmbushRetreatZone : ScriptComponentBehavior
    {
        /// <summary>
        /// Distance (metres) from the zone centre at which the player's main agent
        /// is considered "in the zone." Scener-tunable per instance.
        /// </summary>
        public float Radius = 10f;

        /// <summary>
        /// Non-hero troops only need to be within <c>Radius * this</c>
        /// metres of the zone to count as extracted. Forgives
        /// stragglers who couldn't quite keep up with the hero into
        /// the tight inner circle. Scener-tunable per instance;
        /// default 1.5 means a 10m player zone forgives troops out
        /// to 15m.
        /// </summary>
        public float TroopExtractionRadiusMultiplier = 1.5f;

        /// <summary>
        /// If true, also draw the ground markers during a running mission (not just in
        /// the editor). Useful for playtesting zone placement before dedicated art
        /// is parented. Default off so shipped scenes don't accidentally leak the
        /// debug visual into normal play. The Phase-2 runtime render is independent
        /// of this (gated by <see cref="ShowRuntimeMarker"/>).
        /// </summary>
        public bool ShowDebugMarkerInGame = false;

        /// <summary>
        /// Set true by <see cref="AmbushMissionController"/> when Phase 2 begins so
        /// the retreat zone visuals light up on the ground. Flipped off on mission
        /// teardown via <see cref="ResetRegistry"/>. Zones in a scene without an
        /// ambush controller (test scenes, editor preview) stay dark unless
        /// <see cref="ShowDebugMarkerInGame"/> is set.
        /// </summary>
        public static bool ShowRuntimeMarker { get; set; }

        // ARGB tint fed to the map_circle_decal shader via
        // SetFactor1Linear. Fully opaque green — the material's own
        // ring shader thins it to a soft outline so an opaque tint
        // still reads as a glowing ring, not a solid circle.
        private const uint PlayerZoneColor = 0xFF_00_C8_32u;
        // Vanilla hover-ring material. Same one AmbushRangeDebugMapView
        // uses on the campaign map.
        private const string RingMaterial = "map_circle_decal";
        // Natural decal-mesh radius (vanilla uses ~1 for party hover
        // rings). Our scale factor = Radius / BaseMeshRadius.
        private const float BaseMeshRadius = 1f;

        private TaleWorlds.Engine.GameEntity _decalEntity;
        private Decal _decal;

        private static readonly List<AmbushRetreatZone> _activeZones = new List<AmbushRetreatZone>();

        /// <summary>Zones currently alive in the running mission's scene.</summary>
        public static IReadOnlyList<AmbushRetreatZone> ActiveZones => _activeZones;

        /// <summary>Called by AmbushMissionController at mission-boot to purge any
        /// stale references left over from a prior scene (scripts don't always
        /// call OnRemoved on scene teardown).</summary>
        public static void ResetRegistry()
        {
            _activeZones.Clear();
            ProceduralFallbackPosition = null;
            ShowRuntimeMarker = false;
        }

        /// <summary>Procedural fallback zone position set by
        /// AmbushMissionController when a scene has no scener-authored zones.
        /// Null = scene has authored zones (or the controller hasn't run yet).</summary>
        public static Vec3? ProceduralFallbackPosition { get; set; }

        /// <summary>Radius for the procedural fallback. Slightly generous
        /// vs the authored default because the player doesn't have a visual
        /// to home in on.</summary>
        public static float ProceduralFallbackRadius { get; set; } = 15f;

        /// <summary>Returns true if the agent is inside any authored zone OR
        /// inside the procedural fallback. One-stop query for AmbushEndLogic.</summary>
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

        /// <summary>Returns true if the agent is within the forgiving
        /// troop-extraction radius of any authored zone OR the
        /// procedural fallback (scaled by
        /// <see cref="TroopExtractionRadiusMultiplier"/>). Used for the
        /// "troops left behind" count so stragglers who couldn't quite
        /// keep up with the hero into the tight inner circle still
        /// count as extracted.</summary>
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
                // Procedural fallback has no scener-tunable multiplier;
                // reuse the per-zone default (1.5x) so the behaviour
                // matches when a scene has no authored zones.
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
            // Always visible in the editor so the scener sees the actual Radius while placing.
            UpdateDecal(true);
        }

        /// <summary>
        /// Per-tick visibility + position update for the ground ring.
        /// Mirrors the pattern <see cref="CampaignMechanics.Ambush.AmbushRangeDebugMapView"/>
        /// uses on the campaign map for the strike-range ring: one
        /// GameEntity + Decal created on first call, repositioned +
        /// re-tinted per tick, toggled via
        /// <see cref="GameEntity.SetVisibilityExcludeParents"/>.
        /// Shader uniforms reset between passes so the color /
        /// ring-thickness args must be re-set every frame.
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

            // Ring thickness profile matches AmbushRangeDebugMapView
            // (0.166, 1, 0.83, 0 — thin glowing outline).
            _decal.SetFactor1Linear(PlayerZoneColor);
            _decal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
        }

        private void EnsureDecalCreated()
        {
            if (_decalEntity != null) return;
            _decalEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(Scene, true, true, true);
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
        /// World position of the zone centre. Reads the GameEntity frame each call so
        /// moving the entity in-mission (e.g., a scener-placed moving target) works.
        /// </summary>
        public Vec3 Position => GameEntity.GlobalPosition;

        public bool IsAgentInside(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return false;
            // Horizontal distance only — zone is a cylinder, not a sphere; we don't
            // want vertical placement (hilltops, ramps) to accidentally exclude the
            // player.
            var zonePos = GameEntity.GlobalPosition;
            var dx = agent.Position.x - zonePos.x;
            var dy = agent.Position.y - zonePos.y;
            return (dx * dx + dy * dy) <= Radius * Radius;
        }

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
