using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
        private static TroopRoster clone;
        private static CharacterObject originalTroop;

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
            originalTroop = characterTemplate;
            if (Hero.MainHero.GetCareer() == TORCareers.WitchHunter)
            {
                WitchHunterRetinues(characterTemplate);
            }
        }

        private static void WitchHunterRetinues(CharacterObject characterTemplate)
        {
            var level = characterTemplate.Level;
            var index= Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterTemplate);
            
            

            var count = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementCopyAtIndex(index).Number;
            
                
            var retinue = MBObjectManager.Instance.GetObject<CharacterObject>("tor_wh_retinue");

            if (retinue != null)
            { 
                PartyScreenLogic.PartyCommand command = new PartyScreenLogic.PartyCommand(); 
                var list = new List<InquiryElement>();

                 var roster = TroopRoster.CreateDummyTroopRoster();
                 
                 

                 roster.AddToCounts(retinue, count);

                 clone = Hero.MainHero.PartyBelongedTo.MemberRoster.CloneRosterData();
                 
                 Hero.MainHero.PartyBelongedTo.MemberRoster.Clear();
                 
                 //Game.Current.GameStateManager.PushState(new PartyState());
                 PartyScreenManager.OpenScreenAsReceiveTroops(roster,new TextObject("retinues"), CloseWindow); 
                 var amount = 1; 
                 var retinues = new List<CharacterObject>();


                 
            }
            
           

        }
        
        private static void CloseWindow(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
        {
            var count = rightMemberRoster.TotalManCount;
            
            
           clone.Add(rightMemberRoster);

           rightOwnerParty.MemberRoster.Clear();
           rightOwnerParty.MemberRoster.Add(clone);
           rightOwnerParty.AddMember(originalTroop, -count);
           
           
           Game.Current.GameStateManager.PopState();
           
           PartyScreenManager.OpenScreenAsNormal();  //I do not understand why, but it while the previous party screen opens fine a crash is occuring after closing this one. 
           
        }
    }
    
    
  

    public class TroopEventArgs: EventArgs
    {
        public string TroopId { get; set; }
    }
}