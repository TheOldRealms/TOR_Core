using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
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
            _imperialMagisterRoot.Initialize(CareerID, "For 10 seconds, the wizard charges his own magical reserves by channeling Winds of Magic swirling in the air. While charging, the wizard is vulnerable to damage and moves at a greatly reduced pace. Every second, 3% of the total Winds of Magic reserve is regained. For every 50 points in Spellcraft, the duration is extended by 1 second, and every key stone picked grants an additional use of Arcane Conduit.", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => (float) originalValue *  CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.SpellCraft}, 0.0025f),
                        MutationType = OperationType.Add
                    }
                });
            
            _studyAndPractiseKeystone.Initialize(CareerID, "Ability scaling adds  0.25% physical resistance per point.", "StudyAndPractise", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "arcane_conduit_res_buff" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_res_buff",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.SpellCraft}, 0.0025f),
                        MutationType = OperationType.Add
                    },
                });
            
            _teclisTeachingsKeystone.Initialize(CareerID, "You charge 25% longer. Ability scales with Steward.", "TeclisTeachings", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => (float) originalValue * 0.25f ,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_res_buff",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Steward}, 0.0025f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Steward}, 0.0025f),
                        MutationType = OperationType.Add
                    }
                });
            
            _imperialEnchantmentKeystone.Initialize(CareerID, "You are less slowed down using Arcane Conduit. Scales with leadership", "ImperialEnchantment", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) =>
                        {
                            var list = originalValue as List<string>;
                            list.Add("arcane_conduit_slow_light");
                            return list.FindAll(x => x != "arcane_conduit_slow");;
                        }
                        ,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_res_buff",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership}, 0.0025f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership}, 0.0025f),
                        MutationType = OperationType.Add
                    }
                });
            _collegeOrdersKeystone.Initialize(CareerID, "Arcane Conduit also refreshes Winds of Companions.", "CollegeOrders", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "ArcaneConduit",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => 1,
                    MutationType = OperationType.Add
                },
            }, new CareerChoiceObject.PassiveEffect());
            
            _magicCombatTrainingKeystone.Initialize(CareerID, "Your spell damage is increased for 30% during Arcane Conduit.", "MagicCombatTraining", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "arcane_conduit_dmg_buff" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            
            
            _ancientScrollsKeystone.Initialize(CareerID, "Cooldown reduction by 50%. Additional Usage", "AncientScrolls", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 2,
                        MutationType = OperationType.Add
                    },
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "CoolDown",
                        PropertyValue = (choice, originalValue, agent) => -((int)originalValue * 0.5f) ,
                        MutationType = OperationType.Add
                    },
                });
            
            _arcaneKnowledgeKeystone.Initialize(CareerID, "Arcane Conduit resets cooldowns of spells. Halves duration and doubles gain. Additional usage", "ArcaneKnowledge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 2,
                        MutationType = OperationType.Add
                    },
                    new()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => -0.5f,
                        MutationType = OperationType.Multiply
                    },
                    new()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_winds_reg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue*2 ,
                        MutationType = OperationType.Replace
                    },
                });//special


        }

        protected override void InitializePassives()
        {
            _studyAndPractisePassive1.Initialize(CareerID, "Increases max Winds of Magic by 5.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _studyAndPractisePassive2.Initialize(CareerID,
                "10% Ward save if your armor weight does not exceed 11 weight.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All,
                    (attacker, victim, attackmask) => victim == Agent.Main && CareerChoicesHelper.ArmorWeightUndershootCheck(victim, 11) ));
            _studyAndPractisePassive3.Initialize(CareerID, "Increases magic spell damage by 10%.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
            _studyAndPractisePassive4.Initialize(CareerID, "{=vivid_visions_passive3_str}Increases Magic resistance against spells by 25%.", "StudyAndPractise", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,25),AttackTypeMask.Spell));
            
            _teclisTeachingsPassive1.Initialize(CareerID, "Increases fire spell damage by 10%.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Spell));
            _teclisTeachingsPassive2.Initialize(CareerID, "Increases electric spell damage by 10%.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Spell));
            _teclisTeachingsPassive3.Initialize(CareerID, "Increases max Winds of Magic by 5.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _teclisTeachingsPassive4.Initialize(CareerID, "Powerstones cost 35% less Prestige", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            
            _imperialEnchantmentPassive1.Initialize(CareerID, "Increases max Winds of Magic by 5.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _imperialEnchantmentPassive2.Initialize(CareerID, "Friendly fire damage is reduced by 30%", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.Special,true));
            _imperialEnchantmentPassive3.Initialize(CareerID, "Buffs and healing duration is increased by 50%.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.BuffDuration,true));
            _imperialEnchantmentPassive4.Initialize(CareerID, "Power stones reserve 25% less Winds.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
            
            _collegeOrdersPassive1.Initialize(CareerID, "Companion limit of party is increased by 5.", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _collegeOrdersPassive2.Initialize(CareerID, "Magister Companions have 25 more Winds of Magic.", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
            _collegeOrdersPassive3.Initialize(CareerID, "For every type of  College Wizard gain 10% more Prestige from combat", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _collegeOrdersPassive4.Initialize(CareerID, "Gain access to Power stones of other College Orders", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            
            _magicCombatTrainingPassive1.Initialize(CareerID, "Increases max Winds of Magic by 5.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _magicCombatTrainingPassive2.Initialize(CareerID, "{=tales_of_giles_passive4_str}25% spell cooldown reduction.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.WindsCooldownReduction, true)); 
            _magicCombatTrainingPassive3.Initialize(CareerID, "Increases Spell effectiveness by 15% if you wield an offhand staff.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SpellEffectiveness, true,
                (CharacterObject character) => HasMagicStaff()));
            _magicCombatTrainingPassive4.Initialize(CareerID, "Extra 20% Wardsave if your armor weight does not exceed 11 weight.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 20), AttackTypeMask.All,
                (attacker, victim, attackmask) => victim.IsHero && victim.IsMainAgent&&  CareerChoicesHelper.ArmorWeightUndershootCheck(victim, 11) ));
            
            _ancientScrollsPassive1.Initialize(CareerID, "Increase hex durations by 50%.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50f, PassiveEffectType.DebuffDuration,true));
            _ancientScrollsPassive2.Initialize(CareerID, "Increases Windsregeneration by 1.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.WindsRegeneration));
            _ancientScrollsPassive3.Initialize(CareerID, "Recover 80% used Prestige from used Powerstones", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _ancientScrollsPassive4.Initialize(CareerID, "For every varying imperial wizard, reduce the powerstone winds upkeep by 5%.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, true));

            
            _arcaneKnowledgePassive1.Initialize(CareerID, "Spelldamage of companions is increased by 20%", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _arcaneKnowledgePassive2.Initialize(CareerID, "Spell effect radius is increased by 20%.", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius,true));
            _arcaneKnowledgePassive3.Initialize(CareerID, "Increases Spell effectiveness by 20% if your armor weight undershoots 11 stones.", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.SpellEffectiveness, true,
                ( characterObject => characterObject.IsHero &&  characterObject.HeroObject == Hero.MainHero &&  CareerChoicesHelper.ArmorWeightUndershootCheck(Agent.Main,11) )));
            _arcaneKnowledgePassive4.Initialize(CareerID, "For every imperial Magister in your party, your maximum winds increases by 4", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.Special,true));
        }


        private bool HasMagicStaff()
        {
            var weapons = Hero.MainHero.CharacterObject.GetCharacterEquipment(EquipmentIndex.Weapon0,EquipmentIndex.Weapon3);

            if (weapons.Any(x => x.IsMagicalStaff()))
            {
                return true;
            }

            return false;
        }
        
    }
}