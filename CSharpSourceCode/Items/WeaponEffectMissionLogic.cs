using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public class WeaponEffectMissionLogic : MissionLogic
    {
        private List<Mission.Missile> _missiles;
        
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
            if(Agent.Main != null && Agent.Main.GetComponent<ItemTraitAgentComponent>() != null)
            {
                Agent.Main.GetComponent<ItemTraitAgentComponent>().OnTickAsMainAgent(dt);
            }
            
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (affectedAgent == affectorAgent)
                return;

            if (affectorWeapon.Item != null && affectorWeapon.Item.HasTrait(affectorAgent))
            {
                var relevantTraits = affectorWeapon.Item.GetTraits(affectorAgent).Where(x => x.ImbuedStatusEffectId != "none");
                if (relevantTraits != null && relevantTraits.Count() > 0)
                {
                    foreach (var trait in relevantTraits)
                    {
                        affectedAgent.ApplyStatusEffect(trait.ImbuedStatusEffectId, affectorAgent, 5, false);
                    }
                }
                //TODO: disabling this for first release, we dont actually have an item script. This just clogs system resources and spams the screen with debug messages.
                /*
                if(magiceffect.OnHitScriptName != "none")
                {
                    try
                    {
                        var obj = Activator.CreateInstance(Type.GetType(magiceffect.OnHitScriptName));
                        if(obj is IMagicWeaponHitEffect)
                        {
                            var script = obj as IMagicWeaponHitEffect;
                            script.OnHit(affectedAgent, affectorAgent, affectorWeapon);
                        }
                    }
                    catch(Exception)
                    {
                        TOW_Core.Utilities.TORCommon.Log("Tried to create magicweapon onhitscript: " + magiceffect.OnHitScriptName + ", but failed.", NLog.LogLevel.Error);
                    }
                }
                */
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
                    
                    if (traits != null && traits.Count() > 0 && victim!=null && !collisionData.MissileBlockedWithWeapon || collisionData.AttackBlockedWithShield)
                    {
                        foreach (var trait in traits)
                        {
                            if (trait.ImbuedStatusEffectId != "none")
                            {
                                var chance = MBRandom.RandomFloat;

                                if (chance <= trait.ImbuedStatusEffectChance)
                                {
                                    victim.ApplyStatusEffect(trait.ImbuedStatusEffectId, attacker, 5, false);
                                }
                            }
               
                        }
                    }
                    
                    var missileIndex = collisionData.AffectorWeaponSlotOrMissileIndex;
                    var targetMissile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == missileIndex);
                    targetMissile.Entity.RemoveAllParticleSystems();
                }
            }
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody,
            int forcedMissileIndex)
        {
            var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;

            if (weaponData != null && shooterAgent != null)
            {
                var missile = Mission.Current.Missiles.FirstOrDefault(X => X.ShooterAgent == shooterAgent );

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
            list = new List<ItemTrait>();
            if (agent.IsHuman)
            {
                var weapon = agent.WieldedWeapon;
                if (!weapon.IsEmpty)
                {
                    if (weapon.Item != null)
                    {
                        var effects = weapon.Item.GetTraits(agent);
                        if (effects != null &&!effects.IsEmpty())
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
