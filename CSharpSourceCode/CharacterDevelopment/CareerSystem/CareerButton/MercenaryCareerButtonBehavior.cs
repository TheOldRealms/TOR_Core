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
        private CharacterObject _currentTemplate;
        private int _price = 50000;
        
        public MercenaryCareerButtonBehavior(CareerObject career) : base(career)
        {
            if(career != TORCareers.Mercenary) return;
            
        }
        
        public override string CareerButtonIcon => "CareerSystem\\ghal_maraz";
        
        public bool PlayerHasMoney()
        {
            return Hero.MainHero.Gold > _price;
        }
        
        private void  InitiateDialog(string troopID)
        {
            isDialogStart = true;
            
            var characterTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            Game.Current.GameStateManager.PopState(0);

            if (characterTemplate == null)
            {
                //Log error
                return;
            }

            _price = 1000 * characterTemplate.Level + 200 * characterTemplate.TroopWage;
            GameTexts.SetVariable("MERCCOMPANIONPRICE",_price.ToString());
            _currentTemplate = characterTemplate;
            ConversationCharacterData characterData = new ConversationCharacterData(_currentTemplate, null);
            ConversationCharacterData playerData = new ConversationCharacterData(Hero.MainHero.CharacterObject,Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData,characterData);
           
        }
        
        public void MakeMercenaryCompanion()
        {
            var hero = HeroCreator.CreateSpecialHero(_currentTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _price);
            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentTemplate),-1);
        }

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner=false)
        {
            InitiateDialog(characterObject.StringId);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner=false)
        {
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;
            
            return Hero.MainHero.HasCareerChoice("PaymasterPassive4") && 
                !characterObject.IsHero && 
                !isPrisoner &&
                (!characterObject.IsEliteTroop() || 
                    (characterObject.IsEliteTroop() && characterObject.IsRanged));
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

            if (characterObject.IsTreeSpirit() || characterObject.IsUndead())
            {
                displayText = new TextObject("Only works for humans or elves");
                return false;
            }
            
            if (characterObject.Level<=21)
            {
                displayText = new TextObject("Unit needs to reach tier 5 and higher.");
                return false;
            }
            
            return true;
        }
        
    }
}