﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;

namespace TOR_Core.Extensions
{
    public static class FactionExtensions
    {
        public static int GetNumActiveKingdomWars(this IFaction faction)
        {
            var count = 0;
            foreach(var stance in faction.Stances)
            {
                if(stance.Faction1 is Kingdom && stance.Faction2 is Kingdom)
                {
                    if (stance.IsAtWar || stance.IsAtConstantWar) count++;
                }
            }
            return count;
        }

        public static float GetSumEnemyKingdomPower(this IFaction faction)
        {
            float sum = 0;
            foreach (var stance in faction.Stances)
            {
                if (stance.Faction1 is Kingdom && stance.Faction2 is Kingdom)
                {
                    if (stance.IsAtWar || stance.IsAtConstantWar) sum += stance.Faction1 == faction ? stance.Faction2.TotalStrength : stance.Faction1.TotalStrength;
                }
            }
            return sum;
        }
    }
}