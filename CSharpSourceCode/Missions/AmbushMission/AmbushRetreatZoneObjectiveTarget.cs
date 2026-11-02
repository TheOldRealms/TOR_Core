using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Objective-target wrapper around the retreat zone position so
    /// vanilla's <c>MissionObjectiveMarkersVM</c> can render a proper
    /// world-anchored marker (icon + name + distance) for the ambush
    /// extraction point.
    ///
    /// Added to the Phase 2 <see cref="AmbushObjective"/> once the
    /// initial patrol is defeated; removed / replaced on phase
    /// transitions. Pulls the live position from
    /// <see cref="AmbushRetreatZone.ActiveZones"/> or the procedural
    /// fallback — the engine re-calls <see cref="GetGlobalPosition"/>
    /// each tick so moving zones are tracked naturally.
    /// </summary>
    public class AmbushRetreatZoneObjectiveTarget : MissionObjectiveTarget
    {
        private readonly TextObject _name = new TextObject("{=tor_ambush_extraction_marker}Extraction");

        /// <summary>Set by <see cref="UI.AmbushExtractionOrderHandler"/>
        /// each tick based on cursor-vs-marker focus distance. Consumed
        /// by the VM extension on the objective marker so the overridden
        /// <c>MissionObjectiveMarkers.xml</c> prefab can render a hover
        /// highlight. Static because there's only ever one extraction
        /// target per mission and the handler is a MissionView that
        /// can't reach the VM through the Gauntlet layer without this
        /// backchannel.</summary>
        public static bool IsFocused { get; set; }

        public override bool IsActive() => TryGetPosition(out _);

        public override TextObject GetName() => _name;

        public override Vec3 GetGlobalPosition()
        {
            return TryGetPosition(out var pos) ? pos : Vec3.Zero;
        }

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
