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
    public class MercenaryCareerButtonCampaignBehavior: CampaignBehaviorBase
    {
         private bool _mercenaryCompanionDialogBegins;
        private CharacterObject _currentTemplate;
        private int _price = 50000;
        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            obj.AddDialogLine("mercenaryCompanion_bodyguard_start", "start", "mercenaryCompanion_bodyguard_1", new TextObject("Aye what can I do for you?").ToString(), mercenaryCompanionCondition, DeactivateCondition, 200,null);
            obj.AddPlayerLine("mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_2",new TextObject("Your employment has gone quite well and I want to bring you on as a partner").ToString(), null, null,200, null);
            obj.AddDialogLine("mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_3", new TextObject("As a partner?").ToString(), null, null, 200,null);
            obj.AddPlayerLine("mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_4",new TextObject("You get part of the share and need to accomplish a few advanced organisational matters. I will however not pay your wage anymore.").ToString(), null, null,200, null);
            obj.AddDialogLine("mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_5", "First I want to see some hard coin. I am not playing Babysitter or Bodyguard without seeing some money first. You pay me {MERCCOMPANIONPRICE}{GOLD_ICON}", null,null);
            
            obj.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentSuccess",new TextObject("Off course here you go.").ToString(), _playerHasMoney, null,200, null);
            obj.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentFail",new TextObject("Uhm I dont have the money. Maybe later.").ToString(), null,null);

            obj.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_end_success", new TextObject("Thats a good deal, I am looking foward into this partnership").ToString(), null,MakeMercenaryCompanion);
            obj.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentFail", "mercenaryCompanion_bodyguard_end_fail", new TextObject("Well then I stay with my current wage then.").ToString(), null,null);
            obj.AddPlayerLine("mercenaryCompanion_bodyguard_end_success", "mercenaryCompanion_bodyguard_end_success", "close_window", new TextObject("My pleasure, partner.").ToString(), null,null);
            obj.AddPlayerLine("mercenaryCompanion_bodyguard_end_fail", "mercenaryCompanion_bodyguard_end_fail", "close_window", new TextObject("Fine.").ToString(), null,null);
            //Campaign.Current.
        }

        private bool _playerHasMoney()
        {
            return Hero.MainHero.Gold > _price;
        }
        
        public override void RegisterEvents()
        {
            SpecialbuttonEventManagerHandler.Instance.ButtonClickedEventHandler += ButtonPressed;
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        
        private void  InitiateDialog(string troopID)
        {
            
            var characterTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            Game.Current.GameStateManager.PopState(0);

            if (characterTemplate == null)
            {
                //Log error
                return;
            }

            var troopwage = 0;
            if (characterTemplate.TroopWage != null) troopwage = characterTemplate.TroopWage;

            _price = 1000 * characterTemplate.Level + 200 * troopwage;
            GameTexts.SetVariable("MERCCOMPANIONPRICE",_price.ToString());
            
            //_currentHeroTemplate = HeroCreator.CreateSpecialHero(heroTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);
            _currentTemplate = characterTemplate;
            ConversationCharacterData characterData = new ConversationCharacterData(_currentTemplate, null);
            ConversationCharacterData playerData = new ConversationCharacterData(Hero.MainHero.CharacterObject,Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData,characterData);
           
        }


        private void MakeMercenaryCompanion()
        {
            var hero = HeroCreator.CreateSpecialHero(_currentTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _price);
            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentTemplate),-1);
        }

        
        
        public bool mercenaryCompanionCondition()
        {
            return _mercenaryCompanionDialogBegins;
        }
        
        public void  DeactivateCondition()
        {
            _mercenaryCompanionDialogBegins=false;
            return;
        }

        public void ButtonPressed(object sender, TroopEventArgs eventArgs)
        {
            if(Hero.MainHero.GetCareer() != TORCareers.Mercenary) return;
            
            var troopID = eventArgs.TroopId;
            _mercenaryCompanionDialogBegins=true;
            InitiateDialog(troopID);
        }

        public override void SyncData(IDataStore dataStore)
        {
           
        }
    }
}