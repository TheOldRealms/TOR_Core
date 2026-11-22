using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions;

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
}
