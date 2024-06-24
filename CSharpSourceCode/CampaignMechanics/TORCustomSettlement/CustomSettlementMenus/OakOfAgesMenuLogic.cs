using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
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

    private const int TravelCost = 100;
    private const int RootUnlockCost = 200;
    private const int RootTravelCostReductionUpgradeCost = 500;
    private const int RootTravelBackUpgradeCost = 750;
    private const int RootHealUpgradeCost = 1000;
    private Vec2 ArdenLocation = new Vec2(945.4077f, 1111.009f);
    private Vec2 LaurelornLocation = new Vec2(1239.391f, 1276.938f);
    private Vec2 GryphenWoodLocation = new Vec2(1606.698f, 1133.905f);
    
    
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

    private readonly List<List<string>> _upgrades;
    
    public OakOfAgesMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
        _upgrades = [PartyUpgradeAttributes, MaximumHealthUpgradeAttributes, CustomResourceGainUpgrades, UpkeepReductionUpgrades];
    }

    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddOakOfAgeMenus(campaignGameStarter);
    }
    
    private void OakOfAgeMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TORBaseSettlementComponent;
        if (component == null) return;
        
        var text = component.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        
        
        MBTextManager.SetTextVariable("FORESTHARMONY", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
        MBTextManager.SetTextVariable("FORESTHARMONY1", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());

    }
    
    
    public void AddOakOfAgeMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_menu", "{LOCATION_DESCRIPTION}", OakOfAgeMenuInit);
        starter.AddGameMenuOption("oak_of_ages_menu", "branch", "Branches of the Oak",null, delegate
        {
            GameMenu.SwitchToMenu("oak_of_ages_branches_menu");
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "roots", "World roots of the Oak",null, delegate
        {
            GameMenu.SwitchToMenu("oak_of_ages_roots_menu");
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        
        AddBranchesOfTheOakMenu(starter);
        AddWorldRootMenu(starter);
    }

    private void AddWorldRootMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_roots_menu", "World roots of the Oak", OakOfAgeMenuInit);
        
        MBTextManager.SetTextVariable("ROOTUNLOCKCOST",RootUnlockCost);
        MBTextManager.SetTextVariable("ROOTTRAVELUPGRADE", RootTravelCostReductionUpgradeCost);
        MBTextManager.SetTextVariable("ROOTRETURNUPGRADE",RootTravelBackUpgradeCost);
        MBTextManager.SetTextVariable("ROOTHEALUPGRADE",RootHealUpgradeCost);
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock Pathway to the Forest of Arden. {ROOTUNLOCKCOST}{FORESTHARMONY}",args => 
            RootUpgradeCondition(args,RootUnlockCost, "WorldRootTarget_Arden"),_ => RootUpgradeConsequence(RootUnlockCost,"WorldRootTarget_Arden" ));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to the forest of Arden. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",args => 
            RootAcessibleCondition(args, TravelCost, "WorldRootTarget_Arden"),_ => RootTravelConsequence(ArdenLocation));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock the pathway to Laurelorn. {ROOTUNLOCKCOST}{FORESTHARMONY}",args => 
            RootUpgradeCondition(args,RootUnlockCost, "WorldRootTarget_Laurelorn"),_ => RootUpgradeConsequence(RootUnlockCost,"WorldRootTarget_Laurelorn" ));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to Laurelorn. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",args => 
            RootAcessibleCondition(args, TravelCost, "WorldRootTarget_Laurelorn"),_ => RootTravelConsequence(LaurelornLocation));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock the pathway to the Gryphenwood. {ROOTUNLOCKCOST}{FORESTHARMONY}",args => 
            RootUpgradeCondition(args,RootUnlockCost, "WorldRootTarget_GryphenWood"),_ => RootUpgradeConsequence(RootUnlockCost,"WorldRootTarget_GryphenWood" ));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to the Gryphenwood. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",args => 
            RootAcessibleCondition(args, TravelCost, "WorldRootTarget_GryphenWood"),_ => RootTravelConsequence(GryphenWoodLocation));
        
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Reduce the travel cost. {ROOTTRAVELUPGRADE}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootTravelBackUpgradeCost,"WETravelCostUpgrade", true),_ => RootUpgradeConsequence(RootTravelCostReductionUpgradeCost,"WETravelCostUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootTravelBackUpgradeCost,"WETravelBackUpgrade", true),_ => RootUpgradeConsequence(RootTravelBackUpgradeCost,"WETravelBackUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Allow that all troops and heroes are healed upon using the world roots. {ROOTHEALUPGRADE}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootTravelBackUpgradeCost,"WETravelHealUpgrade", true),_ => RootUpgradeConsequence(RootHealUpgradeCost,"WETravelHealUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "branchMenu_leave", "Leave...",
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

    private bool RootUpgradeCondition(MenuCallbackArgs args, int upgradeCost, string upgradeId, bool showAnyway=false)
    {
        if (Hero.MainHero.HasAttribute(upgradeId))
        {
            if (showAnyway)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Already unlocked.");
                return true;
            }
            return false;
        }
        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= upgradeCost)
        {
            return true;
        }
        else
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Not enough Harmony.");
            args.IsEnabled = false;
    
            return true;
        }
        
    }
    
    private bool RootAcessibleCondition(MenuCallbackArgs args, int travelCost, string upgradeId)
    {
        var cost = travelCost;
        if (!Hero.MainHero.HasAttribute(upgradeId)) return false;

        if (Hero.MainHero.HasAttribute("WETravelCostUpgrade"))
        {
            cost /= 2;
        }
        
        
        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() > cost)
        {
            return true;
        }
        else
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Not enough Harmony for traveling.");
            args.IsEnabled = false;
            return true;
        }

    }
    

    private void RootTravelConsequence(Vec2 location)
    {
        LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
        GameMenu.ExitToLast();
        Campaign.Current.MainParty.Position2D =MobilePartyHelper.FindReachablePointAroundPosition(location,0f);

        if (Hero.MainHero.HasAttribute("WETravelHealUpgrade"))
        {
            var heroes = MobileParty.MainParty.GetMemberHeroes();
            foreach (var hero in heroes)
            {
                hero.Heal(1000,false);
            }

            var list = MobileParty.MainParty.MemberRoster.GetTroopRoster();
            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];
                var woundedNumber = element.WoundedNumber;

                MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(index, 0, -woundedNumber);
            }
        }

    }
    
    private void RootUpgradeConsequence(int upgradeCost, string worldRootTargetLocation)
    {
        Hero.MainHero.AddAttribute(worldRootTargetLocation);
        Hero.MainHero.AddCultureSpecificCustomResource(-upgradeCost);
        GameMenu.SwitchToMenu("oak_of_ages_roots_menu");
    }



    private void AddBranchesOfTheOakMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_branches_menu", "Branches of The Oak", null);
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Increase party Size. {PARTYSIZEUPGRADECOST}{FORESTHARMONY}",args => 
            BranchUpgradeCondition(args,PartySizeUpgradeCost, "WEPartySizeUpgrade"),_ => UpgradeConsequence(PartySizeUpgradeCost,"WEPartySizeUpgrade" ));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "WEHealthUpgrade", "Increase maximum health. {HEALTHUPGRADECOST}{FORESTHARMONY}",
            args => BranchUpgradeCondition(args,HealthUpgradeCost, "branchMenu_B"),_ => UpgradeConsequence(HealthUpgradeCost,"WEHealthUpgrade" ));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_C", "Increase the daily harmony gain. {GAINUPGRADECOST}{FORESTHARMONY}",
            args => BranchUpgradeCondition(args,GainUpgradeCost,"WEGainUpgrade"),_ => UpgradeConsequence(GainUpgradeCost, "WEGainUpgrade"));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_D", " Troop harmony upkeep  reduction. {UPKEEPUPGRADECOST}{FORESTHARMONY}", 
            args => BranchUpgradeCondition(args,TroopUpkeepUpgradeCost, "WEUpkeepUpgrade") ,_ => UpgradeConsequence(TroopUpkeepUpgradeCost, "WEGainUpgrade"));
        
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
        foreach (var list in _upgrades)
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



    private string GetTextVariableForUpgradeCost(string upgradeId)
    {
        return upgradeId switch
        {
            "WEPartySizeUpgrade" => "PARTYSIZEUPGRADECOST",
            "WEHealthUpgrade" => "HEALTHUPGRADECOST",
            "WEGainUpgrade" => "GAINUPGRADECOST",
            "WEUpkeepUpgrade" => "UPKEEPUPGRADECOST",
            _ => ""
        };
    }
    
    private bool BranchUpgradeCondition(MenuCallbackArgs args, int cost, string id)
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
        foreach (var list in _upgrades)
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