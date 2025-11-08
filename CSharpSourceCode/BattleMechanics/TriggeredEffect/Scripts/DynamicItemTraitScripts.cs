using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_swiftshiver_shards");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_hagbane");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_starfire_shards");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "flaming_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if(comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }

    public class ApplyKnightlyStrikeTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            var additionalDamage = new DamageProportionTuple();
            additionalDamage.DamageType = DamageType.Physical;
            additionalDamage.Percent = 0.2f;
            
            var ca = triggeredByAgent?.GetComponent<AbilityComponent>().CareerAbility;

            var bonusdamage = 0f;
            if (ca != null)
            { 
                bonusdamage = ca.Template.ScaleVariable1;
            }
            additionalDamage.Percent += bonusdamage;

            var additionalLoads = Hero.MainHero.GetAllCareerChoices().WhereQ(x=> x.Contains("Keystone")).Count();

            if (Hero.MainHero.HasCareerChoice("SecularOrdersKeystone"))
            {
                additionalLoads += 2;
            }
            
            if (Hero.MainHero.HasCareerChoice("TemplarOrdersKeystone"))
            {
                additionalLoads += 2;
            }
            
            var traitList = new List<ItemTrait>();
            
            if (Hero.MainHero.HasCareerChoice("PathOfGloryKeystone"))
            {
                var holyTrait = CareerHelper.GetTraitForReligion(Hero.MainHero, Hero.MainHero.GetDominantReligion());
                if(holyTrait != null && holyTrait != ItemTrait.Invalid) traitList.Add(holyTrait);
            }

            var knightlytrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_knightlystrike");
            if (knightlytrait != null) traitList.Add(knightlytrait);

            triggeredByAgent.ApplyStatusEffect("knightly_strike",triggeredByAgent,30,false,false,true);
            for (int i = 0; i < additionalLoads; i++)
            {
                triggeredByAgent.ApplyStatusEffect("knightly_strike",triggeredByAgent,30,false,false,true);
            }
            
            foreach (Agent agent in triggeredAgents)
            {
                var comp = agent.GetComponent<ItemTraitAgentComponent>();
                if(comp != null)
                {
                    foreach (var trait in traitList)
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "lesser_flaming_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "lesser_hysh_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "hysh_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "lesser_azyr_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
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
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "azyr_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyDeathDamageItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "shyish_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
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
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "greater_azyr_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
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
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "chamon_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
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
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "quicksilver_weapon");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyHolyItemTraitScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "holy_weapon_30");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyAzyrForesightScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "emp_enchant_azyr_azure_mirror_troop");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
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
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "magical_weapon_10");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    public class ApplyTranquillityCadaiTrait : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (triggeredAgents.Count() > 0)
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "we_enchant_tranquillity_cadai_copy");
                if (trait == null) return;

                foreach (Agent agent in triggeredAgents)
                {
                    var comp = agent?.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null)
                    {
                        comp.AddTraitToWieldedWeapon(trait, duration);
                    }
                }
            }
        }
    }
    
    
    public class SpiritLeech: ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            var targets = triggeredAgents.ToList();
            if (targets.Count <= 0) return;
            var target = targets[0];
            target = targets.FirstOrDefaultQ(x => x.IsHero) ?? targets.MaxBy(x => x.Character.Level);
            var tier = target.Character.GetBattleTier();
            triggeredByAgent.ApplyStatusEffect("spirit_leech_heal",triggeredByAgent,tier * duration);
        }
    }
}
