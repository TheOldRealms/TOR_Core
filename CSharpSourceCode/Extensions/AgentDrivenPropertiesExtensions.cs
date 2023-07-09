using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;

namespace TOR_Core.Extensions
{
    public static class AgentDrivenPropertiesExtensions
    {
        public static void SetDynamicCombatProperties(this AgentDrivenProperties agentDrivenProperties, StatusEffectComponent statusEffectComponent, float multiplier)
        {
            agentDrivenProperties.SwingSpeedMultiplier = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.ThrustOrRangedReadySpeedMultiplier) * multiplier;
            agentDrivenProperties.SwingSpeedMultiplier = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.SwingSpeedMultiplier) * multiplier;
        }

        public static void SetDynamicHumanoidMovementProperties(this AgentDrivenProperties agentDrivenProperties, StatusEffectComponent statusEffectComponent, float multiplier)
        {
            agentDrivenProperties.MaxSpeedMultiplier = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MaxSpeedMultiplier) * multiplier;
        }

        public static void SetDynamicMountMovementProperties(this AgentDrivenProperties agentDrivenProperties, StatusEffectComponent statusEffectComponent, float multiplier)
        {
            agentDrivenProperties.MountSpeed = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MountSpeed) * multiplier;
            agentDrivenProperties.MountDashAccelerationMultiplier = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MountDashAccelerationMultiplier) * multiplier;
            agentDrivenProperties.MountManeuver = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MountManeuver) * multiplier;
        }
    }
}