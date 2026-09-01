using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Menagery;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.CustomResources;

public class OathGoldHelper
{

    public static List<TooltipProperty> GetOathGoldInfo()
    {
        var list = new List<TooltipProperty>();

        var behavior = Campaign.Current.GetCampaignBehavior<OathGoldBehavior>();

        if (behavior == null) return list;
        var lastVisitToTown = behavior.LastVisitAtTown;
        var expeditionMaximum = behavior.ExpeditionMaximum;
        var expeditionCount = behavior.CurrentExpeditions;
        var engineerRank = GetOathGoldGuildRespectText(behavior.EngineerGuildReputation);
        var warriorsRank = GetOathGoldGuildRespectText(behavior.WarriorsGuildReputation);
        var runeSmithRank = GetOathGoldGuildRespectText(behavior.RunemithGuildReputation);
        var minerRank = GetOathGoldGuildRespectText(behavior.MinerGuildReputation);
        var brewersRank = GetOathGoldGuildRespectText(behavior.BrewersGuildReputation);


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        var time = TORTextHelper.GetTextObject("tor_dw_oathgold", "time_days_value", "{EFFECT_VALUE} days").SetTextVariable("EFFECT_VALUE", (int)lastVisitToTown);
        if (lastVisitToTown / CampaignTime.DaysInWeek > 2)
        {
            time = TORTextHelper.GetTextObject("tor_dw_oathgold", "time_value", ">{EFFECT_VALUE} weeks ago").SetTextVariable("EFFECT_VALUE", 2);
        }
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_oathgold","time_since_visit","Time since last benefits provided"), time.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{ENGINEERS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_engineer_benefit","title","Engineers")).ToString(), engineerRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));

        //could use textObject variables to only add "Reduced gun troop upkeep." when bonus > 0
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","description","Access to ranged weapons and artillery, reduce gunmen and Irondrake upgrade costs."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var gunTroopUpkeepReduction = 0; //can this find the amount elsewhere?
        if (Hero.MainHero.HasAttribute(behavior.EngineerGuild.AttributeBenefit3))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","arsenal","Entire arsenal"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
            gunTroopUpkeepReduction = 25;
        }
        else if (Hero.MainHero.HasAttribute(behavior.EngineerGuild.AttributeBenefit2))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","artillery","Guns and artillery"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
            gunTroopUpkeepReduction = 15;
        }
        else if (Hero.MainHero.HasAttribute(behavior.EngineerGuild.AttributeBenefit1))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","guns","Guns"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (gunTroopUpkeepReduction > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","upkeep_reduction","Upkeep reduction for gun troops"), "-" + gunTroopUpkeepReduction.ToString() + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{RUNESMITHS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_runesmith_benefit","title","Runesmiths.")).ToString(), runeSmithRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));

        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","description","Access to melee weapons and Runecraft, reduce Ironbreaker upgrade costs."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var ironbreakerUpgradeReduction = 0;
        if (Hero.MainHero.HasAttribute(behavior.RunesmithGuild.AttributeBenefit3))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","anvil","Artefact"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
            ironbreakerUpgradeReduction = 20;
        }
        else if (Hero.MainHero.HasAttribute(behavior.RunesmithGuild.AttributeBenefit2))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","equipment2","Weapons and armors"), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
            ironbreakerUpgradeReduction = 10;
        }
        else if (Hero.MainHero.HasAttribute(behavior.RunesmithGuild.AttributeBenefit1))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","equipment1","Weapons"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (ironbreakerUpgradeReduction > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","upgradecost","Ironbreaker upgrade cost reduction."), "-" + ironbreakerUpgradeReduction.ToString() + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{MINERS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_miners_benefit","title","Mining and Expeditions Guild")).ToString(), minerRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_miners_benefit","description","Receive raw materials, increase mining production, launch expeditions."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var oreVillageBoost = 0;
        if (Hero.MainHero.HasAttribute(behavior.MinerGuild.AttributeBenefit3))
        {
            oreVillageBoost = 25;
        }
        else if (Hero.MainHero.HasAttribute(behavior.MinerGuild.AttributeBenefit2))
        {
            oreVillageBoost = 10;
        }
        else if (Hero.MainHero.HasAttribute(behavior.MinerGuild.AttributeBenefit1))
        {
        }
        if (expeditionMaximum > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_miners_benefit","expeditions","Expeditions "), expeditionCount + "/" + expeditionMaximum, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (oreVillageBoost > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_miners_benefit","mine_productivity","Global productivity of mines"), "+" + oreVillageBoost.ToString() + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }



        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{BREWERS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_brewers_benefit","title","Brewers")).ToString(), brewersRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_brewers_benefit","description","Receive supplies, increase food production and spotting range near Dwarf holds, boost Rangers."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var dawiLoyaltyBoost = 0;
        var dawiFoodBoost = 0;
        var dawiSightBonus = 0;
        string carePackageSize = "";
        if (Hero.MainHero.HasAttribute(behavior.BrewerGuild.AttributeBenefit3))
        {
            carePackageSize = "Medium";
            dawiSightBonus = 30;
            dawiFoodBoost = 50;
            dawiLoyaltyBoost = 2;
        }
        else if (Hero.MainHero.HasAttribute(behavior.BrewerGuild.AttributeBenefit2))
        {
            carePackageSize = "Medium";
            dawiSightBonus = 20;
            dawiFoodBoost = 25;
            dawiLoyaltyBoost = 1;
        }
        else if (Hero.MainHero.HasAttribute(behavior.BrewerGuild.AttributeBenefit1))
        {
            carePackageSize = "Small";
            dawiSightBonus = 10;
            dawiFoodBoost = 10;
        }
        if (carePackageSize.Length > 0)//"" is empty string so it has length 0, right, right?
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_brewers_benefit","supplies","Biweekly supplies"), carePackageSize, 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (dawiFoodBoost > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_brewers_benefit","foodbonus","Food Production in Dwarf Karak"), "+" + dawiFoodBoost + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (dawiLoyaltyBoost > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_brewers_benefit","loyalty","Global Karak Loyalty"), "+" + dawiLoyaltyBoost, 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (dawiSightBonus > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_brewers_benefit","sightbonus","Extra sight close to Dwarf settlements"), "+" + dawiSightBonus + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{WARRIORS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_oathgold_warrior_benefit","title","Warriors")).ToString(), warriorsRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_oathgold_warrior_benefit","description","Reduce upgrade costs of melee troops, increase Oathgold gains, boost militias."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var warriorUpgradeReduction = 0;
        var oathGoldBonus = 0f;
        var militiaBonus = 0;
        if (Hero.MainHero.HasAttribute(behavior.WarriorGuild.AttributeBenefit3))
        {
            warriorUpgradeReduction = 30;
            oathGoldBonus = 3f;
            militiaBonus = 4;
        }
        else if (Hero.MainHero.HasAttribute(behavior.WarriorGuild.AttributeBenefit2))
        {
            warriorUpgradeReduction = 20;
            oathGoldBonus = 2f;
            militiaBonus = 2;
        }
        else if (Hero.MainHero.HasAttribute(behavior.WarriorGuild.AttributeBenefit1))
        {
            warriorUpgradeReduction = 10;
            oathGoldBonus = 1.5f;
        }
        if (warriorUpgradeReduction > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_oathgold_warriorguild_benefit","upgradecost","Upgrade cost reduction for Slayers, Warriors and Longbeard"), "+" + warriorUpgradeReduction.ToString() + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (oathGoldBonus > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_oathgold_warriorguild_benefit","oathgoldgain","Battle and tournament Oathgold gain"), "+" + oathGoldBonus.ToString(), 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (militiaBonus > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_oathgold_warriorguild_benefit","militiabonus","Additional Militia"), "+" + militiaBonus.ToString(), 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }


        list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost)); //empty line
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        return list;
    }

    public static OathRespectLevel GetOathGoldForGuildRespect(int respect)
    {
        return respect switch
        {
            >= 2000 => OathRespectLevel.Respected,
            >= 1000 => OathRespectLevel.Reliable,
            >= 500 => OathRespectLevel.Trustworthy,
            < 500 => OathRespectLevel.Unknown
        };
    }

    private static TextObject GetOathGoldGuildRespectText(int respect)
    {
        return GetOathGoldForGuildRespect(respect) switch
        {
            OathRespectLevel.Respected => TORTextHelper.GetTextObject("tor_dw_guild_relationship", "respected", "Respected"),
            OathRespectLevel.Reliable => TORTextHelper.GetTextObject("tor_dw_guild_relationship", "reliable", "Reliable"),
            OathRespectLevel.Trustworthy => TORTextHelper.GetTextObject("tor_dw_guild_relationship", "trustworthy", "Trustworthy"),
            _ => TORTextHelper.GetTextObject("tor_dw_guild_relationship", "unknown", "Unknown")
        };
    }
}

public enum OathRespectLevel
{
    Respected = 3,
    Reliable = 2,
    Trustworthy = 1,
    Unknown = 0
}