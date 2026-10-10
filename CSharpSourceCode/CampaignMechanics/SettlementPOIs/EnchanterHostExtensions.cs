using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public static class EnchanterHostExtensions
    {
        public static Hero GetOrCreateEnchanter(this IEnchanterHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi)) return null;
            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            return mgr?.GetOrCreateHero(s, poi.LocationId, host.EnchanterTemplateId);
        }

        public static void EnsurePlaced(this IEnchanterHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi) || !poi.AppliesTo(s)) return;
            var hero = host.GetOrCreateEnchanter(s);
            if (hero == null) return;
            if (!s.HeroesWithoutParty.Contains(hero)) return;
            var location = s.LocationComplex.GetLocationWithId(poi.LocationId);
            TrainerPlacement.EnsureAt(s, hero, location);
        }

        public static bool IsEnchanterHero(this Hero h, IEnchanterHost host)
        {
            return h?.Template?.StringId != null
                   && host?.EnchanterTemplateId != null
                   && h.Template.StringId == host.EnchanterTemplateId;
        }
    }
}
