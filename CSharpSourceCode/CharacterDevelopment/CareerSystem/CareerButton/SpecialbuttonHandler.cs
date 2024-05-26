using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics
{
    public class SpecialbuttonEventManagerHandler
    { 
        private CareerButtonBehaviorBase.OnCareerButtonClickedEvent  _clickEvent;
        private CareerButtonBehaviorBase.OnShouldButtonBeVisible _shouldButtonBeVisible;
        private CareerButtonBehaviorBase.OnShouldButtonBeActive _shouldButtonBeActive;

        private PartyVM _partyVm;
        
        
        private static SpecialbuttonEventManagerHandler _instance;

        public bool IsInit;

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
        
        public void OnButtonClicked(CharacterObject troopID, bool isPrisoner)
        {
            if (_clickEvent != null)
            {
                _clickEvent.Invoke(troopID, isPrisoner);
            }
        }

        public bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
        {
            if (_shouldButtonBeVisible == null) return false;
            
            return _shouldButtonBeVisible(characterObject, isPrisoner);
        }
        
        public bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrsioner)
        {
            displayText = new TextObject();
            if (_shouldButtonBeActive == null) return false;
            
            var value =  _shouldButtonBeActive(characterObject, out displayText, isPrsioner);
            return value;
        }
    }
}