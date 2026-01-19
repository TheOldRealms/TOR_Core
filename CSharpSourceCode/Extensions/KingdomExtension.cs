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
        
        string[] coastalKingdoms = { "nordland", "ostland", "wasteland", "couronne", "anguille", "lyonesse", "mousillon", "bordeleaux", "brionne" };


        return coastalKingdoms.Any(id => kingdom.StringId == id);
    }

    public static bool IsCastleFaction(this Kingdom kingdom)
    {
        return kingdom.RulingClan.IsCastleFaction();
    }
    
    public static IEnumerable<Kingdom> GetTradeAgreementKingdoms(this Kingdom kingdom)
    {
        var tradeAgreementBehavior = Campaign.Current?.GetCampaignBehavior<TradeAgreementsCampaignBehavior>();
        if (tradeAgreementBehavior == null)
            return Enumerable.Empty<Kingdom>();

        return Kingdom.All
            .Where(k => k != kingdom && !k.IsEliminated)
            .Where(k => tradeAgreementBehavior.HasTradeAgreement(kingdom, k));
    }
    
    public static int GetTradeAgreementCount(this Kingdom kingdom)
    {
        return kingdom.GetTradeAgreementKingdoms().Count();
    }
    
    public static bool HasTradeAgreementWith(this Kingdom kingdom, Kingdom otherKingdom)
    {
        var tradeAgreementBehavior = Campaign.Current?.GetCampaignBehavior<TradeAgreementsCampaignBehavior>();
        if (tradeAgreementBehavior == null)
            return false;

        return tradeAgreementBehavior.HasTradeAgreement(kingdom, otherKingdom);
    }
    
    public static IEnumerable<Kingdom> GetEnemyKingdoms(this Kingdom kingdom)
    {
        if (kingdom == null)
            return Enumerable.Empty<Kingdom>();

        return Kingdom.All
            .Where(k => k != kingdom && !k.IsEliminated && kingdom.IsAtWarWith(k));
    }
    
    public static float GetTotalEnemyStrength(this Kingdom kingdom)
    {
        return kingdom.GetEnemyKingdoms().Sum(k => k.CurrentTotalStrength);
    }

    public static IEnumerable<Kingdom> GetAlliedKingdoms(this Kingdom kingdom)
    {
        if (kingdom == null)
            return Enumerable.Empty<Kingdom>();

        return Kingdom.All
            .Where(k => k != kingdom && !k.IsEliminated && kingdom.IsAlliedWith(k));
    }
    
    public static int GetAllianceCount(this Kingdom kingdom)
    {
        return kingdom.GetAlliedKingdoms().Count();
    }
}