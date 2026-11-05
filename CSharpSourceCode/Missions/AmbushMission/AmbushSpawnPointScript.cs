using TaleWorlds.Engine;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Preferred formation class for an ambush spawn point. Underlying
    /// values match <see cref="TaleWorlds.MountAndBlade.FormationClass"/>
    /// so the layout can compare against troops' class directly. Scene
    /// editor renders this as a dropdown because it's a named enum.
    /// </summary>
    public enum AmbushSpotPreferredClass
    {
        /// <summary>Wildcard; any class may claim this spot.</summary>
        None = -1,
        Infantry = 0,
        Ranged = 1,
        Cavalry = 2,
        HorseArcher = 3,
        Skirmisher = 4,
        HeavyInfantry = 5,
        LightCavalry = 6,
        HeavyCavalry = 7,
    }

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

        /// <summary>
        /// Preferred <see cref="TaleWorlds.MountAndBlade.FormationClass"/>
        /// at this spot. Used by the default-assignment pass and the
        /// Reset Deployment button so matching classes land here first.
        /// Scene editor shows this as a named dropdown
        /// (None / Infantry / Ranged / Cavalry / ...). Leave on
        /// <see cref="AmbushSpotPreferredClass.None"/> for a wildcard spot.
        /// </summary>
        public AmbushSpotPreferredClass PreferredClass = AmbushSpotPreferredClass.None;
    }
}
