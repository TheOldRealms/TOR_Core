using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CampaignMechanics.Choices;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class WitchHunterCareerChoices : TORCareerChoicesBase
    {
        private CareerChoiceObject _witchHunterRoot;

        private CareerChoiceObject _toolsOfJudgementKeystone;
        
        private CareerChoiceObject _huntTheWickedKeystone;
        
        private CareerChoiceObject _silverHammerKeystone;
        
        private CareerChoiceObject _endsJustifiesMeansKeystone;
        
        private CareerChoiceObject _swiftProcedureKeystone;
        
        private CareerChoiceObject _guiltyByAssociationKeystone;
        
        private CareerChoiceObject _noRestAgainstEvilKeystone;
        
        public WitchHunterCareerChoices(CareerObject id) : base(id) {}


        protected override void RegisterAll()
        {
            _witchHunterRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WitchHunterRoot"));

            _huntTheWickedKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedKeystone"));
            
            _toolsOfJudgementKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementKeystone"));
            
            _silverHammerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverhammerKeystone"));
            
            _endsJustifiesMeansKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansKeystone"));
            
            _swiftProcedureKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwitftProcedureKeystone"));
            
            _guiltyByAssociationKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationKeystone"));
            
            _noRestAgainstEvilKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilKeystone"));
        }

        protected override void InitializeKeyStones()
        {
            _witchHunterRoot.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _huntTheWickedKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _toolsOfJudgementKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _huntTheWickedKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _silverHammerKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _endsJustifiesMeansKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _swiftProcedureKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _guiltyByAssociationKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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
            
            _noRestAgainstEvilKeystone.Initialize(CareerID, "single enemy is marked for 5 seconds and the first hit inflicted against the target increases the physical damage by 20%. After the first hit there is a 10% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by X% the marker stays on the target.", null, true,
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

            
        }

        protected override void InitializePassives()
        {
            
        }
    }
}