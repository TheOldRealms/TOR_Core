using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class GrailKnightCareerButtonBehavior: CareerButtonBehaviorBase
    {
        public GrailKnightCareerButtonBehavior(CareerObject career) : base(career)
        {
        }

        public override void ButtonClickedEvent(CharacterObject character)
        {
            throw new System.NotImplementedException();
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject)
        {
            throw new System.NotImplementedException();
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText)
        {
            throw new System.NotImplementedException();
        }
    }
}