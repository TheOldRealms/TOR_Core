using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public static class CareerChoicesHelper
    {
        
        public static bool ArmorWeightUndershootCheck(Agent agent, float weightLimit)
        {
            if (!agent.BelongsToMainParty()) return false;
            if (!agent.IsMainAgent) return false;
            var weight = agent.Character.Equipment.GetTotalWeightOfArmor(true);
            return weight <= weightLimit;
        }
    }
}