using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoices
    {
        public static TORCareerChoices Instance { get; private set; }

        private CareerChoiceObject _warriorPriestRoot;
        private CareerChoiceObject _bookOfSigmarKeystone;
        private CareerChoiceObject _sigmarProclaimerKeystone;
        private CareerChoiceObject _relentlessFanaticKeystone;
        private CareerChoiceObject _protectorOfTheWeakKeystone;
        private CareerChoiceObject _holyPurgeKeystone;
        private CareerChoiceObject _archLectorKeystone;

        public TORCareerChoices()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }

        public static CareerChoiceObject GetChoice(string id) => MBObjectManager.Instance.GetObject<CareerChoiceObject>(x => x.StringId == id);

        private void RegisterAll()
        {
            _warriorPriestRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarriorPriestRoot"));
            _bookOfSigmarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarKeyStone"));
            _sigmarProclaimerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SigmarsProclaimerKeyStone"));
            _relentlessFanaticKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticKeyStone"));
            _protectorOfTheWeakKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakKeyStone"));
            _holyPurgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgeKeyStone"));
            _archLectorKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorKeyStone"));
        }

        private void InitializeAll()
        {
            _warriorPriestRoot.Initialize(TORCareers.WarriorPriest, null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.0005f),
                        MutationType = MutationType.Add
                    }
                });
            _bookOfSigmarKeystone.Initialize(TORCareers.WarriorPriest, "BookOfSigmar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"righteousfury_regeneration"}).ToList(),
                        MutationType = MutationType.Replace
                    },
                });
            _sigmarProclaimerKeystone.Initialize(TORCareers.WarriorPriest, "SigmarsProclaimer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = MutationType.Replace
                    },
                });
            _relentlessFanaticKeystone.Initialize(TORCareers.WarriorPriest, "RelentlessFanatic", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.05f),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.0005f),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = MutationType.Replace
                    },
                });
            _protectorOfTheWeakKeystone.Initialize(TORCareers.WarriorPriest, "ProtectorOfTheWeak", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.05f, true),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, true),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"physical_resistance_20"}).ToList(),
                        MutationType = MutationType.Replace
                    },
                });
            _holyPurgeKeystone.Initialize(TORCareers.WarriorPriest, "HolyPurge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"apply_fury_sword_trait"}).ToList(),
                        MutationType = MutationType.Replace
                    },
                });
            _archLectorKeystone.Initialize(TORCareers.WarriorPriest, "ArchLector", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.01f, true),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.01f),
                        MutationType = MutationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.01f),
                        MutationType = MutationType.Add
                    },
                });
        }
    }
}
