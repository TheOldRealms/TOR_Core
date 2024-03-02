using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Aggression
{
    public static class TORAggressionCalculator
    {
        public static float TorAggressionScore(IFaction faction1, IFaction faction2)
        {
            /// Reverse engineered values to make calculations fall in line with native
            var nativeFactionScore = DecompiledNativeAggression.BaseFactionFactor(faction1, faction2);
            var nativeDistanceScore = DecompiledNativeAggression.BaseDistanceFactor(faction1, faction2);

            /// TOR internal score calculations
            var religionScore = ReligiousAggressionCalculator.CalculateReligionMultiplier(faction1, faction2);

            // apply faction score to both scores
            religionScore *= nativeFactionScore;
            var distanceScore = nativeFactionScore * nativeDistanceScore;

            // Logging
            //TORCommon.Say($"Aggression between {faction1.Name} vs {faction2.Name}; \n\t\t" +
            //    $"Faction Score: {nativeFactionScore}, Distance Score: {distanceScore}, Religion Score = {religionScore} ");

            // return aggregate score
            return religionScore - distanceScore;
        }


    }
}
