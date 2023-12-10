using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
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
            
            _silverHammerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerKeystone"));
            
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
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder, DefaultSkills.Crossbow }, 0.0005f,false,true),
                        MutationType = OperationType.Add
                    }
                });
            
            _toolsOfJudgementKeystone.Initialize(CareerID, "The Marker stays 5 seconds longer on the target. One-handed  or two-handed skill counts towards careers ability.", "ToolsOfJudgement", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => 2 ,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded,DefaultSkills.TwoHanded}, 0.0005f,true),
                        MutationType = OperationType.Add
                    }
                });
            
            _huntTheWickedKeystone.Initialize(CareerID, "20% additional physical damage for marked enemies.", "HuntTheWicked", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "accusation_debuff_phy" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            
            _silverHammerKeystone.Initialize(CareerID, "Increases damage to the target by  holy damage 20%. Faith skill counts towards ability.", "SilverHammer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "accusation_debuff_holy" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.0005f),
                        MutationType = OperationType.Add
                    }
                });
            
            _endsJustifiesMeansKeystone.Initialize(CareerID, "If the enemy suffers from one hit more damage than its maximum life a marker is placed on a near-by enemy. Ability scales with rougery", "EndsJustifiesMeans", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery}, 0.0005f),
                        MutationType = OperationType.Add
                    }
                }); // passive
            
            _swiftProcedureKeystone.Initialize(CareerID, "Marked enemies movement & swing speed is decreased. Ability scales with atheletics", "SwiftProcedure", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "accusation_debuff_mov", "accusation_debuff_ats" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Athletics}, 0.0005f),
                        MutationType = OperationType.Add
                    }
                });
            
            _guiltyByAssociationKeystone.Initialize(CareerID, "After adding the marker, the mark may jump to 1-2 surrounding enemies.", "GuiltyByAssociation", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    
                }); //special
            
            _noRestAgainstEvilKeystone.Initialize(CareerID, "Executing marked enemies increases your reload speed and swing speed for 5 seconds.", "NoRestAgainstEvil", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }); // special

            
        }

        protected override void InitializePassives()
        {
            
        }
    }
}