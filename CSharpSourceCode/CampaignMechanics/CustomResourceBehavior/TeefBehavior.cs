using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomResourceBehavior;

public class TeefBehavior : CampaignBehaviorBase
{
    private const int ItemExchange = 400; // item of price of X gets X/400 of teef in return
    private const int GoldToTeefExchangeRate = 100;
    private const string QuartermasterId = "tor_kwartamasta_greenskins_0";

    public override void RegisterEvents()
    {
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, ValidateKwartaMasters);
    }

    private void ValidateKwartaMasters()
    {
        foreach (var townOrCastle in Town.AllFiefs)
        {
            var settlement = townOrCastle.Settlement;
            var isGreenskinOwned = townOrCastle.OwnerClan?.Culture?.StringId == TORConstants.Cultures.GREENSKIN;
            var kwartamastas = GetAllKwartamastasForSettlement(settlement);

            if (isGreenskinOwned)
            {
                if (kwartamastas.Count == 0)
                {
                    CreateKwartamasta(settlement);
                }
                else if (kwartamastas.Count > 1)
                {
                    for (int i = 1; i < kwartamastas.Count; i++)
                    {
                        DisableHeroAction.Apply(kwartamastas[i]);
                    }
                }
            }
            else
            {
                foreach (var kwartamasta in kwartamastas)
                {
                    DisableHeroAction.Apply(kwartamasta);
                }
            }
        }
    }

    private List<Hero> GetAllKwartamastasForSettlement(Settlement settlement)
    {
        return settlement.HeroesWithoutParty.Where(h => h.Template?.StringId == QuartermasterId).ToList();
    }

    private void OnGameMenuOpened(MenuCallbackArgs menu)
    {

    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        AddDialogues(campaignGameStarter);
        EnsureKwartamastasExist();
    }

    private void EnsureKwartamastasExist()
    {
        foreach (var townOrCastle in Town.AllFiefs)
        {
            if (townOrCastle.OwnerClan?.Culture?.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (GetKwartamastaForSettlement(townOrCastle.Settlement) == null)
                {
                    CreateKwartamasta(townOrCastle.Settlement);
                }
            }
        }
    }

    private void OnNewGameStarted(CampaignGameStarter campaignGameStarter)
    {
        foreach (var townOrCastle in Town.AllFiefs)
        {
            if (townOrCastle.OwnerClan?.Culture?.StringId == TORConstants.Cultures.GREENSKIN)
            {
                CreateKwartamasta(townOrCastle.Settlement);
            }
        }
    }

    private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        if (!settlement.IsTown && !settlement.IsCastle)
            return;

        var newOwnerCultureId = newOwner?.MapFaction?.Culture?.StringId;
        var existingKwartamasta = GetKwartamastaForSettlement(settlement);

        if (newOwnerCultureId == TORConstants.Cultures.GREENSKIN)
        {
            if (existingKwartamasta != null)
            {
                existingKwartamasta.SupporterOf = settlement.OwnerClan;
            }
            else
            {
                CreateKwartamasta(settlement);
            }
        }
        else
        {
            if (existingKwartamasta != null)
            {
                DisableHeroAction.Apply(existingKwartamasta);
            }
        }
    }

    private Hero GetKwartamastaForSettlement(Settlement settlement)
    {
        return settlement.HeroesWithoutParty.FirstOrDefault(h => h.Template?.StringId == QuartermasterId);
    }

    private void CreateKwartamasta(Settlement settlement)
    {
        var template = MBObjectManager.Instance.GetObject<CharacterObject>(QuartermasterId);
        if (template != null)
        {
            var kwartamasta = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
            kwartamasta.SupporterOf = settlement.OwnerClan;
            kwartamasta.CharacterObject.HiddenInEncyclopedia = true;
            var nameObject = template.GetName();
            nameObject.SetTextVariable("FIRSTNAME", kwartamasta.FirstName);
            kwartamasta.SetName(nameObject, kwartamasta.Name);
            HeroHelper.SpawnHeroForTheFirstTime(kwartamasta, settlement);
        }
    }

    private void AddDialogues(CampaignGameStarter starter)
    {
        starter.AddDialogLine("gw_quartermaster_regular", "start", "gw_quartermaster_hub", TORTextHelper.GetTextForNative("tor_gs_quartermaster_intro_text", "Yoo dere, youz got sumfing fer da Boss?"), () => IsQuarterMaster() && !PlayerOwnsTown(), null, 200);
        starter.AddDialogLine("gw_quartermaster_regular_reintro", "gw_quartermaster_regular_reintro", "gw_quartermaster_hub", TORTextHelper.GetTextForNative("tor_gs_quartermaster_anything_else_text", "Wot, youz got more?"), null, null, 200);
        starter.AddPlayerLine("gw_quartermaster_hub_regular_shinies_p", "gw_quartermaster_hub", "gw_quartermaster_regular_reintro", TORTextHelper.GetTextForNative("tor_gs_quartermaster_shinies_option_text", "Shinies"), () => Hero.MainHero.Gold >= 5000, () => SpendGold(false));
        starter.AddPlayerLine("gw_quartermaster_hub_regular_loot_p", "gw_quartermaster_hub", "gw_quartermaster_regular_reintro", TORTextHelper.GetTextForNative("tor_gs_quartermaster_loot_option_text", "Loot"), null, OpenForSpending);
        starter.AddPlayerLine("gw_quartermaster_hub_regular_leave_p", "gw_quartermaster_hub", "close_window", TORTextHelper.GetTextForNative("tor_gs_quartermaster_leave_option_text", "Iz outta 'ere!"), null, null);

        starter.AddDialogLine("gw_quartermaster_playertown", "start", "gw_quartermaster_owner_hub", TORTextHelper.GetTextForNative("tor_gs_quartermaster_owner_intro_text", "Oi, Boss, youz got sum loot fer da pile?"), () => IsQuarterMaster() && PlayerOwnsTown(), null, 200);
        starter.AddDialogLine("gw_quartermaster_playertown_reintro", "gw_quartermaster_playertown_reintro", "gw_quartermaster_owner_hub", TORTextHelper.GetTextForNative("tor_gs_quartermaster_owner_anything_else_text", "Iz dere more, Boss?"), null, null, 200);
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_shinies_p", "gw_quartermaster_owner_hub", "gw_quartermaster_playertown_reintro", TORTextHelper.GetTextForNative("tor_gs_quartermaster_shinies_option_text", "Shinies"), () => Hero.MainHero.Gold >= 5000, () => SpendGold(CurrentSettlementIsGreenskinCamp()));//Shiny piles only apply effects in greenskin original settlements; therefore, trading for gold_piles is gated behind the same set of checks. Player ownership is verified earlier in the dialogue tree.
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_teef_p", "gw_quartermaster_owner_hub", "gw_quartermaster_playertown_reintro", TORTextHelper.GetTextForNative("tor_gs_quartermaster_make_teefbags_option_text", "Make Teefbags"), () => Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 1000, MakeTeefBags);
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_loot_p", "gw_quartermaster_owner_hub", "gw_quartermaster_playertown_reintro", TORTextHelper.GetTextForNative("tor_gs_quartermaster_loot_option_text", "Loot"), CurrentSettlementIsGreenskinCamp, OpenForCreatingLootPiles);//Loot piles only apply effects in greenskin original settlements; therefore, trading for loot_piles is gated behind the same set of checks. Player ownership is verified earlier in the dialogue tree.
        starter.AddPlayerLine("gw_quartermaster_hub_playertown_leave_p", "gw_quartermaster_owner_hub", "close_window", TORTextHelper.GetTextForNative("tor_gs_quartermaster_leave_option_text", "Iz outta 'ere!"), null, null);

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

        bool CurrentSettlementIsGreenskinCamp()
        {
            return Hero.MainHero.CurrentSettlement.IsGreenskinCamp();
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

                // Calculate pile value
                long pileValue = 0;
                foreach (var element in difference)
                {
                    var item = element.EquipmentElement.Item;
                    if (item == null) continue;
                    var unitValue = Math.Max(0, element.EquipmentElement.ItemValue);
                    pileValue += (long)unitValue * element.Amount;
                }

                var pileCount = (int)(pileValue / 1000);

                var pileItem = MBObjectManager.Instance.GetObject<ItemObject>("tor_gs_loot_pile");

                Hero.MainHero.CurrentSettlement.Stash.AddToCounts(pileItem, pileCount);

                // Fire event for quest progression (same as OnItemsDiscarded)
                if (pileValue > 0)
                {
                    TORCampaignEvents.Instance.OnTeefTransferred(Hero.MainHero, (int)pileValue);
                }
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

        Action<List<InquiryElement>> action = forPiles ? CreateShinyPiles : TradeGoldForTeef;

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
        
        Hero.MainHero.ChangeHeroGold(-gold);
    }

    private void TradeGoldForTeef(List<InquiryElement> inquiryElements)
    {
        var gold = (int)(inquiryElements[0].Identifier);

        var goldExchange = GoldToTeefExchangeRate;



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