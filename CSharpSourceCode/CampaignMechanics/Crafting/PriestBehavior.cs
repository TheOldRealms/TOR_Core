using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;

public class PriestBehavior : CampaignBehaviorBase
{
    private bool _learnedBlessings;
    /// <remarks>
    /// Using the Town component rather than the Settlement one so that we can search Town.AllTowns rather than Settlement.All as the latter contains every settlement on the map.
    /// </remarks>
    private Dictionary<string, (string town, string cultId, string enchantmentSuffix)> _priestTemplateSpawns = new()
    {
        { "tor_priest_trainer_empire_ulric_0", ("town_comp_ML1", "cult_of_ulric", "emp_blessing_ulric") },
        { "tor_priest_trainer_empire_sigmar_0", ("town_comp_RL1", "cult_of_sigmar","emp_blessing_sigmar") },
        { "tor_priest_trainer_empire_shallya_0", ("town_comp_RL1", "cult_of_shallya", "emp_blessing_shallya") },
        { "tor_priest_trainer_empire_shallya_1", ("town_comp_CO1", "cult_of_shallya", "emp_blessing_shallya") },
    };

    private Dictionary<string, List<string>> _settlementToPriestMap = new();


    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
        CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
    }

    private void OnBeforeMissionStart()
    {
        SpawnPriestIfNeeded();
    }

    private void OnGameMenuOpened(MenuCallbackArgs menuCallbackArgs)
    {
        SpawnPriestIfNeeded();
    }

    private void OnNewGameCreated(CampaignGameStarter campaignStarter)
    {
        foreach (var town in Town.AllTowns)
            foreach (var pair in _priestTemplateSpawns)
                if (pair.Value.Item1 == town.StringId)
                    CreatePriests(town.Settlement, pair.Key, pair.Value.Item2);
    }

    private void CreatePriests(Settlement settlement, string templateId, string religionId)
    {
        CharacterObject template = null;


        template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);

        if (template == null) return;

        var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
        hero.SupporterOf = settlement.OwnerClan;
        hero.SetName(template.Name, template.Name);


        hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == religionId), 100);
        HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);

        if (ExtendedInfoManager.Instance != null) hero.AddAttribute("PriestTrainer"); //if present in the extended xml, the hero creation will copy the attributes attached to the template character onto the hero

        if (_settlementToPriestMap.TryGetValue(settlement.StringId, out var values))
        {
            var list = values.AddItem(hero.StringId).ToList();

            _settlementToPriestMap.AddOrReplace(settlement.StringId, list);
        }
        else
        {
            _settlementToPriestMap.Add(settlement.StringId, new List<string> { hero.StringId });
        }

        var t = hero.HasAttribute("PriestTrainer");
    }

    private void SpawnPriestIfNeeded()
    {
        if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && !IsPriestInTown(Settlement.CurrentSettlement))
            SpawnPriest(Settlement.CurrentSettlement, true);
    }

    private void SpawnPriest(Settlement settlement, bool forceSpawn)
    {
        var trainer = GetPriestForTown(settlement);
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

    private bool IsPriestInTown(Settlement settlement)
    {
        var location = settlement.LocationComplex.GetLocationWithId("lordshall");
        var trainer = GetPriestForTown(settlement);
        if (trainer != null) return location.GetLocationCharacter(trainer) != null;

        return false;
    }


    private Hero GetPriestForTown(Settlement settlement)
    {
        Hero hero = null;
        if (!_settlementToPriestMap.TryGetValue(settlement.StringId, out var ids)) return null;

        foreach (var id in ids)
        {
            hero = Hero.FindFirst(x => x.StringId == id);
            if (hero != null)
                break;
        }

        return hero;
    }

    private void OnSessionLaunched(CampaignGameStarter campaignStarter)
    {
        AddDialogs(campaignStarter);
    }

    private void AddDialogs(CampaignGameStarter campaignStarter)
    {
        var cultsWithEnchanterPriest = new string[] { "cult_of_sigmar", "cult_of_ulric", "cult_of_shallya" };
        for (var index = 0; index < cultsWithEnchanterPriest.Length; index++)
        {
            var i = index;
            var cult = cultsWithEnchanterPriest[index];
            var template = _priestTemplateSpawns.FirstOrDefault(x => x.Value.cultId == cult).Value;


            campaignStarter.AddDialogLine("priest_start_accept" + cult, "start", "priest_hub_intro" + cult,
                TORTextHelper.GetText("tor_priest_start_accept", cult, "Welcome, faithful one. How may I serve you today?", skipValidation: true), () => PriestCondition(cult) && PlayerMeetsRequirements(cult), null, 200);

            campaignStarter.AddDialogLine("priest_start_decline" + cult, "start", "close_window" + cult,
                TORTextHelper.GetText("tor_priest_start_decline", cult, "I am sorry, but I can only help those who follow our faith.", skipValidation: true),
                () => PriestCondition(cult) && !PlayerMeetsRequirements(cult) && !IsEnemyOfCult(cult), null, 200);

            campaignStarter.AddDialogLine("priest_start_decline_hard" + cult, "start", "close_window",
                TORTextHelper.GetText("tor_priest_start_decline_hard", cult, "You dare approach me, servant of darkness? Leave this holy place at once!", skipValidation: true),
                () => PriestCondition(cult) && !PlayerMeetsRequirements(cult) && IsEnemyOfCult(cult), null, 200);


            bool PriestCondition(string cult)
            {
                var partner = CharacterObject.OneToOneConversationCharacter;
                if (Settlement.CurrentSettlement == null)
                    return false;

                if (!_settlementToPriestMap.TryGetValue(Settlement.CurrentSettlement.StringId, out var priests)) return false;

                if (!priests.Contains(partner.StringId)) return false;

                if (!partner.HeroObject.HasAttribute("PriestTrainer")) return false;

                if (partner.HeroObject.GetDominantReligion().StringId == cult) return true;


                return false;
            }

            bool PlayerMeetsRequirements(string cult)
            {
                if (!Hero.MainHero.HasAnyReligion()) return false;

                if (Hero.MainHero.IsPriest() && Hero.MainHero.GetDominantReligion().Affinity == ReligionAffinity.Order) return true;

                if (Hero.MainHero.GetDominantReligion().StringId == cult) return true;

                var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                foreach (var hero in heroes)
                    if (hero.IsPriest())
                        if (hero.GetDominantReligion().StringId == cult)
                            return true;

                return false;
            }

            bool IsEnemyOfCult(string cult)
            {
                var religion = ReligionObject.All.FirstOrDefault(x => x.StringId == cult);

                var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                foreach (var hero in heroes)
                {
                    if (!hero.HasAnyReligion()) continue;
                    if (hero.GetDominantReligion().HostileReligions.Contains(religion)) return true;
                }

                return false;
            }

            campaignStarter.AddDialogLine("priest_hub_intro" + cult, "priest_hub_intro" + cult, "priest_hub" + cult,
                TORTextHelper.GetText("tor_priest_hub_intro", cult, "The blessings of the gods be upon you. What brings you here?", skipValidation: true), null, null, 200);

            campaignStarter.AddDialogLine("priest_hub_reintro" + cult, "priest_hub_reintro" + cult, "priest_hub" + cult,
                TORTextHelper.GetText("tor_priest_hub_reintro", cult, "Is there anything else I can help you with?", skipValidation: true), null, null, 200);


            campaignStarter.AddPlayerLine("priest_hub_blessingshop_p" + cult, "priest_hub" + cult, "priest_blessingshop_1" + cult,
                TORTextHelper.GetText("tor_priest_hub_blessingshop_p", cult, "I seek to learn about holy blessings.", skipValidation: true), null, null, 200);

            campaignStarter.AddPlayerLine("priest_hub_party_blessing_p" + cult, "priest_hub" + cult, "priest_blessing_party_1" + cult,
                TORTextHelper.GetText("tor_priest_hub_party_blessing_p", cult, "Would you bless my party?", skipValidation: true), null, null, 200);

            campaignStarter.AddPlayerLine("priest_hub_quit_p" + cult, "priest_hub" + cult, "close_window",
                TORTextHelper.GetText("tor_priest_hub_quit_p", cult, "Farewell, priest.", skipValidation: true), null, null, 200);


            //blessing shop

            campaignStarter.AddDialogLine("priest_blessingshop_first" + cult, "priest_blessingshop_1" + cult, "priest_hub_reintro" + cult,
                TORTextHelper.GetText("tor_priest_blessingshop_1", cult, "Here, take this tome. It contains the knowledge you seek about our sacred blessings.", skipValidation: true), () => !LearnedBlessings(), teachAboutBlessings, 200);

            campaignStarter.AddDialogLine("priest_blessingshop_1" + cult, "priest_blessingshop_1" + cult, "priest_hub_reintro" + cult,
                TORTextHelper.GetText("tor_priest_blessingshop_1", cult, "Here, take this tome. It contains the knowledge you seek about our sacred blessings.", skipValidation: true), LearnedBlessings, () => OpenBlessingRecipesShop(template.enchantmentSuffix), 200);

            //receive party blessing

            campaignStarter.AddDialogLine("priest_blessing_party_1" + cult, "priest_blessing_party_1" + cult, "priest_hub_reintro" + cult,
                TORTextHelper.GetText("tor_priest_blessing_party_1", cult, "May the divine light guide your steps. Your party is now blessed.", skipValidation: true), () => !Hero.MainHero.PartyBelongedTo.HasAnyActiveBlessing(),
                () => BlessParty(cult), 200);


            campaignStarter.AddDialogLine("priest_hub_party_blessing_decline" + cult, "priest_blessing_party_1" + cult, "priest_hub_reintro" + cult,
                TORTextHelper.GetText("tor_priest_hub_party_blessing_decline", cult, "I see your party already carries a blessing. There is nothing more I can do.", skipValidation: true), null, null, 200);


            bool LearnedBlessings()
            {
                return _learnedBlessings;
            }

            void OpenBlessingRecipesShop(string prefix)
            {
                var partner = CharacterObject.OneToOneConversationCharacter;

                EnchantmentHelper.OpenEnchantmentRecipeShop([prefix], partner.Culture.StringId, true);
            }

            void BlessParty(string cultId)
            {
                Hero.MainHero.PartyBelongedTo.AddBlessingToParty(cultId);
            }



            void teachAboutBlessings()
            {
                var itemId = "tor_book_learn_blessing";
                var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item, 1);
                _learnedBlessings = true;
            }


            //quit
            campaignStarter.AddDialogLine("priest_hub_quit" + cult, "priest_hub_quit", "close_window",
                TORTextHelper.GetText("tor_priest_hub_quit", cult, "Go in peace. May the gods watch over you.", skipValidation: true), () => PriestCondition(cult) && PlayerMeetsRequirements(cult), null, 200);
        }
    }


    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_settlementToPriestMap", ref _settlementToPriestMap);
        dataStore.SyncData("_learnedEnchantment", ref _learnedBlessings);
    }
}