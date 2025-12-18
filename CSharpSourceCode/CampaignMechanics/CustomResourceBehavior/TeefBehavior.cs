using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomResourceBehavior;

public class TeefBehavior : CampaignBehaviorBase
{
    private const int ItemExchange = 400; // item of price of X gets X/400 of teef in return
    private const int GoldExchange = 100;
    private const string QuartermasterId = "tor_kwartamasta_greenskins_0";

    public override void RegisterEvents()
    {
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
    }

    private void OnGameMenuOpened(MenuCallbackArgs menu)
    {

    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        AddDialogues(campaignGameStarter);
    }

    private void OnNewGameStarted(CampaignGameStarter campaignGameStarter)
    {
        foreach (var townOrCastle in Town.AllFiefs)
        {
            if (townOrCastle.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                CreateQuarterMaster(townOrCastle.Settlement);
            }
        }


        void CreateQuarterMaster(Settlement settlement)
        {
            var template = MBObjectManager.Instance.GetObject<CharacterObject>(QuartermasterId);
            if (template != null)
            {
                var quarterMaster = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
                quarterMaster.SupporterOf = settlement.OwnerClan;

                quarterMaster.SetName(template.Name, quarterMaster.Name);
                HeroHelper.SpawnHeroForTheFirstTime(quarterMaster, settlement);
            }
        }
    }

    private void AddDialogues(CampaignGameStarter starter)
    {
        starter.AddDialogLine("gw_quartermaster_regular", "start", "gw_quartermaster_hub", TORTextHelper.GetText("tor_gs_quartermaster_intro_text", "Hey yu. wanna spend for the big boss?"), () => IsQuarterMaster() && !PlayerOwnsTown(), null, 200);
        starter.AddDialogLine("gw_quartermaster_regular_reintro", "gw_quartermaster_regular_reintro", "gw_quartermaster_hub", TORTextHelper.GetText("tor_gs_quartermaster_anything_else_text", "Anything else?"), null, null, 200);
        starter.AddPlayerLine("gw_quartermaster_hub_regular_shinies_p", "gw_quartermaster_hub", "gw_quartermaster_regular_reintro", TORTextHelper.GetText("tor_gs_quartermaster_shinies_option_text", "Shinies"), () => Hero.MainHero.Gold >= 5000, () => SpendGold(false));
        starter.AddPlayerLine("gw_quartermaster_hub_regular_loot_p", "gw_quartermaster_hub", "gw_quartermaster_regular_reintro", TORTextHelper.GetText("tor_gs_quartermaster_loot_option_text", "Loot"), null, OpenForSpending);
        starter.AddPlayerLine("gw_quartermaster_hub_regular_leave_p", "gw_quartermaster_hub", "close_window", TORTextHelper.GetText("tor_gs_quartermaster_leave_option_text", "Leave"), null, null);

        starter.AddDialogLine("gw_quartermaster_playertown", "start", "gw_quartermaster_owner_hub", TORTextHelper.GetText("tor_gs_quartermaster_owner_intro_text", "Hey Big Boss. you wanna pile some loot?"), () => IsQuarterMaster() && PlayerOwnsTown(), null, 200);
        starter.AddDialogLine("gw_quartermaster_playertown_reintro", "gw_quartermaster_playertown_reintro", "gw_quartermaster_owner_hub", TORTextHelper.GetText("tor_gs_quartermaster_owner_anything_else_text", "Anything else Big Boss?"), null, null, 200);
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_shinies_p", "gw_quartermaster_owner_hub", "gw_quartermaster_playertown_reintro", TORTextHelper.GetText("tor_gs_quartermaster_shinies_option_text", "Shinies"), () => Hero.MainHero.Gold >= 5000, () => SpendGold(true));
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_teef_p", "gw_quartermaster_owner_hub", "gw_quartermaster_playertown_reintro", TORTextHelper.GetText("tor_gs_quartermaster_make_teefbags_option_text", "Make Teefbags"), () => Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 1000, MakeTeefBags);
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_loot_p", "gw_quartermaster_owner_hub", "gw_quartermaster_playertown_reintro", TORTextHelper.GetText("tor_gs_quartermaster_loot_option_text", "Loot"), null, OpenForCreatingLootPiles);
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_leave_p", "gw_quartermaster_owner_hub", "close_window", TORTextHelper.GetText("tor_gs_quartermaster_leave_option_text", "Leave"), null, null);

        bool IsQuarterMaster()
        {
            var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;
            if (partner == null) return false;
            return partner.Template?.StringId == QuartermasterId;

        }

        bool PlayerOwnsTown()
        {
            return Hero.MainHero.CurrentSettlement.Owner == Hero.MainHero;
        }

        void MakeTeefBags()
        {
            var title = TORTextHelper.GetTextObject("tor_gs_make_teefbags_title_text", "Make Teef bags");
            var description = TORTextHelper.GetTextObject("tor_gs_make_teefbags_description_text", "Teef are a little 'ard to carry. put them on your pile instead.");

            var currentTeef = 0;
            var selectableOptions = new List<InquiryElement>();
            var value = 0;
            currentTeef = (int)Hero.MainHero.GetCultureSpecificCustomResourceValue();
            GameTexts.SetVariable("TEEF_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
            if (currentTeef >= 1000)
            {
                value = 1000;
                var option = new TextObject("{TEEF_VALUE}{TEEF_ICON}");
                option.SetTextVariable("TEEF_VALUE", value);

                var hint = TORTextHelper.GetTextObject("tor_gs_store_teef_hint_text", "Store {TEEF_VALUE} Teef");
                hint.SetTextVariable("TEEF_VALUE", value);
                selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
            }

            if (currentTeef >= 3000)
            {
                value = 3000;
                var option = new TextObject("{TEEF_VALUE}{TEEF_ICON}");
                option.SetTextVariable("TEEF_VALUE", value);

                var hint = TORTextHelper.GetTextObject("tor_gs_store_teef_hint_text", "Store {TEEF_VALUE} Teef");
                hint.SetTextVariable("TEEF_VALUE", value);
                selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
            }

            if (currentTeef >= 5000)
            {
                value = 5000;
                var option = new TextObject("{TEEF_VALUE}{TEEF_ICON}");
                option.SetTextVariable("TEEF_VALUE", value);

                var hint = TORTextHelper.GetTextObject("tor_gs_store_teef_hint_text", "Store {TEEF_VALUE} Teef");
                hint.SetTextVariable("TEEF_VALUE", value);
                selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
            }

            var inquirydata = new MultiSelectionInquiryData(title.ToString(), description.ToString(), selectableOptions, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                CreateTeefContainer, null);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);


            void CreateTeefContainer(List<InquiryElement> inquiryElements)
            {
                var teefCount = (int)inquiryElements[0].Identifier;
                var pileCount = teefCount / 1000;

                var teefBagItem = MBObjectManager.Instance.GetObject<ItemObject>("tor_gs_teef_bag");
                Hero.MainHero.CurrentSettlement.Stash.AddToCounts(teefBagItem, pileCount);
                Hero.MainHero.AddCultureSpecificCustomResource(-teefCount);
            }
        }

        void OpenForSpending()
        {
            var donatedItems = new ItemRoster();

            InventoryScreenHelper.OpenScreenAsReceiveItems(
                donatedItems,
                TORTextHelper.GetTextObject("tor_gs_give_items_to_boss_text", "Give Items to the Big boss"),
                () => OnItemsDiscarded(donatedItems));
        }
        
        void OpenForCreatingLootPiles()
        {
            var currentRoster = new ItemRoster(Hero.MainHero.PartyBelongedTo.ItemRoster);
            var currentRosterWithEquipment = new ItemRoster();
            var equipment = Hero.MainHero.GetHeroEquipment();

            foreach (var item in currentRoster)
            {
                if (item.EquipmentElement.Item == null) continue;
                currentRosterWithEquipment.AddToCounts(item.EquipmentElement.Item, item.Amount);
            }

            foreach (var item in equipment)
            {
                if (item == null) continue;
                currentRosterWithEquipment.AddToCounts(item, 1);
            }

            var emptyRoster = new ItemRoster();
            InventoryScreenHelper.OpenScreenAsReceiveItems(emptyRoster, TORTextHelper.GetTextObject("tor_gs_create_loot_piles_text", "Create Loot piles for the big pile of loot"), () => CreateLootPiles(currentRosterWithEquipment));


            void CreateLootPiles(ItemRoster beforeTransferRoster)
            {
                var roster = beforeTransferRoster;
                var currentRoster = new ItemRoster();

                foreach (var item in Hero.MainHero.PartyBelongedTo.ItemRoster)
                {
                    currentRoster.Add(item);
                }

                var equipment = Hero.MainHero.GetHeroEquipment();

                foreach (var item in equipment)
                {
                    if (item == null) continue;
                    currentRoster.AddToCounts(item, 1);
                }

                var difference = new ItemRoster();
                foreach (var item in roster)
                {
                    if (currentRoster.FindIndexOfElement(item.EquipmentElement) != -1)
                    {
                        continue;
                    }
                    difference.Add(item);
                }

                var pileValue = difference.Sum(item => item.EquipmentElement.ItemValue);
                var pileCount = pileValue / 1000;


                var pileItem = MBObjectManager.Instance.GetObject<ItemObject>("tor_gs_loot_pile");

                Hero.MainHero.CurrentSettlement.Stash.AddToCounts(pileItem, pileCount);
            }
        }
    }

    private void SpendGold(bool forPiles)
    {
        var selectableOptions = new List<InquiryElement>();
        var currentGold = Hero.MainHero.Gold;
        var title = TORTextHelper.GetTextObject("tor_gs_spend_gold_title_text", "Spend Gold");

        var description = TORTextHelper.GetTextObject("tor_gs_spend_gold_description_text", "The Big Boss takes your shinies. How much would you like to spend to obtain some Teef?");

        if (currentGold < 5000)
        {
            return;
        }

        var value = 0;
        if (currentGold >= 5000)
        {
            value = 5000;
            var option = new TextObject("{GOLD_COST}{GOLD_ICON}");
            option.SetTextVariable("GOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_gs_spend_gold_hint_text", "Spend {GOLD_COST} Gold");
            hint.SetTextVariable("GOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        if (currentGold >= 50000)
        {
            value = 50000;
            var option = new TextObject("{GOLD_COST}{GOLD_ICON}");
            option.SetTextVariable("GOLD_COST", value);

            GameTexts.SetVariable("GOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_gs_spend_gold_hint_text", "Spend {GOLD_COST} Gold");
            hint.SetTextVariable("GOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        if (currentGold >= 100000)
        {
            value = 100000;
            var option = new TextObject("{GOLD_COST}{GOLD_ICON}");
            option.SetTextVariable("GOLD_COST", value);
            option.SetTextVariable("OATHGOLD_SYMBOL", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

            GameTexts.SetVariable("GOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_gs_spend_gold_hint_text", "Spend {GOLD_COST} Gold");
            hint.SetTextVariable("GOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        if (currentGold >= 250000)
        {
            value = 250000;
            var option = new TextObject("{GOLD_COST}{GOLD_ICON}");
            option.SetTextVariable("GOLD_COST", value);

            GameTexts.SetVariable("GOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_gs_spend_gold_hint_text", "Spend {GOLD_COST} Gold");
            hint.SetTextVariable("GOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        Action<List<InquiryElement>> action = forPiles ? CreateShinyPiles : AddGoldForTeef;

        var inquirydata = new MultiSelectionInquiryData(title.ToString(), description.ToString(), selectableOptions, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
            action, null);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }

    private void CreateShinyPiles(List<InquiryElement> inquiryElements)
    {
        var gold = (int)(inquiryElements[0].Identifier);

        var pileCount = gold / 5000;

        var pileItem = MBObjectManager.Instance.GetObject<ItemObject>("tor_gs_gold_pile");

        Hero.MainHero.CurrentSettlement.Stash.AddToCounts(pileItem, pileCount);
    }

    private void AddGoldForTeef(List<InquiryElement> inquiryElements)
    {
        var gold = (int)(inquiryElements[0].Identifier);

        var goldExchange = GoldExchange;



        var teefValue = gold / goldExchange;

        // MeanestanDaBaddestPassive3: double conversation -> 50% extra
        if (Hero.MainHero.HasCareer(TORCareers.OrcBoss) && Hero.MainHero.HasCareerChoice("MeanestanDaBaddestPassive3"))
        {
            teefValue *= 2;
        }

        Hero.MainHero.AddCultureSpecificCustomResource(teefValue);
        Hero.MainHero.Gold -= gold;
    }

    private void OnItemsDiscarded(ItemRoster itemRoster)
    {
        long totalItemValue = 0;

        foreach (var element in itemRoster)
        {
            var item = element.EquipmentElement.Item;
            if (item == null)
                continue;

            if (item.Culture!= null && item.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                continue;
            }

            var unitValue = Math.Max(0, element.EquipmentElement.ItemValue);
            totalItemValue += (long)unitValue * element.Amount;
        }

        var teef = (int)(totalItemValue / ItemExchange);
        if (teef <= 0)
            return;

        Hero.MainHero.AddCultureSpecificCustomResource(teef);
        TORCampaignEvents.Instance.OnTeefTransferred(Hero.MainHero, (int)totalItemValue);
    }


    public override void SyncData(IDataStore dataStore)
    {
    }
}