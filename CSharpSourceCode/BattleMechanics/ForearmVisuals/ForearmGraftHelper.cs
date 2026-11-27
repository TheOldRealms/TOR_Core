using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.BattleMechanics.ForearmVisuals
{
    /// <summary>
    /// Static primitives for the forearm-mesh graft system. Consumed by
    /// both <see cref="ForearmVisualMissionBehavior"/> (mission context)
    /// and <c>CharacterTableauForearmPatch</c> (character / inventory /
    /// party preview screens). Preview callers pass a null tracking set
    /// because the vanilla <c>AgentVisuals.Refresh</c> rebuilds the
    /// skeleton on each tableau refresh, so no cross-wield state is
    /// needed; mission callers own a <c>HashSet&lt;string&gt;</c> and
    /// pass it in for the inactive-forearm cleanup pass.
    /// </summary>
    public static class ForearmGraftHelper
    {
        private const VisibilityMaskFlags HiddenMask = (VisibilityMaskFlags)0;

        /// <summary>
        /// Applies forearm replacements for every weapon slot in an
        /// <see cref="Equipment"/> (spawn-equipment variant). Returns
        /// <c>true</c> if any declared forearm mesh failed to resolve.
        /// </summary>
        public static bool ApplyFromEquipment(MBAgentVisuals visuals, Equipment equipment, HashSet<string> trackingForearmNames = null)
        {
            if (visuals == null || equipment == null) return false;

            HashSet<string> activeForearmNames = null;
            HashSet<string> rigidNamesToHide = null;
            bool anyFailed = false;

            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var element = equipment[i];
                if (element.IsEmpty || element.Item == null) continue;

                string forearmMeshName = element.Item.GetForearmMeshName();
                if (string.IsNullOrEmpty(forearmMeshName)) continue;

                if (!EnsureForearmMeshOnSkeleton(visuals, forearmMeshName, trackingForearmNames))
                {
                    anyFailed = true;
                    continue;
                }

                activeForearmNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                activeForearmNames.Add(forearmMeshName);

                rigidNamesToHide ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectRigidItemMeshNames(element.Item, rigidNamesToHide);
            }

            if (trackingForearmNames != null)
            {
                SyncInactiveForearmMeshes(visuals, activeForearmNames, trackingForearmNames);
            }

            if (rigidNamesToHide != null && rigidNamesToHide.Count > 0)
            {
                HideRigidMeshes(visuals, rigidNamesToHide);
            }

            return anyFailed;
        }

        /// <summary>
        /// Mission-equipment variant of <see cref="ApplyFromEquipment"/>.
        /// When an <paramref name="agent"/> is supplied, rigid mesh hides
        /// are scoped to the specific weapon entity on the arm that
        /// receives the forearm graft (via
        /// <c>Agent.GetWeaponEntityFromEquipmentSlot</c>), leaving an
        /// offhand-wielded copy of the same item visible. Needed for
        /// dual-wielded claws where mainhand and offhand share one
        /// rigid mesh name: the global hide would blank both arms.
        /// </summary>
        public static bool ApplyFromMissionEquipment(MBAgentVisuals visuals, MissionEquipment equipment, Agent agent = null, HashSet<string> trackingForearmNames = null)
        {
            if (visuals == null || equipment == null) return false;

            HashSet<string> activeForearmNames = null;
            HashSet<string> rigidNamesToHide = null;
            List<EquipmentIndex> graftedSlots = null;
            bool anyFailed = false;

            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var element = equipment[i];
                if (element.IsEmpty || element.Item == null) continue;

                string forearmMeshName = element.Item.GetForearmMeshName();
                if (string.IsNullOrEmpty(forearmMeshName)) continue;

                if (!EnsureForearmMeshOnSkeleton(visuals, forearmMeshName, trackingForearmNames))
                {
                    anyFailed = true;
                    continue;
                }

                activeForearmNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                activeForearmNames.Add(forearmMeshName);

                // Offhand-side slots (1 = vanilla shield, 4 = banner slot
                // used by Yotthani's dual-wield reshuffle) share the
                // mainhand item's rigid mesh name in a two-claw setup.
                // Hiding both would leave the offhand arm empty, so skip
                // the rigid-hide bookkeeping for those slots.
                bool offhandSide = i == EquipmentIndex.Weapon1 || i == EquipmentIndex.ExtraWeaponSlot;
                if (offhandSide) continue;

                rigidNamesToHide ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectRigidItemMeshNames(element.Item, rigidNamesToHide);

                graftedSlots ??= new List<EquipmentIndex>();
                graftedSlots.Add(i);
            }

            if (trackingForearmNames != null)
            {
                SyncInactiveForearmMeshes(visuals, activeForearmNames, trackingForearmNames);
            }

            if (rigidNamesToHide != null && rigidNamesToHide.Count > 0)
            {
                if (agent != null && graftedSlots != null)
                {
                    HideRigidMeshesOnGraftedSlotEntities(visuals, agent, graftedSlots, rigidNamesToHide);
                }
                else
                {
                    HideRigidMeshes(visuals, rigidNamesToHide);
                }
            }

            return anyFailed;
        }

        /// <summary>
        /// Hides the vanilla hand submesh on the side(s) where a claw
        /// is active, and restores visibility on the side(s) where it
        /// isn't. Identification is tag-based (<c>right_hand</c> /
        /// <c>left_hand</c> on the rig's hand meshes); rigs without
        /// those tags are a content issue, not a code fallback.
        /// </summary>
        private static int _handLogged;

        public static void ApplyPerHandVisibility(MBAgentVisuals visuals, HideHandType hideMode, HashSet<string> trackingForearmNames = null)
        {
            if (visuals == null) return;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return;

            VisibilityMaskFlags visibleMask = ResolveVisibleSkinnedMask(skeleton, trackingForearmNames);
            bool hideRight = hideMode == HideHandType.Right || hideMode == HideHandType.Both;
            bool hideLeft = hideMode == HideHandType.Left || hideMode == HideHandType.Both;

            int rightFound = 0, leftFound = 0;
            // Always collect for claw-hide (hideMode != None) events, cap
            // only neutral refreshes to keep log readable.
            bool forceLog = hideMode != HideHandType.None;
            string allMeshNames = (forceLog || _handLogged < 10) ? "" : null;

            // Rigs rarely tag hand submeshes; the vanilla and TOR races
            // (cultist, gor2/slaangor, etc.) use mesh naming like
            // 'hands_male_a' / 'gor_hands_v2' / 'gor_hands_v2.1' with
            // no tag. Fall back to name match -- "hands" (plural) is
            // narrow enough to miss "handguard" / "handle".
            int nameHandsHidden = 0;
            foreach (Mesh mesh in skeleton.GetAllMeshes())
            {
                if (mesh == null) continue;

                bool isRightHand = mesh.HasTag("right_hand");
                bool isLeftHand = mesh.HasTag("left_hand");
                bool isNameHands = !string.IsNullOrEmpty(mesh.Name)
                    && mesh.Name.IndexOf("hands", StringComparison.OrdinalIgnoreCase) >= 0
                    && mesh.Name.IndexOf("handguard", StringComparison.OrdinalIgnoreCase) < 0;

                if (allMeshNames != null)
                {
                    string tagFlag = isRightHand ? "[RH]" : isLeftHand ? "[LH]" : (isNameHands ? "[NH]" : "");
                    allMeshNames += $"{mesh.Name}{tagFlag},";
                }

                if (isRightHand)
                {
                    rightFound++;
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
                    leftFound++;
                    if (hideLeft)
                    {
                        mesh.SetVisibilityMask(HiddenMask);
                    }
                    else if (mesh.VisibilityMask == HiddenMask)
                    {
                        mesh.SetVisibilityMask(visibleMask);
                    }
                }
                else if (isNameHands)
                {
                    // Untagged hand submesh (common case). Hide when
                    // hideMode is Both. For single-hand hides we lack
                    // the discriminator; the forearm graft covering the
                    // right arm is enough in the one-claw case.
                    if (hideRight && hideLeft)
                    {
                        mesh.SetVisibilityMask(HiddenMask);
                        nameHandsHidden++;
                    }
                }
            }

            if (forceLog)
            {
                TOR_Core.Utilities.TORCommon.Log($"[Claw.Hands] name-fallback hid {nameHandsHidden} 'hands' meshes (hideMode={hideMode})", NLog.LogLevel.Info);
            }

            if (allMeshNames != null)
            {
                _handLogged++;
                TOR_Core.Utilities.TORCommon.Log($"[Claw.Hands] hideMode={hideMode} rightFound={rightFound} leftFound={leftFound} meshes=[{allMeshNames}]", NLog.LogLevel.Info);
            }
        }

        /// <summary>
        /// Picks the visibility mask the skeleton's other skinned meshes
        /// are currently rendered with so a newly-grafted forearm mesh
        /// matches the rest of the body (shadow casting etc.). Hand
        /// submeshes are skipped. If a tracking set is provided,
        /// previously-grafted forearm meshes are skipped too.
        /// </summary>
        public static VisibilityMaskFlags ResolveVisibleSkinnedMask(Skeleton skeleton, HashSet<string> trackingForearmNames = null)
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
                        bool isHandMesh = mesh.HasTag("right_hand") || mesh.HasTag("left_hand");
                        bool isTrackedForearm = trackingForearmNames != null && trackingForearmNames.Contains(baseName);
                        if (!isHandMesh && !isTrackedForearm)
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

        /// <summary>
        /// Resolves a named <see cref="MetaMesh"/> and either re-shows
        /// an existing instance on the skeleton or adds it fresh via
        /// <c>AddMultiMesh</c>. Writes the mesh name into the tracking
        /// set when provided. Returns <c>false</c> if the mesh name
        /// cannot be resolved as a loadable MetaMesh.
        /// </summary>
        public static bool EnsureForearmMeshOnSkeleton(MBAgentVisuals visuals, string forearmMeshName, HashSet<string> trackingForearmNames = null)
        {
            if (visuals == null || string.IsNullOrEmpty(forearmMeshName)) return false;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return false;

            trackingForearmNames?.Add(forearmMeshName);
            VisibilityMaskFlags visibleMask = ResolveVisibleSkinnedMask(skeleton, trackingForearmNames);

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
        /// previous forearm mesh floating on the rig. Only used in the
        /// mission path; preview rebuilds visuals from scratch on each
        /// refresh so this step is skipped there.
        /// </summary>
        public static void SyncInactiveForearmMeshes(MBAgentVisuals visuals, HashSet<string> activeForearmNames, HashSet<string> trackingForearmNames)
        {
            if (visuals == null || trackingForearmNames == null || trackingForearmNames.Count == 0) return;

            Skeleton skeleton = visuals.GetSkeleton();
            if (skeleton == null) return;

            foreach (Mesh mesh in skeleton.GetAllMeshes())
            {
                if (mesh == null || string.IsNullOrEmpty(mesh.Name)) continue;
                string baseName = StripLodToken(mesh.Name.Trim());
                if (!trackingForearmNames.Contains(baseName)) continue;

                bool shouldBeVisible = activeForearmNames != null && activeForearmNames.Contains(baseName);
                if (!shouldBeVisible && mesh.VisibilityMask != HiddenMask)
                {
                    mesh.SetVisibilityMask(HiddenMask);
                }
            }
        }

        public static void CollectRigidItemMeshNames(ItemObject item, HashSet<string> target)
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

        public static void HideRigidMeshes(MBAgentVisuals visuals, HashSet<string> rigidNames)
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

        /// <summary>
        /// Agent-scoped rigid-mesh hide: for each slot whose item got a
        /// forearm graft, hides only the meshes on that slot's own
        /// <c>GetWeaponEntityFromEquipmentSlot</c> entity. Also walks the
        /// agent's body skeleton (holstered / native-weapon copies live
        /// there). An offhand-wielded sibling with the same rigid mesh
        /// name keeps its native visuals — the dual-wield case this
        /// method exists for.
        /// </summary>
        public static void HideRigidMeshesOnGraftedSlotEntities(MBAgentVisuals visuals, Agent agent, List<EquipmentIndex> graftedSlots, HashSet<string> rigidNames)
        {
            if (visuals == null || agent == null || graftedSlots == null || rigidNames == null || rigidNames.Count == 0)
            {
                return;
            }

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

            foreach (var slot in graftedSlots)
            {
                WeakGameEntity weak = agent.GetWeaponEntityFromEquipmentSlot(slot);
                if (!weak.IsValid) continue;

                GameEntity weaponEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weak);
                if (weaponEntity == null) continue;

                HideMatchingChildEntitiesRecursive(weaponEntity, rigidNames);
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

        public static string StripLodToken(string name)
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
