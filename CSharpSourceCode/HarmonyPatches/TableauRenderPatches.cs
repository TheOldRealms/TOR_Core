using HarmonyLib;
using SandBox.View.Map;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TOR_Core.Items;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class TableauRenderPatches
    {
        private const int ITEM_THUMBNAIL_CACHE_CAPACITY = 300;

        private static readonly FieldInfo ThumbnailCachesField =
            AccessTools.Field(typeof(ThumbnailCacheManager), "_thumbnailCaches");

        private static readonly FieldInfo ItemThumbnailCacheCapacityField =
            AccessTools.Field(typeof(ThumbnailCache<ItemThumbnailCreationData>), "_capacity");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewSubModule), "OnBeforeInitialModuleScreenSetAsRoot")]
        public static void IncreaseItemThumbnailCacheCapacity()
        {
            var thumbnailCaches = ThumbnailCachesField.GetValue(ThumbnailCacheManager.Current) as IEnumerable<IThumbnailCache>;

            foreach (var thumbnailCache in thumbnailCaches)
            {
                if (thumbnailCache is ItemThumbnailCache)
                {
                    var currentCapacity = (int)ItemThumbnailCacheCapacityField.GetValue(thumbnailCache);
                    if (currentCapacity < ITEM_THUMBNAIL_CACHE_CAPACITY)
                    {
                        ItemThumbnailCacheCapacityField.SetValue(thumbnailCache, ITEM_THUMBNAIL_CACHE_CAPACITY);
                    }

                    return;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemThumbnailCache), "GetRenderIdToUse")]
        public static bool UseOriginalThumbnailForTorDuplicatedItems(ItemThumbnailCreationData thumbnailCreationData, ref string __result)
        {
            var item = thumbnailCreationData.ItemObject;
            var renderItemId = item.StringId;

            if (ExtendedItemObjectManager.TryGetRuntimeDuplicateSourceItemId(item, out var sourceItemId))
            {
                renderItemId = sourceItemId;
            }

            __result = item.Type == ItemObject.ItemTypeEnum.Shield
                ? renderItemId + "_" + thumbnailCreationData.AdditionalArgs
                : renderItemId;

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapConversationTableau), "FirstTimeInit")]
        public static void PostfixMapConversationRender(ref Camera ____continuousRenderCamera, List<AgentVisuals> ____agentVisuals)
        {
            if (____continuousRenderCamera != null && ____agentVisuals != null && ____agentVisuals.Count > 0)
            {
                var eyePos = ____agentVisuals[0].GetGlobalStableEyePoint(true);
                var cameraFrame = ____continuousRenderCamera.Frame;
                cameraFrame.origin.z = eyePos.z - 0.15f;
                ____continuousRenderCamera.Frame = cameraFrame;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterThumbnailCache), "CreateCharacterBaseEntity")]
        public static void PostFixCreateCharacter(CharacterCode characterCode, Scene scene, ref Camera camera, bool isBig)
        {
            //do we need a depth offset to be added for orcs or other troops that are wide and can be out of shot?
            var raceName = FaceGen.GetRaceNames()[characterCode.Race];
            if (raceName == "dwarf")
            {
                var cameraFrame = camera.Frame;
                cameraFrame.origin.z -= 0.22f;
                camera.Frame = cameraFrame;
            }
            if (raceName == "goblin") //troop cards are a good height, but the preview on the left for them is still only shoulders+head
            {
                var cameraFrame = camera.Frame;
                cameraFrame.origin.z -= 0.5f;
                camera.Frame = cameraFrame;
            }
            if (raceName == "troll")
            {
                var cameraFrame = camera.Frame;
                cameraFrame.origin.z += 1.75f;
                camera.Frame = cameraFrame;
            }

            //Sly : an exception will need to be looked into for wolves as the camera for mounts assumes horse height
        }
    }
}