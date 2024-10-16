using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerChoiceObject : PropertyObject
    {
        private List<MutationObject> _mutations = new List<MutationObject>();
        public CareerObject OwnerCareer { get; private set; }
        public CareerChoiceGroupObject BelongsToGroup { get; private set; }

        public CareerChoiceObject(string stringId) : base(stringId) { }
        public override string ToString() => Name.ToString();
        public PassiveEffect Passive { get; private set; }

        public void Initialize(CareerObject ownerCareer, string description, string belongsToGroup, bool isRootNode, ChoiceType type, List<MutationObject> mutations = null, PassiveEffect passiveEffect = null)
        {
            TextObject text;
            text = new TextObject(description);
            Passive = passiveEffect;
            if(GameTexts.TryGetText("careerchoice_description", out var descriptionOverride, StringId))
            {
                if(Passive != null)
                {
                    if (Passive.DamageProportionTuple != null)
                    {
                        var damageType = Passive.DamageProportionTuple.DamageType;
                        GameTexts.TryGetText("tor_damagetype",out var damageTypeText,damageType.ToString());
                        GameTexts.SetVariable("EFFECT_DAMAGE_TYPE",damageTypeText);

                        var attackType = Passive.AttackTypeMask;
                        GameTexts.TryGetText("tor_attacktype",out var attackTypeText,attackType.ToString());
                        GameTexts.SetVariable("EFFECT_ATTACK_TYPE",damageTypeText);
                        GameTexts.SetVariable("EFFECT_VALUE", (Passive.DamageProportionTuple.Percent).ToString("R"));
                    }
                    else
                    {
                        
                        
                        if (Passive.InterpretAsPercentage)
                        {
                            if (Passive.PassiveEffectType == PassiveEffectType.ArmorPenetration)
                            {
                                GameTexts.SetVariable("EFFECT_VALUE", (-Passive.EffectMagnitude).ToString("R"));
                            }
                            else
                            {
                                GameTexts.SetVariable("EFFECT_VALUE", Passive.EffectMagnitude.ToString("R"));
                            }
                        }
                        else 
                        {
                            
                            GameTexts.SetVariable("EFFECT_VALUE", Passive.EffectMagnitude.ToString());
                        }
                    }
                    
                }
                
                if (descriptionOverride != null)
                {
                    text = new TextObject(descriptionOverride.ToString());
                }
            }
            base.Initialize(new TextObject(StringId), text);
            OwnerCareer = ownerCareer;
            if (!string.IsNullOrEmpty(belongsToGroup))
            {
                BelongsToGroup = MBObjectManager.Instance.GetObject<CareerChoiceGroupObject>(x => x.StringId == belongsToGroup);
            }
            else BelongsToGroup = null;
            if (BelongsToGroup != null) BelongsToGroup.Choices.Add(this);
            if(mutations != null) _mutations.AddRange(mutations);
            if (isRootNode) OwnerCareer.RootNode = this;
            AfterInitialized();
        }

        public void MutateAbility(AbilityTemplate ability, Agent agent) => MutateObject(ability, agent);

        public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Agent agent) => MutateObject(effect, agent);

        public void MutateStatusEffect(StatusEffectTemplate effect, Agent agent) => MutateObject(effect, agent);

        private void MutateObject(ITemplate target, Agent agent)
        {
            var type = target.GetType();
            foreach(var mutation in _mutations)
            {
                if (mutation != null && mutation.MutationTargetType == type && mutation.MutationType != OperationType.None && !string.IsNullOrEmpty(mutation.MutationTargetOriginalId) && !string.IsNullOrEmpty(mutation.PropertyName) && mutation.PropertyValue != null)
                {
                    var originalId = target.StringID.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (originalId == null || originalId != mutation.MutationTargetOriginalId) continue;
                    var traverse = Traverse.Create(target);
                    if (traverse.Property(mutation.PropertyName).PropertyExists())
                    {
                        object newValue = mutation.PropertyValue(this, traverse.Property(mutation.PropertyName).GetValue(), agent);
                        var propertyType = traverse.Property(mutation.PropertyName).GetValueType();
                        switch (mutation.MutationType)
                        {
                            case OperationType.Replace:
                                if(newValue.GetType() == propertyType) 
                                {
                                    traverse.Property(mutation.PropertyName).SetValue(newValue);
                                }
                                break;
                            case OperationType.Multiply:
                                
                                if (propertyType == typeof(float))
                                {
                                    var value = traverse.Property(mutation.PropertyName).GetValue<float>();
                                    traverse.Property(mutation.PropertyName).SetValue(value * (1 + Convert.ToSingle(newValue)));
                                }
                                else if (propertyType == typeof(int))
                                {
                                    var value = traverse.Property(mutation.PropertyName).GetValue<int>();
                                    traverse.Property(mutation.PropertyName).SetValue(value * (1 + Convert.ToInt32(newValue)));
                                }
                                break;
                            case OperationType.Add:
                                if (propertyType == typeof(float))
                                {
                                    var value = traverse.Property(mutation.PropertyName).GetValue<float>();
                                    traverse.Property(mutation.PropertyName).SetValue(value + Convert.ToSingle(newValue));
                                }
                                else if (propertyType == typeof(int))
                                {
                                    var value = traverse.Property(mutation.PropertyName).GetValue<int>();
                                    traverse.Property(mutation.PropertyName).SetValue(value + Convert.ToInt32(newValue));
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        public class MutationObject
        {
            public Type MutationTargetType { get; set; }
            public string MutationTargetOriginalId { get; set; } = string.Empty;
            public string PropertyName { get; set; } = string.Empty;
            public Func<CareerChoiceObject, object, Agent, object> PropertyValue { get; set; } = null;
            public OperationType MutationType { get; set; } = OperationType.None;
        }

        public class PassiveEffect
        {
            public float EffectMagnitude = 0f;
            public OperationType Operation = OperationType.None;
            public PassiveEffectType PassiveEffectType = PassiveEffectType.Special;
            public bool InterpretAsPercentage = true;
            public bool WithFactorFlatSwitch;
            public DamageProportionTuple DamageProportionTuple;
            public AttackTypeMask AttackTypeMask = AttackTypeMask.Melee;
            
            public delegate bool SpecialCombatInteractionFunction(Agent attacker, Agent victim, AttackTypeMask mask);
            private readonly SpecialCombatInteractionFunction _specialCombatInteractionFunction;
            public delegate bool SpecialCharacterEvaluationFunction(CharacterObject characterObject);
            private readonly SpecialCharacterEvaluationFunction _specialCharacterEvaluationFunction;

            public bool IsValidCombatInteraction(Agent attacker, Agent victim, AttackTypeMask mask) =>  _specialCombatInteractionFunction==null||_specialCombatInteractionFunction.Invoke(attacker,victim, mask);

            public bool IsValidCharacterObject(CharacterObject characterObject) =>_specialCharacterEvaluationFunction==null || _specialCharacterEvaluationFunction.Invoke(characterObject);
            
            public PassiveEffect(float effectValue, PassiveEffectType type, AttackTypeMask mask)
            {
                EffectMagnitude = effectValue;
                Operation = OperationType.Add;
                InterpretAsPercentage = true;
                PassiveEffectType = type;
                AttackTypeMask = mask;
            }

            
            public PassiveEffect(PassiveEffectType type, DamageProportionTuple damageProportionTuple, AttackTypeMask mask, SpecialCombatInteractionFunction function = null)
            {
                InterpretAsPercentage = true;
                EffectMagnitude = 0;
                Operation = OperationType.Add;
                InterpretAsPercentage = true;
                PassiveEffectType = type;
                DamageProportionTuple = damageProportionTuple;
                AttackTypeMask = mask;
                _specialCombatInteractionFunction = function;
            }

            public PassiveEffect(float effectValue=0, PassiveEffectType type = PassiveEffectType.Special, bool asPercent=false,  SpecialCharacterEvaluationFunction function = null, bool withFactorFlatSwitch=false)
            {
                EffectMagnitude = effectValue;
                Operation = OperationType.Add;
                InterpretAsPercentage = asPercent;
                PassiveEffectType = type;
                _specialCharacterEvaluationFunction = function;
                WithFactorFlatSwitch = withFactorFlatSwitch;
            }
        }
        
        public float GetPassiveValue()
        {
            if (Passive == null)return 0;
            return Passive.InterpretAsPercentage ? Passive.EffectMagnitude / 100 : Passive.EffectMagnitude;
        }

        public bool HasMutations()
        {
            return !_mutations.IsEmpty();
        }
    }
    
    

    public enum ChoiceType
    {
        Keystone,
        Passive
    }

    public enum OperationType
    {
        Add,
        Multiply,
        Replace,
        None
    }

    public enum PassiveEffectType
    {
        Special,            //For everything that requires special implementation
        
        Health,             //Player health points, flat number
        CustomResourceUpkeepModifier, //scales custom resource upkeep
        CustomResourceUpgradeCostModifier, //scales custom upgrade costs
        CustomResourceGain, //daily gain for custom resource , flat number
        HealthRegeneration, //player life regeneration, as flat number
        Damage,             //player damage, requires damage tuple
        Resistance,         //player resistance requires damage tuple
        AccuracyPenalty,           //spray of ranged weapons
        RangedMovementPenalty, // inaccuracy for ranged weapons penality due to movement
        ArmorPenetration,   //player ignores armor with attack mask - this cant be Spells, will be ignored
        HorseHealth,        //only player, percentage based
        HorseChargeDamage,  //Damage When Horse is raced into infantry.
        WindsOfMagic,       //player Winds of Magic, as flat number
        WindsCostReduction, //player Winds of Magic cost reduction as Percentage
        WindsRegeneration,
        BuffDuration,       //Increases duration for friendly augments    
        DebuffDuration,     //Increases duration for hex
        SpellRadius,        //
        SpellEffectiveness, //Damage spell effectiveness - for direct effects
        WindsCooldownReduction, //player cooldown reduction as Percentage
        PrayerCoolDownReduction, //player cooldown reduction as Percentage
        PartyMovementSpeed, //general party speed
        PartySize,
        CompanionLimit,
        TroopDamage,
        TroopResistance,
        TroopRegeneration,  //troop regeneration, flat number
        TroopMorale,        //Morale
        TroopWages,         //Negative number decrease
        TroopUpgradeCost,
        Ammo,               //Player ammo , flat number
        SwingSpeed,
        EquipmentWeightReduction
    }
}
