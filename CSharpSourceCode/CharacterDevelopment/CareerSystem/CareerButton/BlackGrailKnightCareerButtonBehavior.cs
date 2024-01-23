using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class BlackGrailKnightCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private TroopRoster _copiedTroopRoster;
        private string _knightId = "tor_m_knight_of_misfortune";
        private CharacterObject _originalTroop;
        private CharacterObject _convertedKnight;
        private bool _isPrisoner;
        private int _exchangeCost = 15;
        
        public override string CareerButtonIcon => "CareerSystem\\blackgrail";
        
        public BlackGrailKnightCareerButtonBehavior(CareerObject career) : base(career)
        {
        }

        public override void ButtonClickedEvent(CharacterObject characterObject)
        {
            SetupKnightExchange(characterObject);
        }

        private void SetupKnightExchange(CharacterObject characterObject)
        {
            _originalTroop = characterObject;
            var index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterObject);
            var count = 0;
            if (index != -1)
            { 
                count = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementCopyAtIndex(index).Number;
            }
            else
            {
                index = Hero.MainHero.PartyBelongedTo.PrisonRoster.FindIndexOfTroop(characterObject);
                count = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementCopyAtIndex(index).Number;
            }
            
            
            var value = Hero.MainHero.GetCustomResourceValue("DarkEnergy");
            
            var canAfford = (int) value/_exchangeCost;

            var availableKnights = (int) Mathf.Min(count, canAfford);
            
            
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
                        var indexOfTroop = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(elem.Troop);
                        if (elem.IsWounded)
                        {
                            _copiedTroopRoster.AddToCounts(elem.Troop, 0,false,1,elem.Xp,true,indexOfTroop);
                        }
                        else
                        {
                            _copiedTroopRoster.AddToCounts(elem.Troop, 1,false,0,elem.Xp,true);
                        }
                        Hero.MainHero.PartyBelongedTo.MemberRoster.RemoveTroop(elem.Troop);
                    }
                }
                _copiedTroopRoster.RemoveZeroCounts();
                PartyScreenManager.OpenScreenAsReceiveTroops(roster, new TextObject("Ill fated Knights"), ExchangeKnights);
            }
            
        }
        
        private void ExchangeKnights(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftPrisonRoster, PartyBase rightownerparty, TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            var count = rightmemberroster.TotalManCount - rightmemberroster.TotalHeroes;
            if (count >= 0)
            {
                rightownerparty.MemberRoster.Add(_copiedTroopRoster);
                rightownerparty.AddMember(_originalTroop, -count);
                Hero.MainHero.AddCustomResource("DarkEnergy", - _exchangeCost*count);
                
            }
            
            _knightId = null;
            Game.Current.GameStateManager.PopState();
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (characterObject.IsHero) return false;

            if (characterObject.Culture.StringId != "vlandia") return false;

            if (!characterObject.IsKnightUnit()) return false;

            return true;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
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