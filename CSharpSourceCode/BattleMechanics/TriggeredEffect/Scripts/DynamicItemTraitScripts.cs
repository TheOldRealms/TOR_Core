using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts
{
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
                trait.ImbuedStatusEffectId = "none";
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
