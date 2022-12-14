﻿using System;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class SpecialMove : Ability
    {
        public SpecialMove(AbilityTemplate template) : base(template)
        {

        }

        public override void ActivateAbility(Agent casterAgent)
        {
            base.ActivateAbility(casterAgent);
            _chargeLevel = 0;
        }

        public override bool CanCast(Agent casterAgent)
        {
            return !IsCasting &&
                   !IsOnCooldown() &&
                   IsCharged &&
                   (casterAgent.IsPlayerControlled || (casterAgent.IsActive() && casterAgent.Health > 0 && casterAgent.GetMorale() > 1 && casterAgent.IsAbilityUser())) &&
                   !casterAgent.HasMount;
        }

        public void AddCharge(float amount)
        {
            _chargeLevel += amount;
            _chargeLevel = Math.Min(100, _chargeLevel);
        }

        public bool IsUsing
        {
            get
            {
                return (ShadowStepScript)AbilityScript != null && !((ShadowStepScript)AbilityScript).IsFadinOut;
            }
        }

        public bool IsCharged => _chargeLevel >= 100f;
        private float _chargeLevel = 50f;
        public float ChargeLevel => _chargeLevel;
    }
}
