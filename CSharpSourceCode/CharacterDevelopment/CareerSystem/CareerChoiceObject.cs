using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerChoiceObject : PropertyObject
    {
        private List<CareerChoiceObject> _nextNodes = new List<CareerChoiceObject>();
        private MutationObject _mutation;
        public CareerObject OwnerCareer { get; private set; }

        public CareerChoiceObject(string stringId) : base(stringId) { }
        public override string ToString() => Name.ToString();

        public void Initialize(string name, string description, CareerObject ownerCareer, bool isRootNode, ChoiceType type, MutationObject mutation)
        {
            base.Initialize(new TextObject(name), new TextObject(description));
            OwnerCareer = ownerCareer;
            _mutation = mutation;
            if (isRootNode) OwnerCareer.RootNode = this;
        }

        public void MutateAbility(CareerAbility ability, Hero hero)
        {
            if(_mutation != null && _mutation.MutationTarget == typeof(CareerAbility) && _mutation.MutationType != MutationType.None && !string.IsNullOrEmpty(_mutation.FieldName) && _mutation.FieldValue != null)
            {
                var traverse = Traverse.Create(ability.Template);
                if (traverse.Field(_mutation.FieldName).FieldExists())
                {
                    switch (_mutation.MutationType)
                    {
                        case MutationType.Replace:
                            traverse.Field(_mutation.FieldName).SetValue(_mutation.FieldValue);
                            break;
                        case MutationType.Multiply:
                            var type = traverse.Field(_mutation.FieldName).GetValueType();
                            if (type == typeof(float))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<float>();
                                traverse.Field(_mutation.FieldName).SetValue(value * (1 + Convert.ToSingle(_mutation.FieldValue)));
                            }
                            else if(type == typeof(int))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<int>();
                                traverse.Field(_mutation.FieldName).SetValue(value * (1 + Convert.ToInt32(_mutation.FieldValue)));
                            }
                            break;
                        case MutationType.Add:
                            var type2 = traverse.Field(_mutation.FieldName).GetValueType();
                            if (type2 == typeof(float))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<float>();
                                traverse.Field(_mutation.FieldName).SetValue(value + Convert.ToSingle(_mutation.FieldValue));
                            }
                            else if (type2 == typeof(int))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<int>();
                                traverse.Field(_mutation.FieldName).SetValue(value + Convert.ToInt32(_mutation.FieldValue));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Hero hero)
        {
            if (_mutation != null && _mutation.MutationTarget == typeof(TriggeredEffectTemplate) && _mutation.MutationType != MutationType.None && !string.IsNullOrEmpty(_mutation.FieldName) && _mutation.FieldValue != null)
            {
                var traverse = Traverse.Create(effect);
                if (traverse.Field(_mutation.FieldName).FieldExists())
                {
                    switch (_mutation.MutationType)
                    {
                        case MutationType.Replace:
                            traverse.Field(_mutation.FieldName).SetValue(_mutation.FieldValue);
                            break;
                        case MutationType.Multiply:
                            var type = traverse.Field(_mutation.FieldName).GetValueType();
                            if (type == typeof(float))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<float>();
                                traverse.Field(_mutation.FieldName).SetValue(value * (1 + Convert.ToSingle(_mutation.FieldValue)));
                            }
                            else if (type == typeof(int))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<int>();
                                traverse.Field(_mutation.FieldName).SetValue(value * (1 + Convert.ToInt32(_mutation.FieldValue)));
                            }
                            break;
                        case MutationType.Add:
                            var type2 = traverse.Field(_mutation.FieldName).GetValueType();
                            if (type2 == typeof(float))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<float>();
                                traverse.Field(_mutation.FieldName).SetValue(value + Convert.ToSingle(_mutation.FieldValue));
                            }
                            else if (type2 == typeof(int))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<int>();
                                traverse.Field(_mutation.FieldName).SetValue(value + Convert.ToInt32(_mutation.FieldValue));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void MutateStatusEffect(StatusEffect effect, Hero hero)
        {
            if (_mutation != null && _mutation.MutationTarget == typeof(StatusEffectTemplate) && _mutation.MutationType != MutationType.None && !string.IsNullOrEmpty(_mutation.FieldName) && _mutation.FieldValue != null)
            {
                var traverse = Traverse.Create(effect);
                if (traverse.Field(_mutation.FieldName).FieldExists())
                {
                    switch (_mutation.MutationType)
                    {
                        case MutationType.Replace:
                            traverse.Field(_mutation.FieldName).SetValue(_mutation.FieldValue);
                            break;
                        case MutationType.Multiply:
                            var type = traverse.Field(_mutation.FieldName).GetValueType();
                            if (type == typeof(float))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<float>();
                                traverse.Field(_mutation.FieldName).SetValue(value * (1 + Convert.ToSingle(_mutation.FieldValue)));
                            }
                            else if (type == typeof(int))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<int>();
                                traverse.Field(_mutation.FieldName).SetValue(value * (1 + Convert.ToInt32(_mutation.FieldValue)));
                            }
                            break;
                        case MutationType.Add:
                            var type2 = traverse.Field(_mutation.FieldName).GetValueType();
                            if (type2 == typeof(float))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<float>();
                                traverse.Field(_mutation.FieldName).SetValue(value + Convert.ToSingle(_mutation.FieldValue));
                            }
                            else if (type2 == typeof(int))
                            {
                                var value = traverse.Field(_mutation.FieldName).GetValue<int>();
                                traverse.Field(_mutation.FieldName).SetValue(value + Convert.ToInt32(_mutation.FieldValue));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public class MutationObject
        {
            public Type MutationTarget { get; set; }
            public string FieldName { get; set; } = string.Empty;
            public object FieldValue { get; set; } = null;
            public MutationType MutationType { get; set; } = MutationType.None;
        }
    }

    public enum ChoiceType
    {
        Keystone,
        Passive
    }

    public enum MutationType
    {
        Add,
        Multiply,
        Replace,
        None
    }
}
