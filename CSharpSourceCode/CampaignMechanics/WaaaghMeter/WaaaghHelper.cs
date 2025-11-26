namespace TOR_Core.CampaignMechanics.WaaaghMeter;

public static class WaaaghHelper
{
    public static WaaaghLevel GetWaaaghLevelForResource(float level)
    {
        return level switch
        {
            < 250 => WaaaghLevel.InternalFightin,
            < 600 => WaaaghLevel.PettySquabblin,
            < 900 => WaaaghLevel.EreWeGo,
            _ => WaaaghLevel.WAAAGH
        };
    }

    public static float GetResourceMinimumForWaaaghRank(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.WAAAGH => 900,
            WaaaghLevel.EreWeGo => 600,
            WaaaghLevel.PettySquabblin => 250,
            WaaaghLevel.InternalFightin => 0,
            _ => 0
        };
    }
}

public enum WaaaghLevel
{
    WAAAGH = 3,
    EreWeGo = 2,
    PettySquabblin = 1,
    InternalFightin = 0
}