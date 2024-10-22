using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class WitchHunterCareerChoices : TORCareerChoicesBase
    {
        private CareerChoiceObject _witchHunterRoot;

        private CareerChoiceObject _toolsOfJudgementKeystone;
        private CareerChoiceObject _toolsOfJudgementPassive1;
        private CareerChoiceObject _toolsOfJudgementPassive2;
        private CareerChoiceObject _toolsOfJudgementPassive3;
        private CareerChoiceObject _toolsOfJudgementPassive4;
        
        private CareerChoiceObject _huntTheWickedKeystone;
        private CareerChoiceObject _huntTheWickedPassive1;
        private CareerChoiceObject _huntTheWickedPassive2;
        private CareerChoiceObject _huntTheWickedPassive3;
        private CareerChoiceObject _huntTheWickedPassive4;
        
        private CareerChoiceObject _silverHammerKeystone;
        private CareerChoiceObject _silverHammerPassive1;
        private CareerChoiceObject _silverHammerPassive2;
        private CareerChoiceObject _silverHammerPassive3;
        private CareerChoiceObject _silverHammerPassive4;
        
        private CareerChoiceObject _noRestAgainstEvilKeystone;
        private CareerChoiceObject _noRestAgainstEvilPassive1;
        private CareerChoiceObject _noRestAgainstEvilPassive2;
        private CareerChoiceObject _noRestAgainstEvilPassive3;
        private CareerChoiceObject _noRestAgainstEvilPassive4;
        
        private CareerChoiceObject _swiftProcedureKeystone;
        private CareerChoiceObject _swiftProcedurePassive1;
        private CareerChoiceObject _swiftProcedurePassive2;
        private CareerChoiceObject _swiftProcedurePassive3;
        private CareerChoiceObject _swiftProcedurePassive4;
        
        private CareerChoiceObject _guiltyByAssociationKeystone;
        private CareerChoiceObject _guiltyByAssociationPassive1;
        private CareerChoiceObject _guiltyByAssociationPassive2;
        private CareerChoiceObject _guiltyByAssociationPassive3;
        private CareerChoiceObject _guiltyByAssociationPassive4;
        
        private CareerChoiceObject _endsJustifiesMeansKeystone;
        private CareerChoiceObject _endsJustifiesMeansPassive1;
        private CareerChoiceObject _endsJustifiesMeansPassive2;
        private CareerChoiceObject _endsJustifiesMeansPassive3;
        private CareerChoiceObject _endsJustifiesMeansPassive4;
        public WitchHunterCareerChoices(CareerObject id) : base(id) {}


        protected override void RegisterAll()
        {
            _witchHunterRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WitchHunterRoot"));
            _huntTheWickedKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedKeystone"));
            _huntTheWickedPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive1"));
            _huntTheWickedPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive2"));
            _huntTheWickedPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive3"));
            _huntTheWickedPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive4"));
            
            _toolsOfJudgementKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementKeystone"));
            _toolsOfJudgementPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive1"));
            _toolsOfJudgementPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive2"));
            _toolsOfJudgementPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive3"));
            _toolsOfJudgementPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive4"));
            
            _silverHammerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerKeystone"));
            _silverHammerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive1"));
            _silverHammerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive2"));
            _silverHammerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive3"));
            _silverHammerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive4"));
                
            _noRestAgainstEvilKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilKeystone"));
            _noRestAgainstEvilPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive1"));
            _noRestAgainstEvilPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive2"));
            _noRestAgainstEvilPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive3"));
            _noRestAgainstEvilPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive4"));
            
            _swiftProcedureKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedureKeystone"));
            _swiftProcedurePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedurePassive1"));
            _swiftProcedurePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwitftProcedurePassive2"));
            _swiftProcedurePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwitftProcedurePassive3"));
            _swiftProcedurePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwitftProcedurePassive4"));
            
            _guiltyByAssociationKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationKeystone"));
            _guiltyByAssociationPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive1"));
            _guiltyByAssociationPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive2"));
            _guiltyByAssociationPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive3"));
            _guiltyByAssociationPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive4"));
            
            _endsJustifiesMeansKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansKeystone"));
            _endsJustifiesMeansPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive1"));
            _endsJustifiesMeansPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive2"));
            _endsJustifiesMeansPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive3"));
            _endsJustifiesMeansPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _witchHunterRoot.Initialize(CareerID, "A single enemy is marked for 8 seconds and the first hit inflicted against the target increases the physical damage by 20%. After every hit there is a 50% chance the marker stays on the target. For every point in Gunpowder or crossbow, the chances increases by 0.1% additional targets are marked.", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder, DefaultSkills.Crossbow }, 0.001f,false,true),
                        MutationType = OperationType.Add
                    }
                });
            
            _toolsOfJudgementKeystone.Initialize(CareerID, "The Marker stays 6 seconds longer on the target. Ability scales with the higehest One- or Two-Handed-skill.", "ToolsOfJudgement", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => 6 ,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded,DefaultSkills.TwoHanded}, 0.001f,true),
                        MutationType = OperationType.Add
                    }
                });
            
            _huntTheWickedKeystone.Initialize(CareerID, "20% additional physical damage for marked enemies. Melee damage can charge ability", "HuntTheWicked", false,
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
            
            _silverHammerKeystone.Initialize(CareerID, "Increases holy damage by 20% to the marked target. Ability scales with Faith.", "SilverHammer", false,
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
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
            
            _noRestAgainstEvilKeystone.Initialize(CareerID, "Shield penetration for the duration of the ability. Enemy stays marked", "NoRestAgainstEvil", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "apply_accusation_selfbuff" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            
            _swiftProcedureKeystone.Initialize(CareerID, "Marked enemies movement & swing speed is decreased. Ability scales with athletics", "SwiftProcedure", false,
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
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Athletics}, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
            
            _guiltyByAssociationKeystone.Initialize(CareerID, "Companions and Retinues can trigger mark effects. Companions can charge ability", "GuiltyByAssociation", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    
                }); 
            
            _endsJustifiesMeansKeystone.Initialize(CareerID, "Marked enemies suffering from overkill damage propagate mark. Scales with Rougery", "EndsJustifiesMeans", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery}, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
        }

        protected override void InitializePassives()
        {
            _toolsOfJudgementPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _toolsOfJudgementPassive2.Initialize(CareerID, "5 extra ammo", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _toolsOfJudgementPassive3.Initialize(CareerID, "Extra melee damage (10%).", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _toolsOfJudgementPassive4.Initialize(CareerID, "Every headshot kill gains you roguery, count twice for marked targets.", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10,PassiveEffectType.Special,true));
           
            _huntTheWickedPassive1.Initialize(CareerID, "Increases health regeneration after battles by 3.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.HealthRegeneration));
            _huntTheWickedPassive2.Initialize(CareerID, "Ranged Infantry wages are 15% reduced.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.TroopWages,true, 
                characterObject => !characterObject.IsHero && !characterObject.IsMounted && characterObject.IsRanged));
            _huntTheWickedPassive3.Initialize(CareerID, "Every  equipped ranged weapon increases melee damage by 10%.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10,PassiveEffectType.Special,true));
            _huntTheWickedPassive4.Initialize(CareerID, "Increases the damage of all ranged troops by 10%.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee,
                (attacker, victim, mask) => !attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero)&& mask == AttackTypeMask.Ranged));

            _silverHammerPassive1.Initialize(CareerID, "Exterminated Undead and Ruinous powers increase faith per fallen Unit.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10,PassiveEffectType.Special));
            _silverHammerPassive2.Initialize(CareerID, "All units deal more damage against undead and chaos.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 15), AttackTypeMask.All,
                (attacker, victim, mask) => victim.Character.Race != 0));
            _silverHammerPassive3.Initialize(CareerID, "Increases Hitpoints by 25.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _silverHammerPassive4.Initialize(CareerID, "Troops can be upgraded to Witch Hunter Retinues.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));          //TORAgentStatCalculateModel 458

            _noRestAgainstEvilPassive1.Initialize(CareerID, "Extra holy melee damage (20%).", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy, 20), AttackTypeMask.Melee));
            _noRestAgainstEvilPassive2.Initialize(CareerID, "All regular troops have 20 higher ranged and melee skill", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));          //TORAgentStatCalculateModel 458
            _noRestAgainstEvilPassive3.Initialize(CareerID, "Increases accuracy by 15%.", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.AccuracyPenalty, true));  
            _noRestAgainstEvilPassive4.Initialize(CareerID, "Increases Companion Limit by 5.", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            
            _swiftProcedurePassive1.Initialize(CareerID, "10 extra ammo", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
            _swiftProcedurePassive2.Initialize(CareerID, "Reduce range Accuracy movement penalty by 15%.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.RangedMovementPenalty, true));  
            _swiftProcedurePassive3.Initialize(CareerID, "Party movement speed is increased by 1.5.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.5f, PassiveEffectType.PartyMovementSpeed,false));  
            _swiftProcedurePassive4.Initialize(CareerID, "Weapon swing speed increased by 15%.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed,true)); 
            
            _endsJustifiesMeansPassive1.Initialize(CareerID, "Extra ranged damage (10%).", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
            _endsJustifiesMeansPassive2.Initialize(CareerID, "Increases range resistance by 15%.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 25), AttackTypeMask.Ranged));
            _endsJustifiesMeansPassive3.Initialize(CareerID, "Extra 25% armor penetration of melee attacks.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _endsJustifiesMeansPassive4.Initialize(CareerID, "Ranged shots can penetrate multiple targets.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //TORAgentApplyDamage 29

            _guiltyByAssociationPassive1.Initialize(CareerID, "Increases troop regeneration by 2.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration)); //TORAgentApplyDamage 29
            _guiltyByAssociationPassive2.Initialize(CareerID, "Every ranged troop deals 15% extra holy damage.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 10), AttackTypeMask.Ranged, 
                (attacker, victim, mask) => attacker.BelongsToMainParty()&& attacker.IsMainAgent && mask == AttackTypeMask.Ranged)); 
            
            _guiltyByAssociationPassive3.Initialize(CareerID, "Companions have 50 additional health points.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special));
            _guiltyByAssociationPassive4.Initialize(CareerID, "Killing blows in the head increase temporary reload & swing speed", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special));
        }
    }
}