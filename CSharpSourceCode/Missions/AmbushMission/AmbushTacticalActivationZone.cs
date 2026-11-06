using TaleWorlds.Engine;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Scener-authored volume that gates when a spawn-point's tactical
    /// children are "live". Attach to the same game_entity that carries
    /// <see cref="AmbushSpawnPointScript"/>; our anchor logic only
    /// assigns archers to tactical children while the formation's
    /// plate position is inside this volume. Keeps the proximity
    /// decision scener-controlled rather than hard-coded, so the same
    /// scene can place tight pockets (hill tops) next to broad activation
    /// zones (forest clearings) without the Ranged formation getting
    /// confused when it crosses between them on street level.
    ///
    /// Vanilla analogue: siege scripts carry per-entity activation
    /// ranges so the AI only considers an entity "in play" when the
    /// relevant unit is close enough. This is the ambush equivalent.
    /// </summary>
    public class AmbushTacticalActivationZone : ScriptComponentBehavior
    {
        /// <summary>
        /// Volume radius in metres. The formation plate position must
        /// be inside this volume for the spawn-point's tactical
        /// children to be reachable. Zero disables the volume (nothing
        /// anchors, equivalent to the spawn-point having no tactical
        /// children). Scener-tunable in the scene editor.
        /// </summary>
        public float Radius = 30f;

        /// <summary>
        /// When true the volume is a flat circle on the XZ plane
        /// (height difference between plate and spawn-point is ignored).
        /// When false it's a 3D sphere; the plate being directly
        /// underneath an elevated spawn-point on street level will
        /// therefore read as "out of range" and archers won't try to
        /// claim hill-top tactical points from below.
        /// </summary>
        public bool IgnoreHeight = false;
    }
}
