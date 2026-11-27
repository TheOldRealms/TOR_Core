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
        private static int _refreshLogged;
        private static int _rootTreeLogged;

        private static string DescribeEntityTree(TaleWorlds.Engine.GameEntity entity, int depth)
        {
            if (entity == null) return new string(' ', depth * 2) + "(null)";
            var sb = new System.Text.StringBuilder();
            string indent = new string(' ', depth * 2);
            string name = "";
            try { name = entity.Name ?? ""; } catch { }
            int mmCount = 0;
            try { mmCount = entity.MultiMeshComponentCount; } catch { }
            sb.Append(indent).Append("entity '").Append(name).Append("' mmCount=").Append(mmCount);

            for (int m = 0; m < mmCount; m++)
            {
                var mm = entity.GetMetaMesh(m);
                if (mm == null) continue;
                for (int i = 0; i < mm.MeshCount; i++)
                {
                    var sub = mm.GetMeshAtIndex(i);
                    if (sub == null || string.IsNullOrEmpty(sub.Name)) continue;
                    sb.Append(" mesh='").Append(sub.Name).Append("'/vis=").Append((int)sub.VisibilityMask);
                }
            }
            var first = entity.GetFirstMesh();
            if (first != null && !string.IsNullOrEmpty(first.Name))
            {
                sb.Append(" first='").Append(first.Name).Append("'/vis=").Append((int)first.VisibilityMask);
            }

            foreach (var child in entity.GetChildren())
            {
                sb.Append('\n').Append(DescribeEntityTree(child, depth + 1));
            }
            return sb.ToString();
        }

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

            if (agent.Equipment != null)
            {
                bool anyClaw = false;
                string slots = "";
                for (var i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    var el = agent.Equipment[i];
                    if (el.IsEmpty || el.Item == null) continue;
                    var mode = el.Item.GetHideHandType();
                    if (mode != HideHandType.None) anyClaw = true;
                    slots += $" s{(int)i}='{el.Item.StringId}'/HH={mode}";

                    // Log once per claw-equipped refresh.
                    if (mode != HideHandType.None && _rootTreeLogged < 10)
                    {
                        _rootTreeLogged++;
                        var root = agent.AgentVisuals?.GetEntity();
                        if (root != null)
                        {
                            string tree = DescribeEntityTree(root, 0);
                            TORCommon.Log($"[Claw.RootTree] agent={agent.Name} ENTIRE agent root tree:\n{tree}", LogLevel.Info);
                        }
                        break;
                    }
                }
                if (anyClaw || _refreshLogged < 50)
                {
                    _refreshLogged++;
                    TORCommon.Log($"[Claw.Refresh] agent={agent.Name} combinedHideMode={hideMode} slots:{slots}", LogLevel.Info);
                }
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
