using HarmonyLib;
using SandBox.ViewModelCollection.SaveLoad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class AnimationSystemDataPatches
    {
        private static string ActionSetName = "as_human_warrior";
        private static string SkeletonName = "human_skeleton";//used during debugging, left available

        /// <summary>
        /// Copies the main character visual code from a save file into the relevant VM property and triggers parsing to store the related ActionSet name.
        /// </summary>
        /// <remarks>
        /// This SaveLoad patch is here because it works in tandem with the patch to GetHardcodedAnimationSystemDataForHumanSkeleton to fix the main hero visual preview in the SaveLoadVM.
        /// When mod version mismatches, changes in the loaded mods are detected, etc... the preview is reverted to a silouhette as the character visual code is ignored. This updates updates the relevant code for a specific SavedGameVM which is attached to the save file clicked on in the list.
        /// </remarks>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadVM), "OnSaveSelection")]
        public static void Selection(SavedGameVM save)
        {
            save.MainHeroVisualCode = save.Save.MetaData.GetCharacterVisualCode();
            ParseSkeletonAndActionSet(save.MainHeroVisualCode);
        }

        /// <summary>
        /// Finds the skeleton for the main hero attached to a save file, then deduces the appropriate action set based on TOR's nomenclature.
        /// </summary>
        /// <param name="characterCode">Code is conatenated in MainHeroSaveVisualSupplier.</param>
        public static void ParseSkeletonAndActionSet (string characterCode)
        {
            var code = characterCode;
            string[] array = code.Split('|');
            string skeletonName = "";
            
            if (int.TryParse(array[0], out int result) && 4 == result)//the 4 is a TW internal value about some sort of versioning
            {
                skeletonName = array[1];
            }
            
            if (!string.IsNullOrEmpty(skeletonName))
            {
                SkeletonName = skeletonName;
                string raceName = skeletonName.Split(['_'])[0];
                ActionSetName = "as_" + raceName + "_warrior";
                return;
            }

            ActionSetName = "as_human_warrior";
        }

        /// <summary>
        /// Creates the animation data for the SaveLoadVM hero preview with the skeleton-relevant action set.
        /// </summary>
        /// <remarks>
        /// When this is missing, non-human skeletons like dwarfs can have shoulders folded out of place and other body morphing issues.
        /// </remarks>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AnimationSystemData), "GetHardcodedAnimationSystemDataForHumanSkeleton")]
        public static void UnHardingTheCode(ref AnimationSystemData __result)
        {
            MBActionSet actionSetWithIndex = MBActionSet.GetActionSet(ActionSetName);

            AnimationSystemData result = default(AnimationSystemData);
            result.ActionSet = actionSetWithIndex;
            result.MonsterUsageSetIndex = -1;
            result.WalkingSpeedLimit = 1f;
            result.CrouchWalkingSpeedLimit = 1f;
            result.StepSize = 1f;
            result.HasClippingPlane = false;
            result.Bones = new AnimationSystemBoneData
            {
                IndicesOfRagdollBonesToCheckForCorpses = new sbyte[11]
                {
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "neck"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foretwist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foretwist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_clavicle"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
                    -1,
                    -1
                },
                CountOfRagdollBonesToCheckForCorpses = 9,
                RagdollFallSoundBoneIndices = new sbyte[4]
                {
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine2"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
                    -1
                },
                RagdollFallSoundBoneIndexCount = 3,
                HeadLookDirectionBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head"),
                SpineLowerBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
                SpineUpperBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
                ThoraxLookDirectionBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine2"),
                NeckRootBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "neck"),
                PelvisBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "pelvis"),
                RightUpperArmBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
                LeftUpperArmBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
                FallBlowDamageBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_calf"),
                TerrainDecalBone0Index = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
                TerrainDecalBone1Index = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot")
            };
            result.Biped = new AnimationSystemBoneDataBiped
            {
                RagdollStationaryCheckBoneIndices = new sbyte[8]
                {
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_upperarm_twist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_upperarm_twist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_thigh"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_thigh"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_calf"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_calf"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "pelvis"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head")
                },
                RagdollStationaryCheckBoneCount = 8,
                MoveAdderBoneIndices = new sbyte[7]
                {
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine2"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_clavicle"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "neck"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "head")
                },
                MoveAdderBoneCount = 7,
                SplashDecalBoneIndices = new sbyte[6]
                {
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_calf"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_toe0"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_calf"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_toe0")
                },
                SplashDecalBoneCount = 6,
                BloodBurstBoneIndices = new sbyte[8]
                {
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_clavicle"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foretwist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine1"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foretwist"),
                    Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "spine")
                },
                BloodBurstBoneCount = 8,
                MainHandBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_hand"),
                OffHandBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_hand"),
                MainHandItemBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_finger0"),
                OffHandItemBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_finger0"),
                MainHandItemSecondaryBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foretwist1"),
                OffHandItemSecondaryBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foretwist1"),
                OffHandShoulderBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_clavicle"),
                HandNumBonesForIk = 6,
                PrimaryFootBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
                SecondaryFootBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot"),
                RightFootIkEndEffectorBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_foot"),
                LeftFootIkEndEffectorBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_foot"),
                RightFootIkTipBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "r_toe0"),
                LeftFootIkTipBoneIndex = Skeleton.GetBoneIndexFromName(actionSetWithIndex.GetSkeletonName(), "l_toe0"),
                FootNumBonesForIk = 3
            };
            result.Quadruped = default(AnimationSystemDataQuadruped);
            __result = result;
        }
    }
}
