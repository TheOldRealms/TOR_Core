using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class GrailKnightCareerButtonCampaignBehavior : CampaignBehaviorBase
    {
        private bool _grailKnightCompanionDialogBegins;
        private CharacterObject _currentCharacterTemplate;
        
        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            obj.AddDialogLine("grailKnightCompanionHonoring_start1", "start", "grailKnightCompanionHonoring_vow1", new TextObject("Hello Lord, it is rare that I have the pleasure of speaking with you. How can I serve you?").ToString(), GrailKnightCompanionVowCondition, DeactivateCondition, 200,null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow2",new TextObject("You have served me well for some time now and like I have completed your quest for the grail and have passed the lady’s secret trials.").ToString(), null, null,200, null);
            obj.AddDialogLine("grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow3", new TextObject("I would serve no other! What an honour to fight by the side of a fellow Grail Knight, especially one as accomplished and renowned as you.").ToString(), null,null , 200,null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow4",new TextObject("Get down on your knees brother").ToString(), null, null,200, null);
            obj.AddDialogLine("grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow5", new TextObject("wait what are you serious now?").ToString(), null,null , 200,null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow5", "grailKnightCompanionHonoring_vow5", "grailKnightCompanionHonoring_vow6",new TextObject("Get on your fucking knees!").ToString(), null, null,200, null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow6", "grailKnightCompanionHonoring_vow6", "grailKnightCompanionHonoring_vow7",new TextObject("Now Swear by the Lady!").ToString(), null, null,200, null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow7", "grailKnightCompanionHonoring_vow7", "grailKnightCompanionHonoring_vow8",new TextObject("I am a motherfucking grail knight !").ToString(), null, null,200, null);
            obj.AddDialogLine("grailKnightCompanionHonoring_vow8", "grailKnightCompanionHonoring_vow8", "grailKnightCompanionHonoring_vow9", new TextObject("I am a motherfucking grail knight.").ToString(), null,null , 200,null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow9", "grailKnightCompanionHonoring_vow9", "grailKnightCompanionHonoring_vow10",new TextObject("I kick now some more ass!").ToString(), null, null,200, null);
            obj.AddDialogLine("grailKnightCompanionHonoring_vow10", "grailKnightCompanionHonoring_vow10", "grailKnightCompanionHonoring_vow11",new TextObject("...I kick now some more ass!").ToString(), null, null,200, null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow11", "grailKnightCompanionHonoring_vow11", "grailKnightCompanionHonoring_vow12",new TextObject("now lift yourself and be my loyal servant!").ToString(), null, MakeGrailKnightCompanion,200, null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow12", "grailKnightCompanionHonoring_vow12", "close_window",new TextObject("now lift yourself and be my loyal servant!").ToString(), null, null,200, null);
        }
        
        public override void RegisterEvents()
        {
            SpecialbuttonEventManagerHandler.Instance.ButtonClickedEventHandler += ButtonPressed;
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        
        private void  InitiateDialog(string troopID)
        {
            var heroTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            Game.Current.GameStateManager.PopState(0);

            if (heroTemplate == null)
            {
                //Log error
                return;
            }
           

            _currentCharacterTemplate = heroTemplate;
            ConversationCharacterData characterData = new ConversationCharacterData(heroTemplate, null);
            ConversationCharacterData playerData = new ConversationCharacterData(Hero.MainHero.CharacterObject,Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData,characterData);

            
        }


        private void MakeGrailKnightCompanion()
        {
            var hero = HeroCreator.CreateSpecialHero(_currentCharacterTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);
            
            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentCharacterTemplate),-1);
        }
        
        
        public bool GrailKnightCompanionVowCondition()
        {
            return _grailKnightCompanionDialogBegins;
        }
        
        public void  DeactivateCondition()
        {
            _grailKnightCompanionDialogBegins=false;
            return;
        }

        public void ButtonPressed(object sender, TroopEventArgs eventArgs)
        {
            if(Hero.MainHero.GetCareer() != TORCareers.GrailKnight) return;
            
            var troopID = eventArgs.TroopId;
            _grailKnightCompanionDialogBegins=true;
            InitiateDialog(troopID);
        }

        public override void SyncData(IDataStore dataStore)
        {
           
        }
    } 
}