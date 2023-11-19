using System.Collections.Generic;
using System.ComponentModel;

namespace TOR_Core.BattleMechanics.AI.InfluenceMap
{
    public class InfluenceMap
    {

        public InfluenceMap()
        {

        }


        public static InfluenceMap CreateStandardArtilleryPlacementMap()
        {
            return new InfluenceMap();
        }
    }

    public enum MapLayer
    {
        Height,
        DistanceToEnemy,
        LineOfSightToEnemy,
    }
}
