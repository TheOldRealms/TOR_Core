using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: hosts an enchanter hero (crafts magical items / applies
    /// enchantment suffixes). May be the same hero as the POI's spell-trainer
    /// via same-template-id dedup (see architecture-redesign.md).
    /// </summary>
    public interface IEnchanterHost
    {
        /// <summary>Template id used to create / find this POI's enchanter hero.</summary>
        string EnchanterTemplateId { get; }

        /// <summary>Enchantment suffix pool this host can apply (e.g. "vc_enchant").</summary>
        List<string> EnchantmentSuffixes { get; }

        /// <summary>
        /// Can the player's party engage this enchanter? Same shape as
        /// <see cref="ISpellTrainerHost.IsPartyEligibleForTrainer"/> — each POI
        /// owns the "main hero OR party-companion matches this culture" rule.
        /// In practice, non-Empire POIs delegate to IsPartyEligibleForTrainer
        /// since the same hero plays both roles via template-id dedup.
        /// </summary>
        bool IsPartyEligibleForEnchanter();
    }

    /// <summary>
    /// Lifecycle helpers for <see cref="IEnchanterHost"/>: get-or-create the
    /// enchanter hero, place them at the POI's Location, identify an enchanter-hero
    /// by template id. Co-located with the interface so new host subclasses discover
    /// the full capability contract in one file.
    /// </summary>
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
