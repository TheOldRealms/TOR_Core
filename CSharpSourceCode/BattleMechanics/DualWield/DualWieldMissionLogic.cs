using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.DualWield
{
    /// <summary>
    /// Phase 1 of the dual-wield revival. Auto-wields a second
    /// Berserk-governed attack weapon into the offhand when the
    /// mainhand already holds one, and re-wields it if the vanilla
    /// weapon rater un-wields it mid-combat.
    ///
    /// Modeled on the deleted <c>DualWieldMissionLogic</c> in
    /// <c>TOR_Core_old</c>, trimmed: no <c>AgentComponent</c>, no
    /// per-frame <c>Tick</c> state machine, fires for all agents (not
    /// just Agent.Main).
    ///
    /// Berserk governs dual-wield of ATTACK offhand weapons (two
    /// claws, two swords). Detection is the explicit
    /// <c>GovernedBy = Berserk</c> tag on <c>ExtendedItemObjectProperties</c>,
    /// NOT the presence of a forearm mesh (which would also catch
    /// defensive-claw items and other passive offhand forearm skins).
    /// See <see cref="ItemObjectExtensions.IsDualWieldItem"/>.
    ///
    /// Phase 1 acceptance: offhand attack claw stays wielded through
    /// a 30s slaangor skirmish, confirming 1.3 fixed the weapon-rater
    /// sheath-churn that killed Péter's original feature.
    /// </summary>
    public class DualWieldMissionLogic : MissionLogic
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent == null || !agent.IsHuman) return;

            agent.OnAgentWieldedItemChange += () => OnWieldedItemChange(agent);
            OnWieldedItemChange(agent);
        }

        /// <summary>
        /// Single case: if mainhand is a Berserk-governed attack
        /// weapon and the offhand is empty, search equipment slots
        /// for another Berserk-governed item and wield it offhand.
        /// Re-fires every wield-change so a weapon-rater
        /// un-wield triggers an immediate re-wield.
        /// </summary>
        private static void OnWieldedItemChange(Agent agent)
        {
            if (!agent.IsActive()) return;

            var main = agent.WieldedWeapon;
            var off = agent.WieldedOffhandWeapon;

            if (main.IsEmpty || !off.IsEmpty) return;
            if (main.Item == null || !main.Item.IsDualWieldItem()) return;

            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var el = agent.Equipment[i];
                if (el.IsEmpty || el.Item == null) continue;
                if (el.Item == main.Item) continue;
                if (!el.Item.IsDualWieldItem()) continue;

                agent.TryToWieldWeaponInSlot(i, Agent.WeaponWieldActionType.Instant, false);
                return;
            }
        }
    }
}
