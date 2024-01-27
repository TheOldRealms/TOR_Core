using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class NecrarchCareerChoices : TORCareerChoicesBase
    {
        public NecrarchCareerChoices(CareerObject id) : base(id) {}
        
        private CareerChoiceObject _necrarchRoot;

        private CareerChoiceObject _discipleOfAccursedKeystone;
        private CareerChoiceObject _discipleOfAccursedPassive1;
        private CareerChoiceObject _discipleOfAccursedPassive2;
        private CareerChoiceObject _discipleOfAccursedPassive3;
        private CareerChoiceObject _discipleOfAccursedPassive4;

        private CareerChoiceObject _witchSightKeystone;
        private CareerChoiceObject _witchSightPassive1;
        private CareerChoiceObject _witchSightPassive2;
        private CareerChoiceObject _witchSightPassive3;
        private CareerChoiceObject _witchSightPassive4;

        private CareerChoiceObject _darkVisionKeystone;
        private CareerChoiceObject _darkVisionPassive1;
        private CareerChoiceObject _darkVisionPassive2;
        private CareerChoiceObject _darkVisionPassive3;
        private CareerChoiceObject _darkVisionPassive4;

        private CareerChoiceObject _unhallowedSoulKeystone;
        private CareerChoiceObject _unhallowedSoulPassive1;
        private CareerChoiceObject _unhallowedSoulPassive2;
        private CareerChoiceObject _unhallowedSoulPassive3;
        private CareerChoiceObject _unhallowedSoulPassive4;

        private CareerChoiceObject _hungerForKnowledgeKeystone;
        private CareerChoiceObject _hungerForKnowledgePassive1;
        private CareerChoiceObject _hungerForKnowledgePassive2;
        private CareerChoiceObject _hungerForKnowledgePassive3;
        private CareerChoiceObject _hungerForKnowledgePassive4;

        private CareerChoiceObject _wellspringOfDharKeystone;
        private CareerChoiceObject _wellspringOfDharPassive1;
        private CareerChoiceObject _wellspringOfDharPassive2;
        private CareerChoiceObject _wellspringOfDharPassive3;
        private CareerChoiceObject _wellspringOfDharPassive4;

        private CareerChoiceObject _everlingsSecretKeystone;
        private CareerChoiceObject _everlingsSecretPassive1;
        private CareerChoiceObject _everlingsSecretPassive2;
        private CareerChoiceObject _everlingsSecretPassive3;
        private CareerChoiceObject _everlingsSecretPassive4;
        
        
        protected override void RegisterAll()
        {
            _necrarchRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NecrarchRoot"));

            _discipleOfAccursedKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedKeystone).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive1).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive2).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive3).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive4).UnderscoreFirstCharToUpper()));

            _witchSightKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightKeystone).UnderscoreFirstCharToUpper()));
            _witchSightPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive1).UnderscoreFirstCharToUpper()));
            _witchSightPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive2).UnderscoreFirstCharToUpper()));
            _witchSightPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive3).UnderscoreFirstCharToUpper()));
            _witchSightPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive4).UnderscoreFirstCharToUpper()));
            
            _darkVisionKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionKeystone).UnderscoreFirstCharToUpper()));
            _darkVisionPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive1).UnderscoreFirstCharToUpper()));
            _darkVisionPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive2).UnderscoreFirstCharToUpper()));
            _darkVisionPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive3).UnderscoreFirstCharToUpper()));
            _darkVisionPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive4).UnderscoreFirstCharToUpper()));

            _unhallowedSoulKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulKeystone).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive1).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive2).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive3).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive4).UnderscoreFirstCharToUpper()));

            _hungerForKnowledgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgeKeystone).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive1).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive2).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive3).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive4).UnderscoreFirstCharToUpper()));

            _wellspringOfDharKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharKeystone).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive1).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive2).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive3).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive4).UnderscoreFirstCharToUpper()));
            
            _everlingsSecretKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretKeystone).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive1).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive2).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive3).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _necrarchRoot.Initialize(CareerID, "The Blood Knight is channeling focus and rage towards the enemies. Damage increased by 45% and physical resistance by 10% for the next 6 seconds. Both bonuses increase with the skill of the equipped weapon by 0.05% per point. Requires 5 kills to recharge, +1 kill per a final perk picked up to max 10.", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, true),
                        MutationType = OperationType.Add
                    }
                });
        }

        protected override void InitializePassives()
        {
            
        }
    }
}