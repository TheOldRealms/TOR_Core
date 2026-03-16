using System.Collections.Generic;
using System.Net.Http.Headers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Menagery;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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
        var engineerRank = GetOathGoldForGuildRespect(behavior.EngineerGuildReputation);
        var warriorsRank = GetOathGoldForGuildRespect(behavior.WarriorsGuildReputation);
        var runeSmithRank = GetOathGoldForGuildRespect(behavior.RuneSmithReputation);
        var gemcutterRank = GetOathGoldForGuildRespect(behavior.GemcuttersAndMinersReputation);
        var brewersRank = GetOathGoldForGuildRespect(behavior.BrewersGuildReputation);


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        string time = ((int)lastVisitToTown).ToString() + " days";
        if (lastVisitToTown / CampaignTime.DaysInWeek > 2)
        {
            time = ">2 weeks ago";
        }
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_oathgold","time_since_visit","Time since last benefits provided"), time, 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{ENGINEERS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_engineer_benefit","title","Engineers")).ToString(), engineerRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));

        //could use textObject variables to only add "Reduced gun troop upkeep." when bonus > 0
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","description","Access to ranged weapons and artillery, reduce gunmen and Irondrake upgrade costs."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var gunTroopUpkeepReduction = 0; //can this find the amount elsewhere?
        if (Hero.MainHero.HasAttribute("DwarfEngineersIII"))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","arsenal","Entire arsenal"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
            gunTroopUpkeepReduction = 25;
        }
        else if (Hero.MainHero.HasAttribute("DwarfEngineersII"))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_engineer_benefit","artillery","Guns and artillery"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
            gunTroopUpkeepReduction = 15;
        }
        else if (Hero.MainHero.HasAttribute("DwarfEngineersI"))
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
        if (Hero.MainHero.HasAttribute("RuneSmithIII"))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","anvil","Artefact"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
            ironbreakerUpgradeReduction = 20;
        }
        else if (Hero.MainHero.HasAttribute("RuneSmithII"))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","equipment2","Weapons and armors"), " ", 0, false, TooltipProperty.TooltipPropertyFlags.None));
            ironbreakerUpgradeReduction = 10;
        }
        else if (Hero.MainHero.HasAttribute("RuneSmithI"))
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","equipment1","Weapons"), " ", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }
        if (ironbreakerUpgradeReduction > 0)
        {
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_runesmith_benefit","upgradecost","Ironbreaker upgrade cost reduction."), "-" + ironbreakerUpgradeReduction.ToString() + "%", 0, true, TooltipProperty.TooltipPropertyFlags.None));
        }

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));

        list.Add(new TooltipProperty(new TextObject("{MINERS_GUILD_ICON} " + TORTextHelper.GetText("tor_dw_miners_benefit","title","Mining and Expeditions Guild")).ToString(), gemcutterRank.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_miners_benefit","description","Receive raw materials, increase mining production, launch expeditions."), "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
        var oreVillageBoost = 0;
        if (Hero.MainHero.HasAttribute("DwarfMinersIII"))
        {
            oreVillageBoost = 25;
        }
        else if (Hero.MainHero.HasAttribute("DwarfMinersII"))
        {
            oreVillageBoost = 10;
        }
        else if (Hero.MainHero.HasAttribute("DwarfMinersI"))
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
        if (Hero.MainHero.HasAttribute("DwarfBrewersIII"))
        {
            carePackageSize = "Medium";
            dawiSightBonus = 30;
            dawiFoodBoost = 50;
            dawiLoyaltyBoost = 2;
        }
        else if (Hero.MainHero.HasAttribute("DwarfBrewersII"))
        {
            carePackageSize = "Medium";
            dawiSightBonus = 20;
            dawiFoodBoost = 25;
            dawiLoyaltyBoost = 1;
        }
        else if (Hero.MainHero.HasAttribute("DwarfBrewersI"))
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
            list.Add(new TooltipProperty(TORTextHelper.GetText("tor_dw_brewers_benefit","loyalty","Global Karak Loyality"), "+" + dawiLoyaltyBoost, 0, true, TooltipProperty.TooltipPropertyFlags.None));
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
        if (Hero.MainHero.HasAttribute("DwarfWarriorIII"))
        {
            warriorUpgradeReduction = 30;
            oathGoldBonus = 3f;
            militiaBonus = 4;
        }
        else if (Hero.MainHero.HasAttribute("DwarfWarriorII"))
        {
            warriorUpgradeReduction = 20;
            oathGoldBonus = 2f;
            militiaBonus = 2;
        }
        else if (Hero.MainHero.HasAttribute("DwarfWarriorI"))
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
}

public enum OathRespectLevel
{
    Respected = 3,
    Reliable = 2,
    Trustworthy = 1,
    Unknown = 0
}