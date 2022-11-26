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
        private CareerCampaignBase _career;
        public CareerAbility(AbilityTemplate template, Agent owner) : base(template)
        {
            this.Owner = owner;

            if (template.StartsOnCoolDown)
            {
                _coolDownLeft = Template.CoolDown;
                _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
                _timer.Start();
                _currentCharges = Template.Charges;
                
            }
            
            if (Game.Current.GameType is Campaign)
            {
                _career=Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }
            
        }
        
        public override void ActivateAbility(Agent casterAgent)
        {

         
            
           // Template.AssociatedTriggeredEffectTemplate.ImbuedStatusEffectID = "fireball_dot";

            base.ActivateAbility(casterAgent);

         this.AbilityScript.OnEffectTriggeredSucessfull += PostTriggeredSucessfullEffect;
        }

        public override bool CanCast(Agent casterAgent)
        {
            bool canCast;
            if (casterAgent.WieldedWeapon.IsEmpty) return false;
            
            var weapondata = casterAgent.WieldedWeapon.CurrentUsageItem;

            if (_career.HasRequiredWeaponFlags(weapondata.WeaponClass))
            {
                canCast = !casterAgent.HasMount || _career.CanBeUsedWhileMounted();
            }
            else
            {
                canCast= false;
            }

            return canCast && base.CanCast(casterAgent);
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