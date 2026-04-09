using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.MountAndBlade.View.MissionViews;
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
        // thumbnail/portrait render path marker
        [ThreadStatic]
        private static int _characterThumbnailRenderDepth;

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
        private static bool IsSafeMissionFaceCacheRace(int race)
        {
            string raceName = FaceGen.GetRaceNames()[race];

            // add eonir and asrai here if you want to have a diverse look
            return raceName == "human" || raceName == "bretonnian";
        }

        // This patch makes the created AgentVisuals use the correct action set and so the correct skeleton when it is refreshed
        // Method to avoid having to insert a bunch of instructions and instead only insert 2 (LdArg0 and Call)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MissionFaceCacheView), "CheckForSimilarFacesFromCache")]
        public static bool DisableMissionFaceReuseForCustomRaces(
            FaceGenerationParams newFaceGen,
            ref int __result)
        {
            if (!IsSafeMissionFaceCacheRace(newFaceGen.CurrentRace))
            {
                __result = -1;
                return false;
            }

            return true;
        }

        private static bool CanReuseMissionFaceCache(FaceGenerationParams firstFace, FaceGenerationParams secondFace)
        {
            if (firstFace.CurrentRace == secondFace.CurrentRace)
            {
                return true;
            }

            string[] raceNames = FaceGen.GetRaceNames();
            string firstRaceName = raceNames[firstFace.CurrentRace];
            string secondRaceName = raceNames[secondFace.CurrentRace];

            // keep human bret similarities
            return (firstRaceName == "human" && secondRaceName == "bretonnian") || (firstRaceName == "bretonnian" && secondRaceName == "human");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionFaceCacheView), "ComputeSimilarityOfFace")]
        public static void BlockCrossRaceMissionFaceReuse(
            FaceGenerationParams f0,
            FaceGenerationParams f1,
            ref float __result)
        {
            // block other cross race spawns
            if (!CanReuseMissionFaceCache(f0, f1))
            {
                __result = 1000000000f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Agent), nameof(Agent.EquipItemsFromSpawnEquipment))]
        public static void DisableNativeMissionFaceCacheForCustomRaces(
            Agent __instance,
            ref bool useFaceCache)
        {
            if (!useFaceCache)
            {
                return;
            }

            var character = __instance.Character;
            if (character == null)
            {
                return;
            }

            if (!IsSafeMissionFaceCacheRace(character.Race))
            {
                useFaceCache = false;
            }
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
            
            if (monster.ActionSetCode == null)
            {
                int i = 0;
            }
            //Sly : monster ids may contain underscores that must be kept to find the base monster, eg. large_humanoid_monster must be retrievable from large_humanoid_monster_settlement_slow. Similarly, large_humanoid_monster_child should remain untouched as its base is human_child.
            //This could possibly be done differently with recursion by following the base_monster reference chain to look for an action set.
            if (monsterId.Contains("_"))
            {
                monsterId = TrimSettlementFromMonsterId(monsterId);
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

        // marks the character thumbnail render path
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterThumbnailCache), "OnCreateTexture")]
        public static void MarkCharacterThumbnailRenderStart()
        {
            _characterThumbnailRenderDepth++;
        }

        // ensures thumbnail render depth is restored in case texture creation fails
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CharacterThumbnailCache), "OnCreateTexture")]
        public static Exception MarkCharacterThumbnailRenderEnd(Exception __exception)
        {
            _characterThumbnailRenderDepth--;
            return __exception;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MetaMesh), nameof(MetaMesh.UseHeadBoneFaceGenScaling))]
        public static bool ModifyHeadBoneScalingForCustomSkeletons(Skeleton skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame)
        {
            var skeletonName = skeleton.GetName();

            if (skeletonName == "orc_skeleton2")
            {
                frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                return true;
            }

            if (skeletonName != "goblin_skeleton")
            {
                return true;
            }
            // goblin thumbnail renders must skip native scaling to avoid portrait corruption
            if (_characterThumbnailRenderDepth > 0)
            {
                return false;
            }

            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            return true;
        }

        public static string TrimSettlementFromMonsterId(string monsterId)
        {
            if (monsterId.Contains("_settlement"))
            {
                var trimmedId = "";
                foreach (var substring in monsterId.Split('_'))
                {
                    if (substring.Contains("settlement"))
                        break;

                    trimmedId = trimmedId.Add(substring, false);
                }

                return trimmedId;
            }

            return monsterId;
        }
    }
}