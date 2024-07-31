using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
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
    }
}