using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Menagery;

public class EonirFavorEnvoyTownBehavior : CampaignBehaviorBase
{
    private const string _druchiiEnvoyId = "tor_eonir_druchii_envoy_0";
    private const string _asurEnvoyId = "tor_eonir_asur_envoy_0";
    private const string _empireEnvoyId = "tor_eonir_empire_envoy_0";
    private const string _spellsingerEnvoyId = "tor_eonir_spellsinger_envoy_0";

    private Hero _druchiiEnvoy;
    private Hero _asurEnvoy;
    private Hero _empireEnvoy;
    private Hero _spellsingerEnvoy;
    private List<Hero> envoys;
    private Settlement _torLithanel;

    public override void RegisterEvents()
    {
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
    }

    private void OnGameMenuOpened(MenuCallbackArgs obj)
    {
        EnforceEnvoyLocation();
    }

    private void OnSessionLaunched(CampaignGameStarter obj)
    {
        AddDruchiiEnvoyDialogLines(obj);
        AddAsurEnvoyDialogLines(obj);
        AddEmpireEnvoyDialogLines(obj);
        AddSpellsingerEnvoyDialogLines(obj);
    }

    private void AddSpellsingerEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => !EonirEnvoyDialogCondition(), null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_spellsinger", "start", "spellsinger_envoy_main_hub", "How can the Forestborn be of use?",
            () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_prestige_to_favour", "spellsinger_envoy_main_hub", "close_window",
            "I need to travel to roots of the Asrai, would you guide me over it?", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_favour_to_prestige", "spellsinger_envoy_main_hub", "close_window",
            "I would like to make the influence of my count for your people. If your people willing to ", () => IsSpellsingerEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_spellsinger_peace", "spellsinger_envoy_main_hub", "close_window",
            "I want to learn more magic can you help?", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_whyareyouhere", "spellsinger_envoy_main_hub", "spellsinger_envoy_whyareyouhere",
            "Why are you here?", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_close", "spellsinger_envoy_main_hub", "close_window", "That's all thank you.",
            () => IsSpellsingerEnvoy(), null, 200);

        
                //why are you here
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere", "spellsinger_envoy_whyareyouhere", "envoy_spellsinger_wayh_reaction",
            "I am representing a coven of Spellsingers dedicated to the defense of Laurelorn. While the Council is busy with politics, the Faniour, the forest born elves, are endagered by all the threads of the forest.", () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_displeased", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            "Why are you then not defending the forest?", () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_undecided", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            "What matters can the Council solve for you? What can you give me in turn?", () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_agreement", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            "The Faniour aswell as the Touriour are Eonir. Your matters, are also matters of the Touriour.", () => IsSpellsingerEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_3",
            "I am not a man of politics. I am here to stand our case. I know that seemingly the Council, sometimes forgets about us. Thats why I am here, and standing for the forest people. I know that nothing works here, without giving something in return.", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_3", "spellsinger_envoy_whyareyouhere_3", "back_to_main_hub_spellsinger",
            "Help us with your political power, and I will try to make it worth. I can assist you with some magic, like using the world roots of the Asrai, or can make your influence provide you power over the council. Apart, I can see what i can do for teaching you some magic", () => IsSpellsingerEnvoy(), null, 200);

        
        campaignGameStarter.AddDialogLine("back_to_main_hub_spellsinger", "back_to_main_hub_spellsinger", "spellsinger_envoy_main_hub",
            "Is there something else I could do for you?", () => IsSpellsingerEnvoy(), null, 200);
        
        

        bool IsSpellsingerEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null) return partner.HeroObject.HasAttribute("SpellsingerEnvoy");

            return false;
        }
    }


    private void AddEmpireEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => !EonirEnvoyDialogCondition(), null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_empire", "start", "empire_envoy_main_hub", "How can the empire help?",
            () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_prestige_to_favour", "empire_envoy_main_hub", "close_window",
            "I want to use my prestigious goods, to gain more power in the council.", () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_favour_to_prestige", "empire_envoy_main_hub", "close_window",
            "I need a few prestigious goods of the empire, what does it take me to get them from you?", () => IsEmpireEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_empire_peace", "empire_envoy_main_hub", "close_window",
            "Our people need to make peace. What does it take to stop the war?", () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_whyareyouhere", "empire_envoy_main_hub", "close_window", "Why are you here?",
            () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_close", "empire_envoy_main_hub", "close_window", "That's all thank you.",
            () => IsEmpireEnvoy(), null, 200);
        
        
        



        bool IsEmpireEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null) return partner.HeroObject.HasAttribute("EmpireEnvoy");

            return false;
        }
    }

    private void AddDruchiiEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => !EonirEnvoyDialogCondition(), null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_druchii", "start", "druchii_envoy_main_hub",
            "I am a servant of the Witchking, what can I do for you?", () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_force_war", "druchii_envoy_main_hub", "druchii_envoy_force_war",
            "We need to get rid of one of our surrounding threads. Can you help to cause chaos among our enemies?", () => IsDruchiiEnvoy(), null,
            200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_prisoners", "druchii_envoy_main_hub", "druchii_envoy_prisoners",
            "I would like to provide you prisoners", () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_slaver_tide", "druchii_envoy_main_hub", "druchii_envoy_slaver_tide",
            "I want you to adjust your slaver catchment areas", () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_whyareyouhere", "druchii_envoy_main_hub", "druchii_envoy_whyareyouhere", "Why are you here?",
            () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_close", "druchii_envoy_main_hub", "close_window", "That's all thank you.",
            () => IsDruchiiEnvoy(), null, 200);
        
        
        
        //force war
        campaignGameStarter.AddDialogLine("druchii_envoy_force_war", "druchii_envoy_force_war", "druchii_envoy_force_war_choice",
            "You would be surprised what trouble  the tip of a Khainite dagger could cause. If it finds the wrong throat to the wrong time, war can emerge.", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_1", "druchii_envoy_force_war_choice", "druchii_envoy_force_war_choice_result",
            "Let's do this.", () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_2", "druchii_envoy_force_war_choice", "back_to_main_hub_druchii",
            "I need to think about this.", () => IsDruchiiEnvoy(), null, 200);
            
        campaignGameStarter.AddDialogLine("druchii_envoy_force_war_choice_result", "druchii_envoy_force_war_choice_result", "druchii_envoy_main_hub",
            "We will see what we can do.", () => IsDruchiiEnvoy(), null, 200);
        
        //exchange prisoners
        
        campaignGameStarter.AddDialogLine("druchii_envoy_prisoners", "druchii_envoy_prisoners", "back_to_main_hub_druchii",
            "What a promising trade. This will be credited for your next negotiation with the Witch king I believe the Council will like this.", () => IsDruchiiEnvoy(), null, 200);
        
        
        
        //slaver tide
        
        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide_choice",
            "...this will cost you dearly your influence over the Council... (1000 Favor) I could see what I can do.  So where do you suggest our Black Arks to anchor?", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_1", "druchii_envoy_slaver_tide_choice", "druchii_envoy_slaver_tide_choice_result",
            "Let's do this.", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_2", "druchii_envoy_slaver_tide_choice", "back_to_main_hub_druchii",
            "I need to think about this.", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide_choice_result", "druchii_envoy_slaver_tide_choice_result", "back_to_main_hub_druchii",
            "Our slaver fleets will head towards there. I will pull a few strings to let it happen", () => IsDruchiiEnvoy(), null, 200);
        
        
        
        // why are you here?
        
        
        
        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere", "druchii_envoy_whyareyouhere", "envoy_druchii_wayh_reaction",
            "The Witchking, sends his best regards. I know we were not always on the best terms, yet we think both, the druchii and the Eonir, share undeniable commonalities.... we were both betrayed by the Asur..", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_displeased", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            "You are raiding the coasts of the Eonir, enslaved and drugged our people and now have the audacity to come here and pretend this never happened?", () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_undecided", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            "I am skeptical, you come here without evil ambitions. What would be the benefit for the Eonir, letting you talk here?", () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_agreement", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            "The Asur betrayed us, left us to die. I might not share your sentiment, but in the end you also set yourself free from their reign", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere_2", "druchii_envoy_whyareyouhere_2", "druchii_envoy_whyareyouhere_3",
            "Listen, I am not your enemy. The opposite is true, the witch king send me, to improve our relationship. there are many ways we could avoid harm on the eonir, and the druchii are one of it. Take our hand as long as it available, I am sure it will be for your best.", () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere_3", "druchii_envoy_whyareyouhere_3", "back_to_main_hub_druchii",
            "We can take care of some of your problems, some of your darkest desires without any question being asked, and ensure the sea protection of your fleets... all we want is a little power within your council, do you think you can do that?", () => IsDruchiiEnvoy(), null, 200);

        
        campaignGameStarter.AddDialogLine("back_to_main_hub_druchii", "back_to_main_hub_druchii", "druchii_envoy_main_hub",
            "Is there something else I could do for you?", () => IsDruchiiEnvoy(), null, 200);
        
        //back to hub
        
        campaignGameStarter.AddDialogLine("back_to_main_hub_druchii", "back_to_main_hub_druchii", "druchii_envoy_main_hub",
            "Is there something else the Witch king could do for you?", () => IsDruchiiEnvoy(), null, 200);
        
        
        
        
        bool IsDruchiiEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null) return partner.HeroObject.HasAttribute("DruchiiEnvoy");

            return false;
        }
    }

    private void AddAsurEnvoyDialogLines(CampaignGameStarter starter)
    {
        starter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => !EonirEnvoyDialogCondition(), null, 200);

        starter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && !HasRenown2(),
            null, 200);


        starter.AddDialogLine("envoy_hub_intro_asur", "start", "asur_envoy_main_hub",
            "I am the Envoy of the Pheonix King, what can the Asur do for you?", () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_money", "asur_envoy_main_hub", "asur_envoy_money",
            "I can put a good word for you or your ambitions, but money is short... is there something the Phoenix Throne can do?",
            () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_troops", "asur_envoy_main_hub", "asur_envoy_troops", "We need your help can you spare some troops?",
            () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_diplomacy", "asur_envoy_main_hub", "asur_envoy_diplomacy",
            "Can you help the eonir to improve the relationship with the Kingdoms of Men?", () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_whyareyouhere", "asur_envoy_main_hub", "asur_envoy_whyareyouhere", "Why are you here?", () => IsAsurianEnvoy(),
            null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_close", "asur_envoy_main_hub", "close_window", "That's all thank you.", () => IsAsurianEnvoy(),
            null, 200);
        
        
        //money
        starter.AddDialogLine("asur_envoy_money", "asur_envoy_money", "asur_envoy_money_choice",
            "Nothing easier than this. How much do you need?", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("asur_envoy_money_choice_1", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "10000 for 100", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("asur_envoy_money_choice_2", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "100000 for 500", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("asur_envoy_money_choice_3", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "300000 for 1000", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("asur_envoy_money_choice_quit", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "I need to think about this.", () => IsAsurianEnvoy(), null, 200);
        
        //troops
        starter.AddDialogLine("asur_envoy_troops", "asur_envoy_troops", "back_to_main_hub_asur",
            "Obviously we can help. A batch of our garrison in Marienburg can be made available.", () => IsAsurianEnvoy(), null, 200);
        
        //diplomacy
        starter.AddDialogLine("asur_envoy_diplomacy", "asur_envoy_diplomacy", "back_to_main_hub_asur",
            "The Asur have diplomatic embassies through out the Empire, and have an embassy in Coronne. We can help the Eonir, to improve their overall relationship as a mediator. What do you me to try?", () => IsAsurianEnvoy(), null, 200);
        
        
        //why are you here
        
        starter.AddDialogLine("asur_envoy_whyareyouhere", "asur_envoy_whyareyouhere", "envoy_asur_wayh_reaction",
            "The Phoenix King, send his best regards, from the far Ulthuan. We are brothers, maybe even the same People, that should form an alliance. If there is anything the Asur can do for the Eonir, we are pleased to help where we can.", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_displeased", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            "You left us, descendants of your own kin, to die. We were on our own so long, that your words sound questionable at best.", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_undecided", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            "Be careful Asur, we did not forget your betrayal. I am however not here to maintain a grudge. What do you offer?", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_agreement", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            "Our people are one. Its a long time, we need the help of the Asur. I appreciate you are here.", () => IsAsurianEnvoy(), null, 200);
        
        starter.AddDialogLine("asur_envoy_whyareyouhere_2", "asur_envoy_whyareyouhere_2", "asur_envoy_whyareyouhere_3",
            "The Asur help where we can. If there is anything you need from us. We are are happy to help... in exchange for a good word within the council for us.", () => IsAsurianEnvoy(), null, 200);

        starter.AddDialogLine("asur_envoy_whyareyouhere_3", "asur_envoy_whyareyouhere_3", "back_to_main_hub_asur",
            "The Asur can send to you montary or military help, or try to improve the relationship with other empire kingdoms. In return to a few favors, self explainatory.", () => IsAsurianEnvoy(), null, 200);

        
        starter.AddDialogLine("back_to_main_hub_asur", "back_to_main_hub_asur", "asur_envoy_main_hub",
            "Is there something else I could do for you?", () => IsAsurianEnvoy(), null, 200);
        


        bool IsAsurianEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null) return partner.HeroObject.HasAttribute("AsurEnvoy");

            return false;
        }
    }

    private bool HasRenown2()
    {
        return Clan.PlayerClan.Tier >= 2;
    }

    private bool EonirEnvoyDialogCondition()
    {
        return Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR;
    }

    private void OnNewGameStarted(CampaignGameStarter obj)
    {
        foreach (var settlement in Settlement.All)
            if (settlement.StringId == "town_LL1")
            {
                _torLithanel = settlement;
                CreateEnvoys();
                break;
            }
    }

    private void EnforceEnvoyLocation()
    {
        if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != _torLithanel) return;
        foreach (var envoy in envoys)
        {
            var locationchar = _torLithanel.LocationComplex.GetLocationCharacterOfHero(envoy);
            var lordsHall = _torLithanel.LocationComplex.GetLocationWithId("lordshall");
            var currentloc = _torLithanel.LocationComplex.GetLocationOfCharacter(locationchar);
            if (locationchar is null || lordsHall is null || currentloc is null) return;
            if (currentloc != lordsHall) _torLithanel.LocationComplex.ChangeLocation(locationchar, currentloc, lordsHall);
        }
    }

    private void CreateEnvoys()
    {
        var templateDruchii = MBObjectManager.Instance.GetObject<CharacterObject>(_druchiiEnvoyId);
        var templateAsur = MBObjectManager.Instance.GetObject<CharacterObject>(_asurEnvoyId);
        var templateEmpire = MBObjectManager.Instance.GetObject<CharacterObject>(_empireEnvoyId);
        var templateSpellsinger = MBObjectManager.Instance.GetObject<CharacterObject>(_spellsingerEnvoyId);

        envoys = new List<Hero>();
        if (templateDruchii != null)
        {
            _druchiiEnvoy = HeroCreator.CreateSpecialHero(templateDruchii, _torLithanel, null, null, 50);
            _druchiiEnvoy.SupporterOf = _torLithanel.OwnerClan;
            var name = _druchiiEnvoy.FirstName;
            _druchiiEnvoy.SetName(new TextObject("{=tor_eonir_envoy_asur}Envoy of the Witch King"), name);

            HeroHelper.SpawnHeroForTheFirstTime(_druchiiEnvoy, _torLithanel);
            envoys.Add(_druchiiEnvoy);
        }

        if (templateAsur != null)
        {
            _asurEnvoy = HeroCreator.CreateSpecialHero(templateAsur, _torLithanel, null, null, 50);
            _asurEnvoy.SupporterOf = _torLithanel.OwnerClan;
            var name = _asurEnvoy.FirstName;
            HeroHelper.SpawnHeroForTheFirstTime(_asurEnvoy, _torLithanel);
            _asurEnvoy.SetName(new TextObject("{=tor_eonir_envoy_asur}Envoy of the Phoenix King"), name);
            envoys.Add(_asurEnvoy);
        }

        if (templateEmpire != null)
        {
            _empireEnvoy = HeroCreator.CreateSpecialHero(templateEmpire, _torLithanel, null, null, 50);
            _empireEnvoy.SupporterOf = _torLithanel.OwnerClan;
            var name = _empireEnvoy.FirstName;
            _empireEnvoy.SetName(new TextObject("{=tor_eonir_envoy_asur}Envoy of the Empire"), name);
            HeroHelper.SpawnHeroForTheFirstTime(_empireEnvoy, _torLithanel);
            envoys.Add(_empireEnvoy);
        }

        if (templateSpellsinger != null)
        {
            _spellsingerEnvoy = HeroCreator.CreateSpecialHero(templateSpellsinger, _torLithanel, null, null, 50);
            _spellsingerEnvoy.SupporterOf = _torLithanel.OwnerClan;
            var name = _spellsingerEnvoy.FirstName;
            _spellsingerEnvoy.SetName(new TextObject("{=tor_eonir_envoy_asur}Spellsinger of the Faniour"), name);

            HeroHelper.SpawnHeroForTheFirstTime(_spellsingerEnvoy, _torLithanel);

            envoys.Add(_spellsingerEnvoy);
        }
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
}