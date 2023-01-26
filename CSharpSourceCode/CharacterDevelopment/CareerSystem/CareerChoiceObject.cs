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
                if (mutation != null && mutation.MutationTargetType == type && mutation.MutationType != MutationType.None && !string.IsNullOrEmpty(mutation.MutationTargetOriginalId) && !string.IsNullOrEmpty(mutation.PropertyName) && mutation.PropertyValue != null)
                {
                    var originalId = target.StringID.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (originalId == null || originalId != mutation.MutationTargetOriginalId) continue;
                    var traverse = Traverse.Create(target);
                    if (traverse.Property(mutation.PropertyName).PropertyExists())
                    {
                        object newValue = mutation.PropertyValue(this, traverse.Property(mutation.PropertyName).GetValue(), hero);
                        var propertyType = traverse.Property(mutation.PropertyName).GetValueType();
                        switch (mutation.MutationType)
                        {
                            case MutationType.Replace:
                                if(newValue.GetType() == propertyType) 
                                {
                                    traverse.Property(mutation.PropertyName).SetValue(newValue);
                                }
                                break;
                            case MutationType.Multiply:
                                
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
                            case MutationType.Add:
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
            public Func<CareerChoiceObject, object, Hero, object> PropertyValue { get; set; } = null;
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
