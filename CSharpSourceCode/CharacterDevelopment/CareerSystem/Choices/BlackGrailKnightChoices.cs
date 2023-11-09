using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class BlackGrailKnightChoices : GrailKnightCareerChoices
    {
        private CareerChoiceObject _blackGrailVowPassive1;
        private CareerChoiceObject _blackGrailVowPassive2;
        private CareerChoiceObject _blackGrailVowPassive3;
        private CareerChoiceObject _blackGrailVowPassive4;
        private CareerChoiceObject _blackgrailKnightRoot;

        public BlackGrailKnightChoices(CareerObject id) : base(id)
        {
            
        }

        protected override void RegisterAll()
        {
          base.RegisterAll();
            _blackgrailKnightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailKnightRoot"));
            _blackGrailVowPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlackGrailVowPassive1"));
            _blackGrailVowPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlackGrailVowPassive2"));
            _blackGrailVowPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlackGrailVowPassive3"));
            _blackGrailVowPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlackGrailVowPassive4"));
        }

        protected override void InitializeKeyStones()
        {
  
            _blackgrailKnightRoot.Initialize(CareerID, "The knight prepares a devastating charge, mounted or on foot, for the next 6 seconds. When mounted, the knight receives perk buffs as well as a 20% chance of his lance not bouncing off after a couched lance attack. The couched lance attack chance increases by 0.1% for every point in Riding. When on foot, the knight only receives perk buffs.", null,
                true, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
        }

        protected override void InitializePassives()
        {
           
            _blackGrailVowPassive1.Initialize(CareerID, "{=grail_vow_passive1_str}Increases Hitpoints by 40.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _blackGrailVowPassive2.Initialize(CareerID, "{=grail_vow_passive2_str}20% extra holy damage for Battle pilgrim troops.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _blackGrailVowPassive3.Initialize(CareerID, "{=grail_vow_passive3_str}20% extra melee holy damage.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy, 20), AttackTypeMask.Melee));
            _blackGrailVowPassive4.Initialize(CareerID, "{=grail_vow_passive4_str}Gain 15% Ward save.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All));

        }
    }
}