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
        private List<MutationObject> _mutations = new List<MutationObject>();
        public CareerObject OwnerCareer { get; private set; }

        public CareerChoiceObject(string stringId) : base(stringId) { }
        public override string ToString() => Name.ToString();

        public void Initialize(string name, string description, CareerObject ownerCareer, bool isRootNode, ChoiceType type, List<MutationObject> mutations)
        {
            base.Initialize(new TextObject(name), new TextObject(description));
            OwnerCareer = ownerCareer;
            _mutations.AddRange(mutations);
            if (isRootNode) OwnerCareer.RootNode = this;
        }

        public void MutateAbility(AbilityTemplate ability, Hero hero) => MutateObject(ability, hero);

        public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Hero hero) => MutateObject(effect, hero);

        public void MutateStatusEffect(StatusEffectTemplate effect, Hero hero) => MutateObject(effect, hero);

        private void MutateObject(ITemplate target, Hero hero)
        {
            var type = target.GetType();
            foreach(var mutation in _mutations)
            {
                if (mutation != null && mutation.MutationTargetType == type && mutation.MutationType != MutationType.None && !string.IsNullOrEmpty(mutation.MutationTargetOriginalId) && !string.IsNullOrEmpty(mutation.FieldName) && mutation.FieldValue != null)
                {
                    var originalId = target.StringID.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (originalId == null || originalId != mutation.MutationTargetOriginalId) continue;
                    var traverse = Traverse.Create(target);
                    if (traverse.Field(mutation.FieldName).FieldExists())
                    {
                        object newValue = mutation.FieldValue(this, traverse.Field(mutation.FieldName).GetValue(), hero);
                        var fieldType = traverse.Field(mutation.FieldName).GetValueType();
                        switch (mutation.MutationType)
                        {
                            case MutationType.Replace:
                                if(newValue.GetType() == fieldType) 
                                {
                                    traverse.Field(mutation.FieldName).SetValue(newValue);
                                }
                                break;
                            case MutationType.Multiply:
                                
                                if (fieldType == typeof(float))
                                {
                                    var value = traverse.Field(mutation.FieldName).GetValue<float>();
                                    traverse.Field(mutation.FieldName).SetValue(value * (1 + Convert.ToSingle(newValue)));
                                }
                                else if (fieldType == typeof(int))
                                {
                                    var value = traverse.Field(mutation.FieldName).GetValue<int>();
                                    traverse.Field(mutation.FieldName).SetValue(value * (1 + Convert.ToInt32(newValue)));
                                }
                                break;
                            case MutationType.Add:
                                if (fieldType == typeof(float))
                                {
                                    var value = traverse.Field(mutation.FieldName).GetValue<float>();
                                    traverse.Field(mutation.FieldName).SetValue(value + Convert.ToSingle(newValue));
                                }
                                else if (fieldType == typeof(int))
                                {
                                    var value = traverse.Field(mutation.FieldName).GetValue<int>();
                                    traverse.Field(mutation.FieldName).SetValue(value + Convert.ToInt32(newValue));
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
            public string FieldName { get; set; } = string.Empty;
            public Func<CareerChoiceObject, object, Hero, object> FieldValue { get; set; } = null;
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
