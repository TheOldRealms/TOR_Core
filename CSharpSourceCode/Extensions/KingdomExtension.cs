using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.Extensions;

public static class KingdomExtension
{
    private static readonly ConditionalWeakTable<IFaction, KingdomAdditionalInfo> ExtraData = new();

    private class KingdomAdditionalInfo
    {
        public bool IsAllyTriggered = false;
    }

    public static bool IsAllyTriggered(this IFaction obj)
    {
        if (ExtraData.TryGetValue(obj, out var data))
        {
            return data.IsAllyTriggered;
        }
        return false;
    }

    public static void SetAllyTriggered(this IFaction obj, bool value)
    {
        var data = ExtraData.GetOrCreateValue(obj);
        data.IsAllyTriggered = value;
    }

    public static bool IsCoastalKingdom(this Kingdom kingdom)
    {

        //Nordland
        //Marienburg
        //Ostland
        //Mousillon
        //Lyonesse
        //Bordeleaux
        //Coronne
        //Brionne
        //Languille

        string[] coastalKingdoms = { "nordland", "ostland", "wasteland", "couronne", "anguille", "lyonesse", "mousillon", "bordeleaux", "brionne" };


        return coastalKingdoms.Any(id => kingdom.StringId == id);
    }

    public static bool IsCastleFaction(this Kingdom kingdom)
    {
        return kingdom.RulingClan.IsCastleFaction();
    }
}