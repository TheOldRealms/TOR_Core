using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class SpecialbuttonEventManagerHandler
    { 
        public event EventHandler<TroopEventArgs> ButtonClickedEventHandler;
        
        private static SpecialbuttonEventManagerHandler _instance;
        private SpecialbuttonEventManagerHandler()
        {
            
        }

        public static SpecialbuttonEventManagerHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SpecialbuttonEventManagerHandler();
                }
                return _instance;
            }
        }
        
        
        public void OnButtonClicked(string troopID)
        {

            
            var e= new TroopEventArgs();
            e.TroopId = troopID;
            HandleBasicTroopExchanges(troopID);
            ButtonClickedEventHandler(this,e);      //Special campaign behaviors like dialogs ect.
        }
        
        private  void HandleBasicTroopExchanges(string troopID)
        {
            var characterTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            if (Hero.MainHero.GetCareer() == TORCareers.WitchHunter)
            {
                WitchHunterRetinues(characterTemplate);
            }
        }

        private static void WitchHunterRetinues(CharacterObject characterTemplate)
        {
            var level = characterTemplate.Level;
            var index= Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterTemplate);
            Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCountsAtIndex(index, -1);
                
            var retinue = MBObjectManager.Instance.GetObject<CharacterObject>("tor_wh_retinue");

            if (retinue != null)
            {
                Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(retinue, 1);
                PartyScreenLogic.PartyCommand command = new PartyScreenLogic.PartyCommand();
              //  command.FillForShiftTroop(PartyScreenLogic.PartyRosterSide.Right,PartyScreenLogic.TroopType.Member,retinue,index+1);
               // PartyScreenManager.PartyScreenLogic.UpdateDelegate.Invoke(command);
              // PartyScreenManager.PartyScreenLogic.DoneLogic(true);
             // PartyScreenManager.PartyScreenLogic.SavePartyScreenData();

             PartyScreenManager.PartyScreenLogic.CurrentData.RightParty.AddMember(retinue, 1, 0); 
             command.FillForShiftTroop(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member, retinue, index+2); 
             PartyScreenManager.PartyScreenLogic.AddCommand(command);
                
                var amount = 1;
                var retinues = new List<CharacterObject>();
                for (var i = 0; i < Hero.MainHero.PartyBelongedTo.MemberRoster.Count; i++)
                {
                    var unit = Hero.MainHero.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(i);
                    if (!unit.StringId.StartsWith("tor_wh_retinue")) continue;
                    amount++;
                    retinues.Add(unit);
                }

                var bonus = 50 * level / amount;

                foreach (var retinueUnit in retinues)
                {
                    Hero.MainHero.PartyBelongedTo.MemberRoster.AddXpToTroop(bonus, retinueUnit);

                   
                }
                
               // PartyScreenManager.OpenScreenAsNormal();
            }
        }
    }
    
    
  

    public class TroopEventArgs: EventArgs
    {
        public string TroopId { get; set; }
    }
}