using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAHireling;

public static class ServeAsAHirelingHelpers
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
            
            if (cultureId == TORConstants.Cultures.EONIR)
            {
                number.Add(5, new TextObject("Gained Power"));
                benefits.AddFactor((0.05f * battles));
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

        var cultureId = hero.Culture.StringId;

        if (cultureId == TORConstants.Cultures.EMPIRE || cultureId == TORConstants.Cultures.SYLVANIA || cultureId == TORConstants.Cultures.EONIR)
        {
            wage.AddFactor((0.1f * battles));
            wage.AddFactor(-0.5f+duration/20);

            if (cultureId == TORConstants.Cultures.SYLVANIA)
            {
                wage.AddFactor(0.2f);   //vampires pay better ;)
            }
        }

        if (cultureId == TORConstants.Cultures.BRETONNIA)
        {
            wage.AddFactor((0.1f * battles));       //payment in bretonnia is bad
            var malus = 3 - ((int)hero.GetChivalryLevel());     //from level 3 on you increase your wage
            wage.AddFactor((-0.1f * malus));
        }
        
        var multiplier = hero.PartyBelongedTo.GetMemberHeroes().Count-1;
        
        wage.AddFactor(multiplier);
        
        number.Add(wage.ResultNumber ,new TextObject("Hireling Wage"));
    }

    public static bool HirelingServiceConditions()
    {
        var dialogPartner = Campaign.Current.ConversationManager.OneToOneConversationHero;

        if (GameTexts.TryGetText("HirelingLordExplain", out var explainText, dialogPartner.Culture.StringId))
        {
            GameTexts.SetVariable("HIRELING_EXPLAIN_TEXT",explainText);
        }
        else
        {
            var text= GameTexts.FindText("HirelingLordExplain", "default");
            GameTexts.SetVariable("HIRELING_EXPLAIN_TEXT",text);
        }
        
        if (GameTexts.TryGetText("HirelingLordResult", out var resultText, dialogPartner.Culture.StringId))
        {
            GameTexts.SetVariable("HIRELING_DECISION_TEXT",resultText);
        }
        else
        {
            var text= GameTexts.FindText("HirelingLordResult", "default");
            GameTexts.SetVariable("HIRELING_DECISION_TEXT",text);
        }
        
        
        if (dialogPartner.Culture.StringId == TORConstants.Cultures.EMPIRE)
        {
            var career = Hero.MainHero.GetCareer();
            if (career == TORCareers.Necromancer || career == TORCareers.MinorVampire)
                return false;

            return true;
        }
        
        if (dialogPartner.Culture.StringId == TORConstants.Cultures.SYLVANIA)
        {
            
            var career = Hero.MainHero.GetCareer();
            if (career == TORCareers.GrailKnight || career == TORCareers.GrailDamsel)
                return false;
            
            if (career == TORCareers.WitchHunter || 
                career == TORCareers.WarriorPriest||
                career == TORCareers.WarriorPriestUlric||
                career == TORCareers.ImperialMagister)
                return false;

            return true;
        }

        if (dialogPartner.Culture.StringId == TORConstants.Cultures.BRETONNIA)
        {
            if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.BRETONNIA)
                return false;
            
            return true;
        }
        
        if (dialogPartner.Culture.StringId == TORConstants.Cultures.MOUSILLON)
        {
            if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.BRETONNIA || dialogPartner.GetRelation(Hero.MainHero)>15)
                return true;

            return false;
        }

        if (dialogPartner.Culture.StringId == Hero.MainHero.Culture.StringId)
        {
            return true;
        }
        
        
        return false;
    }
}