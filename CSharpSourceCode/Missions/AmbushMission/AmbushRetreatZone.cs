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

        // Semi-transparent green: 0xAARRGGBB. Alpha 0x60 (~37%), full green.
        private const uint PlayerZoneColor = 0x60_00_FF_00u;

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
            if (ShowRuntimeMarker || ShowDebugMarkerInGame) RenderZone();
        }

        protected override void OnEditorTick(float dt)
        {
            base.OnEditorTick(dt);
            // Always visible in the editor so the scener sees the actual Radius while placing.
            RenderZone();
        }

        private void RenderZone()
        {
            if (Scene == null) return;
            // Flat terrain-hugging circle instead of a 3D debug sphere.
            // DebugExtensions.RenderDebugCircleOnTerrain is what vanilla
            // uses to draw area outlines on the ground (see any
            // scripted mission with a defendable region).
            DebugExtensions.RenderDebugCircleOnTerrain(
                Scene, GameEntity.GetGlobalFrame(), Radius, PlayerZoneColor,
                depthCheck: false, isDotted: false);
        }

        protected override void OnRemoved(int removeReason)
        {
            _activeZones.Remove(this);
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
