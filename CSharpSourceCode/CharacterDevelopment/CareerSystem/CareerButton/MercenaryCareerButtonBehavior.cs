using Helpers;
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
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Button
{
    public class MercenaryCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private CharacterObject _currentTemplate;
        private int _price = 50000;

        public MercenaryCareerButtonBehavior(CareerObject career) : base(career)
        {
            if (career != TORCareers.Mercenary) return;

        }

        public override string CareerButtonIcon => "CareerSystem\\ghal_maraz";

        public bool PlayerHasMoney()
        {
            return Hero.MainHero.Gold > _price;
        }

        private void InitiateDialog(string troopID)
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
            GameTexts.SetVariable("MERCCOMPANIONPRICE", _price.ToString());
            _currentTemplate = characterTemplate;
            ConversationCharacterData characterData = new ConversationCharacterData(_currentTemplate, null);
            ConversationCharacterData playerData = new ConversationCharacterData(Hero.MainHero.CharacterObject, Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData, characterData);

        }

        public void MakeMercenaryCompanion()
        {
            var hero = HeroCreator.CreateSpecialHero(_currentTemplate, Campaign.Current.MainParty.CurrentSettlement, null, null, 40);
            hero.SetNewOccupation(Occupation.Special); //see what happens if a special hero joins a clan
            //do companions created through this show up in the encyclopedia?
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, _price);
            AddCompanionAction.Apply(MobileParty.MainParty.ActualClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(MobileParty.MainParty.MemberRoster.FindIndexOfTroop(_currentTemplate), -1);
        }

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
        {
            InitiateDialog(characterObject.StringId);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;

            //Elite + Ranged allows empire mercs to recruit outriders as companions but not the rest of the tree
            return Hero.MainHero.HasCareerChoice("PaymasterPassive4") &&
                !characterObject.IsHero &&
                !isPrisoner &&
                (!characterObject.IsEliteTroop() ||
                    (characterObject.IsEliteTroop() && characterObject.IsRanged));
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText = TORTextHelper.GetTextObject("tor_career_button_mercenary_default", "Makes the selected troop a companion.");
            if (Campaign.Current.Models.ClanTierModel.GetCompanionLimit(Hero.MainHero.Clan) <= Clan.PlayerClan.Companions.Count())
            {
                displayText = TORTextHelper.GetTextObject("tor_career_button_mercenary_companion_limit", "Clan companion limit has been reached.");
                return false;
            }

            //For the time being, cross-culture companioning is disabled until a more detailed system can be created to account for cultures, norms, religions, etc... that differ from bannerlord's "cultures" and create a complex web of which groups would be willing to work in service of which others. At that point, the dialogue for this mechanic's conversation would also need to be rethought as the only axis for it at the moment is economic gain.
            //As a player can only accumulate conformity on prisoners of their culture, having T5+ troops of a different culture requires the player to enter a town of that culture and directly recruit the troop from a notable. Disabled to deal naively with that case.
            if (Hero.MainHero.Culture != characterObject.Culture)
            {
                displayText = TORTextHelper.GetTextObject("tor_career_button_mercenary_wrong_culture", "Troop must be of your culture.");
                return false;
            }

            if (characterObject.IsKnightUnit() && !characterObject.IsRanged)
            {
                displayText = TORTextHelper.GetTextObject("tor_career_button_mercenary_no_knightly", "Only works for non-knightly units.");
                return false;
            }

            if (characterObject.IsTreeSpirit() || characterObject.IsUndead())
            {
                displayText = TORTextHelper.GetTextObject("tor_career_button_mercenary_no_tree_undead", "Only works for humans, dwarfs or elves");
                return false;
            }

            if (characterObject.Level < 26)
            {
                displayText = TORTextHelper.GetTextObject("tor_career_button_mercenary_not_tier5", "Troop needs to reach tier 5 or higher.");
                return false;
            }

            return true;
        }

    }
}