using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

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

    /// <summary>
    /// Gets all kingdoms that have a trade agreement with this kingdom.
    /// </summary>
    public static IEnumerable<Kingdom> GetTradeAgreementKingdoms(this Kingdom kingdom)
    {
        var tradeAgreementBehavior = Campaign.Current?.GetCampaignBehavior<TradeAgreementsCampaignBehavior>();
        if (tradeAgreementBehavior == null)
            return Enumerable.Empty<Kingdom>();

        return Kingdom.All
            .Where(k => k != kingdom && !k.IsEliminated)
            .Where(k => tradeAgreementBehavior.HasTradeAgreement(kingdom, k));
    }

    /// <summary>
    /// Gets the count of trade agreements for this kingdom.
    /// </summary>
    public static int GetTradeAgreementCount(this Kingdom kingdom)
    {
        return kingdom.GetTradeAgreementKingdoms().Count();
    }

    /// <summary>
    /// Checks if this kingdom has a trade agreement with another kingdom.
    /// </summary>
    public static bool HasTradeAgreementWith(this Kingdom kingdom, Kingdom otherKingdom)
    {
        var tradeAgreementBehavior = Campaign.Current?.GetCampaignBehavior<TradeAgreementsCampaignBehavior>();
        if (tradeAgreementBehavior == null)
            return false;

        return tradeAgreementBehavior.HasTradeAgreement(kingdom, otherKingdom);
    }
}