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
using TOR_Core.CharacterDevelopment.CareerSystem.Button;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class SpecialbuttonEventManagerHandler
    { 
        public event EventHandler<TroopEventArgs> ButtonClickedEventHandler;
        
        private CareerButtonBehaviorBase.OnCareerButtonClickedEvent  _clickEvent;
        private CareerButtonBehaviorBase.OnShouldButtonBeVisible _shouldButtonBeVisible;
        private CareerButtonBehaviorBase.OnShouldButtonBeActive _shouldButtonBeActive;
        
        
        private static SpecialbuttonEventManagerHandler _instance;

        private readonly WitchHunterRetinueRecruitment _witchHunterRetinueRecruitment;
        


        private SpecialbuttonEventManagerHandler()
        {
            _witchHunterRetinueRecruitment = new WitchHunterRetinueRecruitment();
        }

        public void RegisterNewButton(CareerButtonBehaviorBase buttonBehavior)
        {
         
            _shouldButtonBeActive = buttonBehavior.ShouldButtonBeActive;
            _shouldButtonBeVisible = buttonBehavior.ShouldButtonBeVisible;
            _clickEvent = buttonBehavior.ButtonClickedEvent;
        }

        public void Disable()
        {
            _shouldButtonBeActive = null;
            _shouldButtonBeVisible = null;
            _clickEvent = null;
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
        
        
        public void OnButtonClicked(CharacterObject troopID)
        {
            if (_clickEvent != null)
            {
                _clickEvent.Invoke(troopID);
            }
        }

        public bool ShouldButtonBeVisible(CharacterObject characterObject)
        {
            if (_shouldButtonBeVisible == null) return false;
            
            return _shouldButtonBeVisible(characterObject);
        }
        
        public bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText )
        {
            displayText = new TextObject();
            if (_shouldButtonBeActive == null) return false;
            
            var value =  _shouldButtonBeActive(characterObject, out displayText);
            return value;
        }
        
        private void HandleBasicTroopExchanges(string troopID)
        {
            
            /*var characterTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopID);
            if (Hero.MainHero.GetCareer() == TORCareers.WitchHunter)
            {
                _witchHunterRetinueRecruitment.SetUpRetinueExchange(characterTemplate);
            }*/
        }
        
    }
    
    
  

    public class TroopEventArgs: EventArgs
    {
        public string TroopId { get; set; }
    }
}