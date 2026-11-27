using HarmonyLib;
using NLog;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Renders the inventory- and world-icon mesh invisibly for items
    /// that declare an <c>ExtendedItemObjectProperties.ForearmMesh</c>.
    ///
    /// Problem: a claw item's "sword" mesh must not render in the item
    /// icon (inventory, loot tooltip, world-dropped pickup). Visually
    /// the weapon IS the forearm mesh grafted onto the agent, so the
    /// backing rigid mesh needs to vanish everywhere the item is drawn.
    ///
    /// Why not non-patching: all other forearm visual work happens in
    /// <see cref="TOR_Core.BattleMechanics.ForearmVisuals.ForearmVisualMissionBehavior"/>
    /// via normal MissionBehavior hooks. Item-icon rendering has no
    /// equivalent seam: <c>ItemCollectionElementView</c> resolves the
    /// mesh inline from the item data, with no event, no overridable
    /// model, and no view-model extension point.
    ///
    /// Scope: one postfix on
    /// <c>ItemCollectionElementViewExtensions.OnGetWeaponData</c>,
    /// filtered by <c>item.GetForearmMeshName() != ""</c> so items
    /// without a forearm declaration are untouched.
    /// </summary>
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ForearmInventoryIconPatch
    {
        private const string InvisibleWeaponMaterialName = "lhm_fist_weapon";
        private const VisibilityMaskFlags HiddenMask = (VisibilityMaskFlags)0;
        private static bool _loggedMissingMaterial;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemCollectionElementViewExtensions), nameof(ItemCollectionElementViewExtensions.OnGetWeaponData))]
        public static void OnGetWeaponDataPostfix(ref WeaponData weaponData, MissionWeapon weapon)
        {
            if (weapon.IsEmpty || weapon.Item == null) return;

            string forearmMeshName = weapon.Item.GetForearmMeshName();
            if (string.IsNullOrEmpty(forearmMeshName)) return;

            // OnGetWeaponData fires for inventory/world-icon renders AND
            // for in-mission wielded weapons. In the mission case, the
            // forearm graft (ForearmGraftHelper) hides the rigid mesh
            // surgically per slot so the offhand sibling stays visible.
            // Without this skip, the shared MetaMesh here would get an
            // invisible material swap that affects every render of the
            // item -- including the offhand claw, which has no graft to
            // cover it.
            if (Mission.Current != null) return;

            MakeWeaponMetaMeshInvisible(weaponData.WeaponMesh);
            MakeWeaponMetaMeshInvisible(weaponData.HolsterMesh);
            MakeWeaponMetaMeshInvisible(weaponData.HolsterMeshWithWeapon);
            MakeWeaponMetaMeshInvisible(weaponData.FlyingMesh);
        }

        private static void MakeWeaponMetaMeshInvisible(MetaMesh mm)
        {
            if (mm == null) return;

            // Material.GetFromResource returns a shared cached material.
            // Calling mm.SetMaterial with it mutated the MetaMesh for the
            // whole session, leaving every later render of the claw --
            // including in-mission offhand copies -- with no material.
            // Rely on per-instance SetVisibilityMask on the Mesh handles
            // returned by this specific MetaMesh query only.
            for (int i = 0; i < mm.MeshCount; i++)
            {
                Mesh sub = mm.GetMeshAtIndex(i);
                if (sub != null)
                {
                    sub.SetVisibilityMask(HiddenMask);
                }
            }
        }
    }

    /// <summary>
    /// Per-equip-entity hook that hides the raw weapon entity's mesh
    /// right when the engine attaches it to a hand bone. OnAgentBuild
    /// runs BEFORE weapon entities exist, so the old refresh path had
    /// no entity to find and the raw claw mesh stayed visible on top
    /// of the forearm graft (double-claw on the right hand).
    ///
    /// Scope: only mainhand-side slots (0, 2, 3). The offhand-side
    /// slots 1 and 4 (Yotthani's banner-slot reshuffle target) keep
    /// their native visuals so the dual-wielded claw shows on the
    /// left arm where no left-rigged forearm graft exists.
    /// </summary>
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ForearmWeaponEntityHidePatch
    {
        private const VisibilityMaskFlags HiddenMask = (VisibilityMaskFlags)0;
        private static int _logged;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "WeaponEquipped")]
        public static void WeaponEquippedPostfix(Agent __instance, EquipmentIndex equipmentSlot, WeakGameEntity weaponEntity)
        {
            // Always log claw-item events (bypass global cap). For non-claw
            // items, cap to prevent log spam.
            var element = __instance == null ? default : __instance.Equipment[equipmentSlot];
            bool isClawItem = element.Item != null && !string.IsNullOrEmpty(element.Item.GetForearmMeshName());
            bool logThis = isClawItem || _logged < 50;

            if (__instance == null || !__instance.IsHuman)
            {
                if (logThis) { _logged++; TORCommon.Log($"[Claw.WEq] skip: nonhuman/null agent={__instance?.Name}", LogLevel.Info); }
                return;
            }
            if (equipmentSlot < EquipmentIndex.WeaponItemBeginSlot || equipmentSlot >= EquipmentIndex.NumAllWeaponSlots)
            {
                if (logThis) { _logged++; TORCommon.Log($"[Claw.WEq] skip: out-of-range slot={(int)equipmentSlot} agent={__instance.Name}", LogLevel.Info); }
                return;
            }
            if (element.IsEmpty || element.Item == null)
            {
                if (logThis) { _logged++; TORCommon.Log($"[Claw.WEq] skip: empty slot={(int)equipmentSlot} agent={__instance.Name}", LogLevel.Info); }
                return;
            }
            string fm = element.Item.GetForearmMeshName();
            if (string.IsNullOrEmpty(fm))
            {
                if (logThis) { _logged++; TORCommon.Log($"[Claw.WEq] skip: no ForearmMesh on '{element.Item.StringId}' slot={(int)equipmentSlot} agent={__instance.Name}", LogLevel.Info); }
                return;
            }
            if (equipmentSlot == EquipmentIndex.Weapon1 || equipmentSlot == EquipmentIndex.ExtraWeaponSlot)
            {
                TORCommon.Log($"[Claw.WEq] skip: offhand-side slot={(int)equipmentSlot} item='{element.Item.StringId}' agent={__instance.Name} (leaving native visual)", LogLevel.Info);
                return;
            }
            // Postfix param weaponEntity is often invalid (natural-weapon
            // / Yotthani-wielded path). Re-fetch via the agent's live
            // slot accessor instead -- that one works after the attach
            // completes.
            if (!weaponEntity.IsValid)
            {
                weaponEntity = __instance.GetWeaponEntityFromEquipmentSlot(equipmentSlot);
                if (!weaponEntity.IsValid)
                {
                    TORCommon.Log($"[Claw.WEq] skip: no valid WeakGameEntity even via agent lookup slot={(int)equipmentSlot} item='{element.Item.StringId}' agent={__instance.Name}", LogLevel.Info);
                    return;
                }
            }

            GameEntity entity = GameEntity.CreateFromWeakEntity(weaponEntity);
            if (entity == null)
            {
                TORCommon.Log($"[Claw.WEq] skip: CreateFromWeakEntity returned null slot={(int)equipmentSlot} agent={__instance.Name}", LogLevel.Info);
                return;
            }

            int hidden = HideAllMeshesRecursive(entity, 0);
            TORCommon.Log($"[Claw.WEq] HIDE slot={(int)equipmentSlot} item='{element.Item.StringId}' agent={__instance.Name} meshesHidden={hidden}", LogLevel.Info);
        }

        private static int HideAllMeshesRecursive(GameEntity entity, int counter)
        {
            if (entity == null) return counter;

            int mmCount = entity.MultiMeshComponentCount;
            for (int m = 0; m < mmCount; m++)
            {
                MetaMesh mm = entity.GetMetaMesh(m);
                if (mm == null) continue;
                for (int i = 0; i < mm.MeshCount; i++)
                {
                    Mesh sub = mm.GetMeshAtIndex(i);
                    if (sub != null)
                    {
                        sub.SetVisibilityMask(HiddenMask);
                        counter++;
                    }
                }
            }

            Mesh firstMesh = entity.GetFirstMesh();
            if (firstMesh != null)
            {
                firstMesh.SetVisibilityMask(HiddenMask);
                counter++;
            }

            foreach (GameEntity child in entity.GetChildren())
            {
                counter = HideAllMeshesRecursive(child, counter);
            }

            return counter;
        }
    }
}
