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
