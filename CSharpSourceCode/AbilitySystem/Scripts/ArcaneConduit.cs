using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ArcaneConduit : CareerAbilityScript
    {
        private bool _init;

        protected override void OnAfterTick(float dt)
        {
            base.OnAfterTick(dt);
            if (!_init)
            {
                _init = true;
                Init();
            }
        }

        private void Init()
        {
            var effects = GetEffectsToTrigger();
            
            if (Hero.MainHero.HasCareerChoice("CollegeOrdersKeystone"))
            {
                var heroAgents = Mission.Current.Agents.Where(x => x.IsHero).ToList();
                
                foreach (var agent in heroAgents)
                {
                    if (!agent.BelongsToMainParty() && !agent.IsSpellCaster()) continue;
                    
                    foreach (var effect in effects)
                    {
                        foreach (var statusEffect in effect.StatusEffects)
                        {
                            agent.ApplyStatusEffect(statusEffect, Agent.Main, effect.ImbuedStatusEffectDuration);
                        }
                    }
                }
            }

            if (Hero.MainHero.HasCareerChoice("ArcaneKnowledgeKeystone"))
            {
                var component = Agent.Main.GetComponent<AbilityComponent>();

                if (component != null)
                {
                    var abilities = component.KnownAbilitySystem;

                    foreach (var abilitySpell in abilities)
                    {
                        if (abilitySpell == Ability) continue;
                        abilitySpell.SetCoolDown(0);
                    }
                }
            }
        }
    }
}