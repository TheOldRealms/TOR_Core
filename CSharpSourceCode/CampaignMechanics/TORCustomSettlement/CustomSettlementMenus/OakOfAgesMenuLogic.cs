using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class OakOfAgesMenuLogic : TORBaseSettlementMenuLogic
{
    private const int PartySizeUpgradeCost = 100;
    private const int HealthUpgradeCost = 125;
    private const int GainUpgradeCost = 150;
    
    private const int TroopUpkeepUpgradeCost = 200;
    private const int TroopXPUpgradeCost = 175;
    
    public static readonly List<string> PartyUpgradeAttributes =
    [
        "WEPartySizeUpgrade1",
        "WEPartySizeUpgrade2",
        "WEPartySizeUpgrade3",
        "WEPartySizeUpgrade4",
        "WEPartySizeUpgrade5"
    ];
    
    public static readonly List<string> MaximumHealthUpgradeAttributes =
    [
        "WEHealthUpgrade1",
        "WEHealthUpgrade2",
        "WEHealthUpgrade3",
        "WEHealthUpgrade4",
        "WEHealthUpgrade5"
    ];
    
    public static readonly List<string> CustomResourceGainUpgrades =
    [
        "WEGainUpgrade1",
        "WEGainUpgrade2",
        "WEGainUpgrade3",
        "WEGainUpgrade4",
        "WEGainUpgrade5"
    ];
    
    public static readonly List<string> UpkeepReductionUpgrades =
    [
        "WEUpkeepUpgrade1",
        "WEUpkeepUpgrade2",
        "WEUpkeepUpgrade3",
    ];
    
    public static readonly List<string> DailyXPForTroops =
    [
        "WEXPUpgrade1",
        "WEXPUpgrade2",
        "WEXPUpgrade2",
        "WEXPUpgrade2",
        "WEXPUpgrade2"
    ];

    private List<List<string>> Upgrades;
    
    public OakOfAgesMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
        Upgrades = new List<List<string>>()
        {
            PartyUpgradeAttributes, MaximumHealthUpgradeAttributes, CustomResourceGainUpgrades, DailyXPForTroops, UpkeepReductionUpgrades
        };
    }

    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddOakOfAgeMenus(campaignGameStarter);
    }
    
    
    public void AddOakOfAgeMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_menu", "{LOCATION_DESCRIPTION}", OakOfAgeMenuInit);
        starter.AddGameMenuOption("oak_of_ages_menu", "branch", "Branches of the Oak",null, delegate
        {
            GameMenu.SwitchToMenu("oak_of_ages_branches_menu");
        }, false, 4, false);
        starter.AddGameMenuOption("oak_of_ages_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        
        AddBranchesOfTheOakMenu(starter);
    }

    private void OakOfAgeMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TORBaseSettlementComponent;
        var text = component.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        
        
        MBTextManager.SetTextVariable("FORESTHARMONY", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
        MBTextManager.SetTextVariable("FORESTHARMONY1", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
    }


    private void AddBranchesOfTheOakMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_branches_menu", "Branches of The Oak", OakOfAgeMenuInit);
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Increase party Size. {PARTYSIZEUPGRADECOST}{FORESTHARMONY}",args => 
            UpgradeCondition(args,PartySizeUpgradeCost, "WEPartySizeUpgrade"),_ => UpgradeConsequence(PartySizeUpgradeCost,"WEPartySizeUpgrade" ));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "WEHealthUpgrade", "Increase maximum health. {HEALTHUPGRADECOST}{FORESTHARMONY}",
            args => UpgradeCondition(args,HealthUpgradeCost, "branchMenu_B"),_ => UpgradeConsequence(HealthUpgradeCost,"WEHealthUpgrade" ));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_C", "Increase the daily harmony gain. {GAINUPGRADECOST}{FORESTHARMONY}",
            args => UpgradeCondition(args,GainUpgradeCost,"WEGainUpgrade"),_ => UpgradeConsequence(GainUpgradeCost, "WEGainUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_D", " Troop {FORESTHARMONY1} Upkeep  reduction. {UPKEEPUPGRADECOST}{FORESTHARMONY}", 
            args => UpgradeCondition(args,TroopUpkeepUpgradeCost, "WEUpkeepUpgrade") ,_ => UpgradeConsequence(TroopUpkeepUpgradeCost, "WEGainUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_E", "Increase daily XP gain for troops. {XPUPGRADECOST}{FORESTHARMONY} ", 
            args => UpgradeCondition(args,TroopXPUpgradeCost, "WEXPUpgrade") , _ => UpgradeConsequence(TroopXPUpgradeCost, "WEXPUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_leave", "Leave...",
            delegate(MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("oak_of_ages_menu");
            }, 
            false, -1, false);
    }
    
    private void UpgradeConsequence(int cost, string attributeID)
    {

        List<string> upgradeList = new List<string>();
        foreach (var list in Upgrades)
        {
            if (list.Any(X => X.Contains(attributeID)))
            {
                upgradeList = list;
                break;
            }
        }

        var attributeUnlocked = UnlockedAllUpgradesOfType(out var count, attributeID);
        
        if (attributeUnlocked) return;
        
        var attribute = upgradeList.FirstOrDefault(x => !Hero.MainHero.HasAttribute(x));
        if (attribute == null) return;
        
        
        Hero.MainHero.AddAttribute(attribute);
        
        Hero.MainHero.AddCultureSpecificCustomResource(-(cost*(1+count)));
        GameMenu.SwitchToMenu("oak_of_ages_branches_menu");
    }



    private string GetTextVariableForUpgradeCost(string UpgradeID)
    {
        return UpgradeID switch
        {
            "WEPartySizeUpgrade" => "PARTYSIZEUPGRADECOST",
            "WEHealthUpgrade" => "HEALTHUPGRADECOST",
            "WEGainUpgrade" => "GAINUPGRADECOST",
            "WEUpkeepUpgrade" => "UPKEEPUPGRADECOST",
            "WEXPUpgrade" => "XPUPGRADECOST",
            _ => ""
        };
    }
    
    private bool UpgradeCondition(MenuCallbackArgs args, int cost, string id)
    {
        var textVariable = GetTextVariableForUpgradeCost(id);
        MBTextManager.SetTextVariable("FORESTHARMONY", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
        if (UnlockedAllUpgradesOfType(out int upgradeCount, id))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}You reached the maximum number of branches", null);
            MBTextManager.SetTextVariable(textVariable,  "");
            MBTextManager.SetTextVariable("FORESTHARMONY",  "");
            args.IsEnabled = false;
            return true;
        }

        var totalCost = cost * (1 + upgradeCount);
        MBTextManager.SetTextVariable(textVariable,  totalCost);
        if (totalCost > Hero.MainHero.GetCustomResourceValue("ForestHarmony"))
        {
            var text = $"{{=tor_custom_settlement_we_party_size_info_str}}Requires {{{textVariable}}}{{FORESTBINDING}} for the next upgrade.";
            args.Tooltip = new TextObject(text);
            args.IsEnabled = false;
        }

        return true;
    }
    
    private bool UnlockedAllUpgradesOfType( out int upgradeCount, string UpgradeType)
    {
        List<string> targetList = new List<string>();
        foreach (var list in Upgrades)
        {
            if (list.Any(x => x.Contains(UpgradeType)))
            {
                targetList = list;
                break;
            }
        }

        if (targetList.Count == 0)
        {
            upgradeCount = 0;
            return true;
        }
        
        upgradeCount = 0;
        foreach (var attribute in targetList)
        {
            if (!Hero.MainHero.HasAttribute(attribute))
            {
                return false;
            }
            upgradeCount++;
        }
        
        return true;
    }
}