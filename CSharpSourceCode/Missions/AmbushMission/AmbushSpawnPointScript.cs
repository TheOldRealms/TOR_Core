using TaleWorlds.Engine;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Scene-level knobs for a single ambush spawn-point entity. Attach to
    /// any <c>attacker_ambush_spawnpoint</c> game_entity; the ambush
    /// controller reads the script off the entity when it collects
    /// SceneSpots so the picker UI can cap how many formations stack
    /// on the same point and the per-spot layout can line-break troops
    /// that would otherwise spill past the walkable stretch.
    ///
    /// Zero means unlimited for both fields (sentinel; ScriptComponentBehavior
    /// fields are primitives so there is no real null). A scener who leaves
    /// the script at its defaults gets no caps — same behaviour as before
    /// the script existed.
    /// </summary>
    public class AmbushSpawnPointScript : ScriptComponentBehavior
    {
        /// <summary>
        /// Maximum number of formations that may be assigned to this spot
        /// at the same time. Zero = unlimited.
        /// </summary>
        public int MaxFormations = 0;

        /// <summary>
        /// Maximum formation width (in metres) before a per-spot troop
        /// layout line-wraps into additional ranks. Zero = unlimited.
        /// </summary>
        public float MaxWidth = 0f;
    }
}
