using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
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
    private const int WheatToOathGoldGain = 2;
    private const int ArtilleryCrewOathGoldCost = 50;
    private const int ArtilleryCrewGoldCost = 500;
    private const int RangerOathGoldCost = 20;
    private const int RangerGoldCost = 500;
    private const int MinimumWarriorTier = 5;
    private const int WarriorOathGoldPerTier = 7;
    private const int InfluenceOathGoldCost = 100;
    private const int InfluenceGainAmount = 125;
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
        // Initialize guild icon text variables
        MBTextManager.SetTextVariable("ENGINEERS_GUILD_ICON", "<img src=\"engineers_icon\" extend=\"8\">");
        MBTextManager.SetTextVariable("RUNESMITHS_GUILD_ICON", "<img src=\"runesmiths_icon\" extend=\"8\">");
        MBTextManager.SetTextVariable("MINERS_GUILD_ICON", "<img src=\"miners_icon\" extend=\"8\">");
        MBTextManager.SetTextVariable("BREWERS_GUILD_ICON", "<img src=\"brewers_icon\" extend=\"8\">");
        MBTextManager.SetTextVariable("WARRIORS_GUILD_ICON", "<img src=\"warriors_icon\" extend=\"8\">");

        AddRuneSmithDialogue(campaignGameStarter);
        AddEngineerDialogue(campaignGameStarter);
        AddGemCutterDialogue(campaignGameStarter);
        AddBrewerDialogue(campaignGameStarter);
        AddWarriorDialogue(campaignGameStarter);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_decline", "start", "close_window", TORTextHelper.GetText("tor_dw_guildmaster_reject_non_dwarf", "You dont belong here. begone"),
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
            TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_learn_rune_magic_p", "I wish to learn more of Rune Magic and the Anvils of Doom."),
    () => Hero.MainHero.HasAttribute("PlayerRunesmith") || Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.HasAttribute("Runesmith")) && Hero.MainHero.PartyBelongedTo.HasAnvilOfDoom(), openbookconsequence, 200);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_1_p", hub, "tor_dw_guildmaster_rune_smith_hub_rune_lord_career",
            TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_1_p", "Rhunrik, I wish to prove myself to the Burudin and raise my position within the guild."),
            () => !Hero.MainHero.HasAttribute("PlayerRunesmith") && Hero.MainHero.HasCareer(TORCareers.Runelord), () => FinalizeCareerQuest("runelord_quest_1", 3, "PlayerRunesmith"), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2_p", hub, "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord",
            TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2_p", "Rhunrik, I wish to further raise my position within the guild."),
            () => Hero.MainHero.HasCareer(TORCareers.Runelord) && Hero.MainHero.HasAttribute("PlayerRunesmith") && !Hero.MainHero.HasAttribute("PlayerRunelord"), () => FinalizeCareerQuest("runelord_quest_2", 5, "PlayerRunelord"), 200);

        //HUB
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_buy_equipment_p", hub, "tor_dw_guildmaster_rune_smith_buy_equipment", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_buy_equipment_p", "Can I order gear produced by your Guild?"),
            () => Hero.MainHero.HasAttribute("RuneSmithI"), null, 200);
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_deliver_steel_p", hub, "tor_dw_guildmaster_rune_smith_deliver_steel", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_deliver_steel_p", "Will you take these valuable metals?"),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_quit_p", "That will be all."),
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
            TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_0", "Such respect is not easily given, prove yourself under Grungni's stern gaze and we'll see if you are worthy."),
            () => !Hero.MainHero.HasAttribute("PlayerRunesmith") && Hero.MainHero.HasCareer(TORCareers.Runelord), () =>
            {
                var quest = TORQuestHelper.GetCurrentQuest<RunesmithQuest>("runelord_quest_1", true, IsRunelordInFront, out var existent);

                if (!existent)
                {
                    quest.StartQuest();
                }

            }, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career", "By Grungni's beard! It has been a long time indeed since I have seen someone with such potential."),
            () => Hero.MainHero.HasAttribute("PlayerRunesmith") && Hero.MainHero.HasCareer(TORCareers.Runelord), null);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_2", "In the name of the Burudin, I hereby declare you part of the Rhunki. Take pride in your achievements, lad. Although I fear the celebrations will need to be postponed as desperate times call for desperate measures. And dark clouds are hanging above the Karaz Ankor."),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_3", "I am bound to the Grongaz, but you can travel and battle as you please. You have proven yourself worthy, and that is why I share with you my Anvil of Doom. But know this, there are binding oaths of secrecy cast upon it; one cannot simply abandon the Anvil. Protect it with your life."),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", reintro, TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_4", "You may use it in battle as you see fit and unleash the ancient runes inscribed upon its surface, but be wary, its runes are powerful, and only the most skilled Rhunki can ever hope to awaken them. The most straightforward combination is the Rune of Hearth and Home. Never forget where you belong and where you came from."),
            null, UnlockRuneLordCareerTier2, 200);

        // Chapter 2
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord_0", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", reintro, TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord_0", "You get ahead of yourself, Rhunki. Not a long time ago we were still discussing the fundamentals, and now you think you derserve the title of a Runelord? Travel the Karaz Ankor, help our kin in need. Prove yourself worthy of this great honour."),
            () => !Hero.MainHero.HasAttribute("PlayerRunelord"), () =>
            {
                var quest = TORQuestHelper.GetCurrentQuest<RunelordQuest>("runelord_quest_2", true, IsRunelordInFront, out var existent);

                if (!existent)
                {
                    quest.StartQuest();
                }

            }, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord", "Not since the times of Thorek or even Kragg has a Rhunki displayed such skill in the arts of the runes!"),
            () => Hero.MainHero.HasAttribute("PlayerRunelord"), null);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", "tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", reintro, TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_hub_rune_lord_career_runelord2", "As Thungni is my witness, henceforth you shall be known as a Rhunriki; may you walk with pride amongst the forges of our Ancestors. Now, let us discuss the secrets of the Third Rune…"), null, null);

        //buy equipment
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_buy_equipment", "tor_dw_guildmaster_rune_smith_buy_equipment", reintro, TORTextHelper.GetText("tor_dw_shop_show_goods_text", "Very well, take a look at our inventions."),
            null, OpenRuneLordShop, 200);

        // Deliver Steel

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_rune_smith_deliver_steel", "tor_dw_guildmaster_rune_smith_deliver_steel", "tor_dw_guildmaster_rune_smith_deliver_steel_p", TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_deliver_steel", "Those are always welcome, what do you wish to donate?"),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_deliver_steel_accept_p", "tor_dw_guildmaster_rune_smith_deliver_steel_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_deliver_steel_accept_p", "Have a look."),
            HasAnyMetal, () => DeliverSteel(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_rune_smith_deliver_steel_decline_p", "tor_dw_guildmaster_rune_smith_deliver_steel_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_rune_smith_deliver_steel_decline_p", "That will be all."),
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
            var excludedItems = new List<string>
            {
                // Weapons
                "tor_dwarf_weapon_1h_axe_of_grimnir",
                "tor_dwarf_weapon_greataxe_001",
                "tor_dwarf_weapon_greataxe_ungrim_001",
                "tor_dwarf_1h_spanner_001",
                "dwarf_1h_engineer_hammer_001",
                "tor_dwarf_weapon_hammer_of_angrund",
                "tor_dwarf_2h_spanner_001",
                "dwarf_2h_engineer_hammer_001",
                // Armors - Head
                "tor_dw_head_helm_ungrim_001",
                "tor_dw_head_helm_ranger_001",
                "tor_dw_head_helm_ranger_002",
                "tor_dw_head_helm_ranger_003",
                "tor_dw_head_helm_ranger_004",
                "tor_dw_head_apprentice_002",
                "tor_dw_head_apprentice_001",
                "tor_dw_head_journeyman_001",
                "tor_dw_head_engineer_001",
                "tor_dw_head_engineer_002",
                // Armors - Shoulder
                "tor_dw_shoulder_cape_ranger_001",
                "tor_dw_shoulder_cape_ranger_002",
                "tor_dw_shoulder_shoulderpads_apprentice_001",
                "tor_dw_shoulder_shoulderpads_journeyman_001",
                "tor_dw_shoulder_shoulderpads_engineer_001",
                "tor_dw_shoulder_shoulderpads_ungrim_001",
                // Armors - Body
                "tor_dw_body_armour_apprentice_001",
                "tor_dw_body_armour_journeyman_001",
                "tor_dw_body_armour_engineer_001",
                "tor_dw_body_armour_ungrim_001",
                "tor_dw_body_armour_ranger_001",
                // Armors - Arms
                "tor_dw_arm_gloves_apprentice_001",
                "tor_dw_arm_gloves_journeyman_001",
                "tor_dw_arm_gloves_engineer_001",
                "tor_dw_arm_bracers_ranger_001",
                // Armors - Legs
                "tor_dw_leg_boots_apprentice_001",
                "tor_dw_leg_boots_journeyman_001",
                "tor_dw_leg_boots_engineer_001",
                "tor_dw_leg_boots_ranger_001"
            };

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


            items.WhereQ(x => (!x.IsCraftedByPlayer || x.HasAnyLootTraits()) && !excludedItems.Contains(x.StringId))
                .ToMBList().ForEach(x => roster.Add(new ItemRosterElement(x, MBRandom.RandomInt(1, 2))));

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

                if (item == DefaultItems.IronIngot4)
                    AddSteelDeliveryOption(selectable, item, element.Amount, MinimumSteelAmount, SteelGain, "lesserSteel");
                else if (item == DefaultItems.IronIngot5)
                    AddSteelDeliveryOption(selectable, item, element.Amount, MinimumFineSteelAmount, FineSteelGain, "regularSteel");
                else if (item == DefaultItems.IronIngot6)
                    AddSteelDeliveryOption(selectable, item, element.Amount, MinimumGromrilAmount, GromrilGain, "gromril");
            }
            var title = TORTextHelper.GetText("tor_dw_rune_smith_deliverSteel_prompt_title", "Deliver Metals");
            var description = TORTextHelper.GetText("tor_dw_rune_smith_deliverSteel_prompt_description", "Select which metals you wish to deliver to the guild.");

            var inquirydata = new MultiSelectionInquiryData(title, description, selectable, true, 1, 3, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
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

    private string GetGuildIcon(string guild)
    {
        return guild switch
        {
            "engineer" => "{ENGINEERS_GUILD_ICON}",
            "runesmith" => "{RUNESMITHS_GUILD_ICON}",
            "miner" => "{MINERS_GUILD_ICON}",
            "brewer" => "{BREWERS_GUILD_ICON}",
            "warrior" => "{WARRIORS_GUILD_ICON}",
            _ => ""
        };
    }

    #region Resource Spending Helpers

    private void AddOathGoldSpendingOption(List<InquiryElement> options, int threshold, float currentAmount)
    {
        if (currentAmount < threshold) return;

        var option = new TextObject("{OATHGOLD_COST}{OATHGOLD_SYMBOL}");
        option.SetTextVariable("OATHGOLD_COST", threshold);
        var hint = TORTextHelper.GetTextObject("tor_dw_spend_oath_gold_hint_text", "Spend {OATHGOLD_COST} Oath Gold on this guild");
        hint.SetTextVariable("OATHGOLD_COST", threshold);
        options.Add(new InquiryElement(threshold, option.ToString(), null, true, hint.ToString()));
    }

    private void AddWheatDonationOption(List<InquiryElement> options, int threshold, int currentAmount, ItemObject grainItem)
    {
        if (currentAmount < threshold) return;

        var gainedOathGold = threshold / WheatToOathGoldGain;
        var itemTitle = TORTextHelper.GetText("tor_dw_brewers_deliverWheat_item_title", "wheat", "Grain", true);
        var hint = TORTextHelper.GetTextObject("tor_dw_brewers_deliverWheat_item_hint", "wheat", "Deliver {WHEAT_COUNT} grain for {OATH_GOLD_GAIN_WHEAT} Oath Gold.", true);
        hint.SetTextVariable("WHEAT_COUNT", threshold);
        hint.SetTextVariable("OATH_GOLD_GAIN_WHEAT", gainedOathGold);
        options.Add(new InquiryElement(threshold.ToString(), itemTitle, new ItemImageIdentifier(grainItem), true, hint.ToString()));
    }

    private void AddSteelDeliveryOption(List<InquiryElement> options, ItemObject steelItem, int currentAmount, int requiredAmount, int oathGoldGain, string variation)
    {
        if (currentAmount < requiredAmount) return;

        var itemTitle = TORTextHelper.GetText("tor_dw_rune_smith_deliver_steel_item_title", variation, steelItem.Name.ToString(), true);
        var hint = TORTextHelper.GetTextObject("tor_dw_rune_smith_deliver_steel_item_hint", variation, "Deliver {STEEL_COUNT} ingots for {OATH_GOLD_GAIN_STEEL} Oath Gold.", true);
        hint.SetTextVariable("STEEL_COUNT", requiredAmount);
        hint.SetTextVariable("OATH_GOLD_GAIN_STEEL", oathGoldGain);
        options.Add(new InquiryElement(steelItem, itemTitle, new ItemImageIdentifier(steelItem), true, hint.ToString()));
    }

    #endregion

    private void SpendOathGold(string guildmaster)
    {
        var selectableOptions = new List<InquiryElement>();
        var currentOathGold = Hero.MainHero.GetCultureSpecificCustomResourceValue();

        GameTexts.SetVariable("OATHGOLD_SYMBOL", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
        var guildIcon = GetGuildIcon(guildmaster);

        if (!GameTexts.TryGetText("oath_gold_spending_title", out var title, guildmaster))
        {
            title = TORTextHelper.GetTextObject("tor_dw_oath_gold_spending_title_text", "Spend Oath Gold");
        }

        // Add guild icon to title
        title = new TextObject(title + " " + guildIcon);

        if (!GameTexts.TryGetText("oath_gold_spending_description", out var description, guildmaster))
        {
            description = TORTextHelper.GetTextObject("tor_dw_oath_gold_spending_description_text", "Select how much Oath Gold you want to spend");
        }

        if (currentOathGold < 20)
        {
            return;
        }

        AddOathGoldSpendingOption(selectableOptions, 20, currentOathGold);
        AddOathGoldSpendingOption(selectableOptions, 50, currentOathGold);
        AddOathGoldSpendingOption(selectableOptions, 100, currentOathGold);
        AddOathGoldSpendingOption(selectableOptions, 250, currentOathGold);

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
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_1_start", "start", hub, TORTextHelper.GetText("tor_dw_guildmaster_1_start", guild, "Greetings, fellow Dwarf.", true),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_2_start", "start", hub, TORTextHelper.GetText("tor_dw_guildmaster_2_start", guild, "Ah, a visitor. What brings you here?", true),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_3_start", "start", hub, TORTextHelper.GetText("tor_dw_guildmaster_3_start", guild, "The ancestors watch over us. How may I assist?", true),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_4_start", "start", hub, TORTextHelper.GetText("tor_dw_guildmaster_4_start", guild, "Welcome to the guild.", true),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_start_reintro", reintro, "tor_dw_guildmaster_" + guild + "_hub", TORTextHelper.GetText("tor_dw_guildmaster_reintro", guild, "Is there anything else you need?", true),
            () => IsGuildMaster() && onCondition() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI, null, 200);
    }

    private void AddOathGoldDialog(CampaignGameStarter campaignGameStarter, string guild, string reintro)
    {
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_hub_oath_gold_" + guild, "tor_dw_guildmaster_" + guild + "_hub", "tor_dw_guildmaster_" + guild + "_oath_gold", TORTextHelper.GetText("tor_dw_guildmaster_hub_oath_gold", guild, "I wish to contribute Oath Gold to the guild.", true),
            () => _guildValues[guild] < MAXIMUMVALUE, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_oath_gold", "tor_dw_guildmaster_" + guild + "_oath_gold", "tor_dw_guildmaster_" + guild + "_oath_gold_p", TORTextHelper.GetText("tor_dw_guildmaster_oath_gold", guild, "Your contribution will be remembered in the Book of Grudges.", true),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_" + guild + "_oath_gold_accept_p", "tor_dw_guildmaster_" + guild + "_oath_gold_p", "tor_dw_guildmaster_" + guild + "_oath_gold_end", TORTextHelper.GetText("tor_dw_guildmaster_oath_gold_accept_p", guild, "Here is my contribution.", true),
            null, () => SpendOathGold(guild), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_" + guild + "_oath_gold_decline_p", "tor_dw_guildmaster_" + guild + "_oath_gold_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_oath_gold_decline_p", guild, "Perhaps another time.", true),
            null, null, 200);
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_oath_gold_end", "tor_dw_guildmaster_" + guild + "_oath_gold_end", reintro, TORTextHelper.GetText("tor_dw_guildmaster_oath_gold_end", guild, "The guild thanks you. Your standing has improved.", true),
            null, null, 200);
    }

    private void AddUnlockInfoDialogues(CampaignGameStarter campaignGameStarter, string guild, string hub, string reintro)
    {
        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_" + guild + "_info_p", hub, "tor_dw_guildmaster_" + guild + "_unlock_info", TORTextHelper.GetText("tor_dw_guildmaster_info_p", guild, "Tell me about the guild benefits.", true),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_" + guild + "_unlock_info", "tor_dw_guildmaster_" + guild + "_unlock_info", "tor_dw_guildmaster_2" + guild + "_unlock_info", TORTextHelper.GetText("tor_dw_guildmaster_unlock_info", guild, "The guild offers various benefits as you gain standing.", true),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_2" + guild + "_unlock_info", "tor_dw_guildmaster_2" + guild + "_unlock_info", reintro, TORTextHelper.GetText("tor_dw_guildmaster_unlock_info_2", guild, "Contribute Oath Gold to increase your standing.", true),
            null, null, 200);
    }

    private void AddEngineerDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateEngineer.guild;
        AddDialogStart(campaignGameStarter, guild, IsEngineer, out var hub, out var reintro);

        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);

        AddOathGoldDialog(campaignGameStarter, _templateEngineer.guild, reintro);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_hub_buy_weapons_shop_p", hub, "tor_dw_guildmaster_engineer_buy_weapons_shop", TORTextHelper.GetText("tor_dw_engineer_buy_weapons_text", "I need better gear, Master Engineer."),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_hub_recruit_crew_p", hub, "tor_dw_guildmaster_engineer_recruit_crew", TORTextHelper.GetText("tor_dw_engineer_recruit_crew_text", "I require reliable crewmen to operate my artillery."),
            () => Hero.MainHero.HasAttribute("DwarfEngineersI"), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_quit_text", "That will be all."),
            null, null, 200);

        //buy equipment
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_engineer_buy_weapons_shop", "tor_dw_guildmaster_engineer_buy_weapons_shop", "tor_dw_guildmaster_engineer_start_reintro", TORTextHelper.GetText("tor_dw_shop_show_goods_text", "Very well, take a look at our inventions."),
            null, OpenEngineerShop, 200);

        //recruit artillery crew
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_engineer_recruit_crew", "tor_dw_guildmaster_engineer_recruit_crew", "tor_dw_guildmaster_engineer_recruit_crew_options",
            TORTextHelper.GetText("tor_dw_engineer_recruit_crew_offer_text", "Aye, a pair of our beardlings are eager to prove themselves. It'll cost ye {ENGINEER_GOLD_PRICE}{GOLD_ICON} and {ENGINEER_OATHGOLD_PRICE} Oathgold."),
            UpdateEngineerRecruitmentPrices, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_recruit_crew_accept_p", "tor_dw_guildmaster_engineer_recruit_crew_options", "tor_dw_guildmaster_engineer_start_reintro",
            TORTextHelper.GetText("tor_dw_engineer_recruit_crew_accept_text", "They'll serve the hold well."),
            HasEnoughForEngineerRecruitment, RecruitEngineerCrew, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_recruit_crew_decline_funds_p", "tor_dw_guildmaster_engineer_recruit_crew_options", "tor_dw_guildmaster_engineer_start_reintro",
            TORTextHelper.GetText("tor_dw_engineer_recruit_crew_no_funds_text", "I don't have enough coin or Oathgold right now."),
            () => !HasEnoughForEngineerRecruitment(), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_engineer_recruit_crew_decline_p", "tor_dw_guildmaster_engineer_recruit_crew_options", "tor_dw_guildmaster_engineer_start_reintro",
            TORTextHelper.GetText("tor_dw_engineer_recruit_crew_decline_text", "Maybe another time."),
            null, null, 200);


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
            items.Add(MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_ammo_musket_ball"));
            items.Add(MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_gun_beardling_handgun"));
            if (Hero.MainHero.HasAttribute("DwarfEngineersI"))
            {
                //Add blasting charges
                var blastingCharges = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_blasting_charges");
                if (blastingCharges != null) items.Add(blastingCharges);

                //Add Tier1 spanners
                var spanner1h = MBObjectManager.Instance.GetObject<ItemObject>("tor_dwarf_1h_spanner_001");
                if (spanner1h != null) items.Add(spanner1h);
                var spanner2h = MBObjectManager.Instance.GetObject<ItemObject>("tor_dwarf_2h_spanner_001");
                if (spanner2h != null) items.Add(spanner2h);

                //Add Tier1 crossbows and handguns
                var tier1Items = new List<string>
                {
                    "tor_dw_weapon_crossbow_001",
                    "tor_dw_weapon_crossbow_002",
                    "tor_dw_weapon_gun_handgun_001",
                    "tor_dw_weapon_gun_handgun_002",
                    "tor_dw_head_apprentice_002",
                    "tor_dw_head_apprentice_001",
                    "tor_dw_head_journeyman_001",
                    "tor_dw_shoulder_shoulderpads_apprentice_001",
                    "tor_dw_body_armour_apprentice_001",
                    "tor_dw_body_armour_journeyman_001",
                    "tor_dw_body_armour_engineer_001",
                    "tor_dw_arm_gloves_apprentice_001",
                    "tor_dw_arm_gloves_journeyman_001",
                    "tor_dw_arm_gloves_engineer_001",
                    "tor_dw_leg_boots_apprentice_001",
                    "tor_dw_leg_boots_journeyman_001",
                    "tor_dw_leg_boots_engineer_001"
                };
                foreach (var itemId in tier1Items)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    if (item != null) items.Add(item);
                }
            }
            if (Hero.MainHero.HasAttribute("DwarfEngineersII"))
            {
                items.AppendList(MBObjectManager.Instance.GetObjectTypeList<ItemObject>().WhereQ(x =>
                    x.Culture?.StringId == TORConstants.Cultures.DAWI && x.IsTorItem() &&
                    x.IsGunPowderWeapon() && !x.IsFlameThrowerItem()).ToMBList());

                //Add cannons
                var cannon = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_artillery_cannon_001");
                if (cannon != null) items.Add(cannon);

                var drakegunCanister = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_drakegun_canister");
                if (drakegunCanister != null) items.Add(drakegunCanister);

                var drakefirePistol = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_gun_drakefire_pistol");
                if (drakefirePistol != null) items.Add(drakefirePistol);

                //Add hand grenades
                var handGrenades = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_grenade_hand_grenade");
                if (handGrenades != null) items.Add(handGrenades);

                //Add Tier2 engineer hammers
                var engineerHammer1h = MBObjectManager.Instance.GetObject<ItemObject>("dwarf_1h_engineer_hammer_001");
                if (engineerHammer1h != null) items.Add(engineerHammer1h);
                var engineerHammer2h = MBObjectManager.Instance.GetObject<ItemObject>("dwarf_2h_engineer_hammer_001");
                if (engineerHammer2h != null) items.Add(engineerHammer2h);

                //Add Grudge Raker and buckshot
                var grudgeRaker = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_gun_grudge_raker_001");
                if (grudgeRaker != null) items.Add(grudgeRaker);
                var buckshot = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_ammo_buckshot");
                if (buckshot != null) items.Add(buckshot);

                //Add Tier2 crossbows, handguns and armor
                var tier2Items = new List<string>
                {
                    "tor_dw_weapon_crossbow_003",
                    "tor_dw_weapon_crossbow_004",
                    "tor_dw_weapon_gun_handgun_003",
                    "tor_dw_head_engineer_002",
                    "tor_dw_shoulder_shoulderpads_journeyman_001"
                };
                foreach (var itemId in tier2Items)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    if (item != null) items.Add(item);
                }
            }
            if (Hero.MainHero.HasAttribute("DwarfEngineersIII"))
            {
                var drakegun = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_gun_drakegun");
                if (drakegun != null) items.Add(drakegun);

                //Add Dronazgrund
                var dronazgrund = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_gun_dronazgrund");
                if (dronazgrund != null) items.Add(dronazgrund);

                //Add Tier3 handgun
                var handgun004 = MBObjectManager.Instance.GetObject<ItemObject>("tor_dw_weapon_gun_handgun_004");
                if (handgun004 != null) items.Add(handgun004);

                //Add Tier3 trollhammer and armor
                var tier3Items = new List<string>
                {
                    "tor_dw_weapon_gun_trollhammer",
                    "tor_dw_iron_drake_trollhammer_torpedo",
                    "tor_dw_head_engineer_001",
                    "tor_dw_shoulder_shoulderpads_engineer_001"
                };
                foreach (var itemId in tier3Items)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    if (item != null) items.Add(item);
                }
            }


            items.ForEach(x => roster.Add(new ItemRosterElement(x, MBRandom.RandomInt(1, 2))));

            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        bool UpdateEngineerRecruitmentPrices()
        {
            MBTextManager.SetTextVariable("ENGINEER_GOLD_PRICE", ArtilleryCrewGoldCost.ToString());
            MBTextManager.SetTextVariable("ENGINEER_OATHGOLD_PRICE", ArtilleryCrewOathGoldCost.ToString());
            return true;
        }

        bool HasEnoughForEngineerRecruitment()
        {
            return Hero.MainHero.Gold >= ArtilleryCrewGoldCost && Hero.MainHero.GetCustomResourceValue("OathGold") >= ArtilleryCrewOathGoldCost;
        }

        void RecruitEngineerCrew()
        {
            var artilleryCrew = MBObjectManager.Instance.GetObject<CharacterObject>("tor_dw_artillery_crew");
            if (artilleryCrew == null) return;

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, ArtilleryCrewGoldCost);
            Hero.MainHero.AddCustomResource("OathGold", -ArtilleryCrewOathGoldCost);
            MobileParty.MainParty.MemberRoster.AddToCounts(artilleryCrew, 2);
        }

    }




    private void AddGemCutterDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateGemcutters.guild;
        AddDialogStart(campaignGameStarter, guild, IsGemCutter, out var hub, out var reintro);

        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);

        AddOathGoldDialog(campaignGameStarter, guild, reintro);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_hub_spend_troops_p", hub, "tor_dw_guildmaster_gemcutter_spend_troops", TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_hub_spend_troops_p", "Can I provide you with troops to guard mining shafts?"),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_expedition_hub_found_artefacts_p", hub, "tor_dw_guildmaster_gemcutter_found_artefacts", TORTextHelper.GetText("tor_dw_guildmaster_expedition_hub_found_artefacts_p", "You find something of interest for me?"),
            () => ActiveExpeditions(), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_expedition_hub_launch_expedition_p", hub, "tor_dw_guildmaster_gemcutter_launch_expedition", TORTextHelper.GetText("tor_dw_guildmaster_expedition_hub_launch_expedition_p", "Can I launch an expedition?"),
            () => AbleToLaunchExpeditions(), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_hub_quit_p", "That will be all, Master Boki."),
            null, null, 200);

        // found expedition artefacts info
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_found_artefacts", "tor_dw_guildmaster_gemcutter_found_artefacts", reintro, TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_found_artefacts", "Some of the explorer teams return with treasuers or artifacts. Naturally, a portion would be shared with you."),
            HasUnresolvedExpeditions, AddExpeditionsReward, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_found_artefacts_no_result", "tor_dw_guildmaster_gemcutter_found_artefacts", reintro, TORTextHelper.GetText("tor_dw_guildmaster_expedition_found_artefacts_no_expeditions", "The expeditions have not returned yet."),
            null, null, 200);

        //start expedition

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_expedition_launch_expedition_decline", "tor_dw_guildmaster_gemcutter_launch_expedition", reintro, TORTextHelper.GetText("tor_dw_guildmaster_expedition_launch_expedition_decline", "We are spread too thin; all we can do is wait for our explorers to return."),
            () => !CanLaunchExpedition(), null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_expedition_launch_expedition", "tor_dw_guildmaster_gemcutter_launch_expedition", "tor_dw_guildmaster_gemcutter_launch_expedition_p", TORTextHelper.GetText("tor_dw_guildmaster_expedition_launch_expedition", "Aye, with sufficient funding we can send teams to reclaim treasures from the deepest tunnels of the Underway."),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_launch_expedition_accept_p", "tor_dw_guildmaster_gemcutter_launch_expedition_p", "tor_dw_guildmaster_gemcutter_launch_expedition_end", TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_launch_expedition_accept_p", "Let me fund an expeditions."),
            null, () => LaunchExpedition(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_launch_expedition_decline_p", "tor_dw_guildmaster_gemcutter_launch_expedition_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_launch_expedition_decline_p", "Forgive me, I changed my mind."),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_launch_expedition_end", "tor_dw_guildmaster_gemcutter_launch_expedition_end", reintro, TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_launch_expedition_end", "Very well, our finest explorers and miners will depart after gathering supplies."),
            null, null, 200);



        // spend troops

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_spend_troops", "tor_dw_guildmaster_gemcutter_spend_troops", "tor_dw_guildmaster_gemcutter_spend_troops_p", TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_spend_troops", "Aye, the Guild will appreciate any help in this regard."),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_spend_troops_accept_p", "tor_dw_guildmaster_gemcutter_spend_troops_p", "tor_dw_guildmaster_gemcutter_spend_troops_end", TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_spend_troops_accept_p", "Let me station these men."),
            null, () => ProvideTroops(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_gemcutter_spend_troops_decline_p", "tor_dw_guildmaster_gemcutter_spend_troops_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_spend_troops_decline_p", "I changed my mind, forgive me."),
            null, null, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_gemcutter_spend_troops_end", "tor_dw_guildmaster_gemcutter_spend_troops_end", reintro, TORTextHelper.GetText("tor_dw_guildmaster_gemcutter_spend_troops_end", "This is greatly appreciated, Dawongr."),
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
                gainedOathGold += (10 + element.Character.Tier) * element.Number;
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

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_spend_food_p", hub, "tor_dw_guildmaster_brewer_hub_spend_food", TORTextHelper.GetText("tor_dw_guildmaster_brewer_hub_spend_food_p", "Can I deliver you some wheat to help brew ale?"),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_recruit_rangers_p", hub, "tor_dw_guildmaster_brewer_recruit_rangers", TORTextHelper.GetText("tor_dw_brewer_recruit_rangers_text", "I need some rangers for scouting."),
            () => Hero.MainHero.HasAttribute("DwarfBrewersI"), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_guildmaster_brewer_hub_quit_p", "That will be all, Master Brewer."),
            null, null, 200);


        // spend food
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_brewer_hub_spend_food", "tor_dw_guildmaster_brewer_hub_spend_food", "tor_dw_guildmaster_brewer_hub_spend_food_p", TORTextHelper.GetText("tor_dw_guildmaster_brewer_hub_spend_food", "It is how we brew our finest ale. An' we can always use more Gertun."),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_spend_food_accept_p", "tor_dw_guildmaster_brewer_hub_spend_food_p", "tor_dw_guildmaster_brewer_hub_spend_food_end", TORTextHelper.GetText("tor_dw_guildmaster_brewer_hub_spend_food_accept_p", "Have these sacks of grain then."),
            null, () => SpendFood(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_hub_spend_food_decline_p", "tor_dw_guildmaster_brewer_hub_spend_food_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_brewer_hub_spend_food_decline_p", "Actually I changed my mind."),
            null, null, 200);

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_brewer_hub_spend_food_end", "tor_dw_guildmaster_brewer_hub_spend_food_end", reintro, TORTextHelper.GetText("tor_dw_guildmaster_brewer_hub_spend_food_end", "Thank ye for yer contribution."),
            null, null, 200);

        //recruit rangers
        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_brewer_recruit_rangers", "tor_dw_guildmaster_brewer_recruit_rangers", "tor_dw_guildmaster_brewer_recruit_rangers_options",
            TORTextHelper.GetText("tor_dw_brewer_recruit_rangers_offer_text", "Aye, we have some skilled rangers eager to serve. It'll cost ye {RANGER_GOLD_PRICE}{GOLD_ICON} and {RANGER_OATHGOLD_PRICE} Oathgold."),
            UpdateRangerRecruitmentPrices, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_recruit_rangers_accept_p", "tor_dw_guildmaster_brewer_recruit_rangers_options", reintro,
            TORTextHelper.GetText("tor_dw_brewer_recruit_rangers_accept_text", "They'll serve the holds well."),
            HasEnoughForRangerRecruitment, RecruitRangerCrew, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_recruit_rangers_decline_funds_p", "tor_dw_guildmaster_brewer_recruit_rangers_options", reintro,
            TORTextHelper.GetText("tor_dw_brewer_recruit_rangers_no_funds_text", "I don't have enough coin or Oathgold right now."),
            () => !HasEnoughForRangerRecruitment(), null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_brewer_recruit_rangers_decline_p", "tor_dw_guildmaster_brewer_recruit_rangers_options", reintro,
            TORTextHelper.GetText("tor_dw_brewer_recruit_rangers_decline_text", "Maybe another time."),
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

                if (item == DefaultItems.Grain)
                {
                    AddWheatDonationOption(selectable, 30, element.Amount, item);
                    AddWheatDonationOption(selectable, 50, element.Amount, item);
                    AddWheatDonationOption(selectable, 100, element.Amount, item);
                }
            }
            var title = TORTextHelper.GetText("tor_dw_brewers_deliverWheat_prompt_title", "Deliver Grain");
            var description = TORTextHelper.GetText("tor_dw_brewers_deliverWheat_prompt_description", "Select how much grain you wish to deliver to the guild.");

            var inquirydata = new MultiSelectionInquiryData(title, description, selectable, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                AddOathGoldForGrain, null);

            void AddOathGoldForGrain(List<InquiryElement> inquiryElements)
            {
                var amout = int.Parse(inquiryElements.FirstOrDefault().Identifier as string);
                var grain = DefaultItems.Grain;


                Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(grain, -amout);


                Hero.MainHero.AddCultureSpecificCustomResource(amout / WheatToOathGoldGain);
            }

            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        bool UpdateRangerRecruitmentPrices()
        {
            MBTextManager.SetTextVariable("RANGER_GOLD_PRICE", RangerGoldCost.ToString());
            MBTextManager.SetTextVariable("RANGER_OATHGOLD_PRICE", RangerOathGoldCost.ToString());
            return true;
        }

        bool HasEnoughForRangerRecruitment()
        {
            return Hero.MainHero.Gold >= RangerGoldCost && Hero.MainHero.GetCustomResourceValue("OathGold") >= RangerOathGoldCost;
        }

        void RecruitRangerCrew()
        {
            var basicRanger = MBObjectManager.Instance.GetObject<CharacterObject>("tor_dw_ranger");
            var veteranRanger = MBObjectManager.Instance.GetObject<CharacterObject>("tor_dw_ol_deadeye");
            var bugmanRanger = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ror_bugman_ranger");
            if (basicRanger == null) return;

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, RangerGoldCost);
            Hero.MainHero.AddCustomResource("OathGold", -RangerOathGoldCost);

            // Recruit 4 rangers
            for (int i = 0; i < 4; i++)
            {
                // Level II and III have 30% chance for veteran rangers and 15% for bugman rangers
                if (Hero.MainHero.HasAttribute("DwarfBrewersII") || Hero.MainHero.HasAttribute("DwarfBrewersIII"))
                {
                    var roll = MBRandom.RandomFloat;
                    var ranger = basicRanger;

                    if (bugmanRanger != null && roll < 0.15f)
                    {
                        ranger = bugmanRanger;
                    }
                    else if (veteranRanger != null && roll < 0.45f)
                    {
                        ranger = veteranRanger;
                    }

                    MobileParty.MainParty.MemberRoster.AddToCounts(ranger, 1);
                }
                else
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(basicRanger, 1);
                }
            }
        }

    }

    private void AddWarriorDialogue(CampaignGameStarter campaignGameStarter)
    {
        var guild = _templateWarrior.guild;
        AddDialogStart(campaignGameStarter, guild, IsWarrior, out string hub, out var reintro);

        //oath gold
        AddUnlockInfoDialogues(campaignGameStarter, guild, hub, reintro);
        AddOathGoldDialog(campaignGameStarter, guild, reintro);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_hub_spend_troops_p", hub, "tor_dw_guildmaster_warrior_spend_troops", TORTextHelper.GetText("tor_dw_guildmaster_warrior_hub_spend_troops_p", "Can I leave some Dawi to help defend our Holds?"),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_hub_influence_for_oath_p", hub, "tor_dw_guildmaster_warrior_influence_for_oath",
            TORTextHelper.GetText("tor_dw_guildmaster_warrior_influence_for_oath_p", "Can you help improve my standing within the Karaz Ankor?"), () => Hero.MainHero.Clan.Kingdom!=null && !Hero.MainHero.Clan.IsUnderMercenaryService, null, 200);


        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_hub_quit_p", hub, "close_window", TORTextHelper.GetText("tor_dw_quit_text", "That will be all."),
            null, null, 200);


        // spend troops

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_warrior_spend_troops", "tor_dw_guildmaster_warrior_spend_troops", "tor_dw_guildmaster_warrior_spend_troops_p", TORTextHelper.GetText("tor_dw_guildmaster_warrior_spend_troops", "Honoured warriors of sure grip and stern eye are always welcome."),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_spend_troops_accept_p", "tor_dw_guildmaster_warrior_spend_troops_p", "tor_dw_guildmaster_warrior_spend_troops_end", TORTextHelper.GetText("tor_dw_guildmaster_warrior_spend_troops_accept_p", "In that case I will leave these Dawi with you."),
            HasTransferableTroops, () => ProvideTroops(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_spend_troops_decline_p", "tor_dw_guildmaster_warrior_spend_troops_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_warrior_spend_troops_decline_p", "Forgive me, I changed my mind."),
            null, null, 200);


        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_warrior_spend_troops_end", "tor_dw_guildmaster_warrior_spend_troops_end", reintro, TORTextHelper.GetText("tor_dw_guildmaster_warrior_spend_troops_end", "We appreciate any warrior you can spare."),
            null, null, 200);


        // influence

        campaignGameStarter.AddDialogLine("tor_dw_guildmaster_warrior_influence_for_oath", "tor_dw_guildmaster_warrior_influence_for_oath", "tor_dw_guildmaster_warrior_influence_for_oath_p", TORTextHelper.GetText("tor_dw_guildmaster_warrior_influence_for_oath", "With a donation of Oathgold our warriors will spread the Influence of your Clan across the Karaz Ankor."),
            null, null, 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_influence_for_oath_accept_p", "tor_dw_guildmaster_warrior_influence_for_oath_p", "tor_dw_guildmaster_warrior_influence_for_oath", TORTextHelper.GetText("tor_dw_guildmaster_warrior_influence_for_oath_accept_p", "We have a deal, that is a worthy trade."),
            CanTransferOathGoldForInfluence, () => BuyInfluenceForOathGold(), 200);

        campaignGameStarter.AddPlayerLine("tor_dw_guildmaster_warrior_influence_for_oath_decline_p", "tor_dw_guildmaster_warrior_influence_for_oath_p", reintro, TORTextHelper.GetText("tor_dw_guildmaster_warrior_influence_for_oath_decline_p", "Actually I changed my mind."),
            null, null, 200);

        bool CanTransferOathGoldForInfluence()
        {
            return Hero.MainHero.GetCultureSpecificCustomResourceValue() >= InfluenceOathGoldCost;
        }
        void BuyInfluenceForOathGold()
        {
            Hero.MainHero.AddInfluenceWithKingdom(InfluenceGainAmount);
            Hero.MainHero.AddCultureSpecificCustomResource(-InfluenceOathGoldCost);
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
                gainedOathGold += element.Character.Tier * element.Number * WarriorOathGoldPerTier;
            }

            Hero.MainHero.AddCultureSpecificCustomResource(gainedOathGold);
        }

        bool HasTransferableTroops()
        {
            var roster = Hero.MainHero.PartyBelongedTo.MemberRoster;

            if (roster == null) return false;
            return roster.GetTroopRoster().Any(elem => elem.Character.Culture.StringId == TORConstants.Cultures.DAWI && elem.Character.Tier >= MinimumWarriorTier);
        }

        bool IsTransferableVeteranUnit(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftownerparty)
        {
            if (type != PartyScreenLogic.TroopType.Member) return false;
            if (character.IsHero) return false;
            if (Hero.MainHero.Culture.CaravanGuard == character) return false;

            return character.Culture.StringId == TORConstants.Cultures.DAWI && character.Tier >= MinimumWarriorTier;
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