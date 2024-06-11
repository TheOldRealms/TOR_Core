using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class WaywatcherCareerButtonBehavior : CareerButtonBehaviorBase
{
    private string _shiftshiverShardsIcon = "CareerSystem\\azyr";
    private string _hagbaneTipps = "CareerSystem\\ghur";
    private string _starfireShafts = "CareerSystem\\aqshy";
    public WaywatcherCareerButtonBehavior(CareerObject career) : base(career)
    {
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false)
    {
        throw new System.NotImplementedException();
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
    {
        if (characterObject.IsHero) return false;

        if (!characterObject.IsRanged) return false;

        if (isPrisoner) return false;
        
        if(ch)
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
    {
        throw new System.NotImplementedException();
    }
}