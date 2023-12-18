using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.Careers
{
    public class WitchHunterRetinueRecruitment
    {
        private TroopRoster copiedTroopRoster;

        private CharacterObject originalTroop;
        private CharacterObject _retinue;
        private int level;

        private const string retinueID = "tor_wh_retinue";


        public void SetUpRetinueExchange(CharacterObject characterTemplate)
        {
            originalTroop = characterTemplate; 
            level = characterTemplate.Level;
            var index= Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterTemplate);
            var count = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementCopyAtIndex(index).Number;
            _retinue = MBObjectManager.Instance.GetObject<CharacterObject>(retinueID);

            if (_retinue != null)
            { 
                var roster = TroopRoster.CreateDummyTroopRoster();
                roster.AddToCounts(_retinue, count);
                copiedTroopRoster =TroopRoster.CreateDummyTroopRoster();
                foreach (var elem in Hero.MainHero.PartyBelongedTo.MemberRoster.ToFlattenedRoster())
                {
                    if (!elem.Troop.IsHero)
                    {
                        Hero.MainHero.PartyBelongedTo.MemberRoster.RemoveTroop(elem.Troop);
                        copiedTroopRoster.AddToCounts(elem.Troop, 1);
                    }
                }
               
                PartyScreenManager.OpenScreenAsReceiveTroops(roster,new TextObject("Witch Hunter Retinues"), AddRetinuesAndCalculateXPGain); 
            }
        }

        private void AddRetinuesAndCalculateXPGain(PartyBase leftownerparty, TroopRoster leftmemberroster, TroopRoster leftPrisonRoster, PartyBase rightownerparty, TroopRoster rightmemberroster, TroopRoster rightprisonroster, bool fromcancel)
        {
            
            var count = rightmemberroster.TotalManCount-rightmemberroster.TotalHeroes;
            if(count<=0)
                return;
            //copiedTroopRoster.Add(rightmemberroster);

            //rightownerparty.MemberRoster.Clear();
            rightownerparty.MemberRoster.Add(copiedTroopRoster);
            rightownerparty.AddMember(originalTroop, -count);

          
                
            var retinues = rightownerparty.MobileParty.MemberRoster.ToFlattenedRoster().ToList(); 
            retinues= retinues.Where(x => x.Troop.StringId.Contains(retinueID)).ToList();
            
            var xpGain = CalculateXPGainForRetinues(count, level, retinues.Count);
            if(xpGain==0) return;
            foreach (var retinue in retinues)
            {
                var index = MobileParty.MainParty.MemberRoster.FindIndexOfTroop(retinue.Troop);
                MobileParty.MainParty.MemberRoster.SetElementXp(index, MobileParty.MainParty.MemberRoster.GetElementXp(retinue.Troop) + xpGain);
            }
                
                

            _retinue = null;
            Game.Current.GameStateManager.PopState();
        }

        private int CalculateXPGainForRetinues(int unitCount, int level, int retinueCount)
        {
            if (retinueCount <= 0) return 0;
            
            return ( 15 * level * unitCount )/retinueCount;
        }
    }
}