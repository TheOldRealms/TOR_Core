using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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
            
            var healthyTroops= Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(index);
            var woundedTroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);
            
            var count = healthyTroops - woundedTroops;
            
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
                        copiedTroopRoster.AddToCounts(elem.Troop, 1,false,0,elem.Xp,true);
                        Hero.MainHero.PartyBelongedTo.MemberRoster.RemoveTroop(elem.Troop);
                    }
                }
                copiedTroopRoster.RemoveZeroCounts();
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
                
                var xpGain = CalculateXPGainForRetinues(count, level, retinues);
                if (xpGain >= 0)
                {
                    foreach (var retinue in rightownerparty.MobileParty.MemberRoster.GetTroopRoster().Where(x=> x.Character.StringId.Contains(retinueID)))
                    {
                        if (retinue.Character.UpgradeTargets.Length <= 0) continue;
                        MobileParty.MainParty.MemberRoster.AddXpToTroop(xpGain * retinue.Number, retinue.Character);
                    }
                }
                
            }
            
            _retinue = null;
            Game.Current.GameStateManager.PopState();
        }

        private int CalculateXPGainForRetinues(int unitCount, int level, List<FlattenedTroopRosterElement> retinues)
        {
            var retinueCount = retinues.Count;
            foreach (var elem in retinues)
            {
                if (elem.Troop.UpgradeTargets.Length > 0)
                {
                    if (elem.Troop.GetUpgradeXpCost(PartyBase.MainParty, 0)<=0)
                    {
                        retinueCount--;
                    }
                }
            }
            
            if (retinueCount <= 0) return 0;

            return ( 15 * level * unitCount ) / retinueCount;
        }
        
        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner=false)
        {
            SetUpRetinueExchange(characterObject);
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
        {
            
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;

            if (characterObject.IsHero) return false;

            if (isPrisoner) return false;
            
            if (!Hero.MainHero.HasCareerChoice("SilverHammerPassive4")) return false;

            if (Hero.MainHero.GetCustomResourceValue("Prestige") < retinueCost) return false;

            if (characterObject.StringId.Contains(retinueID))
                return false;

            if (!characterObject.IsHero)
                return true;

            return false;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner=false)
        {
            var index = -1;
            displayText = new TextObject();
            index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterObject);

            if (index == -1) return false;
            
            displayText = new TextObject("Upgrades troop to a Witch Hunter Retinue");
            if (characterObject.IsEliteTroop())
            {
                displayText = new TextObject("Knights Cant be upgraded to Retinues");
                return false;
            }
            
            var healthyTroops= Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(index);
            var woundedTroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);
            if (healthyTroops - woundedTroops < 0 )
            {
                displayText = new TextObject("Not enough healthy troops available");
                return false;
            }

            if (characterObject.Culture.StringId == TORConstants.Cultures.BRETONNIA || characterObject.Race != 0)
            {
                displayText = new TextObject("Needs to be part of the empire or southern realms");
                return false;
            }

            return true;
        }
    }
}