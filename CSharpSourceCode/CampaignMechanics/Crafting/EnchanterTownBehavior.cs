using Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SpellTrainers;

public class EnchanterTownBehavior : CampaignBehaviorBase
{
    private bool _learnedEnchantment;
    private bool _spokeToEnchanter;

    private Dictionary<string, string> _settlementToEnchanterMap = new();


    private readonly Dictionary<string, (string enchanterTemplate, List<string> enchantmentSuffixes)> _cultureToTemplateMap = new()
    {
        { TORConstants.Cultures.EMPIRE, ("tor_enchanter_empire_0", new List<string>()
        {
            "emp_enchant"
        }) },
        { TORConstants.Cultures.SYLVANIA, ("tor_spelltrainer_vc_0", new List<string>()
        {
            "vc_enchant",
        }) },
        { TORConstants.Cultures.BRETONNIA, ("tor_spelltrainer_bretonnia_0", new List<string>()
        {
            "emp_enchant", "bret_blessing"
        }) },
        { TORConstants.Cultures.MOUSILLON, ("tor_spelltrainer_vc_0",  new List<string>()
        {
            "vc_enchant",
        }) },
        { TORConstants.Cultures.ASRAI, ("tor_spelltrainer_woodelves_0", new List<string>()
        {
            "we_enchant", "asrai_enchant"
        }) },
        { TORConstants.Cultures.EONIR, ("tor_eonir_spellsinger_envoy_0", new List<string>()
        {
            "we_enchant", "eo_enchant"
        }) },
        { TORConstants.Cultures.DAWI, ("tor_dawi_runelord_trainer_0",  new List<string>()
        {
            "dw_rune",
        }) },
        { TORConstants.Cultures.GREENSKIN, ("tor_spelltrainer_greenskins_0", new List<string>()
        {
            "gs_enchant",
        })}
    };

    private List<string> _multiUseCultures = new()
    {
        TORConstants.Cultures.GREENSKIN,
        TORConstants.Cultures.BRETONNIA,
        TORConstants.Cultures.SYLVANIA,
        TORConstants.Cultures.MOUSILLON,
        TORConstants.Cultures.ASRAI,
        TORConstants.Cultures.EONIR,
        TORConstants.Cultures.DAWI
    };

    private List<string> _careerEligableForFreebee = ["GrailDamsel", "ImperialMagister", "Spellsinger", "Greylord", "Necromancer", "Runelord", "OrcShaman"];

    public bool SpokeToEnchanter => _spokeToEnchanter;
    private static bool SharesVcEnchanterBranch(string requestedCulture, string actualCulture)
    {
        if (requestedCulture == actualCulture)
            return true;

        return (requestedCulture == TORConstants.Cultures.SYLVANIA && actualCulture == TORConstants.Cultures.MOUSILLON)
            || (requestedCulture == TORConstants.Cultures.MOUSILLON && actualCulture == TORConstants.Cultures.SYLVANIA);
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
    }

    private void OnSessionLaunched(CampaignGameStarter obj)
    {
        AddDialogs(obj);
    }

    private void OnBeforeMissionStart()
    {
        SpawnEnchanterIfNeeded();
    }

    private void OnGameMenuOpened(MenuCallbackArgs obj)
    {
        SpawnEnchanterIfNeeded();
    }

    private void SpawnEnchanterIfNeeded()
    {
        if (Settlement.CurrentSettlement == null || !Settlement.CurrentSettlement.IsTown)
            return;

        EnsureEnchanterMappedForSettlement(Settlement.CurrentSettlement);

        if (!IsTrainerInCollege(Settlement.CurrentSettlement))
            SpawnEnchanter(Settlement.CurrentSettlement, true);
    }

    private void EnsureEnchanterMappedForSettlement(Settlement settlement)
    {
        if (_settlementToEnchanterMap.ContainsKey(settlement.StringId))
            return;

        // Check if this is an enchanter town (culture has enchanter support)
        if (!_cultureToTemplateMap.TryGetValue(settlement.Culture.StringId, out var templateInfo))
            return;

        if (_multiUseCultures.Contains(settlement.Culture.StringId))
        {
            // Multi-use cultures share an enchanter - find and map them
            var enchanterHero = Hero.FindFirst(x => x.StringId == templateInfo.enchanterTemplate);
            if (enchanterHero != null)
            {
                _settlementToEnchanterMap.Add(settlement.StringId, enchanterHero.StringId);
            }
        }
    }

    private void SpawnEnchanter(Settlement settlement, bool forceSpawn = false)
    {
        var trainer = GetEnchanterForTown(settlement);
        if(settlement.IsTorLithanel())  //needs fixing with more care later. 3 behaviors all at once are here interfering.
            return;
        
        var currentLocation = settlement.LocationComplex.GetLocationWithId("house_1");
        if (trainer != null && currentLocation != null)
        {
            if (forceSpawn)
            {
                var locationCharacter = new LocationCharacter(
                    new AgentData(new SimpleAgentOrigin(trainer.CharacterObject, -1, null, default)).Monster(
                        FaceGen.GetBaseMonsterFromRace(trainer.CharacterObject.Race)),
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true,
                    LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
                currentLocation.AddCharacter(locationCharacter);
            }
            else
            {
                var locChar = settlement.LocationComplex.GetLocationCharacterOfHero(trainer);
                var currentloc = settlement.LocationComplex.GetLocationOfCharacter(trainer);
                if (currentloc != currentLocation) settlement.LocationComplex.ChangeLocation(locChar, currentloc, currentLocation);
            }
        }
    }

    private bool IsTrainerInCollege(Settlement settlement)
    {
        var location = settlement.LocationComplex.GetLocationWithId("house_1");
        var trainer = GetEnchanterForTown(settlement);
        if (trainer != null) return location.GetLocationCharacter(trainer) != null;

        return false;
    }

    private Hero GetEnchanterForTown(Settlement settlement)
    {
        Hero hero = null;
        var heroId = "";
        if (_settlementToEnchanterMap.TryGetValue(settlement.StringId, out heroId)) hero = Hero.FindFirst(x => x.StringId == heroId);
        return hero;
    }

    private void AddDialogs(CampaignGameStarter campaignGameStarter)
    {
        GenericEnchantmentDialogs(campaignGameStarter);
    }


    private void OnNewGameCreated(CampaignGameStarter obj)
    {
        foreach (var settlement in Settlement.All)
            if (settlement.IsTown)
                CreateEnchanter(settlement);
    }

    private void CreateEnchanter(Settlement settlement)
    {
        CharacterObject template = null;

        var cultureID = settlement.Culture.StringId;

        _cultureToTemplateMap.TryGetValue(cultureID, out var enchanterTemplateId);

        if (enchanterTemplateId.enchanterTemplate == null)
            return;


        if (_multiUseCultures.Contains(cultureID))
        {
            var hero = Hero.FindFirst(x => x.StringId == enchanterTemplateId.enchanterTemplate);
            if (hero != null) _settlementToEnchanterMap.Add(settlement.StringId, hero.StringId);
            return; //we know it gets instantiated somewhere else, we are not creating another instance
        }

        template = MBObjectManager.Instance.GetObject<CharacterObject>(enchanterTemplateId.enchanterTemplate);


        if (template != null)
        {
            var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
            hero.SupporterOf = settlement.OwnerClan;
            hero.Culture = settlement.Culture;//for templates shared between cultures
            var nameObject = template.GetName();
            nameObject.SetTextVariable("FIRSTNAME", hero.FirstName);
            hero.SetName(nameObject, hero.FirstName);
            hero.CharacterObject.HiddenInEncyclopedia = true;
            HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
            _settlementToEnchanterMap.Add(settlement.StringId, hero.StringId);
        }
    }

    public bool IsEnchanter(Hero hero)
    {
        if (hero.Template != null)
        {
            _cultureToTemplateMap.TryGetValue(hero.Culture.StringId, out var templateId);
            if (hero.Template.StringId == templateId.enchanterTemplate)
            {
                return true;
            }
        }
        return _settlementToEnchanterMap.ContainsValue(hero.StringId);
        
    }

    private bool SettlementMatchesEnchanterCulture(string culture)
    {
        var settlementCulture = Settlement.CurrentSettlement?.Culture?.StringId;
        if (settlementCulture == null)
        {
            return false;
        }

        return SharesVcEnchanterBranch(culture, settlementCulture);
    }

    private bool HasMatchingEnchanterCompanion(Func<Hero, bool> predicate)
    {
        if (Hero.MainHero.PartyBelongedTo == null)
        {
            return false;
        }

        return Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x != Hero.MainHero && predicate(x));
    }

    private bool HasEnchanterAccess(string culture)
    {
        if (!SettlementMatchesEnchanterCulture(culture))
        {
            return false;
        }

        switch (culture)
        {
            case TORConstants.Cultures.EMPIRE:
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => x.Culture.StringId == TORConstants.Cultures.EMPIRE && x.IsSpellCaster());

            case TORConstants.Cultures.SYLVANIA:
            case TORConstants.Cultures.MOUSILLON:
                if (SharesVcEnchanterBranch(culture, Hero.MainHero.Culture.StringId))
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => SharesVcEnchanterBranch(culture, x.Culture.StringId) && x.IsSpellCaster());

            case TORConstants.Cultures.BRETONNIA:
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster());

            case TORConstants.Cultures.ASRAI:
                if (Hero.MainHero.HasCareer(TORCareers.Spellsinger))
                {
                    return true;
                }

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI &&
                    Hero.MainHero.IsSpellCaster() &&
                    Hero.MainHero.GetKnownLoreCount() > 0)
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster());

            case TORConstants.Cultures.EONIR:
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR)
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => x.Culture.StringId == TORConstants.Cultures.EONIR && x.IsSpellCaster());

            case TORConstants.Cultures.DAWI:
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI)
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => x.HasAttribute("Runesmith"));

            case TORConstants.Cultures.GREENSKIN:
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
                {
                    return true;
                }

                return HasMatchingEnchanterCompanion(x => x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsSpellCaster());

            default:
                return false;
        }
    }

    private bool IsDirectEnchanterBlocked(string culture)
    {
        var partner = CharacterObject.OneToOneConversationCharacter;
        if (partner?.HeroObject == null || partner.HeroObject.IsSpellTrainer())
        {
            return false;
        }

        return partner.HeroObject.IsEnchanter() && partner.Culture.StringId == culture && !HasEnchanterAccess(culture);
    }

    private void GenericEnchantmentDialogs(CampaignGameStarter campaignGameStarter)
    {
        var cultures = _cultureToTemplateMap.Keys.ToList();

        for (var index = 0; index < cultures.Count; index++)
        {
            var i = index; // i still dont get it, but it works...

            var template = _cultureToTemplateMap[cultures[i]];
            var quittoken = "close_window";
            if (_multiUseCultures.Contains(cultures[i]))
            {
                var hub = "choices";
                quittoken = "start";
                if (cultures[i] == TORConstants.Cultures.BRETONNIA)
                {
                    hub = "choices_prophetesse";
                    quittoken = "hub_prophetesse";
                }

                if (cultures[i] == TORConstants.Cultures.ASRAI)
                {
                    hub = "choices_spellsinger";
                    quittoken = "hub_spellsinger";
                }

                if (cultures[i] == TORConstants.Cultures.EONIR) hub = "spellsinger_envoy_main_hub";

                if (cultures[i] == TORConstants.Cultures.DAWI) hub = "tor_dw_guildmaster_runesmith_hub";

                if (cultures[i] == TORConstants.Cultures.GREENSKIN)
                {
                    hub = "choices_greenskin";
                    quittoken = "hub_greenskin";
                }

                if (cultures[i] == TORConstants.Cultures.SYLVANIA || cultures[i] == TORConstants.Cultures.MOUSILLON)
                {
                    hub = "vampire_choices";
                    quittoken = "hub_vampire";
                }

                campaignGameStarter.AddPlayerLine("enchanter_start_p" + cultures[i], hub, "enchanter_start" + cultures[i],
                    TORTextHelper.GetText("tor_enchanter_start_p", cultures[i], "I wish to learn about enchantment.", true),
                    () => EnchanterCondition(cultures[i]) && cultureCheck(cultures[i]), null, 210);


                campaignGameStarter.AddDialogLine("enchanter_start_enchanter" + cultures[i], "enchanter_start" + cultures[i],
                    "enchanter_hub_intro" + cultures[i], TORTextHelper.GetText("tor_enchanter_start_enchanter", cultures[i], "Welcome, fellow practitioner of the arts.", true),
                    () => enchanterCareer() && cultureCheck(cultures[i]), null, 200);

                campaignGameStarter.AddDialogLine("enchanter_start" + cultures[i], "enchanter_start" + cultures[i],
                    "enchanter_hub_intro" + cultures[i], TORTextHelper.GetText("tor_enchanter_dialog_start", cultures[i], "Greetings, visitor. How may I assist you?", true),
                    () => EnchanterCondition(cultures[i]) && cultureCheck(cultures[i]), null, 200);
            }
            else
            {
                campaignGameStarter.AddDialogLine("enchanter_start_quit" + cultures[i], "start", "close_window",
                    TORTextHelper.GetText("tor_enchanter_dialog_start_quit", cultures[i], "You are not of my culture. Begone.", true),
                    () => IsDirectEnchanterBlocked(cultures[i]), null, 200);

                campaignGameStarter.AddDialogLine("enchanter_start_2" + cultures[i], "start", "enchanter_hub_intro" + cultures[i],
                    TORTextHelper.GetText("tor_enchanter_dialog_start_2", cultures[i], "Ah, a fellow practitioner! You are most welcome here.", true),
                    () => EnchanterCondition(cultures[i])  && cultureCheck(cultures[i]) && enchanterCareer(), null, 200);

                campaignGameStarter.AddDialogLine("enchanter_start_1" + cultures[i], "start", "enchanter_hub_intro" + cultures[i],
                    TORTextHelper.GetText("tor_enchanter_dialog_start_1", cultures[i], "Welcome. You have proven yourself worthy of my attention.", true),
                    () => EnchanterCondition(cultures[i]) && cultureCheck(cultures[i]), null, 200);
            }

            bool cultureCheck(string culture)
            {
                return HasEnchanterAccess(culture);
            }

            bool enchanterCareer()
            {
                return _careerEligableForFreebee.Contains(Hero.MainHero.GetCareer().StringId);
            }

            bool fullfillsFreeBeeCondition()
            {
                return !_learnedEnchantment  && _careerEligableForFreebee.Contains(Hero.MainHero.GetCareer().StringId);
            }

            //enchantment hub

            campaignGameStarter.AddDialogLine("enchanter_hub_intro" + cultures[i], "enchanter_hub_intro" + cultures[i],
                "enchantment_hub" + cultures[i], TORTextHelper.GetText("tor_enchanter_hub_intro", cultures[i], "So, what brings you here?", true), null, null, 200);

            campaignGameStarter.AddDialogLine("enchanter_hub_reintro" + cultures[i], "enchanter_hub_reintro" + cultures[i],
                "enchantment_hub" + cultures[i], TORTextHelper.GetText("tor_enchanter_hub_reintro", cultures[i], "Is there anything else?", true), null, null, 200);


            campaignGameStarter.AddPlayerLine("enchanter_hub_info_p" + cultures[i], "enchantment_hub" + cultures[i],
                "enchanter_info_hub_intro" + cultures[i], TORTextHelper.GetText("tor_enchanter_hub_info_p", cultures[i], "Can you teach me about enchantment?", true),
                () => !_learnedEnchantment, null, 200);
            campaignGameStarter.AddPlayerLine("enchanter_hub_blueprints_p" + cultures[i], "enchantment_hub" + cultures[i],
                "enchanter_blueprints" + cultures[i], TORTextHelper.GetText("tor_enchanter_hub_blueprints_p", cultures[i], "I would like to learn new enchantments.", true),
                () => _spokeToEnchanter, null, 200);
            campaignGameStarter.AddPlayerLine("enchanter_hub_donate_items_p" + cultures[i], "enchantment_hub" + cultures[i],
                "enchanter_donate_items_1" + cultures[i], TORTextHelper.GetText("tor_enchanter_hub_donate_items_p", cultures[i], "I have magical items I no longer need.", true),
                () => _spokeToEnchanter, null, 200);
            campaignGameStarter.AddPlayerLine("enchanter_hub_open_enchanter_p" + cultures[i], "enchantment_hub" + cultures[i],
                "enchanter_hub_reintro" + cultures[i], TORTextHelper.GetText("tor_enchanter_hub_open_enchanter_p", cultures[i], "May I use your enchanting facilities?", true),
                () => _spokeToEnchanter, () => EnchantingScreen.Open(), 200);

            campaignGameStarter.AddPlayerLine("enchanter_hub_return_p" + cultures[i], "enchantment_hub" + cultures[i], quittoken,
                TORTextHelper.GetText("tor_enchanter_hub_return_p", cultures[i], "I must go. Farewell.", true), null, null, 200);

            //buy Enchanting Blue prints

            campaignGameStarter.AddDialogLine("enchanter_blueprints" + cultures[i], "enchanter_blueprints" + cultures[i],
                "enchanter_hub_reintro" + cultures[i], TORTextHelper.GetText("tor_enchanter_blueprints", cultures[i], "Let me show you what I have available.", true),
                () => HasAnyViableEnchanterCharacter(), () => OpenEnchantmentShop(template.enchantmentSuffixes, cultures[i]), 200);

            campaignGameStarter.AddDialogLine("enchanter_blueprints_decline" + cultures[i], "enchanter_blueprints" + cultures[i],
                "enchanter_hub_reintro" + cultures[i], TORTextHelper.GetText("tor_enchanter_blueprints_decline", cultures[i], "I'm afraid you have no one capable of learning these arts.", true), null, null, 200);


            bool HasAnyViableEnchanterCharacter()
            {
                return EnchantmentHelper.HasAnyLearnableEnchantmentRecipe(template.enchantmentSuffixes);
            }

            //donation
            campaignGameStarter.AddDialogLine("enchanter_donate_items_1" + cultures[i], "enchanter_donate_items_1" + cultures[i],
                "enchanter_gift_items_hub" + cultures[i], TORTextHelper.GetText("tor_enchanter_donate_items_1", cultures[i], "I am interested in magical items from your travels. What do you have?", true),
                ConditionForDonation, null, 200);

            campaignGameStarter.AddDialogLine("enchanter_donate_items_1_negative" + cultures[i], "enchanter_donate_items_1" + cultures[i],
                "enchanter_hub_reintro" + cultures[i], TORTextHelper.GetText("tor_enchanter_donate_items_1_negative", cultures[i], "You have nothing of interest to me at this time.", true), null, null,
                200);

            campaignGameStarter.AddPlayerLine("enchanter_donate_items_hub_p" + cultures[i], "enchanter_gift_items_hub" + cultures[i],
                "enchanter_donate_items_2" + cultures[i], TORTextHelper.GetText("tor_enchanter_donate_items_hub_p", cultures[i], "Let me provide you these items…", true), null,
                () => DonationMode(true), 200);

            campaignGameStarter.AddPlayerLine("enchanter_gift_items_hub_return_p" + cultures[i], "enchanter_gift_items_hub" + cultures[i],
                "enchanter_hub_reintro" + cultures[i], TORTextHelper.GetText("tor_enchanter_gift_items_hub_return_p", cultures[i], "I need to think about this.", true), null, null,
                200);


            campaignGameStarter.AddDialogLine("enchanter_donate_items_2" + cultures[i], "enchanter_donate_items_2" + cultures[i],
                "enchanter_hub_reintro" + cultures[i], TORTextHelper.GetText("tor_enchanter_donate_items_2", cultures[i], "Which items do you wish to donate?", true), null, null, 200);


            // enchantment info hub
            campaignGameStarter.AddDialogLine("enchanter_info_hub_intro_enchanter_first_visit" + cultures[i],
                "enchanter_info_hub_intro" + cultures[i], "enchanter_hub_reintro" + cultures[i],
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_enchanter_first_visit", cultures[i], "Of course! Take this, it will help you get started.", true), fullfillsFreeBeeCondition, () =>
                {
                    _learnedEnchantment = true;
                    spokeFirstTime();
                    AddBeginnerBlueprintsForEnchantment();
                }, 200);


            campaignGameStarter.AddDialogLine("enchanter_info_hub_intro_first_visit" + cultures[i],
                "enchanter_info_hub_intro" + cultures[i], "enchanter_hub_reintro" + cultures[i],
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_first_visit", cultures[i], "Of course! Here, study this manual.", true), () => !_spokeToEnchanter,
                () => { spokeFirstTime(); }, 200);

            campaignGameStarter.AddDialogLine("enchanter_info_hub_intro_visit" + cultures[i],
                "enchanter_info_hub_intro" + cultures[i], "enchanter_hub_reintro" + cultures[i],
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_first_visit", cultures[i], "Of course! Here, study this manual.", true), () => !HasManual(),
                () => { spokeFirstTime(); }, 200);

            campaignGameStarter.AddDialogLine("enchanter_info_hub_intro_read_book" + cultures[i],
                "enchanter_info_hub_intro" + cultures[i], "enchanter_hub_reintro" + cultures[i],
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_read_book", cultures[i], "Just read the manual I gave you.", true), null,
                () => { spokeFirstTime(); }, 200);




            void spokeFirstTime()
            {
                _spokeToEnchanter = true;
                AddManual();
            }


            void AddManual()
            {
                var itemId = "";
                itemId= GetManual();

                if (itemId == "")
                    return;

                var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item, 1);
            }

            bool HasManual()
            {
                var itemId = "";
                itemId = GetManual();

                if (itemId == "")
                {
                    return false;
                }

                return Hero.MainHero.PartyBelongedTo.ItemRoster.Any(x => x.EquipmentElement.Item.StringId == itemId);
            }

            string GetManual()
            {
                switch (Hero.MainHero.Culture.StringId)
                {
                    case TORConstants.Cultures.EMPIRE:
                        return "tor_book_learn_enchanting";
                    case TORConstants.Cultures.DAWI:
                        return "tor_book_learn_runecraft";
                }
                
                TORCommon.Log("ENCHANTMENT: could not find manual item for" + Hero.MainHero.Culture.StringId, LogLevel.Warn);
                return "";
            }



            void AddBeginnerBlueprintsForEnchantment()
            {
                var career = Hero.MainHero.GetCareer();

                if (Hero.MainHero.IsSpellCaster() && career == TORCareers.ImperialMagister)
                {
                    if (Hero.MainHero.HasKnownLore("LoreOfDeath")) 
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_shyish_whisper", true);

                    if (Hero.MainHero.HasKnownLore("LoreOfMetal")) 
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_chamon_whisper", true);

                    if (Hero.MainHero.HasKnownLore("LoreOfLight"))
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_hysh_whisper", true);

                    if (Hero.MainHero.HasKnownLore("LoreOfHeavens")) 
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_azyr_whisper", true);

                    if (Hero.MainHero.HasKnownLore("LoreOfBeasts")) 
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_ghur_whisper", true);

                    if (Hero.MainHero.HasKnownLore("LoreOfLife")) 
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_ghyran_whisper", true);

                    if (Hero.MainHero.HasKnownLore("LoreOfFire")) 
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_aqshy_whisper", true);
                }

                if (Hero.MainHero.IsSpellCaster() && Hero.MainHero.HasCareer(TORCareers.GrailDamsel))
                    if (Hero.MainHero.HasKnownLore("LoreOfLife"))
                        Hero.MainHero.AddEnchantmentBlueprint("emp_enchant_ghyran_whisper", true);

                if (Hero.MainHero.HasCareer(TORCareers.Runelord)) Hero.MainHero.AddEnchantmentBlueprint("dw_rune_stone", true);
            }

            bool ConditionForDonation()
            {
                var roster = Hero.MainHero.PartyBelongedTo.Party.ItemRoster;

                foreach (var elem in roster)
                {
                    var item = elem.EquipmentElement.Item;
                    if (item.IsCraftedByPlayer) continue;
                    if (item.HasAnyTrait())
                        if (item.IsArmor() || item.IsWeapon() || item.IsShield())
                            return true;
                }

                return false;
            }

            void DonationMode(bool customResourceExchange)
            {
                var i = 0;

                var roster = Hero.MainHero.PartyBelongedTo.Party.ItemRoster.ToMBList();

                if (customResourceExchange) roster = roster.FindAll(x => !x.EquipmentElement.Item.IsCraftedByPlayer).ToMBList();

                var selectableItems = new List<InquiryElement>();
                foreach (var elem in roster)
                {
                    var equipmentElement = elem.EquipmentElement;
                    var item = equipmentElement.Item;
                    if (item.HasAnyTrait() && (item.IsWeapon() || item.IsArmor() || item.IsShield()))
                        selectableItems.Add(new InquiryElement(equipmentElement, item.Name.ToString(), new ItemImageIdentifier(item)));
                }

                GameTexts.SetVariable("CUSTOMRESOURCE_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());


                var title = GameTexts.FindText("tor_enchant_prompt_disintegrate", "title");
                var text = GameTexts.FindText("tor_enchant_prompt_disintegrate", "text");
                if (customResourceExchange)
                {
                    title = GameTexts.FindText("tor_enchant_prompt_donate_items", "title");
                    text = GameTexts.FindText("tor_enchant_prompt_donate_items", "text");
                }


                var inquirydata = new MultiSelectionInquiryData(title.ToString(), text.ToString(), selectableItems, true, 1, 15, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    Disenchant, null);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);


                void Disenchant(List<InquiryElement> inquiryElements)
                {
                    var model = Campaign.Current.Models.GetEnchantmentIngredientModel();

                    var resources = new int[Enum.GetValues(typeof(TorTradeGoodType)).Length];
                    var customResources = 0;


                    foreach (var element in inquiryElements)
                    {
                        var customResourceElem = 0;
                        var customResourceFactor = 1 / 3f;
                        var equipmentElement = (EquipmentElement)element.Identifier;
                        var item = equipmentElement.Item;

                        if (!item.IsCraftedByPlayer)
                        {
                            var adjusted = item.Value / 200; // used to be 100. now buying and recycling 1 trait wizard staves is at least somewhere close to buying prestige from altdorf noble, still higher though.
                            customResourceElem += MBMath.ClampIndex(adjusted, 50, 500);
                        }

                        var traits = item.GetTraits();

                        if (!traits.Any()) return;

                        Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(equipmentElement, -1);

                        foreach (var trait in item.GetTraits())
                            foreach (TorTradeGoodType type in Enum.GetValues(typeof(TorTradeGoodType)))
                            {
                                if (trait.IngredientItem != type) continue;

                                if (trait.IngredientAmount > 0)
                                {
                                    var factor = model.GetPercentOfIngredientForRecycleItems(type, item);

                                    resources[(int)type] += (int)Mathf.Max(1, trait.IngredientAmount * factor);

                                    customResourceElem += model.GetCustomResourceValueForIngredient(type) * trait.IngredientAmount;
                                    customResourceFactor += customResourceFactor;
                                }
                            }

                        customResourceElem = (int)(customResourceElem * customResourceFactor);

                        customResources += customResourceElem;
                    }

                    if (customResourceExchange)
                    {
                        var resultValue = (int)customResources;
                        var gainedText = TORTextHelper.GetTextObject("tor_gained_resource_notification_text", "Gained {AMOUNT}{RESOURCE_ICON}");
                        gainedText.SetTextVariable("AMOUNT", resultValue);
                        gainedText.SetTextVariable("RESOURCE_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
                        MBInformationManager.AddQuickInformation(gainedText, 2000, Hero.MainHero.CharacterObject);
                        Hero.MainHero.AddCultureSpecificCustomResource(resultValue);

                        return;
                    }

                    var text = "";

                    foreach (TorTradeGoodType type in Enum.GetValues(typeof(TorTradeGoodType)))
                    {
                        text.Add(" ");
                        var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(type);
                        if (ingredient == null)
                            continue;

                        Hero.MainHero.PartyBelongedTo.ItemRoster.Add(new ItemRosterElement(ingredient, resources[(int)type]));
                        if (resources[(int)type] > 0) text += resources[(int)type] + ", " + ingredient.Name;
                    }


                    var gainedItemsText = TORTextHelper.GetTextObject("tor_gained_items_notification_text", "Gained {ITEMS}");
                    gainedItemsText.SetTextVariable("ITEMS", text);
                    MBInformationManager.AddQuickInformation(gainedItemsText, 2000, Hero.MainHero.CharacterObject);
                }
            }

            bool EnchanterCondition(string culture)
            {
                var partner = CharacterObject.OneToOneConversationCharacter;
                if (partner?.HeroObject == null)
                {
                    return false;
                }

                if (partner.Culture.StringId != culture)
                {
                    return false;
                }

                if (!SettlementMatchesEnchanterCulture(culture))
                {
                    return false;
                }

                return partner.HeroObject.IsEnchanter();
            }


            void OpenEnchantmentShop(List<string> prefixList, string culture)
            {
                EnchantmentHelper.OpenEnchantmentRecipeShop(prefixList, culture, false);
            }
        }
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_spokeToEnchanter", ref _spokeToEnchanter);
        dataStore.SyncData("_learnedEnchantment", ref _learnedEnchantment);
        dataStore.SyncData("_settlementToEnchanterMap", ref _settlementToEnchanterMap);
    }
}