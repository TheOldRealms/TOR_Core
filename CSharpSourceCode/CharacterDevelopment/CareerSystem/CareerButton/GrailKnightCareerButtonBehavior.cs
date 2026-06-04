using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class GrailKnightCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private CharacterObject _currentCharacterTemplate;
        private const int GRAILKNIGHTCOMPANIONPROMOTIONCOST = 100;

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
            hero.SetNewOccupation(Occupation.Special); //see what happens if a special hero joins a clan
            //do companions created through this show up in the encyclopedia?

            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentCharacterTemplate), -1);
        }

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
        {
            InitiateDialog(characterObject.StringId);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;

            if (characterObject.IsHero) return false;
            if (characterObject.StringId != "tor_br_grail_knight") return false;

            var choices = Hero.MainHero.GetAllCareerChoices();

            if (choices.Contains("HolyCrusaderPassive3"))
            {
                return true;
            }

            return false;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            var icon = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText();
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() <= GRAILKNIGHTCOMPANIONPROMOTIONCOST)
            {
                displayText = TORTextHelper.GetTextObject("tor_generic_customResourceCost", "Requires {AMOUNT} {RESOURCE_ICON}", true);
                displayText.SetTextVariable("AMOUNT", GRAILKNIGHTCOMPANIONPROMOTIONCOST);
                displayText.SetTextVariable("RESOURCE_ICON", icon);
                return false;
            }
            else if (Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
            {
                displayText = TORTextHelper.GetTextObject("tor_generic_companionLimitReached", "Clan companion limit reached", true);
                return false;
            }

            var promotionText = TORTextHelper.GetTextObject("tor_grail_knight_promote_companion_text", "Promotes your Grail Knight to a companion (Costs {AMOUNT} {RESOURCE_ICON})");
            promotionText.SetTextVariable("AMOUNT", GRAILKNIGHTCOMPANIONPROMOTIONCOST);
            promotionText.SetTextVariable("RESOURCE_ICON", icon);

            displayText = promotionText;

            return true;
        }
    }
}