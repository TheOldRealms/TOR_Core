using TaleWorlds.CampaignSystem;

namespace TOR_Core.Models.Diplomacy.Aggression
{
    public static class TORAggressionCalculator
    {
        public static float TorAggressionScore(IFaction faction1, IFaction faction2)
        {
            /// Reverse engineered values to make calculations fall in line with native
            var nativeFactionScore = DecompiledNativeAggression.BaseFactionFactor(faction1, faction2);
            var nativeDistanceScore = DecompiledNativeAggression.BaseDistanceFactor(faction1, faction2);

            /// TOR internal score calculations
            var religionScore = ReligiousAggressionCalculator.DetermineEffectOfReligion(faction1, faction2);
            // normalize effect of religion based on average hero "agro" to approximately between 0.0 and 1.0
            religionScore = religionScore / ((faction2.Heroes.Count + faction1.Heroes.Count) * 100);

            //TORCommon.Say($"Aggression between {faction1.Name} vs {faction2.Name}; \n\t\t" +
            //    $"Faction Score: {factionScore}, Distance Score: {distanceScore}, Religion Score = {religionScore} ");

            // weigh religion as much as faction strength, distance is kinda between 0-1
            return (religionScore * nativeFactionScore) + (nativeDistanceScore * nativeFactionScore);
        }


    }
}
