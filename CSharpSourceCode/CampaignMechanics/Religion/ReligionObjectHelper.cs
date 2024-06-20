namespace TOR_Core.CampaignMechanics.Religion;

public static class ReligionObjectHelper
{
    public static float CalculateSimilarityScore(ReligionObject x, ReligionObject y)
    {
        if (x == null || y == null) return 0;
        return x.GetSimilarityScore(y);
    }
}