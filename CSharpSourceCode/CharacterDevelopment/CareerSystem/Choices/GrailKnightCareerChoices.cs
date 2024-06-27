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
    public class GrailKnightCareerChoices : TORCareerChoicesBase
    {
        public GrailKnightCareerChoices(CareerObject id) : base(id) {}

        private CareerChoiceObject _grailKnightRoot;

        private CareerChoiceObject _errantryWarKeystone;
        private CareerChoiceObject _errantryWarPassive1;
        private CareerChoiceObject _errantryWarPassive2;
        private CareerChoiceObject _errantryWarPassive3;
        private CareerChoiceObject _errantryWarPassive4;

        private CareerChoiceObject _enhancedHorseCombatKeystone;
        private CareerChoiceObject _enhancedHorseCombatPassive1;
        private CareerChoiceObject _enhancedHorseCombatPassive2;
        private CareerChoiceObject _enhancedHorseCombatPassive3;
        private CareerChoiceObject _enhancedHorseCombatPassive4;

        private CareerChoiceObject _questingVowKeyStone;
        private CareerChoiceObject _questingVowPassive1;
        private CareerChoiceObject _questingVowPassive2;
        private CareerChoiceObject _questingVowPassive3;
        private CareerChoiceObject _questingVowPassive4;

        private CareerChoiceObject _monsterSlayerKeystone;
        private CareerChoiceObject _monsterSlayerPassive1;
        private CareerChoiceObject _monsterSlayerPassive2;
        private CareerChoiceObject _monsterSlayerPassive3;
        private CareerChoiceObject _monsterSlayerPassive4;

        private CareerChoiceObject _masterHorsemanKeystone;
        private CareerChoiceObject _masterHorsemanPassive1;
        private CareerChoiceObject _masterHorsemanPassive2;
        private CareerChoiceObject _masterHorsemanPassive3;
        private CareerChoiceObject _masterHorsemanPassive4;

        private CareerChoiceObject _grailVowKeystone;
        private CareerChoiceObject _grailVowPassive1;
        private CareerChoiceObject _grailVowPassive2;
        private CareerChoiceObject _grailVowPassive3;
        private CareerChoiceObject _grailVowPassive4;

        private CareerChoiceObject _holyCrusaderKeystone;
        private CareerChoiceObject _holyCrusaderPassive1;
        private CareerChoiceObject _holyCrusaderPassive2;
        private CareerChoiceObject _holyCrusaderPassive3;
        private CareerChoiceObject _holyCrusaderPassive4;

        protected override void RegisterAll()
        {
            _grailKnightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailKnightRoot"));

            _errantryWarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarKeystone"));
            _errantryWarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive1"));
            _errantryWarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive2"));
            _errantryWarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive3"));
            _errantryWarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive4"));

            _enhancedHorseCombatKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatKeystone"));
            _enhancedHorseCombatPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive1"));
            _enhancedHorseCombatPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive2"));
            _enhancedHorseCombatPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive3"));
            _enhancedHorseCombatPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive4"));

            _questingVowKeyStone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowKeyStone"));
            _questingVowPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive1"));
            _questingVowPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive2"));
            _questingVowPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive3"));
            _questingVowPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive4"));

            _monsterSlayerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerKeystone"));
            _monsterSlayerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive1"));
            _monsterSlayerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive2"));
            _monsterSlayerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive3"));
            _monsterSlayerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive4"));

            _masterHorsemanKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanKeystone"));
            _masterHorsemanPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive1"));
            _masterHorsemanPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive2"));
            _masterHorsemanPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive3"));
            _masterHorsemanPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive4"));

            _grailVowKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowKeystone"));
            _grailVowPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive1"));
            _grailVowPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive2"));
            _grailVowPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive3"));
            _grailVowPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive4"));
            
            _holyCrusaderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyCrusaderKeystone"));
            _holyCrusaderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyCrusaderPassive1"));
            _holyCrusaderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyCrusaderPassive2"));
            _holyCrusaderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyCrusaderPassive3"));
            _holyCrusaderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyCrusaderPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _grailKnightRoot.Initialize(CareerID, "The knight prepares a devastating charge, mounted or on foot, for the next 6 seconds, increasing the speed by 20%. When mounted, the knight receives perk buffs as well as a 20% chance of his lance not bouncing off after a couched lance attack. The couched lance attack chance increases by 0.1% for every point in Riding. When on foot, the knight only receives perk buffs.", null,
                true, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_speed" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });

            _errantryWarKeystone.Initialize(CareerID, "10% extra physical damage during Knightly Charge. The ability scales with the One Handed skill.", "ErrantryWar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_phys_dmg" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
            _enhancedHorseCombatKeystone.Initialize(CareerID, "Ability scales now  with the Polearm skill and starts recharged.", "EnhancedHorseCombat", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Polearm }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Polearm }, 0.001f),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special));

            _questingVowKeyStone.Initialize(CareerID, "Knightly Charge grants 20% physical resistance. Mount will not rear during the ability.", "QuestingVow", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_phys_res" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_horse_steady" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _monsterSlayerKeystone.Initialize(CareerID, "Ability duration scales with Polearm and Riding. The lance attack scales with Two Handed.", "MonsterSlayer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "KnightlyCharge",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Polearm, DefaultSkills.Riding }, 0.025f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.TwoHanded }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.TwoHanded }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
            _masterHorsemanKeystone.Initialize(CareerID, "Reduces cooldown of Knightly Charge by 30s. During the ability, all healing affects the horse.", "MasterHorseman", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "KnightlyCharge",
                        PropertyName = "CoolDown",
                        PropertyValue = (choice, originalValue, agent) => -30,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_link" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            _grailVowKeystone.Initialize(CareerID, "Ability scales with Faith. When active, get +20% holy damage and regen.", "GrailVow", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { TORSkills.Faith }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_healing" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "KnightlyCharge",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "apply_holy_grail_lance_trait" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { TORSkills.Faith }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
            _holyCrusaderKeystone.Initialize(CareerID, "Ability scales with Leadership and propagates from all Companions to units in a 5m radius ", "HolyCrusader", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Leadership }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Leadership }, 0.001f),
                        MutationType = OperationType.Add
                    },

                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "KnightlyCharge",
                        PropertyName = "AbilityTargetType",
                        PropertyValue = (choice, originalValue, agent) => AbilityTargetType.AlliesInAOE,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "TargetType",
                        PropertyValue = (choice, originalValue, agent) => TargetType.Friendly,
                        MutationType = OperationType.Replace
                    }
                });
        }

        protected override void InitializePassives()
        {
            _errantryWarPassive1.Initialize(CareerID, "10% extra melee damage.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee)); //
            _errantryWarPassive2.Initialize(CareerID, "Increases Hitpoints by 40.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health)); // 
            _errantryWarPassive3.Initialize(CareerID, "All Knight troops receive 20 bonus points in One and Two Handed skills.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special)); //
            _errantryWarPassive4.Initialize(CareerID, "All melee troops in the party gain 25 exp per day.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //

            _enhancedHorseCombatPassive1.Initialize(CareerID, "50% additional Hitpoints for the player's mount.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true)); //
            _enhancedHorseCombatPassive2.Initialize(CareerID, "10% extra melee damage while on horseback.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, 
                (attacker, victim, mask) => attacker.IsMainAgent&& mask == AttackTypeMask.Melee && attacker.HasMount));
            
            _enhancedHorseCombatPassive3.Initialize(CareerID, "Upgrade costs are reduced by 25%.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true)); //
            _enhancedHorseCombatPassive4.Initialize(CareerID, "All Knight troops receive 30 bonus points in their Polearm skill.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special)); //

            _questingVowPassive1.Initialize(CareerID, "Increases Hitpoints by 40.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health)); //
            _questingVowPassive2.Initialize(CareerID, "Gain 15% physical resistance to melee and ranged attacks.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _questingVowPassive3.Initialize(CareerID, "All Knight troops gain 10% physical resistance.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All, 
                (attacker, victim, mask) => !victim.BelongsToMainParty()&&!victim.IsHero&&victim.Character.IsKnightUnit()));
            
            _questingVowPassive4.Initialize(CareerID, "For every Knight Companion you gain 3 Chivalry per Day.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Special));

            _monsterSlayerPassive1.Initialize(CareerID, "10% extra melee fire damage.", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Melee));
            _monsterSlayerPassive2.Initialize(CareerID, "20% extra armor penetration of melee attacks.", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _monsterSlayerPassive3.Initialize(CareerID, "40% chance to recruit an extra unit of the same type free of charge.", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special, true));
            _monsterSlayerPassive4.Initialize(CareerID, "Hits below 15 damage will not stagger the player.", "MonsterSlayer", false, ChoiceType.Passive, null); // Agent extension 83,

            _masterHorsemanPassive1.Initialize(CareerID, "Horse charge damage is increased by 50%.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.HorseChargeDamage, true));
            _masterHorsemanPassive2.Initialize(CareerID, "Party movement speed is increased by 2.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.PartyMovementSpeed));
            _masterHorsemanPassive3.Initialize(CareerID, "+4 health regeneration on the campaign map.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.HealthRegeneration, false));
            _masterHorsemanPassive4.Initialize(CareerID, "All Knight troops wages are reduced by 25%.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages, true, 
                characterObject => characterObject.IsKnightUnit()));

            _grailVowPassive1.Initialize(CareerID, "{=grail_vow_passive1_str}Increases Hitpoints by 40.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _grailVowPassive2.Initialize(CareerID, "{=grail_vow_passive2_str}20% extra holy damage for Battle pilgrim troops.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee, 
                (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.BelongsToMainParty() && attacker.Character.UnitBelongsToCult("cult_of_lady")));
            _grailVowPassive3.Initialize(CareerID, "{=grail_vow_passive3_str}20% extra melee holy damage.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy, 20), AttackTypeMask.Melee));
            _grailVowPassive4.Initialize(CareerID, "{=grail_vow_passive4_str}Gain 15% Ward save.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All));
            
            _holyCrusaderPassive1.Initialize(CareerID, "{=holy_crusader_passive1_str}Increases Hitpoints by 40.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _holyCrusaderPassive2.Initialize(CareerID, "{=holy_crusader_passive2_str}Companion Health increases by 15 for every 'Knight' Companion.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
            _holyCrusaderPassive3.Initialize(CareerID, "{=holy_crusader_passive3_str}Grail Knights can be upgraded to Companions.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _holyCrusaderPassive4.Initialize(CareerID, "{=holy_crusader_passive4_str}Companion limit of party is increased by 10.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.CompanionLimit));
        }
        
    }
}