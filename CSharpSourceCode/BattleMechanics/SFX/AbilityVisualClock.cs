using System.Reflection;
using TaleWorlds.Engine;
using TOR_Core.AbilitySystem.Scripts;

namespace TOR_Core.BattleMechanics.SFX
{
    // Read the spell timer so its visuals stay in sync.
    internal static class AbilityVisualClock
    {
        private static readonly FieldInfo AbilityLifeField =
            typeof(AbilityScript).GetField("_abilityLife", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AdditionalDurationField =
            typeof(AbilityScript).GetField("_additionalDuration", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static AbilityScript FindOwner(WeakGameEntity prefabRoot)
        {
            var parent = prefabRoot.Parent;
            return parent.IsValid ? parent.GetFirstScriptOfType<AbilityScript>() : null;
        }

        internal static float Remaining(AbilityScript ability, float fallbackElapsed)
        {
            float duration = ability?.Ability?.Template?.Duration ?? 0f;
            float life = fallbackElapsed;
            float extra = 0f;
            if (AbilityLifeField?.GetValue(ability) is float abilityLife && abilityLife >= 0f) life = abilityLife;
            if (AdditionalDurationField?.GetValue(ability) is float additional) extra = additional;
            return duration + extra - life;
        }

        // The spell timer starts at -1 until its first update.
        internal static float Life(AbilityScript ability, float fallbackElapsed)
        {
            if (AbilityLifeField?.GetValue(ability) is float abilityLife) return abilityLife > 0f ? abilityLife : 0f;
            return fallbackElapsed;
        }
    }
}
