namespace TOR_Core.CampaignMechanics.WaaaghMeter;

public static class WaaaghHelper
{
    public const float MaxWaaagh = 1000f;

    // Thresholds as absolute values
    public const float InternalFightinThreshold = 0f;
    public const float PettySquabblinThreshold = 250f;
    public const float EreWeGoThreshold = 600f;
    public const float WaaaghThreshold = 900f;

    // Thresholds as percentages (0-1)
    public static float GetThresholdPercentage(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => InternalFightinThreshold / MaxWaaagh,
            WaaaghLevel.PettySquabblin => PettySquabblinThreshold / MaxWaaagh,
            WaaaghLevel.EreWeGo => EreWeGoThreshold / MaxWaaagh,
            WaaaghLevel.WAAAGH => WaaaghThreshold / MaxWaaagh,
            _ => 0f
        };
    }

    public static WaaaghLevel GetWaaaghLevelForResource(float level)
    {
        return level switch
        {
            < PettySquabblinThreshold => WaaaghLevel.InternalFightin,
            < EreWeGoThreshold => WaaaghLevel.PettySquabblin,
            < WaaaghThreshold => WaaaghLevel.EreWeGo,
            _ => WaaaghLevel.WAAAGH
        };
    }

    public static float GetResourceMinimumForWaaaghRank(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.WAAAGH => WaaaghThreshold,
            WaaaghLevel.EreWeGo => EreWeGoThreshold,
            WaaaghLevel.PettySquabblin => PettySquabblinThreshold,
            WaaaghLevel.InternalFightin => InternalFightinThreshold,
            _ => 0
        };
    }

    public static string GetLevelName(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => "Internal Fightin'",
            WaaaghLevel.PettySquabblin => "Petty Squabblin'",
            WaaaghLevel.EreWeGo => "'Ere We Go!",
            WaaaghLevel.WAAAGH => "WAAAGH!!!!",
            _ => "Unknown"
        };
    }

    public static string GetLevelDescription(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => "Da Boys uv da mob are demoralized. They 'ave no gits to focus on an' resort to fightin' each other.",
            WaaaghLevel.PettySquabblin => "Da mob found sum gits to bash but smaller scraps are still occurin' among da tribe. Da Boys will soon start gettin' restless again.",
            WaaaghLevel.EreWeGo => "Da recent exploits uv your mob 'ave been 'eard in other tribes as well. Greenskins from other tribes start gatherin', an' your Boys are preparin' fer a proppa big scrap.",
            WaaaghLevel.WAAAGH => "Now da Boys are proppa eager an' killy! Wez gonna show all dem humies an' stunties an' all da uva gits too! DIS IZ WAAAAGH!!!",
            _ => ""
        };
    }

    public static string GetLevelEffects(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => "Morale: -40\nDamage Dealt: -20%\nFood Consumed: -60%",
            WaaaghLevel.PettySquabblin => "Morale: -20\nDamage Dealt: -10%\nFood Consumed: -30%\nDaily Wounded: Smaller chance",
            WaaaghLevel.EreWeGo => "Damage Dealt: +10%\nFood Consumed: +25%\nParty Size: +60\nDaily Recruitment: Small chance (T1-3)",
            WaaaghLevel.WAAAGH => "Damage Dealt: +20%\nFood Consumed: +100%\nParty Size: +120\nDaily Recruitment: Big chance (T1-3)",
            _ => ""
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
