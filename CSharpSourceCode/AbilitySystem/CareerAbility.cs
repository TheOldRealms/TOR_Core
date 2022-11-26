using System;
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
        private AbilityTemplate _ability;
        
        //None Timer based Cooldown
        private int _maximumCoolDownCharge;
        private int _currentCoolDownCharge;

        public CareerAbility(AbilityTemplate template, Agent owner) : base(template)
        {
            this.Owner = owner;
            _maximumCoolDownCharge = template.CoolDownRequirement;
            if (template.StartsOnCoolDown)
            {
                _coolDownLeft = Template.CoolDown;
                _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
                _timer.Start();
                _currentCoolDownCharge = 0;

            }
            else
            {
                _currentCoolDownCharge = template.CoolDownRequirement;
            }
            
            if (Game.Current.GameType is Campaign)
            {
                _career=Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }
            Template.AssociatedTriggeredEffectTemplate.ImbuedStatusEffectID = "fireball_dot";
            //Set here career ability overrides
            
        }
        
        

        protected override void InitCoolDown()
        {
            if (Template.CoolDownType != CoolDownType.Time)
            {
                var charges = _currentCharges;
                charges--;
                if (charges > 0)
                {
                    base.InitCoolDown();
                }
                else
                {
                    _currentCoolDownCharge = 0;
                    //Cooldown Timer set insible
                    //
                }
                
            }
            else
            {
                base.InitCoolDown();
            }
           
            
        }

        public bool ReachedCoolDownLoad()
        {
            return _currentCoolDownCharge >= _maximumCoolDownCharge;
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

            if (Template.CoolDownType != CoolDownType.Time)
            {
                if (!ReachedCoolDownLoad()) return false;
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
        
        public void AddCharge(int amount)
        {
            _currentCoolDownCharge += amount;
            _currentCoolDownCharge = Math.Min(_maximumCoolDownCharge, _currentCoolDownCharge);
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