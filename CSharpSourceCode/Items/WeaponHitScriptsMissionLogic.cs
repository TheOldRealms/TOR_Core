using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items.WeaponHitScripts;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public class WeaponHitScriptsMissionLogic : MissionLogic
    {
        private static readonly float _triggerCooldown = 2;
        private Dictionary<int, Dictionary<string, float>> _traitCoolDownMap = new();
        private float _deltaTime;
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.IsHuman)
            {
                var comp = new ItemTraitAgentComponent(agent);
                agent.AddComponent(comp);
                agent.OnAgentWieldedItemChange += comp.OnWieldedItemChanged;
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (Agent.Main != null && Agent.Main.GetComponent<ItemTraitAgentComponent>() != null)
            {
                Agent.Main.GetComponent<ItemTraitAgentComponent>().OnTickAsMainAgent(dt);
            }

            _deltaTime += dt;
            if (_deltaTime < _triggerCooldown)
            {
                return;
            }

            _deltaTime = 0f;

            TickBaneOfTheBeastkin();
            if (!_traitCoolDownMap.AnyQ())
            {
                return;
            }

            var currentMissionTime = Mission.Current.CurrentTime;

            foreach (var agentIndex in _traitCoolDownMap.Keys.ToList())
            {
                var traitCooldowns = _traitCoolDownMap[agentIndex];

                foreach (var cooldownId in traitCooldowns.Keys.ToList())
                {
                    if (traitCooldowns[cooldownId] <= currentMissionTime)
                    {
                        traitCooldowns.Remove(cooldownId);
                    }
                }

                if (traitCooldowns.Count == 0)
                {
                    _traitCoolDownMap.Remove(agentIndex);
                }
            }
        }

        public override void OnBattleEnded()
        {
            base.OnBattleEnded();
            _traitCoolDownMap.Clear();
            _deltaTime = 0f;
        }

        private void TickBaneOfTheBeastkin()
        {
            var bearers = Mission.Current.Agents
                .Where(agent =>
                    agent.IsHuman &&
                    agent.IsActive() &&
                    agent.Health > 0f &&
                    !agent.IsFadingOut() &&
                    agent.Character
                        .GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot, EquipmentIndex.ArmorItemEndSlot)
                        .Any(item => item.GetTraits(agent).Any(trait => trait.ItemTraitStringId == "asrai_enchant_bane_beastkin")))
                .ToList();

            if (bearers.Count == 0)
            {
                return;
            }

            var beastmen = Mission.Current.Agents
                .Where(agent =>
                    agent.IsHuman &&
                    agent.IsActive() &&
                    agent.Health > 0f &&
                    !agent.IsFadingOut() &&
                    agent.Character?.Culture?.StringId == TORConstants.Cultures.BEASTMEN)
                .ToList();

            if (beastmen.Count == 0)
            {
                return;
            }

            var damagedThisPulse = new HashSet<int>();

            foreach (var bearer in bearers)
            {
                foreach (var beastman in beastmen)
                {
                    if (damagedThisPulse.Count >= 25)
                    {
                        return;
                    }

                    if (beastman == bearer ||
                        damagedThisPulse.Contains(beastman.Index) ||
                        bearer.Position.DistanceSquared(beastman.Position) > 25f)
                    {
                        continue;
                    }

                    beastman.ApplyDamage(3, beastman.Position, bearer, doBlow: false, hasShockWave: false, originatesFromAbility: false);
                    damagedThisPulse.Add(beastman.Index);
                }
            }
        }

        public override void OnRegisterBlow(Agent attacker, Agent victim, WeakGameEntity realHitEntity, Blow blow, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon) // melee & ranged hits
        {
            if (attacker == null || victim == null || attacker == victim || !attacker.IsActive() || attacker.IsFadingOut() || !victim.IsHuman || !victim.IsActive() || victim.Health <= 0f || victim.IsFadingOut() || !collisionData.IsColliderAgent || collisionData.AttackBlockedWithShield || collisionData.MissileBlockedWithWeapon || collisionData.CollidedWithShieldOnBack || blow.BaseMagnitude <= 0f || TORSpellBlowHelper.IsSpellBlow(blow))
            {
                return;
            }

            var poisonChance = attacker.GetPoisonousHitChance();
            if (poisonChance <= 0f || MBRandom.RandomFloat > poisonChance)
            {
                return;
            }

            var poisonDuration = 9f; // first second is consumed before the damage tick
            ApplyWeaponStatusEffect(victim, "trait_poisonous_dot", attacker, poisonDuration, false);
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (affectedAgent == affectorAgent)
                return;

            if (affectorWeapon.Item != null &&
                affectorWeapon.Item.HasAnyTrait(affectorAgent) &&
                CanUseOffensiveWeaponTraitTarget(affectedAgent, affectorAgent))
            {
                var statusEffectTraits = affectorWeapon.Item.GetTraits(affectorAgent)
                 .Where(x => !string.IsNullOrWhiteSpace(x.ImbuedStatusEffectId) &&
                 !x.ImbuedStatusEffectId.Equals("none", StringComparison.OrdinalIgnoreCase));

                foreach (var trait in statusEffectTraits)
                {
                    if (!CanReceiveWeaponStatusEffect(affectedAgent))
                    {
                        continue;
                    }

                    if (MBRandom.RandomFloat <= trait.ImbuedEffectChance)
                    {
                        ApplyWeaponStatusEffect(affectedAgent, trait.ImbuedStatusEffectId, affectorAgent, 5, false);
                    }
                }

                var onHitTraits = affectorWeapon.Item.GetTraits(affectorAgent)
                    .WhereQ(x => x.OnWeaponHitScript != null && !string.IsNullOrWhiteSpace(x.OnWeaponHitScript.WeaponScriptName) && x.OnWeaponHitScript.WeaponScriptName != "invalid");

                if (onHitTraits != null && onHitTraits.Count() > 0)
                {
                    foreach (var trait in onHitTraits)
                    {
                        if (!CanApplyOffensiveWeaponTrait(trait, affectedAgent, affectorAgent, blow))
                        {
                            continue;
                        }

                        var coolDownOnAffectedAgent = !ShouldCooldownOnWielder(trait);
                        ApplySpecialTrait(trait, affectorAgent, affectedAgent, coolDownOnAffectedAgent, blow, affectorWeapon, attackCollisionData);
                    }
                }
            }

            if (affectedAgent.IsHero)
            {
                var items = new List<ItemObject>();
                if (attackCollisionData.CollidedWithShieldOnBack || attackCollisionData.AttackBlockedWithShield || attackCollisionData.IsShieldBroken)
                {
                    if (attackCollisionData.IsShieldBroken)
                    {
                        var item = FindBrokenShield(affectedAgent);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                    else if (affectedAgent.WieldedOffhandWeapon.Item != null)
                    {
                        items.Add(affectedAgent.WieldedOffhandWeapon.Item);
                    }

                }
                else
                {
                    items = affectedAgent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot);
                }

                var traits = new List<ItemTrait>();

                foreach (var item in items)
                {
                    traits.AddRange(item.GetTraits());
                }

                if (affectedAgent.Health <= 0f &&
                    (attackCollisionData.CollidedWithShieldOnBack ||
                     attackCollisionData.AttackBlockedWithShield ||
                     attackCollisionData.IsShieldBroken))
                {
                    foreach (var armorItem in affectedAgent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot))
                    {
                        traits.AddRange(armorItem.GetTraits().WhereQ(trait =>
                        {
                            var scriptType = GetWeaponHitScriptType(trait);

                            return scriptType != null &&
                                   typeof(ReviveScript).IsAssignableFrom(scriptType);
                        }));
                    }
                }

                var simpleStatusEffects = traits.Where(x => x.ImbuedStatusEffectId != "none" && x.ImbuedStatusEffectId != null);
                if (simpleStatusEffects != null && simpleStatusEffects.Count() > 0)
                {
                    foreach (var trait in simpleStatusEffects)
                    {
                        if (MBRandom.RandomFloat > trait.ImbuedEffectChance)
                        {
                            continue;
                        }
                        ApplyWeaponStatusEffect(affectedAgent, trait.ImbuedStatusEffectId, affectedAgent, 5, false);
                    }
                }
                var onHitTraits = traits.WhereQ(x => x.OnWeaponHitScript != null && !string.IsNullOrWhiteSpace(x.OnWeaponHitScript.WeaponScriptName) && x.OnWeaponHitScript.WeaponScriptName != "invalid");
                if (onHitTraits != null && onHitTraits.Count() > 0)
                {
                    foreach (var trait in onHitTraits)
                    {
                        ApplySpecialTrait(trait, affectorAgent, affectedAgent, true, blow, affectorWeapon, attackCollisionData);
                    }
                }
            }
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            _traitCoolDownMap.Remove(affectedAgent.Index);
        }

        /// <summary>
        /// this is to find a currently "destroyed" shield a last time to trigger it�s effect.
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        private ItemObject FindBrokenShield(Agent agent)
        {
            var possibleItems = agent.GetHero()?.CharacterObject.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3).Where(x => x.IsShield() && x.HasAnyTrait());
            ItemObject targetItem = null;
            foreach (var itemObject in possibleItems)
            {
                targetItem = itemObject;
                for (int i = 0; i < 3; i++)
                {
                    var equipment = agent.Equipment[i];
                    if (equipment.Item == itemObject)
                    {
                        targetItem = null;
                        break;
                    }
                }
            }

            return targetItem;
        }
        private static bool ShouldCooldownOnWielder(ItemTrait trait)
        {
            var scriptType = GetWeaponHitScriptType(trait);
            if (scriptType == null ||
                !typeof(WeaponTriggerEffectScript).IsAssignableFrom(scriptType))
            {
                return false;
            }
            var arguments = trait.OnWeaponHitScript.WeaponScriptArguments;
            if (arguments == null || arguments.Count < 2)
            {
                return false;
            }

            if (!bool.TryParse(arguments[1], out var appliesAroundAttacker))
            {
                return false;
            }
            // aoe procs using wielder cooldown to prevent per agent cd
            return appliesAroundAttacker;
        }
        private static string GetWeaponTriggerEffectId(ItemTrait trait)
        {
            var arguments = trait?.OnWeaponHitScript?.WeaponScriptArguments;
            if (arguments == null || arguments.Count == 0)
            {
                return "";
            }

            return arguments[0];
        }
        private static Type GetWeaponHitScriptType(ItemTrait trait)
        {
            var scriptName = trait?.OnWeaponHitScript?.WeaponScriptName;
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                return null;
            }

            return AccessTools.TypeByName(scriptName);
        }
        private static bool CanAttemptSpecialTrait(ItemTrait trait, Agent affectedAgent)
        {
            var scriptType = GetWeaponHitScriptType(trait);
            if (scriptType == null)
            {
                return true;
            }

            if (typeof(ReviveScript).IsAssignableFrom(scriptType))
            {
                return affectedAgent.Health <= 0f;
            }

            if (affectedAgent.Health > 0f)
            {
                return true;
            }

            return typeof(TriggerOnKillScript).IsAssignableFrom(scriptType) ||
                   typeof(KnockOutCheckTriggerScript).IsAssignableFrom(scriptType) ||
                   typeof(BuffStackOnKill).IsAssignableFrom(scriptType);
        }

        private static bool CanUseOffensiveWeaponTraitTarget(Agent affectedAgent, Agent affectorAgent)
        {
            return affectedAgent != null &&
                   affectorAgent != null &&
                   affectedAgent != affectorAgent &&
                   affectedAgent.IsActive() &&
                   !affectedAgent.IsFadingOut() &&
                   (Mission.Current == null || Mission.Current.FindAgentWithIndex(affectedAgent.Index) == affectedAgent);
        }

        private static bool CanApplyOffensiveWeaponTrait(ItemTrait trait, Agent affectedAgent, Agent affectorAgent, Blow blow)
        {
            if (!CanUseOffensiveWeaponTraitTarget(affectedAgent, affectorAgent))
            {
                return false;
            }

            if (IsTriggerOnKillScript(trait))
            {
                return affectedAgent.Health <= 0f && !blow.IsMissile;
            }

            return affectedAgent.Health > 0f;
        }

        private static bool IsTriggerOnKillScript(ItemTrait trait)
        {
            var scriptName = trait?.OnWeaponHitScript?.WeaponScriptName;

            return string.Equals(scriptName, typeof(TriggerOnKillScript).FullName, StringComparison.Ordinal) ||
                   string.Equals(scriptName, nameof(TriggerOnKillScript), StringComparison.Ordinal);
        }

        private bool IsTraitOnCooldown(ItemTrait trait, Agent targetAgent)
        {
            if (trait == null || targetAgent == null)
            {
                return false;
            }

            var cooldownId = GetTraitCooldownId(trait);
            if (string.IsNullOrWhiteSpace(cooldownId))
            {
                return false;
            }

            if (!_traitCoolDownMap.TryGetValue(targetAgent.Index, out var traitCooldowns))
            {
                return false;
            }

            return traitCooldowns.TryGetValue(cooldownId, out var cooldownEndTime) &&
                   cooldownEndTime > Mission.Current.CurrentTime;
        }

        private void RegisterTraitCooldown(ItemTrait trait, Agent targetAgent)
        {
            if (trait == null || targetAgent == null)
            {
                return;
            }

            var cooldownId = GetTraitCooldownId(trait);
            if (string.IsNullOrWhiteSpace(cooldownId))
            {
                return;
            }

            if (!_traitCoolDownMap.TryGetValue(targetAgent.Index, out var traitCooldowns))
            {
                traitCooldowns = new Dictionary<string, float>();
                _traitCoolDownMap.Add(targetAgent.Index, traitCooldowns);
            }

            traitCooldowns[cooldownId] = Mission.Current.CurrentTime + _triggerCooldown;
        }

        private static string GetTraitCooldownId(ItemTrait trait)
        {
            var cooldownId = trait.ItemTraitStringId;
            if (string.IsNullOrWhiteSpace(cooldownId))
            {
                cooldownId = trait.OnWeaponHitScript?.WeaponScriptName;
            }

            if (string.IsNullOrWhiteSpace(cooldownId) ||
                cooldownId.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return cooldownId;
        }

        private static bool CanReceiveWeaponStatusEffect(Agent agent)
        {
            return agent != null && agent.IsHuman && agent.IsActive() && agent.Health > 0f && !agent.IsFadingOut();
        }
        private static void ApplyWeaponStatusEffect(Agent target, string effectId, Agent applierAgent, float duration, bool append = false, bool isMutated = false)
        {
            if (target == null ||
                string.IsNullOrWhiteSpace(effectId))
            {
                return;
            }

            var abilityLogic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (abilityLogic != null)
            {
                abilityLogic.QueueTriggeredStatusEffect(target, effectId, applierAgent, duration, append, isMutated);

                return;
            }

            if (!CanReceiveWeaponStatusEffect(target))
            {
                return;
            }
            target.ApplyStatusEffect(effectId, applierAgent, duration, append, isMutated);
        }
        private void ApplySpecialTrait(ItemTrait trait, Agent affectorAgent, Agent affectedAgent, bool cooldownOnTarget, Blow blow, MissionWeapon affectorWeapon, AttackCollisionData attackCollisionData, bool fromMissile = false)
        {
            var targetAgent = cooldownOnTarget ? affectedAgent : affectorAgent;
            if (targetAgent == null)
            {
                return;
            }

            if (!CanAttemptSpecialTrait(trait, affectedAgent))
            {
                return;
            }

            if (MBRandom.RandomFloatRanged(0f, 1f) > trait.ImbuedEffectChance)
            {
                return;
            }

            var appliesAroundAttacker = ShouldCooldownOnWielder(trait);
            var cooldownOnWielder = !cooldownOnTarget;
            var triggeredEffectId = GetWeaponTriggerEffectId(trait);

            if (IsTraitOnCooldown(trait, targetAgent))
            {
                return;
            }

            try
            {
                object script;
                var scriptType = GetWeaponHitScriptType(trait);

                if (scriptType == null)
                {
                    TORCommon.Log(
                        "weapon on-hit script type not found" +" | script=" + trait.OnWeaponHitScript.WeaponScriptName +" | trait=" + trait.ItemTraitStringId +" | args=" + string.Join(",", trait.OnWeaponHitScript.WeaponScriptArguments ?? []),
                        NLog.LogLevel.Error);

                    return;
                }

                if (trait.OnWeaponHitScript.WeaponScriptArguments != null && trait.OnWeaponHitScript.WeaponScriptArguments.Count > 0)
                {
                    script = Activator.CreateInstance(scriptType, [trait.OnWeaponHitScript.WeaponScriptArguments.ToArray()]);
                }
                else
                {
                    script = Activator.CreateInstance(scriptType);
                }
                if (script is BaseWeaponHitScript weaponHitScript)
                {
                    weaponHitScript.OnHit(affectorAgent, affectedAgent, blow, affectorWeapon, attackCollisionData);
                    RegisterTraitCooldown(trait, targetAgent);

                }

            }
            catch (Exception ex)
            {
                TORCommon.Log("weapon on-hit script failed" + " | script=" + trait.OnWeaponHitScript.WeaponScriptName + " | trait=" + trait.ItemTraitStringId + " | args=" + string.Join(",", trait.OnWeaponHitScript.WeaponScriptArguments ?? []) + " | exception=" + ex.GetType().Name + " | message=" + ex.Message,
                    NLog.LogLevel.Error);
            }

        }

        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            if (attacker == victim || attacker == null)
            {
                return;
            }

            // TODO: ranged hits are also evaluated in OnAgentHit using the weapon stored on the missile
            // needs a single missile hit path that preserves the launch weapon context
            if (!HasWeaponWithTrait(attacker, out var traits))
            {
                return;
            }

            bool canApplyTraitsToVictim = victim != null && victim.IsActive() && victim.Health > 0f && !victim.IsFadingOut();

            if (canApplyTraitsToVictim)
            {
                foreach (var trait in traits)
                {
                    if (!collisionData.MissileBlockedWithWeapon &&
                        !collisionData.AttackBlockedWithShield &&
                        !string.IsNullOrWhiteSpace(trait.ImbuedStatusEffectId) &&
                        !trait.ImbuedStatusEffectId.Equals("none", StringComparison.OrdinalIgnoreCase))
                    {
                        var chance = MBRandom.RandomFloat;

                        if (chance <= trait.ImbuedEffectChance)
                        {
                            ApplyWeaponStatusEffect(victim, trait.ImbuedStatusEffectId, attacker, 5, false);
                        }
                    }

                    // StarfireEssencePassive3: Troops with Starfire Shafts also apply fire vulnerability
                    if (Campaign.Current != null && trait.ItemTraitStringId == "ca_starfire_shards" &&
                        !attacker.IsMainAgent && attacker.BelongsToMainParty() &&
                        Hero.MainHero.HasCareerChoice("StarfireEssencePassive3"))
                    {
                        ApplyWeaponStatusEffect(victim, "starfire_fire_vulnerability", attacker, 6, false);
                    }
                }

                var onHitTraits = traits.WhereQ(x =>
                    x.OnWeaponHitScript != null &&
                    !string.IsNullOrWhiteSpace(x.OnWeaponHitScript.WeaponScriptName) &&
                    x.OnWeaponHitScript.WeaponScriptName != "invalid");

                if (onHitTraits != null && onHitTraits.Any())
                {
                    foreach (var trait in onHitTraits)
                    {
                        ApplySpecialTrait(trait, attacker, victim, false, default, attacker.WieldedWeapon, collisionData, true);
                    }
                }
            }

            var missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;
            var targetMissile = Mission.Current.MissilesList.FirstOrDefault(x => x.Index == missileIndex);
            targetMissile?.Entity.RemoveAllParticleSystems();
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody,
            int forcedMissileIndex)
        {
            var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;

            if (weaponData != null && shooterAgent != null)
            {
                var missile = Mission.Current.MissilesList.FirstOrDefault(X => X.ShooterAgent == shooterAgent);

                if (missile != null)
                {

                    if (HasWeaponWithTrait(shooterAgent, out var traits))
                    {
                        foreach (var trait in traits)
                        {
                            if (trait.WeaponParticlePreset != null)
                            {
                                missile.Entity.AddParticleSystemComponent(trait.WeaponParticlePreset.ParticlePrefab);
                            }
                        }
                    }

                    // Lethal Shot consumption - only for player with Waywatcher career - in future maybe more
                    if (shooterAgent.IsMainAgent &&
                        Campaign.Current != null &&
                        Hero.MainHero.HasCareer(TORCareers.Waywatcher))
                    {
                        var weapon = shooterAgent.WieldedWeapon;
                        if (!weapon.IsEmpty && weapon.Item != null)
                        {
                            var weaponTraits = weapon.Item.GetTraits(shooterAgent);
                            if (weaponTraits != null && weaponTraits.Any(t => t.ItemTraitStringId.StartsWith("ca_lethal_shot")))
                            {
                                HandleLethalShotConsumption(shooterAgent);
                            }
                        }
                    }
                }
            }
        }

        private bool HasWeaponWithTrait(Agent agent, out List<ItemTrait> list)
        {
            list = [];
            if (agent.IsHuman)
            {
                var weapon = agent.WieldedWeapon;
                if (!weapon.IsEmpty)
                {
                    if (weapon.Item != null)
                    {
                        var effects = weapon.Item.GetTraits(agent);
                        if (effects != null && !effects.IsEmpty())
                        {
                            list = effects;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void HandleLethalShotConsumption(Agent shooterAgent)
        {
            var statusEffectComponent = shooterAgent.GetComponent<StatusEffectComponent>();

            if (statusEffectComponent == null) return;

            var lethalShots = statusEffectComponent.GetTemporaryAttributes(true)
                .Where(x => x == "LethalShot").ToList();

            if (lethalShots.Count == 0) return;

            // Remove one stack
            statusEffectComponent.RemoveStatusEffect("lethal_shot");

            // Check if any stacks remain
            lethalShots = statusEffectComponent.GetTemporaryAttributes(true)
                .Where(x => x == "LethalShot").ToList();

            if (lethalShots.Count > 0) return;

            // No stacks left - remove all Lethal Shot traits
            var weaponComponent = shooterAgent.GetComponent<ItemTraitAgentComponent>();

            if (weaponComponent != null)
            {
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_hagbane");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_loec");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_scatter");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_reload");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_pierce");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_swiftshiver");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_speed");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_starfire");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_starfire_shield");
                weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_moonfire");
            }
        }
    }
}