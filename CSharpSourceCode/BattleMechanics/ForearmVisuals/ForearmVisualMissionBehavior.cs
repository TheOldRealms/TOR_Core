using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.BattleMechanics.ForearmVisuals
{
    /// <summary>
    /// Grafts a replacement forearm mesh onto agents wielding items that
    /// declare one in <c>ExtendedItemObjectProperties.ForearmMesh</c>, and
    /// hides the item's own rigid weapon mesh plus the matching vanilla
    /// hand submesh (via <c>HideHand</c>).
    ///
    /// Visual-equivalent of what <c>ItemVisualPatches</c> used to do with
    /// nine Harmony postfixes. The one remaining Harmony patch
    /// (<see cref="TOR_Core.HarmonyPatches.ForearmInventoryIconPatch"/>)
    /// handles only the inventory / world item-icon case, which has no
    /// MissionBehavior seam.
    ///
    /// All forearm-name bookkeeping is now instance state cleared at
    /// mission teardown, replacing the former static
    /// <c>KnownForearmMeshNames</c> that leaked slaangor mesh names into
    /// unrelated missions.
    /// </summary>
    public class ForearmVisualMissionBehavior : MissionLogic
    {
        private const string RightHandSubmeshName = "gor_hands_v2";
        private const string LeftHandSubmeshName = "gor_hands_v2.1";
        private const string InvisibleWeaponMaterialName = "lhm_fist_weapon";
        private const VisibilityMaskFlags HiddenMask = (VisibilityMaskFlags)0;

        private readonly HashSet<string> _knownForearmMeshNames = new(StringComparer.OrdinalIgnoreCase);

        public override void AfterStart()
        {
            base.AfterStart();
            if (Mission.Current == null) return;
            Mission.Current.OnItemPickUp += OnItemPickUp;
            Mission.Current.OnItemDrop += OnItemDrop;
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            if (Mission.Current != null)
            {
                Mission.Current.OnItemPickUp -= OnItemPickUp;
                Mission.Current.OnItemDrop -= OnItemDrop;
            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent == null || !agent.IsHuman) return;

            MaybeAutoWieldForearmOnce(agent);
            RefreshAgentForearmAndHands(agent);
            agent.OnAgentWieldedItemChange += () => RefreshAgentForearmAndHands(agent);
        }

        private void OnItemPickUp(Agent agent, SpawnedItemEntity item) => RefreshAgentForearmAndHands(agent);

        private void OnItemDrop(Agent agent, SpawnedItemEntity item) => RefreshAgentForearmAndHands(agent);

        /// <summary>
        /// One-shot at <see cref="OnAgentBuild"/>: if vanilla spawn
        /// equipment selection left the agent empty-handed but a weapon
        /// slot holds a forearm-mesh item, wield it. Previously this ran
        /// again on every <c>EquipWeaponWithNewEntity</c>; doing so means
        /// a hero who ever swaps a claw alongside a normal weapon has the
        /// claw force-wielded back, which overrides player intent. One
        /// shot at build only.
        /// </summary>
        private void MaybeAutoWieldForearmOnce(Agent agent)
        {
            if (!agent.IsActive() || agent.Equipment == null) return;

            if (agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.None)
            {
                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    var element = agent.Equipment[i];
                    if (element.IsEmpty || element.Item == null) continue;
                    if (string.IsNullOrEmpty(element.Item.GetForearmMeshName())) continue;
                    if (element.Item.IsShield() || element.Item.IsAmmoItem()) continue;

                    agent.TryToWieldWeaponInSlot(i, Agent.WeaponWieldActionType.Instant, true);
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

                    agent.TryToWieldWeaponInSlot(i, Agent.WeaponWieldActionType.Instant, true);
                    break;
                }
            }
        }

        /// <summary>
        /// Unified refresh: recomputes which forearm meshes should be
        /// visible given the agent's current weapon slots, syncs the
        /// skeleton mesh visibility, and hides the vanilla hand submesh
        /// on the side(s) a claw is active.
        /// </summary>
        private void RefreshAgentForearmAndHands(Agent agent)
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

        private void ApplyForearmReplacementFromMissionEquipment(MBAgentVisuals visuals, MissionEquipment equipment)
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

        private void ApplyForearmReplacementFromEquipment(MBAgentVisuals visuals, Equipment equipment)
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

        /// <summary>
        /// Picks the visibility mask the skeleton's other skinned meshes
        /// are currently rendered with so a newly-grafted forearm mesh
        /// matches the rest of the body (shadow casting etc.). Hand-
        /// submesh names and already-registered forearm names are
        /// skipped so a partially-hidden rig doesn't bias the pick.
        /// </summary>
        private VisibilityMaskFlags ResolveVisibleSkinnedMask(Skeleton skeleton)
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
                            && !_knownForearmMeshNames.Contains(baseName))
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

        private bool EnsureForearmMeshOnSkeleton(MBAgentVisuals visuals, string forearmMeshName)
        {
            if (visuals == null || string.IsNullOrEmpty(forearmMeshName)) return false;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return false;

            _knownForearmMeshNames.Add(forearmMeshName);
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

        /// <summary>
        /// Hides any forearm mesh previously grafted onto this skeleton
        /// that isn't active in the current equipment slot set. Fixes
        /// the case where swapping away from a claw would leave the
        /// previous forearm mesh floating on the rig.
        /// </summary>
        private void SyncInactiveForearmMeshes(MBAgentVisuals visuals, HashSet<string> activeForearmNames)
        {
            if (visuals == null || _knownForearmMeshNames.Count == 0) return;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return;

            foreach (Mesh mesh in skeleton.GetAllMeshes())
            {
                if (mesh == null || string.IsNullOrEmpty(mesh.Name)) continue;
                string baseName = StripLodToken(mesh.Name.Trim());
                if (!_knownForearmMeshNames.Contains(baseName)) continue;

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

        /// <summary>
        /// Hides the vanilla hand submesh on the side(s) where a claw
        /// is active, and restores visibility on the side(s) where it
        /// isn't. Beastmen-rig literals preserved pending Task 4 (which
        /// drops the literals in favour of mesh tags).
        /// </summary>
        private void ApplyPerHandVisibility(MBAgentVisuals visuals, HideHandType hideMode)
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

                bool isRightHand = baseName.Equals(RightHandSubmeshName, StringComparison.OrdinalIgnoreCase)
                                   || mesh.HasTag("right_hand");

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
