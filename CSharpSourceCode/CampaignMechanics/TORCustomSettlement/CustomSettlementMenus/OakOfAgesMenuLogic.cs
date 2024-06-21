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
    private int PartySizeUpgradeCost = 100;
    private int HealthUpgradeCost = 125;
    
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
    
    public OakOfAgesMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
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
        
        
        MBTextManager.SetTextVariable("FORESTBINDING", CustomResourceManager.GetResourceObject("ForestBinding").GetCustomResourceIconAsText());
    }


    private void AddBranchesOfTheOakMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_branches_menu", "Branches of The Oak", OakOfAgeMenuInit);
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Increase party Size. ({PARTYSIZEUPGRADECOST}{FORESTBINDING})",PartySizeUpgradeCondition,PartySizeUpgradeConsequence);
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_B", "Increase maximum health. ({PARTYSIZEUPGRADECOST}{FORESTBINDING})",null,null);
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_C", "C",null,null);
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_D", "D", null,null);
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_D", "E", null,null);
        
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
    
    
    private void HealthUpgradeConsequence(MenuCallbackArgs args)
    {
        foreach (var attribute in MaximumHealthUpgradeAttributes.Where(attribute => !Hero.MainHero.HasAttribute(attribute)))
        {
            Hero.MainHero.AddAttribute(attribute);
            return;
        }
    }
    
    private bool HealthUpgradeCondition(MenuCallbackArgs args)
    {
        if (UnlockedAllHealthUpgrades(out int upgradeCount))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}You can't increase your maximum health further.", null);
            args.IsEnabled = false;
        }

        var cost = PartySizeUpgradeCost * (1+upgradeCount);
        MBTextManager.SetTextVariable("HEALTHUPGRADECOST", cost);
        if (cost > Hero.MainHero.GetCustomResourceValue("ForestBinding"))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Requires {HEALTHUPGRADECOST}{FORESTBINDING} for the next upgrade .", null);
            args.IsEnabled = false;
        }

        return true;
    }
    
    
    
    private void PartySizeUpgradeConsequence(MenuCallbackArgs args)
    {
        foreach (var attribute in PartyUpgradeAttributes.Where(attribute => !Hero.MainHero.HasAttribute(attribute)))
        {
            Hero.MainHero.AddAttribute(attribute);
            return;
        }
    }
    
    private bool PartySizeUpgradeCondition(MenuCallbackArgs args)
    {
        if (UnlockedAllPartySizeModules(out int upgradeCount))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}You can't increase your party size further.", null);
            args.IsEnabled = false;
        }

        var cost = PartySizeUpgradeCost * (1+upgradeCount);
        MBTextManager.SetTextVariable("PARTYSIZEUPGRADECOST", cost);
        if (cost > Hero.MainHero.GetCustomResourceValue("ForestBinding"))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Requires {PARTYSIZEUPGRADECOST}{FORESTBINDING} for the next upgrade .", null);
            args.IsEnabled = false;
        }

        return true;
    }
    
    private bool UnlockedAllPartySizeModules( out int upgradeCount)
    {
        upgradeCount = 0;
        foreach (var attribute in PartyUpgradeAttributes)
        {
            if (!Hero.MainHero.HasAttribute(attribute))
            {
                return false;
            }
            upgradeCount++;
        }
        
        return true;
    }
    
    private bool UnlockedAllHealthUpgrades( out int upgradeCount)
    {
        upgradeCount = 0;
        foreach (var attribute in PartyUpgradeAttributes)
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