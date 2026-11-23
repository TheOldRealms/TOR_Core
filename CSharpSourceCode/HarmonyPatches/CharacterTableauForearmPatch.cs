using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TOR_Core.BattleMechanics.ForearmVisuals;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Mirrors the mission-time forearm graft onto the 3D preview
    /// agents used by the Character sheet (N), Inventory (I), and
    /// Party screen (P).
    ///
    /// Problem: the preview-scene Agent is built outside any Mission
    /// context, so <see cref="ForearmVisualMissionBehavior"/>'s
    /// <c>OnAgentBuild</c> never fires for it. Without this patch a
    /// slaangor companion viewed in those screens has empty arms (the
    /// claw item's own mesh is hidden by
    /// <see cref="ForearmInventoryIconPatch"/> and nothing grafts the
    /// replacement forearm back on).
    ///
    /// Why not non-patching: <c>Agent</c>, <c>AgentVisuals</c>, and
    /// <c>Scene</c> expose no public lifecycle events that fire at
    /// visuals-ready. The native <c>CharacterTableauWidget</c> that
    /// owns the preview is unhookable from C#. A ViewModel extension
    /// can't reach the Widget-internal Agent. No <c>GameModel</c>
    /// surface exists for tableau-agent visual init.
    ///
    /// Scope: one postfix on <c>CharacterTableau.RefreshCharacterTableau</c>
    /// (a method TOR already patches six times in
    /// <c>RaceFixPatches</c>, so the surface is well-trodden in the
    /// mod). Pulls the tableau's internal <c>_agentVisuals</c> and
    /// <c>_equipment</c> via Harmony field prefix and delegates the
    /// actual graft to <see cref="ForearmGraftHelper"/> with a null
    /// tracking set (the vanilla <c>AgentVisuals.Refresh</c> wipes
    /// the skeleton each call, so cross-wield cleanup isn't needed
    /// in this path).
    /// </summary>
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class CharacterTableauForearmPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterTableau), "RefreshCharacterTableau")]
        public static void ForearmGraftAfterRefresh(AgentVisuals ____agentVisuals, Equipment ____equipment)
        {
            if (____agentVisuals == null || ____equipment == null) return;

            var mbVisuals = ____agentVisuals.GetVisuals();
            if (mbVisuals == null) return;

            if (!HasAnyForearmItem(____equipment)) return;

            ForearmGraftHelper.ApplyFromEquipment(mbVisuals, ____equipment);
            ForearmGraftHelper.ApplyPerHandVisibility(mbVisuals, ____equipment.GetCombinedHideHandType());
        }

        /// <summary>
        /// Early-exits the patch when the previewed equipment has no
        /// forearm-mesh items at all. Keeps the common-case cost at
        /// one-pass-over-weapon-slots-and-done.
        /// </summary>
        private static bool HasAnyForearmItem(Equipment equipment)
        {
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var element = equipment[i];
                if (element.IsEmpty || element.Item == null) continue;
                if (!string.IsNullOrEmpty(element.Item.GetForearmMeshName())) return true;
            }
            return false;
        }
    }
}
