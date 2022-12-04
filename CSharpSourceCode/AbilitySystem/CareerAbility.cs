using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbility : Ability
    {
        private Hero _ownerHero = null;
        private CareerObject _career = null;
        public float ChargeLevel { get; private set; }
        public bool IsCharged => ChargeLevel >= 100f;
        public ChargeType ChargeType { get; set; } = ChargeType.CooldownOnly;

        public CareerAbility(AbilityTemplate template, Agent agent) : base(template)
        {
            _ownerHero = agent.GetHero();
            if(_ownerHero != null)
            {
                _career = TORCareers.All.FirstOrDefault(x => x.AbilityTemplateID == Template.StringID);
                if(_career != null)
                {
                    Template = template.Clone(template.StringID + "_modified_" + _ownerHero.StringId);
                    _career.MutateAbility(this, _ownerHero);
                }
            }
        }

        protected override void AddExactBehaviour<TAbilityScript>(GameEntity parentEntity, Agent casterAgent)
        {
            if(_career.AbilityScriptType != null)
            {
                parentEntity.CreateAndAddScriptComponent(_career.AbilityScriptType.Name);
                AbilityScript = parentEntity.GetFirstScriptOfType<TAbilityScript>();
                var prefabEntity = SpawnEntity();
                parentEntity.AddChild(prefabEntity);
                AbilityScript?.Initialize(this);
                AbilityScript?.SetAgent(casterAgent);
                parentEntity.CallScriptCallbacks();
            }
            else base.AddExactBehaviour<TAbilityScript>(parentEntity, casterAgent);
        }

        public override void ActivateAbility(Agent casterAgent)
        {
            base.ActivateAbility(casterAgent);
            ChargeLevel = 0;
        }

        public override bool CanCast(Agent casterAgent)
        {
            return !IsCasting &&
                   !IsOnCooldown() &&
                   IsCharged &&
                   !casterAgent.HasMount &&
                   casterAgent.IsPlayerControlled;
        }

        public void AddCharge(float amount)
        {
            ChargeLevel += amount;
            ChargeLevel = Math.Min(100, ChargeLevel);
        }
    }

    public enum ChargeType
    {
        CooldownOnly,
        NumberOfKills,
        DamageDone
    }
}
