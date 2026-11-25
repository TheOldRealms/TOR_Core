using TaleWorlds.Engine;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Preferred formation class for an ambush spawn point. Values align with FormationClass.
    /// </summary>
    public enum AmbushSpotPreferredClass
    {
        /// <summary>
        /// Wildcard; any class may claim this spot.
        /// </summary>
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
    /// Scene-level knobs for a single ambush spawn-point entity.
    /// </summary>
    public class AmbushSpawnPointScript : ScriptComponentBehavior
    {
        /// <summary>
        /// Maximum formations assigned to this spot. Zero means unlimited.
        /// </summary>
        public int MaxFormations = 0;

        /// <summary>
        /// Max formation width in metres before the layout line-wraps. Zero means unlimited.
        /// </summary>
        public float MaxWidth = 0f;

        /// <summary>
        /// Preferred FormationClass; matching classes land here first.
        /// </summary>
        public AmbushSpotPreferredClass PreferredClass = AmbushSpotPreferredClass.None;
    }
}
