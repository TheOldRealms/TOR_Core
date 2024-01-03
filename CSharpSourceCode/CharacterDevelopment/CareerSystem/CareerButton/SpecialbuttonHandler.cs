using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class SpecialbuttonEventManagerHandler
    { 
        private CareerButtonBehaviorBase.OnCareerButtonClickedEvent  _clickEvent;
        private CareerButtonBehaviorBase.OnShouldButtonBeVisible _shouldButtonBeVisible;
        private CareerButtonBehaviorBase.OnShouldButtonBeActive _shouldButtonBeActive;
        
        
        private static SpecialbuttonEventManagerHandler _instance;

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
        
    }
}