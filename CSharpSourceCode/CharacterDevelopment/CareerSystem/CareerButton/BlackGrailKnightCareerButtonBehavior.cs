using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class BlackGrailKnightCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private TroopRoster _copiedTroopRoster;
        private const string _knightId = "tor_m_knight_of_misfortune";
        private CharacterObject _originalTroop;
        private CharacterObject _convertedKnight;
        private int _exchangeCost = 15;
        private bool _isPrisoner;

        public override string CareerButtonIcon => "CareerSystem\\blackgrail";

        public BlackGrailKnightCareerButtonBehavior(CareerObject career) : base(career)
        {
        }

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false)
        {
            SetupKnightExchange(characterObject, isPrisoner);
        }

        private void SetupKnightExchange(CharacterObject characterObject, bool isPrisoner)
        {
            _isPrisoner = isPrisoner;
            _originalTroop = characterObject;
            var index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterObject);
            var count = 0;
            if (!_isPrisoner)
            {
                count += Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementCopyAtIndex(index).Number;
            }
            else
            {
                index = Hero.MainHero.PartyBelongedTo.PrisonRoster.FindIndexOfTroop(characterObject);
                count = Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementNumber(index);
            }
            
            var value = Hero.MainHero.GetCustomResourceValue("DarkEnergy");

            var canAfford = (int)value / _exchangeCost;

            var availableKnights = (int)Mathf.Min(count, canAfford);
            availableKnights -= Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);

            _convertedKnight = MBObjectManager.Instance.GetObject<CharacterObject>(_knightId);

            if (_convertedKnight != null)
            {
                var roster = TroopRoster.CreateDummyTroopRoster();
                roster.AddToCounts(_convertedKnight, availableKnights);
                _copiedTroopRoster = TroopRoster.CreateDummyTroopRoster();
                foreach (var elem in Hero.MainHero.PartyBelongedTo.MemberRoster.ToFlattenedRoster())
                {
                    if (!elem.Troop.IsHero)
                    {
                        if (elem.IsWounded)
                        {
                            _copiedTroopRoster.AddToCounts(elem.Troop, 0, false, 1, elem.Xp, true);
                        }
                        else
                        {
                            _copiedTroopRoster.AddToCounts(elem.Troop, 1, false, 0, elem.Xp, true);
                        }

                        Hero.MainHero.PartyBelongedTo.MemberRoster.RemoveTroop(elem.Troop);
                    }
                }

                _copiedTroopRoster.RemoveZeroCounts();
                PartyScreenManager.OpenScreenAsReceiveTroops(roster, new TextObject("Ill fated Knights: Exchange " + _originalTroop.Name.ToString() + "for mousillon Knights"), ExchangeKnights);
            }
        }

        private void ExchangeKnights(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftPrisonRoster, PartyBase rightownerparty, TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            var count = rightmemberroster.TotalManCount - rightmemberroster.TotalHeroes;
            if (count >= 0)
            {
                rightownerparty.MemberRoster.Add(_copiedTroopRoster);
                if (!_isPrisoner)
                {
                    rightownerparty.AddMember(_originalTroop, -count);
                }
                else
                {
                    rightownerparty.AddPrisoner(_originalTroop, -count);
                }

                Hero.MainHero.AddCustomResource("DarkEnergy", -_exchangeCost * count);
            }

            Game.Current.GameStateManager.PopState();
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;

            if (!Hero.MainHero.HasCareerChoice("ScourgeOfBretonniaPassive4")) return false;
            if (characterObject.IsHero) return false;


            if (characterObject.Culture.StringId != TORConstants.Cultures.BRETONNIA) return false;

            if (!characterObject.IsKnightUnit()) return false;

            return true;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText = new TextObject("");
            var index = -1;
            if (!isPrisoner)
            {
                index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterObject);
            }
            else
            {
                index = Hero.MainHero.PartyBelongedTo.PrisonRoster.FindIndexOfTroop(characterObject);
            }

            if (index == -1) return false;

            if (isPrisoner)
            {
                var healthyPrisoners = Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementNumber(index);
                var woundedPrisoners = Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementWoundedNumber(index);
                if (healthyPrisoners - woundedPrisoners < 0)
                {
                    displayText = new TextObject("Not enough healthy prisoners available");
                    return false;
                }
            }
            else
            {
                var healthytroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(index);
                var woundedtroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);
                if (healthytroops - woundedtroops < 0)
                {
                    displayText = new TextObject("Not enough healthy troops available");
                    return false;
                }
            }
            
            if (_exchangeCost > Hero.MainHero.GetCustomResourceValue("DarkEnergy"))
            {
                displayText = new TextObject("Requires atleast " + _exchangeCost + " " + CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                return false;
            }

            displayText = new TextObject("");

            if (characterObject.StringId == "tor_br_grail_knight")
            {
                displayText = new TextObject("Grail knights can't be convinced");
                return false;
            }

            return true;
        }
    }
}