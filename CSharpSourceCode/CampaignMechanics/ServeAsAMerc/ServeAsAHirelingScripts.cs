using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAMerc;

public static class ServeAsAHirelingScripts
{
    public static void AddHirelingCustomResourceBenefits(Hero hero, ref ExplainedNumber number)
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
                benefits.AddFactor((0.05f * battles));
                benefits.AddFactor(-0.5f+duration/40);
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
                benefits.AddFactor((0.05f * battles));
                return;
            }
        }
        
        number.AddFactor(hirelingCampaignBehavior.DurationInDays/10);
    }


    public static void AddHirelingWage(Hero hero, ref ExplainedNumber number)
    {
        var hirelingCampaignBehavior = Campaign.Current.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
        if (hirelingCampaignBehavior == null) return;
        if(hero.PartyBelongedTo==null) return; // can be null and causes crash
        var duration = hirelingCampaignBehavior.DurationInDays;
        var battles = hirelingCampaignBehavior.ManuallyFoughtBattles;
        var wage = new ExplainedNumber(25 * hero.Level);

        var cultureID = hero.Culture.StringId;

        if (cultureID == TORConstants.Cultures.EMPIRE || cultureID == TORConstants.Cultures.SYLVANIA)
        {
            wage.AddFactor((0.1f * battles));
            wage.AddFactor(-0.5f+duration/20);

            if (cultureID == TORConstants.Cultures.SYLVANIA)
            {
                wage.AddFactor(0.2f);   //vampires pay better ;)
            }
        }

        if (cultureID == TORConstants.Cultures.BRETONNIA)
        {
            wage.AddFactor((0.1f * battles));       //payment in bretonnia is bad
            var malus = 3 - ((int)hero.GetChivalryLevel());     //from level 3 on you increase your wage
            wage.AddFactor((-0.1f * malus));
        }
        
        var multiplier = hero.PartyBelongedTo.GetMemberHeroes().Count-1;
        
        wage.AddFactor(multiplier);
        
        number.Add(wage.ResultNumber ,new TextObject("Hireling Wage"));
    }
}