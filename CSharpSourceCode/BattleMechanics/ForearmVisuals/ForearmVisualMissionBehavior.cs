using System;
using System.Collections.Generic;
using NLog;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.ForearmVisuals
{
    /// <summary>
    /// Mission-time driver for the forearm-mesh graft system. Agents
    /// wielding items that declare an
    /// <c>ExtendedItemObjectProperties.ForearmMesh</c> get the mesh
    /// grafted onto their skeleton, the item's own rigid weapon mesh
    /// hidden, and the matching vanilla hand hidden per
    /// <c>HideHand</c>.
    ///
    /// Visual-equivalent of what <c>ItemVisualPatches</c> used to do
    /// with nine Harmony postfixes. Two Harmony patches remain on the
    /// forearm system as a whole:
    /// <see cref="TOR_Core.HarmonyPatches.ForearmInventoryIconPatch"/>
    /// (inventory / world item icon) and
    /// <c>CharacterTableauForearmPatch</c> (character / inventory /
    /// party 3D preview). Both delegate to
    /// <see cref="ForearmGraftHelper"/>.
    ///
    /// Instance state (<c>_knownForearmMeshNames</c>) scopes to a
    /// single mission and is cleared at teardown; preview callers
    /// pass a null tracking set because the vanilla tableau refresh
    /// rebuilds the skeleton each call.
    /// </summary>
    public class ForearmVisualMissionBehavior : MissionLogic
    {
        private readonly HashSet<string> _knownForearmMeshNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _loggedGraftFailureAgents = new();

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
        /// slot holds a forearm-mesh item, wield it. Previously this
        /// ran on every wield-change; doing so means a hero who ever
        /// swaps a claw alongside a normal weapon has the claw force-
        /// wielded back, which overrides player intent. One shot at
        /// build only.
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
        /// skeleton mesh visibility, and hides the vanilla hand on the
        /// side(s) a claw is active. Delegates primitives to
        /// <see cref="ForearmGraftHelper"/>; passes the instance
        /// tracking HashSet so cross-wield cleanup happens.
        /// </summary>
        private void RefreshAgentForearmAndHands(Agent agent)
        {
            if (agent == null || !agent.IsHuman || agent.AgentVisuals == null) return;

            var visuals = agent.AgentVisuals;
            bool anyGraftFailed;
            HideHandType hideMode;

            if (agent.Equipment != null)
            {
                anyGraftFailed = ForearmGraftHelper.ApplyFromMissionEquipment(visuals, agent.Equipment, agent, _knownForearmMeshNames);
                hideMode = agent.Equipment.GetCombinedHideHandType();
            }
            else if (agent.SpawnEquipment != null)
            {
                anyGraftFailed = ForearmGraftHelper.ApplyFromEquipment(visuals, agent.SpawnEquipment, _knownForearmMeshNames);
                hideMode = agent.SpawnEquipment.GetCombinedHideHandType();
            }
            else
            {
                return;
            }

            ForearmGraftHelper.ApplyPerHandVisibility(visuals, hideMode, _knownForearmMeshNames);

            if (anyGraftFailed && _loggedGraftFailureAgents.Add(agent.Index))
            {
                TORCommon.Log(
                    $"ForearmVisual: failed to graft forearm mesh on agent Index={agent.Index} Character={agent.Character?.StringId ?? "<null>"} - rig may be missing the declared MetaMesh.",
                    LogLevel.Warn);
            }
        }
    }
}
