using SandBox.Missions.MissionLogics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics
{
    public class TORBattleAgentLogic : BattleAgentLogic
    {
        // for dismounted cavalry with lances, how often to check for weapon swaps
        private const float TICK_INTERVAL_SECONDS = 1.50f;
        // treat any weapon longer than this as lance
        private const int LANCE_LENGTH_THRESHOLD = 200;
        // cavalry charge detection radius for dismounted cav (for when they might actually make use of a lance)
        private const float CAVALRY_CHARGE_THREAT_DISTANCE = 30f;
        // charge test: enemy must be moving mostly towards agent (1 is riding directly at agent)
        private const float CAVALRY_APPROACH_DOT_THRESHOLD = 0.85f;
        // minimum cavalry speed to consider it a lance target
        private const float MIN_CAVALRY_SPEED_TO_ALLOW_LANCE = 4f;
        // switch to a sidearm when they get this close
        private const float CLOSE_MELEE_DISTANCE_TO_FORCE_SIDEARM = 4f;
        // ignore mounted enemies that are already too close to be a meaningful lance target, wont switch if they're this close
        private const float MIN_CAVALRY_DISTANCE_FOR_CHARGE_THREAT = 6.75f;
        // if the agent already has a close on foot target, prefer sidearm handling before broader local threat scanning
        private const float TARGET_AGENT_DISTANCE_TO_FORCE_SIDEARM = 6f;

        private float _elapsedSinceLastTick;

        private bool _fixedHirelingSpawn;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (!_fixedHirelingSpawn)
            {
                TryFixHirelingSpawn();
            }

            if (Mission.Mode == MissionMode.Deployment || Mission.IsTeleportingAgents || Mission.IsSiegeBattle)
            {
                _elapsedSinceLastTick = 0f;
                return;
            }

            _elapsedSinceLastTick += dt;
            if (_elapsedSinceLastTick < TICK_INTERVAL_SECONDS)
            {
                return;
            }

            _elapsedSinceLastTick = 0f;

            Agent[] missionAgentsSnapshot = Mission.Agents.ToArray();

            foreach (var agent in missionAgentsSnapshot)
            {
                if (!ShouldProcessAgent(agent))
                {
                    continue;
                }

                var lanceSlot = FindBestLanceWeaponSlot(agent);
                if (!lanceSlot.HasValue)
                {
                    continue;
                }

                Agent targetAgent = agent.GetTargetAgent();
                if (targetAgent != null &&
                    targetAgent.IsActive() &&
                    targetAgent.IsHuman &&
                    targetAgent.IsEnemyOf(agent))
                {
                    Vec2 targetOffset = targetAgent.Position.AsVec2 - agent.Position.AsVec2;
                    float targetDistanceSq = targetOffset.LengthSquared;
                    float targetDistanceThresholdSq =
                        TARGET_AGENT_DISTANCE_TO_FORCE_SIDEARM * TARGET_AGENT_DISTANCE_TO_FORCE_SIDEARM;

                    if (targetAgent.MountAgent == null && targetDistanceSq <= targetDistanceThresholdSq)
                    {
                        if (IsLanceUsage(agent.WieldedWeapon.CurrentUsageItem))
                        {
                            var targetSidearmSlot = FindBestNonLanceMeleeWeaponSlot(agent);
                            if (targetSidearmSlot.HasValue)
                            {
                                TryWieldSlotIfNotAlready(agent, targetSidearmSlot.Value);
                            }
                        }

                        continue;
                    }
                }

                EvaluateLocalThreats(agent,
                    missionAgentsSnapshot,
                    out bool hasCloseFootEnemy,
                    out bool hasIncomingMountedCharge,
                    out bool isIncomingMountedChargeTooCloseToSwapToLance);

                bool shouldWieldLanceOnFoot = !hasCloseFootEnemy && hasIncomingMountedCharge;

                if (shouldWieldLanceOnFoot)
                {
                    if (!isIncomingMountedChargeTooCloseToSwapToLance)
                    {
                        TryWieldSlotIfNotAlready(agent, lanceSlot.Value);
                    }

                    // even if it didnt swap to lance, dont force swap away from lance either
                    continue;
                }

                if (!IsLanceUsage(agent.WieldedWeapon.CurrentUsageItem))
                {
                    continue;
                }

                var sidearmSlot = FindBestNonLanceMeleeWeaponSlot(agent);
                if (!sidearmSlot.HasValue)
                {
                    continue;
                }

                TryWieldSlotIfNotAlready(agent, sidearmSlot.Value);
            }
        }
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.Origin is SummonedAgentOrigin) return;
            base.OnAgentBuild(agent, banner);
        }

        public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
        {
            if (agent.Origin is SummonedAgentOrigin) return;
            base.OnAgentTeamChanged(prevTeam, newTeam, agent);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (affectedAgent.Origin is SummonedAgentOrigin) return;
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
        {
            //Sly : base leads to EnemyHitReward which casts Origin.BattleCombattant to PartyBase which is unsupported for summoned troops
            if (affectorAgent.Origin is SummonedAgentOrigin) return;
            if (affectorAgent.IsMount && affectorAgent.RiderAgent?.Origin is SummonedAgentOrigin) return; //necromancer champion riding a mount which deals damage

            // Set flag for spell hits so TORCombatXpModel can skip weapon XP while still tracking kills
            bool isSpellHit = collisionData.AffectorWeaponSlotOrMissileIndex == TORSpellBlowHelper.SpellBlowSentinel;
            if (isSpellHit)
                TORSpellBlowHelper.IsProcessingSpellHit = true;

            try
            {
                base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, blow, collisionData, damagedHp, hitDistance, shotDifficulty);
            }
            finally
            {
                if (isSpellHit)
                    TORSpellBlowHelper.IsProcessingSpellHit = false;
            }

        }

        private void TryFixHirelingSpawn()
        {
            if (_fixedHirelingSpawn)
            {
                return;
            }

            var hirelingBehavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (hirelingBehavior?.IsEnlisted() != true)
            {
                _fixedHirelingSpawn = true;
                return;
            }

            if (Mission.Mode == MissionMode.Deployment || Mission.IsTeleportingAgents)
            {
                return;
            }

            var enlistedParty = hirelingBehavior.EnlistingLord?.PartyBelongedTo?.Party;
            if (enlistedParty == null)
            {
                return;
            }

            var mainAgent = Mission.MainAgent;
            if (mainAgent == null || !mainAgent.IsActive())
            {
                return;
            }

            if (Mission.IsSiegeBattle)
            {
                return;
            }

            var enlistedPartyAgent = Mission.Agents.FirstOrDefault(agent =>
                agent != mainAgent &&
                agent.IsActive() &&
                agent.IsHuman &&
                agent.Origin?.BattleCombatant == enlistedParty);

            if (enlistedPartyAgent == null)
            {
                return;
            }

            const float maxReasonableSpawnDistance = 12f;
            var maxReasonableSpawnDistanceSquared = maxReasonableSpawnDistance * maxReasonableSpawnDistance;
            var distanceToPartySquared = (enlistedPartyAgent.Position.AsVec2 - mainAgent.Position.AsVec2).LengthSquared;

            if (distanceToPartySquared > maxReasonableSpawnDistanceSquared)
            {
                // joined battles can spawn the player behind
                var targetPosition = Mission.GetRandomPositionAroundPoint(enlistedPartyAgent.Position, 0.5f, 2f, true);
                mainAgent.TeleportToPosition(targetPosition);
            }

            _fixedHirelingSpawn = true;
        }

        private static bool ShouldProcessAgent(Agent agent)
        {
            if (!agent.IsActive() || !agent.IsHuman)
            {
                return false;
            }

            if (agent == Agent.Main)
            {
                return false;
            }

            if (agent.Origin is SummonedAgentOrigin)
            {
                return false;
            }

            if (agent.Character?.Culture?.StringId == TORConstants.Cultures.DAWI)
            {
                return false;
            }

            if (agent.IsUsingGameObject)
            {
                return false;
            }

            if (agent.MovementLockedState != AgentMovementLockedState.None)
            {
                return false;
            }

            // only dismounted
            if (agent.MountAgent != null)
            {
                return false;
            }

            // keep infantry pikes
            var formationClass = agent.Character?.DefaultFormationClass ?? FormationClass.Infantry;
            return formationClass == FormationClass.Cavalry || formationClass == FormationClass.HorseArcher;
        }


        private static bool IsLanceUsage(WeaponComponentData usageItem)
        {
            if (usageItem == null)
            {
                return false;
            }

            if (!usageItem.IsMeleeWeapon || usageItem.IsRangedWeapon || usageItem.IsShield)
            {
                return false;
            }
            return usageItem.WeaponLength > LANCE_LENGTH_THRESHOLD;
        }
        private void EvaluateLocalThreats(
            Agent agent,
            Agent[] missionAgentsSnapshot,
            out bool hasCloseFootEnemy,
            out bool hasIncomingMountedCharge,
            out bool isIncomingMountedChargeTooCloseToSwapToLance)
        {
            hasCloseFootEnemy = false;
            hasIncomingMountedCharge = false;
            isIncomingMountedChargeTooCloseToSwapToLance = false;

            Vec2 agentPosition = agent.Position.AsVec2;

            float closeFootEnemyDistanceSq = CLOSE_MELEE_DISTANCE_TO_FORCE_SIDEARM * CLOSE_MELEE_DISTANCE_TO_FORCE_SIDEARM;
            float chargeThreatDistanceSq = CAVALRY_CHARGE_THREAT_DISTANCE * CAVALRY_CHARGE_THREAT_DISTANCE;
            float minChargeDistanceSq = MIN_CAVALRY_DISTANCE_FOR_CHARGE_THREAT * MIN_CAVALRY_DISTANCE_FOR_CHARGE_THREAT;

            float minCavalrySpeedSq = MIN_CAVALRY_SPEED_TO_ALLOW_LANCE * MIN_CAVALRY_SPEED_TO_ALLOW_LANCE;
            float approachDotThresholdSq = CAVALRY_APPROACH_DOT_THRESHOLD * CAVALRY_APPROACH_DOT_THRESHOLD;

            foreach (var otherAgent in missionAgentsSnapshot)
            {
                if (!otherAgent.IsActive() || !otherAgent.IsHuman)
                {
                    continue;
                }

                if (otherAgent == agent)
                {
                    continue;
                }

                if (!otherAgent.IsEnemyOf(agent))
                {
                    continue;
                }

                Vec2 otherPosition = otherAgent.Position.AsVec2;
                Vec2 otherToAgent = agentPosition - otherPosition;
                float distanceSq = otherToAgent.LengthSquared;

                // on foot close melee
                if (otherAgent.MountAgent == null)
                {
                    if (distanceSq <= closeFootEnemyDistanceSq)
                    {
                        hasCloseFootEnemy = true;
                        // highest priority
                        return;
                    }

                    continue;
                }

                // incoming charge towards this agent
                if (distanceSq > chargeThreatDistanceSq)
                {
                    continue;
                }

                Vec2 otherVelocity = otherAgent.Velocity.AsVec2;
                float speedSq = otherVelocity.LengthSquared;

                if (speedSq < minCavalrySpeedSq)
                {
                    continue;
                }

                float velocityToAgentDot = Vec2.DotProduct(otherVelocity, otherToAgent);
                if (velocityToAgentDot <= 0f)
                {
                    continue;
                }

                float dotSq = velocityToAgentDot * velocityToAgentDot;
                float rhs = approachDotThresholdSq * speedSq * distanceSq;

                if (dotSq >= rhs)
                {
                    hasIncomingMountedCharge = true;

                    // if theres a close incoming charge, allow to keep lance but block swap to lance
                    if (distanceSq < minChargeDistanceSq)
                    {
                        isIncomingMountedChargeTooCloseToSwapToLance = true;
                    }
                }
            }
        }

        private static void TryWieldSlotIfNotAlready(Agent agent, EquipmentIndex weaponSlot)
        {
            EquipmentIndex currentSlot = agent.GetPrimaryWieldedItemIndex();
            if (currentSlot == weaponSlot)
            {
                return;
            }

            if (currentSlot == EquipmentIndex.None && !agent.WieldedWeapon.IsEmpty)
            {
                return;
            }

            if (!agent.GetAgentFlags().HasAnyFlag(AgentFlag.CanWieldWeapon))
            {
                return;
            }

            Agent.ActionCodeType upperBodyActionType = agent.GetCurrentActionType(1);
            if (upperBodyActionType == Agent.ActionCodeType.EquipUnequip ||
                upperBodyActionType == Agent.ActionCodeType.SwitchAlternative ||
                upperBodyActionType == Agent.ActionCodeType.ReleaseMelee ||
                upperBodyActionType == Agent.ActionCodeType.ReleaseRanged ||
                upperBodyActionType == Agent.ActionCodeType.ReleaseThrowing ||
                upperBodyActionType == Agent.ActionCodeType.WeaponBash)
            {
                return;
            }

            Agent.ActionCodeType fullBodyActionType = agent.GetCurrentActionType(0);
            if (fullBodyActionType == Agent.ActionCodeType.Mount ||
                fullBodyActionType == Agent.ActionCodeType.Dismount ||
                fullBodyActionType == Agent.ActionCodeType.Fall ||
                fullBodyActionType == Agent.ActionCodeType.JumpStart ||
                fullBodyActionType == Agent.ActionCodeType.Jump ||
                fullBodyActionType == Agent.ActionCodeType.JumpEnd ||
                fullBodyActionType == Agent.ActionCodeType.JumpEndHard ||
                agent.IsInBeingStruckAction)
            {
                return;
            }

            MissionWeapon missionWeapon = agent.Equipment[weaponSlot];
            if (missionWeapon.IsEmpty || missionWeapon.CurrentUsageItem == null)
            {
                return;
            }

            agent.TryToWieldWeaponInSlot(
                weaponSlot,
                Agent.WeaponWieldActionType.WithAnimation,
                false);
        }

        private static EquipmentIndex? FindBestLanceWeaponSlot(Agent agent)
        {
            EquipmentIndex? bestSlot = null;
            int bestWeaponLength = -1;

            for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.ExtraWeaponSlot; i++)
            {
                var slot = (EquipmentIndex)i;
                MissionWeapon missionWeapon = agent.Equipment[slot];
                if (missionWeapon.IsEmpty || missionWeapon.CurrentUsageItem == null)
                {
                    continue;
                }

                var usageItem = missionWeapon.CurrentUsageItem;
                if (!IsLanceUsage(usageItem))
                {
                    continue;
                }

                int weaponLength = usageItem.WeaponLength;
                if (weaponLength > bestWeaponLength)
                {
                    bestWeaponLength = weaponLength;
                    bestSlot = slot;
                }
            }

            return bestSlot;
        }

        private static EquipmentIndex? FindBestNonLanceMeleeWeaponSlot(Agent agent)
        {
            EquipmentIndex? bestSlot = null;
            int bestWeaponLength = int.MaxValue;

            for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.ExtraWeaponSlot; i++)
            {
                var slot = (EquipmentIndex)i;
                MissionWeapon missionWeapon = agent.Equipment[slot];
                if (missionWeapon.IsEmpty || missionWeapon.CurrentUsageItem == null)
                {
                    continue;
                }

                var usageItem = missionWeapon.CurrentUsageItem;
                if (!usageItem.IsMeleeWeapon || usageItem.IsRangedWeapon || usageItem.IsShield)
                {
                    continue;
                }

                if (IsLanceUsage(usageItem))
                {
                    continue;
                }

                int weaponLength = usageItem.WeaponLength;
                if (weaponLength < bestWeaponLength)
                {
                    bestWeaponLength = weaponLength;
                    bestSlot = slot;
                }
            }

            return bestSlot;
        }
    }
}