using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
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
        public GrailKnightCareerChoices(CareerObject id) : base(id) { }

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
            _grailKnightRoot.Initialize(CareerID, "Honour is all, Chivalry is all! Slay your foes with a Knightly Charge! For the next 6s while mounted, gain +20% movement speed and +20% chance for a couched lance to not reset after a hit. For every level of Riding, gain +0.1% chance to not reset a couched lance during Knightly Charge. (60s cooldown)", null,
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

            _errantryWarKeystone.Initialize(CareerID, "Knightly Charge also scales with One-Handed skill, and gains +10% 'Physical' melee damage.", "ErrantryWar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "knightly_charge_phys_dmg", "knightly_charge_speed_bonus" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            _enhancedHorseCombatKeystone.Initialize(CareerID, "Knightly Charge also scales with Polearm skill, and can be used on battle start.", "EnhancedHorseCombat", false,
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

            _questingVowKeyStone.Initialize(CareerID, "Knightly Charge gains +20% personal 'Physical Resistance', mount doesn't rear during ability.", "QuestingVow", false,
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

            _monsterSlayerKeystone.Initialize(CareerID, "Knightly Charge duration scales with Polearm/Riding, its damage also scales with Two-Handed.", "MonsterSlayer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "KnightlyCharge",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.025f),
                        MutationType = OperationType.Add
                    },
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
                        MutationTargetOriginalId = "knightly_charge_phys_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Polearm }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
            _masterHorsemanKeystone.Initialize(CareerID, "Knightly Charge's cooldown is 30s shorter, and any healing recieved also affects your horse.", "MasterHorseman", false,
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
            _grailVowKeystone.Initialize(CareerID, "Knightly Charge also scales with Faith, gives regeneration, and gains +20% 'Holy' damage.", "GrailVow", false,
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
            _holyCrusaderKeystone.Initialize(CareerID, "Knightly Charge also scales with Leadership, and applies to Companions within a 5m radius.", "HolyCrusader", false,
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
            _errantryWarPassive1.Initialize(CareerID, "+5% personal 'Physical' melee damage.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee));
            _errantryWarPassive2.Initialize(CareerID, "+15 personal Hitpoints.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
            _errantryWarPassive3.Initialize(CareerID, "+20 One/Two-Handed skill for all 'Knight' troops.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string> { nameof(DefaultSkills.OneHanded), nameof(DefaultSkills.TwoHanded) },
                characterObject => characterObject.IsKnightUnit()));
            _errantryWarPassive4.Initialize(CareerID, "+12 daily experience for all 'Melee' troops.", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(12, PassiveEffectType.Special));

            _enhancedHorseCombatPassive1.Initialize(CareerID, "+50% Hitpoints for your mount.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true));
            _enhancedHorseCombatPassive2.Initialize(CareerID, "+5% personal 'Physical' melee damage while mounted.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && attacker.HasMount));

            _enhancedHorseCombatPassive3.Initialize(CareerID, "-12% 'Chivalry' upgrade costs for all troops.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-12, PassiveEffectType.CustomResourceUpgradeCostModifier, true));
            _enhancedHorseCombatPassive4.Initialize(CareerID, "+30 Polearm skill for all 'Knight' troops.", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, nameof(DefaultSkills.Polearm),
                (characterObject) => characterObject.IsKnightUnit()));

            _questingVowPassive1.Initialize(CareerID, "+35 personal Hitpoints.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(35, PassiveEffectType.Health));
            _questingVowPassive2.Initialize(CareerID, "+12% personal weapon swing speed.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(12f, PassiveEffectType.SwingSpeed, true));
            _questingVowPassive3.Initialize(CareerID, "+8% 'Physical Resistance' for all 'Knight' troops.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 8), AttackTypeMask.All,
                (attacker, victim, mask) => !victim.BelongsToMainParty() && !victim.IsHero && victim.Character.IsKnightUnit()));

            _questingVowPassive4.Initialize(CareerID, "+2 'Chivalry' daily per 'Knight' Companion.", "QuestingVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));

            _monsterSlayerPassive1.Initialize(CareerID, "+5% personal 'Fire' melee damage.", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 5), AttackTypeMask.Melee));
            _monsterSlayerPassive2.Initialize(CareerID, "+20% personal 'Armour Penetration' for melee attacks.", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _monsterSlayerPassive3.Initialize(CareerID, "+20% personal 'Physical' melee damage against mounted enemies and monsters.", "MonsterSlayer", false, ChoiceType.Passive, null,
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee,
                    (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && (victim.Character as CharacterObject).IsLargeTarget()));
            _monsterSlayerPassive4.Initialize(CareerID, "Hits below 15 damage no longer stagger you.", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));

            _masterHorsemanPassive1.Initialize(CareerID, "+40% charge damage bonus for your mount.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.HorseChargeDamage, true));
            _masterHorsemanPassive2.Initialize(CareerID, "+1 party move speed on campaign map.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
            _masterHorsemanPassive3.Initialize(CareerID, "+8% personal 'Physical Resistance'.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 8), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _masterHorsemanPassive4.Initialize(CareerID, "-25% gold wages for 'Knight' troops.", "MasterHorseman", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages, true,
                characterObject => characterObject.IsKnightUnit()));

            _grailVowPassive1.Initialize(CareerID, "-30% 'Chivalry' upgrade cost for 'Grail Knight' troops.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.CustomResourceUpgradeCostModifier, true,
                characterObject => characterObject.StringId.ToLower().Contains("grail")));
            _grailVowPassive2.Initialize(CareerID, "+15% 'Holy' melee damage for Battle Pilgrim troops.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee,
                (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.BelongsToMainParty() && attacker.Character.UnitBelongsToCult("cult_of_lady")));
            _grailVowPassive3.Initialize(CareerID, "+10% personal 'Holy' melee damage.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy, 10), AttackTypeMask.Melee));
            _grailVowPassive4.Initialize(CareerID, "+15% personal 'Ward Save'.", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All));

            _holyCrusaderPassive1.Initialize(CareerID, "+40 personal Hitpoints.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _holyCrusaderPassive2.Initialize(CareerID, "'Knight' Companions gain +5 Hitpoints per 'Knight' Companion.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special));
            _holyCrusaderPassive3.Initialize(CareerID, "'Grail Knight' troops can be upgraded to Companions.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _holyCrusaderPassive4.Initialize(CareerID, "+7 Companion limit.", "HolyCrusader", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(7, PassiveEffectType.CompanionLimit));
        }
    }
}