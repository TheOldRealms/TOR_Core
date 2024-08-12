using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class OakOfAgesMenuLogic : TORBaseSettlementMenuLogic
{
    private const int TreemanPrice = 800;
    private const int PartySizeUpgradeCost = 100;
    private const int HealthUpgradeCost = 125;
    private const int GainUpgradeCost = 150;
    private const int TroopUpkeepUpgradeCost = 200;

    private const int TravelCost = 100;
    private const int RootUnlockCost = 200;
    private const int RootTravelCostReductionUpgradeCost = 500;
    private const int RootTravelBackUpgradeCost = 750;
    private const int RootHealUpgradeCost = 1000;
    private readonly Vec2 _ardenLocation = new(945.4077f, 1111.009f);
    private readonly Vec2 _laurelornLocation = new(1239.391f, 1276.938f);
    private readonly Vec2 _gryphenWoodLocation = new(1606.698f, 1133.905f);
    private readonly Vec2 _athelLorenLocation = new(1238.435f, 778.6498f);
    private readonly Vec2 _maisonTaalLocation = new(1228.051f, 973.7142f);

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
        "WEUpkeepUpgrade3"
    ];

    private static readonly List<TreeSymbol> TreeSymbols =
    [
        new TreeSymbol()
        {
            UpgradeId = "WEKithbandSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 0,
            UpgradeText = new TextObject("Decipher the Symbol of the Kithband. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Kithband. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject(
                "Increase your party size by 50% , Forestharmony gain is decreased by 25% and wages are increased by 15%.{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEWardancerSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 0,
            UpgradeText = new TextObject("Decipher the Symbol of the Wardancer. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Wardancer. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject(
                "The Player and companion gain 25% additional health, and recover wounds 25% faster. All troops heal 25% slower and Forest Harmony is gained 25% slower.{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WETreekinSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 0,
            UpgradeText = new TextObject("Decipher the Symbol of the Treekin. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Treekin. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject("Treekin Forest Harmony upkeep is 50% lower. Wages of elves are 25% higher.{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEOrionSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 15,
            UpgradeText = new TextObject("Decipher the Symbol of Orion. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of Orion. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject("Wages of elves are reduced by 50%. Forest Spirit unit upkeep is doubled.{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEArielSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 15,
            UpgradeText = new TextObject("Decipher the Symbol of Ariel. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of Ariel. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject(
                "Winds capacity is increased by 25%. Regenerate +1 more Winds in Athel Loren. Upkeepcosts are increased by 50%.{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEWandererSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 15,
            UpgradeText = new TextObject("Decipher the Symbol of the Wanderer.{TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Wanderer. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject(
                "Low Forest Harmony has no negative Effect. Forest harmony gain is reduced by 50% and upkeep is increased by 50%.{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEDurthuSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 20,
            UpgradeText = new TextObject("Decipher the Symbol of Durthu.{TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of Durthu. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject(
                "Treeman capacity is increased by 50%. Health is increased by 25%. Party size is decreased by 25%. -20% Fire resistance.{UPGRADEFAILEDREASON}")
        }
    ];


    private readonly List<List<string>> _upgrades;

    public OakOfAgesMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
        _upgrades =
        [
            PartyUpgradeAttributes, MaximumHealthUpgradeAttributes, CustomResourceGainUpgrades, UpkeepReductionUpgrades
        ];
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

        var text = component.IsActive
            ? GameTexts.FindText("customsettlement_intro", settlement.StringId)
            : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);

        MBTextManager.SetTextVariable("FORESTHARMONY", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
        MBTextManager.SetTextVariable("FORESTHARMONY1", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
    }


    public void AddOakOfAgeMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_menu", "{LOCATION_DESCRIPTION}", OakOfAgeMenuInit);
        
        
        starter.AddGameMenuOption("oak_of_ages_menu", "tree_spirits", "Tree Spirits", _ => IsAsrai(), delegate
        {
            _currentMenu = "oak_of_ages_tree_spirits_menu";
            GameMenu.SwitchToMenu("oak_of_ages_tree_spirits_menu");
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "branch", "Branches of the Oak", _ => IsAsrai(), delegate
        {
            _currentMenu = "oak_of_ages_branches_menu";
            GameMenu.SwitchToMenu(_currentMenu);
        }, false, 4, false);

        starter.AddGameMenuOption("oak_of_ages_menu", "roots", "World roots of the Oak", _ => IsAsrai(), delegate
        {
            _currentMenu = "oak_of_ages_roots_menu";
            GameMenu.SwitchToMenu(_currentMenu);
        }, false, 4, false);

        starter.AddGameMenuOption("oak_of_ages_menu", "symbols", "Tree Symbols", _ => IsAsrai(), delegate
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
        AddWorldRootsMenus(starter);
        AddTreeSpiritMenu(starter);
    }

    private void AddTreeSpiritMenu(CampaignGameStarter starter)
    {
        
        starter.AddGameMenu("oak_of_ages_tree_spirits_menu",
            "Communicate with the Tree spirits around the oak.",
            OakOfAgeMenuInit);
        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpirits_B", "{tor_custom_settlement_menu_cursed_site_ghost_str}Commune with the forest. 100 {FORESTHARMONY}", 
             (args) => CanBindTreeSpirits() && CanBindDryads(args), _ =>
             {
                 Hero.MainHero.AddCustomResource("ForestHarmony",-100);
                 GameMenu.SwitchToMenu("oak_of_ages_tree_spirits_menu_bind_dryads");
             });

        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpirits_B", "Rouse Treemen. 800 {FORESTHARMONY}", args => CanBindTreeSpirits()&& CanBindTreeman(args),
            AddTreemen);
        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpirits_C", "Relief Treespirits",null, (args) 
                => PartyScreenManager.OpenScreenAsQuest(TroopRoster.CreateDummyTroopRoster(), new TextObject("Donated Spirits"),
                    500,0,null,TranferCompleted, IsTransferableTreeSpirit,null),
            false);

        starter.AddWaitGameMenu("oak_of_ages_tree_spirits_menu_bind_dryads", "{=tor_custom_settlement_cursed_site_ghosts_progress_str}Performing binding ritual...",
            delegate (MenuCallbackArgs args)
            {
                _startWaitTime = CampaignTime.Now;
                numberOfTroopsFromInteraction = 0;
                PlayerEncounter.Current.IsPlayerWaiting = true;
                args.MenuContext.GameMenu.StartWait();
            }, null, DryadConsequence,
            BindingTick,
            GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 8f, GameMenu.MenuFlags.None, null);
        
        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSymbolMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate { GameMenu.SwitchToMenu("oak_of_ages_menu"); });
        
        starter.AddGameMenu("dryads_result","You were able to bind {NUMBEROFTROOPS} dryads to your party",null);
        starter.AddGameMenuOption("dryads_result", "dryads_result_leave", "back...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate { GameMenu.SwitchToMenu("oak_of_ages_tree_spirits_menu"); });
        
        
        void TranferCompleted(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftprisonroster, PartyBase rightownerparty,
            TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            if(fromcancel) return;
            var gainedSpiritHarmony = 0;
            foreach (var element in leftmemberroster.GetTroopRoster())
            {
                if (element.Character.StringId.Contains("dryad"))
                {
                    gainedSpiritHarmony += 10 * element.Number;
                }
                if (element.Character.StringId.Contains("treeman"))
                {
                    gainedSpiritHarmony += 100 * element.Number;
                }
            }
        
            Hero.MainHero.AddCultureSpecificCustomResource(gainedSpiritHarmony);
        }

        bool CanBindTreeSpirits()
        {
            var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();
            
            
            
            return heroes.Any(hero => hero.IsSpellSinger());
        }

        bool CanBindTreeman(MenuCallbackArgs args)
        {
            args.IsEnabled = Hero.MainHero.GetCultureSpecificCustomResourceValue() >= TreemanPrice;

            return true;
        }
        
        bool CanBindDryads(MenuCallbackArgs args)
        {
            args.IsEnabled = Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 100;

            return true;
        }
        
        bool IsTransferableTreeSpirit(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftownerparty)
        {
            if (type != PartyScreenLogic.TroopType.Member) return false;
            if (character.IsHero) return false;
            return character.Culture.StringId == TORConstants.Cultures.ASRAI && character.Race != FaceGen.GetRaceOrDefault("elf");
        }

        void AddTreemen(MenuCallbackArgs args)
        {
            
        }
        
        void BindingTick(MenuCallbackArgs args, CampaignTime dt)
        {
            float progress = args.MenuContext.GameMenu.Progress;
            int diff = (int)_startWaitTime.ElapsedHoursUntilNow;
            if (diff > 0)
            {
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
                if (args.MenuContext.GameMenu.Progress != progress)
                {
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_we_dryad");
                    var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                    int raisePower = Math.Max(1, (int)Hero.MainHero.GetExtendedInfo().SpellCastingLevel);
                    var count = MBRandom.RandomInt(1, 3);
                    
                    count *= raisePower;
                    if (freeSlots > 0)
                    {
                        if (freeSlots < count) count = freeSlots;
                        MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                        CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, Settlement.CurrentSettlement, null, troop, count);
                        numberOfTroopsFromInteraction += count;
                    }
                    
                    GameTexts.SetVariable("NUMBEROFTROOPS",numberOfTroopsFromInteraction);
                }
            }
        }
        
        void DryadConsequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            args.MenuContext.GameMenu.EndWait();
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
            GameMenu.SwitchToMenu("dryads_result");
        }
    }

   


    private bool IsAsrai()
    {
        return Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI;
    }

    private void AddTreeSymbolMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_tree_symbols_menu",
            "Tree Symbols: Choose one Symbol activated for your party. The Symbols provide strong enhancements, yet they will also provide strong disadvantages. Choose wisely, only one Symbol can be active at once.",
            OakOfAgeMenuInit);

        MBTextManager.SetTextVariable("TREESYMBOLCHANGECOST", TreeSymbolChangeCost);
        MBTextManager.SetTextVariable("TREESYMBOLUNLOCKCOST", TreeSymbolUnlockCosts);
        MBTextManager.SetTextVariable("TREESYMBOLFREEUPGRADE", TreeSymbolUpgradeNoCost);
        MBTextManager.SetTextVariable("TREESYMBOLCHANGEDAILY", TreeSymbolUpgradeDaily);

        var index = 0;
        foreach (var symbol in TreeSymbols)
        {
            starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", $"treeSymbolMenu_{index}_unlock", symbol.UpgradeText.Value,
                args => UnlockTreeSymbolCondition(args, symbol.UpgradeId, symbol.ToolTipText, symbol.Cost, symbol.MinimumLevel),
                _ => UnlockOakUpgrade(symbol.UpgradeId, symbol.Cost));

            starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", $"treeSymbolMenu_{index}", symbol.ApplyText.Value,
                args => SelectTreeSymbolCondition(args, symbol.UpgradeId, symbol.ToolTipText, TreeSymbolChangeCost, true),
                _ => SelectTreeSymbolConsequence(symbol.UpgradeId, TreeSymbolChangeCost));
            index++;
        }

        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_H", "Remove symbol", _ => RemoveSymbolCondition(),
            _ => RemoveSymbolConsequence(true));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_H", "Magical chalk. {TREESYMBOLFREEUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WESymbolReduceCosts", TreeSymbolUpgradeNoCost,
                new TextObject("Remove Symbol change costs.{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WESymbolReduceCosts", TreeSymbolUpgradeNoCost));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_I", "Decorated Bark. {TREESYMBOLCHANGEDAILY}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WESymbolsChangeCycle", TreeSymbolUpgradeDaily,
                new TextObject("Tree signs can be changed every day instead of every week.{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WESymbolsChangeCycle", TreeSymbolUpgradeDaily));

        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate { GameMenu.SwitchToMenu("oak_of_ages_menu"); });
    }

    private bool RemoveSymbolCondition()
    {
        foreach (var symbol in TreeSymbols)
            if (Hero.MainHero.HasAttribute(symbol.UpgradeId))
                return true;

        return false;
    }

    private void RemoveSymbolConsequence(bool reload = false)
    {
        foreach (var element in TreeSymbols)
            if (Hero.MainHero.HasAttribute(element.UpgradeId))
                Hero.MainHero.RemoveAttribute(element.UpgradeId);

        if (reload) GameMenu.ActivateGameMenu(_currentMenu);
    }

    private bool DefaultUnlockOakUpgradeCondition(MenuCallbackArgs args, string upgrade, int cost, TextObject toolTipDescription = null,
        int minimumLevel = 0)
    {
        if(Hero.MainHero.Culture.StringId != TORConstants.Cultures.ASRAI) return false;
        
        var toolTipText = toolTipDescription.Value;
        var failreasonStringBuilder = new StringBuilder();

        if (upgrade == "")
        {
            failreasonStringBuilder.Append("{newline}No further upgrades available");
            args.IsEnabled = false;
        }

        args.Tooltip = toolTipDescription;


        if (minimumLevel > 0)
            if (Hero.MainHero.Level < minimumLevel)
            {
                args.IsEnabled = false;
                MBTextManager.SetTextVariable("UPGRADEFAILEDREASON", "Requires level " + minimumLevel);
                failreasonStringBuilder.Append("{newline}Requires level " + minimumLevel);
            }

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < cost)
        {
            failreasonStringBuilder.Append("{newline}Not enough harmony");
            args.IsEnabled = false;
        }

        if (HasUnlockedUpgrade(upgrade))
        {
            failreasonStringBuilder.Clear();
            failreasonStringBuilder.Append("{newline}Upgrade has been already unlocked");
            args.IsEnabled = false;
        }


        var resultText = new TextObject(toolTipText);
        resultText.SetTextVariable("UPGRADEFAILEDREASON", failreasonStringBuilder.ToString());
        args.Tooltip = resultText;
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


    private bool UnlockTreeSymbolCondition(MenuCallbackArgs args, string upgrade, TextObject toolTipText, int upgradeCost, int minimumLevel = 0)
    {
        if (HasUnlockedUpgrade(upgrade)) return false;
        if (minimumLevel > 0)
            if (Hero.MainHero.Level < minimumLevel)
                return false;

        var failreasonStringBuilder = new StringBuilder();

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < upgradeCost)
        {
            failreasonStringBuilder.Append("{newline}Not enough Harmony.");
            args.IsEnabled = false;
        }

        var resultText = toolTipText;
        resultText.SetTextVariable("UPGRADEFAILEDREASON", failreasonStringBuilder.ToString());
        args.Tooltip = resultText;
        return true;
    }


    private bool SelectTreeSymbolCondition(MenuCallbackArgs args, string upgrade, TextObject toolTipText, int upgradeCost, bool showAnyway)
    {
        var failreasonStringBuilder = new StringBuilder();

        if (Hero.MainHero.HasAttribute(upgrade))
        {
            if (showAnyway)
            {
                failreasonStringBuilder.Clear();
                failreasonStringBuilder.Append("{newline}Treesymbol is currently activ");
                args.IsEnabled = false;
            }
            else
            {
                return false;
            }
        }

        if (!HasUnlockedUpgrade(upgrade)) return false;

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < upgradeCost)
        {
            args.IsEnabled = false;
            failreasonStringBuilder.Append("{newline}Not enough Harmony.");
        }

        var resultText = toolTipText;
        resultText.SetTextVariable("UPGRADEFAILEDREASON", failreasonStringBuilder.ToString());
        args.Tooltip = resultText;

        return true;
    }

    private void SelectTreeSymbolConsequence(string upgrade, int upgradeCost)
    {
        RemoveSymbolConsequence();
        Hero.MainHero.AddAttribute(upgrade); // needed
        Hero.MainHero.AddCultureSpecificCustomResource(-upgradeCost);
        GameMenu.ActivateGameMenu(_currentMenu);
    }

    private void AddWorldRootMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_roots_menu",
            "World Roots : The world roots create a braid of pathways with the oaks Roots. With them the Asrai are allowed to travel world. Help to restablish the roots and travel to roots ends in the old World",
            OakOfAgeMenuInit);

        MBTextManager.SetTextVariable("ROOTUNLOCKCOST", RootUnlockCost);
        MBTextManager.SetTextVariable("ROOTTRAVELUPGRADE", RootTravelCostReductionUpgradeCost);
        MBTextManager.SetTextVariable("ROOTRETURNUPGRADE", RootTravelBackUpgradeCost);
        MBTextManager.SetTextVariable("ROOTHEALUPGRADE", RootHealUpgradeCost);
        MBTextManager.SetTextVariable("WORLDROOTTRAVELCOST", TravelCost);

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock",
            "Unlock Pathway to the Forest of Arden. {ROOTUNLOCKCOST}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootUnlockCost, "WorldRootTarget_Arden"),
            _ => UnlockOakUpgrade("WorldRootTarget_Arden", RootUnlockCost));
        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel",
            "Travel to the forest of Arden. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",
            args => RootAccessibleCondition(args, TravelCost, "WorldRootTarget_Arden"), _ => RootTravelConsequence(_ardenLocation));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock", "Unlock the pathway to Laurelorn. {ROOTUNLOCKCOST}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootUnlockCost, "WorldRootTarget_Laurelorn"),
            _ => UnlockOakUpgrade("WorldRootTarget_Laurelorn", RootUnlockCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to Laurelorn. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",
            args => RootAccessibleCondition(args, TravelCost, "WorldRootTarget_Laurelorn"), _ => RootTravelConsequence(_laurelornLocation));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_Unlock",
            "Unlock the pathway to the Gryphenwood. {ROOTUNLOCKCOST}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootUnlockCost, "WorldRootTarget_GryphenWood"),
            _ => UnlockOakUpgrade("WorldRootTarget_GryphenWood", RootUnlockCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Travel to the Gryphenwood. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",
            args => RootAccessibleCondition(args, TravelCost, "WorldRootTarget_GryphenWood"), _ => RootTravelConsequence(_gryphenWoodLocation));


        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Internalize the root pathways. {ROOTTRAVELUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelCostUpgrade", RootTravelCostReductionUpgradeCost,
                new TextObject("Reduce the travel cost.{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelCostUpgrade", RootTravelCostReductionUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel",
            "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelBackUpgrade", RootTravelBackUpgradeCost,
                new TextObject("Return to the Oak from the root exit.{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelBackUpgrade", RootTravelBackUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_A_travel", "Healing Aura of roots. {ROOTHEALUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelHealUpgrade", RootTravelBackUpgradeCost,
                new TextObject("All troops and heroes are healed upon using the world roots{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelHealUpgrade", RootHealUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "branchMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate { GameMenu.SwitchToMenu("oak_of_ages_menu"); });
    }

    private bool RootUpgradeCondition(MenuCallbackArgs args, int upgradeCost, string upgradeId, bool showAnyway = false)
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

    private bool RootAccessibleCondition(MenuCallbackArgs args, int travelCost, string upgradeId)
    {
        var cost = travelCost;
        if (!HasUnlockedUpgrade(upgradeId)) return false;

        if (HasUnlockedUpgrade("WETravelCostUpgrade")) cost /= 2;

        MBTextManager.SetTextVariable("WORLDROOTTRAVELCOST", cost);


        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= cost)
        {
            return true;
        }

        args.Tooltip = new TextObject("{=tor_custom_settlement_we_party_size_info_str}Not enough Harmony for traveling.");
        args.IsEnabled = false;
        return true;
    }

    private void RootTravelConsequence(Vec2 location, bool hasTravelCost = true)
    {
        LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
        GameMenu.ExitToLast();
        Campaign.Current.MainParty.Position2D = MobilePartyHelper.FindReachablePointAroundPosition(location, 0f);

        if (hasTravelCost)
        {
            var cost = TravelCost;
            if (HasUnlockedUpgrade("WETravelCostUpgrade")) cost /= 2;
            Hero.MainHero.AddCultureSpecificCustomResource(-cost);
        }

        if (HasUnlockedUpgrade("WETravelHealUpgrade"))
        {
            var heroes = MobileParty.MainParty.GetMemberHeroes();
            foreach (var hero in heroes) hero.Heal(1000);

            var list = MobileParty.MainParty.MemberRoster.GetTroopRoster();
            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];
                var woundedNumber = element.WoundedNumber;

                MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(index, 0, -woundedNumber);
            }
        }
    }

    private void AddWorldRootsMenus(CampaignGameStarter starter)
    {
        MBTextManager.SetTextVariable("ROOTRETURNUPGRADE", RootTravelBackUpgradeCost);
        starter.AddGameMenu("worldroots_menu", "{LOCATION_DESCRIPTION}", WorldRootsMenuInit);
        
        
        starter.AddGameMenuOption("worldroots_menu", "travel_eonir", "{tor_custom_settlement_menu_leave_str}Travel to Maisontaal",
            args => TravelEonirCondition(args) && Hero.MainHero.CurrentSettlement.StringId == "worldroot_02", (MenuCallbackArgs args) => RootTravelConsequence(_maisonTaalLocation, false), true);
        
        starter.AddGameMenuOption("worldroots_menu", "travel_eonir", "{tor_custom_settlement_menu_leave_str}Travel to Laurelorn",
            args => TravelEonirCondition(args) && false, (MenuCallbackArgs args) => RootTravelConsequence(_laurelornLocation, false), true);

        starter.AddGameMenuOption("worldroots_menu", "travel_back", "{tor_custom_settlement_menu_leave_str}Travel back to Athel Loren...",
            args => TravelBackCondition(args), (MenuCallbackArgs args) => RootTravelConsequence(_athelLorenLocation, false), true);

        starter.AddGameMenuOption("worldroots_menu", "rootMenu_A_travel",
            "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelBackUpgrade", RootTravelBackUpgradeCost,
                new TextObject("Return to the Oak from the root exit.{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelBackUpgrade", RootTravelBackUpgradeCost));

        starter.AddGameMenuOption("worldroots_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
    }

    private bool TravelEonirCondition(MenuCallbackArgs args)
    {
        if(Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR)
        {
            return false;
        }

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < 50)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("Not enough Favor");
            return true;
        }

        return true;
    }

    private bool TravelBackCondition(MenuCallbackArgs args)
    {
        if(Hero.MainHero.Culture.StringId != TORConstants.Cultures.ASRAI)
        {
            return false;
        }
        
        if (HasUnlockedUpgrade("WETravelBackUpgrade"))
        {
            return true;
        }
        else
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("You can't travel back without the required enhancements");
        }

        return true;
    }

    private void WorldRootsMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TORBaseSettlementComponent;
        var text = component.IsActive
            ? GameTexts.FindText("customsettlement_intro", settlement.StringId)
            : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private void AddBranchesOfTheOakMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_branches_menu", "Branches of The Oak", null);

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Build Outposts. {PARTYSIZEUPGRADECOST}{FORESTHARMONY}",
            args =>
            {
                var text = GetCurrentUpdate("WEPartySizeUpgrade", out var numberOfUpgrades);
                GameTexts.SetVariable("PARTYSIZEUPGRADECOST",PartySizeUpgradeCost * (numberOfUpgrades +1) );
                return DefaultUnlockOakUpgradeCondition(args, text , PartySizeUpgradeCost,
                    new TextObject("Increase party Size by 10%.{newline}{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpdate("WEPartySizeUpgrade", out var numberOfUpgrades), PartySizeUpgradeCost * (1 + numberOfUpgrades)));

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_B", "Strong branches. {HEALTHUPGRADECOST}{FORESTHARMONY}",
            args =>
            {
                var text = GetCurrentUpdate("WEHealthUpgrade", out var numberOfUpgrades);
                GameTexts.SetVariable("HEALTHUPGRADECOST",HealthUpgradeCost *(numberOfUpgrades + 1) );
                return DefaultUnlockOakUpgradeCondition(args, text, HealthUpgradeCost,
                    new TextObject("Increase maximum health by 10%.{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpdate("WEHealthUpgrade", out var numberOfUpgrades), HealthUpgradeCost * (1 + numberOfUpgrades)));

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_C", "Thriving Leaves. {GAINUPGRADECOST}{FORESTHARMONY}",
            args =>
            {
                var text = GetCurrentUpdate("WEGainUpgrade", out var numberOfUpgrades);
                GameTexts.SetVariable("GAINUPGRADECOST",GainUpgradeCost * (numberOfUpgrades + 1) );
                
                
                
                return DefaultUnlockOakUpgradeCondition(args, text, GainUpgradeCost,
                    new TextObject("Increase the daily harmony gain from settlements by 20%.{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpdate("WEGainUpgrade", out var numberOfUpgrades), GainUpgradeCost * (1 + numberOfUpgrades)));


        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_D", "Troop harmony upkeep  reduction. {UPKEEPUPGRADECOST}{FORESTHARMONY}",
            args =>
            {
                var text = GetCurrentUpdate("WEUpkeepUpgrade", out var numberOfUpgrades);
                GameTexts.SetVariable("UPKEEPUPGRADECOST",TroopUpkeepUpgradeCost * (1+numberOfUpgrades));
                return DefaultUnlockOakUpgradeCondition(args, text, TroopUpkeepUpgradeCost,
                    new TextObject("Increase the daily harmony gain by 15.{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpdate("WEUpkeepUpgrade", out var numberOfUpgrades), TroopUpkeepUpgradeCost * (1 + numberOfUpgrades)));

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate { GameMenu.SwitchToMenu("oak_of_ages_menu"); });
    }


    private string GetCurrentUpdate(string upgradeCategory, out int upgradeCount)
    {
        var targetList = new List<string>();
        foreach (var list in _upgrades)
            if (list.Any(x => x.Contains(upgradeCategory)))
            {
                targetList = list;
                break;
            }

        upgradeCount = 0;

        foreach (var attribute in targetList)
        {
            if (!HasUnlockedUpgrade(attribute)) return attribute;
            upgradeCount++;
        }

        
        

        if (targetList.Count == upgradeCount) return "";

        throw new Exception("Upgrade could not be found");
        return "";
    }


    private class TreeSymbol
    {
        public int Cost { get; init; }
        public string UpgradeId { get; init; }
        public int MinimumLevel { get; init; }
        public TextObject ToolTipText = new();
        public TextObject UpgradeText = new();
        public TextObject ApplyText = new();
    }
}