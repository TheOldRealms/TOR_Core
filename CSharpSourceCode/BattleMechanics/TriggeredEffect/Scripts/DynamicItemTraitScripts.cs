using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts
{
    
    public class ApplySwiftShiverTrait : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
  
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Magical;
                additionalDamage.Percent = 0.15f;
                var trait = new ItemTrait
                {
                    ItemTraitName = "Swiftshiver shards Trait",
                    ItemTraitDescription = "The damage is increased by 20% extra magical damage",
                    WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "magic_trait" },
                    AdditionalDamageTuple = additionalDamage,
                    OnHitScriptName = "none"
                };

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        TraitHelper.ApplyEffectToRangedWeapons(comp, trait, agent, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyHagbaneTrait : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait
                {
                    ItemTraitName = "Hagbane Trait",
                    ItemTraitDescription = "The weapon has been poisoned. Slows down enemies",
                    ImbuedStatusEffectId = "hagbane_debuff",
                    WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "hagbane_trait" },
                    OnHitScriptName = "none"
                };

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        TraitHelper.ApplyEffectToRangedWeapons(comp, trait, agent, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyStarFireTrait : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait
                {
                    ItemTraitName = "Starfire shards Trait",
                    ItemTraitDescription = "Adds Armor penetration effect, fire damage and dot",
                    ImbuedStatusEffectId = "starfire_debuff",
                    WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_flaming_weapon" },
                    AdditionalDamageTuple = new DamageProportionTuple
                    {
                        DamageType = DamageType.Fire, Percent = 0.20f
                    },
                    OnHitScriptName = "none"
                };
                
                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        TraitHelper.ApplyEffectToRangedWeapons(comp, trait, agent, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyFlamingItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Fire;
                additionalDamage.Percent = 0.25f;
                
                trait.ItemTraitName = "Flaming Sword";
                trait.ItemTraitDescription = "This sword is on fire. It deals fire damage and applies the burning damage over time effect.";
                trait.ImbuedStatusEffectId = "fireball_dot";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_flaming_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyLesserFlamingItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Fire;
                additionalDamage.Percent = 0.15f;
                
                trait.ItemTraitName = "Lesser Flaming Sword";
                trait.ItemTraitDescription = "This sword is on fire. It deals additional fire damage.";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_flaming_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyLesserLightItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Magical;
                additionalDamage.Percent = 0.25f;
                
                trait.ItemTraitName = "Hysh infused Sword";
                trait.ItemTraitDescription = "This sword is guided by Hysh. It deals magical damage.";
                trait.ImbuedStatusEffectId = "powerstone_light_mov_debuff";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_light_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyLightItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Magical;
                additionalDamage.Percent = 0.4f;
                
                trait.ItemTraitName = "Hysh infused Sword";
                trait.ItemTraitDescription = "This sword is guided by Hysh. It deals magical damage.";
                trait.ImbuedStatusEffectId = "powerstone_light_mov_debuff";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_light_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyLesserHeavensItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Lightning;
                additionalDamage.Percent = 0.2f;
                
                trait.ItemTraitName = "Azyr infused weapon";
                trait.ItemTraitDescription = "This weapon is guided by Azyr. It deals lightning damage.";
                trait.ImbuedStatusEffectId = "none";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "electric_weapon" };
                
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyHeavensItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Lightning;
                additionalDamage.Percent = 0.4f;
                
                trait.ItemTraitName = "Azyr infused weapon";
                trait.ItemTraitDescription = "This sword is guided by Azyr. It deals electrical damage.";
                trait.ImbuedStatusEffectId = "none";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "electric_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyGreaterHeavensItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Lightning;
                additionalDamage.Percent = 0.3f;
                
                additionalDamage.DamageType = DamageType.Frost;
                additionalDamage.Percent = 0.3f;
                
                trait.ItemTraitName = "Azyr infused weapon";
                trait.ItemTraitDescription = "This sword is guided by Azyr. It deals electrical damage.";
                trait.ImbuedStatusEffectId = "powerstone_heavens_debuff";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_heavens_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyMetalItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var trait2 = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();
                var additionalDamage2 = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Fire;
                additionalDamage.Percent = 0.2f;
                
                additionalDamage2.DamageType = DamageType.Magical;
                additionalDamage2.Percent = 0.2f;
                
                trait.ItemTraitName = "Chamon infused weapon";
                trait.ItemTraitDescription = "This weapon is guided by chamon. It deals lightning damage.";
                trait.ImbuedStatusEffectId = "none";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_flaming_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";
                
                trait2.AdditionalDamageTuple  = additionalDamage2;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                        comp.AddTraitToWieldedWeapon(trait2,duration);
                    }
                }
            }
        }
    }
    
    public class ApplyQuickSilverWeaponItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();
                additionalDamage.DamageType = DamageType.Physical;
                additionalDamage.Percent = 0.25f;
                trait.ItemTraitName = "Quick silver Weapon Enchantment";
                trait.ItemTraitDescription = "Quicksilver surrounds your weapons.";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_quicksilver_swords" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";
                var trait2 = new ItemTrait();
                
                var additionalDamage2 = new DamageProportionTuple();
                additionalDamage2.DamageType = DamageType.Magical;
                additionalDamage2.Percent = 0.25f;
                
                trait.ItemTraitName = "Quick silver Weapon Enchantment";
                trait.ItemTraitDescription = "Quicksilver surrounds your weapons.";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_quicksilver_swords" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                        comp.AddTraitToWieldedWeapon(trait2, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyHolyItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if(triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Holy;
                additionalDamage.Percent = 0.30f;
                
                trait.ItemTraitName = "Holy Weapon Enchantment";
                trait.ItemTraitDescription = "This sword is on fire. It deals fire damage and applies the burning damage over time effect.";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_holy_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }

    public class EnchantWeaponScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (triggeredAgents.Count() > 0)
            {
                var trait = new ItemTrait();
                var additionalDamage = new DamageProportionTuple();

                additionalDamage.DamageType = DamageType.Magical;
                additionalDamage.Percent = 0.10f;

                trait.ItemTraitName = "Enchanted Weapon";
                trait.ItemTraitDescription = "This weapon deals additional magic damage.";
                trait.ImbuedStatusEffectId = "none";
                trait.WeaponParticlePreset = new WeaponParticlePreset { ParticlePrefab = "psys_magic_weapon" };
                trait.AdditionalDamageTuple = additionalDamage;
                trait.OnHitScriptName = "none";

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
}
