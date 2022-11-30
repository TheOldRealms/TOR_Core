using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
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
        private AbilityTemplate _ability;
        
        //None Timer based Cooldown



        public CareerAbility(AbilityTemplate template, Agent owner) : base(template)
        {
            this.Owner = owner;
           // _maximumCoolDownCharge = template.ChargeRequirement;
            if (template.StartsOnCoolDown)
            {
                _coolDownLeft = Template.CoolDown;
                _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
                _timer.Start();
                _currentCharge = 0;

            }

            _currentCharge = Template.Charge;

           // _currentCharge = Math.Min(_maximumCoolDownCharge, _currentCharge);

            if (Game.Current.GameType is Campaign)
            {
                _career=Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }
            Template.AssociatedTriggeredEffectTemplate.ImbuedStatusEffectID = "fireball_dot";
            
            
            //Set here career ability overrides
            
        }
        
        

        /*protected override void InitCoolDown()
        {
            if (Template.ChargeType != ChargeType.Time)
            {
                var charges = _leftUsages;
                charges--;
                if (charges > 0)
                {
                    base.InitCoolDown();
                }
                else
                {
                    _currentCharge = 0;
                    //Cooldown Timer set insible
                    //
                }
                
            }
            else
            {
                base.InitCoolDown();
            }
           
            
        }*/

        /*public override bool ReachedChargeRequirement(out float percentage)
        {
            percentage = Mathf.Clamp((float)_currentCharge /(float) _maximumCoolDownCharge,0,1);
         
            return _currentCharge >= _maximumCoolDownCharge;
        }*/
        
        public bool IsUsing
        {
            get
            {
                return (ShadowStepScript)AbilityScript != null && !((ShadowStepScript)AbilityScript).IsFadinOut;
            }
        }


        public override void ActivateAbility(Agent casterAgent)
        {

            base.ActivateAbility(casterAgent);

         this.AbilityScript.OnEffectTriggeredSucessfull += PostTriggeredSucessfullEffect;
        }

        public override bool CanCast(Agent casterAgent)
        {
            bool canCast;
            if (casterAgent.WieldedWeapon.IsEmpty) return false;

            if (Template.ChargeType != ChargeType.Time)
            {
                if (!ReachedChargeRequirement(out float t)) return false;
            }
            
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