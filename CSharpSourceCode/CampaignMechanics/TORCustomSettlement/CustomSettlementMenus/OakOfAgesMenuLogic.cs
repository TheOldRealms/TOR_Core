using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    private static readonly List<TreeSymbol> TreeSymbols = 
    [
        new TreeSymbol()
        {
                upgradeID = "WEKithbandSymbol",
                cost = TreeSymbolUnlockCosts,
                minimumLevel = 0,
                UpgradeText =new TextObject("Decipher the Sign of the Kithband. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
                ApplyText = new TextObject("Apply the Sign of the Kithband. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
        },
        new TreeSymbol()
        {
            upgradeID = "WEWardancerSymbol",
            cost = TreeSymbolUnlockCosts,
            minimumLevel = 0,
            UpgradeText =new TextObject("Decipher the Sign of the Wardancer. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Sign of the Wardancer. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
        },
        new TreeSymbol()
        {
            upgradeID = "WETreekinSymbol",
            cost = TreeSymbolUnlockCosts,
            minimumLevel = 0,
            UpgradeText =new TextObject("Decipher the Sign of the Treekin. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Sign of the Treekin. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
        },
        new TreeSymbol()
        {
            upgradeID = "WEOrionSymbol",
            cost = TreeSymbolUnlockCosts,
            minimumLevel = 15,
            UpgradeText =new TextObject("Decipher the Sign of Orion. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Sign of Orion. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
            
        },
        new TreeSymbol()
        {
            upgradeID = "WEArielSymbol",
            cost = TreeSymbolUnlockCosts,
            minimumLevel = 15,
            UpgradeText =new TextObject("Decipher the Sign of Ariel. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Sign of Ariel. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
        },
        new TreeSymbol()
        {
            upgradeID = "WEWandererSymbol",
            cost = TreeSymbolUnlockCosts,
            minimumLevel = 15,
            UpgradeText =new TextObject("Decipher the Sign of the Wanderer.{TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Sign of the Wanderer. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
        },
        new TreeSymbol()
        {
            upgradeID = "WEDurthuSymbol",
            cost = TreeSymbolUnlockCosts,
            minimumLevel = 20,
            UpgradeText =new TextObject("Decipher the Sign of Durthu.{TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Sign of Durthu. {TREESYMBOLCHANGECOST}{FORESTHARMONY}")
        }
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
        
        starter.AddGameMenuOption("oak_of_ages_menu", "symbols", "Tree Symbols",null, delegate
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
        starter.AddGameMenu("oak_of_ages_tree_symbols_menu", "Tree Symbols: Choose one Symbol activated for your party. The Symbols provide strong enhancements, yet they will also provide strong disadvantages. Choose wisely, only one Symbol can be active at once.", OakOfAgeMenuInit);
        
        MBTextManager.SetTextVariable("TREESYMBOLCHANGECOST",TreeSymbolChangeCost);
        MBTextManager.SetTextVariable("TREESYMBOLUNLOCKCOST",TreeSymbolUnlockCosts);
        MBTextManager.SetTextVariable("TREESYMBOLFREEUPGRADE",TreeSymbolUpgradeNoCost);
        MBTextManager.SetTextVariable("TREESYMBOLCHANGEDAILY",TreeSymbolUpgradeDaily);

        var index = 0;
        foreach (var symbol in TreeSymbols)
        {
            starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", $"treeSymbolMenu_{index}_unlock", symbol.UpgradeText.Value,
                args => UnlockTreeSymbolCondition(args,symbol.upgradeID,symbol.cost, symbol.minimumLevel),_ => UnlockOakUpgrade(symbol.upgradeID,symbol.cost));
            
            starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", $"treeSymbolMenu_{index}", symbol.ApplyText.Value,
                args => SelectTreeSymbolCondition(args,symbol.upgradeID,TreeSymbolChangeCost,true),_ => SelectTreeSymbolConsequence(symbol.upgradeID,TreeSymbolChangeCost));
            index++;
        }
       
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_H", "Magical chalk. {TREESYMBOLFREEUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args,"WESymbolReduceCosts",TreeSymbolUpgradeNoCost,
                new TextObject("Remove Symbol change costs.{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WESymbolReduceCosts", TreeSymbolUpgradeNoCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_I", "Decorated Bark. {TREESYMBOLCHANGEDAILY}{FORESTHARMONY}",
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
        var toolTipText = toolTipDescription.Value;
        var failreasonStringBuilder = new StringBuilder();

        if (upgrade == "")
        {
            failreasonStringBuilder.Append("{newline}No further upgrades available");
            args.IsEnabled = false;
        }
        args.Tooltip = toolTipDescription;
        if (HasUnlockedUpgrade(upgrade))
        {
            failreasonStringBuilder.Append("{newline}Upgrade has been already unlocked");
            args.IsEnabled = false;
        }
        
        
        if (minimumLevel > 0)
        {
            if (Hero.MainHero.Level < minimumLevel)
            {
                args.IsEnabled = false;
                MBTextManager.SetTextVariable("UPGRADEFAILEDREASON","Requires level " +minimumLevel);
                failreasonStringBuilder.Append("{newline}Requires level " +minimumLevel);
            }
        }

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < cost)
        {
            MBTextManager.SetTextVariable("UPGRADEFAILEDREASON","Not enough harmony");
            failreasonStringBuilder.Append("{newline}Not enough harmony");
            args.IsEnabled = false;
            
        }
        
        
        var resultText = new TextObject(toolTipText);
        resultText.SetTextVariable("UPGRADEFAILEDREASON",failreasonStringBuilder.ToString());
        args.Tooltip =  resultText;
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
            if(Hero.MainHero.HasAttribute(element.upgradeID))
            {
                Hero.MainHero.RemoveAttribute(element.upgradeID);
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
        MBTextManager.SetTextVariable("WORLDROOTTRAVELCOST", TravelCost);
        
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
        
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Internalize the root pathways. {ROOTTRAVELUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelCostUpgrade", RootTravelCostReductionUpgradeCost, new TextObject("Reduce the travel cost.{newline}{UPGRADEFAILEDREASON}")),_ => UnlockOakUpgrade("WETravelCostUpgrade", RootTravelCostReductionUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}", args => 
            DefaultUnlockOakUpgradeCondition(args, "WETravelBackUpgrade",RootTravelBackUpgradeCost, new TextObject("Return to the Oak from the root exit.{UPGRADEFAILEDREASON}") ), _ => UnlockOakUpgrade("WETravelBackUpgrade",RootTravelBackUpgradeCost));
        
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Healing Aura of roots. {ROOTHEALUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args,"WETravelHealUpgrade", RootTravelBackUpgradeCost, new TextObject("All troops and heroes are healed upon using the world roots{newline}{UPGRADEFAILEDREASON}")),_ => UnlockOakUpgrade("WETravelHealUpgrade",RootHealUpgradeCost));
        
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
        if (HasUnlockedUpgrade(upgradeId))
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
        if (!HasUnlockedUpgrade(upgradeId)) return false;
       
        if ( HasUnlockedUpgrade("WETravelCostUpgrade"))
        {
            cost /= 2;
        }
        
        MBTextManager.SetTextVariable("WORLDROOTTRAVELCOST",cost);
        
        
        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= cost)
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

        var cost = TravelCost;
        if (HasUnlockedUpgrade("WETravelCostUpgrade"))
        {
            cost /= 2;
        }
        Hero.MainHero.AddCultureSpecificCustomResource(-cost);
        
        if (HasUnlockedUpgrade("WETravelHealUpgrade"))
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
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Build Outposts.{PARTYSIZEUPGRADECOST}{FORESTHARMONY}",args => 
            DefaultUnlockOakUpgradeCondition(args,GetCurrentUpdate("WEPartySizeUpgrade", out int numberOfUpgrades),PartySizeUpgradeCost, new TextObject("Increase party Size by 10%.{newline}{UPGRADEFAILEDREASON}"), 4*numberOfUpgrades),_ => UnlockOakUpgrade(GetCurrentUpdate("WEPartySizeUpgrade", out int numberOfUpgrades),PartySizeUpgradeCost * (1+numberOfUpgrades)));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_B", "Strong branches. {HEALTHUPGRADECOST}{FORESTHARMONY}", 
            args => DefaultUnlockOakUpgradeCondition(args,
                GetCurrentUpdate("WEHealthUpgrade", out int numberOfUpgrades),HealthUpgradeCost, new TextObject("Increase maximum health by 10%.{UPGRADEFAILEDREASON}"), 4*numberOfUpgrades),
            _ => UnlockOakUpgrade(GetCurrentUpdate("WEHealthUpgrade", out int numberOfUpgrades),HealthUpgradeCost * (1+numberOfUpgrades)));
        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_C", "Thriving Leaves. {GAINUPGRADECOST}{FORESTHARMONY}",args =>
        DefaultUnlockOakUpgradeCondition(args,GetCurrentUpdate("WEGainUpgrade", out int numberOfUpgrades),GainUpgradeCost, new TextObject("Increase the daily harmony gain by 15.{UPGRADEFAILEDREASON}"), 
            4*numberOfUpgrades),_ => UnlockOakUpgrade(GetCurrentUpdate("WEGainUpgrade", out int numberOfUpgrades),GainUpgradeCost * (1+numberOfUpgrades)));

        
        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_D", " Troop harmony upkeep  reduction. {UPKEEPUPGRADECOST}{FORESTHARMONY}", args => 
        DefaultUnlockOakUpgradeCondition(args,GetCurrentUpdate("WEUpkeepUpgrade", out int numberOfUpgrades),TroopUpkeepUpgradeCost, 
            new TextObject("Increase the daily harmony gain by 15.{UPGRADEFAILEDREASON}"), 4*numberOfUpgrades),_ => 
            UnlockOakUpgrade(GetCurrentUpdate("WEUpkeepUpgrade", out int numberOfUpgrades),TroopUpkeepUpgradeCost * (1+numberOfUpgrades)));
        
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


    private string GetCurrentUpdate(string upgradeCategory, out int upgradeCount)
    {
        List<string> targetList = new List<string>();
        foreach (var list in _upgrades)
        {
            if (list.Any(x => x.Contains(upgradeCategory)))
            {
                targetList = list;
                break;
            }
        }
        upgradeCount = 0;
        
        foreach (var attribute in targetList)
        {
            if (!HasUnlockedUpgrade(attribute))
            {
                return attribute;
            }
            upgradeCount++;
        }

        if (targetList.Count == upgradeCount)
        {
            return "";
        }

        throw new Exception("Upgrade could not be found");
        return "";
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



    public class TreeSymbol()
    {
        public int cost { get; set; }
        public string upgradeID { get; set; }
        public int minimumLevel { get; set; }
        
        public TextObject UpgradeText= new TextObject();
        
        public TextObject ApplyText= new TextObject();
    }
}