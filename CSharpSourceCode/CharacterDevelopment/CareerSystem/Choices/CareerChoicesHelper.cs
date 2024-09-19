using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public static class CareerChoicesHelper
    {
        
        public static bool ArmorWeightUndershootCheck(Agent agent, float weightLimit)
        {
            if (agent == null) return false;
            if (!agent.BelongsToMainParty()) return false;
            if (!agent.IsHero) return false;
            var model =  (TORAgentStatCalculateModel) MissionGameModels.Current.AgentStatCalculateModel;
            var encumbrance = model?.GetEffectiveArmorEncumbrance(agent, agent.SpawnEquipment);
            return encumbrance <= weightLimit;
        }


        public static bool ContainsSpellType(AbilityComponent component, int spellCount, AbilityEffectType excludedEffectType)
        {
            var wrongSpell = false;
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
            var wrongSpell = false;
            if (component == null) return false;
            for (int i = 0; i < spellCount; i++)
            {
                var ability = component.GetAbility(i);
                if(ability.Template==null) continue;
                if (ability.Template.AbilityTargetType == excludedTargetType)
                {
                    return true;
                }
            }
            return false;
        }
        
        public static bool ContainsSpellType(AbilityComponent component, AbilityTargetType[] excludedTargetTypes)
        {
            var wrongSpell = false;
            if (component == null) return false;
            var spellCount = component.KnownAbilitySystem.Count;
            for (int i = 0; i < spellCount; i++)
            {
                var ability = component.GetAbility(i);
                if(ability is CareerAbility) continue;
                if(ability.Template==null) continue;
                
                if(excludedTargetTypes.AnyQ(x=> x == ability.Template.AbilityTargetType ))
                {
                    return true;
                }
            }
            return false;
        }
    }
}