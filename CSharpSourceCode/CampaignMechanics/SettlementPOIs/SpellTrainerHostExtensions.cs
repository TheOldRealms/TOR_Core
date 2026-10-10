using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public static class SpellTrainerHostExtensions
    {
        public static Hero GetOrCreateTrainer(this ISpellTrainerHost host, Settlement s)
        {
            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            return mgr?.GetOrCreateHero(s, host.SpellTrainerTemplateId);
        }

        public static void EnsurePlaced(this ISpellTrainerHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi) || !poi.AppliesTo(s)) return;
            var hero = host.GetOrCreateTrainer(s);
            if (hero == null) return;
            if (hero.PartyBelongedTo != null) return;   // off fighting / caravan / etc.
            var location = s.LocationComplex.GetLocationWithId(poi.LocationId);
            TrainerPlacement.EnsureAt(s, hero, location);
        }

        public static bool IsSpellTrainerHero(this Hero h, ISpellTrainerHost host)
        {
            return h?.Template?.StringId != null
                   && host?.SpellTrainerTemplateId != null
                   && h.Template.StringId == host.SpellTrainerTemplateId;
        }
    }
}
