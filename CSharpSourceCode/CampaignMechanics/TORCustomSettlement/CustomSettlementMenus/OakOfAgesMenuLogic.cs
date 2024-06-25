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
    
    private const int TreeSymbolChangeCost = 100;
    private const int TreeSymbolUnlockCosts = 500;
    private const int TreeSymbolUpgradeNoCost = 400;
    private const int TreeSymbolUpgradeDaily = 600;

    private string _currentMenu;

    private static readonly List<string> PartyUpgradeAttributes =
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

    private static readonly List<string> UpkeepReductionUpgrades =
    [
        "WEUpkeepUpgrade1",
        "WEUpkeepUpgrade2",
        "WEUpkeepUpgrade3",
    ];

    private static readonly List<string> TreeSymbols = 
    [
        "WEKithbandSymbol",
        "WEWardancerSymbol",
        "WETreekinSymbol",
        "WEOrionSymbol",
        "WEArielSymbol",
        "WEDurthuSymbol",
        "WEWardenSymbol"
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
            _currentMenu = "oak_of_ages_branches_menu";
            GameMenu.SwitchToMenu(_currentMenu);
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "roots", "World roots of the Oak",null, delegate
        {
            _currentMenu = "oak_of_ages_roots_menu";
            GameMenu.SwitchToMenu(_currentMenu);
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "symbols", "Trunk Symbols",null, delegate
        {
            _currentMenu = "oak_of_ages_tree_symbols_menu";
            GameMenu.SwitchToMenu("oak_of_ages_tree_symbols_menu");
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            _currentMenu = "";
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, _ => PlayerEncounter.Finish(true), true);
        
        AddBranchesOfTheOakMenu(starter);
        AddWorldRootMenu(starter);
        AddTreeSymbolMenu(starter);
    }

    private void AddTreeSymbolMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_tree_symbols_menu", "Trunk Symbols: Choose one Symbol activated for your party. The Symbols provide strong enhancements, yet they will also provide strong disadvantages. Choose wisely, only one Symbol can be active at once.", OakOfAgeMenuInit);
        
        MBTextManager.SetTextVariable("TREESYMBOLCHANGECOST",TreeSymbolChangeCost);
        MBTextManager.SetTextVariable("TREESYMBOLUNLOCKCOST",TreeSymbolUnlockCosts);
        
        MBTextManager.SetTextVariable("TREESYMBOLFREEUPGRADE",TreeSymbolUpgradeNoCost);
        MBTextManager.SetTextVariable("TREESYMBOLCHANGEDAILY",TreeSymbolUpgradeDaily);
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_A_unlock", "Decipher the Sign of the Kithband. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WEKithbandSymbol",200),_ => UnlockOakUpgrade("WEKithbandSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_B_unlock", "Decipher the Sign of the Wardancers. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WEWardancerSymbol",200),_ => UnlockOakUpgrade("WEWardancerSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_C_unlock", "Decipher the Sign of the Treekin. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WETreekinSymbol",200),_ => UnlockOakUpgrade("WETreekinSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_D_unlock", "Decipher the Sign of the Orion. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WEOrionSymbol",300, 15),_ => UnlockOakUpgrade("WEOrionSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_E_unlock", "Decipher the Sign of the Ariel. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WEArielSymbol",300,15),_ => UnlockOakUpgrade("WEArielSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_F_unlock", "Decipher the Sign of Durthu. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WEDurthuSymbol",500,20),_ => UnlockOakUpgrade("WEDurthuSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_G_unlock", "Decipher the Sign of the Warden. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}",
            args => UnlockTreeSymbolCondition(args,"WEWardenSymbol",500,20),_ => UnlockOakUpgrade("WEWardenSymbol",TreeSymbolUnlockCosts));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_A", "Apply the Sign of the Kithband. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WEKithbandSymbol",TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence("WEKithbandSymbol",TreeSymbolChangeCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_B", "Apply the Sign of the Wardancers. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WEWardancerSymbol",TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence("WEWardancerSymbol",TreeSymbolChangeCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_C", "Apply the Sign of the Treekin. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WETreekinSymbol",TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence("WETreekinSymbol",TreeSymbolChangeCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_D", "Apply the Sign of the Orion. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WEOrionSymbol",TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence("WEOrionSymbol",TreeSymbolChangeCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_E", "Apply the Sign of the Ariel. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WEArielSymbol",TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence("WEArielSymbol",TreeSymbolChangeCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_F", "Apply the Sign of Durthu. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WEDurthuSymbol",TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence("WEDurthuSymbol",TreeSymbolChangeCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_G", "Apply the Sign of the Warden. {TREESYMBOLCHANGECOST}{FORESTHARMONY}",
            args => SelectTreeSymbolCondition(args,"WEWardenSymbol",TreeSymbolChangeCost,true), _ => SelectTreeSymbolConsequence("WEWardenSymbol",TreeSymbolChangeCost));
       
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_H", "Magical chalk. {TREESYMBOLFREEUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args,"WESymbolReduceCosts",TreeSymbolUpgradeNoCost, new TextObject("Remove Symbol change costs.{newline}{UPGRADEFAILEDREASON}")),_ => UnlockOakUpgrade("WESymbolReduceCosts", TreeSymbolUpgradeNoCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_I", "Decorated Trunk. {TREESYMBOLCHANGEDAILY}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args,"WESymbolsChangeCycle",TreeSymbolUpgradeDaily, new TextObject("Tree signs can be changed every day instead of every week.{newline}{UPGRADEFAILEDREASON}")),_ => UnlockOakUpgrade("WESymbolsChangeCycle", TreeSymbolUpgradeDaily));
        
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_leave", "Leave...",
            delegate(MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            },
            delegate
            {
                GameMenu.SwitchToMenu("oak_of_ages_menu");
            });
    }

    private bool DefaultUnlockOakUpgradeCondition(MenuCallbackArgs args, string upgrade, int cost, TextObject toolTipDescription = null , int minimumLevel=0)
    {
        MBTextManager.SetTextVariable("UPGRADEFAILEDREASON","");
        if (HasUnlockedUpgrade(upgrade))
        {
            MBTextManager.SetTextVariable("UPGRADEFAILEDREASON","Upgrade has been already unlocked");
            args.IsEnabled = false;
            args.Tooltip = toolTipDescription;
            return true;
        }
        
        
        if (minimumLevel > 0)
        {
            if (Hero.MainHero.Level < minimumLevel)
            {
                args.IsEnabled = false;
                MBTextManager.SetTextVariable("UPGRADEFAILEDREASON","requires level " +minimumLevel);
                args.Tooltip = new TextObject();
                return true;
            }
        }

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < cost)
        {
            MBTextManager.SetTextVariable("UPGRADEFAILEDREASON","Not enough harmony");
            args.IsEnabled = false;
            args.Tooltip = toolTipDescription;
            return true;
        }
        
        args.Tooltip = toolTipDescription;
        return true;
    }
    
    private void UnlockOakUpgrade(string upgrade, int cost)
    {

        var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
        settlementBehavior.UnlockOakUpgrade(upgrade);
        Hero.MainHero.AddCultureSpecificCustomResource(-cost);
        GameMenu.SwitchToMenu(_currentMenu);
    }

    private static bool HasUnlockedUpgrade(string upgrade)
    {
        var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
        return settlementBehavior.HasUnlockedOakUpgrade(upgrade);
    }
    


    private bool UnlockTreeSymbolCondition(MenuCallbackArgs args, string upgrade, int upgradeCost, int minimumLevel=0)
    {
        if (HasUnlockedUpgrade(upgrade))
        {
            return false;
        }
        if (minimumLevel > 0)
        {
            if (Hero.MainHero.Level < minimumLevel)
                return false;
        }

        if (Hero.MainHero.HasAttribute(upgrade))
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("Currently active");
            return true;
        }

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= upgradeCost)
        {
            return true;
        }
        
        args.IsEnabled = false;
        args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Not enough Harmony.");
        return true;
    }

    
    
    
    private bool SelectTreeSymbolCondition(MenuCallbackArgs args, string upgrade, int upgradeCost, bool showAnyway)
    {
        if (Hero.MainHero.HasAttribute(upgrade))
        {
            if (showAnyway)
            {
                args.Tooltip = new TextObject("Treesymbol is currently active");
                args.IsEnabled = false;
                return true;
            }

            return false;
        }

        if (!HasUnlockedUpgrade(upgrade))
        {
            return false;
        }

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= upgradeCost)
        {
            return true;
        }

        args.IsEnabled = false;
        args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Not enough Harmony.");
        return true;
    }

    private void SelectTreeSymbolConsequence( string upgrade, int upgradeCost)
    {
        foreach (var element in TreeSymbols)
        {
            if(Hero.MainHero.HasAttribute(element))
            {
                Hero.MainHero.RemoveAttribute(element);
            }
        }
        
        Hero.MainHero.AddAttribute(upgrade); // needed
        
        
        Hero.MainHero.AddCultureSpecificCustomResource(-upgradeCost);
        
        GameMenu.ActivateGameMenu(_currentMenu);
        
        
        
    }

    private void AddWorldRootMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_roots_menu", "World Roots : The world roots create a braid of pathways with the oaks Roots. With them the Asrai are allowed to travel world. Help to restablish the roots and travel to roots ends in the old World", OakOfAgeMenuInit);
        
        MBTextManager.SetTextVariable("ROOTUNLOCKCOST",RootUnlockCost);
        MBTextManager.SetTextVariable("ROOTTRAVELUPGRADE", RootTravelCostReductionUpgradeCost);
        MBTextManager.SetTextVariable("ROOTRETURNUPGRADE",RootTravelBackUpgradeCost);
        MBTextManager.SetTextVariable("ROOTHEALUPGRADE",RootHealUpgradeCost);
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock Pathway to the Forest of Arden. {ROOTUNLOCKCOST}{FORESTHARMONY}",args => 
            RootUpgradeCondition(args,RootUnlockCost, "WorldRootTarget_Arden"),_ => UnlockOakUpgrade("WorldRootTarget_Arden",RootUnlockCost));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to the forest of Arden. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",args => 
            RootAcessibleCondition(args, TravelCost, "WorldRootTarget_Arden"),_ => RootTravelConsequence(ArdenLocation));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock the pathway to Laurelorn. {ROOTUNLOCKCOST}{FORESTHARMONY}",args => 
            RootUpgradeCondition(args,RootUnlockCost, "WorldRootTarget_Laurelorn"),_ => UnlockOakUpgrade("WorldRootTarget_Laurelorn",RootUnlockCost));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to Laurelorn. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",args => 
            RootAcessibleCondition(args, TravelCost, "WorldRootTarget_Laurelorn"),_ => RootTravelConsequence(LaurelornLocation));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock the pathway to the Gryphenwood. {ROOTUNLOCKCOST}{FORESTHARMONY}",args => 
            RootUpgradeCondition(args,RootUnlockCost, "WorldRootTarget_GryphenWood"),_ => UnlockOakUpgrade("WorldRootTarget_GryphenWood",RootUnlockCost));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to the Gryphenwood. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",args => 
            RootAcessibleCondition(args, TravelCost, "WorldRootTarget_GryphenWood"),_ => RootTravelConsequence(GryphenWoodLocation));
        
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Reduce the travel cost. {ROOTTRAVELUPGRADE}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootTravelBackUpgradeCost,"WETravelCostUpgrade", true),_ => UnlockOakUpgrade("WETravelCostUpgrade", RootTravelCostReductionUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}", args => 
            RootUpgradeCondition(args, RootTravelBackUpgradeCost, "WETravelBackUpgrade", true), _ => UnlockOakUpgrade("WETravelBackUpgrade",RootTravelBackUpgradeCost));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Allow that all troops and heroes are healed upon using the world roots. {ROOTHEALUPGRADE}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootTravelBackUpgradeCost,"WETravelHealUpgrade", true),_ => UnlockOakUpgrade("WETravelHealUpgrade",RootHealUpgradeCost));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "branchMenu_leave", "Leave...",
            delegate(MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            },
            delegate
            {
                GameMenu.SwitchToMenu("oak_of_ages_menu");
            });
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

       
        if ( HasUnlockedUpgrade("WETravelCostUpgrade"))
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
    




    private void AddBranchesOfTheOakMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_branches_menu", "Branches of The Oak", null);
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Increase party Size. {PARTYSIZEUPGRADECOST}{FORESTHARMONY}",args => 
            BranchUpgradeCondition(args,PartySizeUpgradeCost, "WEPartySizeUpgrade"),_ => UpgradeConsequence(PartySizeUpgradeCost,"WEPartySizeUpgrade" ));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_B", "Increase maximum health. {HEALTHUPGRADECOST}{FORESTHARMONY}",
            args => BranchUpgradeCondition(args,HealthUpgradeCost, "WEHealthUpgrade"),_ => UpgradeConsequence(HealthUpgradeCost,"WEHealthUpgrade" ));
        
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
            delegate
            {
                GameMenu.SwitchToMenu("oak_of_ages_menu");
            });
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
        
        var attribute = upgradeList.FirstOrDefault(x => !HasUnlockedUpgrade(x));
        if (attribute == null) return;
        var total = cost * (1 + count);
        
        UnlockOakUpgrade(attribute,total);
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
            var text = $"{{=tor_custom_settlement_we_party_size_info_str}}Not enough Harmony. Requires {{{textVariable}}}{{FORESTHARMONY}} for the next upgrade.";
            args.Tooltip = new TextObject(text);
            args.IsEnabled = false;
        }

        return true;
    }
    
    private bool UnlockedAllUpgradesOfType( out int upgradeCount, string upgradeType)
    {
        List<string> targetList = new List<string>();
        foreach (var list in _upgrades)
        {
            if (list.Any(x => x.Contains(upgradeType)))
            {
                targetList = list;
                break;
            }
        }
        upgradeCount = 0;
        if (targetList.Count == 0)
        {
            return true;
        }
        
        foreach (var attribute in targetList)
        {
            if (!HasUnlockedUpgrade(attribute))
            {
                return false;
            }
            upgradeCount++;
        }
        
        return true;
    }
}