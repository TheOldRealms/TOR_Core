using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public static class SkillTrainerHostExtensions
    {
        public static Hero GetOrCreateSkillTrainer(this ISkillTrainerHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi)) return null;
            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            return mgr?.GetOrCreateHero(s, poi.LocationId, host.SkillTrainerTemplateId);
        }

        public static void EnsurePlaced(this ISkillTrainerHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi) || !poi.AppliesTo(s)) return;
            var hero = host.GetOrCreateSkillTrainer(s);
            if (hero == null) return;
            if (!s.HeroesWithoutParty.Contains(hero)) return;
            var location = s.LocationComplex.GetLocationWithId(poi.LocationId);
            TrainerPlacement.EnsureAt(s, hero, location);
        }

        public static bool IsSkillTrainerHero(this Hero h, ISkillTrainerHost host)
        {
            return h?.Template?.StringId != null
                   && host?.SkillTrainerTemplateId != null
                   && h.Template.StringId == host.SkillTrainerTemplateId;
        }
    }
}
