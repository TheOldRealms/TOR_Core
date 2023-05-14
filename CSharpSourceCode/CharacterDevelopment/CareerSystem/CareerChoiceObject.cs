using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

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
            TextObject descriptionOverride;
            if(GameTexts.TryGetText("careerchoice_description", out descriptionOverride, StringId))
            {
                if(Passive != null)
                {
                    if (Passive.InterpretAsPercentage) GameTexts.SetVariable("EFFECT_VALUE", Passive.EffectMagnitude.ToString("R"));
                    else GameTexts.SetVariable("EFFECT_VALUE", Passive.EffectMagnitude.ToString());
                }
                description = descriptionOverride.ToString();
            }
            base.Initialize(new TextObject(StringId), new TextObject(description));
            OwnerCareer = ownerCareer;
            if (!string.IsNullOrEmpty(belongsToGroup))
            {
                BelongsToGroup = MBObjectManager.Instance.GetObject<CareerChoiceGroupObject>(x => x.StringId == belongsToGroup);
            }
            else BelongsToGroup = null;
            if (BelongsToGroup != null) BelongsToGroup.Choices.Add(this);
            if(mutations != null) _mutations.AddRange(mutations);
            Passive = passiveEffect;
            if (isRootNode) OwnerCareer.RootNode = this;
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
            public bool InterpretAsPercentage = true;
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
}
