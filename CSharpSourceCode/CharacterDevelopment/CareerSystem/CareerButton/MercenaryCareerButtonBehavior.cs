using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Button
{
    public class MercenaryCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private bool _mercenaryCompanionDialogBegins;
        private CharacterObject _currentTemplate;
        private int _price = 50000;
        
        public MercenaryCareerButtonBehavior(CareerObject career) : base(career)
        {
            if(career != TORCareers.Mercenary) return;
            
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        
        public override string CareerButtonIcon => "CareerSystem\\ghal_maraz";

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddDialogLine("mercenaryCompanion_bodyguard_start", "start", "mercenaryCompanion_bodyguard_1", new TextObject("Aye what can I do for you?").ToString(), mercenaryCompanionCondition, disableDialogCondition, 200,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_2",new TextObject("Your employment has gone quite well and I want to bring you on as a partner").ToString(), null, null,200, null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_3", new TextObject("As a partner?").ToString(), null, null, 200,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_4",new TextObject("You get part of the share and need to accomplish a few advanced organisational matters. I will however not pay your wage anymore.").ToString(), null, null,200, null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_5", "First I want to see some hard coin. I am not playing Babysitter or 'Partner' without seeing some money first. You pay me {MERCCOMPANIONPRICE}{GOLD_ICON}", null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentSuccess",new TextObject("Of course, consider this a forward on your upcoming shares.").ToString(), _playerHasMoney, null,200, null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentFail",new TextObject("I don’t have that in hand right now.").ToString(), null,null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_end_success", new TextObject("Thats a good deal, I am looking foward into this partnership").ToString(), null,MakeMercenaryCompanion);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentFail", "mercenaryCompanion_bodyguard_end_fail", new TextObject("Well then I stay with my current wage then.").ToString(), null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_end_success", "mercenaryCompanion_bodyguard_end_success", "close_window", new TextObject("What a wise decision!").ToString(), null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_end_fail", "mercenaryCompanion_bodyguard_end_fail", "close_window", new TextObject("Fine.").ToString(), null,null);
        }
        
        private bool _playerHasMoney()
        {
            return Hero.MainHero.Gold > _price;
        }
        
        private void  InitiateDialog(string troopID)
        {
            _mercenaryCompanionDialogBegins=true;
            
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

        private bool mercenaryCompanionCondition()
        {
            return _mercenaryCompanionDialogBegins;
        }

        private void  disableDialogCondition()
        {
            _mercenaryCompanionDialogBegins=false;
        }

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner=false)
        {
            InitiateDialog(characterObject.StringId);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner=false)
        {
            return Hero.MainHero.HasCareerChoice("PaymasterPassive4")&& !characterObject.IsHero&&  (!characterObject.IsEliteTroop()||characterObject.IsEliteTroop()&& characterObject.IsRanged);
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner=false)
        { 
            displayText = new TextObject("Makes the current unit a companion");
            if (Campaign.Current.Models.ClanTierModel.GetCompanionLimit(Hero.MainHero.Clan) <= Hero.MainHero.CompanionsInParty.Count())
            {
                displayText = new TextObject("Party limit has been reached.");
                return false;
            }

            if (characterObject.IsKnightUnit()&& !characterObject.IsRanged)
            {
                displayText = new TextObject("Only works for non-knightly units.");
                return false;
            }
            
            if (characterObject.Level<=26)
            {
                displayText = new TextObject("Unit needs to reach tier 5 and higher.");
                return false;
            }
            
            return true;
        }
    }
}