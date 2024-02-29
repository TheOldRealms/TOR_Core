using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace TOR_Core.Extensions
{
    public static class KingdomExtension
    {
        public static MBReadOnlyList<Kingdom> AllActiveKingdoms => (MBReadOnlyList<Kingdom>)Kingdom.All.Where(k => !k.IsEliminated).ToList();

        public static bool IsAlliedWith(this IFaction faction1, IFaction faction2)
        {
            if (faction1 == faction2)
            {
                return false;
            }
            var stanceLink = faction1.GetStanceWith(faction2);
            return stanceLink.IsAllied;
        }

        public static IEnumerable<Kingdom> GetAlliedKingdoms(this Kingdom kingdom)
        {
            foreach (var stanceLink in kingdom.Stances)
            {
                if (stanceLink.IsAllied)
                {
                    IFaction alliedFaction = null;
                    if (stanceLink.Faction1 == kingdom)
                    {
                        alliedFaction = stanceLink.Faction2;
                    }
                    else if (stanceLink.Faction2 == kingdom)
                    {
                        alliedFaction = stanceLink.Faction1;
                    }
                    if (!(alliedFaction is null) && alliedFaction.IsKingdomFaction)
                    {
                        yield return (alliedFaction as Kingdom);
                    }
                }
            }
        }

        public static bool IsStrong(this Kingdom kingdom) => kingdom.TotalStrength > GetMedianStrength();

        public static float GetAllianceStrength(this Kingdom kingdom) => kingdom.GetAlliedKingdoms().Select(curKingdom => curKingdom.TotalStrength).Sum() + kingdom.TotalStrength;

        private static float GetMedianStrength()
        {
            float medianStrength;
            var kingdomStrengths = AllActiveKingdoms.Select(curKingdom => curKingdom.TotalStrength).OrderBy(a => a).ToArray();

            var halfIndex = kingdomStrengths.Length / 2;

            if ((kingdomStrengths.Length % 2) == 0)
            {
                medianStrength = (kingdomStrengths.ElementAt(halfIndex) + kingdomStrengths.ElementAt(halfIndex - 1)) / 2;
            }
            else
            {
                medianStrength = kingdomStrengths.ElementAt(halfIndex);
            }
            return medianStrength;
        }
    }
}
