using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.Models;

public class TORCompanionTrainingModel : GameModel
{
    public bool HeroIsEligibleForTraining(Hero hero, SkillObject skillObject)
    {
        if (hero == Hero.MainHero)
        {
            return false;
        }

        var skillValue = hero.GetSkillValue(skillObject);
        switch (GetMainHeroLevelForTraining())
        {
            case MainHeroTierForSkillTraining.None:
                break;
            case MainHeroTierForSkillTraining.Novice when skillValue <= 100:
            case MainHeroTierForSkillTraining.Adept when skillValue <= 200:
            case MainHeroTierForSkillTraining.Master when skillValue <= 300:
                return true;
        }

        return false;
    }


    public int DailySkillGainForTraining(Hero hero, SkillObject skillObject)
    {
        var skillValue = hero.GetSkillValue(skillObject);


        skillValue = Math.Max(skillValue, 1);
        var gain = MBRandom.RandomInt(100, 250) * skillValue;

        return gain;
    }

    public bool ReachedSkillCap(Hero hero, SkillObject skillObject, int pendingValue)
    {
        var value = hero.GetSkillValue(skillObject);
        var max = GetMaximumSkillLevels(value);

        if (value > max)
        {
            return true;
        }


        return false;

    }

    private int GetMaximumSkillLevels(int value)
    {
        var level = GetMainHeroLevelForTraining();
        switch (level)
        {
            case MainHeroTierForSkillTraining.None:
                return 100;
            case MainHeroTierForSkillTraining.Novice:
                return 100;
            case MainHeroTierForSkillTraining.Adept:
                return 200;
            case MainHeroTierForSkillTraining.Master:
                return 300;
        }

        return 0;
    }

    public (int goldcost, int customResourceCost) GetCostForTraining(Hero hero, SkillObject skillObject)
    {
        var skillValue = hero.GetSkillValue(skillObject);

        var customResourceFactor = 1;
        var goldCostFactor = 1;


        var skillCapForLevel = 0;

        switch (skillValue)
        {
            case < 100:
                customResourceFactor = 1;
                goldCostFactor = 1;
                skillCapForLevel = 100;
                break;
            case < 200:
                customResourceFactor = 2;
                goldCostFactor = 5;
                skillCapForLevel = 200;
                break;
            case > 200:
                customResourceFactor = 3;
                goldCostFactor = 5;
                skillCapForLevel = 300;
                break;
        }


        var basegoldCost = 1000f;

        var crModel = Campaign.Current.Models.GetCustomResourceModel();


        var baseCRCost = 5f * crModel.GetFactorForGeneralizedCosts(Hero.MainHero.GetCultureSpecificCustomResource());

        var goldCostForCurrentSkill = (int)basegoldCost * (skillCapForLevel - skillValue) * goldCostFactor;

        var cRForCurrentSkill = (int)baseCRCost * (skillCapForLevel - skillValue) * customResourceFactor;


        return (goldCostForCurrentSkill, cRForCurrentSkill);


    }

    public MainHeroTierForSkillTraining GetMainHeroLevelForTraining()
    {
        var hero = Hero.MainHero;

        return hero.Level switch
        {
            < 10 => MainHeroTierForSkillTraining.None,
            <= 10 => MainHeroTierForSkillTraining.Novice,
            <= 25 => MainHeroTierForSkillTraining.Adept,
            >= 25 => MainHeroTierForSkillTraining.Master
        };
    }

    public enum MainHeroTierForSkillTraining
    {
        None = 0,
        Novice = 10,
        Adept = 20,
        Master = 25
    }
}