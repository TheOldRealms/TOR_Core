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
    public class WitchHunterCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private TroopRoster copiedTroopRoster;

        private CharacterObject originalTroop;
        private CharacterObject _retinue;
        private int level;

        private int retinueCost = 3;

        private const string retinueID = "tor_wh_retinue";

        public override string CareerButtonIcon => "CareerSystem\\ghal_maraz";

        public WitchHunterCareerButtonBehavior(CareerObject career) : base(career)
        {
        }

        public void SetUpRetinueExchange(CharacterObject characterTemplate)
        {
            originalTroop = characterTemplate;
            level = characterTemplate.Level;
            var index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterTemplate);
            var count = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementCopyAtIndex(index).Number;

            

            var value = Hero.MainHero.GetCustomResourceValue("Prestige");
            
            var canAfford = (int) value/retinueCost;

            var availableRetinues = (int) Mathf.Min(count, canAfford);
            
            _retinue = MBObjectManager.Instance.GetObject<CharacterObject>(retinueID);

            if (_retinue != null)
            {
                var roster = TroopRoster.CreateDummyTroopRoster();
                roster.AddToCounts(_retinue, availableRetinues);
                copiedTroopRoster = TroopRoster.CreateDummyTroopRoster();
                foreach (var elem in Hero.MainHero.PartyBelongedTo.MemberRoster.ToFlattenedRoster())
                {
                    if (!elem.Troop.IsHero)
                    {
                        Hero.MainHero.PartyBelongedTo.MemberRoster.RemoveTroop(elem.Troop);
                        copiedTroopRoster.AddToCounts(elem.Troop, 1);
                    }
                }
                PartyScreenManager.OpenScreenAsReceiveTroops(roster, new TextObject("Witch Hunter Retinues"), AddRetinuesAndCalculateXPGain);
            }
        }

        private void AddRetinuesAndCalculateXPGain(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftPrisonRoster, PartyBase rightownerparty, TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            //TODO rework, needs a bit more love
            var count = rightmemberroster.TotalManCount - rightmemberroster.TotalHeroes;
            if (count >= 0)
            {
                rightownerparty.MemberRoster.Add(copiedTroopRoster);
                rightownerparty.AddMember(originalTroop, -count);
                
                Hero.MainHero.AddCustomResource("Prestige", - retinueCost*count);


                var retinues = rightownerparty.MobileParty.MemberRoster.ToFlattenedRoster().ToList();
                retinues = retinues.Where(x => x.Troop.StringId.Contains(retinueID)).ToList();

                var xpGain = CalculateXPGainForRetinues(count, level, retinues.Count);
                foreach (var retinue in retinues)
                {
                    var index = MobileParty.MainParty.MemberRoster.FindIndexOfTroop(retinue.Troop);
                    MobileParty.MainParty.MemberRoster.SetElementXp(index, MobileParty.MainParty.MemberRoster.GetElementXp(retinue.Troop) + xpGain);
                }
            }
            
            _retinue = null;
            Game.Current.GameStateManager.PopState();
        }

        private int CalculateXPGainForRetinues(int unitCount, int level, int retinueCount)
        {
            if (retinueCount <= 0) return 0;

            return ( 15 * level * unitCount ) / retinueCount;
        }
        
        public override void ButtonClickedEvent(CharacterObject characterObject)
        {
            SetUpRetinueExchange(characterObject);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
        {

            if (characterObject.IsHero) return false;

            if (isPrisoner) return false;
            
            if (!Hero.MainHero.HasCareerChoice("SilverHammerPassive4")) return false;

            if (Hero.MainHero.GetCustomResourceValue("Prestige") < retinueCost) return false;

            if (characterObject.StringId == "tor_wh_retinue")
                return false;

            if (!characterObject.IsHero)
                return true;

            return false;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner=false)
        {
            displayText = new TextObject("Upgrades troop to a Witch Hunter Retinue");
            if (characterObject.IsEliteTroop())
            {
                displayText = new TextObject("Knights Cant be upgraded to Retinues");
                return false;
            }

            if (characterObject.Culture.StringId == "vlandia" || characterObject.Race != 0)
            {
                displayText = new TextObject("Needs to be part of the empire or southern realms");
                return false;
            }

            return true;
        }
    }
}