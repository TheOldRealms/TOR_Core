using Helpers;
using Ink.Parsed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Quests;
using TOR_Core.Utilities;
using Text = Ink.Parsed.Text;

namespace TOR_Core.CampaignMechanics.Menagery;

public class OathGoldBehavior : CampaignBehaviorBase
{
    public const int MAXIMUMVALUE = 2000;

    private const int MinimumSteelAmount = 25;
    private const int MinimumFineSteelAmount = 10;
    private const int MinimumGromrilAmount = 10;
    private const int SteelGain = 10;
    private const int FineSteelGain = 50;
    private const int GromrilGain = 150;
    private Dictionary<string, int> _guildValues;
    private double _lastTimeVistedTown;
    private int _expeditionMaximum;
    private string _currentGuild;
    private readonly List<InquiryElement> _currentItems = new();
    private readonly (string template, string guild, string location) _templateRuneSmith = ("tor_dawi_runelord_trainer_0", "runesmith", "house_1");
    private readonly (string template, string guild, string location) _templateEngineer = ("tor_dawi_engineers_guild_npc_0", "engineer", "house_2");
    private readonly (string template, string guild, string location) _templateGemcutters = ("tor_dawi_miner_guild_npc_0", "miner", "house_2");
    private readonly (string template, string guild, string location) _templateBrewer = ("tor_dawi_brewers_guild_npc_0", "brewer", "tavern");
    private readonly (string template, string guild, string location) _templateWarrior = ("tor_dawi_warriors_guild_npc_0", "warrior", "lordshall");
    private readonly List<(string template, string guild, string location)> _templates = [];
    private Dictionary<string, List<string>> _settlementToGuildmasters = new();
    private Dictionary<string, double> _guildActions = new();
    private int _craftingOrdersCompleted;

    public double LastVisitAtTown => CampaignTime.Now.ToDays - _lastTimeVistedTown;
    public int ExpeditionMaximum => _expeditionMaximum;
    public int CurrentExpeditions => _guildActions.Keys.WhereQ(x => x.Contains("expedition")).Count();
    public int WarriorsGuildReputation => _guildValues[_templateWarrior.guild];
    public int EngineerGuildReputation => _guildValues[_templateEngineer.guild];
    public int BrewersGuildReputation => _guildValues[_templateBrewer.guild];
    public int GemcuttersAndMinersReputation => _guildValues[_templateGemcutters.guild];
    public int RuneSmithReputation => _guildValues[_templateRuneSmith.guild];
    public int CraftingOrdersCompleted => _craftingOrdersCompleted;

    public override void RegisterEvents()
    {
        _templates.Add(_templateRuneSmith);
        _templates.Add(_templateEngineer);
        _templates.Add(_templateGemcutters);
        _templates.Add(_templateBrewer);
        _templates.Add(_templateWarrior);

        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
        CampaignEvents.OnCraftingOrderCompletedEvent.AddNonSerializedListener(this, CraftingOrderCompleted);
    }

    private void CraftingOrderCompleted(Town town, CraftingOrder order, ItemObject craftedItem, Hero delivered)
    {
        _craftingOrdersCompleted++;
    }


    private void AddResourcesToGuild(List<InquiryElement> inquiryElements)
    {
        var value = (int)inquiryElements[0].Identifier;

        _guildValues.TryGetValue(_currentGuild, out var currentValue);

        var newValue = Math.Min(MAXIMUMVALUE, currentValue + value);
        var resultCost = newValue - currentValue;
        _guildValues[_currentGuild] = newValue;
        Hero.MainHero.AddCultureSpecificCustomResource(-resultCost);

        AddGuildBenefits(_templateGemcutters.guild, "DwarfMinersI", "DwarfMinersII", "DwarfMinersIII");
        AddGuildBenefits(_templateBrewer.guild, "DwarfBrewersI", "DwarfBrewersII", "DwarfBrewersIII");
        AddGuildBenefits(_templateEngineer.guild, "DwarfEngineersI", "DwarfEngineersII", "DwarfEngineersIII");
        AddGuildBenefits(_templateWarrior.guild, "DwarfWarriorI", "DwarfWarriorII", "DwarfWarriorIII");
        AddGuildBenefits(_templateRuneSmith.guild, "RuneSmithI", "RuneSmithII", "RuneSmithIII");
    }


    private void SettlementEntered(MobileParty party, Settlement settlement, Hero leaderHero)
    {
        if (leaderHero == null || leaderHero != Hero.MainHero) return;

        if (!settlement.IsDwarfKarak()) return;

        var currentTime = CampaignTime.Now.ToDays;

        if (!(currentTime > (_lastTimeVistedTown + CampaignTime.Weeks(2).ToDays))) return;
        if (party.Party.MemberRoster.Count > 50)
        {
            AddCarePackage();
        }

        AddMinersBenefits();

        _lastTimeVistedTown = CampaignTime.Now.ToDays;
    }

    private void AddMinersBenefits()
    {
        if (!Hero.MainHero.HasAttribute("DwarfMinersI")) return;

        var items = new List<ItemObject>();
        items.Add(DefaultItems.Charcoal);
        items.Add(TorEnchantingIngredients.GemStone);
        items.Add(DefaultItems.IronOre);
        foreach (var item in items)
        {
            var random = MBRandom.RandomInt(5, 10);
            if (Hero.MainHero.HasAttribute("DwarfMinersIII"))
            {
                random = MBRandom.RandomInt(10, 15);
            }
            else if (Hero.MainHero.HasAttribute("DwarfMinersII"))
            {
                random = MBRandom.RandomInt(8, 12);
            }

            if (item == TorEnchantingIngredients.GemStone)
            {
                random -= 2;
            }

            PartyBase.MainParty.ItemRoster.AddToCounts(item, random);
        }
    }

    private void AddCarePackage()
    {
        if (!Hero.MainHero.HasAttribute("DwarfBrewersI")) return;

        var items = TaleWorlds.CampaignSystem.Extensions.Items.AllTradeGoods.WhereQ(x =>
            x.ItemCategory == DefaultItemCategories.Beer
            || x.ItemCategory == DefaultItemCategories.Meat
            || x.ItemCategory == DefaultItemCategories.Cheese
            || x.ItemCategory == DefaultItemCategories.Grain).ToList();

        foreach (var item in items)
        {
            var random = MBRandom.RandomInt(5, 15);
            if (Hero.MainHero.HasAttribute("DwarfBrewersII"))
            {
                random = MBRandom.RandomInt(15, 25);
            }
            else if (Hero.MainHero.HasAttribute("DwarfBrewersIII"))
            {
                random = MBRandom.RandomInt(25, 30);
            }

            PartyBase.MainParty.ItemRoster.AddToCounts(item, random);
        }
    }

    private void OnGameMenuOpened(MenuCallbackArgs obj)
    {
        SpawnGuildmastersIfNeeded();
    }

    private void SpawnGuildmastersIfNeeded()
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement == null) return;

        if (!settlement.IsDwarfKarak()) return;

        foreach (var template in _templates.Where(template => !IsGuildMasterInDesignatedLocation(settlement, template.location, template.template)))
        {
            SpawnGuildMaster(settlement, true, template.location, template.template);
        }
    }

    private void SpawnGuildMaster(Settlement settlement, bool forceSpawn, string locationId, string template)
    {
        if (!_settlementToGuildmasters.TryGetValue(settlement.StringId, out var heroIds))
        {
            return;
        }
        Hero hero = null;
        foreach (var heroId in heroIds)
        {
            hero = Hero.FindFirst(x => x.StringId == heroId);
            if (hero.Template.StringId == template)
            {
                break;
            }
        }
        if (hero == null) return;

        var heroLocation = settlement.LocationComplex.GetLocationWithId(locationId);

        if (heroLocation == null) return;

        var currentLocation = settlement.LocationComplex.GetLocationOfCharacter(hero);
        if (currentLocation == null) return;
        var locationCharacter = settlement.LocationComplex.GetLocationCharacterOfHero(hero);

        if (currentLocation != heroLocation) settlement.LocationComplex.ChangeLocation(locationCharacter, currentLocation, heroLocation);

    }

    private bool IsGuildMasterInDesignatedLocation(Settlement settlement, string templateLocation, string templateTemplate)
    {
        var locationId = templateLocation;
        var location = settlement.LocationComplex.GetLocationWithId(locationId);

        if (location == null) return false;
        var characters = location.GetCharacterList().ToList().Select(x => x.Character);

        return characters.Select(character => character.HeroObject).Where(hero => hero != null && hero.Template != null).Any(hero => templateTemplate == hero.Template.StringId);
    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        AddRuneSmithDialogue(campaignGameStarter);
        AddEngineerDialogue(campaignGameStarter);
        AddGemCutterDialogue(campaignGameStarter);
        AddBrewerDialogue(campaignGameStarter);
        AddWarriorDialogue(campaignGameStarter);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_decline", "start", "close_window", GameTexts.FindText("tor_dw_guildmaster_reject_non_dwarf").ToString(),
            () => IsGuildMaster() && Hero.MainHero.Culture.StringId != TORConstants.Cultures.DAWI, null, 200);

    }

    private bool IsGuildMaster()
    {
        var currentSettlment = Hero.MainHero.CurrentSettlement;
        if (currentSettlment == null) return false;

        var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;

        if (partner == null) return false;

        if (_settlementToGuildmasters.TryGetValue(currentSettlment.StringId, out List<string> list))
        {
            return list.Any(heroID => heroID == partner.StringId);
        }

        return false;
    }

    private void AddRuneSmithDialogue(CampaignGameStarter campaignGameStarter)
    {


        AddDialogStart(campaignGameStarter, "runesmith", IsRuneLord, out string hub, out string reintro);
        //add Rune crafting - Enchanting 
        AddUnlockInfoDialogues(campaignGameStarter, "runesmith", hub, reintro);
        AddOathGoldDialog(campaignGameStarter, "runesmith", reintro);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_learn_rune_magic_p", hub, reintro,
            GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_learn_rune_magic_p").ToString(),
    () => Hero.MainHero.HasAttribute("PlayerRunesmith") || Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.HasAttribute("Runesmith")) && Hero.MainHero.PartyBelongedTo.HasAnvilOfDoom(), openbookconsequence, 200);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_1_p", hub, "tor_dw_guildmaster_rune_smith_hub_rune_lord_career",
            GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_1_p").ToString(),
            () => !Hero.MainHero.HasAttribute("PlayerRunesmith"), () => FinalizeCareerQuest("runelord_quest_1", 3, "PlayerRunesmith"), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2_p", hub, "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord",
            GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2_p").ToString(),
            () => Hero.MainHero.HasAttribute("PlayerRunesmith") && !Hero.MainHero.HasAttribute("PlayerRunelord"), () => FinalizeCareerQuest("runelord_quest_2", 5, "PlayerRunelord"), 200);

        //HUB
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_buy_equipment_p", hub, "tor_dw_guildmaster_rune_smith_buy_equipment", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_buy_equipment_p").ToString(),
            () => Hero.MainHero.HasAttribute("RuneSmithI"), null, 200);
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_deliver_steel_p", hub, "tor_dw_guildmaster_rune_smith_deliver_steel", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_deliver_steel_p").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_quit_p", hub, "close_window", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_quit_p").ToString(),
            null, null, 200);

        void openbookconsequence()
        {
            var state = Game.Current.GameStateManager.CreateState<SpellBookState>();
            state.IsTrainerMode = true;
            state.TrainerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            Game.Current.GameStateManager.PushState(state);
        }
        //careerRunelord rank 2 condition
        void FinalizeCareerQuest(string id, int finalCount, string attributeId)
        {
            var quest = Campaign.Current.QuestManager.Quests.FirstOrDefault(x => x.StringId == id);

            if (quest == null) return;

            if (quest.JournalEntries.Count > finalCount - 1) // easiest way to check if all conditions were fullfilled
            {
                quest.JournalEntries[finalCount - 1].UpdateCurrentProgress(1);
                quest.CompleteQuestWithSuccess();
                Hero.MainHero.AddAttribute(attributeId);
            }
        }
        void UnlockRuneLordCareerTier2()
        {
            var anvilOfDoom = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_anvil_of_doom");
            Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(anvilOfDoom, 1);

            Hero.MainHero.AddAttribute("SpellCaster");
            Hero.MainHero.AddAbility("HearthAndHome");

            Hero.MainHero.AddSkillXp(TORSkills.Spellcraft, 5000);
            Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
            Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
        }
        //Runelord Career real talk
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_0", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career", reintro,
            GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_0").ToString(),
            () => !Hero.MainHero.HasAttribute("PlayerRunesmith"), () =>
            {
                var quest = TORQuestHelper.GetCurrentQuest<RunesmithQuest>("runelord_quest_1", true, IsRunelordInFront, out var existent);

                if (!existent)
                {
                    quest.StartQuest();
                }

            }, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career").ToString(),
            () => Hero.MainHero.HasAttribute("PlayerRunesmith"), null);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2").ToString(),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3").ToString(),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", reintro, GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4").ToString(),
            null, UnlockRuneLordCareerTier2, 200);

        // Chapter 2
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord_0", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", reintro, GameTexts.FindText("str_tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord_0").ToString(),
            () => !Hero.MainHero.HasAttribute("PlayerRunelord"), () =>
            {
                var quest = TORQuestHelper.GetCurrentQuest<RunelordQuest>("runelord_quest_2", true, IsRunelordInFront, out var existent);

                if (!existent)
                {
                    quest.StartQuest();
                }

            }, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord").ToString(),
            () => Hero.MainHero.HasAttribute("PlayerRunelord"), null);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", reintro, GameTexts.FindText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2").ToString(), null, null);

        //buy equipment
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_buy_equipment", "tor_dw_guildmaster_rune_smith_buy_equipment", reintro, TORTextHelper.GetText("tor_dw_shop_show_goods_text", "Sure let me show what I got"),
            null, OpenRuneLordShop, 200);

        // Deliver Steel

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_deliver_steel", "tor_dw_guildmaster_rune_smith_deliver_steel", "tor_dw_guildmaster_rune_smith_deliver_steel_p", GameTexts.FindText("tor_dw_guildmaster_rune_smith_deliver_steel").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_deliver_steel_accept_p", "tor_dw_guildmaster_rune_smith_deliver_steel_p", reintro, GameTexts.FindText("tor_dw_guildmaster_rune_smith_deliver_steel_accept_p").ToString(),
            HasAnyMetal, () => DeliverSteel(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_deliver_steel_decline_p", "tor_dw_guildmaster_rune_smith_deliver_steel_p", reintro, GameTexts.FindText("tor_dw_guildmaster_rune_smith_deliver_steel_decline_p").ToString(),
            null, null, 200);

        bool IsRunelordInFront(Hero hero)
        {
            if (hero == Hero.OneToOneConversationHero && hero.Template.StringId == _templateRuneSmith.template)
            {
                return true;
            }

            return false;
        }

        bool IsRuneLord()
        {
            var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;
            if (partner == null) return false;
            return partner.Template.StringId == _templateRuneSmith.template;
        }



        void OpenRuneLordShop()
        {

            ItemRoster roster = new ItemRoster();

            var items = new MBList<ItemObject>();

            if (Hero.MainHero.HasAttribute("RuneSmithI"))
            {
                items.AppendList(MBObjectManager.Instance.GetObjectTypeList<ItemObject>().WhereQ(x => x.Culture?.StringId == TORConstants.Cultures.DAWI && x.IsTorItem() && (x.IsMeleeWeapon()) && x.Tier < ItemObject.ItemTiers.Tier3).ToMBList());
            }

            if (Hero.MainHero.HasAttribute("RuneSmithII"))
            {
                items.AppendList(MBObjectManager.Instance.GetObjectTypeList<ItemObject>().WhereQ(x => x.Culture?.StringId == TORConstants.Cultures.DAWI && x.IsTorItem() && (x.IsMeleeWeapon() || x.IsArmor()) && x.Tier < ItemObject.ItemTiers.Tier4).ToMBList());
            }
            if (Hero.MainHero.HasAttribute("RuneSmithIII"))
            {
                items.AppendList(MBObjectManager.Instance.GetObjectTypeList<ItemObject>().WhereQ(x => x.Culture?.StringId == TORConstants.Cultures.DAWI && x.IsTorItem() && (x.IsMeleeWeapon() || (x.IsArmor() && x.Tier > ItemObject.ItemTiers.Tier4))).ToMBList());
                var anvilOfDoom = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_anvil_of_doom");
                items.Add(anvilOfDoom);
            }


            items.WhereQ(x => !x.IsCraftedByPlayer || x.HasAnyLootTraits()).ToMBList().ForEach(x => roster.Add(new ItemRosterElement(x, MBRandom.RandomInt(1, 2))));

            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }


        bool HasAnyMetal()
        {
            var roster = Hero.MainHero.PartyBelongedTo.ItemRoster;
            return roster.AnyQ(x =>
                x.EquipmentElement.Item == DefaultItems.IronIngot4 && x.Amount >= 25 ||
                x.EquipmentElement.Item == DefaultItems.IronIngot5 && x.Amount >= 10 ||
                x.EquipmentElement.Item == DefaultItems.IronIngot6 && x.Amount >= 10);
        }

        void DeliverSteel()
        {
            var roster = Hero.MainHero.PartyBelongedTo.ItemRoster;
            var selectable = new List<InquiryElement>();

            foreach (var element in roster)
            {
                var item = element.EquipmentElement.Item;
                if (item == null) continue;

                if (item == DefaultItems.IronIngot4 && element.Amount >= MinimumSteelAmount)
                {
                    var itemTitle = GameTexts.FindText("tor_dw_rune_smith_deliver_steel_item_title", "lesserSteel");
                    var hint = GameTexts.FindText("tor_dw_rune_smith_deliver_steel_item_hint", "lesserSteel");
                    hint.SetTextVariable("STEEL_COUNT", MinimumSteelAmount);
                    hint.SetTextVariable("OATH_GOLD_GAIN_STEEL", SteelGain);
                    selectable.Add(new InquiryElement(item, itemTitle.ToString(), new ItemImageIdentifier(item), true, hint.ToString()));
                    continue;
                }
                if (item == DefaultItems.IronIngot5 && element.Amount >= MinimumFineSteelAmount)
                {
                    var itemTitle = GameTexts.FindText("tor_dw_rune_smith_deliver_steel_item_title", "regularSteel");
                    var hint = GameTexts.FindText("tor_dw_rune_smith_deliver_steel_item_hint", "regularSteel");
                    hint.SetTextVariable("STEEL_COUNT", MinimumFineSteelAmount);
                    hint.SetTextVariable("OATH_GOLD_GAIN_STEEL", FineSteelGain);
                    selectable.Add(new InquiryElement(item, itemTitle.ToString(), new ItemImageIdentifier(item), true, hint.ToString()));
                    continue;
                }
                if (item == DefaultItems.IronIngot6 && element.Amount >= MinimumGromrilAmount)
                {
                    var itemTitle = GameTexts.FindText("tor_dw_rune_smith_deliver_steel_item_title", "gromril");
                    var hint = GameTexts.FindText("tor_dw_rune_smith_deliver_steel_item_hint", "gromril");
                    hint.SetTextVariable("STEEL_COUNT", MinimumGromrilAmount);
                    hint.SetTextVariable("OATH_GOLD_GAIN_STEEL", GromrilGain);
                    selectable.Add(new InquiryElement(item, itemTitle.ToString(), new ItemImageIdentifier(item), true, hint.ToString()));
                }
            }
            var title = GameTexts.FindText("tor_dw_rune_smith_deliverSteel_prompt_title");
            var description = GameTexts.FindText("tor_dw_rune_smith_deliverSteel_prompt_description");

            var inquirydata = new MultiSelectionInquiryData(title.ToString(), description.ToString(), selectable, true, 1, 3, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                AddOathGoldForSteel, null);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }


        void AddOathGoldForSteel(List<InquiryElement> inquiryElements)
        {
            var oathGold = 0;
            foreach (var element in inquiryElements)
            {
                var reduced = 0;

                var item = (ItemObject)element.Identifier;

                if (item == DefaultItems.IronIngot4)
                {
                    reduced = -MinimumSteelAmount;
                    oathGold += SteelGain;
                }
                if (item == DefaultItems.IronIngot5)
                {
                    reduced = -MinimumFineSteelAmount;
                    oathGold += FineSteelGain;
                }
                if (item == DefaultItems.IronIngot6)
                {
                    reduced = -MinimumGromrilAmount;
                    oathGold += GromrilGain;
                }

                Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item, reduced);
            }

            if (Hero.MainHero.HasCareerChoice("LegacyOfGrungniPassive1"))
            {
                var choice = TORCareerChoices.GetChoice("LegacyOfGrungniPassive1");
                oathGold = (int)((oathGold) * (1 + choice.GetPassiveValue()));
            }

            Hero.MainHero.AddCultureSpecificCustomResource(oathGold);
        }
    }

    private void SpendOathGold(string guildmaster)
    {
        var selectableOptions = new List<InquiryElement>();
        var currentOathGold = Hero.MainHero.GetCultureSpecificCustomResourceValue();

        GameTexts.SetVariable("OATHGOLD_SYMBOL", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

        if (!GameTexts.TryGetText("oath_gold_spending_title", out var title, guildmaster))
        {
            title = TORTextHelper.GetTextObject("tor_dw_oath_gold_spending_title_text", "Spend Oath Gold");
        }

        if (!GameTexts.TryGetText("oath_gold_spending_description", out var description, guildmaster))
        {
            description = TORTextHelper.GetTextObject("tor_dw_oath_gold_spending_description_text", "Select how much Oath Gold you want to spend");
        }

        if (currentOathGold < 20)
        {
            return;
        }

        var value = 0;

        if (currentOathGold >= 20)
        {
            value = 20;
            var option = new TextObject("{OATHGOLD_COST}{OATHGOLD_SYMBOL}");
            option.SetTextVariable("OATHGOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_dw_spend_oath_gold_hint_text", "Spend {OATHGOLD_COST} Oath Gold on this guild");
            hint.SetTextVariable("OATHGOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        if (currentOathGold >= 50)
        {
            value = 50;
            var option = new TextObject("{OATHGOLD_COST}{OATHGOLD_SYMBOL}");
            option.SetTextVariable("OATHGOLD_COST", value);
            GameTexts.SetVariable("OATHGOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_dw_spend_oath_gold_hint_text", "Spend {OATHGOLD_COST} Oath Gold on this guild");
            hint.SetTextVariable("OATHGOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        if (currentOathGold >= 100)
        {
            value = 100;
            var option = new TextObject("{OATHGOLD_COST}{OATHGOLD_SYMBOL}");
            option.SetTextVariable("OATHGOLD_COST", value);
            GameTexts.SetVariable("OATHGOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_dw_spend_oath_gold_hint_text", "Spend {OATHGOLD_COST} Oath Gold on this guild");
            hint.SetTextVariable("OATHGOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        if (currentOathGold >= 250)
        {
            value = 250;
            var option = new TextObject("{OATHGOLD_COST}{OATHGOLD_SYMBOL}");
            option.SetTextVariable("OATHGOLD_COST", value);
            GameTexts.SetVariable("OATHGOLD_COST", value);
            var hint = TORTextHelper.GetTextObject("tor_dw_spend_oath_gold_hint_text", "Spend {OATHGOLD_COST} Oath Gold on this guild");
            hint.SetTextVariable("OATHGOLD_COST", value);
            selectableOptions.Add(new InquiryElement(value, option.ToString(), null, true, hint.ToString()));
        }

        _currentGuild = guildmaster;



        var inquirydata = new MultiSelectionInquiryData(title.ToString(), description.ToString(), selectableOptions, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
            AddResourcesToGuild, null);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }



    private void AddGuildBenefits(string guild, string benefitI, string benefitII, string benefitIII)
    {
        var guildValue = _guildValues[guild];
        var level = OathGoldHelper.GetOathGoldForGuildRespect(guildValue);
        switch (level)
        {
            case OathRespectLevel.Respected:
                Hero.MainHero.AddAttribute(benefitIII);
                break;
            case OathRespectLevel.Reliable:
                Hero.MainHero.AddAttribute(benefitII);
                break;
            case OathRespectLevel.Trustworthy:
                Hero.MainHero.AddAttribute(benefitI);
                break;
            case OathRespectLevel.Unknown:
                break;
        }

        TORCampaignEvents.Instance.OnGuildOathLevelChanged(_guildValues[guild], guild);
    }

    private void AddDialogStart(CampaignGameStarter campaignGameStarter, string guild, Func<bool> onCondition, out string hub, out string reintro)
    {
        hub = "tor_dw_guildmaster_" + guild + "_hub";
        reintro = "tor_dw_guildmaster_" + guild + "_start_reintro";
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_1_start", "start", hub, GameTexts.FindText("tor_dw_guildmaster_1_start", guild).ToString(),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_2_start", "start", hub, GameTexts.FindText("tor_dw_guildmaster_2_start", guild).ToString(),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_3_start", "start", hub, GameTexts.FindText("tor_dw_guildmaster_3_start", guild).ToString(),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_4_start", "start", hub, GameTexts.FindText("tor_dw_guildmaster_4_start", guild).ToString(),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_start_reintro", reintro, "tor_dw_guildmaster_" + guild + "_hub", GameTexts.FindText("tor_dw_guildmaster_reintro", guild).ToString(),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);
    }

    private void AddOathGoldDialog(CampaignGameStarter campaignGameStarter, string guild, string reintro)
    {
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_hub_oath_gold_" + guild, "tor_dw_guildmaster_" + guild + "_hub", "tor_dw_guildmaster_" + guild + "_oath_gold", GameTexts.FindText("tor_dw_guildmaster_hub_oath_gold", guild).ToString(),
            () => _guildValues[guild] < MAXIMUMVALUE, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_oath_gold", "tor_dw_guildmaster_" + guild + "_oath_gold", "tor_dw_guildmaster_" + guild + "_oath_gold_p", GameTexts.FindText("tor_dw_guildmaster_oath_gold", guild).ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_" + guild + "_oath_gold_accept_p", "tor_dw_guildmaster_" + guild + "_oath_gold_p", "tor_dw_guildmaster_" + guild + "_oath_gold_end", GameTexts.FindText("tor_dw_guildmaster_oath_gold_accept_p", guild).ToString(),
            null, () => SpendOathGold(guild), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_" + guild + "_oath_gold_decline_p", "tor_dw_guildmaster_" + guild + "_oath_gold_p", reintro, GameTexts.FindText("tor_dw_guildmaster_oath_gold_decline_p", guild).ToString(),
            null, null, 200);
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_oath_gold_end", "tor_dw_guildmaster_" + guild + "_oath_gold_end", reintro, GameTexts.FindText("tor_dw_guildmaster_oath_gold_end", guild).ToString(),
            null, null, 200);
    }

    private void AddUnlockInfoDialogues(CampaignGameStarter campaignGameStarter, string guild, string hub, string reintro)
    {

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_" + guild + "_info_p", hub, "tor_dw_guildmaster_" + guild + "_unlock_info", GameTexts.FindText("tor_dw_guildmaster_info_p", guild).ToString(),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_unlock_info", "tor_dw_guildmaster_" + guild + "_unlock_info", "tor_dw_guildmaster_2" + guild + "_unlock_info", GameTexts.FindText("tor_dw_guildmaster_unlock_info", guild).ToString(),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_2" + guild + "_unlock_info", "tor_dw_guildmaster_2" + guild + "_unlock_info", reintro, GameTexts.FindText("tor_dw_guildmaster_unlock_info_2", guild).ToString(),
            null, null, 200);


    }

    private void AddEngineerDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateEngineer.guild;
        AddDialogStart(campaignGameStarter, guild, IsEngineer, out var hub, out var reintro);

        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);

        AddOathGoldDialog(campaignGameStarter, _templateEngineer.guild, reintro);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_hub_buy_weapons_shop_p", hub, "tor_dw_guildmaster_engineer_buy_weapons_shop", TORTextHelper.GetText("tor_dw_engineer_buy_weapons_text", "I need better weapons master engineer"),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_quit_text", "Thats all"),
            null, null, 200);

        //buy equipment
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_engineer_buy_weapons_shop", "tor_dw_guildmaster_engineer_buy_weapons_shop", "tor_dw_guildmaster_engineer_start_reintro", TORTextHelper.GetText("tor_dw_shop_show_goods_text", "Sure let me show what I got"),
            null, OpenEngineerShop, 200);


        bool IsEngineer()
        {
            var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;

            if (partner == null) return false;

            return partner.Template.StringId == _templateEngineer.template;
        }

        void OpenEngineerShop()
        {
            ItemRoster roster = new ItemRoster();

            var items = new MBList<ItemObject>();
            items.Add(MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_ammo_musket_ball"));
            items.Add(MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_gun_beardling_handgun"));
            if (Hero.MainHero.HasAttribute("DwarfEngineersII"))
            {
                items.AppendList(MBObjectManager.Instance.GetObjectTypeList<ItemObject>().WhereQ(x =>
                    x.Culture?.StringId == TORConstants.Cultures.DAWI && x.IsTorItem() &&
                    x.IsGunPowderWeapon() && !x.IsFlameThrowerItem()).ToMBList());

                //Add cannons
            }
            if (Hero.MainHero.HasAttribute("DwarfEngineersIII"))
            {
                items.AppendList(MBObjectManager.Instance.GetObjectTypeList<ItemObject>().WhereQ(x =>
                    x.Culture?.StringId == TORConstants.Cultures.DAWI && x.IsTorItem() &&
                     x.IsFlameThrowerItem()).ToMBList());

            }


            items.ForEach(x => roster.Add(new ItemRosterElement(x, MBRandom.RandomInt(1, 2))));

            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

    }




    private void AddGemCutterDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateGemcutters.guild;
        AddDialogStart(campaignGameStarter, guild, IsGemCutter, out var hub, out var reintro);

        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);

        AddOathGoldDialog(campaignGameStarter, guild, reintro);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_hub_spend_troops_p", hub, "tor_dw_guildmaster_gemcutter_spend_troops", GameTexts.FindText("tor_dw_guildmaster_gemcutter_hub_spend_troops_p").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_expedition_hub_found_artefacts_p", hub, "tor_dw_guildmaster_gemcutter_found_artefacts", GameTexts.FindText("tor_dw_guildmaster_expedition_hub_found_artefacts_p").ToString(),
            () => ActiveExpeditions(), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_expedition_hub_launch_expedition_p", hub, "tor_dw_guildmaster_gemcutter_launch_expedition", GameTexts.FindText("tor_dw_guildmaster_expedition_hub_launch_expedition_p").ToString(),
            () => AbleToLaunchExpeditions(), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_hub_quit_p", hub, "close_window", GameTexts.FindText("tor_dw_guildmaster_gemcutter_hub_quit_p").ToString(),
            null, null, 200);

        // found expedition artefacts info
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_found_artefacts", "tor_dw_guildmaster_gemcutter_found_artefacts", reintro, GameTexts.FindText("tor_dw_guildmaster_gemcutter_found_artefacts").ToString(),
            HasUnresolvedExpeditions, AddExpeditionsReward, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_found_artefacts_no_result", "tor_dw_guildmaster_gemcutter_found_artefacts", reintro, GameTexts.FindText("tor_dw_guildmaster_expedition_found_artefacts_no_expeditions").ToString(),
            null, null, 200);

        //start expedition

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_expedition_launch_expedition_decline", "tor_dw_guildmaster_gemcutter_launch_expedition", reintro, GameTexts.FindText("tor_dw_guildmaster_expedition_launch_expedition_decline").ToString(),
            () => !CanLaunchExpedition(), null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_expedition_launch_expedition", "tor_dw_guildmaster_gemcutter_launch_expedition", "tor_dw_guildmaster_gemcutter_launch_expedition_p", GameTexts.FindText("tor_dw_guildmaster_expedition_launch_expedition").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_launch_expedition_accept_p", "tor_dw_guildmaster_gemcutter_launch_expedition_p", "tor_dw_guildmaster_gemcutter_launch_expedition_end", GameTexts.FindText("tor_dw_guildmaster_gemcutter_launch_expedition_accept_p").ToString(),
            null, () => LaunchExpedition(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_launch_expedition_decline_p", "tor_dw_guildmaster_gemcutter_launch_expedition_p", reintro, GameTexts.FindText("tor_dw_guildmaster_gemcutter_launch_expedition_decline_p").ToString(),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_launch_expedition_end", "tor_dw_guildmaster_gemcutter_launch_expedition_end", reintro, GameTexts.FindText("tor_dw_guildmaster_gemcutter_launch_expedition_end").ToString(),
            null, null, 200);



        // spend troops

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_spend_troops", "tor_dw_guildmaster_gemcutter_spend_troops", "tor_dw_guildmaster_gemcutter_spend_troops_p", GameTexts.FindText("tor_dw_guildmaster_gemcutter_spend_troops").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_spend_troops_accept_p", "tor_dw_guildmaster_gemcutter_spend_troops_p", "tor_dw_guildmaster_gemcutter_spend_troops_end", GameTexts.FindText("tor_dw_guildmaster_gemcutter_spend_troops_accept_p").ToString(),
            null, () => ProvideTroops(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_spend_troops_decline_p", "tor_dw_guildmaster_gemcutter_spend_troops_p", reintro, GameTexts.FindText("tor_dw_guildmaster_gemcutter_spend_troops_decline_p").ToString(),
            null, null, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_spend_troops_end", "tor_dw_guildmaster_gemcutter_spend_troops_end", reintro, GameTexts.FindText("tor_dw_guildmaster_gemcutter_spend_troops_end").ToString(),
            null, null, 200);



        void LaunchExpedition()
        {
            var randomEnd = MBRandom.RandomFloatRanged(3, 10);
            var time = CampaignTime.DaysFromNow(randomEnd * CampaignTime.DaysInWeek).ToDays;

            _guildActions.Add("expedition" + time, time);//try get value to check for an identical key to avoid an exceedingly rare double key exception?
        }

        bool ActiveExpeditions()
        {
            var expeditions = _guildActions.Where(x => x.Key.Contains("expedition"));
            return expeditions.Any();
        }

        bool HasUnresolvedExpeditions()
        {
            var expeditions = _guildActions.Where(x => x.Key.Contains("expedition"));
            return expeditions.Any(entry => entry.Value < CampaignTime.Now.ToDays);
        }

        bool AbleToLaunchExpeditions()
        {
            CalculateMaximumExpeditions();
            return _expeditionMaximum > 0;
        }

        bool CanLaunchExpedition()
        {
            CalculateMaximumExpeditions();
            var expeditions = _guildActions.Where(x => x.Key.Contains("expedition"));
            return expeditions.Count() < _expeditionMaximum;
        }


        void AddExpeditionsReward()
        {
            _currentItems.Clear();
            var rewards = 0;
            var expeditions = _guildActions.Where(x => x.Key.Contains("expedition")).ToList();
            for (int i = expeditions.Count() - 1; i >= 0; i--)
            {
                if ((expeditions[i].Value > CampaignTime.Now.ToDays)) continue;

                rewards++;
                _guildActions.Remove(expeditions[i].Key);
            }

            for (int i = 0; i < rewards; i++)
            {
                var item = GetReward();

                if (item.rewardItem != null)
                {
                    var description = TORTextHelper.GetTextObject("tor_dw_expedition_reward_item_description_text", "Expedition reward item");
                    _currentItems.Add(new InquiryElement(item, item.rewardItem.ToString(), new ItemImageIdentifier(item.rewardItem), true, TORTextHelper.GetText("tor_dw_expedition_reward_item_hint_text", "Expedition reward item")));
                }
                else
                {
                    var description = new TextObject(item.GoldAmount + "{GOLD_ICON}");
                    _currentItems.Add(new InquiryElement(item, new TextObject(item.GoldAmount + "{GOLD_ICON}").ToString(), null, true, description.ToString()));
                }
            }


            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_dw_expedition_rewards_title_text", "Expedition Rewards"), TORTextHelper.GetText("tor_dw_expedition_rewards_description_text", "Your expeditions have returned with rewards"), _currentItems, false, 0, 0, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                affirmed => AddRewardsToInventory(), null);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        void CalculateMaximumExpeditions()
        {//Sly : default value is 0 when the field is initialized I think
            if (Hero.MainHero.HasAttribute("DwarfMinersIII"))
            {
                _expeditionMaximum = 3;
            }
            else if (Hero.MainHero.HasAttribute("DwarfMinersII"))
            {
                _expeditionMaximum = 2;
            }
            else if (Hero.MainHero.HasAttribute("DwarfMinersI"))
            {
                _expeditionMaximum = 1;
            }
        }


        ExpeditionReward GetReward()
        {
            var reward = new ExpeditionReward();

            var chance = MBRandom.RandomFloatRanged(0, 1);

            if (chance < 0.05f)
            {
                reward.rewardItem = GetRewardItem();
            }

            if (chance < 0.10f)
            {
                reward.GoldAmount += 50000;
                return reward;
            }
            if (chance < 0.50f)
            {
                reward.GoldAmount += 10000;
                return reward;
            }

            reward.GoldAmount += 2500;
            return reward;

        }


        void AddRewardsToInventory()
        {
            var gold = 0;
            foreach (var element in _currentItems)
            {
                var reward = (ExpeditionReward)element.Identifier;
                if (reward == null) continue;

                if (reward.GoldAmount > 0)
                {
                    gold += reward.GoldAmount;
                }

                if (reward.rewardItem != null)
                {
                    Hero.MainHero.PartyBelongedTo.ItemRoster.Add(new ItemRosterElement(reward.rewardItem, 1));
                }
            }

            Hero.MainHero.ChangeHeroGold(gold);

            _currentItems.Clear();

        }

        ItemObject GetRewardItem()
        {
            var dwarfitemroster = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .WhereQ(X => X.Culture != null && X.Culture.StringId == TORConstants.Cultures.DAWI && (X.IsWeapon() || X.IsArmor()));
            var foundItem = dwarfitemroster.TakeRandom(1).FirstOrDefault();
            if (foundItem == null) return null;

            var model = (TORBattleRewardModel)Campaign.Current.Models.BattleRewardModel;

            var unlocks = 0;
            if (Hero.MainHero.HasAttribute("DwarfMinersIII"))
            {
                unlocks = 3;
            }
            else if (Hero.MainHero.HasAttribute("DwarfMinersII"))
            {
                unlocks = 2;
            }
            else if (Hero.MainHero.HasAttribute("DwarfMinersI"))
            {
                unlocks = 1;
            }
            var traitCount = MBRandom.RandomInt(0, unlocks);
            var traits = ItemTrait.All.WhereQ(x => x.ItemTraitStringId.Contains("dw_rune") && ItemTrait.IsValidFor(x, foundItem.ItemType)).TakeRandom(traitCount).ToList();

            var traitIds = traits.Select(x => x.ItemTraitStringId).ToList();

            var nameModifier = model.GetNameModifierForTraits(traitCount);

            if (traitCount > 0)
            {
                var item = EnchantmentHelper.CreateEnchantedItem(foundItem, traitIds, nameModifier);
                return item;
            }

            return foundItem;
        }

        bool IsGemCutter()
        {
            var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;
            if (partner == null) return false;
            return partner.Template.StringId == _templateGemcutters.template;
        }

        void ProvideTroops()
        {
            PartyScreenHelper.OpenScreenAsQuest(TroopRoster.CreateDummyTroopRoster(), TORTextHelper.GetTextObject("tor_dw_donate_miners_to_guild_text", "Donate Miners to the Miners guild"), 500, 0, null,
                TranferCompleted, IsTransferableMinerUnit);
        }

        void TranferCompleted(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftprisonroster, PartyBase rightownerparty,
            TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            if (fromcancel) return;
            var gainedOathGold = 0;
            foreach (var element in leftmemberroster.GetTroopRoster())
            {
                gainedOathGold += 10;
                gainedOathGold += element.Character.Tier;
            }

            Hero.MainHero.AddCultureSpecificCustomResource(gainedOathGold);
        }

        bool IsTransferableMinerUnit(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftownerparty)
        {
            if (type != PartyScreenLogic.TroopType.Member) return false;
            if (character.IsHero) return false;
            return character.Culture.StringId == TORConstants.Cultures.DAWI && character.HasAttribute("DwarfMiner");
        }
    }

    private void AddBrewerDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateBrewer.guild;
        AddDialogStart(campaignGameStarter, guild, IsBrewer, out var hub, out var reintro);

        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);

        AddOathGoldDialog(campaignGameStarter, guild, reintro);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_spend_food_p", hub, "tor_dw_guildmaster_brewer_hub_spend_food", GameTexts.FindText("tor_dw_guildmaster_brewer_hub_spend_food_p").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_quit_p", hub, "close_window", GameTexts.FindText("tor_dw_guildmaster_brewer_hub_quit_p").ToString(),
            null, null, 200);


        // spend food
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_brewer_hub_spend_food", "tor_dw_guildmaster_brewer_hub_spend_food", "tor_dw_guildmaster_brewer_hub_spend_food_p", GameTexts.FindText("tor_dw_guildmaster_brewer_hub_spend_food").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_spend_food_accept_p", "tor_dw_guildmaster_brewer_hub_spend_food_p", "tor_dw_guildmaster_brewer_hub_spend_food_end", GameTexts.FindText("tor_dw_guildmaster_brewer_hub_spend_food_accept_p").ToString(),
            null, () => SpendFood(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_spend_food_decline_p", "tor_dw_guildmaster_brewer_hub_spend_food_p", reintro, GameTexts.FindText("tor_dw_guildmaster_brewer_hub_spend_food_decline_p").ToString(),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_brewer_hub_spend_food_end", "tor_dw_guildmaster_brewer_hub_spend_food_end", reintro, GameTexts.FindText("tor_dw_guildmaster_brewer_hub_spend_food_end").ToString(),
            null, null, 200);


        bool IsBrewer()
        {
            var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;

            if (partner == null) return false;

            var value = partner.Template.StringId == _templateBrewer.template;
            return value;
        }


        void SpendFood()
        {
            var roster = Hero.MainHero.PartyBelongedTo.ItemRoster;
            var selectable = new List<InquiryElement>();

            foreach (var element in roster)
            {
                var item = element.EquipmentElement.Item;
                if (item == null) continue;

                var itemTitle = GameTexts.FindText("tor_dw_brewers_deliverWheat_item_title", "wheat");
                var hint = GameTexts.FindText("tor_dw_brewers_deliverWheat_item_hint", "wheat");
                if (item == DefaultItems.Grain && element.Amount >= 30)
                {

                    hint.SetTextVariable("WHEAT_COUNT", 30);
                    hint.SetTextVariable("OATH_GOLD_GAIN_WHEAT", SteelGain);
                    selectable.Add(new InquiryElement(30.ToString(), itemTitle.ToString(), new ItemImageIdentifier(item), true, hint.ToString()));
                    continue;
                }
                if (item == DefaultItems.Grain && element.Amount >= 50)
                {
                    hint.SetTextVariable("WHEAT_COUNT", 50);
                    hint.SetTextVariable("OATH_GOLD_GAIN_WHEAT", SteelGain);
                    selectable.Add(new InquiryElement(50.ToString(), itemTitle.ToString(), new ItemImageIdentifier(item), true, hint.ToString()));
                    continue;
                }
                if (item == DefaultItems.Grain && element.Amount >= 100)
                {
                    hint.SetTextVariable("WHEAT_COUNT", 100);
                    hint.SetTextVariable("OATH_GOLD_GAIN_WHEAT", SteelGain);
                    selectable.Add(new InquiryElement(100.ToString(), itemTitle.ToString(), new ItemImageIdentifier(item), true, hint.ToString()));
                }
            }
            var title = GameTexts.FindText("tor_dw_brewers_deliverWheat_prompt_title");
            var description = GameTexts.FindText("tor_dw_brewers_deliverWheat_prompt_description");

            var inquirydata = new MultiSelectionInquiryData(title.ToString(), description.ToString(), selectable, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                AddOathGoldForGrain, null);

            void AddOathGoldForGrain(List<InquiryElement> inquiryElements)
            {
                var amout = int.Parse(inquiryElements.FirstOrDefault().Identifier as string);
                var grain = DefaultItems.Grain;


                Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(grain, -amout);


                Hero.MainHero.AddCultureSpecificCustomResource(amout / 2);
            }

            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

    }

    private void AddWarriorDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateWarrior.guild;
        AddDialogStart(campaignGameStarter, guild, IsWarrior, out string hub, out var reintro);

        //oath gold
        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);
        AddOathGoldDialog(campaignGameStarter, guild, reintro);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_hub_spend_troops_p", hub, "tor_dw_guildmaster_warrior_spend_troops", GameTexts.FindText("tor_dw_guildmaster_warrior_hub_spend_troops_p").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_hub_influence_for_oath_p", hub, "tor_dw_guildmaster_warrior_influence_for_oath",
            GameTexts.FindText("tor_dw_guildmaster_warrior_influence_for_oath_p").ToString(), null, null, 200);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_quit_text", "Thats all"),
            null, null, 200);


        // spend troops

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_warrior_spend_troops", "tor_dw_guildmaster_warrior_spend_troops", "tor_dw_guildmaster_warrior_spend_troops_p", GameTexts.FindText("tor_dw_guildmaster_warrior_spend_troops").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_spend_troops_accept_p", "tor_dw_guildmaster_warrior_spend_troops_p", "tor_dw_guildmaster_warrior_spend_troops_end", GameTexts.FindText("tor_dw_guildmaster_warrior_spend_troops_accept_p").ToString(),
            HasTransferableTroops, () => ProvideTroops(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_spend_troops_decline_p", "tor_dw_guildmaster_warrior_spend_troops_p", reintro, GameTexts.FindText("tor_dw_guildmaster_warrior_spend_troops_decline_p").ToString(),
            null, null, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_warrior_spend_troops_end", "tor_dw_guildmaster_warrior_spend_troops_end", reintro, GameTexts.FindText("tor_dw_guildmaster_warrior_spend_troops_end").ToString(),
            null, null, 200);


        // influence

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_warrior_influence_for_oath", "tor_dw_guildmaster_warrior_influence_for_oath", "tor_dw_guildmaster_warrior_influence_for_oath_p", GameTexts.FindText("tor_dw_guildmaster_warrior_influence_for_oath").ToString(),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_influence_for_oath_accept_p", "tor_dw_guildmaster_warrior_influence_for_oath_p", "tor_dw_guildmaster_warrior_influence_for_oath", GameTexts.FindText("tor_dw_guildmaster_warrior_influence_for_oath_accept_p").ToString(),
            CanTransferOathGoldForInfluence, () => BuyInfluenceForOathGold(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_influence_for_oath_decline_p", "tor_dw_guildmaster_warrior_influence_for_oath_p", reintro, GameTexts.FindText("tor_dw_guildmaster_warrior_influence_for_oath_decline_p").ToString(),
            null, null, 200);

        bool CanTransferOathGoldForInfluence()
        {
            return Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 100;
        }
        void BuyInfluenceForOathGold()
        {
            Hero.MainHero.AddInfluenceWithKingdom(250);
            Hero.MainHero.AddCultureSpecificCustomResource(-100);
        }

        void ProvideTroops()
        {
            PartyScreenHelper.OpenScreenAsQuest(TroopRoster.CreateDummyTroopRoster(), TORTextHelper.GetTextObject("tor_dw_donate_warriors_to_guild_text", "Donate Warriors to the Warriors guild"), 500, 0, null,
                TranferCompleted, IsTransferableVeteranUnit);
        }

        void TranferCompleted(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftprisonroster, PartyBase rightownerparty,
            TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            if (fromcancel) return;
            var gainedOathGold = 0;
            foreach (var element in leftmemberroster.GetTroopRoster())
            {
                gainedOathGold += element.Character.Tier * element.Number * 15;
            }

            Hero.MainHero.AddCultureSpecificCustomResource(gainedOathGold);
        }

        bool HasTransferableTroops()
        {
            var roster = Hero.MainHero.PartyBelongedTo.MemberRoster;

            if (roster == null) return false;
            return roster.GetTroopRoster().Any(elem => elem.Character.Culture.StringId == TORConstants.Cultures.DAWI && elem.Character.Tier > 4);
        }

        bool IsTransferableVeteranUnit(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftownerparty)
        {
            if (type != PartyScreenLogic.TroopType.Member) return false;
            if (character.IsHero) return false;
            return character.Culture.StringId == TORConstants.Cultures.DAWI && character.Tier >= 4;
        }

        bool IsWarrior()
        {
            var partner = CharacterObject.OneToOneConversationCharacter?.HeroObject;

            if (partner == null) return false;

            return partner.Template.StringId == _templateWarrior.template;
        }
    }

    private void OnNewGameStarted(CampaignGameStarter campaignGameStarter)
    {
        _guildValues = new Dictionary<string, int>();
        foreach (var template in _templates)
        {
            _guildValues.Add(template.guild, 0);
        }

        foreach (var town in Town.AllTowns)
        {
            var settlement = town.Settlement;
            if (settlement.Culture.StringId == TORConstants.Cultures.DAWI)
            {
                CreateGuildMasters(settlement);
            }
        }
    }

    private void CreateGuildMasters(Settlement settlement)
    {
        var guildmasters = new List<string>();
        foreach (var template in _templates)
        {
            var hero = CreateGuildMaster(settlement, template.template);
            guildmasters.Add(hero.StringId);
        }

        _settlementToGuildmasters.Add(settlement.StringId, guildmasters);
    }

    private Hero CreateGuildMaster(Settlement settlement, string templateId)
    {
        var template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);
        if (template == null) return null;

        var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
        hero.SupporterOf = settlement.OwnerClan;
        hero.Culture = settlement.Culture;
        var nameObject = template.GetName();
        nameObject.SetTextVariable("FIRSTNAME", hero.FirstName);
        hero.SetName(nameObject, hero.FirstName);
        HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
        hero.CharacterObject.HiddenInEncyclopedia = true;
        return hero;
    }


    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_guildValues", ref _guildValues);
        dataStore.SyncData("_settlementToGuidmasters", ref _settlementToGuildmasters);
        dataStore.SyncData("_lastTimeVistedTown", ref _lastTimeVistedTown);
        dataStore.SyncData("_expeditionMaximum", ref _expeditionMaximum);
        dataStore.SyncData("_guildActions", ref _guildActions);
        dataStore.SyncData("_craftingOrdersCompleted", ref _craftingOrdersCompleted);
    }


    public class ExpeditionReward
    {
        public ItemObject rewardItem;
        public int GoldAmount;
    }
}