using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORCrimeModel : DefaultCrimeModel
    {
        public override ExplainedNumber GetEffectiveCrimeChange(IFaction faction, float deltaCrimeRating)
        {
            var result = base.GetEffectiveCrimeChange(faction, deltaCrimeRating);
            // BurntEarth primary: personal-role roguery perk shaves
            // positive crime-rating accrual by its PrimaryBonus (-20% default).
            // We rebuild the result rather than mutating the base one so
            // the scalar applies before vanilla's LimitMin/Max clamps run.
            if (deltaCrimeRating > 0f
                && Hero.MainHero != null
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.BurntEarth))
            {
                float factor = 1f + TORPerks.Roguery.BurntEarth.PrimaryBonus; // 1 + (-0.2) = 0.8
                float scaledDelta = deltaCrimeRating * factor;
                var scaled = new ExplainedNumber(scaledDelta);
                scaled.Add(faction.MainHeroCrimeRating);
                scaled.LimitMin(0f);
                scaled.LimitMax(Campaign.Current.Models.CrimeModel.GetMaxCrimeRating());
                scaled.Add(0f, new TextObject("{=tor_perk_burnt_earth_crime}Burnt Earth"));
                return scaled;
            }
            return result;
        }
    }
}
