using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Usables;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics
{
    public class TORMonsterSiegeLogic : MissionLogic
    {
        internal const float GateDamageMultiplier = 7f; // gate damage of the monstrous units

        // their target agents right after they destory the gate. meant to prevent them from getting stuck trying to climb staircases
        private const float LocalEnemySearchRadius = 15f;
        private const float MaxSameLevelHeightDifference = 1.5f;

        // they will be pushing straight(ish) after destorying the gate, it's width
        private const float BreachHalfWidth = 1.50f;

        // target strictly post breach for this amount of time
        private const float PostBreachFilterDuration = 2.1f;
        private float _breachWidthFilterTimeRemaining;

        private MatrixFrame _lastGateFrame;
        private bool _hasLastGateFrame;
        private bool _pendingGateStateRefresh;

        // overridden agents after the deployment
        private readonly List<Agent> _forcedGateAgents = new();
        private readonly ClimbingMachineDetachment _monsterGateDetachment = new(new MBList<ClimbingMachine>());

        public override void AfterStart()
        {
            if (!IsRelevantSiege(Mission) || !Mission.IsDeploymentFinished)
            {
                return;
            }
            RegisterForcedGateAgents();
            PrepareForcedGateAgents();
            RefreshForcedGateTargets();
        }

        public override void OnAfterDeploymentFinished()
        {
            if (!IsRelevantSiege(Mission))
            {
                return;
            }
            RegisterForcedGateAgents();
            PrepareForcedGateAgents();
            RefreshForcedGateTargets();
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!ShouldUseMonsterSiegeBehavior(agent))
            {
                return;
            }

            if (!_forcedGateAgents.Contains(agent))
            {
                _forcedGateAgents.Add(agent);
            }

            // reinforcements
            if (Mission.IsDeploymentFinished)
            {
                PrepareForcedGateAgent(agent);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            _forcedGateAgents.Remove(affectedAgent);

            if (GetPreferredClosedGate() != null)
            {
                return;
            }

            for (int i = _forcedGateAgents.Count - 1; i >= 0; i--)
            {
                Agent agent = _forcedGateAgents[i];
                if (!ShouldUseMonsterSiegeBehavior(agent))
                {
                    _forcedGateAgents.RemoveAt(i);
                    continue;
                }

                Agent currentTarget = agent.GetTargetAgent();
                if (currentTarget == null || !currentTarget.IsActive() || currentTarget == affectedAgent)
                {
                    AttackClosestSameLevelEnemy(agent);
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (_pendingGateStateRefresh)
            {
                _pendingGateStateRefresh = false;

                if (IsRelevantSiege(Mission))
                {
                    RefreshForcedGateTargetsAfterGateStateChange();
                }
            }

            if (_breachWidthFilterTimeRemaining <= 0f)
            {
                return;
            }

            float previousTimeRemaining = _breachWidthFilterTimeRemaining;
            _breachWidthFilterTimeRemaining -= dt;

            if (previousTimeRemaining > 0f && _breachWidthFilterTimeRemaining <= 0f)
            {
                ReleaseForcedGateAgentsToNativeCombat();
            }
        }

        protected override void OnEndMission()
        {
            _pendingGateStateRefresh = false;
            _breachWidthFilterTimeRemaining = 0f;
            _hasLastGateFrame = false;
            _forcedGateAgents.Clear();
        }

        private void RegisterForcedGateAgents()
        {
            _forcedGateAgents.Clear();

            foreach (Agent agent in Mission.Agents)
            {
                if (ShouldUseMonsterSiegeBehavior(agent))
                {
                    _forcedGateAgents.Add(agent);
                }
            }
        }

        private void PrepareForcedGateAgents()
        {
            for (int i = _forcedGateAgents.Count - 1; i >= 0; i--)
            {
                Agent agent = _forcedGateAgents[i];
                if (!ShouldUseMonsterSiegeBehavior(agent))
                {
                    _forcedGateAgents.RemoveAt(i);
                    continue;
                }

                PrepareForcedGateAgent(agent);
            }
        }

        private void PrepareForcedGateAgent(Agent agent)
        {
            StripCurrentUsage(agent);
            agent.TryRemoveAllDetachmentScores();

            if (agent.IsDetachedFromFormation && agent.Detachment != _monsterGateDetachment)
            {
                agent.TryAttachToFormation();
            }

            if (agent.Formation != null)
            {
                EnsureMonsterGateDetachment(agent.Formation);

                if (!agent.IsDetachedFromFormation || agent.Detachment != _monsterGateDetachment)
                {
                    _monsterGateDetachment.AddAgentAtSlotIndex(agent, -1);
                }

                // prevent native siege ai from assigning them back to ladders
                agent.SetDetachableFromFormation(false);
            }

            AssignCurrentGateTarget(agent);
        }

        private void EnsureMonsterGateDetachment(Formation formation)
        {
            foreach (IDetachment detachment in formation.Detachments)
            {
                if (ReferenceEquals(detachment, _monsterGateDetachment))
                {
                    return;
                }
            }

            formation.JoinDetachment(_monsterGateDetachment);
        }

        private void RefreshForcedGateTargets()
        {
            for (int i = _forcedGateAgents.Count - 1; i >= 0; i--)
            {
                Agent agent = _forcedGateAgents[i];
                if (!ShouldUseMonsterSiegeBehavior(agent))
                {
                    _forcedGateAgents.RemoveAt(i);
                    continue;
                }

                AssignCurrentGateTarget(agent);
            }
        }

        /// <summary>
        /// re evaluating monster targets after the gate state changes
        /// while a gate is closed, monsters are forced back onto first the outer and then the inner gate
        /// after the last gate falls, a short post breach filter window is opened before native combat targeting takes over again, to prevent monstrous units from stucking around
        /// </summary>
        private void RefreshForcedGateTargetsAfterGateStateChange()
        {
            CastleGate preferredGate = GetPreferredClosedGate();

            if (preferredGate != null)
            {
                _breachWidthFilterTimeRemaining = 0f;
            }
            else if (_hasLastGateFrame)
            {
                _breachWidthFilterTimeRemaining = PostBreachFilterDuration;
            }
            else
            {
                _breachWidthFilterTimeRemaining = 0f;
            }
            for (int i = _forcedGateAgents.Count - 1; i >= 0; i--)
            {
                Agent agent = _forcedGateAgents[i];
                if (!ShouldUseMonsterSiegeBehavior(agent))
                {
                    _forcedGateAgents.RemoveAt(i);
                    continue;
                }

                if (preferredGate != null)
                {
                    StripCurrentUsage(agent);
                    ClearScriptedTarget(agent);

                    _lastGateFrame = preferredGate.GameEntity.GetGlobalFrame();
                    _hasLastGateFrame = true;
                    agent.SetAutomaticTargetSelection(false);
                    agent.SetScriptedTargetEntity(
                        preferredGate.GameEntity,
                        Agent.AISpecialCombatModeFlags.AttackEntity,
                        ignoreIfAlreadyAttacking: false);

                    agent.ForceAiBehaviorSelection();
                    continue;
                }

                RetargetWithoutScenePathQuery(agent);
            }
        }

        private void AssignCurrentGateTarget(Agent agent)
        {
            CastleGate preferredGate = GetPreferredClosedGate();
            if (preferredGate == null)
            {
                AttackClosestSameLevelEnemy(agent);
                return;
            }
            StripCurrentUsage(agent);
            ClearScriptedTarget(agent);

            _lastGateFrame = preferredGate.GameEntity.GetGlobalFrame();
            _hasLastGateFrame = true;
            _breachWidthFilterTimeRemaining = 0f;

            agent.SetAutomaticTargetSelection(false);
            agent.SetScriptedTargetEntity(
                preferredGate.GameEntity,
                Agent.AISpecialCombatModeFlags.AttackEntity,
                ignoreIfAlreadyAttacking: false);

            agent.ForceAiBehaviorSelection();
        }

        private void AttackClosestSameLevelEnemy(Agent agent)
        {
            StripCurrentUsage(agent);
            ClearScriptedTarget(agent);

            Mission mission = agent.Mission;
            if (mission == null || agent.Team == null || mission.Scene == null)
            {
                return;
            }

            Agent closestEnemy = FindClosestReachableSameLevelEnemy(agent, mission);
            if (closestEnemy == null)
            {
                // leaving to native combat if no target has found
                agent.SetAutomaticTargetSelection(true);
                agent.InvalidateTargetAgent();
                agent.ForceAiBehaviorSelection();
                return;
            }
            agent.SetAutomaticTargetSelection(false);
            agent.SetTargetAgent(closestEnemy);
            agent.ForceAiBehaviorSelection();
        }

        private CastleGate GetPreferredClosedGate()
        {
            CastleGate innerGate = null;

            foreach (CastleGate gate in Mission.ActiveMissionObjects.FindAllWithType<CastleGate>())
            {
                if (gate == null || gate.IsDestroyed || gate.IsGateOpen)
                {
                    continue;
                }

                if (gate.GameEntity.HasTag(CastleGate.OuterGateTag))
                {
                    return gate;
                }

                if (gate.GameEntity.HasTag(CastleGate.InnerGateTag))
                {
                    innerGate = gate;
                }
            }
            return innerGate;
        }

        private bool IsInsideLastGateCorridor(Agent targetAgent)
        {
            if (!_hasLastGateFrame || targetAgent == null)
            {
                return true;
            }

            if (_breachWidthFilterTimeRemaining <= 0f)
            {
                return true;
            }

            Vec2 toTarget = targetAgent.Position.AsVec2 - _lastGateFrame.origin.AsVec2;

            Vec2 gateRight = _lastGateFrame.rotation.s.AsVec2;
            gateRight.Normalize();

            float lateralOffset =
                MathF.Abs((toTarget.X * gateRight.X) + (toTarget.Y * gateRight.Y));

            return lateralOffset <= BreachHalfWidth;
        }
        private static void StripCurrentUsage(Agent agent)
        {
            if (agent.IsInLadderQueue)
            {
                agent.DisableScriptedMovement();
                agent.SetIsInLadderQueue(false);
                agent.SetIsLadderQueueUsing(false);
            }

            if (agent.IsUsingGameObject || agent.InteractingWithAnyGameObject())
            {
                agent.StopUsingGameObject(isSuccessful: false, Agent.StopUsingGameObjectFlags.None);
            }
        }

        private static void ClearScriptedTarget(Agent agent)
        {
            if (agent.GetScriptedCombatFlags().HasAnyFlag(Agent.AISpecialCombatModeFlags.AttackEntity))
            {
                agent.DisableScriptedCombatMovement();
            }
        }

        private void RetargetWithoutScenePathQuery(Agent agent)
        {
            StripCurrentUsage(agent);
            ClearScriptedTarget(agent);

            Mission mission = agent.Mission;
            if (mission == null || agent.Team == null)
            {
                return;
            }

            Agent currentTarget = agent.GetTargetAgent();
            if (currentTarget != null &&
                currentTarget.IsActive() &&
                currentTarget.IsEnemyOf(agent) &&
                MathF.Abs(currentTarget.Position.z - agent.Position.z) <= MaxSameLevelHeightDifference &&
                IsInsideLastGateCorridor(currentTarget))
            {
                return;
            }

            Agent closestEnemy = FindClosestSameLevelEnemy(agent, mission);
            if (closestEnemy == null)
            {
                agent.SetAutomaticTargetSelection(true);
                agent.InvalidateTargetAgent();
                agent.ForceAiBehaviorSelection();
                return;
            }

            agent.SetAutomaticTargetSelection(false);
            agent.SetTargetAgent(closestEnemy);
            agent.ForceAiBehaviorSelection();
        }

        // keep monsters out of siege detach flow after native takes over
        private void ReleaseForcedGateAgentsToNativeCombat()
        {
            for (int i = _forcedGateAgents.Count - 1; i >= 0; i--)
            {
                Agent agent = _forcedGateAgents[i];
                if (!ShouldUseMonsterSiegeBehavior(agent))
                {
                    _forcedGateAgents.RemoveAt(i);
                    continue;
                }

                StripCurrentUsage(agent);
                ClearScriptedTarget(agent);

                agent.SetAutomaticTargetSelection(true);
                agent.InvalidateTargetAgent();
                agent.ForceAiBehaviorSelection();
            }
        }

        private Agent FindClosestSameLevelEnemy(Agent agent, Mission mission)
        {
            Agent closestEnemy = null;
            float bestDistanceSquared = LocalEnemySearchRadius * LocalEnemySearchRadius;
            float agentHeight = agent.Position.z;

            foreach (Agent otherAgent in mission.Agents)
            {
                if (!otherAgent.IsActive() || !otherAgent.IsHuman || !otherAgent.IsEnemyOf(agent))
                {
                    continue;
                }

                if (MathF.Abs(otherAgent.Position.z - agentHeight) > MaxSameLevelHeightDifference)
                {
                    continue;
                }

                if (!IsInsideLastGateCorridor(otherAgent))
                {
                    continue;
                }

                float distanceSquared = (otherAgent.Position.AsVec2 - agent.Position.AsVec2).LengthSquared;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    closestEnemy = otherAgent;
                }
            }

            return closestEnemy;
        }

        private Agent FindClosestReachableSameLevelEnemy(Agent agent, Mission mission)
        {
            const float PathAgentRadius = 1.15f; // largest agent radius being 1.14, gs_troll
            const float MaxAllowedPathStretch = 1.05f; // follow the closest agent only

            Agent closestEnemy = null;
            float bestPathDistance = float.MaxValue;
            float maxDirectDistanceSquared = LocalEnemySearchRadius * LocalEnemySearchRadius;
            float agentHeight = agent.Position.z;

            WorldPosition agentWorldPosition = agent.GetWorldPosition();

            foreach (Agent otherAgent in mission.Agents)
            {
                if (!otherAgent.IsActive() || !otherAgent.IsHuman || !otherAgent.IsEnemyOf(agent))
                {
                    continue;
                }

                if (MathF.Abs(otherAgent.Position.z - agentHeight) > MaxSameLevelHeightDifference)
                {
                    continue;
                }

                if (!IsInsideLastGateCorridor(otherAgent))
                {
                    continue;
                }

                float directDistanceSquared = (otherAgent.Position.AsVec2 - agent.Position.AsVec2).LengthSquared;
                if (directDistanceSquared > maxDirectDistanceSquared)
                {
                    continue;
                }

                WorldPosition otherAgentWorldPosition = otherAgent.GetWorldPosition();
                if (!mission.Scene.GetPathDistanceBetweenPositions(
                        ref agentWorldPosition,
                        ref otherAgentWorldPosition,
                        PathAgentRadius,
                        out float pathDistance))
                {
                    continue;
                }

                float directDistance = MathF.Sqrt(directDistanceSquared);

                if (pathDistance > directDistance * MaxAllowedPathStretch)
                {
                    continue;
                }

                if (pathDistance < bestPathDistance)
                {
                    bestPathDistance = pathDistance;
                    closestEnemy = otherAgent;
                }
            }

            return closestEnemy;
        }
        internal static bool ShouldBlockLadderUsage(Agent agent)
        {
            return agent != null &&
                   agent.IsActive() &&
                   IsRelevantSiege(agent.Mission) &&
                   agent.Character?.CanClimbLadders() == false;
        }

        internal static bool ShouldUseMonsterSiegeBehavior(Agent agent)
        {
            return agent != null &&
                   agent.IsActive() &&
                   IsRelevantSiege(agent.Mission) &&
                   agent.Team?.Side == BattleSideEnum.Attacker &&
                   agent.Character?.CanClimbLadders() == false;
        }

        internal static bool IsClosedGate(DestructableComponent destructableComponent)
        {
            if (destructableComponent == null)
            {
                return false;
            }

            CastleGate gate = destructableComponent.GameEntity.GetFirstScriptOfType<CastleGate>();
            return gate != null && !gate.IsDestroyed && !gate.IsGateOpen;
        }

        internal static void RefreshCurrentMissionGateTargets()
        {
            Mission mission = Mission.Current;
            if (mission == null)
            {
                return;
            }

            TORMonsterSiegeLogic logic = mission.GetMissionBehavior<TORMonsterSiegeLogic>();
            if (logic == null)
            {
                return;
            }

            logic._pendingGateStateRefresh = true;
        }

        private static bool IsRelevantSiege(Mission mission)
        {
            return mission != null &&
                   mission.IsSiegeBattle &&
                   !mission.IsSallyOutBattle;
        }
    }

    // native queue enter check
    [HarmonyPatch(typeof(LadderQueueManager), "ConditionsAreMet",
        new[] { typeof(Agent), typeof(Agent.AIScriptedFrameFlags) })]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeLadderQueueConditionsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Agent agent, Agent.AIScriptedFrameFlags flags, ref bool __result)
        {
            if (__result && TORMonsterSiegeLogic.ShouldBlockLadderUsage(agent))
            {
                __result = false;
            }
        }
    }

    // native ladder intent check
    [HarmonyPatch(typeof(LadderQueueManager), "ShouldAgentUseTheLadder",
        new[] { typeof(Agent) })]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeShouldUseLadderPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Agent agent, ref bool __result)
        {
            if (__result && TORMonsterSiegeLogic.ShouldBlockLadderUsage(agent))
            {
                __result = false;
            }
        }
    }

    // block queue insert
    [HarmonyPatch(typeof(LadderQueueManager), "AddAgentToQueue",
        new[] { typeof(Agent) })]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeAddAgentToQueuePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Agent agent)
        {
            return !TORMonsterSiegeLogic.ShouldBlockLadderUsage(agent);
        }
    }

    // block machine slot assignment
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeDetachmentSlotAvailabilityPatch
    {
        [HarmonyPatch]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(UsableMachine),
                "TaleWorlds.MountAndBlade.IDetachment.IsSlotAtIndexAvailableForAgent",
                new[] { typeof(int), typeof(Agent) });
        }

        [HarmonyPrefix]
        private static bool Prefix(UsableMachine __instance, int slotIndex, Agent agent, ref bool __result)
        {
            if (!TORMonsterSiegeLogic.ShouldBlockLadderUsage(agent))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    // block native standing point grabs
    [HarmonyPatch(typeof(StandingPoint), "IsDisabledForAgent")]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeStandingPointPatch
    {
        [HarmonyPostfix]
        private static void Postfix(StandingPoint __instance, Agent agent, ref bool __result)
        {
            if (!__result && TORMonsterSiegeLogic.ShouldBlockLadderUsage(agent))
            {
                __result = true;
            }
        }
    }

    // block any usable object fallback
    [HarmonyPatch(typeof(Agent), "UseGameObject",
        new[] { typeof(UsableMissionObject), typeof(int) })]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeUseGameObjectPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Agent __instance, UsableMissionObject usedObject)
        {
            return !TORMonsterSiegeLogic.ShouldBlockLadderUsage(__instance);
        }
    }

    [HarmonyPatch(typeof(DestructableComponent), "OnHit")]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeGateDamagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(
            DestructableComponent __instance,
            Agent attackerAgent,
            ref int inflictedDamage,
            MissionWeapon weapon,
            ScriptComponentBehavior attackerScriptComponentBehavior)
        {
            if (__instance == null || __instance.IsDestroyed || inflictedDamage <= 0)
            {
                return;
            }

            if (attackerScriptComponentBehavior is BatteringRam)
            {
                return;
            }

            if (!TORMonsterSiegeLogic.ShouldUseMonsterSiegeBehavior(attackerAgent) ||
                !TORMonsterSiegeLogic.IsClosedGate(__instance))
            {
                return;
            }

            if (weapon.IsEmpty ||
                weapon.CurrentUsageItem == null ||
                !weapon.CurrentUsageItem.IsMeleeWeapon)
            {
                return;
            }

            inflictedDamage = (int)(inflictedDamage * TORMonsterSiegeLogic.GateDamageMultiplier);
        }
    }

    // retarget when gate state changes
    [HarmonyPatch(typeof(CastleGate), "OpenDoor")]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeGateOpenPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            TORMonsterSiegeLogic.RefreshCurrentMissionGateTargets();
        }
    }

    [HarmonyPatch(typeof(CastleGate), "CloseDoor")]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeGateClosePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            TORMonsterSiegeLogic.RefreshCurrentMissionGateTargets();
        }
    }

    [HarmonyPatch(typeof(CastleGate), "OnDestroyed")]
    [HarmonyPatchCategory("LatePatches")]
    internal static class TORMonsterSiegeGateDestroyedPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            TORMonsterSiegeLogic.RefreshCurrentMissionGateTargets();
        }
    }
}