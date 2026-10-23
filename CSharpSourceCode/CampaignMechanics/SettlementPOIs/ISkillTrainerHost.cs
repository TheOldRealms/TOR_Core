using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: hosts a skill-trainer hero (non-magic trainer, e.g. the
    /// Empire Engineer Hall master). The POI framework creates one hero per
    /// settlement using <see cref="SkillTrainerTemplateId"/>, places them at
    /// the POI's Location, and routes dialog triggers.
    /// </summary>
    public interface ISkillTrainerHost
    {
        /// <summary>Template id used to create / find this POI's trainer hero.</summary>
        string SkillTrainerTemplateId { get; }
    }

    /// <summary>
    /// Lifecycle helpers for <see cref="ISkillTrainerHost"/>: get-or-create the
    /// trainer hero, place them at the POI's Location, identify a trainer-hero by
    /// template id. Co-located with the interface so new host subclasses discover
    /// the full capability contract in one file.
    /// </summary>
    public static class SkillTrainerHostExtensions
    {
        public static Hero GetOrCreateSkillTrainer(this ISkillTrainerHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi)) return null;
            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            return mgr?.GetOrCreateHero(s, poi.LocationId, host.SkillTrainerTemplateId);
        }

        public static bool IsSkillTrainerHero(this Hero h, ISkillTrainerHost host)
        {
            return h?.Template?.StringId != null
                   && host?.SkillTrainerTemplateId != null
                   && h.Template.StringId == host.SkillTrainerTemplateId;
        }
    }
}
