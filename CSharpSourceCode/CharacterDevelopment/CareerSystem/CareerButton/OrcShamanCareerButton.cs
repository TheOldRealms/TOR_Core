using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class OrcShamanCareerButton(CareerObject career) : GreenskinCareerButton(career)
{
    // Orc Shaman uses the base GreenskinCareerButton functionality
    // (chop prisoners for meat)
    // No additional functionality like the OrcBoss extorsion system
}
