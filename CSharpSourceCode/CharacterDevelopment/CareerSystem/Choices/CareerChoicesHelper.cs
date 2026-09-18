using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public static class CareerChoicesHelper
    {

        public static bool ArmorWeightCheck(Agent agent, float targetWeight, bool undershoot = true)
        {
            if (agent == null) return false;
            if (!agent.IsHero) return false;
            if (!agent.BelongsToMainParty()) return false;
            var model = (TORAgentStatCalculateModel)MissionGameModels.Current.AgentStatCalculateModel;
            var encumbrance = model?.GetEffectiveArmorEncumbrance(agent, agent.SpawnEquipment);

            if (undershoot)
            {
                return encumbrance <= targetWeight;
            }
            else
            {
                return encumbrance >= targetWeight;
            }
        }

        public static bool HealthLostCheck(Agent agent, float healthLost)
        {
            if (agent == null) return false;
            return agent.HealthLimit - agent.Health >= healthLost;
        }

        public static bool ContainsSpellType(AbilityComponent component, int spellCount, AbilityEffectType excludedEffectType)
        {
            //TODO: Why not use linq?
            for (int i = 0; i < spellCount; i++)
            {
                var ability = component.GetAbility(i);
                if (ability.AbilityEffectType == excludedEffectType)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContainsSpellType(AbilityComponent component, int spellCount, AbilityTargetType excludedTargetType)
        {

            if (component == null) return false;
            for (int i = 0; i < spellCount; i++)
            {
                var ability = component.GetAbility(i);
                if (ability.Template == null) continue;
                if (ability.Template.AbilityTargetType == excludedTargetType)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Detects the presence of an unwanted AbilityTargetType in the agent's known ability list for the relevant AbilityType.
        /// </summary>
        /// <returns>
        /// True when an excluded ability is found.
        /// </returns>
        public static bool ContainsAbilityType(AbilityComponent component, AbilityType checkedType, AbilityTargetType[] excludedTargetTypes)
        {
            if (component == null) return false;
            var spellCount = component.KnownAbilitySystem.Count;
            for (int i = 0; i < spellCount; i++)
            {
                var ability = component.GetAbility(i);

                if (ability.Template?.AbilityType != checkedType) continue;

                if (excludedTargetTypes.AnyQ(x => x == ability.Template.AbilityTargetType))
                {
                    return true;
                }
            }
            return false;
        }
    }
}