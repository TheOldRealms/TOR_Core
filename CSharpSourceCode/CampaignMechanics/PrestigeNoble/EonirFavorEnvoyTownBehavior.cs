using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Menagery;

public class EonirFavorEnvoyTownBehavior : CampaignBehaviorBase
{
    private const string _druchiiEnvoyId = "tor_eonir_druchii_envoy_0";
    private const string _asurEnvoyId = "tor_eonir_asur_envoy_0";
    private const string _empireEnvoyId = "tor_eonir_empire_envoy_0";
    private const string _spellsingerEnvoyId = "tor_eonir_spellsinger_envoy_0";


    private int asur_favor_price1 = 100;
    private int asur_favor_price2 = 500;
    private int asur_favor_price3 = 1000;
    private bool _isDruchiiEnvoyTrade;

    private int druchii_force_war_price_base = 750;
    private int druchii_slaver_tide_price_base = 1000;

    private int empireCalculatedExchangeBack;
    private int peaceCost = 750;

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
        
        CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonersSold);
    }

    private void OnPrisonersSold(MobileParty arg1, FlattenedTroopRoster arg2, Settlement arg3)
    {
        if (_isDruchiiEnvoyTrade)
        {
            foreach (var element in arg2)
            {
                if (!element.Troop.IsHero)
                {
                    Hero.MainHero.AddCultureSpecificCustomResource(element.Troop.Tier);
                    arg3.Party.PrisonRoster.RemoveTroop(element.Troop);
                }
            }
        }
  
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
        SetTextVariables();
    }

    private void SetTextVariables()
    {
        GameTexts.SetVariable("EONIR_FAVOR",CustomResourceManager.GetResourceObject("CouncilFavor").GetCustomResourceIconAsText(false));
    }
    

    private void AddSpellsingerEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => EonirEnvoyDialogCondition()&& Hero.MainHero.Culture.StringId!=TORConstants.Cultures.EONIR, null, 200);
        

        campaignGameStarter.AddDialogLine("envoy_hub_intro_spellsinger", "start", "spellsinger_envoy_main_hub", "The forest told of me your coming, yet not why. What have you come to ask of me?",
            () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_world_roots", "spellsinger_envoy_main_hub", "spellsinger_envoy_world_roots",
            "I need to travel to roots of the Asrai, would you guide me over it?", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_troop_refill", "spellsinger_envoy_main_hub", "spellsinger_envoy_troop_refill",
            "We need the Forestborn, can you call for them?", () => IsSpellsingerEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_spellsinger_magic", "spellsinger_envoy_main_hub", "back_to_main_hub_spellsinger", "{=tor_spelltrainer_eonir_open_book_str}I want to learn more magic can you help?.", () => MobileParty.MainParty.HasSpellCasterMember()&&Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR && IsSpellsingerEnvoy(), openbookconsequence, 200, null);


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_whyareyouhere", "spellsinger_envoy_main_hub", "spellsinger_envoy_whyareyouhere",
            "Why are you here?", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_close", "spellsinger_envoy_main_hub", "close_window", "That's all thank you.",
            () => IsSpellsingerEnvoy(), null, 200);
        
        //travel info
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_world_roots", "spellsinger_envoy_world_roots", "spellsinger_envoy_world_roots_choice",
            "See , I can help you with that, I won't do for nothing though. Help our people to gain more power, and I will allow you to travel via the roots.", () => IsSpellsingerEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("spellsinger_envoy_world_roots_choice_1", "spellsinger_envoy_world_roots_choice", "spellsinger_envoy_world_roots_results",
            "This is fine. I am willing to help you. How can I travel then?", () => IsSpellsingerEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_world_roots_results", "spellsinger_envoy_world_roots_results", "back_to_main_hub_spellsinger",
            "Look for the world root end in the Laurelorn forest, a spellsinger will help you travel along the roots. We can take you to the Maisontaal, in the far away Bretonnia. I am not willing to upset the Asrai, therefore we will not take you the whole root up to their Oak of Ages.", () => IsSpellsingerEnvoy(), null, 200);
        
        //refill
        
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill_choice",
            "Many Forestborn, are living as nomades far from our villages. Reaching them can be tricky, one needs to call them. You are willing me to do so?", () => IsSpellsingerEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_1", "spellsinger_envoy_troop_refill_choice", "spellsinger_envoy_troop_refill_result",
            "That would be kind. I am sure we will be able to pay that favor back one day. I will decide in favor of the forestborn", () => IsSpellsingerEnvoy()&& 200<=Hero.MainHero.GetCultureSpecificCustomResourceValue(), null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_2", "spellsinger_envoy_troop_refill_choice", "back_to_main_hub_spellsinger",
            "I need to think about this.", () => IsSpellsingerEnvoy(), null, 200);
            
        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill_result", "spellsinger_envoy_troop_refill_result", "back_to_main_hub_spellsinger",
            "I will see what I can do.", () => IsSpellsingerEnvoy(), RefillVillages, 200);


        void RefillVillages()
        {
            foreach (var village in Settlement.All.WhereQ(x=> x.IsVillage && x.Culture.StringId == TORConstants.Cultures.EONIR))
            {
                foreach (var notable in village.Notables)
                {
                    var eonirCulture = village.Culture;
                    for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                    {
                        if (notable.VolunteerTypes[i] == null)
                        {
                            notable.VolunteerTypes[i] = eonirCulture.BasicTroop;
                        }
                        
                    }
                }
            }
            
            Hero.MainHero.AddCultureSpecificCustomResource(-200);
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm,200f);
        }
        
                //why are you here
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere", "spellsinger_envoy_whyareyouhere", "envoy_spellsinger_wayh_reaction",
            "I am representing a coven of Spellsingers dedicated to the defense of Laurelorn. While the Council is busy with politics, the Faniour, the forest born elves, are endagered by all the threads of the forest.", () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_displeased", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            "The forest is beset by destructive beasts and men alike, what then, are you protecting?", () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_undecided", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            "What matters can the Council solve for you? What can you give me in turn?", () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_agreement", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            "The Faniour aswell as the Touriour follow the same people. Your matters, are my matters.", () => IsSpellsingerEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_3",
            "I am not a man of politics. I am here to stand our case. I know that seemingly the Council, sometimes forgets about us. Thats why I am here, and standing for the forest people. I know that nothing works here, without giving something in return.", () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_3", "spellsinger_envoy_whyareyouhere_3", "back_to_main_hub_spellsinger",
            "Help us with your political power, and I will try to make it worth. I can assist you with some magic, like using the world roots of the Asrai, or can make your influence provide you power over the council. Apart, I can see what i can do for teaching you some magic", () => IsSpellsingerEnvoy(), null, 200);


        campaignGameStarter.AddDialogLine("back_to_main_hub_spellsinger", "back_to_main_hub_spellsinger", "spellsinger_envoy_main_hub",
            "Is there something else I could do for you?", () => IsSpellsingerEnvoy(), null, 200);
        
        

        bool IsSpellsingerEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null&& partner.IsHero) return partner.HeroObject.HasAttribute("SpellsingerEnvoy");

            return false;
        }
    }


    private void AddEmpireEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => EonirEnvoyDialogCondition()&& Hero.MainHero.Culture.StringId!=TORConstants.Cultures.EONIR, null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && IsEmpireEnvoy() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_empire", "start", "empire_envoy_main_hub", "How can the empire help?",
            () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_prestige_to_favour", "empire_envoy_main_hub", "empire_envoy_prestige_to_favour",
            "I bring quality goods to trade and wish to build my reputation amongst the High Council. Are you interested?", () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("Prestige")> 3, null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_favour_to_prestige", "empire_envoy_main_hub", "empire_envoy_favour_to_prestige",
            "I need a few prestigious goods of the empire, what does it take me to get them from you?", () => IsEmpireEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_empire_peace", "empire_envoy_main_hub", "empire_envoy_force_peace",
            "Our people need to make peace. What does it take to stop the war?", () => IsEmpireEnvoy() && AllEmpireFactionsAtWar().Count>0, null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_whyareyouhere", "empire_envoy_main_hub", "empire_envoy_whyareyouhere", "Why are you here?",
            () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_close", "empire_envoy_main_hub", "close_window", "That's all thank you.",
            () => IsEmpireEnvoy(), null, 200);
        
        
        //force peace
        campaignGameStarter.AddDialogLine("empire_envoy_force_peace", "empire_envoy_force_peace", "empire_envoy_force_peace_choice",
            "The Empire and the Council should make peace. Your people, neither ours will do this without hesitantion ({PEACE_COSTS}{EONIR_FAVOR})", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_1", "empire_envoy_force_peace_choice", "empire_envoy_force_peace_choice_result",
            "Let's do this.", () => IsEmpireEnvoy() && AllEmpireFactionsAtWar().Count>0 && peaceCost<=Hero.MainHero.GetCultureSpecificCustomResourceValue(), null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_2", "empire_envoy_force_peace_choice", "back_to_main_hub_empire",
            "I need to think about this.", () => IsEmpireEnvoy(), null, 200);
            
        campaignGameStarter.AddDialogLine("empire_envoy_force_peace_choice_result", "empire_envoy_force_peace_choice_result", "back_to_main_hub_empire",
            "We will see what we can do.", () => IsEmpireEnvoy(), ForcePeacePrompt, 200);


        List<Kingdom> AllEmpireFactionsAtWar()
        {
            var laurelorn = Campaign.Current.Kingdoms.FirstOrDefault(x => x.StringId == "laurelorn");
            var allElectorStatesAtWar = Campaign.Current.Kingdoms.WhereQ(x => !x.IsEliminated&& x.Culture.StringId==TORConstants.Cultures.EMPIRE && (x.IsAtWarWith(laurelorn) || Hero.MainHero.IsKingdomLeader&& x.IsAtWarWith(Hero.MainHero.Clan.Kingdom))).ToList();

            return allElectorStatesAtWar;
        }
        
        void ForcePeacePrompt()
        {
            List<InquiryElement> list = [];

            var laurelorn = Campaign.Current.Kingdoms.FirstOrDefault(x => x.StringId == "laurelorn");

            var allElectorStatesAtWar = AllEmpireFactionsAtWar();

            foreach (var kingdom in allElectorStatesAtWar)
            {
                list.Add(new InquiryElement(kingdom,kingdom.EncyclopediaTitle.ToString(),null,true,"Force Peace with"));
            }
            
            if (list.IsEmpty()) return;
            
            var inquirydata = new MultiSelectionInquiryData("Force Peace", "Force an empire state to be in peace with the eonir", list, true, 1, 1, "Confirm", "Cancel", ForcePeace, null,"",true);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);


            void ForcePeace(List<InquiryElement> inquiryElements)
            {
                var kingdom = (Kingdom)inquiryElements[0].Identifier;
                if (Hero.MainHero.IsKingdomLeader)
                {
                    MakePeaceAction.Apply(kingdom,Hero.MainHero.Clan.Kingdom,0);
                }
                else
                {
                    MakePeaceAction.Apply(kingdom,laurelorn,0);
                }
                
                Hero.MainHero.AddCultureSpecificCustomResource(-peaceCost);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, peaceCost);
            }
        }
        
        
        //Exchange all Prestige to Council Favor
        
        campaignGameStarter.AddDialogLine("empire_envoy_prestige_to_favour", "empire_envoy_prestige_to_favour", "empire_envoy_prestige_to_favour_choice",
            "Obviously your offering the empire can benefit the Council.(Exchange {ORIGINAL_PRESTIGE} to {RETURN_FAVOR}{EONIR_FAVOR}) ", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("empire_envoy_prestige_to_favour_choice_1", "empire_envoy_prestige_to_favour_choice", "empire_envoy_prestige_to_favour_result",
            "Let's do this.", () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("Prestige")> 3, null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_prestige_to_favour_choice_2", "empire_envoy_prestige_to_favour_choice", "back_to_main_hub_empire",
            "I need to think about this.", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("empire_envoy_prestige_to_favour_result", "empire_envoy_prestige_to_favour_result", "back_to_main_hub_empire",
            "I am glad to make businesses with you(Exchanged {ORIGINAL_PRESTIGE} Prestige to {RETURN_FAVOR}{EONIR_FAVOR})", () => IsEmpireEnvoy(), ExchangePrestigeToFavor, 200);
        
        void ExchangePrestigeToFavor()
        {
            var prestige = Hero.MainHero.GetCustomResourceValue("Prestige");
            Hero.MainHero.AddCultureSpecificCustomResource(empireCalculatedExchangeBack);
            
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, empireCalculatedExchangeBack);
            
            Hero.MainHero.AddCustomResource("Prestige",-prestige);
        }
        
        //Favor to Prestige
        
        campaignGameStarter.AddDialogLine("empire_envoy_favour_to_prestige", "empire_envoy_favour_to_prestige", "empire_envoy_favour_to_prestige_choice",
            "The Empires ambitions need to way in the Council. (gain for 50 Council Favour 30 Prestige)", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("empire_envoy_favour_to_prestige_choice_1", "empire_envoy_favour_to_prestige_choice", "empire_envoy_favour_to_prestige_result",
            "Let's do this.", () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("CouncilFavor")> 50, null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_favour_to_prestige_choice_2", "empire_envoy_favour_to_prestige_choice", "back_to_main_hub_empire",
            "I need to think about this.", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("empire_envoy_favour_to_prestige_result", "empire_envoy_favour_to_prestige_result", "back_to_main_hub_empire",
            "I am glad to make businesses with you.", () => IsEmpireEnvoy(), ExchangeFavorToPrestige, 200);
        
        
        
        // why are you here?
        
        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere", "empire_envoy_whyareyouhere", "envoy_empire_wayh_reaction",
            "As an envoy of Graf Boris Todbringer I represent the interests of Middenland, and to a minor extent that of the Empire as a whole. We wish to maintain peaceful relations with Eonir, built on trust, trade and mutual respect so that we may all benefit.", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_displeased", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            "You encroach upon our lands without heeding what is sacred, yet talk to us about respect? You do not belong here.", () => IsEmpireEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_undecided", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            "It benefits neither of us to make enemies when they aren't needed, trade and peace can only benefit both our peoples.", () => IsEmpireEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_agreement", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            "The problems of the empire are shared with the Eonir. We need to fight together side on side, all of our common enemies.", () => IsEmpireEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere_2", "empire_envoy_whyareyouhere_2", "empire_envoy_whyareyouhere_3",
            "We seek only to cooperate, to the mutual benefit of all involved. Conflict between our peoples serves to aid none but our enemies, surely you can agree with me on this.", () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere_3", "empire_envoy_whyareyouhere_3", "back_to_main_hub_empire",
            "My word carries weight, as does that of our Graf. I can ensure a state of peace and profitable trade between our nations, we may even be able to provide mercenaries should your lands ever be threatened. ...Think on it, we both have much to gain.", () => IsEmpireEnvoy(), null, 200);
        
        void ExchangeFavorToPrestige()
        {
            Hero.MainHero.AddCustomResource("CouncilFavor",-50);
            Hero.MainHero.AddCustomResource("Prestige",30);
        }
        
        void calculateCost(float prestige)
        {
            empireCalculatedExchangeBack = (int)(prestige * (1f / 2) + prestige *(1f/3* (Hero.MainHero.GetSkillValue(DefaultSkills.Charm)/300f)));
            GameTexts.SetVariable("ORIGINAL_PRESTIGE",prestige);
            GameTexts.SetVariable("RETURN_FAVOR",empireCalculatedExchangeBack);
            GameTexts.SetVariable("PEACE_COSTS",peaceCost);
        }
        
        bool IsEmpireEnvoy()
        {
            calculateCost(Hero.MainHero.GetCustomResourceValue("Prestige"));
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null && partner.IsHero) return partner.HeroObject.HasAttribute("EmpireEnvoy");

            return false;
        }
        
        //back to hub
        
        campaignGameStarter.AddDialogLine("back_to_main_hub_empire", "back_to_main_hub_empire", "empire_envoy_main_hub",
            "Is there another way I can be of service?", () => IsEmpireEnvoy(),null, 200);
        
    }

 

    private void AddDruchiiEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => EonirEnvoyDialogCondition()&& Hero.MainHero.Culture.StringId!=TORConstants.Cultures.EONIR, null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && IsDruchiiEnvoy() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_druchii", "start", "druchii_envoy_main_hub",
            "I am a servant of the Witchking, what can I do for you?", () => EonirEnvoyDialogCondition() && IsDruchiiEnvoy(), null, 200);

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
            "You would be surprised what trouble  the tip of a Khainite dagger could cause. If it finds the wrong throat to the wrong time, war can emerge. (Declare war between 2 factions ({FORCEWAR_PRICE}{EONIR_FAVOR})", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_1", "druchii_envoy_force_war_choice", "druchii_envoy_force_war_choice_result",
            "Let's do this.", () => IsDruchiiEnvoy() && (druchii_force_war_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)<=Hero.MainHero.GetCultureSpecificCustomResourceValue()), null, 200);
        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_2", "druchii_envoy_force_war_choice", "back_to_main_hub_druchii",
            "I need to think about this.", () => IsDruchiiEnvoy(), null, 200);
            
        campaignGameStarter.AddDialogLine("druchii_envoy_force_war_choice_result", "druchii_envoy_force_war_choice_result", "back_to_main_hub_druchii",
            "We will see what we can do.", () => IsDruchiiEnvoy(), ForceWarPrompt, 200);
        
        //exchange prisoners
        
        campaignGameStarter.AddDialogLine("druchii_envoy_prisoners", "druchii_envoy_prisoners", "back_to_main_hub_druchii",
            "What a promising trade. This will be credited for your next negotiation with the Witch king I believe the Council will like this.", () => IsDruchiiEnvoy(), ExchangePrisoners, 200);


        void ExchangePrisoners()
        {
            _isDruchiiEnvoyTrade = true;
            
            PartyScreenManager.OpenScreenAsDonatePrisoners();
        }
        
        void ForceWarPrompt()
        {
            List<InquiryElement> list = [];

            var allKingdoms = Campaign.Current.Kingdoms.WhereQ(x => !x.IsEliminated).ToList();

            foreach (var kingdom in allKingdoms)
            {
                list.Add(new InquiryElement(kingdom,kingdom.EncyclopediaTitle.ToString(),null,true,"Force war between two kingdoms"));
            }
            
            if (list.IsEmpty()) return;
            
            var inquirydata = new MultiSelectionInquiryData("Improve Relationship with one faction", "Select 2 Factions war will emerge between.", list, true, 2, 2, "Confirm", "Cancel", ForceWar, null,"",true);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);

            void ForceWar(List<InquiryElement> inquiryElements)
            {
                var kingdom1 = (Kingdom)inquiryElements[0].Identifier;
                var kingdom2 = (Kingdom)inquiryElements[1].Identifier;

                if (MBRandom.RandomFloat < 0.5f)
                {
                    DeclareWarAction.ApplyByDefault(kingdom1, kingdom2);
                }
                else
                {
                    DeclareWarAction.ApplyByDefault(kingdom2,kingdom1);
                }
                
                Hero.MainHero.AddCultureSpecificCustomResource(-(druchii_force_war_price_base-Hero.MainHero.GetSkillValue(DefaultSkills.Charm)));
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, druchii_force_war_price_base);
            }
        }
        
        
        //slaver tide
        
        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide_choice",
            "...this will cost you dearly your influence over the Council... ({SLAVERTIDE_PRICE}{EONIR_FAVOR}) I could see what I can do.  So where do you suggest our Black Arks to anchor?", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_1", "druchii_envoy_slaver_tide_choice", "druchii_envoy_slaver_tide_choice_result",
            "Let's do this({SLAVERTIDE_PRICE}{EONIR_FAVOR}).", () => IsDruchiiEnvoy() && (druchii_slaver_tide_price_base-Hero.MainHero.GetSkillValue(DefaultSkills.Charm))<=Hero.MainHero.GetCultureSpecificCustomResourceValue(), SlaverTidePrompt, 200);
        
        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_2", "druchii_envoy_slaver_tide_choice", "back_to_main_hub_druchii",
            "I need to think about this.", () => IsDruchiiEnvoy(), null, 200);
        
        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide_choice_result", "druchii_envoy_slaver_tide_choice_result", "back_to_main_hub_druchii",
            "Our slaver fleets will head towards there. I will pull a few strings to let it happen", () => IsDruchiiEnvoy(), SlaverTidePrompt, 200);


        void SlaverTidePrompt()
        {
            List<InquiryElement> list = [];
            var coastalKingdoms = Campaign.Current.Kingdoms.WhereQ(x => !x.IsEliminated && x.IsCoastalKingdom()).ToList();

            foreach (var kingdom in coastalKingdoms)
            {
                list.Add(new InquiryElement(kingdom,kingdom.Name.ToString(),null,true,""));
            }
            
            var inquirydata = new MultiSelectionInquiryData("Choose kingdom to swarm of druchii troops", "Select a kingdom being swarmed by druchii slaver troops.", list, true, 1, 1, "Confirm", "Cancel", SwarmKingdomWithDruchii, null,"",true);

            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            void SwarmKingdomWithDruchii(List<InquiryElement> inquiryElements)
            {
                var kingdom = (Kingdom)inquiryElements[0].Identifier;

                var slaverBay = Campaign.Current.Settlements.FirstOrDefaultQ(x => x.StringId == "darkelf_camp_01");

                var slaverBaySettlementComponent =(SlaverCampComponent)slaverBay.SettlementComponent;

                if (slaverBaySettlementComponent != null)
                {
                    foreach (var settlement in kingdom.Settlements)
                    {
                        if(!settlement.IsVillage) continue;

                        if (MBRandom.RandomFloat < 0.25f)
                        {
                            slaverBaySettlementComponent.SpawnNewParty(out var druchiiParty1);
                            slaverBaySettlementComponent.SpawnNewParty(out var druchiiParty2);
                            var ta = druchiiParty1.MemberRoster.CloneRosterData();
                            
                            druchiiParty1.Position2D = settlement.Position2D;
                            druchiiParty2.Position2D = settlement.Position2D;
                            
                            druchiiParty1.MemberRoster.Add(ta);
                            druchiiParty2.MemberRoster.Add(ta);
                            continue;
                        }
                        slaverBaySettlementComponent.SpawnNewParty(out var druchiiParty);

                        druchiiParty.Position2D = settlement.Position2D;

                        var t = druchiiParty.MemberRoster.CloneRosterData();
                        druchiiParty.MemberRoster.Add(t);
                    }
                }
                
                DeclareWarAction.ApplyByDefault(slaverBay.OwnerClan,kingdom);

                Hero.MainHero.AddCultureSpecificCustomResource(-(druchii_slaver_tide_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)));
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, druchii_slaver_tide_price_base);
            }
        }
        
        
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
            "Is there something else the Witch king could do for you?", () => IsDruchiiEnvoy(),null, 200);

        
        void setDruchiiPrices()
        {
            GameTexts.SetVariable("FORCEWAR_PRICE",druchii_force_war_price_base-Hero.MainHero.GetSkillValue(DefaultSkills.Charm));
            GameTexts.SetVariable("SLAVERTIDE_PRICE",druchii_slaver_tide_price_base-Hero.MainHero.GetSkillValue(DefaultSkills.Charm));
        }
        
        bool IsDruchiiEnvoy()
        {
            _isDruchiiEnvoyTrade = false;
            setDruchiiPrices();
            var partner = CharacterObject.OneToOneConversationCharacter;
            
            
            if (partner != null && partner.IsHero) return partner.HeroObject.HasAttribute("DruchiiEnvoy");

            return false;
        }
    }
    
    private void openbookconsequence()
    {
        var state = Game.Current.GameStateManager.CreateState<SpellBookState>();
        state.IsTrainerMode = true;
        state.TrainerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
        Game.Current.GameStateManager.PushState(state);
    }

    private void AddAsurEnvoyDialogLines(CampaignGameStarter starter)
    {
        starter.AddDialogLine("envoy_foreign", "start", "close_window", "You are not part of these people, begone.",
            () => EonirEnvoyDialogCondition()&& Hero.MainHero.Culture.StringId!=TORConstants.Cultures.EONIR, null, 200);

        starter.AddDialogLine("envoy_missRank", "start", "close_window",
            "You do not have the previleg to serve the council. You are of no use. (Low Renown).", () => EonirEnvoyDialogCondition() && IsAsurianEnvoy()&&  !HasRenown2(),
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
            "{ASUR_MONEYRETURN1}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY1}{EONIR_FAVOR}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue()>=asur_favor_price1, () => TransferMoney(1,asur_favor_price1), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_2", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN2}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY2}{EONIR_FAVOR}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue()>=asur_favor_price2, () => TransferMoney(10,asur_favor_price2), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_3", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN3}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY3}{EONIR_FAVOR}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue()>=asur_favor_price3, () => TransferMoney(30,asur_favor_price3), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_quit", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "I need to think about this.", () => IsAsurianEnvoy(), null, 200);
        
        //troops
        starter.AddDialogLine("asur_envoy_troops", "asur_envoy_troops", "asur_envoy_troops_choice",
            "Obviously we can help. A batch of our garrison in Marienburg can be made available.", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("asur_envoy_troops_choice_1", "asur_envoy_troops_choice", "back_to_main_hub_asur",
            "Sure I accept (150 {EONIR_FAVOR}).", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue()>=150, ShowTroopSelectionScreen, 200);
        starter.AddPlayerLine("asur_envoy_troops_choice_2", "asur_envoy_troops_choice", "back_to_main_hub_asur",
            "I need to think about this.", () => IsAsurianEnvoy(), null, 200);
        
        //diplomacy
        starter.AddDialogLine("asur_envoy_diplomacy", "asur_envoy_diplomacy", "asur_envoy_diplomacy_choice",
            "The Asur have diplomatic embassies through out the Empire, and have an embassy in Coronne. We can help the Eonir, to improve their overall relationship as a mediator. What do you me to try?", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("asur_envoy_diplomacy_choice_1", "asur_envoy_diplomacy_choice", "back_to_main_hub_asur",
            "Sure I accept (400 {EONIR_FAVOR}).", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue()>=400, AsurDiplomacyPrompt, 200);
        starter.AddPlayerLine("asur_envoy_diplomacy_choice_2", "asur_envoy_diplomacy_choice", "back_to_main_hub_asur",
            "I need to think about this.", () => IsAsurianEnvoy(), null, 200);

        
        //why are you here
        
        starter.AddDialogLine("asur_envoy_whyareyouhere", "asur_envoy_whyareyouhere", "envoy_asur_wayh_reaction",
            "The Phoenix King, send his best regards, from the far Ulthuan. We are brothers, maybe even the same People, that should form an alliance. If there is anything the Asur can do for the Eonir, we are pleased to help where we can.", () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_displeased", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            "You left us, descendants of your own kin, to die. We were on our own so long, that your words sound questionable at best.", () => IsAsurianEnvoy(), null,200);
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


        void SetupPrices()
        {
            GameTexts.SetVariable("ASUR_MONEYRETURN1",CalculateBasePrice());
            GameTexts.SetVariable("ASUR_MONEYRETURN2",CalculateBasePrice()*10);
            GameTexts.SetVariable("ASUR_MONEYRETURN3",CalculateBasePrice()*30);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY1",asur_favor_price1);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY2",asur_favor_price2);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY3",asur_favor_price3);
        }

        int CalculateBasePrice()
        {
            var moneyReturn = 1000;
            var charm = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
            moneyReturn += (int)(moneyReturn * ( (float)charm/100));
            return moneyReturn;
        }

        void TransferMoney(int factor, int favorPrice)
        {
            var t = CalculateBasePrice();

            var revenue = t * factor;
            
            Hero.MainHero.ChangeHeroGold(revenue);
            Hero.MainHero.AddCultureSpecificCustomResource(-favorPrice);
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, favorPrice);

        }
        
        bool IsAsurianEnvoy()
        {
            SetupPrices();
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null && partner.IsHero) return partner.HeroObject.HasAttribute("AsurEnvoy");

            return false;
        }

        void ShowTroopSelectionScreen()
        {
            var roster = TroopRoster.CreateDummyTroopRoster();
            
            
            var asurBaseTroop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_he_seaelf_militia");

            var skillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
            var finished = false;
            var count = 3;
            while (!finished || count>=25)
            {
                if (MBRandom.RandomFloat < ((float)skillValue -10) / 300)
                {
                    count++;
                }
                else
                {
                    finished = true;
                }
            }

            var troop = asurBaseTroop;
            for (int i = 0; i < count; i++)
            {
                var upgradeFailed = false;
                while (!upgradeFailed )
                {
                    if (troop.UpgradeTargets == null || troop.UpgradeTargets.Length == 0)
                    {
                        if (MBRandom.RandomFloat < ((float)skillValue -150)/ 300)
                        {
                            troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_he_white_lion_chrace");
                        }
                        break;
                    }
                  
                    if (MBRandom.RandomFloat < ((float)skillValue -50)/ 300)
                    {
                        troop = troop.UpgradeTargets.GetRandomElement();
                    }
                    else
                    {
                        upgradeFailed=true;
                    }
                }

                roster.AddToCounts(troop, 1);
            }
            
            PartyScreenManager.OpenScreenAsReceiveTroops(roster, new TextObject("Asur support"), OnscreenClosed);

            void OnscreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
            {
                if(fromCancel) return;

                if (leftMemberRoster.Count < count)
                {
                    Hero.MainHero.AddCultureSpecificCustomResource(-150);
                    
                    Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 150);
                }
            }
        }

        void AsurDiplomacyPrompt()
        {
            List<InquiryElement> list = [];

            var humanKingdoms =
                Campaign.Current.Kingdoms.WhereQ(X => X.Culture.StringId == TORConstants.Cultures.BRETONNIA || X.Culture.StringId == TORConstants.Cultures.EMPIRE).ToList();

            foreach (var kingdom in humanKingdoms)
            {
                
                list.Add(new InquiryElement(kingdom,kingdom.EncyclopediaTitle.ToString(),null,true,"Improve relationship"));
            }
            
            if (list.IsEmpty()) return;
            
            var inquirydata = new MultiSelectionInquiryData("Improve Relationship with one faction", "Choose a faction, the relation of you will improve by 15, and the eonir faction aswell.", list, true, 1, 1, "Confirm", "Cancel", AddRelationship, null,"",true);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);

            void AddRelationship(List<InquiryElement> inquiryElements)
            {
                var eonirClans = Hero.MainHero.CurrentSettlement.OwnerClan.Kingdom.Clans;
                
                var kingdom = (Kingdom)inquiryElements[0].Identifier;
                

                var bonus = Hero.MainHero.GetSkillValue(DefaultSkills.Charm)/20;
                float chance = (float)Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 300;
                
                foreach (var hero in kingdom.Heroes){
                    foreach (var clan in eonirClans)
                    {
                        if (MBRandom.RandomFloat < chance)
                        {
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader,hero, 15+bonus,false);
                        }
                    }
                }
                Hero.MainHero.AddCultureSpecificCustomResource(-400);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 400);
            }
        }
    }

    private bool HasRenown2()
    {
        return Clan.PlayerClan.Tier >= 2;
    }

    private bool EonirEnvoyDialogCondition()
    {
        if (Settlement.CurrentSettlement == null) return false;
        var partner = CharacterObject.OneToOneConversationCharacter;
        if (partner != null && partner.IsHero)
        {
            if (partner.HeroObject.HasAttribute("AsurEnvoy") || partner.HeroObject.HasAttribute("EmpireEnvoy") ||
                partner.HeroObject.HasAttribute("AsurEnvoy") || partner.HeroObject.HasAttribute("SpellsingerEnvoy"))
            {
                return true;
            }
        }

        return false;
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