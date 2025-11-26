using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// This class contains all harmonypatches required for TOR custom races to work correctly
    /// </summary>
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public class RaceFixPatches
    {
        // This patch makes the created AgentVisuals use the correct action set and so the correct skeleton when it is refreshed
        // Method to avoid having to insert a bunch of instructions and instead only insert 2 (LdArg0 and Call)
        public static MBActionSet GetActionSet(BodyGeneratorView bodyGeneratorView)
        {
            var monsterName = FaceGen.GetRaceNames()[bodyGeneratorView.BodyGen.Race];
            var monster = FaceGen.GetMonster(monsterName);
            string isFemale = bodyGeneratorView.BodyGen.IsFemale ? "_female_" : "";
            return MBGlobals.GetActionSet($"as_{monsterName}{isFemale}_facegen");
        }

        public static MBActionSet GetActionSetTableau(int raceId, bool isFemale)
        {
            var monsterName = FaceGen.GetRaceNames()[raceId];
            var monster = FaceGen.GetMonster(monsterName);
            string isFemaleText = isFemale ? "_female_" : "";
            return MBGlobals.GetActionSet($"as_{monsterName}{isFemaleText}_warrior");
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BodyGeneratorView), "RefreshCharacterEntityAux")]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var newInstructions = new List<CodeInstruction>(instructions);
            var insertionIndex = -1;
            for (int i = 0; i < newInstructions.Count - 1; i++)
            {
                var instruction = newInstructions[i];
                // Find where AgentVisualsData is instantiated, and insert our new instructions after it
                if (instruction.opcode == OpCodes.Newobj && instruction.operand == AccessTools.Constructor(typeof(AgentVisualsData)))
                {
                    insertionIndex = i + 1;
                    break;
                }
            }
            if (insertionIndex < 0)
            {
                throw new ArgumentException("Cannot find instruction. Patch: RefreshCharacterEntityAuxPatch");
            }
            else
            {
                var actionSetMethod = typeof(AgentVisualsData).GetMethod(nameof(AgentVisualsData.ActionSet));
                var insertedInstructions = new List<CodeInstruction>
                {
                    // Load "this" (The BodyGeneratorView) unto the stack
                    new(OpCodes.Ldarg_0),
                    // Pass it as an argument to our static method that gets the correct action set and then puts it on the stack
                    new(OpCodes.Call, AccessTools.Method(typeof(RaceFixPatches), nameof(RaceFixPatches.GetActionSet))),
                    // equivalent to AgentVisualsData.ActionSet(RefreshCharacterEntityAuxPatch.GetActionSet(this));
                    new(OpCodes.Callvirt, actionSetMethod)
                };
                newInstructions.InsertRange(insertionIndex, insertedInstructions);
            }
            return newInstructions.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionSetCode), "GenerateActionSetNameWithSuffix")]
        public static bool OverrideForActionSet(ref string __result, Monster monster, bool isFemale, string suffix)
        {
            if (monster == null)
            {
                __result = "as_human" + (isFemale ? "_female" : "") + suffix;
            }
            var monsterId = monster.StringId;
            if (monsterId.Contains("_"))
            {
                monsterId = monsterId.Split('_')[0];
            }
            __result = "as_" + monsterId + (isFemale ? "_female" : "") + suffix;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterTableau), "RefreshCharacterTableau")]
        public static void SetEarlyActionSet(ref AgentVisuals ____oldAgentVisuals, ref AgentVisuals ____agentVisuals, int ____race)
        {
            var newdata = ____oldAgentVisuals.GetCopyAgentVisualsData();
            var raceName = FaceGen.GetRaceNames()[____race];
            newdata.ActionSet(GetActionSetTableau(____race, false)).Race(____race).Monster(FaceGen.GetMonster(raceName));
            ____oldAgentVisuals.Refresh(false, newdata, false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MetaMesh), nameof(MetaMesh.UseHeadBoneFaceGenScaling))]
        public static bool ModifyHeadBoneScalingForCustomSkeletons(Skeleton skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame)
        {
            var skeletonName = skeleton.GetName();
            if (skeletonName == "orc_skeleton2")
            {
                frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            }
            return true;
        }
    }
}