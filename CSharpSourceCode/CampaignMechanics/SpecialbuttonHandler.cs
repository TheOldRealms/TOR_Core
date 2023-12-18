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
using TOR_Core.CampaignMechanics.Careers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class SpecialbuttonEventManagerHandler
    { 
        public event EventHandler<TroopEventArgs> ButtonClickedEventHandler;
        
        
        private static SpecialbuttonEventManagerHandler _instance;

        private readonly WitchHunterRetinueRecruitment _witchHunterRetinueRecruitment;
        
        

        private SpecialbuttonEventManagerHandler()
        {
            _witchHunterRetinueRecruitment = new WitchHunterRetinueRecruitment();
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
        
        private void HandleBasicTroopExchanges(string troopID)
        {
            var characterTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            if (Hero.MainHero.GetCareer() == TORCareers.WitchHunter)
            {
                _witchHunterRetinueRecruitment.SetUpRetinueExchange(characterTemplate);
            }
        }
        
    }
    
    
  

    public class TroopEventArgs: EventArgs
    {
        public string TroopId { get; set; }
    }
}