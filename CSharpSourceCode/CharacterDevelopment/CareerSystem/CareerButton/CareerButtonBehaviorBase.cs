using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public abstract class CareerButtonBehaviorBase
    {
        
        public virtual bool isDialogStart { get; protected set; }

        public virtual void DeactivateDialog()
        {
            isDialogStart = false;
        }

        public delegate void OnCareerButtonClickedEvent(CharacterObject troopID, bool isPrisoner,bool isShiftClick);

        public delegate bool OnShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner);

        public delegate bool OnShouldButtonBeActive(CharacterObject characterObject, out TextObject deactivateCondition, bool isPrsioner);

        protected CareerButtonBehaviorBase(CareerObject career)
        {
        }

        public virtual string CareerButtonIcon => "General\\Icons\\Coin@2x";

        public void Register()
        {
            SpecialbuttonEventManagerHandler.Instance.RegisterNewButton(this);
        }
        
        public abstract void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick);

        public abstract bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner);

        public abstract bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner);
    }
}