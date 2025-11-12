using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// This class contains all harmonypatches required for TOR custom races to work correctly
    /// </summary>
    [HarmonyPatch(typeof(BodyGeneratorView), "RefreshCharacterEntityAux")]
    public class RaceFixPatches
    {
        // This patch makes the created AgentVisuals use the correct action set and so the correct skeleton when it is refreshed
        // Method to avoid having to insert a bunch of instructions and instead only insert 2 (LdArg0 and Call)
        public static MBActionSet GetActionSet(BodyGeneratorView bodyGeneratorView)
        {
            var baseMonsterFromRace = FaceGen.GetBaseMonsterFromRace(bodyGeneratorView.BodyGen.Race);
            return MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, bodyGeneratorView.BodyGen.IsFemale, "_facegen");
        }

        [HarmonyTranspiler]
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