using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions;

public static class ClanExtensions
{
    public static bool IsCastleFaction(this Clan clan)
    {
        if (clan.StringId.Contains(TORConstants.Factions.NECRACHS) || clan.StringId.Contains(TORConstants.Factions.BRASSKEEP) || clan.StringId.Contains(TORConstants.Factions.BLOODDRAGONS) || clan.StringId.Contains(TORConstants.Factions.REAVAZ) || clan.StringId.Contains(TORConstants.Factions.BLACK_PIT))
        {
            return true;
        }

        return false;
    }
}