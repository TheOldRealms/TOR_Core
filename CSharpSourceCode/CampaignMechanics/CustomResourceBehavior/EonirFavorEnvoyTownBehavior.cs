using Helpers;
using SandBox;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
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


    private int _asurFavorPrice1 = 100;
    private int _asurFavorPrice2 = 500;
    private int _asurFavorPrice3 = 1000;

    private bool _isDruchiiEnvoyTrade;
    private int _druchiiForceWarPriceBase = 750;
    private int _druchiiSlaverTidePriceBase = 1000;

    private int _empireFavorConvertedFromPrestige = 0;
    private int _peaceCost = 750;

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
        CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonersSold);
    }

    private void OnPrisonersSold(MobileParty sellingParty, FlattenedTroopRoster flatRosterSold, Settlement receivingSettlement)
    {
        if (_isDruchiiEnvoyTrade)
        {
            foreach (var element in flatRosterSold)
            {
                if (!element.Troop.IsHero)
                {
                    Hero.MainHero.AddCultureSpecificCustomResource(element.Troop.Tier);
                    receivingSettlement.Party.PrisonRoster.RemoveTroop(element.Troop);
                }
                if (element.Troop.IsHero)//trade heroes for council favour, but in turn they are immediately "released" by the envoy and will cycle back into the war - goal is that anything ransomed to the envoy is worthwhile and the player doesn't need to micro the action to avoid direct "losses"
                {
                    Hero.MainHero.AddCultureSpecificCustomResource((int)(element.Troop.HeroObject.Level/5 - 1));
                    EndCaptivityAction.ApplyByRansom(element.Troop.HeroObject, null);
                }
            }
        }

    }

    private void OnGameMenuOpened(MenuCallbackArgs obj)
    {
        EnforceEnvoyLocation();
    }

    private void OnBeforeMissionStart()
    {
        EnforceEnvoyLocation();
    }

    private void OnSessionLaunched(CampaignGameStarter obj)
    {
        SetTextVariables();
        AddDruchiiEnvoyDialogLines(obj);
        AddAsurEnvoyDialogLines(obj);
        AddEmpireEnvoyDialogLines(obj);
        AddSpellsingerEnvoyDialogLines(obj);

        // Populate envoys when loading a save game
        if (_torLithanel == null)
        {
            _torLithanel = Campaign.Current.Settlements.FirstOrDefault(x => x.IsTorLithanel());
        }
        PopulateEnvoys();
    }

    private void SetTextVariables()
    {
        GameTexts.SetVariable("FAVOR_ICON", CustomResourceManager.GetResourceObject("CouncilFavor").GetCustomResourceIconAsText(false));
        //PRESTIGE_ICON is already set in PrestigeNobleTownBehavior - at a later point these should probably be regrouped into a single location for defining the global variables. It's also set in MasterEngineer behavior as well iirc - possibly elsewhere I haven't seen.

        GameTexts.SetVariable("PEACE_COST", _peaceCost);
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


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_spellsinger_magic", "spellsinger_envoy_main_hub", "back_to_main_hub_spellsinger",  TORTextHelper.GetText("tor_spellsinger_envoy_main_hub_spellsinger_magic","I wish to learn magic from you."), () => MobileParty.MainParty.HasSpellCasterMember() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR && IsSpellsingerEnvoy(), openbookconsequence, 200, null);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_spellsinger_lores", "spellsinger_envoy_main_hub", "spellsinger_envoy_spellsinger_lores", TORTextHelper.GetText("spellsinger_envoy_main_hub_spellsinger_lores","Teach me about the lores of magic."), () => IsSpellsingerEnvoy() && CanGreylordLearnMoreLores(), null, 200, null);


        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_whyareyouhere", "spellsinger_envoy_main_hub", "spellsinger_envoy_whyareyouhere",
            TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_main_hub_close", "spellsinger_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That is all thank you."),
            () => IsSpellsingerEnvoy(), null, 200);

        //travel info

        campaignGameStarter.AddDialogLine("spellsinger_envoy_world_roots", "spellsinger_envoy_world_roots", "spellsinger_envoy_world_roots_choice",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_price_text", "I can, but for a price. Help our people, they need more power and then I will allow you to travel the worldroot."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("spellsinger_envoy_world_roots_choice_1", "spellsinger_envoy_world_roots_choice", "spellsinger_envoy_world_roots_results",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_accept_text", "Of course, I am willing to help. Now as my guide, please tell me how I can travel along these ancient pathways?"), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_world_roots_results", "spellsinger_envoy_world_roots_results", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_spellsinger_world_roots_explain_text", "There is an entranceway to the Worldroots here in Laurelorn."), () => IsSpellsingerEnvoy(), null, 200);

        //refill

        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill_choice",
            TORTextHelper.GetText("eonir_spellsinger_troop_refill_offer_text", "There are many who live as nomads, far from our villages and deep within the woods. It will take time for any messages to reach them but it can be done. Is this what you desire?"), () => IsSpellsingerEnvoy() && EnoughTimePassedSinceLastEvent("troop_refill", 10), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill", "spellsinger_envoy_troop_refill_fail_choice",
            TORTextHelper.GetText("eonir_spellsinger_troop_refill_cooldown_text", "We recently called for the forestborn aid. We should wait longer for another call"), () => (IsSpellsingerEnvoy() && !EnoughTimePassedSinceLastEvent("troop_refill", 10)), null, 200);

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
            TORTextHelper.GetText("eonir_spellsinger_learn_lore_offer_text", "I can teach you, but as much as you are ready to do so, I need your word in the Council."), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("spellsinger_envoy_spellsinger_lores_choice_1", "spellsinger_envoy_spellsinger_lores_choice", "spellsinger_envoy_spellsinger_lores_result",
            TORTextHelper.GetText("eonir_spellsinger_learn_lore_accept_text", "It should not be for your disadvantage"), () => IsSpellsingerEnvoy() && 500 <= Hero.MainHero.GetCultureSpecificCustomResourceValue() && CanGreylordLearnMoreLores(), LearnNewLoresPrompt, 200);
        campaignGameStarter.AddPlayerLine("spellsinger_envoy_spellsinger_lores_choice_2", "spellsinger_envoy_spellsinger_lores_choice", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_spellsinger_lores_result", "spellsinger_envoy_spellsinger_lores_result", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_envoy_will_see_what_can_do_text", "I will see what I can do."), () => IsSpellsingerEnvoy(), null, 200);

        bool CanGreylordLearnMoreLores()
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
            TORTextHelper.GetText("eonir_spellsinger_why_here_intro_text", "I am representing a coven of Spellsingers dedicated to the defense of Laurelorn."), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_displeased", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_spellsinger_why_here_displeased_text", "The forest is beset by destructive beasts and men alike, what then, are you protecting?"), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_undecided", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_spellsinger_why_here_undecided_text", "What matters can the Council solve for you? What can you give me in turn?"), () => IsSpellsingerEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_spellsinger_wayh_reaction_agreement", "envoy_spellsinger_wayh_reaction", "spellsinger_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_spellsinger_why_here_agreement_text", "The Faniour aswell as the Touriour follow the same people. Your matters, are my matters."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_2", "spellsinger_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_spellsinger_why_here_explain_text", "I am not a man of politics. I am here to stand our case."), () => IsSpellsingerEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("spellsinger_envoy_whyareyouhere_3", "spellsinger_envoy_whyareyouhere_3", "back_to_main_hub_spellsinger",
            TORTextHelper.GetText("eonir_spellsinger_why_here_services_text", "Help us with your political power, and I will try to make it worth."), () => IsSpellsingerEnvoy(), null, 200);


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
            TORTextHelper.GetText("eonir_envoy_low_clan_tier_text", "You do not have the privilege to serve the council. You are of no use. (Low Clan Tier)."), () => EonirEnvoyDialogCondition() && IsEmpireEnvoy() && !HasClanTier2(),
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

        campaignGameStarter.AddPlayerLine("empire_envoy_main_hub_close", "empire_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That is all thank you."),
            () => IsEmpireEnvoy(), null, 200);


        //force peace

        campaignGameStarter.AddDialogLine("empire_envoy_force_peace", "empire_envoy_force_peace", "empire_envoy_force_peace_choice",
            TORTextHelper.GetText("eonir_empire_peace_offer_text", "The Empire and the Council should make peace. Your people, nor ours, will do this without hesitation. Let us remove that barrier. ({PEACE_COST}{FAVOR_ICON})"), () => IsEmpireEnvoy() && EnoughTimePassedSinceLastEvent("force_peace", 10), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_force_peace_failed", "empire_envoy_force_peace", "empire_envoy_force_peace_failed_choice",
            TORTextHelper.GetText("eonir_empire_peace_cooldown_text", "My political power is limited. We became too demanding, you should ask another time"), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_failed_choice", "empire_envoy_force_peace_failed_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_force_peace_choice_1", "empire_envoy_force_peace_choice", "empire_envoy_force_peace_choice_result",
            TORTextHelper.GetText("eonir_envoy_lets_do_this_text", "Let us do this."), () => IsEmpireEnvoy() && AllEmpireFactionsAtWar().Count > 0 && _peaceCost <= Hero.MainHero.GetCultureSpecificCustomResourceValue(), null, 200);
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

                Hero.MainHero.AddCultureSpecificCustomResource(-_peaceCost);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, _peaceCost);
                _latestEnvoyActionsPerformed.AddOrReplace("force_peace", CampaignTime.Now.ToDays);
            }
        }


        //Exchange all Prestige to Council Favor

        campaignGameStarter.AddDialogLine("empire_envoy_prestige_to_favour", "empire_envoy_prestige_to_favour", "empire_envoy_prestige_to_favour_choice",
            TORTextHelper.GetText("eonir_empire_prestige_to_favor_offer_text", "Obviously your offering to the empire can benefit the Council."), () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("Prestige") > 0, null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_prestige_to_favour_choice_1", "empire_envoy_prestige_to_favour_choice", "empire_envoy_prestige_to_favour_result",
            TORTextHelper.GetText("eonir_empire_exchange_prestige_to_favor", "Let us do this. (Receive {CONVERTED_FAVOR} {FAVOR_ICON})"), () => IsEmpireEnvoy() && CalculateFavorConversion(Hero.MainHero.GetCustomResourceValue("Prestige")), null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_prestige_to_favour_choice_2", "empire_envoy_prestige_to_favour_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_prestige_to_favour_result", "empire_envoy_prestige_to_favour_result", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_empire_prestige_to_favor_result_text", "The trade has been completed."), () => IsEmpireEnvoy(), ExchangePrestigeToFavor, 200);
        
        bool CalculateFavorConversion(float prestige)
        {
            _empireFavorConvertedFromPrestige = (int)(prestige * (1f / 2) + prestige * (1f / 3 * (Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 300f)));
            GameTexts.SetVariable("CONVERTED_FAVOR", _empireFavorConvertedFromPrestige);

            if (_empireFavorConvertedFromPrestige > 0) return true;

            return false;
        }

        void ExchangePrestigeToFavor()
        {
            var prestige = Hero.MainHero.GetCustomResourceValue("Prestige");
            Hero.MainHero.AddCultureSpecificCustomResource(_empireFavorConvertedFromPrestige);

            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, _empireFavorConvertedFromPrestige);

            Hero.MainHero.AddCustomResource("Prestige", -prestige);
        }

        //Favor to Prestige

        campaignGameStarter.AddDialogLine("empire_envoy_favour_to_prestige", "empire_envoy_favour_to_prestige", "empire_envoy_favour_to_prestige_choice",
            TORTextHelper.GetText("eonir_empire_favor_to_prestige_offer_text", "I can supply you with quality goods."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("empire_envoy_favour_to_prestige_choice_1", "empire_envoy_favour_to_prestige_choice", "empire_envoy_favour_to_prestige_result",
            TORTextHelper.GetText("eonir_empire_exchange_favor_to_prestige", "Let us do this. (Exchange 50 {FAVOR_ICON} for 30 {PRESTIGE_ICON})"), () => IsEmpireEnvoy() && Hero.MainHero.GetCustomResourceValue("CouncilFavor") >= 50, null, 200);
        campaignGameStarter.AddPlayerLine("empire_envoy_favour_to_prestige_choice_2", "empire_envoy_favour_to_prestige_choice", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_favour_to_prestige_result", "empire_envoy_favour_to_prestige_result", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_empire_glad_to_do_business_text", "Glad to do business with you."), () => IsEmpireEnvoy(), ExchangeFavorToPrestige, 200);
        
        void ExchangeFavorToPrestige()
        {
            Hero.MainHero.AddCustomResource("CouncilFavor", -50);//this doesn't make use of a text variable and so the text is sensitive to this value changing
            Hero.MainHero.AddCustomResource("Prestige", 30);
        }


        // why are you here?

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere", "empire_envoy_whyareyouhere", "envoy_empire_wayh_reaction",
            TORTextHelper.GetText("eonir_empire_why_here_intro_text", "I represent the Empire interests here."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_displeased", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_empire_why_here_displeased_text", "You have not proven useful."), () => IsEmpireEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_undecided", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_empire_why_here_undecided_text", "I am still deciding your worth."), () => IsEmpireEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_empire_wayh_reaction_agreement", "envoy_empire_wayh_reaction", "empire_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_empire_why_here_agreement_text", "We have a productive relationship."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere_2", "empire_envoy_whyareyouhere_2", "empire_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_empire_why_here_explain_text", "I facilitate relations between the Empire and the Eonir."), () => IsEmpireEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("empire_envoy_whyareyouhere_3", "empire_envoy_whyareyouhere_3", "back_to_main_hub_empire",
            TORTextHelper.GetText("eonir_empire_why_here_services_text", "I can help with trade and diplomacy."), () => IsEmpireEnvoy(), null, 200);



        bool IsEmpireEnvoy()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null && partner.IsHero) return partner.HeroObject.HasAttribute("EmpireEnvoy");

            return false;
        }

        //back to hub

        campaignGameStarter.AddDialogLine("back_to_main_hub_empire", "back_to_main_hub_empire", "empire_envoy_main_hub",
            TORTextHelper.GetText("eonir_empire_anything_else_text", "Is there anything else I can help with?"), () => IsEmpireEnvoy(), null, 200);

    }



    private void AddDruchiiEnvoyDialogLines(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddDialogLine("envoy_foreign", "start", "close_window", TORTextHelper.GetText("eonir_envoy_foreign_text", "You are not part of these people, begone."),
            () => EonirEnvoyDialogCondition() && Hero.MainHero.Culture.StringId != TORConstants.Cultures.EONIR, null, 200);

        campaignGameStarter.AddDialogLine("envoy_missRank", "start", "close_window",
            TORTextHelper.GetText("eonir_envoy_low_clan_tier_text", "You do not have the privilege to serve the council. You are of no use. (Low Clan Tier)."), () => EonirEnvoyDialogCondition() && IsDruchiiEnvoy() && !HasClanTier2(),
            null, 200);


        campaignGameStarter.AddDialogLine("envoy_hub_intro_druchii", "start", "druchii_envoy_main_hub",
            TORTextHelper.GetText("eonir_druchii_intro_text", "Greetings. What brings you to speak with me?"), () => EonirEnvoyDialogCondition() && IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_force_war", "druchii_envoy_main_hub", "druchii_envoy_force_war",
            TORTextHelper.GetText("eonir_druchii_force_war_ask_text", "I wish to discuss matters of war."), () => IsDruchiiEnvoy(), null,
            200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_prisoners", "druchii_envoy_main_hub", "druchii_envoy_prisoners",
            TORTextHelper.GetText("eonir_druchii_prisoners_ask_text", "I have prisoners that may interest you."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_slaver_tide", "druchii_envoy_main_hub", "druchii_envoy_slaver_tide",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_ask_text", "Tell me about the slaver tide."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_whyareyouhere", "druchii_envoy_main_hub", "druchii_envoy_whyareyouhere", TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"),
            () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_main_hub_close", "druchii_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That is all thank you."),
            () => IsDruchiiEnvoy(), null, 200);



        //force war
        campaignGameStarter.AddDialogLine("druchii_envoy_force_war", "druchii_envoy_force_war", "druchii_envoy_force_war_choice",
            TORTextHelper.GetText("eonir_druchii_force_war_offer_text", "War can be arranged, for a price."), () => IsDruchiiEnvoy() && EnoughTimePassedSinceLastEvent("force_war", 20), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_force_war_failed", "druchii_envoy_force_war", "druchii_envoy_force_war_failed_choice",
            TORTextHelper.GetText("eonir_druchii_force_war_cooldown_text", "We have recently stirred enough conflict. Wait some time before asking again."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_failed_choice", "druchii_envoy_force_war_failed_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_1", "druchii_envoy_force_war_choice", "druchii_envoy_force_war_choice_result",
            TORTextHelper.GetText("eonir_envoy_lets_do_this_text", "Let us do this."), () => IsDruchiiEnvoy() && (_druchiiForceWarPriceBase - Hero.MainHero.GetSkillValue(DefaultSkills.Charm) <= Hero.MainHero.GetCultureSpecificCustomResourceValue()), null, 200);
        campaignGameStarter.AddPlayerLine("druchii_envoy_force_war_choice_2", "druchii_envoy_force_war_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_force_war_choice_result", "druchii_envoy_force_war_choice_result", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_we_will_see_text", "We will see what we can do."), () => IsDruchiiEnvoy(), ForceWarPrompt, 200);

        //exchange prisoners

        campaignGameStarter.AddDialogLine("druchii_envoy_prisoners", "druchii_envoy_prisoners", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_druchii_prisoners_offer_text", "What a promising trade. This will be credited for your next negotiation with the Witch king. I believe the Council will like this."), () => IsDruchiiEnvoy(), ExchangePrisoners, 200);


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

                Hero.MainHero.AddCultureSpecificCustomResource(-(_druchiiForceWarPriceBase - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)));
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, _druchiiForceWarPriceBase);
                _latestEnvoyActionsPerformed.AddOrReplace("force_war", CampaignTime.Now.ToDays);

            }
        }


        //slaver tide

        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide_choice",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_offer_text", "The slaver tide is a dark elf raiding fleet."), () => IsDruchiiEnvoy() && EnoughTimePassedSinceLastEvent("slaver_tide", 20), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide_failed", "druchii_envoy_slaver_tide", "druchii_envoy_slaver_tide_failed_choice",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_cooldown_text", "The slaver tide has recently been called. Wait some time."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_slaver_tide_failed_choice", "druchii_envoy_slaver_tide_failed_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsDruchiiEnvoy(), null, 200);


        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_1", "druchii_envoy_slaver_tide_choice", "druchii_envoy_slaver_tide_choice_result",
            TORTextHelper.GetText("eonir_druchii_slaver_tide_accept_text", "Call the slaver tide."), () => IsDruchiiEnvoy() && (_druchiiSlaverTidePriceBase - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)) <= Hero.MainHero.GetCultureSpecificCustomResourceValue(), SlaverTidePrompt, 200);

        campaignGameStarter.AddPlayerLine("druchii_envoy_choice_2", "druchii_envoy_slaver_tide_choice", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_slaver_tide_choice_result", "druchii_envoy_slaver_tide_choice_result", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_druchii_could_see_text", "I could see what can be done."), () => IsDruchiiEnvoy(), SlaverTidePrompt, 200);


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

                Hero.MainHero.AddCultureSpecificCustomResource(-(_druchiiSlaverTidePriceBase - Hero.MainHero.GetSkillValue(DefaultSkills.Charm)));
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, _druchiiSlaverTidePriceBase);
                _latestEnvoyActionsPerformed.AddOrReplace("slaver_tide", CampaignTime.Now.ToDays);
            }
        }

        // why are you here?
        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere", "druchii_envoy_whyareyouhere", "envoy_druchii_wayh_reaction",
            TORTextHelper.GetText("eonir_druchii_why_here_intro_text", "I represent the interests of Naggaroth in these lands."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_displeased", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_druchii_why_here_displeased_text", "You have disappointed me."), () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_undecided", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_druchii_why_here_undecided_text", "I am still deciding what to make of you."), () => IsDruchiiEnvoy(), null, 200);
        campaignGameStarter.AddPlayerLine("envoy_druchii_wayh_reaction_agreement", "envoy_druchii_wayh_reaction", "druchii_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_druchii_why_here_agreement_text", "We have an understanding."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere_2", "druchii_envoy_whyareyouhere_2", "druchii_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_druchii_why_here_explain_text", "I am here to further Druchii interests."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("druchii_envoy_whyareyouhere_3", "druchii_envoy_whyareyouhere_3", "back_to_main_hub_druchii",
            TORTextHelper.GetText("eonir_druchii_why_here_services_text", "I can offer various services for the right price."), () => IsDruchiiEnvoy(), null, 200);

        campaignGameStarter.AddDialogLine("back_to_main_hub_druchii", "back_to_main_hub_druchii", "druchii_envoy_main_hub",
            TORTextHelper.GetText("eonir_druchii_anything_else_text", "Is there anything else?"), () => IsDruchiiEnvoy(), null, 200);

        void setDruchiiPrices()
        {
            GameTexts.SetVariable("FORCEWAR_PRICE", _druchiiForceWarPriceBase - Hero.MainHero.GetSkillValue(DefaultSkills.Charm));
            GameTexts.SetVariable("SLAVERTIDE_PRICE", _druchiiSlaverTidePriceBase - Hero.MainHero.GetSkillValue(DefaultSkills.Charm));
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
            TORTextHelper.GetText("eonir_envoy_low_clan_tier_text", "You do not have the privilege to serve the council. You are of no use. (Low Clan Tier)."), () => EonirEnvoyDialogCondition() && IsAsurianEnvoy() && !HasClanTier2(),
            null, 200);


        starter.AddDialogLine("envoy_hub_intro_asur", "start", "asur_envoy_main_hub",
            TORTextHelper.GetText("eonir_asur_intro_text", "Greetings, kinsman. How may I assist you?"), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_money", "asur_envoy_main_hub", "asur_envoy_money",
            TORTextHelper.GetText("eonir_asur_money_ask_text", "I wish to discuss trade matters."),
            () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_troops", "asur_envoy_main_hub", "asur_envoy_troops", TORTextHelper.GetText("eonir_asur_troops_ask_text", "I need warriors from Ulthuan."),
            () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_diplomacy", "asur_envoy_main_hub", "asur_envoy_diplomacy",
            TORTextHelper.GetText("eonir_asur_diplomacy_ask_text", "I wish to discuss diplomacy."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_whyareyouhere", "asur_envoy_main_hub", "asur_envoy_whyareyouhere", TORTextHelper.GetText("eonir_envoy_why_are_you_here_text", "Why are you here?"), () => IsAsurianEnvoy(),
            null, 200);

        starter.AddPlayerLine("asur_envoy_main_hub_close", "asur_envoy_main_hub", "close_window", TORTextHelper.GetText("eonir_envoy_close_text", "That is all thank you."), () => IsAsurianEnvoy(),
            null, 200);


        //money
        starter.AddDialogLine("asur_envoy_money", "asur_envoy_money", "asur_envoy_money_choice",
            TORTextHelper.GetText("eonir_asur_money_offer_text", "Trade can be arranged."), () => IsAsurianEnvoy() && EnoughTimePassedSinceLastEvent("asur_money", 5), null, 200);

        starter.AddDialogLine("asur_envoy_money_failed", "asur_envoy_money", "asur_envoy_money_failed_choice",
            TORTextHelper.GetText("eonir_asur_money_cooldown_text", "We have recently conducted trade. Wait some time."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_money_failed_choice", "asur_envoy_money_failed_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsAsurianEnvoy(), null, 200);


        starter.AddPlayerLine("asur_envoy_money_choice_1", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN1}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY1}{FAVOR_ICON}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= _asurFavorPrice1, () => TransferMoney(1, _asurFavorPrice1), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_2", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN2}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY2}{FAVOR_ICON}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= _asurFavorPrice2, () => TransferMoney(10, _asurFavorPrice2), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_3", "asur_envoy_money_choice", "back_to_main_hub_asur",
            "{ASUR_MONEYRETURN3}{GOLD_ICON} for {ASUR_FAVORCOST_MONEY3}{FAVOR_ICON}", () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= _asurFavorPrice3, () => TransferMoney(30, _asurFavorPrice3), 200);
        starter.AddPlayerLine("asur_envoy_money_choice_quit", "asur_envoy_money_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsAsurianEnvoy(), null, 200);

        //troops

        starter.AddDialogLine("asur_envoy_troops", "asur_envoy_troops", "asur_envoy_troops_choice",
            TORTextHelper.GetText("eonir_asur_troops_offer_text", "Warriors can be sent from Ulthuan."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_troops_choice_1", "asur_envoy_troops_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_asur_troops_accept_text", "Send the warriors."), () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 150, ShowTroopSelectionScreen, 200);
        starter.AddPlayerLine("asur_envoy_troops_choice_2", "asur_envoy_troops_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsAsurianEnvoy(), null, 200);

        //diplomacy
        starter.AddDialogLine("asur_envoy_diplomacy", "asur_envoy_diplomacy", "asur_envoy_diplomacy_choice",
            TORTextHelper.GetText("eonir_asur_diplomacy_offer_text", "Diplomatic arrangements can be made."), () => IsAsurianEnvoy() && EnoughTimePassedSinceLastEvent("asur_diplomacy", 15), null, 200);

        starter.AddDialogLine("asur_envoy_diplomacy_failed", "asur_envoy_diplomacy", "asur_envoy_diplomacy_failed_choice",
            TORTextHelper.GetText("eonir_asur_diplomacy_cooldown_text", "We have recently made diplomatic arrangements. Wait some time."), () => IsAsurianEnvoy(), null, 200);

        starter.AddPlayerLine("asur_envoy_diplomacy_failed_choice", "asur_envoy_diplomacy_failed_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_understood_text", "Understood."), () => IsAsurianEnvoy(), null, 200);


        starter.AddPlayerLine("asur_envoy_diplomacy_choice_1", "asur_envoy_diplomacy_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_asur_diplomacy_accept_text", "Make the arrangements."), () => IsAsurianEnvoy() && Hero.MainHero.GetCultureSpecificCustomResourceValue() >= 400, AsurDiplomacyPrompt, 200);
        starter.AddPlayerLine("asur_envoy_diplomacy_choice_2", "asur_envoy_diplomacy_choice", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_envoy_need_to_think_text", "I need to think about this."), () => IsAsurianEnvoy(), null, 200);


        //why are you here

        starter.AddDialogLine("asur_envoy_whyareyouhere", "asur_envoy_whyareyouhere", "envoy_asur_wayh_reaction",
            TORTextHelper.GetText("eonir_asur_why_here_intro_text", "I represent the Phoenix King interests in these lands."), () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_displeased", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_asur_why_here_displeased_text", "You have not impressed me."), () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_undecided", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_asur_why_here_undecided_text", "I am still evaluating your worth."), () => IsAsurianEnvoy(), null, 200);
        starter.AddPlayerLine("envoy_asur_wayh_reaction_agreement", "envoy_asur_wayh_reaction", "asur_envoy_whyareyouhere_2",
            TORTextHelper.GetText("eonir_asur_why_here_agreement_text", "We have a good understanding."), () => IsAsurianEnvoy(), null, 200);

        starter.AddDialogLine("asur_envoy_whyareyouhere_2", "asur_envoy_whyareyouhere_2", "asur_envoy_whyareyouhere_3",
            TORTextHelper.GetText("eonir_asur_why_here_explain_text", "I am here to protect High Elf interests."), () => IsAsurianEnvoy(), null, 200);

        starter.AddDialogLine("asur_envoy_whyareyouhere_3", "asur_envoy_whyareyouhere_3", "back_to_main_hub_asur",
            TORTextHelper.GetText("eonir_asur_why_here_services_text", "I can offer various services to those who prove worthy."), () => IsAsurianEnvoy(), null, 200);


        starter.AddDialogLine("back_to_main_hub_asur", "back_to_main_hub_asur", "asur_envoy_main_hub",
            TORTextHelper.GetText("eonir_envoy_anything_else_text", "Is there something else I could do for you?"), () => IsAsurianEnvoy(), null, 200);


        void SetupPrices()
        {
            GameTexts.SetVariable("ASUR_MONEYRETURN1", CalculateBasePrice());
            GameTexts.SetVariable("ASUR_MONEYRETURN2", CalculateBasePrice() * 10);
            GameTexts.SetVariable("ASUR_MONEYRETURN3", CalculateBasePrice() * 30);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY1", _asurFavorPrice1);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY2", _asurFavorPrice2);
            GameTexts.SetVariable("ASUR_FAVORCOST_MONEY3", _asurFavorPrice3);
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

    private bool HasClanTier2()
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
        if (_torLithanel == null)
        {
            _torLithanel = Campaign.Current.Settlements.FirstOrDefault(x => x.IsTorLithanel());
        }

        if (envoys == null || envoys.Count == 0)
        {
            PopulateEnvoys();
        }

        if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != _torLithanel) return;
        if (envoys == null || envoys.Count == 0) return;

        foreach (var envoy in envoys)
        {
            var locationchar = _torLithanel.LocationComplex.GetLocationCharacterOfHero(envoy);
            var lordsHall = _torLithanel.LocationComplex.GetLocationWithId("lordshall");
            var currentloc = _torLithanel.LocationComplex.GetLocationOfCharacter(locationchar);
            if (locationchar is null || lordsHall is null || currentloc is null) continue;
            if (currentloc != lordsHall) _torLithanel.LocationComplex.ChangeLocation(locationchar, currentloc, lordsHall);
        }
    }

    private void PopulateEnvoys()
    {
        if (envoys == null)
        {
            envoys = new List<Hero>();
        }
        else
        {
            envoys.Clear();
        }

        // Find existing envoys by their attributes
        _druchiiEnvoy = Hero.AllAliveHeroes.FirstOrDefault(x => x.HasAttribute("DruchiiEnvoy"));
        _asurEnvoy = Hero.AllAliveHeroes.FirstOrDefault(x => x.HasAttribute("AsurEnvoy"));
        _empireEnvoy = Hero.AllAliveHeroes.FirstOrDefault(x => x.HasAttribute("EmpireEnvoy"));
        _spellsingerEnvoy = Hero.AllAliveHeroes.FirstOrDefault(x => x.HasAttribute("SpellsingerEnvoy"));

        if (_druchiiEnvoy != null) envoys.Add(_druchiiEnvoy);
        if (_asurEnvoy != null) envoys.Add(_asurEnvoy);
        if (_empireEnvoy != null) envoys.Add(_empireEnvoy);
        if (_spellsingerEnvoy != null) envoys.Add(_spellsingerEnvoy);
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