namespace TOR_Core.Models.Ambush
{
    /// <summary>
    /// Qualitative outcome tier for an ambush strike. Used by the AI-vs-AI skirmish resolver.
    /// </summary>
    public enum AmbushOutcomeTier
    {
        /// <summary>
        /// Decisive hit. Victim takes 1.5x wounds, ambusher 0.5x.
        /// </summary>
        CriticalSuccess,

        /// <summary>
        /// Clean strike. Both sides take base-rate wounds.
        /// </summary>
        NormalSuccess,

        /// <summary>
        /// Spotted early. Half wounds both sides, ambusher usually retreats.
        /// </summary>
        MinorFail,

        /// <summary>
        /// Botched. Ambusher takes 1.5x wounds, victim largely untouched. AI-vs-AI only.
        /// </summary>
        TerribleFail,
    }
}
