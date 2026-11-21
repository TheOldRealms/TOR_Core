using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Objective-target wrapper on the retreat-zone position so vanilla renders a world-anchored marker.
    /// </summary>
    public class AmbushRetreatZoneObjectiveTarget : MissionObjectiveTarget
    {
        private readonly TextObject _name = new TextObject("{=tor_ambush_extraction_marker}Extraction");

        /// <summary>
        /// Hover-focus flag written by the MissionView and read by the marker VM extension.
        /// </summary>
        public static bool IsFocused { get; set; }

        public override bool IsActive() => TryGetPosition(out _);

        public override TextObject GetName() => _name;

        public override Vec3 GetGlobalPosition()
        {
            return TryGetPosition(out var pos) ? pos : Vec3.Zero;
        }

        /// <summary>
        /// Resolve the current retreat-zone position from an active zone or the procedural fallback.
        /// </summary>
        private static bool TryGetPosition(out Vec3 position)
        {
            var zones = AmbushRetreatZone.ActiveZones;
            if (zones.Count > 0 && zones[0] != null)
            {
                position = zones[0].Position;
                return true;
            }
            if (AmbushRetreatZone.ProceduralFallbackPosition.HasValue)
            {
                position = AmbushRetreatZone.ProceduralFallbackPosition.Value;
                return true;
            }
            position = Vec3.Zero;
            return false;
        }
    }
}
