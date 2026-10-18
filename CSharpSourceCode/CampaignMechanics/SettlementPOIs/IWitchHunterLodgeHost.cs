using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: hosts the Order of Sigmar's Inquisitor hero. The POI framework
    /// creates one hero per lodge-eligible settlement using <see cref="InquisitorTemplateId"/>,
    /// places them at the POI's Location, and lets the POI's RegisterDialogs wire the hub.
    /// </summary>
    public interface IWitchHunterLodgeHost
    {
        /// <summary>Template id used to create / find this POI's Inquisitor hero.</summary>
        string InquisitorTemplateId { get; }
    }

    public static class WitchHunterLodgeHostExtensions
    {
        public static Hero GetOrCreateInquisitor(this IWitchHunterLodgeHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi)) return null;
            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            return mgr?.GetOrCreateHero(s, poi.LocationId, host.InquisitorTemplateId);
        }

        public static void EnsurePlaced(this IWitchHunterLodgeHost host, Settlement s)
        {
            if (!(host is SettlementPOI poi) || !poi.AppliesTo(s)) return;
            var hero = host.GetOrCreateInquisitor(s);
            if (hero == null) return;
            if (!s.HeroesWithoutParty.Contains(hero)) return;
            var location = s.LocationComplex.GetLocationWithId(poi.LocationId);
            TrainerPlacement.EnsureAt(s, hero, location);
        }
    }
}
