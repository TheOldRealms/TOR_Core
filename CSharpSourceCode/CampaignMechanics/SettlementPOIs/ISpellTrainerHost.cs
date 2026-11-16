using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: hosts a spell-trainer hero. The POI framework creates
    /// one hero per settlement using <see cref="SpellTrainerTemplateId"/>, places
    /// them at the POI's Location, and routes dialog triggers.
    /// </summary>
    public interface ISpellTrainerHost
    {
        /// <summary>Template id used to create / find this POI's trainer hero.</summary>
        string SpellTrainerTemplateId { get; }

        /// <summary>
        /// Can the player's party engage this trainer in dialog?
        /// Each POI owns the "main hero OR party-companion matches this culture"
        /// decision — avoids a central switch-on-template-id in the helper.
        ///
        /// Called when the player opens a conversation with the trainer hero.
        /// Distinct from <see cref="SettlementPOI.CheckAccess"/>, which gates
        /// walking into the POI location at all.
        /// </summary>
        bool IsPartyEligibleForTrainer();
    }

    /// <summary>
    /// Lifecycle helpers for <see cref="ISpellTrainerHost"/>: get-or-create the
    /// trainer hero, place them at the POI's Location, identify a trainer-hero by
    /// template id. Co-located with the interface so new host subclasses discover
    /// the full capability contract in one file.
    /// </summary>
    public static class SpellTrainerHostExtensions
    {
        public static Hero GetOrCreateTrainer(this ISpellTrainerHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi)) return null;
            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            return mgr?.GetOrCreateHero(s, poi.LocationId, host.SpellTrainerTemplateId);
        }

        public static bool IsSpellTrainerHero(this Hero h, ISpellTrainerHost host)
        {
            return h?.Template?.StringId != null
                   && host?.SpellTrainerTemplateId != null
                   && h.Template.StringId == host.SpellTrainerTemplateId;
        }
    }
}
