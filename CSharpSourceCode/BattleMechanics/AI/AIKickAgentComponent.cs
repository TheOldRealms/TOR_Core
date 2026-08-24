﻿using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI
{
    public class AIKickAgentComponent : AgentComponent
    {
        private static readonly Random _random = new Random();

        private float _inputRefreshElapsed;
        private bool _ownsAIInputCallback;
        private float _nextCloseActionTime;

        public AIKickAgentComponent(Agent agent) : base(agent)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            _inputRefreshElapsed = (float)_random.NextDouble() * Agent.AgentDrivenProperties.AiCheckDoSimpleBehaviorInterval;
        }

        public override void OnTick(float dt)
        {
            _inputRefreshElapsed += dt;
            var refreshInterval = Agent.AgentDrivenProperties.AiCheckDoSimpleBehaviorInterval;
            if (_inputRefreshElapsed < refreshInterval)
            {
                return;
            }

            _inputRefreshElapsed -= refreshInterval;
            UpdateAIInputRegistration();
        }

        public override void OnAIInputSet(ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
        {
            if (!CanUseCloseCombatActions() || (eventFlag & Agent.EventControlFlag.Kick) != 0)
            {
                return;
            }

            var enemy = Agent.ImmediateEnemy;
            if (!IsUsableTarget(enemy))
            {
                return;
            }

            var toEnemy = enemy.Position.AsVec2 - Agent.Position.AsVec2;
            var distance = toEnemy.Normalize();

            var lookDirection = Agent.LookDirection.AsVec2;
            var hasLookDirection = lookDirection.Normalize() > 0f;
            if (distance > 0f && hasLookDirection && Vec2.DotProduct(lookDirection, toEnemy) <= 0f)
            {
                return;
            }

            var time = Agent.Mission.CurrentTime;
            if (time < _nextCloseActionTime || !Agent.KickClear())
            {
                return;
            }

            var enemyOffhand = enemy.WieldedOffhandWeapon;
            var enemyBlocking = enemy.CurrentGuardMode != Agent.GuardMode.None;
            var enemyBlockingWithShield = enemyBlocking && !enemyOffhand.IsEmpty && enemyOffhand.IsShield();

            var offhand = Agent.WieldedOffhandWeapon;
            var hasShield = !offhand.IsEmpty && offhand.IsShield();
            var weapon = Agent.WieldedWeapon;
            var weaponUsage = weapon.IsEmpty ? null : weapon.CurrentUsageItem;
            var hasMeleeWeapon = weaponUsage?.IsMeleeWeapon == true;
            var weaponReach = weaponUsage == null ? 0f : weaponUsage.WeaponLength / 100f;

            var kickReach = GetKickReach(enemy);
            var bashReach = GetBashReach(enemy, hasShield, weaponReach);
            // important: reading the intention to block is necessary as CurrentGuardMode reflects the previously applied guard decision that is possibly an outdated source for kick evaluation. 
            var defendInput = Agent.MovementControlFlag.DefendMask | Agent.MovementControlFlag.DefendBlock;
            var guarding = (movementFlag & defendInput) != Agent.MovementControlFlag.None;

            var canKick = !guarding && enemyBlockingWithShield && distance <= kickReach;

            var firstEnemyActionStage = enemy.GetCurrentActionStage(0);
            var secondEnemyActionStage = enemy.GetCurrentActionStage(1);
            var enemyAttacking = IsAttackStage(firstEnemyActionStage) || IsAttackStage(secondEnemyActionStage);

            var canBash = guarding &&
                          (hasShield || hasMeleeWeapon) &&
                          distance <= bashReach &&
                          (enemyBlocking || !enemyAttacking);

            if (!canKick && !canBash)
            {
                return;
            }

            var drivenProperties = Agent.AgentDrivenProperties;
            var aiKick = MBMath.ClampFloat(drivenProperties.AiKick, 0f, 1f);
            var blockingAwareness = MBMath.ClampFloat(
                drivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility,
                0f,
                1f);

            var kickIntent = MBMath.ClampFloat(
                drivenProperties.AIAttackOnDecideChance + aiKick,
                0f,
                1f);

            if (enemyBlocking)
            {
                kickIntent = Math.Max(kickIntent, blockingAwareness);
            }

            var bashIntent = MBMath.ClampFloat(
                drivenProperties.AIAttackOnDecideChance +
                aiKick +
                drivenProperties.AIAttackOnParryChance,
                0f,
                1f);

            var actionIntent = canKick ? kickIntent : bashIntent;
            if (_random.NextDouble() > actionIntent)
            {
                _nextCloseActionTime = time + drivenProperties.AiCheckDoSimpleBehaviorInterval;
                return;
            }

            eventFlag |= Agent.EventControlFlag.Kick;
            _nextCloseActionTime = time + GetCloseActionRecovery();
        }

        public override void OnRetreating()
        {
            DisableOwnedAIInputCallback();
        }

        public override void OnAgentRemoved()
        {
            DisableOwnedAIInputCallback();
        }

        public override void OnComponentRemoved()
        {
            DisableOwnedAIInputCallback();
        }

        private void UpdateAIInputRegistration()
        {
            var shouldReceiveAIInput = CanUseCloseCombatActions();
            if (shouldReceiveAIInput)
            {
                var enemy = Agent.ImmediateEnemy;
                shouldReceiveAIInput = IsUsableTarget(enemy) &&
                                       Agent.Position.AsVec2.Distance(enemy.Position.AsVec2) <= GetAIInputRange(enemy);
            }

            var hasAIInputCallback = Agent.GetHasOnAiInputSetCallback();
            if (shouldReceiveAIInput)
            {
                if (!hasAIInputCallback)
                {
                    Agent.SetHasOnAiInputSetCallback(true);
                    _ownsAIInputCallback = true;
                }

                return;
            }

            DisableOwnedAIInputCallback();
        }

        private bool CanUseCloseCombatActions()
        {
            return Agent.IsActive() &&
                   Agent.IsAIControlled &&
                   !Agent.IsMainAgent &&
                   !Agent.HasMount &&
                   !Agent.IsRunningAway &&
                   Agent.Team != null &&
                   Agent.CombatActionsEnabled &&
                   Agent.Health > 0f &&
                  // monstrous units/treespirits cant kick/bash. perhaps dawi too?
                  (Agent.GetAgentFlags() & AgentFlag.CanDefend) != 0;
        }
            
        private bool IsUsableTarget(Agent target)
        {
            return target != null &&
                   target.Mission == Agent.Mission &&
                   target.IsActive() &&
                   target.IsHuman &&
                   !target.HasMount &&
                   target.Health > 0f &&
                   Agent.IsEnemyOf(target);
        }

        private float GetAIInputRange(Agent enemy)
        {
            var offhand = Agent.WieldedOffhandWeapon;
            var hasShield = !offhand.IsEmpty && offhand.IsShield();
            var weapon = Agent.WieldedWeapon;
            var weaponUsage = weapon.IsEmpty ? null : weapon.CurrentUsageItem;
            var weaponReach = weaponUsage == null ? 0f : weaponUsage.WeaponLength / 100f;

            var actionReach = Math.Max(GetKickReach(enemy), GetBashReach(enemy, hasShield, weaponReach));
            var closingSpeed = Math.Max(Agent.Monster.WalkingSpeedLimit, enemy.Monster.WalkingSpeedLimit);
            return actionReach + closingSpeed * Agent.AgentDrivenProperties.AiCheckDoSimpleBehaviorInterval;
        }

        private float GetKickReach(Agent enemy)
        {
            return Agent.CollisionCapsule.Radius +
                   enemy.CollisionCapsule.Radius +
                   Agent.Monster.StandingPelvisHeight * Agent.AgentScale;
        }

        private float GetBashReach(Agent enemy, bool hasShield, float weaponReach)
        {
            var bodyReach = Agent.CollisionCapsule.Radius +
                            enemy.CollisionCapsule.Radius +
                            Agent.GetArmLength() +
                            (Agent.Monster.StandingChestHeight - Agent.Monster.StandingPelvisHeight) * Agent.AgentScale;

            if (hasShield)
            {
                return bodyReach;
            }

            var weaponContribution = Math.Min(
                weaponReach,
                Agent.Monster.StandingPelvisHeight * Agent.AgentScale);
            return bodyReach + weaponContribution;
        }

        private static bool IsAttackStage(Agent.ActionStage actionStage)
        {
            return actionStage == Agent.ActionStage.AttackReady ||
                   actionStage == Agent.ActionStage.AttackQuickReady ||
                   actionStage == Agent.ActionStage.AttackRelease;
        }

        private float GetCloseActionRecovery()
        {
            var drivenProperties = Agent.AgentDrivenProperties;
            return drivenProperties.AiCheckDecideSimpleBehaviorInterval +
                   drivenProperties.AiCheckDecideSimpleBehaviorInterval +
                   drivenProperties.AiCheckDoSimpleBehaviorInterval;
        }

        private void DisableOwnedAIInputCallback()
        {
            if (_ownsAIInputCallback && Agent.IsActive() && Agent.GetHasOnAiInputSetCallback())
            {
                Agent.SetHasOnAiInputSetCallback(false);
            }

            _ownsAIInputCallback = false;
        }
    }
}
