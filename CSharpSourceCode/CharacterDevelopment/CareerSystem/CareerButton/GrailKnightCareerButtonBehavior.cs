using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class GrailKnightCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private CharacterObject _currentCharacterTemplate;
        private bool _grailKnightCompanionDialogBegins;

        public GrailKnightCareerButtonBehavior(CareerObject career) : base(career)
        {
            if (career != TORCareers.GrailKnight) return;
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override string CareerButtonIcon => "CareerSystem\\grail";

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            obj.AddDialogLine("grailKnightCompanionHonoring_start1", "start", "grailKnightCompanionHonoring_vow1", new TextObject("{=str_tor_grail_companion_vow_start}Hello Lord, it is rare that I have the pleasure of speaking with you. How can I serve you?").ToString(), GrailKnightCompanionVowCondition, DeactivateCondition, 200, null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow2", new TextObject("{=str_tor_grail_companion_vow_1}You have served me well for some time now and like I have completed your quest for the grail and have passed the lady’s secret trials.").ToString(), null, null, 200,
                null);

            obj.AddDialogLine("grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow3", new TextObject("{=str_tor_grail_companion_vow_2}I would serve no other! What an honour to fight by the side of a fellow Grail Knight, especially one as accomplished and renowned as you.").ToString(), null, null, 200,
                null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow4",
                new TextObject("{=str_tor_grail_companion_vow_3}I wish to join you to myself, as a member of my family and inner circle. In such a role you will lead my men into battle, handle what tasks I set aside for you and I may even grant you your own band to campaign with.").ToString(), null, null, 200, null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow3_end", "grailKnightCompanionHonoring_vow3", "close_window", new TextObject("{=str_tor_grail_companion_vow_3}Thank you, it was good speaking with you.").ToString(), null, null, 200, null);

            obj.AddDialogLine("grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow5", new TextObject("{=str_tor_grail_companion_vow_4}I am flattered, truly my lord, I accept wholeheartedly!").ToString(), null, MakeGrailKnightCompanion, 200, null);
            obj.AddDialogLine("grailKnightCompanionHonoring_vow5", "grailKnightCompanionHonoring_vow5", "close_window", new TextObject("{=str_tor_grail_companion_vow_5}Thank you, it was good speaking with you.").ToString(), null, null, 200, null);
        }

        private void InitiateDialog(string troopID)
        {
            _grailKnightCompanionDialogBegins = true;
            var heroTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            Game.Current.GameStateManager.PopState(0);

            if (heroTemplate == null)
            {
                return;
            }
            
            _currentCharacterTemplate = heroTemplate;
            ConversationCharacterData characterData = new ConversationCharacterData(heroTemplate, null);
            ConversationCharacterData playerData = new ConversationCharacterData(Hero.MainHero.CharacterObject, Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData, characterData);
        }
        
        private void MakeGrailKnightCompanion()
        {
            var hero = HeroCreator.CreateSpecialHero(_currentCharacterTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);

            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentCharacterTemplate), -1);
        }
        
        public bool GrailKnightCompanionVowCondition()
        {
            return _grailKnightCompanionDialogBegins;
        }

        public void DeactivateCondition()
        {
            _grailKnightCompanionDialogBegins = false;
        }
        
        public override void ButtonClickedEvent(CharacterObject characterObject)
        {
            InitiateDialog(characterObject.StringId);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject)
        {
            if (characterObject.IsHero) return false;
            if (characterObject.StringId != "tor_br_grail_knight") return false;

            var choices = Hero.MainHero.GetAllCareerChoices();

            if (choices.Contains("HolyCrusaderPassive3"))
            {
                return true;
            }

            return false;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText)
        {
            displayText = new TextObject("Promotes your Grail Knight to a companion");
            return true;
        }
    }
}