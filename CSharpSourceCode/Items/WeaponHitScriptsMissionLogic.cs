using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items.WeaponHitScripts;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public class WeaponHitScriptsMissionLogic : MissionLogic
    {
        private static readonly float _triggerCooldown = 2;
        private Dictionary<int, Queue<string>> _traitCoolDownMap = new();
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
            if (_deltaTime > _triggerCooldown)
            {
                if (_traitCoolDownMap.AnyQ())
                {
                    foreach (var entry in _traitCoolDownMap.WhereQ(entry => entry.Value.AnyQ()))
                    {
                        entry.Value.Dequeue();
                    }
                }
            }
        }

        public override void OnBattleEnded()
        {
            base.OnBattleEnded();
            _traitCoolDownMap.Clear();
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (affectedAgent == affectorAgent)
                return;

            if (affectorWeapon.Item != null && affectorWeapon.Item.HasAnyTrait(affectorAgent))
            {
                var statusEffectTraits = affectorWeapon.Item.GetTraits(affectorAgent).Where(x => x.ImbuedStatusEffectId != "none" && x.ImbuedStatusEffectId != null);
                if (statusEffectTraits != null && statusEffectTraits.Count() > 0)
                {
                    foreach (var trait in statusEffectTraits)
                    {
                        if (MBRandom.RandomFloatNormal > trait.ImbuedEffectChance)
                        {
                            affectedAgent.ApplyStatusEffect(trait.ImbuedStatusEffectId, affectorAgent, 5, false);
                        }

                    }
                }

                var onHitTraits = affectorWeapon.Item.GetTraits(affectorAgent)
                    .WhereQ(x => x.OnWeaponHitScript != null && !string.IsNullOrWhiteSpace(x.OnWeaponHitScript.WeaponScriptName) && x.OnWeaponHitScript.WeaponScriptName != "invalid");

                if (onHitTraits != null && onHitTraits.Count() > 0)
                {
                    foreach (var trait in onHitTraits)
                    {
                        ApplySpecialTrait(trait, affectorAgent, affectedAgent, true, blow, affectorWeapon, attackCollisionData);
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
                    traits.AddRange(item.GetTraits().Where(x => x.OnWeaponHitScript != null).ToList());
                }

                var simpleStatusEffects = traits.Where(x => x.ImbuedStatusEffectId != "none" && x.ImbuedStatusEffectId != null);
                if (simpleStatusEffects != null && simpleStatusEffects.Count() > 0)
                {
                    foreach (var trait in simpleStatusEffects)
                    {
                        if (MBRandom.RandomFloatNormal > trait.ImbuedEffectChance)
                        {
                            continue;
                        }
                        affectedAgent.ApplyStatusEffect(trait.ImbuedStatusEffectId, affectedAgent, 5, false);
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

        /// <summary>
        /// this is to find a currently "destroyed" shield a last time to trigger it�s effect.
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        private ItemObject FindBrokenShield(Agent agent)
        {
            var possibleItems = agent.GetHero().CharacterObject.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3).Where(x => x.IsShield() && x.HasAnyTrait());
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


        private void ApplySpecialTrait(ItemTrait trait, Agent affectorAgent, Agent affectedAgent, bool isDefenceTrait, Blow blow, MissionWeapon affectorWeapon, AttackCollisionData attackCollisionData)
        {
            Agent targetAgent = null;

            targetAgent = isDefenceTrait ? affectedAgent : affectorAgent;
            if (MBRandom.RandomFloatRanged(0f, 1f) > trait.ImbuedEffectChance)
            {
                return;
            }
            if (_traitCoolDownMap.TryGetValue(targetAgent.Index, out var value))
            {
                if (value.Contains(trait.ItemTraitStringId))
                    return;
                _traitCoolDownMap[targetAgent.Index] = value;
            }
            else
            {
                _traitCoolDownMap.Add(targetAgent.Index, new Queue<string>([trait.ItemTraitStringId]));

            }

            try
            {
                object script;

                if (trait.OnWeaponHitScript.WeaponScriptArguments != null && trait.OnWeaponHitScript.WeaponScriptArguments.Count > 0)
                {
                    script = Activator.CreateInstance(Type.GetType(trait.OnWeaponHitScript.WeaponScriptName), [trait.OnWeaponHitScript.WeaponScriptArguments.ToArray()]);
                }
                else
                {
                    var type = Type.GetType(trait.OnWeaponHitScript.WeaponScriptName);
                    script = Activator.CreateInstance(type);
                }
                if (script is BaseWeaponHitScript weaponHitScript)
                {
                    weaponHitScript.OnHit(affectorAgent, affectedAgent, blow, affectorWeapon, attackCollisionData);
                }

            }
            catch (Exception)
            {
                TORCommon.Log("Tried to create magicweapon onhitscript: " + trait.OnWeaponHitScript.WeaponScriptName + ", but failed.", NLog.LogLevel.Error);
            }

        }

        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            if (attacker == victim)
                return;

            if (attacker != null)
            {
                if (HasWeaponWithTrait(attacker, out var traits))
                {

                    if (traits != null && traits.Count() > 0 && victim != null && !(collisionData.MissileBlockedWithWeapon || collisionData.AttackBlockedWithShield))
                    {
                        foreach (var trait in traits)
                        {
                            if (trait.ImbuedStatusEffectId != "none")
                            {
                                var chance = MBRandom.RandomFloat;

                                if (chance <= trait.ImbuedEffectChance)
                                {
                                    victim.ApplyStatusEffect(trait.ImbuedStatusEffectId, attacker, 5, false);
                                }
                            }

                            // StarfireEssencePassive3: Troops with Starfire Shafts also apply fire vulnerability
                            if (Campaign.Current != null && trait.ItemTraitStringId == "ca_starfire_shards" &&
                                !attacker.IsMainAgent && attacker.BelongsToMainParty() &&
                                Hero.MainHero.HasCareerChoice("StarfireEssencePassive3"))
                            {
                                victim.ApplyStatusEffect("starfire_fire_vulnerability", attacker, 6, false);
                            }
                        }
                    }

                    var missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;
                    var targetMissile = Mission.Current.MissilesList.FirstOrDefault(x => x.Index == missileIndex);
                    targetMissile.Entity.RemoveAllParticleSystems();
                }

                //TODO check if scripts are applied anyway for onHit scripts
            }
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
    }
}