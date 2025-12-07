using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch(typeof(DefaultPartyHealingModel), "GetSurvivalChance")]
    public static class MedicineSurgeryChancePatch
    {
        private const float ORIGINAL_TROOP_LEVEL_SURVIVAL_BONUS = 0.03f;
        private const float NEW_TROOP_LEVEL_SURVIVAL_BONUS = 0.01f;

        private const float ORIGINAL_MEDICINE_SURVIVAL_BONUS = 0.01f;
        private const float NEW_MEDICINE_SURVIVAL_BONUS = 0.005f;

        private const float FLOAT_EPSILON = 0.0001f;

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = new List<CodeInstruction>(instructions);

            var hasReplacedTroopLevelEffect = false;
            var hasReplacedMedicineEffect = false;

            for (var i = 0; i < instructionList.Count; i++)
            {
                var instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float loadedFloat)
                {
                    if (!hasReplacedTroopLevelEffect &&
                        System.Math.Abs(loadedFloat - ORIGINAL_TROOP_LEVEL_SURVIVAL_BONUS) < FLOAT_EPSILON)
                    {
                        instruction.operand = NEW_TROOP_LEVEL_SURVIVAL_BONUS;
                        hasReplacedTroopLevelEffect = true;
                    }
                    else if (!hasReplacedMedicineEffect &&
                             System.Math.Abs(loadedFloat - ORIGINAL_MEDICINE_SURVIVAL_BONUS) < FLOAT_EPSILON)
                    {
                        instruction.operand = NEW_MEDICINE_SURVIVAL_BONUS;
                        hasReplacedMedicineEffect = true;
                    }
                }
            }

            return instructionList;
        }
    }
}
