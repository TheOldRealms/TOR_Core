using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Career;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Spells
{
    public class CareerAbility: Ability
    {
        private Agent Owner;
        public CareerAbility(AbilityTemplate template, Agent owner) : base(template)
        {
            this.Owner = owner;
        }
        
        public override void ActivateAbility(Agent casterAgent)
        {

            if (Game.Current.GameType is Campaign)
            {
                var career =Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }
            
            Template.AssociatedTriggeredEffectTemplate.ImbuedStatusEffectID = "fireball_dot";

            base.ActivateAbility(casterAgent);

         this.AbilityScript.OnEffectTriggeredSucessfull += PostTriggeredSucessfullEffect;
        }
        


        private void PostTriggeredSucessfullEffect(IEnumerable<Agent> affectedAgents, Agent caster)
        {
            TORCommon.Say("hello I triggered my effect on "+affectedAgents.Count() +  " agents");
            if (affectedAgents.Count() > 5)
            {
                Owner.ApplyStatusEffect("healing_regeneration",caster);
            }

            this.AbilityScript.OnEffectTriggeredSucessfull -= PostTriggeredSucessfullEffect;
        }
    }
}