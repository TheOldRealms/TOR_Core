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

        public void Initialize(string name, string description, CareerObject ownerCareer, ChoiceType type, MutationObject mutation)
        {
            base.Initialize(new TextObject(name), new TextObject(description));
            OwnerCareer = ownerCareer;
            _mutation = mutation;
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
                            break;
                        case MutationType.Multiply:
                            break;
                        case MutationType.Add:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void MutateEffect(TriggeredEffectTemplate effect, Hero hero)
        {
            if (_mutation != null && _mutation.MutationTarget == typeof(TriggeredEffectTemplate) && _mutation.MutationType != MutationType.None && !string.IsNullOrEmpty(_mutation.FieldName) && _mutation.FieldValue != null)
            {
                var traverse = Traverse.Create(effect);
                if (traverse.Field(_mutation.FieldName).FieldExists())
                {
                    switch (_mutation.MutationType)
                    {
                        case MutationType.Replace:
                            break;
                        case MutationType.Multiply:
                            break;
                        case MutationType.Add:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public class MutationObject
        {
            public Type MutationTarget { get; set; } = typeof(TriggeredEffectTemplate);
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
