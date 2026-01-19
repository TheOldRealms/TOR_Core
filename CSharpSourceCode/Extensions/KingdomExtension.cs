using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.Utilities;

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
    
    public static IEnumerable<Kingdom> GetAlliedKingdoms(this IFaction faction)
    {
        Kingdom kingdom = faction as Kingdom;
        return kingdom?.GetAlliedKingdoms();
    }

    public static IEnumerable<Kingdom> GetAlliedKingdoms(this Kingdom kingdom)
    {
        if (kingdom == null)
            return Enumerable.Empty<Kingdom>();

        return Kingdom.All
            .Where(k => k != kingdom && !k.IsEliminated && kingdom.IsAllyWith(k));
    }
    
    public static int GetAllianceCount(this Kingdom kingdom)
    {
        return kingdom.GetAlliedKingdoms().Count();
    }

    public static int GetWarCount(this Kingdom kingdom)
    {
        return kingdom.GetEnemyKingdoms().Count();
    }

    public static float GetAllianceTotalStrength(this Kingdom kingdom)
    {
        if (kingdom == null) return 0f;

        float totalStrength = kingdom.CurrentTotalStrength;
        foreach (var ally in kingdom.GetAlliedKingdoms())
        {
            totalStrength += ally.CurrentTotalStrength;
        }
        return totalStrength;
    }

    public static float GetTotalEnemyAllianceStrength(this Kingdom kingdom)
    {
        if (kingdom == null) return 0f;

        float sum = 0f;
        foreach (var enemy in kingdom.GetEnemyKingdoms())
        {
            sum += enemy.GetAllianceTotalStrength();
        }
        return sum;
    }

    public static void SetAlliance(this Kingdom kingdom1, Kingdom kingdom2)
    {
        if (kingdom1 == null || kingdom2 == null)
            return;

        var allianceBehavior = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
        allianceBehavior?.StartAlliance(kingdom1, kingdom2);
    }

    public static void SetAllianceClean(this Kingdom kingdom1, Kingdom kingdom2)
    {
        if (kingdom1 == null || kingdom2 == null)
            return;

        var enemies1 = kingdom1.GetEnemyKingdoms().ToList();
        var enemies2 = kingdom2.GetEnemyKingdoms().ToList();

        var allianceBehavior = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
        allianceBehavior?.StartAlliance(kingdom1, kingdom2);

        JoinAllyWars(kingdom1, kingdom2, enemies2);
        JoinAllyWars(kingdom2, kingdom1, enemies1);
    }

    private static void JoinAllyWars(Kingdom kingdom, Kingdom ally, List<Kingdom> allyEnemies)
    {
        var allianceWarBehavior = Campaign.Current?.GetCampaignBehavior<TORAllianceWarBehavior>();

        foreach (var enemy in allyEnemies)
        {
            if (kingdom.IsAtWarWith(enemy)) continue;
            if (enemy.Culture?.StringId == TORConstants.Cultures.CHAOS) continue;
            if (enemy == kingdom) continue;

            allianceWarBehavior?.MarkAsAllianceWar(kingdom, enemy);
            DeclareWarAction.ApplyByKingdomDecision(kingdom, enemy);
        }
    }
}