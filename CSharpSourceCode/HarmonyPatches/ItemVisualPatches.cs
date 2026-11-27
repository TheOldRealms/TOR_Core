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

            MakeWeaponMetaMeshInvisible(weaponData.WeaponMesh);
            MakeWeaponMetaMeshInvisible(weaponData.HolsterMesh);
            MakeWeaponMetaMeshInvisible(weaponData.HolsterMeshWithWeapon);
            MakeWeaponMetaMeshInvisible(weaponData.FlyingMesh);
        }

        private static void MakeWeaponMetaMeshInvisible(MetaMesh mm)
        {
            if (mm == null) return;

            // SetVisibilityMask(0) alone isn't enough on the batched
            // weapon entities Bannerlord uses in combat -- diagnostic
            // logs show vis=0 but the mesh still renders. The original
            // 9-patch system swapped the MetaMesh's material to the
            // invisible 'lhm_fist_weapon' resource, which does hide it
            // in the render path. Both operations stay so the mesh
            // disappears in every pass.
            Material invisibleMat = Material.GetFromResource(InvisibleWeaponMaterialName);
            if (invisibleMat != null)
            {
                mm.SetMaterial(invisibleMat);
            }
            else if (!_loggedMissingMaterial)
            {
                _loggedMissingMaterial = true;
                TORCommon.Log(
                    $"ForearmInventoryIconPatch: material '{InvisibleWeaponMaterialName}' unresolved. Forearm items fall back to visibility mask only.",
                    LogLevel.Warn);
            }

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
    /// <summary>
    /// Per-equip-entity hook that hides the raw weapon entity's mesh
    /// right when the engine attaches it to a hand bone. OnAgentBuild
    /// runs BEFORE weapon entities exist, so the refresh-only path has
    /// no entity to find and the raw claw mesh survives on top of the
    /// forearm graft. Complements the material-swap in
    /// <see cref="ForearmInventoryIconPatch"/> for first-frame hide
    /// reliability.
    ///
    /// Scope: only mainhand-side slots (0, 2, 3). The offhand-side
    /// slots 1 and 4 (Yotthani's banner-slot reshuffle target) keep
    /// their native visuals so the dual-wielded claw's left-arm
    /// forearm graft is what renders in that position.
    /// </summary>
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ForearmWeaponEntityHidePatch
    {
        private const VisibilityMaskFlags HiddenMask = (VisibilityMaskFlags)0;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "WeaponEquipped")]
        public static void WeaponEquippedPostfix(Agent __instance, EquipmentIndex equipmentSlot, WeakGameEntity weaponEntity)
        {
            if (__instance == null || !__instance.IsHuman) return;
            if (equipmentSlot < EquipmentIndex.WeaponItemBeginSlot || equipmentSlot >= EquipmentIndex.NumAllWeaponSlots) return;
            if (equipmentSlot == EquipmentIndex.Weapon1 || equipmentSlot == EquipmentIndex.ExtraWeaponSlot) return;

            var element = __instance.Equipment[equipmentSlot];
            if (element.IsEmpty || element.Item == null) return;
            if (string.IsNullOrEmpty(element.Item.GetForearmMeshName())) return;

            // Postfix param weaponEntity is often invalid on the
            // natural-weapon / Yotthani-wielded path. Re-fetch via the
            // agent's live slot accessor -- that one works after the
            // attach completes.
            if (!weaponEntity.IsValid)
            {
                weaponEntity = __instance.GetWeaponEntityFromEquipmentSlot(equipmentSlot);
                if (!weaponEntity.IsValid) return;
            }

            GameEntity entity = GameEntity.CreateFromWeakEntity(weaponEntity);
            if (entity == null) return;

            HideAllMeshesRecursive(entity);
        }

        private static void HideAllMeshesRecursive(GameEntity entity)
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
                    sub?.SetVisibilityMask(HiddenMask);
                }
            }

            Mesh firstMesh = entity.GetFirstMesh();
            firstMesh?.SetVisibilityMask(HiddenMask);

            foreach (GameEntity child in entity.GetChildren())
            {
                HideAllMeshesRecursive(child);
            }
        }
    }
}
