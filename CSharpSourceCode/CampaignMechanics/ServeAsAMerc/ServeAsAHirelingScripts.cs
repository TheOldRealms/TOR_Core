using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAMerc;

public static class ServeAsAHirelingScripts
{
    public static void AddHirelingBenefits(Hero hero, ref ExplainedNumber number)
    {
        var hirelingCampaignBehavior = Campaign.Current.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();

        if (hirelingCampaignBehavior == null) return;

        var duration = hirelingCampaignBehavior.DurationInDays;
        var battles = hirelingCampaignBehavior.ManuallyFoughtBattles;
        
        if (hero == Hero.MainHero)
        {
            var benefits = new ExplainedNumber();
            var cultureId = hero.Culture.StringId;
            
            if (cultureId == TORConstants.Cultures.BRETONNIA)
            {
                benefits.Add(10f); 
                benefits.AddFactor((0.1f * battles));
                benefits.AddFactor(-0.5f+duration/20);
                number.Add(benefits.ResultNumber, new TextObject("Knightly Service"));
                return;
            }

            if (cultureId == TORConstants.Cultures.SYLVANIA || cultureId == TORConstants.Cultures.MOUSILLON && hero.IsNecromancer() || (hero.PartyBelongedTo!=null && hero.PartyBelongedTo.HasNecromancer()))
            {
                number.Add(25, new TextObject("Leeched Dark Energy"));
                return;
            }

            if (cultureId == TORConstants.Cultures.EMPIRE)
            {
                number.Add(5, new TextObject("Gained Respect"));
                benefits.AddFactor((0.1f * battles));
                //benefits.AddFactor(-0.5f+duration/10);
                return;
            }
        }
        
        number.AddFactor(hirelingCampaignBehavior.DurationInDays/10);
    }
}