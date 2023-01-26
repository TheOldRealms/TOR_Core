using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoices
    {
        public static TORCareerChoices Instance { get; private set; }
        private List<CareerChoiceObject> _allCareerChoices = new List<CareerChoiceObject>();
        private MBReadOnlyList<CareerChoiceObject> _readonlyCareerChoices;
        public static MBReadOnlyList<CareerChoiceObject> All => Instance._readonlyCareerChoices;


        private CareerChoiceObject _warriorPriestRoot;
        private CareerChoiceObject _bookOfSigmar;
        public static CareerChoiceObject WarriorPriestRoot => Instance._warriorPriestRoot;
        public static CareerChoiceObject BookOfSigmar => Instance._bookOfSigmar;

        public TORCareerChoices()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            _readonlyCareerChoices = new MBReadOnlyList<CareerChoiceObject>(_allCareerChoices);
        }

        private void RegisterAll()
        {
            _warriorPriestRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarriorPriestRoot"));
            _bookOfSigmar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmar"));
            _allCareerChoices.Add(_warriorPriestRoot);
            _allCareerChoices.Add(_bookOfSigmar);
        }

        private void InitializeAll()
        {
            _warriorPriestRoot.Initialize("Warrior Priest Career Tree Root", 
                "The root of the career choices tree.", 
                TORCareers.WarriorPriest, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        FieldName = "ImbuedStatusEffectDuration",
                        FieldValue = (choice, originalValue, hero) => CareerHelper.AddSkillEffectToValue(choice, hero, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        FieldName = "BaseEffectValue",
                        FieldValue = (choice, originalValue, hero) => CareerHelper.AddSkillEffectToValue(choice, hero, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = MutationType.Add
                    }
                });
            _warriorPriestRoot.Initialize("Book of Sigmar",
                "Please provide description.",
                TORCareers.WarriorPriest, false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        FieldName = "ImbuedStatusEffects",
                        FieldValue = (choice, originalValue, hero) => StatusEffectManager.GetStatusEffectTemplatesWithIds(new List<string>(){ "righteous_fury_effect", "healing_regeneration"}),
                        MutationType = MutationType.Replace
                    },
                });
        }
    }
}
