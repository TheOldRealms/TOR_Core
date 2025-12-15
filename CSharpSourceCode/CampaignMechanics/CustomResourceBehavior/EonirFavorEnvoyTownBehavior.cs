using Helpers;
using SandBox;
using System.Collections.Generic;
using System.Linq;
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
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
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

    [SaveableField(0)] private Dictionary<string, double> _latestEnvoyActionsPerformed = [];

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
        GameTexts.SetVariable("EONIR_FAVOR", CustomResourceManager.GetResourceObject("CouncilFavor").GetCustomResourceIconAsText(false));
    }

    public bool isSpellsingerEnvoy(Hero hero)
    {
        return hero == _spellsingerEnvoy;
    }


    private void AddSpellsingerEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", TORTextHelper.GetText("eonir_envoy_foreign_text", "You are not part of these people, begone."),
            () => EonirEnvoyDialogCondition() && Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR, null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_spellsinger", "start", "spellsinger_envoy_main_hub", TORTextHelper.GetText("eonir_spellsinger_intro_text", "The forest told of me your coming, yet not why. What have you come to ask of me?"),
            () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_world_roots", "spellsinger_envoy_main_hub", "spellsinger_envoy_world_roots",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_ask_text", "I want to travel along the roots of the Asrai, can you be my guide?"), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_troop_refill", "spellsinger_envoy_main_hub", "spellsinger_envoy_troop_refill",
            TORTextHelper.GetText("eonir_spellsinger_troop_refill_ask_text", "We need the Forestborn, are there any who can come to our aid?"), () => IsSpellsingerEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_spellsinger_magic", "spellsinger_envoy_main_hub", "back_to_main_hub_spellsinger", "{=str_tor_spelltrainer_eonir_open_book}I seek to further my knowledge of the Winds of Magic, can you help me achieve this?", () => MobileParty.MainParty.HasSpellCasterMember() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR && IsSpellsingerEnvoy(), openbookconsequence, 200, null);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_spellsinger_lores", "spellsinger_envoy_main_hub", "spellsinger_envoy_spellsinger_lores", "{=str_tor_spelltrainer_eonir_open_book}I think i am ready to learn a new facet of the winds of magic.", () => IsSpellsingerEnvoy() && GreylordIsNotAllowedToLearnMoreLores(), null, 200, null);


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_whyareyouhere", "spellsinger_envoy_main_hub", "spellsinger_envoy_whyareyouhere",
            TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_close", "spellsinger_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That's all thank you."),
            () => IsSpellsingerEnvoy(), null, 200);

        //travel info

        campaignGameStarter.AddDialogLine("spellsinger_envoy_world_roots", "spellsinger_envoy_world_roots", "spellsinger_envoy_world_roots_choice",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_price_text", "I can, but for a price. Help our people, they need more power and then I will allow you to travel the worldroot."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_world_roots_choice_1", "spellsinger_envoy_world_roots_choice", "spellsinger_envoy_world_roots_results",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_accept_text", "Of course, I am willing to help. Now as my guide, please tell me how I can travel along these ancient pathways?"), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_world_roots_results", "spellsinger_envoy_world_roots_results", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_explain_text", "There is an entranceway to the Worldroots here in Laurelorn, first you must find it. Once there, a Spellsinger will help you travel across it but only to Maisontaal in the distant lands of Bretonnia.\n\nWe can go no further, as I am not willing to offend the Asrai. The roots are sacred, it is rare enough for non-Asrai to cross them."), () => IsSpellsingerEnvoy(), null, 200);

        //refill

        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill_choice",
            TORTextHelper.GetText("eonir_spellsinger_troop_refill_offer_text", "There are many who live as nomads, far from our villages and deep within the woods. It will take time for any messages to reach them but it can be done. Is this what you desire?"), () => IsSpellsingerEnvoy() && EnoughTimePassedSinceLastEvent("troop_refill", 10), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill_fail_choice",
            TORTextHelper.GetText("eonir_spellsinger_troop_refill_cooldown_text", "We recently called for the forestborn's aid. We should wait longer for another call"), () => (IsSpellsingerEnvoy() && !EnoughTimePassedSinceLastEvent("troop_refill", 10)), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_troop_refill_fail_choice", "spellsinger_envoy_troop_refill_fail_choice", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_1", "spellsinger_envoy_troop_refill_choice", "spellsinger_envoy_troop_refill_result",
            TORTextHelper.GetText("eonir_spellsinger_troop_refill_accept_text", "That would be kind. I am sure we will be able to pay that favor back one day. I will decide in favor of the forestborn"), () => IsSpellsingerEnvoy() && 200 <= Hero.MainHero.GetCultureSpecificCustomResourceValue(), null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_2", "spellsinger_envoy_troop_refill_choice", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill_result", "spellsinger_envoy_troop_refill_result", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_will_see_what_can_do_text", "I will see what I can do."), () => IsSpellsingerEnvoy(), RefillVillages, 200);


        //learn new lores


        campaignGameStarter.AddDialogLine("spellsinger_envoy_spellsinger_lores", "spellsinger_envoy_spellsinger_lores", "spellsinger_envoy_spellsinger_lores_choice",
            TORTextHelper.GetText("eonir_spellsinger_learn_lore_offer_text", "I can teach you, but as much as you are ready to do so, I need your word in the Council (500{EONIR_FAVOR})?"), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("spellsinger_envoy_spellsinger_lores_choice_1", "spellsinger_envoy_spellsinger_lores_choice", "spellsinger_envoy_spellsinger_lores_result",
            TORTextHelper.GetText("eonir_spellsinger_learn_lore_accept_text", "It shouldn't be for your disadvantage"), () => IsSpellsingerEnvoy() && 500 <= Hero.MainHero.GetCultureSpecificCustomResourceValue() && GreylordIsNotAllowedToLearnMoreLores(), LearnNewLoresPrompt, 200);
        campaignGameStarter.AddPlayerLine("spellsinger_envoy_spellsinger_lores_choice_2", "spellsinger_envoy_spellsinger_lores_choice", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_spellsinger_lores_result", "spellsinger_envoy_spellsinger_lores_result", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_will_see_what_can_do_text", "I will see what I can do."), () => IsSpellsingerEnvoy(), null, 200);

        bool GreylordIsNotAllowedToLearnMoreLores()
        {
            if (!Hero.MainHero.HasCareer(TORCareers.GreyLord))
            {
                return false;
            }

            if (!Hero.MainHero.HasAttribute("CareerTier2"))
            {
                return false;
            }

            var lores = LoreObject.GetAll();

            var list = new List<string>()
            {
                "LoreOfFire",
                "LoreOfMetal",
                "LoreOfLife",
                "LoreOfBeasts",
                "LoreOfLight",
                "LoreOfHeavens",
                "LoreOfDeath",
                "HighMagic"
            };
            var count = 0;
            foreach (var lore in lores)
            {
                if (list.Contains(lore.ID))
                {
                    if (Hero.MainHero.HasKnownLore(lore.ID))
                        count++;
                }
            }


            return count <= 4;
        }

        void LearnNewLoresPrompt()
        {
            List<InquiryElement> list = [];

            var lores = LoreObject.GetAll();

            lores = lores.WhereQ(X => !X.DisabledForCultures.Contains(TORConstants.Cultures.EONIR) && X.ID != "DarkMagic" && !Hero.MainHero.HasKnownLore(X.ID)).ToList();

            foreach (var lore in lores)
            {
                list.Add(new InquiryElement(lore, lore.Name, null, true, TORTextHelper.GetText("eonir_spellsinger_learn_lore_hint_text", "Learn new lore")));
            }

            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("eonir_spellsinger_learn_lore_title_text", "Learn New Lore"), TORTextHelper.GetText("eonir_spellsinger_learn_lore_description_text", "Select a new lore to learn ( maximum 3)"), list, true, 1, 1, TORTextHelper.GetText("eonir_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("eonir_inquiry_cancel_text", "Cancel"), SelectLore, null, "", true);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);

            void SelectLore(List<InquiryElement> inquiryElements)
            {
                var newlore = (LoreObject)inquiryElements[0].Identifier;

                Hero.MainHero.AddKnownLore(newlore.ID);
                Hero.MainHero.AddCultureSpecificCustomResource(-500);
            }

        }
        void RefillVillages()
        {
            foreach (var village in Settlement.All.WhereQ(x => x.IsVillage && x.Culture.StringId == TORConstants.Cultures.EONIR))
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
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 200f);
            _latestEnvoyActionsPerformed.AddOrReplace("troop_refill", CampaignTime.Now.ToDays);
        }

        //why are you here

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere", "spellsinger_envoy_whyareyouhere", "envoy_spellsinger_wayh_reaction",
            TORTextHelper.GetText("eonir_spellsinger_why_here_intro_text", "I am representing a coven of Spellsingers dedicated to the defense of Laurelorn. While the Council is busy with politics, the Faniour, the forest born elves, are endagered by all the threads of the forest."), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_displeased", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_spellsinger_why_here_displeased_text", "The forest is beset by destructive beasts and men alike, what then, are you protecting?"), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_undecided", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_spellsinger_why_here_undecided_text", "What matters can the Council solve for you? What can you give me in turn?"), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_agreement", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_spellsinger_why_here_agreement_text", "The Faniour aswell as the Touriour follow the same people. Your matters, are my matters."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_spellsinger_why_here_explain_text", "I am not a man of politics. I am here to stand our case. I know that seemingly the Council, sometimes forgets about us. Thats why I am here, and standing for the forest people. I know that nothing works here, without giving something in return."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_3", "spellsinger_envoy_whyareyouhere_3", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_spellsinger_why_here_services_text", "Help us with your political power, and I will try to make it worth. I can assist you with some magic, like using the world roots of the Asrai, or can make your influence provide you power over the council. Apart, I can see what i can do for teaching you some magic"), () => IsSpellsingerEnvoy(), null, 200);


        campaignGameStarter.AddDialogLine("back_to_main_hub_spellsinger", "back_to_main_hub_spellsinger", "spellsinger_envoy_main_hub",
            TORTextHelper.GetText("eonir_envoy_anything_else_text", "Is there something else I could do for you?"), () => IsSpellsingerEnvoy(), null, 200);



        bool IsSpellsingerEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null && partner.IsHero) return partner.HeroObject.HasAttribute("SpellsingerEnvoy");

            return false;
        }
    }


    private bool EnoughTimePassedSinceLastEvent(string id, int days)
    {
        if (!_latestEnvoyActionsPerformed.ContainsKey(id))
        {
            return true;
        }

        var timestamp = _latestEnvoyActionsPerformed[id];

        return timestamp + days < CampaignTime.Now.ToDays;
    }


    private void AddEmpireEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", TORTextHelper.GetText("eonir_envoy_foreign_text", "You are not part of these people, begone."),
            () => EonirEnvoyDialogCondition() && Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR, null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            TORTextHelper.GetText("eonir_envoy_low_renown_text", "You do not have the privilege to serve the council. You are of no use. (Low Renown)."), () => EonirEnvoyDialogCondition() && IsEmpireEnvoy() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_empire", "start", "empire_envoy_main_hub", TORTextHelper.GetText("eonir_empire_intro_text", "Is there some way I can be of assistance?"),
            () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_prestige_to_favour", "empire_envoy_main_hub", "empire_envoy_prestige_to_favour",
            TORTextHelper.GetText("eonir_empire_prestige_to_favor_ask_text", "I bring quality goods to trade and wish to build my reputation amongst the High Council. Are you interested?"), () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("Prestige") > 3, null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_favour_to_prestige", "empire_envoy_main_hub", "empire_envoy_favour_to_prestige",
            TORTextHelper.GetText("eonir_empire_favor_to_prestige_ask_text", "I find myself in need of quality goods to trade amongst the nobles of the Empire. Can you supply them?"), () => IsEmpireEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_empire_peace", "empire_envoy_main_hub", "empire_envoy_force_peace",
            TORTextHelper.GetText("eonir_empire_peace_ask_text", "Our people need to make peace. What does it take to stop the war?"), () => IsEmpireEnvoy() && AllEmpireFactionsAtWar().Count > 0, null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_whyareyouhere", "empire_envoy_main_hub", "empire_envoy_whyareyouhere", TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"),
            () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_close", "empire_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That's all thank you."),
            () => IsEmpireEnvoy(), null, 200);


        //force peace

        campaignGameStarter.AddDialogLine("empire_envoy_force_peace", "empire_envoy_force_peace", "empire_envoy_force_peace_choice",
            TORTextHelper.GetText("eonir_empire_peace_offer_text", "The Empire and the Council should make peace. Your people, neither ours will do this without hesitantion ({PEACE_COSTS}{EONIR_FAVOR})"), () => IsEmpireEnvoy() && EnoughTimePassedSinceLastEvent("force_peace", 10), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_force_peace_failed", "empire_envoy_force_peace", "empire_envoy_force_peace_failed_choice",
            TORTextHelper.GetText("eonir_empire_peace_cooldown_text", "My political power is limited. We became too demanding, you should ask another time"), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_failed_choice", "empire_envoy_force_peace_failed_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_1", "empire_envoy_force_peace_choice", "empire_envoy_force_peace_choice_result",
            TORTextHelper.GetText("eonir_envoy_lets_do_this_text", "Let's do this."), () => IsEmpireEnvoy() && AllEmpireFactionsAtWar().Count > 0 && peaceCost <= Hero.MainHero.GetCultureSpecificCustomResourceValue(), null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_2", "empire_envoy_force_peace_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_force_peace_choice_result", "empire_envoy_force_peace_choice_result", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_we_will_see_text", "We will see what we can do."), () => IsEmpireEnvoy(), ForcePeacePrompt, 200);


        List<Kingdom> AllEmpireFactionsAtWar()
        {
            var laurelorn = Campaign.Current.Kingdoms.FirstOrDefault(x => x.StringId == "laurelorn");
            var allElectorStatesAtWar = Campaign.Current.Kingdoms.WhereQ(x => !x.IsEliminated && x.Culture.StringId == TORConstants.Cultures.EMPIRE && (x.IsAtWarWith(laurelorn) || Hero.MainHero.IsKingdomLeader && x.IsAtWarWith(Hero.MainHero.Clan.Kingdom))).ToList();

            return allElectorStatesAtWar;
        }

        void ForcePeacePrompt()
        {
            List<InquiryElement> list = [];

            var laurelorn = Campaign.Current.Kingdoms.FirstOrDefault(x => x.StringId == "laurelorn");

            var allElectorStatesAtWar = AllEmpireFactionsAtWar();

            foreach (var kingdom in allElectorStatesAtWar)
            {
                list.Add(new InquiryElement(kingdom, kingdom.EncyclopediaTitle.ToString(), null, true, TORTextHelper.GetText("eonir_empire_peace_with_hint_text", "Force Peace with")));
            }

            if (list.IsEmpty()) return;

            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("eonir_empire_peace_title_text", "Force Peace"), TORTextHelper.GetText("eonir_empire_peace_description_text", "Force an empire state to be in peace with the eonir"), list, true, 1, 1, TORTextHelper.GetText("eonir_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("eonir_inquiry_cancel_text", "Cancel"), ForcePeace, null, "", true);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);


            void ForcePeace(List<InquiryElement> inquiryElements)
            {
                var kingdom = (Kingdom)inquiryElements[0].Identifier;
                if (Hero.MainHero.IsKingdomLeader)
                {
                    MakePeaceAction.Apply(kingdom, Hero.MainHero.Clan.Kingdom);
                }
                else
                {
                    MakePeaceAction.Apply(kingdom, laurelorn);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(-peaceCost);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, peaceCost);
                _latestEnvoyActionsPerformed.AddOrReplace("force_peace", CampaignTime.Now.ToDays);
            }
        }


        //Exchange all Prestige to Council Favor

        campaignGameStarter.AddDialogLine("empire_envoy_prestige_to_favour", "empire_envoy_prestige_to_favour", "empire_envoy_prestige_to_favour_choice",
            TORTextHelper.GetText("eonir_empire_prestige_to_favor_offer_text", "Obviously your offering the empire can benefit the Council.(Exchange {ORIGINAL_PRESTIGE} to {RETURN_FAVOR}{EONIR_FAVOR}) "), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_prestige_to_favour_choice_1", "empire_envoy_prestige_to_favour_choice", "empire_envoy_prestige_to_favour_result",
            TORTextHelper.GetText("eonir_envoy_lets_do_this_text", "Let's do this."), () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("Prestige") > 3, null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_prestige_to_favour_choice_2", "empire_envoy_prestige_to_favour_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_prestige_to_favour_result", "empire_envoy_prestige_to_favour_result", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_empire_prestige_to_favor_result_text", "I am glad to make businesses with you(Exchanged {ORIGINAL_PRESTIGE} Prestige to {RETURN_FAVOR}{EONIR_FAVOR})"), () => IsEmpireEnvoy(), ExchangePrestigeToFavor, 200);

        void ExchangePrestigeToFavor()
        {
            var prestige = Hero.MainHero.GetCustomResourceValue("Prestige");
            Hero.MainHero.AddCultureSpecificCustomResource(empireCalculatedExchangeBack);

            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, empireCalculatedExchangeBack);

            Hero.MainHero.AddCustomResource("Prestige", -prestige);
        }

        //Favor to Prestige

        campaignGameStarter.AddDialogLine("empire_envoy_favour_to_prestige", "empire_envoy_favour_to_prestige", "empire_envoy_favour_to_prestige_choice",
            TORTextHelper.GetText("eonir_empire_favor_to_prestige_offer_text", "The Empires ambitions need to way in the Council. (gain for 50 Council Favour 30 Prestige)"), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_favour_to_prestige_choice_1", "empire_envoy_favour_to_prestige_choice", "empire_envoy_favour_to_prestige_result",
            TORTextHelper.GetText("eonir_envoy_lets_do_this_text", "Let's do this."), () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("CouncilFavor") > 50, null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_favour_to_prestige_choice_2", "empire_envoy_favour_to_prestige_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_favour_to_prestige_result", "empire_envoy_favour_to_prestige_result", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_empire_glad_to_do_business_text", "I am glad to make businesses with you."), () => IsEmpireEnvoy(), ExchangeFavorToPrestige, 200);



        // why are you here?

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere", "empire_envoy_whyareyouhere", "envoy_empire_wayh_reaction",
            TORTextHelper.GetText("eonir_empire_why_here_intro_text", "As an envoy of Graf Boris Todbringer I represent the interests of Middenland, and to a minor extent that of the Empire as a whole. We wish to maintain peaceful relations with Eonir, built on trust, trade and mutual respect so that we may all benefit."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_displeased", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_empire_why_here_displeased_text", "You encroach upon our lands without heeding what is sacred, yet talk to us about respect? You do not belong here."), () => IsEmpireEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_undecided", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_empire_why_here_undecided_text", "It benefits neither of us to make enemies when they aren't needed, trade and peace can only benefit both our peoples."), () => IsEmpireEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_agreement", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_empire_why_here_agreement_text", "The problems of the empire are shared with the Eonir. We need to fight together side on side, all of our common enemies."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere_2", "empire_envoy_whyareyouhere_2", "empire_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_empire_why_here_explain_text", "We seek only to cooperate, to the mutual benefit of all involved. Conflict between our peoples serves to aid none but our enemies, surely you can agree with me on this."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere_3", "empire_envoy_whyareyouhere_3", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_empire_why_here_services_text", "My word carries weight, as does that of our Graf. I can ensure a state of peace and profitable trade between our nations, we may even be able to provide mercenaries should your lands ever be threatened. ...Think on it, we both have much to gain."), () => IsEmpireEnvoy(), null, 200);

        void ExchangeFavorToPrestige()
        {
            Hero.MainHero.AddCustomResource("CouncilFavor", -50);
            Hero.MainHero.AddCustomResource("Prestige", 30);
        }

        void calculateCost(float prestige)
        {
            empireCalculatedExchangeBack = (int)(prestige * (1f / 2) + prestige * (1f / 3 * (Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 300f)));
            GameTexts.SetVariable("ORIGINAL_PRESTIGE", prestige);
            GameTexts.SetVariable("RETURN_FAVOR", empireCalculatedExchangeBack);
            GameTexts.SetVariable("PEACE_COSTS", peaceCost);
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
            TORTextHelper.GetText("eonir_empire_anything_else_text", "Is there another way I can be of service?"), () => IsEmpireEnvoy(), null, 200);

    }



    private void AddDruchiiEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", TORTextHelper.GetText("eonir_envoy_foreign_text", "You are not part of these people, begone."),
            () => EonirEnvoyDialogCondition() && Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR, null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            TORTextHelper.GetText("eonir_envoy_low_renown_text", "You do not have the previleg to serve the council. You are of no use. (Low Renown)."), () => EonirEnvoyDialogCondition() && IsDruchiiEnvoy() && !HasRenown2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_druchii", "start", "druchii_envoy_main_hub",
            TORTextHelper.GetText("eonir_druchii_intro_text", "I am a servant of the Witchking, what can I do for you?"), () => EonirEnvoyDialogCondition() && IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_force_war", "druchii_envoy_main_hub", "druchii_envoy_force_war",
            TORTextHelper.GetText("eonir_druchii_force_war_ask_text", "We are surrounded by enemies, we seek your aid to sow chaos within their ranks to weaken them."), () => IsDruchiiEnvoy(), null,
            200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_prisoners", "druchii_envoy_main_hub", "druchii_envoy_prisoners",
            TORTextHelper.GetText("eonir_druchii_prisoners_ask_text", "I have a fine stock of prisoners to sell, should you be interested."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_slaver_tide", "druchii_envoy_main_hub", "druchii_envoy_slaver_tide",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_ask_text", "I have some lucrative raiding targets for you."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_whyareyouhere", "druchii_envoy_main_hub", "druchii_envoy_whyareyouhere", TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"),
            () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_close", "druchii_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That's all thank you."),
            () => IsDruchiiEnvoy(), null, 200);



        //force war
        campaignGameStarter.AddDialogLine("druchii_envoy_force_war", "druchii_envoy_force_war", "druchii_envoy_force_war_choice",
            TORTextHelper.GetText("eonir_druchii_force_war_offer_text", "A Khainite dagger can make all the difference, if it finds the right throat at the wrong time. Wars have started over less. Upon whose throat should our daggers fall? (Declare war between 2 factions ({FORCEWAR_PRICE}{EONIR_FAVOR})"), () => IsDruchiiEnvoy() && EnoughTimePassedSinceLastEvent("force_war", 20), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_force_war_failed", "druchii_envoy_force_war", "druchii_envoy_force_war_failed_choice",
            TORTextHelper.GetText("eonir_druchii_force_war_cooldown_text", "The Witchking would not allow that. We should wait longer with such a request."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_failed_choice", "druchii_envoy_force_war_failed_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_1", "druchii_envoy_force_war_choice", "druchii_envoy_force_war_choice_result",
            TORTextHelper.GetText("eonir_envoy_lets_do_this_text", "Let's do this."), () => IsDruchiiEnvoy() && (druchii_force_war_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm) <= Hero.MainHero.GetCultureSpecificCustomResourceValue()), null, 200);
        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_2", "druchii_envoy_force_war_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_force_war_choice_result", "druchii_envoy_force_war_choice_result", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_we_will_see_text", "We will see what we can do."), () => IsDruchiiEnvoy(), ForceWarPrompt, 200);

        //exchange prisoners

        campaignGameStarter.AddDialogLine("druchii_envoy_prisoners", "druchii_envoy_prisoners", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_druchii_prisoners_offer_text", "What a promising trade. This will be credited for your next negotiation with the Witch king I believe the Council will like this."), () => IsDruchiiEnvoy(), ExchangePrisoners, 200);


        void ExchangePrisoners()
        {
            _isDruchiiEnvoyTrade = true;

            PartyScreenHelper.OpenScreenAsDonatePrisoners();
        }

        void ForceWarPrompt()
        {
            List<InquiryElement> list = [];

            var allKingdoms = Campaign.Current.Kingdoms.WhereQ(x => !x.IsEliminated).ToList();

            foreach (var kingdom in allKingdoms)
            {
                list.Add(new InquiryElement(kingdom, kingdom.EncyclopediaTitle.ToString(), null, true, TORTextHelper.GetText("eonir_druchii_force_war_hint_text", "Force war between two kingdoms")));
            }

            if (list.IsEmpty()) return;

            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("eonir_druchii_force_war_title_text", "Force War"), TORTextHelper.GetText("eonir_druchii_force_war_description_text", "Select 2 Factions war will emerge between."), list, true, 2, 2, TORTextHelper.GetText("eonir_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("eonir_inquiry_cancel_text", "Cancel"), ForceWar, null, "", true);
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
                    DeclareWarAction.ApplyByDefault(kingdom2, kingdom1);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(-(druchii_force_war_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)));
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, druchii_force_war_price_base);
                _latestEnvoyActionsPerformed.AddOrReplace("force_war", CampaignTime.Now.ToDays);

            }
        }


        //slaver tide

        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide_choice",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_offer_text", "...This will cost you influence with the council, but we are always in need of more slaves. where do you suggest our Black Arks to anchor?({SLAVERTIDE_PRICE}{EONIR_FAVOR})"), () => IsDruchiiEnvoy() && EnoughTimePassedSinceLastEvent("slaver_tide", 20), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide_failed", "druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide_failed_choice",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_cooldown_text", "The Witchking would not allow that. We should wait longer with such a request."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_slaver_tide_failed_choice", "druchii_envoy_slaver_tide_failed_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsDruchiiEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_1", "druchii_envoy_slaver_tide_choice", "druchii_envoy_slaver_tide_choice_result",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_accept_text", "Let's do this({SLAVERTIDE_PRICE}{EONIR_FAVOR})."), () => IsDruchiiEnvoy() && (druchii_slaver_tide_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)) <= Hero.MainHero.GetCultureSpecificCustomResourceValue(), SlaverTidePrompt, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_2", "druchii_envoy_slaver_tide_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide_choice_result", "druchii_envoy_slaver_tide_choice_result", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_druchii_could_see_text", "I could see what I can do."), () => IsDruchiiEnvoy(), SlaverTidePrompt, 200);


        void SlaverTidePrompt()
        {
            List<InquiryElement> list = [];
            var coastalKingdoms = Campaign.Current.Kingdoms.WhereQ(x => !x.IsEliminated && x.IsCoastalKingdom()).ToList();

            foreach (var kingdom in coastalKingdoms)
            {
                list.Add(new InquiryElement(kingdom, kingdom.Name.ToString(), null, true, ""));
            }

            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("eonir_druchii_slaver_tide_title_text", "Choose a kingdom to be swarmed"), TORTextHelper.GetText("eonir_druchii_slaver_tide_description_text", "Select a kingdom being swarmed by druchii slaver troops."), list, true, 1, 1, TORTextHelper.GetText("eonir_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("eonir_inquiry_cancel_text", "Cancel"), SwarmKingdomWithDruchii, null, "", true);

            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            void SwarmKingdomWithDruchii(List<InquiryElement> inquiryElements)
            {
                var kingdom = (Kingdom)inquiryElements[0].Identifier;

                var slaverBay = Campaign.Current.Settlements.FirstOrDefaultQ(x => x.StringId == "darkelf_camp_01");

                var slaverBaySettlementComponent = (SlaverCampComponent)slaverBay.SettlementComponent;

                int maxPartiesToSpawn = 6;
                int partiesSpawned = 0;

                if (slaverBaySettlementComponent != null)
                {
                    foreach (var settlement in kingdom.Settlements)
                    {
                        if (partiesSpawned >= maxPartiesToSpawn) break;

                        if (!settlement.IsVillage || settlement.IsRaided || settlement.IsUnderRaid) continue;

                        if (MBRandom.RandomFloat < 0.25f)
                        {
                            slaverBaySettlementComponent.SpawnNewParty(out var druchiiParty1, settlement);
                            slaverBaySettlementComponent.SpawnNewParty(out var druchiiParty2, settlement);
                            var ta = druchiiParty1.MemberRoster.CloneRosterData();

                            druchiiParty1.Position = NavigationHelper.FindReachablePointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, 20);
                            druchiiParty2.Position = NavigationHelper.FindReachablePointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, 20);

                            druchiiParty1.MemberRoster.Add(ta);
                            druchiiParty2.MemberRoster.Add(ta);
                            partiesSpawned += 2;
                            continue;
                        }
                        slaverBaySettlementComponent.SpawnNewParty(out var druchiiParty, settlement);
                        partiesSpawned++;
                        druchiiParty.Position = NavigationHelper.FindReachablePointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, 20);
                        var memberRosterCopy = druchiiParty.MemberRoster.CloneRosterData();
                        druchiiParty.MemberRoster.Add(memberRosterCopy);
                    }
                }

                DeclareWarAction.ApplyByDefault(slaverBay.OwnerClan, kingdom);

                Hero.MainHero.AddCultureSpecificCustomResource(-(druchii_slaver_tide_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)));
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, druchii_slaver_tide_price_base);
                _latestEnvoyActionsPerformed.AddOrReplace("slaver_tide", CampaignTime.Now.ToDays);
            }
        }

        // why are you here?
        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere", "druchii_envoy_whyareyouhere", "envoy_druchii_wayh_reaction",
            TORTextHelper.GetText("eonir_druchii_why_here_intro_text", "I speak as an envoy of the rightful ruler of the Asur, and the Black Council. We may not always be aligned in our views but we believe both the Druchii and Eonir have much in common, and much to gain from one another."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_displeased", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_druchii_why_here_displeased_text", "You raid our coasts, enslave our people and yet have the audacity to stand before us, acting as if none of this has taken place!"), () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_undecided", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_druchii_why_here_undecided_text", "I am skeptical, you come here with your own ambitions but to what end? How would hearing you speak now, benefit the Eonir?"), () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_agreement", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_druchii_why_here_agreement_text", "The Asur betrayed us and left us to die, I might not share your every sentiment Druchii, but I know you too suffer from our cousin's boundless arrogance."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere_2", "druchii_envoy_whyareyouhere_2", "druchii_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_druchii_why_here_explain_text", "We are not your enemy, the opposite is true. Few share our bond of suffering at the hands of the Asur, few can understand our mutual plight. There are many ways we can coexist and thrive, together. - Join us, we have been scorned by our kin for far too long and this is the best path for our futures combined."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere_3", "druchii_envoy_whyareyouhere_3", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_druchii_why_here_services_text", "Your seas will be safe from raids and protected from any foe, the darkest desires of your people will be free of judgement and all we seek is influence over your council. - We merely wish to lessen the gap between our peoples so that we may stand against the Asur together, a sure and just cause, do you not agree?"), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("back_to_main_hub_druchii", "back_to_main_hub_druchii", "druchii_envoy_main_hub",
            TORTextHelper.GetText("eonir_druchii_anything_else_text", "Is there something else the Witch king could do for you?"), () => IsDruchiiEnvoy(), null, 200);

        void setDruchiiPrices()
        {
            GameTexts.SetVariable("FORCEWAR_PRICE", druchii_force_war_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm));
            GameTexts.SetVariable("SLAVERTIDE_PRICE", druchii_slaver_tide_price_base - Hero.MainHero.GetSkillValue(DefaultSkills.Charm));
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
        starter.AddDialogLine("envoy_foreign", "start", "close_window", TORTextHelper.GetText("eonir_envoy_foreign_text", "You are not part of these people, begone."),
            () => EonirEnvoyDialogCondition() && Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR, null, 200);

        starter.AddDialogLine("envoy_missRank", "start", "close_window",
            TORTextHelper.GetText("eonir_envoy_low_renown_text", "You do not have the previleg to serve the council. You are of no use. (Low Renown)."), () => EonirEnvoyDialogCondition() && IsAsurianEnvoy() && !HasRenown2(),
            null, 200);


        starter.AddDialogLine("envoy_hub_intro_asur", "start", "asur_envoy_main_hub",
            TORTextHelper.GetText("eonir_asur_intro_text", "Kneel before the envoy of the Asur and representative of the illustrious Pheonix King, Finubar the Seafarer. Speak and do so quickly."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_money", "asur_envoy_main_hub", "asur_envoy_money",
            TORTextHelper.GetText("eonir_asur_money_ask_text", "We desire to expand our trade networks, but lack the funding. Would Ulthuan be willing to invest in this endeavour?"),
            () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_troops", "asur_envoy_main_hub", "asur_envoy_troops", TORTextHelper.GetText("eonir_asur_troops_ask_text", "Eonir is threatened, we need soldiers to defend our homes and people, can you aid us?"),
            () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_diplomacy", "asur_envoy_main_hub", "asur_envoy_diplomacy",
            TORTextHelper.GetText("eonir_asur_diplomacy_ask_text", "We seek the aid of your diplomats, tensions between the kingdoms of man is at a high and we seek to reverse this."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_whyareyouhere", "asur_envoy_main_hub", "asur_envoy_whyareyouhere", TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"), () => IsAsurianEnvoy(),
            null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_close", "asur_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That's all thank you."), () => IsAsurianEnvoy(),
            null, 200);


        //money
        starter.AddDialogLine("asur_envoy_money", "asur_envoy_money", "asur_envoy_money_choice",
            TORTextHelper.GetText("eonir_asur_money_offer_text", "With ease, how much is needed?"), () => IsAsurianEnvoy() && EnoughTimePassedSinceLastEvent("asur_money", 5), null, 200);

        starter.AddDialogLine("asur_envoy_money_failed", "asur_envoy_money", "asur_envoy_money_failed_choice",
            TORTextHelper.GetText("eonir_asur_money_cooldown_text", "We wait for another shipment of the tressure fleet, this might take a few days. Come back"), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_money_failed_choice", "asur_envoy_money_failed_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsAsurianEnvoy(), null, 200);


        starter.AddPlayerLine("asur_envoy_money_choice_1", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN1}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY1}{EONIR_FAVOR}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= asur_favor_price1, () => TransferMoney(1, asur_favor_price1), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_2", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN2}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY2}{EONIR_FAVOR}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= asur_favor_price2, () => TransferMoney(10, asur_favor_price2), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_3", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN3}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY3}{EONIR_FAVOR}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= asur_favor_price3, () => TransferMoney(30, asur_favor_price3), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_quit", "asur_envoy_money_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsAsurianEnvoy(), null, 200);

        //troops

        starter.AddDialogLine("asur_envoy_troops", "asur_envoy_troops", "asur_envoy_troops_choice",
            TORTextHelper.GetText("eonir_asur_troops_offer_text", "There may be some amongst the Asur willing to fight, even if it is for our less civlised kin."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_troops_choice_1", "asur_envoy_troops_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_asur_troops_accept_text", "Sure I accept (150 {EONIR_FAVOR})."), () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 150, ShowTroopSelectionScreen, 200);
        starter.AddPlayerLine("asur_envoy_troops_choice_2", "asur_envoy_troops_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsAsurianEnvoy(), null, 200);

        //diplomacy
        starter.AddDialogLine("asur_envoy_diplomacy", "asur_envoy_diplomacy", "asur_envoy_diplomacy_choice",
            TORTextHelper.GetText("eonir_asur_diplomacy_offer_text", "We can put in a good word for you from one of our many embassies, the word of an Asur has much worth amongst men. With whom do you seek better relations?"), () => IsAsurianEnvoy() && EnoughTimePassedSinceLastEvent("asur_diplomacy", 15), null, 200);

        starter.AddDialogLine("asur_envoy_diplomacy_failed", "asur_envoy_diplomacy", "asur_envoy_diplomacy_failed_choice",
            TORTextHelper.GetText("eonir_asur_diplomacy_cooldown_text", "You should wait a bit, good diplomacy requires patience"), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_diplomacy_failed_choice", "asur_envoy_diplomacy_failed_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsAsurianEnvoy(), null, 200);


        starter.AddPlayerLine("asur_envoy_diplomacy_choice_1", "asur_envoy_diplomacy_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_asur_diplomacy_accept_text", "Sure I accept (400 {EONIR_FAVOR})."), () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 400, AsurDiplomacyPrompt, 200);
        starter.AddPlayerLine("asur_envoy_diplomacy_choice_2", "asur_envoy_diplomacy_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsAsurianEnvoy(), null, 200);


        //why are you here

        starter.AddDialogLine("asur_envoy_whyareyouhere", "asur_envoy_whyareyouhere", "envoy_asur_wayh_reaction",
            TORTextHelper.GetText("eonir_asur_why_here_intro_text", "I am here as a representative of Ulthuan, and to stand as a reminder that the freedom and independence you enjoy is a most gracious gift given by the Pheonix King. A gift that can be rescinded at his discretion."), () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_displeased", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_asur_why_here_displeased_text", "You abandoned us to die, then tried to take our lands by force. We have survived without Ulthuan until now and will continue to do so. Begone, 'kin'."), () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_undecided", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_asur_why_here_undecided_text", "Be mindful, our willingess to negotiate is not a sign that we have forgotten your transgressions, what do you come here to offer us?"), () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_agreement", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_asur_why_here_agreement_text", "We are kin and have been apart for far too long, we seek the help of the Asur and appreciate any that can be offered."), () => IsAsurianEnvoy(), null, 200);

        starter.AddDialogLine("asur_envoy_whyareyouhere_2", "asur_envoy_whyareyouhere_2", "asur_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_asur_why_here_explain_text", "We are willing to offer aid in many forms, as long as it increases our influence within the council."), () => IsAsurianEnvoy(), null, 200);

        starter.AddDialogLine("asur_envoy_whyareyouhere_3", "asur_envoy_whyareyouhere_3", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_asur_why_here_services_text", "Ulthuan has connections across the Old World, should finances, a good word with an enemy or soldiers to defend your forest be needed, we can provide it. For the agreed price, of course."), () => IsAsurianEnvoy(), null, 200);


        starter.AddDialogLine("back_to_main_hub_asur", "back_to_main_hub_asur", "asur_envoy_main_hub",
            TORTextHelper.GetText("eonir_envoy_anything_else_text", "Is there something else I could do for you?"), () => IsAsurianEnvoy(), null, 200);


        void SetupPrices()
        {
            GameTexts.SetVariable("ASUR_MONEYRETURN1", CalculateBasePrice());
            GameTexts.SetVariable("ASUR_MONEYRETURN2", CalculateBasePrice() * 10);
            GameTexts.SetVariable("ASUR_MONEYRETURN3", CalculateBasePrice() * 30);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY1", asur_favor_price1);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY2", asur_favor_price2);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY3", asur_favor_price3);
        }

        int CalculateBasePrice()
        {
            var moneyReturn = 1000;
            var charm = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);
            moneyReturn += (int)(moneyReturn * ((float)charm / 100));
            return moneyReturn;
        }

        void TransferMoney(int factor, int favorPrice)
        {
            var basePrice = CalculateBasePrice();

            var revenue = basePrice * factor;

            Hero.MainHero.ChangeHeroGold(revenue);
            Hero.MainHero.AddCultureSpecificCustomResource(-favorPrice);
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, favorPrice);
            _latestEnvoyActionsPerformed.AddOrReplace("asur_money", CampaignTime.Now.ToDays);
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

            var skillValue = Mathf.Min(Hero.MainHero.GetSkillValue(DefaultSkills.Charm), 300);
            var finished = false;
            var count = 3;
            while (!finished && count < 25)
            {
                if (MBRandom.RandomFloat < ((float)skillValue - 10) / 300)
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
                while (!upgradeFailed)
                {
                    if (troop.UpgradeTargets == null || troop.UpgradeTargets.Length == 0)
                    {
                        if (MBRandom.RandomFloat < ((float)(skillValue - 250) / 300))
                        {
                            troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_he_white_lion_chrace");
                        }
                        break;
                    }

                    if (MBRandom.RandomFloat < ((float)skillValue - 100) / 300)
                    {
                        troop = troop.UpgradeTargets.GetRandomElement();
                    }
                    else
                    {
                        upgradeFailed = true;
                    }
                }

                roster.AddToCounts(troop, 1);
            }

            PartyScreenHelper.OpenScreenAsReceiveTroops(roster, TORTextHelper.GetTextObject("eonir_asur_troops_title_text", "Asur support"), OnscreenClosed);

            void OnscreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
            {
                if (fromCancel) return;

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

                list.Add(new InquiryElement(kingdom, kingdom.EncyclopediaTitle.ToString(), null, true, TORTextHelper.GetText("eonir_asur_diplomacy_hint_text", "Improve relationship")));
            }

            if (list.IsEmpty()) return;

            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("eonir_asur_diplomacy_title_text", "Improve Relationship with one faction"), TORTextHelper.GetText("eonir_asur_diplomacy_description_text", "Choose a faction, the relation of you will improve by 15, and the eonir faction aswell."), list, true, 1, 1, TORTextHelper.GetText("eonir_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("eonir_inquiry_cancel_text", "Cancel"), AddRelationship, null, "", true);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);

            void AddRelationship(List<InquiryElement> inquiryElements)
            {
                var eonirClans = Hero.MainHero.CurrentSettlement.OwnerClan.Kingdom.Clans;

                var kingdom = (Kingdom)inquiryElements[0].Identifier;


                var bonus = Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 20;
                float chance = (float)Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 300;

                foreach (var hero in kingdom.Heroes)
                {
                    foreach (var clan in eonirClans)
                    {
                        if (MBRandom.RandomFloat < chance)
                        {
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, hero, 15 + bonus, false);
                        }
                    }
                }
                Hero.MainHero.AddCultureSpecificCustomResource(-400);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 400);

                _latestEnvoyActionsPerformed.AddOrReplace("asur_diplomacy", CampaignTime.Now.ToDays);
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
                partner.HeroObject.HasAttribute("DruchiiEnvoy") || partner.HeroObject.HasAttribute("SpellsingerEnvoy"))
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
            _druchiiEnvoy.SetName(templateDruchii.GetName(), _druchiiEnvoy.FirstName);
            _druchiiEnvoy.CharacterObject.HiddenInEncyclopedia = true;
            HeroHelper.SpawnHeroForTheFirstTime(_druchiiEnvoy, _torLithanel);
            envoys.Add(_druchiiEnvoy);
        }

        if (templateAsur != null)
        {
            _asurEnvoy = HeroCreator.CreateSpecialHero(templateAsur, _torLithanel, null, null, 50);
            _asurEnvoy.SupporterOf = _torLithanel.OwnerClan;
            _asurEnvoy.SetName(templateAsur.GetName(), _asurEnvoy.FirstName);
            _asurEnvoy.CharacterObject.HiddenInEncyclopedia = true;
            HeroHelper.SpawnHeroForTheFirstTime(_asurEnvoy, _torLithanel);
            envoys.Add(_asurEnvoy);
        }

        if (templateEmpire != null)
        {
            _empireEnvoy = HeroCreator.CreateSpecialHero(templateEmpire, _torLithanel, null, null, 50);
            _empireEnvoy.SupporterOf = _torLithanel.OwnerClan;
            _empireEnvoy.SetName(templateEmpire.GetName(), _empireEnvoy.FirstName);
            _empireEnvoy.CharacterObject.HiddenInEncyclopedia = true;
            HeroHelper.SpawnHeroForTheFirstTime(_empireEnvoy, _torLithanel);
            envoys.Add(_empireEnvoy);
        }

        if (templateSpellsinger != null)
        {//Forest Guardian in-game
            _spellsingerEnvoy = HeroCreator.CreateSpecialHero(templateSpellsinger, _torLithanel, null, null, 50);
            _spellsingerEnvoy.SupporterOf = _torLithanel.OwnerClan;
            _spellsingerEnvoy.SetName(templateSpellsinger.GetName(), _spellsingerEnvoy.FirstName);
            _spellsingerEnvoy.CharacterObject.HiddenInEncyclopedia = true;
            HeroHelper.SpawnHeroForTheFirstTime(_spellsingerEnvoy, _torLithanel);
            envoys.Add(_spellsingerEnvoy);
        }
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_latestEnvoyActionsPerformed", ref _latestEnvoyActionsPerformed);
    }
}