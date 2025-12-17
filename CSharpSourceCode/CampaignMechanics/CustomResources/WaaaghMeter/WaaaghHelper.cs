using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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

    public static TextObject GetLevelName(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => TORTextHelper.GetTextObject("tor_waaagh_level_name_internal_fightin", "Internal Fightin'"),
            WaaaghLevel.PettySquabblin => TORTextHelper.GetTextObject("tor_waaagh_level_name_petty_squabblin", "Petty Squabblin'"),
            WaaaghLevel.EreWeGo => TORTextHelper.GetTextObject("tor_waaagh_level_name_ere_we_go", "'Ere We Go!"),
            WaaaghLevel.WAAAGH => TORTextHelper.GetTextObject("tor_waaagh_level_name_waaagh", "WAAAGH!!!!"),
            _ => TORTextHelper.GetTextObject("tor_waaagh_level_name_unknown", "Unknown")
        };
    }

    public static TextObject GetLevelDescription(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => TORTextHelper.GetTextObject("tor_waaagh_level_desc_internal_fightin", "Da Boys uv da mob are demoralized. They 'ave no gits to focus on an' resort to fightin' each other."),
            WaaaghLevel.PettySquabblin => TORTextHelper.GetTextObject("tor_waaagh_level_desc_petty_squabblin", "Da mob found sum gits to bash but smaller scraps are still occurin' among da tribe. Da Boys will soon start gettin' restless again."),
            WaaaghLevel.EreWeGo => TORTextHelper.GetTextObject("tor_waaagh_level_desc_ere_we_go", "Da recent exploits uv your mob 'ave been 'eard in other tribes as well. Greenskins from other tribes start gatherin', an' your Boys are preparin' fer a proppa big scrap."),
            WaaaghLevel.WAAAGH => TORTextHelper.GetTextObject("tor_waaagh_level_desc_waaagh", "Now da Boys are proppa eager an' killy! Wez gonna show all dem humies an' stunties an' all da uva gits too! DIS IZ WAAAAGH!!!"),
            _ => TextObject.GetEmpty()
        };
    }

    public static TextObject GetLevelEffects(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.InternalFightin => TORTextHelper.GetTextObject("tor_waaagh_level_effects_internal_fightin", "Morale: -40{NEWLINE}Damage Dealt: -20%{NEWLINE}Food Consumed: -60%"),
            WaaaghLevel.PettySquabblin => TORTextHelper.GetTextObject("tor_waaagh_level_effects_petty_squabblin", "Morale: -20{NEWLINE}Damage Dealt: -10%{NEWLINE}Food Consumed: -30%{NEWLINE}Daily Wounded: Smaller chance"),
            WaaaghLevel.EreWeGo => TORTextHelper.GetTextObject("tor_waaagh_level_effects_ere_we_go", "Damage Dealt: +10%{NEWLINE}Food Consumed: +25%{NEWLINE}Party Size: +60{NEWLINE}Daily Recruitment: Small chance (T1-3)"),
            WaaaghLevel.WAAAGH => TORTextHelper.GetTextObject("tor_waaagh_level_effects_waaagh", "Damage Dealt: +20%{NEWLINE}Food Consumed: +100%{NEWLINE}Party Size: +120{NEWLINE}Daily Recruitment: Big chance (T1-3)"),
            _ => TextObject.GetEmpty()
        };
    }

    public static TextObject GetBarTooltip(float currentValue)
    {
        var text = TORTextHelper.GetTextObject("tor_waaagh_bar_tooltip", "Waaagh: {CURRENT} / {MAX}");
        text.SetTextVariable("CURRENT", (int)currentValue);
        text.SetTextVariable("MAX", (int)MaxWaaagh);
        return text;
    }
}

public enum WaaaghLevel
{
    WAAAGH = 3,
    EreWeGo = 2,
    PettySquabblin = 1,
    InternalFightin = 0
}
