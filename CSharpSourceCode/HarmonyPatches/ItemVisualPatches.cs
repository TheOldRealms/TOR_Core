using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ItemVisualPatches
    {
        private const string RightHandSubmeshName = "gor_hands_v2";
        private const string LeftHandSubmeshName = "gor_hands_v2.1";
        private const string InvisibleWeaponMaterialName = "lhm_fist_weapon";

        private const VisibilityMaskFlags HiddenMask = (VisibilityMaskFlags)0;

        private static readonly HashSet<string> KnownForearmMeshNames = new(StringComparer.OrdinalIgnoreCase);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), nameof(Mission.SpawnAgent))]
        public static void SpawnAgentPostfix(Agent __result)
        {
            AutoWieldForearmWeaponIfUnarmed(__result, isWieldedOnSpawn: true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), nameof(Agent.EquipItemsFromSpawnEquipment))]
        public static void EquipItemsFromSpawnEquipmentPostfix(Agent __instance)
        {
            RefreshAgentForearmAndHands(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), nameof(Agent.EquipWeaponWithNewEntity))]
        public static void EquipWeaponWithNewEntityPostfix(Agent __instance)
        {
            AutoWieldForearmWeaponIfUnarmed(__instance, isWieldedOnSpawn: false);
        }

        private static void AutoWieldForearmWeaponIfUnarmed(Agent agent, bool isWieldedOnSpawn)
        {
            if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Equipment == null) return;

            if (agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.None)
            {
                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    var element = agent.Equipment[i];
                    if (element.IsEmpty || element.Item == null) continue;
                    if (string.IsNullOrEmpty(element.Item.GetForearmMeshName())) continue;
                    if (element.Item.IsShield() || element.Item.IsAmmoItem()) continue;

                    agent.TryToWieldWeaponInSlot(i, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn);
                    break;
                }
            }

            if (agent.GetOffhandWieldedItemIndex() == EquipmentIndex.None)
            {
                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    var element = agent.Equipment[i];
                    if (element.IsEmpty || element.Item == null) continue;
                    if (string.IsNullOrEmpty(element.Item.GetForearmMeshName())) continue;
                    if (!element.Item.IsShield()) continue;

                    agent.TryToWieldWeaponInSlot(i, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn);
                    break;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "WeaponEquipped")]
        public static void WeaponEquippedPostfix(Agent __instance, EquipmentIndex equipmentSlot, WeakGameEntity weaponEntity)
        {
            if (__instance == null || !__instance.IsHuman || equipmentSlot < EquipmentIndex.WeaponItemBeginSlot || equipmentSlot >= EquipmentIndex.NumAllWeaponSlots) return;

            var element = __instance.Equipment[equipmentSlot];
            string forearmMeshName = element.IsEmpty || element.Item == null ? "" : element.Item.GetForearmMeshName();
            if (!string.IsNullOrEmpty(forearmMeshName) && EnsureForearmMeshOnSkeleton(__instance.AgentVisuals, forearmMeshName))
            {
                if (weaponEntity.IsValid)
                {
                    HideWeakEntityMeshesRecursive(weaponEntity);
                }
            }

            RefreshAgentForearmAndHands(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), nameof(Agent.RemoveEquippedWeapon))]
        public static void RemoveEquippedWeaponPostfix(Agent __instance)
        {
            RefreshAgentForearmAndHands(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "OnWeaponDrop")]
        public static void OnWeaponDropPostfix(Agent __instance)
        {
            RefreshAgentForearmAndHands(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AgentVisuals), "AddSkinArmorWeaponMultiMeshesToEntity")]
        public static void AddSkinArmorWeaponMultiMeshesToEntityPostfix(AgentVisuals __instance)
        {
            ApplyAgentVisualsForearmAndHands(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AgentVisuals), "ClearAndAddChangedVisualComponentsOfWeapons")]
        public static void ClearAndAddChangedVisualComponentsOfWeaponsPostfix(AgentVisuals __instance)
        {
            ApplyAgentVisualsForearmAndHands(__instance);
        }

        private static void ApplyAgentVisualsForearmAndHands(AgentVisuals agentVisuals)
        {
            var data = agentVisuals?.GetCopyAgentVisualsData();
            if (data == null || data.EquipmentData == null) return;

            var visuals = agentVisuals.GetVisuals();
            ApplyForearmReplacementFromEquipment(visuals, data.EquipmentData);

            var hideMode = data.EquipmentData.GetCombinedHideHandType();
            ApplyPerHandVisibility(visuals, hideMode);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemCollectionElementViewExtensions), nameof(ItemCollectionElementViewExtensions.OnGetWeaponData))]
        public static void OnGetWeaponDataPostfix(ref WeaponData weaponData, MissionWeapon weapon)
        {
            if (weapon.IsEmpty || weapon.Item == null) return;

            string forearmMeshName = weapon.Item.GetForearmMeshName();
            if (string.IsNullOrEmpty(forearmMeshName)) return;

            MakeWeaponMetaMeshInvisible(weaponData.WeaponMesh);
            MakeWeaponMetaMeshInvisible(weaponData.HolsterMesh);
            MakeWeaponMetaMeshInvisible(weaponData.HolsterMeshWithWeapon);
            MakeWeaponMetaMeshInvisible(weaponData.FlyingMesh);
        }

        private static void MakeWeaponMetaMeshInvisible(MetaMesh mm)
        {
            if (mm == null) return;

            Material invisibleMat = Material.GetFromResource(InvisibleWeaponMaterialName);
            if (invisibleMat != null)
            {
                mm.SetMaterial(invisibleMat);
            }

            HideMetaMeshMeshes(mm);
        }

        private static void RefreshAgentForearmAndHands(Agent agent)
        {
            if (agent == null || !agent.IsHuman || agent.AgentVisuals == null) return;

            var visuals = agent.AgentVisuals;
            if (agent.Equipment != null)
            {
                ApplyForearmReplacementFromMissionEquipment(visuals, agent.Equipment);
                ApplyPerHandVisibility(visuals, agent.Equipment.GetCombinedHideHandType());
            }
            else if (agent.SpawnEquipment != null)
            {
                ApplyForearmReplacementFromEquipment(visuals, agent.SpawnEquipment);
                ApplyPerHandVisibility(visuals, agent.SpawnEquipment.GetCombinedHideHandType());
            }
        }

        private static void ApplyForearmReplacementFromMissionEquipment(MBAgentVisuals visuals, MissionEquipment equipment)
        {
            if (visuals == null || equipment == null) return;

            HashSet<string> activeForearmNames = null;
            HashSet<string> rigidNamesToHide = null;

            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var element = equipment[i];
                if (element.IsEmpty || element.Item == null) continue;

                string forearmMeshName = element.Item.GetForearmMeshName();
                if (string.IsNullOrEmpty(forearmMeshName)) continue;

                if (!EnsureForearmMeshOnSkeleton(visuals, forearmMeshName)) continue;

                activeForearmNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                activeForearmNames.Add(forearmMeshName);

                rigidNamesToHide ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectRigidItemMeshNames(element.Item, rigidNamesToHide);
            }

            SyncInactiveForearmMeshes(visuals, activeForearmNames);

            if (rigidNamesToHide != null && rigidNamesToHide.Count > 0)
            {
                HideRigidMeshes(visuals, rigidNamesToHide);
            }
        }

        private static void ApplyForearmReplacementFromEquipment(MBAgentVisuals visuals, Equipment equipment)
        {
            if (visuals == null || equipment == null) return;

            HashSet<string> activeForearmNames = null;
            HashSet<string> rigidNamesToHide = null;

            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var element = equipment[i];
                if (element.IsEmpty || element.Item == null) continue;

                string forearmMeshName = element.Item.GetForearmMeshName();
                if (string.IsNullOrEmpty(forearmMeshName)) continue;

                if (!EnsureForearmMeshOnSkeleton(visuals, forearmMeshName)) continue;

                activeForearmNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                activeForearmNames.Add(forearmMeshName);

                rigidNamesToHide ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectRigidItemMeshNames(element.Item, rigidNamesToHide);
            }

            SyncInactiveForearmMeshes(visuals, activeForearmNames);

            if (rigidNamesToHide != null && rigidNamesToHide.Count > 0)
            {
                HideRigidMeshes(visuals, rigidNamesToHide);
            }
        }

        private static VisibilityMaskFlags ResolveVisibleSkinnedMask(Skeleton skeleton)
        {
            if (skeleton != null)
            {
                VisibilityMaskFlags fallback = HiddenMask;
                foreach (Mesh mesh in skeleton.GetAllMeshes())
                {
                    if (mesh == null) continue;
                    var mask = mesh.VisibilityMask;
                    if (mask == HiddenMask) continue;

                    if (fallback == HiddenMask)
                    {
                        fallback = mask;
                    }

                    if (!string.IsNullOrEmpty(mesh.Name))
                    {
                        string baseName = StripLodToken(mesh.Name.Trim());
                        if (!baseName.Equals(RightHandSubmeshName, StringComparison.OrdinalIgnoreCase)
                            && !baseName.Equals(LeftHandSubmeshName, StringComparison.OrdinalIgnoreCase)
                            && !KnownForearmMeshNames.Contains(baseName))
                        {
                            return mask;
                        }
                    }
                }

                if (fallback != HiddenMask)
                {
                    return fallback;
                }
            }
            return VisibilityMaskFlags.DefaultDynamic;
        }

        private static bool EnsureForearmMeshOnSkeleton(MBAgentVisuals visuals, string forearmMeshName)
        {
            if (visuals == null || string.IsNullOrEmpty(forearmMeshName)) return false;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return false;

            KnownForearmMeshNames.Add(forearmMeshName);
            VisibilityMaskFlags visibleMask = ResolveVisibleSkinnedMask(skeleton);

            bool foundExisting = false;
            foreach (Mesh existing in skeleton.GetAllMeshes())
            {
                if (existing == null || string.IsNullOrEmpty(existing.Name)) continue;
                if (StripLodToken(existing.Name.Trim()).Equals(forearmMeshName, StringComparison.OrdinalIgnoreCase))
                {
                    if (existing.VisibilityMask == HiddenMask)
                    {
                        existing.SetVisibilityMask(visibleMask);
                    }
                    foundExisting = true;
                }
            }
            if (foundExisting) return true;

            MetaMesh forearmMeta = MetaMesh.GetCopy(forearmMeshName, false, true);
            if (forearmMeta == null || forearmMeta.MeshCount == 0) return false;

            visuals.AddMultiMesh(forearmMeta, BodyMeshTypes.Invalid);
            forearmMeta.ManualInvalidate();
            return true;
        }

        private static void SyncInactiveForearmMeshes(MBAgentVisuals visuals, HashSet<string> activeForearmNames)
        {
            if (visuals == null || KnownForearmMeshNames.Count == 0) return;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return;

            foreach (Mesh mesh in skeleton.GetAllMeshes())
            {
                if (mesh == null || string.IsNullOrEmpty(mesh.Name)) continue;
                string baseName = StripLodToken(mesh.Name.Trim());
                if (!KnownForearmMeshNames.Contains(baseName)) continue;

                bool shouldBeVisible = activeForearmNames != null && activeForearmNames.Contains(baseName);
                if (!shouldBeVisible && mesh.VisibilityMask != HiddenMask)
                {
                    mesh.SetVisibilityMask(HiddenMask);
                }
            }
        }

        private static void CollectRigidItemMeshNames(ItemObject item, HashSet<string> target)
        {
            if (item == null) return;

            if (!string.IsNullOrEmpty(item.MultiMeshName))
            {
                target.Add(StripLodToken(item.MultiMeshName.Trim()));
            }

            var usedPieces = item.WeaponDesign?.UsedPieces;
            if (usedPieces != null)
            {
                foreach (var pieceElement in usedPieces)
                {
                    string pieceMesh = pieceElement?.CraftingPiece?.MeshName;
                    if (!string.IsNullOrEmpty(pieceMesh))
                    {
                        target.Add(StripLodToken(pieceMesh.Trim()));
                    }
                }
            }
        }

        private static void HideRigidMeshes(MBAgentVisuals visuals, HashSet<string> rigidNames)
        {
            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton != null)
            {
                foreach (Mesh mesh in skeleton.GetAllMeshes())
                {
                    if (mesh == null || string.IsNullOrEmpty(mesh.Name)) continue;
                    if (rigidNames.Contains(StripLodToken(mesh.Name.Trim())))
                    {
                        mesh.SetVisibilityMask(HiddenMask);
                    }
                }
            }

            GameEntity root = visuals.GetEntity();
            if (root != null)
            {
                foreach (GameEntity child in root.GetChildren())
                {
                    HideMatchingChildEntitiesRecursive(child, rigidNames);
                }
            }
        }

        private static void HideMetaMeshMeshes(MetaMesh mm)
        {
            if (mm == null) return;

            for (int i = 0; i < mm.MeshCount; i++)
            {
                Mesh sub = mm.GetMeshAtIndex(i);
                if (sub != null)
                {
                    sub.SetVisibilityMask(HiddenMask);
                }
            }
        }

        private static void HideWeakEntityMeshesRecursive(WeakGameEntity entity)
        {
            if (!entity.IsValid) return;

            int mmCount = entity.MultiMeshComponentCount;
            for (int m = 0; m < mmCount; m++)
            {
                HideMetaMeshMeshes(entity.GetMetaMesh(m));
            }

            Mesh firstMesh = entity.GetFirstMesh();
            if (firstMesh != null)
            {
                firstMesh.SetVisibilityMask(HiddenMask);
            }

            foreach (WeakGameEntity child in entity.GetChildren())
            {
                HideWeakEntityMeshesRecursive(child);
            }
        }

        private static void HideMatchingChildEntitiesRecursive(GameEntity entity, HashSet<string> rigidNames)
        {
            if (entity == null) return;

            int mmCount = entity.MultiMeshComponentCount;
            for (int m = 0; m < mmCount; m++)
            {
                MetaMesh mm = entity.GetMetaMesh(m);
                if (mm == null) continue;
                for (int i = 0; i < mm.MeshCount; i++)
                {
                    Mesh sub = mm.GetMeshAtIndex(i);
                    if (sub != null && !string.IsNullOrEmpty(sub.Name) && rigidNames.Contains(StripLodToken(sub.Name.Trim())))
                    {
                        sub.SetVisibilityMask(HiddenMask);
                    }
                }
            }

            Mesh firstMesh = entity.GetFirstMesh();
            if (firstMesh != null && !string.IsNullOrEmpty(firstMesh.Name) && rigidNames.Contains(StripLodToken(firstMesh.Name.Trim())))
            {
                firstMesh.SetVisibilityMask(HiddenMask);
            }

            foreach (GameEntity grandChild in entity.GetChildren())
            {
                HideMatchingChildEntitiesRecursive(grandChild, rigidNames);
            }
        }

        public static void ApplyPerHandVisibility(MBAgentVisuals visuals, HideHandType hideMode)
        {
            if (visuals == null) return;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return;

            VisibilityMaskFlags visibleMask = ResolveVisibleSkinnedMask(skeleton);
            bool hideRight = hideMode == HideHandType.Right || hideMode == HideHandType.Both;
            bool hideLeft = hideMode == HideHandType.Left || hideMode == HideHandType.Both;

            foreach (Mesh mesh in skeleton.GetAllMeshes())
            {
                if (mesh == null || string.IsNullOrEmpty(mesh.Name)) continue;

                string baseName = StripLodToken(mesh.Name.Trim());

                // Right Hand = "gor_hands_v2"
                bool isRightHand = baseName.Equals(RightHandSubmeshName, StringComparison.OrdinalIgnoreCase)
                                   || mesh.HasTag("right_hand");

                // Left Hand = "gor_hands_v2.1"
                bool isLeftHand = baseName.Equals(LeftHandSubmeshName, StringComparison.OrdinalIgnoreCase)
                                  || mesh.HasTag("left_hand");

                if (isRightHand)
                {
                    if (hideRight)
                    {
                        mesh.SetVisibilityMask(HiddenMask);
                    }
                    else if (mesh.VisibilityMask == HiddenMask)
                    {
                        mesh.SetVisibilityMask(visibleMask);
                    }
                }
                else if (isLeftHand)
                {
                    if (hideLeft)
                    {
                        mesh.SetVisibilityMask(HiddenMask);
                    }
                    else if (mesh.VisibilityMask == HiddenMask)
                    {
                        mesh.SetVisibilityMask(visibleMask);
                    }
                }
            }
        }

        private static string StripLodToken(string name)
        {
            string working = name;
            int idx = working.IndexOf(".lod", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) idx = working.IndexOf("_lod", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int end = idx + 4;
                while (end < working.Length && char.IsDigit(working[end])) end++;
                working = working.Remove(idx, end - idx);
            }
            return working;
        }
    }
}
