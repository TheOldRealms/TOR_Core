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

        public GrailKnightCareerButtonBehavior(CareerObject career) : base(career)
        {
            if (career != TORCareers.GrailKnight) return;
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override string CareerButtonIcon => "CareerSystem\\grail";

        private void OnSessionLaunched(CampaignGameStarter starter)
        {

        }

        private void InitiateDialog(string troopID)
        {
            isDialogStart = true;
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
        
        public void MakeGrailKnightCompanion()
        {
            Hero.MainHero.AddCultureSpecificCustomResource(-100);
            var hero = HeroCreator.CreateSpecialHero(_currentCharacterTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);

            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentCharacterTemplate), -1);
        }
        
        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner=false)
        {
            InitiateDialog(characterObject.StringId);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner=false)
        {
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;
            
            if (characterObject.IsHero) return false;
            if (characterObject.StringId != "tor_br_grail_knight") return false;

            var choices = Hero.MainHero.GetAllCareerChoices();

            if (choices.Contains("HolyCrusaderPassive3")&& Hero.MainHero.GetCultureSpecificCustomResourceValue()>=100)
            {
                return true;
            }

            return false;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner=false)
        {
            var icon = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText();
            displayText = new TextObject($"Promotes your Grail Knight to a companion (Cost 100{icon})");
            return true;
        }
    }
}