using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbilityHUD_VM : AbilityHUD_VM
    {
        protected override Ability SelectAbility()
        {
            var ability = Agent.Main.GetCareerAbility();
            IsVisible = ability != null && (Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Stealth);
            return ability;
        }
    }
}
