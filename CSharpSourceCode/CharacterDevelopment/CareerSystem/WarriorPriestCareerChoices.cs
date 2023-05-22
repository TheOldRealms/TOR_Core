using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class WarriorPriestCareerChoices
    {
        private CareerChoiceObject _warriorPriestRoot;

        private CareerChoiceObject _bookOfSigmarKeystone;
        private CareerChoiceObject _bookOfSigmarPassive1;
        private CareerChoiceObject _bookOfSigmarPassive2;
        private CareerChoiceObject _bookOfSigmarPassive3;
        private CareerChoiceObject _bookOfSigmarPassive4;

        private CareerChoiceObject _sigmarProclaimerKeystone;
        private CareerChoiceObject _sigmarProclaimerPassive1;
        private CareerChoiceObject _sigmarProclaimerPassive2;
        private CareerChoiceObject _sigmarProclaimerPassive3;
        private CareerChoiceObject _sigmarProclaimerPassive4;

        private CareerChoiceObject _relentlessFanaticKeystone;
        private CareerChoiceObject _relentlessFanaticPassive1;
        private CareerChoiceObject _relentlessFanaticPassive2;
        private CareerChoiceObject _relentlessFanaticPassive3;
        private CareerChoiceObject _relentlessFanaticPassive4;

        private CareerChoiceObject _protectorOfTheWeakKeystone;
        private CareerChoiceObject _protectorOfTheWeakPassive1;
        private CareerChoiceObject _protectorOfTheWeakPassive2;
        private CareerChoiceObject _protectorOfTheWeakPassive3;
        private CareerChoiceObject _protectorOfTheWeakPassive4;

        private CareerChoiceObject _holyPurgeKeystone;
        private CareerChoiceObject _holyPurgePassive1;
        private CareerChoiceObject _holyPurgePassive2;
        private CareerChoiceObject _holyPurgePassive3;
        private CareerChoiceObject _holyPurgePassive4;

        private CareerChoiceObject _archLectorKeystone;
        private CareerChoiceObject _archLectorPassive1;
        private CareerChoiceObject _archLectorPassive2;
        private CareerChoiceObject _archLectorPassive3;
        private CareerChoiceObject _archLectorPassive4;

        public WarriorPriestCareerChoices()
        {
            RegisterAll();
            InitializePassives();
            InitializeKeyStones();
        }

        private void RegisterAll()
        {
            _warriorPriestRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarriorPriestRoot"));

            _bookOfSigmarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarKeyStone"));
            _bookOfSigmarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive1"));
            _bookOfSigmarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive2"));
            _bookOfSigmarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive3"));
            _bookOfSigmarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive4"));

            _sigmarProclaimerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SigmarsProclaimerKeyStone"));
            _sigmarProclaimerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SigmarsProclaimerPassive1"));
            _sigmarProclaimerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SigmarsProclaimerPassive2"));
            _sigmarProclaimerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SigmarsProclaimerPassive3"));
            _sigmarProclaimerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SigmarsProclaimerPassive4"));

            _relentlessFanaticKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticKeyStone"));
            _relentlessFanaticPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive1"));
            _relentlessFanaticPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive2"));
            _relentlessFanaticPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive3"));
            _relentlessFanaticPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive4"));

            _protectorOfTheWeakKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakKeyStone"));
            _protectorOfTheWeakPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive1"));
            _protectorOfTheWeakPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive2"));
            _protectorOfTheWeakPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive3"));
            _protectorOfTheWeakPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive4"));

            _holyPurgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgeKeyStone"));
            _holyPurgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive1"));
            _holyPurgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive2"));
            _holyPurgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive3"));
            _holyPurgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive4"));

            _archLectorKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorKeyStone"));
            _archLectorPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive1"));
            _archLectorPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive2"));
            _archLectorPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive3"));
            _archLectorPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive4"));
        }

        private void InitializeKeyStones()
        {
            _warriorPriestRoot.Initialize(TORCareers.WarriorPriest, "root", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.0005f),
                        MutationType = OperationType.Add
                    }
                });
            _bookOfSigmarKeystone.Initialize(TORCareers.WarriorPriest, "Adds a healing buff to Righteous Fury that heals 5 hitpoints per second.", "BookOfSigmar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"righteous_fury_regeneration"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            _sigmarProclaimerKeystone.Initialize(TORCareers.WarriorPriest, "Doubles the aura size of Righteous Fury.", "SigmarsProclaimer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Replace
                    },
                });
            _relentlessFanaticKeystone.Initialize(TORCareers.WarriorPriest, "Leadership skill also counts towards the effectiveness of Righteous Fury. Troops affected are unbreakable for the duration.", "RelentlessFanatic", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.05f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.0005f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                });
            _protectorOfTheWeakKeystone.Initialize(TORCareers.WarriorPriest, "The highest of One Handed, Two Handed or Polearm skills also count towards the effectiveness of Righteous Fury. Adds 20% physical resistance.", "ProtectorOfTheWeak", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.05f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"physical_resistance_20"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            _holyPurgeKeystone.Initialize(TORCareers.WarriorPriest, "All units affected by Righteous Fury recieve a burning weapon effect.", "HolyPurge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"apply_fury_sword_trait"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            _archLectorKeystone.Initialize(TORCareers.WarriorPriest, "Righteous Fury adds a holy aura to the player that damages enemies periodically. Aura radius is increased by 0.01 meters for each skillpoint in the relevant skilltrees.", "ArchLector", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"apply_righteous_fury_pulse"}).ToList(),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.01f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.01f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.01f),
                        MutationType = OperationType.Add
                    },
                });
        }

        private void InitializePassives()
        {
            _bookOfSigmarPassive1.Initialize(TORCareers.WarriorPriest, "Increases hitpoints by 20.", "BookOfSigmar", false, ChoiceType.Passive, null);
            _bookOfSigmarPassive2.Initialize(TORCareers.WarriorPriest, "Grants higher starting morale to troops.", "BookOfSigmar", false, ChoiceType.Passive, null);
            _bookOfSigmarPassive3.Initialize(TORCareers.WarriorPriest, "After battle, all critically wounded companions are restored to minimum 20% of their health.", "BookOfSigmar", false, ChoiceType.Passive, null);
            _bookOfSigmarPassive4.Initialize(TORCareers.WarriorPriest, "Wounded troops in your party heal faster.", "BookOfSigmar", false, ChoiceType.Passive, null);

            _sigmarProclaimerPassive1.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "SigmarsProclaimer", false, ChoiceType.Passive, null);
            _sigmarProclaimerPassive2.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "SigmarsProclaimer", false, ChoiceType.Passive, null);
            _sigmarProclaimerPassive3.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "SigmarsProclaimer", false, ChoiceType.Passive, null);
            _sigmarProclaimerPassive4.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "SigmarsProclaimer", false, ChoiceType.Passive, null);

            _relentlessFanaticPassive1.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "RelentlessFanatic", false, ChoiceType.Passive, null);
            _relentlessFanaticPassive2.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "RelentlessFanatic", false, ChoiceType.Passive, null);
            _relentlessFanaticPassive3.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "RelentlessFanatic", false, ChoiceType.Passive, null);
            _relentlessFanaticPassive4.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "RelentlessFanatic", false, ChoiceType.Passive, null);

            _protectorOfTheWeakPassive1.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ProtectorOfTheWeak", false, ChoiceType.Passive, null);
            _protectorOfTheWeakPassive2.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ProtectorOfTheWeak", false, ChoiceType.Passive, null);
            _protectorOfTheWeakPassive3.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ProtectorOfTheWeak", false, ChoiceType.Passive, null);
            _protectorOfTheWeakPassive4.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ProtectorOfTheWeak", false, ChoiceType.Passive, null);

            _holyPurgePassive1.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "HolyPurge", false, ChoiceType.Passive, null);
            _holyPurgePassive2.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "HolyPurge", false, ChoiceType.Passive, null);
            _holyPurgePassive3.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "HolyPurge", false, ChoiceType.Passive, null);
            _holyPurgePassive4.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "HolyPurge", false, ChoiceType.Passive, null);

            _archLectorPassive1.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ArchLector", false, ChoiceType.Passive, null);
            _archLectorPassive2.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ArchLector", false, ChoiceType.Passive, null);
            _archLectorPassive3.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ArchLector", false, ChoiceType.Passive, null);
            _archLectorPassive4.Initialize(TORCareers.WarriorPriest, "Lorem Ipsum dolor...", "ArchLector", false, ChoiceType.Passive, null);
        }
    }
}
