using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;

namespace TOR_Core.Extensions;

public static class ClanExtensions
{
    public static bool IsCastleFaction(this Clan clan)
    {
        if (clan.StringId.Contains("necrarch_clan") || clan.StringId.Contains("brasskeep_clan") || clan.StringId.Contains("blooddragons_clan"))
        {
            return true;
        }

        return false;
    }
}