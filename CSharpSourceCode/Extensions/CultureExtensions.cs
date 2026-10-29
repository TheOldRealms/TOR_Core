using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions;

public static class CultureExtensions
{
    public static bool IsEmpire(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.EMPIRE || culture.StringId == TORConstants.Cultures.EMPIRE_DESERTERS) return true;
        else return false;
    }

    public static bool IsGreenskin(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.GREENSKIN || culture.StringId == TORConstants.Cultures.GREENSKIN_BANDIT || culture.StringId == TORConstants.Cultures.GOBLIN_BANDIT) return true;
        else return false;
    }

    public static bool IsBretonnian(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.BRETONNIA || culture.StringId == TORConstants.Cultures.HERRIMAULT) return true;
        else return false;
    }

    public static bool IsUndead(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.SYLVANIA || culture.StringId == TORConstants.Cultures.MOUSILLON) return true;
        else return false;
    }

    public static bool IsElf(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.EONIR || culture.StringId == TORConstants.Cultures.ASRAI || culture.StringId == TORConstants.Cultures.DRUCHII) return true;
        else return false;
    }

    public static bool IsBeastmen(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.BEASTMEN || culture.StringId == TORConstants.Cultures.BEASTMEN_BANDIT) return true;
        else return false;
    }

    /// <remarks>
    /// Excludes f.e. Beastmen who are considered forces of chaos.
    /// </remarks>
    public static bool IsChaosStrict(this CultureObject culture)
    {
        if (culture.StringId == TORConstants.Cultures.CHAOS || culture.StringId == TORConstants.Cultures.CHAOS_CULTIST) return true;
        else return false;
    }
}
