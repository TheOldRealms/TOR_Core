using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics
{
    public class GrailKnightCareerButtonCampaignBehavior : CareerButtonBehavior
    {
        private bool _grailKnightCompanionDialogBegins;
        private Hero _currentHeroTemplate;
        
        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            obj.AddDialogLine("grailKnightCompanionHonoring_start1", "start", "grailKnightCompanionHonoring_vow1", new TextObject("Mylord what do you want from me").ToString(), GrailKnightCompanionVowCondition, DeactivateCondition, 200,null);
            obj.AddPlayerLine("grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow2",new TextObject("Yo man you deserve yourself some strips man").ToString(), null, null,200, null);
            obj.AddDialogLine("grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow3", new TextObject("haha really nice!").ToString(), null,null , 200,null);
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
            //Campaign.Current.
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
            _currentHeroTemplate = HeroCreator.CreateSpecialHero(heroTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);

  
            ConversationCharacterData characterData = new ConversationCharacterData(_currentHeroTemplate.CharacterObject, null);
            ConversationCharacterData playerData = new ConversationCharacterData(Hero.MainHero.CharacterObject,Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData,characterData);
        }


        private void MakeGrailKnightCompanion()
        {
            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, _currentHeroTemplate);
            AddHeroToPartyAction.Apply(_currentHeroTemplate, MobileParty.MainParty);
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
            var troopID = eventArgs.TroopId;
            _grailKnightCompanionDialogBegins=true;
            InitiateDialog(troopID);
        }

        public override void SyncData(IDataStore dataStore)
        {
           
        }
    } 
}