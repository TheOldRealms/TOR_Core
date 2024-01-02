using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public abstract  class CareerButtonBehaviorBase
    {
        public delegate void OnCareerButtonClickedEvent(CharacterObject troopID);
        public delegate bool OnShouldButtonBeVisible(CharacterObject characterObject);
        public delegate bool OnShouldButtonBeActive(CharacterObject characterObject, out TextObject deactivateCondition);

        public bool IsIntialized;

        protected CareerButtonBehaviorBase(CareerObject career)
        {
            Register();
        }
        
        public virtual string CareerButtonIcon => "General\\Icons\\Coin@2x";
        
        

        public void Register()
        {
            SpecialbuttonEventManagerHandler.Instance.RegisterNewButton(this);
        }


        public abstract void ButtonClickedEvent(CharacterObject characterObject);

        public abstract bool ShouldButtonBeVisible(CharacterObject characterObject);

        public abstract bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText);
    }
}