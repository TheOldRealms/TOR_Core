using System.Linq;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.Extensions;

public static class KingdomExtension
{
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