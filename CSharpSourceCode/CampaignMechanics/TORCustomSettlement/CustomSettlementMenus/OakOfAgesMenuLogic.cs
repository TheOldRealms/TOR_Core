using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class OakOfAgesMenuLogic(CampaignGameStarter campaignGameStarter) : TORBaseSettlementMenuLogic(campaignGameStarter)
{
    private const int TreemanPrice = 1200;
    private const int DryadPrice = 100;
    private const int PartySizeUpgradeCost = 100;
    private const int HealthUpgradeCost = 125;
    private const int GainUpgradeCost = 150;

    private const int TravelCost = 100;
    private const int RootUnlockCost = 200;
    private const int RootTravelCostReductionUpgradeCost = 500;
    private const int RootTravelBackUpgradeCost = 750;
    private const int RootHealUpgradeCost = 1000;
    private readonly Vec2 _ardenLocation = new(945.4077f, 1111.009f);
    private readonly Vec2 _laurelornLocation = new(1260.19f, 1288.84f);
    private readonly Vec2 _gryphenWoodLocation = new(1606.698f, 1133.905f);
    private readonly Vec2 _athelLorenLocation = new(1238.435f, 778.6498f);
    private readonly Vec2 _maisonTaalLocation = new(1228.051f, 973.7142f);

    private const int TreeSymbolChangeCost = 100;
    private const int TreeSymbolUnlockCosts = 500;
    private const int TreeSymbolUpgradeNoCost = 400;

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
    
    private static readonly List<TreeSymbol> TreeSymbols =
    [
        new TreeSymbol()
        {
            UpgradeId = "WEKithbandSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 0,
            UpgradeText = new TextObject("Decipher the Symbol of the Kithband. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Kithband. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WEKithbandSymbol").Value+"{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEWardancerSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 0,
            UpgradeText = new TextObject("Decipher the Symbol of the Wardancer. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Wardancer. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WEWardancerSymbol").Value+"{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WETreekinSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 0,
            UpgradeText = new TextObject("Decipher the Symbol of the Treekin. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Treekin. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WETreekinSymbol").Value+"{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEOrionSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 15,
            UpgradeText = new TextObject("Decipher the Symbol of Orion. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of Orion. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WEOrionSymbol").Value+"{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEArielSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 15,
            UpgradeText = new TextObject("Decipher the Symbol of Ariel. {TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of Ariel. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WEArielSymbol").Value+"{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEWandererSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 15,
            UpgradeText = new TextObject("Decipher the Symbol of the Wanderer.{TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of the Wanderer. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WEWandererSymbol").Value+"{UPGRADEFAILEDREASON}")
        },
        new TreeSymbol()
        {
            UpgradeId = "WEDurthuSymbol",
            Cost = TreeSymbolUnlockCosts,
            MinimumLevel = 20,
            UpgradeText = new TextObject("Decipher the Symbol of Durthu.{TREESYMBOLUNLOCKCOST}{FORESTHARMONY}"),
            ApplyText = new TextObject("Apply the Symbol of Durthu. {TREESYMBOLCHANGECOST}{FORESTHARMONY}"),
            ToolTipText = new TextObject( GameTexts.FindText("tor_treesymbol_description","WEDurthuSymbol").Value+"{UPGRADEFAILEDREASON}")
        }
    ];


    private readonly List<List<string>> _upgrades =
        [
            PartyUpgradeAttributes, MaximumHealthUpgradeAttributes, CustomResourceGainUpgrades
        ];

    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddOakOfAgeMenus(campaignGameStarter);
    }

    private void OakOfAgeMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not TORBaseSettlementComponent component) return;

        var text = component.IsActive
            ? GameTexts.FindText("customsettlement_intro", settlement.StringId)
            : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);

        MBTextManager.SetTextVariable("FORESTHARMONY", CustomResourceManager.GetResourceObject("ForestHarmony").GetCustomResourceIconAsText());
        GameTexts.SetVariable("DRYAD_PRICE", DryadPrice);
        GameTexts.SetVariable("TREEMAN_PRICE", TreemanPrice);
    }

    public void AddOakOfAgeMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_menu", "{LOCATION_DESCRIPTION}", OakOfAgeMenuInit);
        
        
        starter.AddGameMenuOption("oak_of_ages_menu", "tree_spirits", "Tree Spirits", _ =>
        {
            _.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return IsAsrai();
        }, delegate
        {
            _currentMenu = "oak_of_ages_tree_spirits_menu";
            GameMenu.SwitchToMenu("oak_of_ages_tree_spirits_menu");
        }, false, 4, false);
        
        starter.AddGameMenuOption("oak_of_ages_menu", "branch", "Branches of the Oak", _ =>
        {
            _.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return IsAsrai();
        }, delegate
        {
            _currentMenu = "oak_of_ages_branches_menu";
            GameMenu.SwitchToMenu("oak_of_ages_branches_menu");
        }, false, 4, false);

        starter.AddGameMenuOption("oak_of_ages_menu", "roots", "World roots of the Oak", _ =>
        {
            _.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return IsAsrai();
        }, delegate
        {
            _currentMenu = "oak_of_ages_roots_menu";
            GameMenu.SwitchToMenu("oak_of_ages_roots_menu");
        }, false, 4, false);

        starter.AddGameMenuOption("oak_of_ages_menu", "symbols", "Tree Symbols", _ =>
        {
            _.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return IsAsrai();
        }, delegate
        {
            _currentMenu = "oak_of_ages_tree_symbols_menu";
            GameMenu.SwitchToMenu("oak_of_ages_tree_symbols_menu");
        }, false, 4, false);
        
        
        starter.AddGameMenuOption("oak_of_ages_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            _currentMenu = "";
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate(MenuCallbackArgs args)
        {
            PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
        }, true);

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
        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpirits_A", "{tor_custom_settlement_menu_cursed_site_ghost_str}Commune with the forest {DRYAD_PRICE}{FORESTHARMONY}", 
             (args) => TreeSpiritHelpers.CanBindTreeSpirits() && CanBindDryads(args), _ =>
             {
                 Hero.MainHero.AddCustomResource("ForestHarmony",-DryadPrice);
                 GameMenu.SwitchToMenu("oak_of_ages_tree_spirits_menu_bind_dryads");
             });

        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpirits_B", "Rouse Treemen {TREEMAN_PRICE}{FORESTHARMONY}", args => TreeSpiritHelpers.CanBindTreeSpirits() && CanBindTreeman(args),
            AddTreemen);
        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpirits_C", "Relieve (yourself of) Treespirits",null, (args) 
                => PartyScreenHelper.OpenScreenAsQuest(TroopRoster.CreateDummyTroopRoster(), new TextObject("Donated Spirits"),
                    500,0,null,TranferCompleted, IsTransferableTreeSpirit,null),
            false);     //check if left side only contain tree spirits done button condition

        starter.AddWaitGameMenu("oak_of_ages_tree_spirits_menu_bind_dryads", "{=tor_custom_settlement_cursed_site_ghosts_progress_str}Performing binding ritual...",
            delegate (MenuCallbackArgs args)
            {
                _startWaitTime = CampaignTime.Now;
                numberOfTroopsFromInteraction = 0;
                PlayerEncounter.Current.IsPlayerWaiting = true;
                args.MenuContext.GameMenu.StartWait();
            }, null, DryadConsequence,
            BindingTick,
            GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 8f, GameMenu.MenuFlags.None, null);
        
        
        starter.AddGameMenuOption("oak_of_ages_tree_spirits_menu", "treeSpiritsMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate(MenuCallbackArgs args) { GameMenu.SwitchToMenu("oak_of_ages_menu"); }, true, -1, false, null);
        
        starter.AddGameMenu("dryads_result","{DRYAD_RESULT}",null);
        starter.AddGameMenuOption("dryads_result", "dryads_result_leave", "back...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate { GameMenu.SwitchToMenu("oak_of_ages_tree_spirits_menu"); }, true);
        
        
        void TranferCompleted(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftprisonroster, PartyBase rightownerparty,
            TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            if (fromcancel) return;
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

       

        bool CanBindTreeman(MenuCallbackArgs args)
        {
            var mainHero = Hero.MainHero;
            args.IsEnabled = mainHero.GetCultureSpecificCustomResourceValue() >= TreemanPrice;
            if (!args.IsEnabled)
            {
                args.Tooltip = new TextObject("You broke. May not even need this since the cost is written on the menu text.");
                return true;
            }

            var amount = 0;

            var capableSpellsinger =  MobileParty.MainParty.GetMemberHeroes().Where(x=> x.IsSpellSinger()).MaxBy(x => x.GetSkillValue(TORSkills.SpellCraft));
            amount += capableSpellsinger.GetSkillValue(TORSkills.SpellCraft)/100;

            if (amount == 0) //Durthu symbol only increases the cap, it doesn't decrease the minimum spellcraft required
            {
                args.Tooltip = new TextObject("No sufficiently strong spellsinger present who can convince the spirits to follow you.");
                args.IsEnabled = false;
                return true ;
            }
            if (mainHero.HasAttribute("WEDurthuSymbol")) amount += 1;
            
            if(mainHero.PartyBelongedTo.Party.MemberRoster.GetTroopRoster().Where(x=> x.Character.StringId.Contains("treeman")).Count()>amount)
            {
                args.Tooltip = new TextObject("Too many treeman already follow your party");//one day we'll bother with pluralizing this
                args.IsEnabled = false;
            }
            return true;
        }
        
        bool CanBindDryads(MenuCallbackArgs args)
        {
            args.IsEnabled = Hero.MainHero.GetCultureSpecificCustomResourceValue() >= DryadPrice;
            return true;
        }
        
        bool IsTransferableTreeSpirit(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftownerparty)
        {
            if (type != PartyScreenLogic.TroopType.Member) return false;
            if (character.IsHero) return false;
            return character.Culture.StringId == TORConstants.Cultures.ASRAI && !character.IsElf();
        }

        void AddTreemen(MenuCallbackArgs args)
        {
            var treeman = MBObjectManager.Instance.GetObject<CharacterObject>("tor_we_treeman");
            MobileParty.MainParty.AddElementToMemberRoster(treeman, 1);
            Hero.MainHero.AddCultureSpecificCustomResource(-TreemanPrice);
            GameMenu.ActivateGameMenu("oak_of_ages_tree_spirits_menu");
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
                    var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                    if (freeSlots < 1) return;
                    
                    var capableSpellsinger = MobileParty.MainParty.GetMemberHeroes().Where(x=> x.IsSpellSinger()).MaxBy(x => x.GetSkillValue(TORSkills.SpellCraft));
                    
                    var gainChance = TreeSpiritHelpers.GetSuccessChance(capableSpellsinger);
                    if (MBRandom.RandomFloat > gainChance) return;
                    
                    var count = MBRandom.RandomInt(1, 3);
                    if (freeSlots < count) count = freeSlots;

                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_we_dryad");
                    if (troop != null)
                    {
                        MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                        CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, Settlement.CurrentSettlement, null, troop, count);
                        numberOfTroopsFromInteraction += count;
                    }
                    else
                    {
                        TORCommon.Log("OakOfAgesMenuLogic : dryad character object not found", NLog.LogLevel.Warn);
                    }
                }
            }
        }
        
        void DryadConsequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            args.MenuContext.GameMenu.EndWait();
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);

            if (numberOfTroopsFromInteraction == 0)
            {
                GameTexts.SetVariable("DRYAD_RESULT", "You were unable to bind any tree spirits");
            }
            else
            {
                GameTexts.SetVariable("NUMBEROFTROOPS", numberOfTroopsFromInteraction);
                GameTexts.SetVariable("DRYAD_RESULT", "You were able to bind {NUMBEROFTROOPS} dryads to your party."); //one day we'll pluralize it
            }
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

        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_remove", "Remove symbol", _ => RemoveSymbolCondition(),
            _ => RemoveSymbolConsequence(true));
        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_chalk", "Magical chalk. {TREESYMBOLFREEUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WESymbolReduceCosts", TreeSymbolUpgradeNoCost,
                new TextObject("Remove Symbol change costs.{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WESymbolReduceCosts", TreeSymbolUpgradeNoCost));

        starter.AddGameMenuOption("oak_of_ages_tree_symbols_menu", "treeSymbolMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate(MenuCallbackArgs args) 
        {
            GameMenu.SwitchToMenu("oak_of_ages_menu");
        }, true, -1, false, null);
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
        
        if (upgrade == "")
        {
            failreasonStringBuilder.Clear();
            failreasonStringBuilder.Append("{newline}No further upgrades available");
            
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



        if (!HasUnlockedUpgrade(upgrade)) return false;

        if (!HasUnlockedUpgrade("WESymbolReduceCosts")&&Hero.MainHero.GetCultureSpecificCustomResourceValue() < upgradeCost)
        {
            args.IsEnabled = false;
            failreasonStringBuilder.Append("{newline}Not enough Harmony.");
        }
        
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

        var resultText = toolTipText;
        resultText.SetTextVariable("UPGRADEFAILEDREASON", failreasonStringBuilder.ToString());
        args.Tooltip = resultText;

        return true;
    }

    private void SelectTreeSymbolConsequence(string upgrade, int upgradeCost)
    {
        RemoveSymbolConsequence();
        Hero.MainHero.AddAttribute(upgrade); // needed

        if (!HasUnlockedUpgrade("WESymbolReduceCosts"))
        {
            Hero.MainHero.AddCultureSpecificCustomResource(-upgradeCost);
        }
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

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_arden_unlock",
            "Unlock Pathway to the Forest of Arden. {ROOTUNLOCKCOST}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootUnlockCost, "WorldRootTarget_Arden"),
            _ => UnlockOakUpgrade("WorldRootTarget_Arden", RootUnlockCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_arden_travel",
            "Travel to the forest of Arden. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",
            args => RootAccessibleCondition(args, TravelCost, "WorldRootTarget_Arden"), _ => RootTravelConsequence(_ardenLocation));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_laurelorn_unlock", "Unlock the pathway to Laurelorn. {ROOTUNLOCKCOST}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootUnlockCost, "WorldRootTarget_Laurelorn"),
            _ => UnlockOakUpgrade("WorldRootTarget_Laurelorn", RootUnlockCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_laurelorn_travel", "Travel to Laurelorn. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",
            args => RootAccessibleCondition(args, TravelCost, "WorldRootTarget_Laurelorn"), _ => RootTravelConsequence(_laurelornLocation));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_gryphenWood_unlock",
            "Unlock the pathway to the Gryphenwood. {ROOTUNLOCKCOST}{FORESTHARMONY}",
            args => RootUpgradeCondition(args, RootUnlockCost, "WorldRootTarget_GryphenWood"),
            _ => UnlockOakUpgrade("WorldRootTarget_GryphenWood", RootUnlockCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_gryphenWood_travel", "Travel to the Gryphenwood. {WORLDROOTTRAVELCOST}{FORESTHARMONY}",
            args => RootAccessibleCondition(args, TravelCost, "WorldRootTarget_GryphenWood"), _ => RootTravelConsequence(_gryphenWoodLocation));


        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_internalizePaths", "Internalize the root pathways. {ROOTTRAVELUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelCostUpgrade", RootTravelCostReductionUpgradeCost,
                new TextObject("Reduce the travel cost.{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelCostUpgrade", RootTravelCostReductionUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_returnPath",
            "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelBackUpgrade", RootTravelBackUpgradeCost,
                new TextObject("Return to the Oak from the root exit.{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelBackUpgrade", RootTravelBackUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_healingRoot", "Healing Aura of roots. {ROOTHEALUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelHealUpgrade", RootTravelBackUpgradeCost,
                new TextObject("All troops and heroes are healed upon using the world roots{newline}{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelHealUpgrade", RootHealUpgradeCost));

        starter.AddGameMenuOption("oak_of_ages_roots_menu", "rootMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate(MenuCallbackArgs args) { GameMenu.SwitchToMenu("oak_of_ages_menu"); }, true, -1, false, null);
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
        Campaign.Current.MainParty.Position = NavigationHelper.FindReachablePointAroundPosition(new CampaignVec2(location, true), MobileParty.NavigationType.Default, 0f);

        if (hasTravelCost)
        {
            var cost = TravelCost;
            if (HasUnlockedUpgrade("WETravelCostUpgrade")) cost /= 2;
            


            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR || Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
            {
                cost/=2;
            }
            
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

        if (GameStateManager.Current.ActiveState is MapState mapState)
        {
            mapState.Handler.ResetCamera(false, true);
        }
    }

    private void AddWorldRootsMenus(CampaignGameStarter starter)
    {
        MBTextManager.SetTextVariable("ROOTRETURNUPGRADE", RootTravelBackUpgradeCost);
        starter.AddGameMenu("worldroots_menu", "{LOCATION_DESCRIPTION}", WorldRootsMenuInit);
        
        //duplicate string ids, these will need to be differentiated when implementing string fetching from GameTexts
        starter.AddGameMenuOption("worldroots_menu", "worldroots_travel_maisontaal", "{tor_custom_settlement_menu_leave_str}Travel to Maisontaal",
            args => TravelEonirCondition(args) && Hero.MainHero.CurrentSettlement.StringId == "worldroot_02", (MenuCallbackArgs args) => RootTravelConsequence(_maisonTaalLocation, true), false);
        
        starter.AddGameMenuOption("worldroots_menu", "worldroots_travel_laurelorn", "{tor_custom_settlement_menu_leave_str}Travel to Laurelorn",
            args => TravelEonirCondition(args) && Hero.MainHero.CurrentSettlement.StringId == "worldroot_04", (MenuCallbackArgs args) => RootTravelConsequence(_laurelornLocation, true), false);

        starter.AddGameMenuOption("worldroots_menu", "worldroots_travel_athelloren", "{tor_custom_settlement_menu_leave_str}Travel back to Athel Loren...",
            args => TravelBackCondition(args), (MenuCallbackArgs args) => RootTravelConsequence(_athelLorenLocation, true), false);

        starter.AddGameMenuOption("worldroots_menu", "worldroots_return_paths",
            "Establish pathways back to the Oak of Ages. {ROOTRETURNUPGRADE}{FORESTHARMONY}",
            args => DefaultUnlockOakUpgradeCondition(args, "WETravelBackUpgrade", RootTravelBackUpgradeCost,
                new TextObject("Return to the Oak from the root exit.{UPGRADEFAILEDREASON}")),
            _ => UnlockOakUpgrade("WETravelBackUpgrade", RootTravelBackUpgradeCost));

        starter.AddGameMenuOption("worldroots_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate(MenuCallbackArgs args)
        {
            PlayerEncounter.LeaveSettlement();
			PlayerEncounter.Finish(true);
        }, true);
    }

    private bool TravelEonirCondition(MenuCallbackArgs args)
    {
        if(Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR && Hero.MainHero.Culture.StringId != TORConstants.Cultures.BRETONNIA && Hero.MainHero.Culture.StringId != TORConstants.Cultures.EMPIRE)
        {
            return false;
        }
        

        var resource = Hero.MainHero.GetCultureSpecificCustomResource();
        var cost = Hero.MainHero.StringId == TORConstants.Cultures.BRETONNIA ? 100 : 50;
        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < 50)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("Not enough " + resource.Name + " requires " + cost); //I guess a TO within another one is fine to not call ToString.
            return true;
        }

        args.Tooltip = new TextObject("Travel to target location (" + cost + " " + resource.GetCustomResourceIconAsText() + ")");

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

        if ((Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR || 
            Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE ||
            Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA) 
            && settlement.StringId =="worldroot_02")
        {
            var textb = GameTexts.FindText("customsettlement_intro", settlement.StringId+"b");
            if (textb != null)
            {
                MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", textb);
            }
        }
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private void AddBranchesOfTheOakMenu(CampaignGameStarter starter)
    {
        starter.AddGameMenu("oak_of_ages_branches_menu", "Branches of The Oak", null);

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_A", "Build Outposts. {PARTYSIZEUPGRADECOST}",
            args =>
            {
                var upgrade = GetCurrentUpgrade("WEPartySizeUpgrade", out var numberOfUpgrades);

                if (upgrade != "")
                {
                    GameTexts.SetVariable("PARTYSIZEUPGRADECOST", PartySizeUpgradeCost * (numberOfUpgrades + 1) + "{FORESTHARMONY}");
                }
                else
                {
                    GameTexts.SetVariable("PARTYSIZEUPGRADECOST","");
                }
                
                return DefaultUnlockOakUpgradeCondition(args, upgrade , PartySizeUpgradeCost * (numberOfUpgrades + 1),
                    new TextObject("Increase party Size by 10%.{newline}{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpgrade("WEPartySizeUpgrade", out var numberOfUpgrades), PartySizeUpgradeCost * (1 + numberOfUpgrades)));

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_B", "Strong branches. {HEALTHUPGRADECOST}",
            args =>
            {
                var upgrade = GetCurrentUpgrade("WEHealthUpgrade", out var numberOfUpgrades);

                if (upgrade != "")
                {
                    GameTexts.SetVariable("HEALTHUPGRADECOST", HealthUpgradeCost * (numberOfUpgrades + 1) + "{FORESTHARMONY}");
                }
                else
                {
                    GameTexts.SetVariable("HEALTHUPGRADECOST","");
                }
                return DefaultUnlockOakUpgradeCondition(args, upgrade, HealthUpgradeCost * (numberOfUpgrades + 1),
                    new TextObject("Increase maximum health by 10%.{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpgrade("WEHealthUpgrade", out var numberOfUpgrades), HealthUpgradeCost * (1 + numberOfUpgrades)));

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "branchMenu_C", "Thriving Leaves. {GAINUPGRADECOST}",
            args =>
            {//multiple text objects or a separate string?
                var upgrade = GetCurrentUpgrade("WEGainUpgrade", out var numberOfUpgrades);
                if (upgrade != "")
                {
                    GameTexts.SetVariable("GAINUPGRADECOST", GainUpgradeCost * (numberOfUpgrades + 1) + "{FORESTHARMONY}");
                }
                else
                {
                    GameTexts.SetVariable("GAINUPGRADECOST","");
                }
                
                return DefaultUnlockOakUpgradeCondition(args, upgrade, GainUpgradeCost * (numberOfUpgrades + 1),
                    new TextObject("Increase the daily harmony gain from settlements by 20%.{UPGRADEFAILEDREASON}"), 4 * numberOfUpgrades);
            },
            _ => UnlockOakUpgrade(GetCurrentUpgrade("WEGainUpgrade", out var numberOfUpgrades), GainUpgradeCost * (1 + numberOfUpgrades)));
        

        starter.AddGameMenuOption("oak_of_ages_branches_menu", "treeBranchUpgradeMenu_leave", "Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate(MenuCallbackArgs args) { GameMenu.SwitchToMenu("oak_of_ages_menu"); }, true, -1, false, null);
    }


    private string GetCurrentUpgrade(string upgradeCategory, out int upgradeCount)
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
    }


    private class TreeSymbol
    {
        public int Cost { get; init; }
        public string UpgradeId { get; init; }
        public int MinimumLevel { get; init; }
        public TextObject ToolTipText = TextObject.GetEmpty();
        public TextObject UpgradeText = TextObject.GetEmpty();
        public TextObject ApplyText = TextObject.GetEmpty();
    }
}