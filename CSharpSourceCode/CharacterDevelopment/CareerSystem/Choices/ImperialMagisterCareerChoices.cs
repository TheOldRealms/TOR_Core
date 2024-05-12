using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class ImperialMagisterCareerChoices : TORCareerChoicesBase
    {
        public ImperialMagisterCareerChoices(CareerObject id) : base(id) {}
        
        private CareerChoiceObject _imperialMagisterRoot;
        
        private CareerChoiceObject _studyAndPractiseKeystone;
        private CareerChoiceObject _studyAndPractisePassive1;
        private CareerChoiceObject _studyAndPractisePassive2;
        private CareerChoiceObject _studyAndPractisePassive3;
        private CareerChoiceObject _studyAndPractisePassive4;
        
        private CareerChoiceObject _teclisTeachingsKeystone;    
        private CareerChoiceObject _teclisTeachingsPassive1;
        private CareerChoiceObject _teclisTeachingsPassive2;
        private CareerChoiceObject _teclisTeachingsPassive3;
        private CareerChoiceObject _teclisTeachingsPassive4;
        
        private CareerChoiceObject _imperialEnchantmentKeystone;
        private CareerChoiceObject _imperialEnchantmentPassive1;
        private CareerChoiceObject _imperialEnchantmentPassive2;
        private CareerChoiceObject _imperialEnchantmentPassive3;
        private CareerChoiceObject _imperialEnchantmentPassive4;
        
        private CareerChoiceObject _collegeOrdersKeystone;
        private CareerChoiceObject _collegeOrdersPassive1;
        private CareerChoiceObject _collegeOrdersPassive2;
        private CareerChoiceObject _collegeOrdersPassive3;
        private CareerChoiceObject _collegeOrdersPassive4;
        
        private CareerChoiceObject _magicCombatTrainingKeystone;
        private CareerChoiceObject _magicCombatTrainingPassive1;
        private CareerChoiceObject _magicCombatTrainingPassive2;
        private CareerChoiceObject _magicCombatTrainingPassive3;
        private CareerChoiceObject _magicCombatTrainingPassive4;
        
        private CareerChoiceObject _ancientScrollsKeystone;
        private CareerChoiceObject _ancientScrollsPassive1;
        private CareerChoiceObject _ancientScrollsPassive2;
        private CareerChoiceObject _ancientScrollsPassive3;
        private CareerChoiceObject _ancientScrollsPassive4;
        
        private CareerChoiceObject _arcaneKnowledgeKeystone;
        private CareerChoiceObject _arcaneKnowledgePassive1;
        private CareerChoiceObject _arcaneKnowledgePassive2;
        private CareerChoiceObject _arcaneKnowledgePassive3;
        private CareerChoiceObject _arcaneKnowledgePassive4;
        
        protected override void RegisterAll()
        {
            
            _imperialMagisterRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ImperialMagisterRoot"));
            
            _studyAndPractiseKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractiseKeystone).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive1).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive2).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive3).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive4).UnderscoreFirstCharToUpper()));
            
            _teclisTeachingsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsKeystone).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive1).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive2).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive3).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive4).UnderscoreFirstCharToUpper()));
            
            _imperialEnchantmentKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentKeystone).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive1).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive2).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive3).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive4).UnderscoreFirstCharToUpper()));
            
            _collegeOrdersKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersKeystone).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive1).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive2).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive3).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive4).UnderscoreFirstCharToUpper()));
            
            _magicCombatTrainingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingKeystone).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive1).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive2).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive3).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive4).UnderscoreFirstCharToUpper()));
            
            _ancientScrollsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsKeystone).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive1).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive2).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive3).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive4).UnderscoreFirstCharToUpper()));
            
            _arcaneKnowledgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgeKeystone).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive1).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive2).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive3).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _imperialMagisterRoot.Initialize(CareerID, "Summon a champion that the necromancer take control of. The Champion loses every 2 seconds 5 health points. For every 3 points in spell casting skill the champion gains 1 health point. Charging: applying spell- damage or healing. Alternatively, Let undead units inflict damage.", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "GreaterHarbinger",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.SpellCraft}, 0.33f),
                        MutationType = OperationType.Add
                    }
                });

        }

        protected override void InitializePassives()
        {
            _studyAndPractisePassive1.Initialize(CareerID, "Increases max Winds of Magic by 10.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
        }
    }
}