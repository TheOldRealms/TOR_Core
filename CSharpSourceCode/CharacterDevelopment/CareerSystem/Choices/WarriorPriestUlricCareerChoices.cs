using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class WarriorPriestUlricCareerChoices : TORCareerChoicesBase
    {
        private CareerChoiceObject _warriorPriestUlricRoot;
        private CareerChoiceObject _crusherOfTheWeakKeystone;
        private CareerChoiceObject _crusherOfTheWeakPassive1;
        private CareerChoiceObject _crusherOfTheWeakPassive2;
        private CareerChoiceObject _crusherOfTheWeakPassive3;
        private CareerChoiceObject _crusherOfTheWeakPassive4;
        
        private CareerChoiceObject _wildPackKeystone;
        private CareerChoiceObject _wildPackPassive1;
        private CareerChoiceObject _wildPackPassive2;
        private CareerChoiceObject _wildPackPassive3;
        private CareerChoiceObject _wildPackPassive4;
        
        private CareerChoiceObject _teachingsOfTheWinterFatherKeystone;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive1;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive2;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive3;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive4;
        
        private CareerChoiceObject _frostsBiteKeystone;
        private CareerChoiceObject _frostsBitePassive1;
        private CareerChoiceObject _frostsBitePassive2;
        private CareerChoiceObject _frostsBitePassive3;
        private CareerChoiceObject _frostsBitePassive4;
        
        private CareerChoiceObject _runesOfTheWhiteWolfKeystone;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive1;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive2;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive3;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive4;
        
        private CareerChoiceObject _furyOfWarKeystone;
        private CareerChoiceObject _furyOfWarPassive1;
        private CareerChoiceObject _furyOfWarPassive2;
        private CareerChoiceObject _furyOfWarPassive3;
        private CareerChoiceObject _furyOfWarPassive4;
        
        private CareerChoiceObject _flameOfUlricKeystone;
        private CareerChoiceObject _flameOfUlricPassive1;
        private CareerChoiceObject _flameOfUlricPassive2;
        private CareerChoiceObject _flameOfUlricPassive3;
        private CareerChoiceObject _flameOfUlricPassive4;


        protected override void RegisterAll()
        {
            _warriorPriestUlricRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_warriorPriestUlricRoot).UnderscoreFirstCharToUpper()));

            _crusherOfTheWeakKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakKeystone).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive1).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive2).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive3).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive4).UnderscoreFirstCharToUpper()));

            _wildPackKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackKeystone).UnderscoreFirstCharToUpper()));
            _wildPackPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive1).UnderscoreFirstCharToUpper()));
            _wildPackPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive2).UnderscoreFirstCharToUpper()));
            _wildPackPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive3).UnderscoreFirstCharToUpper()));
            _wildPackPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive4).UnderscoreFirstCharToUpper()));

            _teachingsOfTheWinterFatherKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherKeystone).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive1).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive2).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive3).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive4).UnderscoreFirstCharToUpper()));

            _frostsBiteKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBiteKeystone).UnderscoreFirstCharToUpper()));
            _frostsBitePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive1).UnderscoreFirstCharToUpper()));
            _frostsBitePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive2).UnderscoreFirstCharToUpper()));
            _frostsBitePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive3).UnderscoreFirstCharToUpper()));
            _frostsBitePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive4).UnderscoreFirstCharToUpper()));

            _runesOfTheWhiteWolfKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfKeystone).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive1).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive2).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive3).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive4).UnderscoreFirstCharToUpper()));

            _furyOfWarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarKeystone).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive1).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive2).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive3).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive4).UnderscoreFirstCharToUpper()));
            
            _flameOfUlricKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricKeystone).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive1= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive1).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive2= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive2).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive3= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive3).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive4= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _warriorPriestUlricRoot.Initialize(CareerID, "root", null, true,
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
            _crusherOfTheWeakKeystone.Initialize(CareerID, "Ability can also be charged by applying damage.", "BookOfSigmar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                },new CareerChoiceObject.PassiveEffect());
            
            _wildPackKeystone.Initialize(CareerID, "Doubles the aura size of Righteous Fury.", "SigmarsProclaimer", false,
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
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Replace
                    },
                });
            _teachingsOfTheWinterFatherKeystone.Initialize(CareerID, "Righteous Fury scales with Leadership. Troops affected get unbreakable for the duration.", "RelentlessFanatic", false,
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
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.05f),
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
            _frostsBiteKeystone.Initialize(CareerID, "Righteous Fury scales with the highest melee weapon skill. Adds 20% physical resistance.", "ProtectorOfTheWeak", false,
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
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.05f, true),
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
            _runesOfTheWhiteWolfKeystone.Initialize(CareerID, "All units affected by Righteous Fury receive a burning weapon buff.", "HolyPurge", false,
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
            
            _furyOfWarKeystone.Initialize(CareerID, "Adds a healing buff to Righteous Fury that restores 3 Hitpoints per second.", "ArchLector", false,
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
            
            _flameOfUlricKeystone.Initialize(CareerID, "Righteous Fury adds a damaging aura. Its radius increases slightly when raising relevant skills.", "TwinTailedComet", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"apply_righteous_fury_pulse"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.01f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = OperationType.Add
                    },
                });
        }

        protected override void InitializePassives()
        {
            throw new System.NotImplementedException();
        }
    }
}