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
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class BlackGrailKnightCareerChoices : TORCareerChoicesBase
    {
        public BlackGrailKnightCareerChoices(CareerObject id) : base(id) {}

        private CareerChoiceObject _blackGrailKnightRoot;

        private CareerChoiceObject _curseOfMousillonKeystone;
        private CareerChoiceObject _curseOfMousillonPassive1;
        private CareerChoiceObject _curseOfMousillonPassive2;
        private CareerChoiceObject _curseOfMousillonPassive3;
        private CareerChoiceObject _curseOfMousillonPassive4;

        private CareerChoiceObject _swampRiderKeystone;
        private CareerChoiceObject _swampRiderPassive1;
        private CareerChoiceObject _swampRiderPassive2;
        private CareerChoiceObject _swampRiderPassive3;
        private CareerChoiceObject _swampRiderPassive4;

        private CareerChoiceObject _unbreakableArmyKeystone;
        private CareerChoiceObject _unbreakableArmyPassive1;
        private CareerChoiceObject _unbreakableArmyPassive2;
        private CareerChoiceObject _unbreakableArmyPassive3;
        private CareerChoiceObject _unbreakableArmyPassive4;

        private CareerChoiceObject _scourgeOfMousillonKeystone;
        private CareerChoiceObject _scourgeOfMousillonPassive1;
        private CareerChoiceObject _scourgeOfMousillonPassive2;
        private CareerChoiceObject _scourgeOfMousillonPassive3;
        private CareerChoiceObject _scourgeOfMousillonPassive4;

        private CareerChoiceObject _robberBaronKeystone;
        private CareerChoiceObject _robberBaronPassive1;
        private CareerChoiceObject _robberBaronPassive2;
        private CareerChoiceObject _robberBaronPassive3;
        private CareerChoiceObject _robberBaronPassive4;

        private CareerChoiceObject _lieOfLadyKeystone;
        private CareerChoiceObject _lieOfLadyPassive1;
        private CareerChoiceObject _lieOfLadyPassive2;
        private CareerChoiceObject _lieOfLadyPassive3;
        private CareerChoiceObject _lieOfLadyPassive4;

        private CareerChoiceObject _blackGrailVowKeystone;
        private CareerChoiceObject _blackGrailVowPassive1;
        private CareerChoiceObject _blackGrailVowPassive2;
        private CareerChoiceObject _blackGrailVowPassive3;
        private CareerChoiceObject _blackGrailVowPassive4;

        protected override void RegisterAll()
        {
            _blackGrailKnightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlackGrailKnightRoot"));

            _curseOfMousillonKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_curseOfMousillonKeystone).UnderscoreFirstCharToUpper()));
            _curseOfMousillonPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_curseOfMousillonPassive1).UnderscoreFirstCharToUpper()));
            _curseOfMousillonPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_curseOfMousillonPassive2).UnderscoreFirstCharToUpper()));
            _curseOfMousillonPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_curseOfMousillonPassive3).UnderscoreFirstCharToUpper()));
            _curseOfMousillonPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_curseOfMousillonPassive4).UnderscoreFirstCharToUpper()));

            _swampRiderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_swampRiderKeystone).UnderscoreFirstCharToUpper()));
            _swampRiderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_swampRiderPassive1).UnderscoreFirstCharToUpper()));
            _swampRiderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_swampRiderPassive2).UnderscoreFirstCharToUpper()));
            _swampRiderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_swampRiderPassive3).UnderscoreFirstCharToUpper()));
            _swampRiderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_swampRiderPassive4).UnderscoreFirstCharToUpper()));

            _unbreakableArmyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_swampRiderKeystone).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive1).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive2).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive3).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive4).UnderscoreFirstCharToUpper()));

            _scourgeOfMousillonKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfMousillonKeystone).UnderscoreFirstCharToUpper()));
            _scourgeOfMousillonPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfMousillonPassive1).UnderscoreFirstCharToUpper()));
            _scourgeOfMousillonPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfMousillonPassive2).UnderscoreFirstCharToUpper()));
            _scourgeOfMousillonPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfMousillonPassive3).UnderscoreFirstCharToUpper()));
            _scourgeOfMousillonPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfMousillonPassive4).UnderscoreFirstCharToUpper()));

            _robberBaronKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberBaronKeystone).UnderscoreFirstCharToUpper()));
            _robberBaronPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberBaronPassive1).UnderscoreFirstCharToUpper()));
            _robberBaronPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberBaronPassive2).UnderscoreFirstCharToUpper()));
            _robberBaronPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberBaronPassive3).UnderscoreFirstCharToUpper()));
            _robberBaronPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberBaronPassive4).UnderscoreFirstCharToUpper()));

            _lieOfLadyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_lieOfLadyKeystone).UnderscoreFirstCharToUpper()));
            _lieOfLadyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_lieOfLadyPassive1).UnderscoreFirstCharToUpper()));
            _lieOfLadyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_lieOfLadyPassive2).UnderscoreFirstCharToUpper()));
            _lieOfLadyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_lieOfLadyPassive3).UnderscoreFirstCharToUpper()));
            _lieOfLadyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_lieOfLadyPassive4).UnderscoreFirstCharToUpper()));
            
            _blackGrailVowKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_blackGrailVowKeystone).UnderscoreFirstCharToUpper()));
            _blackGrailVowPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_blackGrailVowPassive1).UnderscoreFirstCharToUpper()));
            _blackGrailVowPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_blackGrailVowPassive2).UnderscoreFirstCharToUpper()));
            _blackGrailVowPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_blackGrailVowPassive3).UnderscoreFirstCharToUpper()));
            _blackGrailVowPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_blackGrailVowPassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _blackGrailKnightRoot.Initialize(CareerID, "The knight prepares a devastating charge, mounted or on foot, for the next 6 seconds. When mounted, the knight receives perk buffs as well as a 20% chance of his lance not bouncing off after a couched lance attack. The couched lance attack chance increases by 0.1% for every point in Riding. When on foot, the knight only receives perk buffs.", null,
                true, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
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

            _curseOfMousillonKeystone.Initialize(CareerID, "10% extra physical damage during Knightly Charge. The ability scales with the One Handed skill.", "CurseOfMousillon", false,
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
            _swampRiderKeystone.Initialize(CareerID, "+20% speed during the ability, which now scales with the Polearm skill and starts recharged.", "SwampRider", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
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

            _unbreakableArmyKeystone.Initialize(CareerID, "Knightly Charge grants 20% physical resistance. Mount will not rear during the ability.", "UnbreakableArmy", false,
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

            _scourgeOfMousillonKeystone.Initialize(CareerID, "Ability duration scales with Polearm and Riding. The lance attack scales with Two Handed.", "ScourgeOfMousillon", false,
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
            
            _robberBaronKeystone.Initialize(CareerID, "Reduces cooldown of Knightly Charge by 30s. During the ability, all healing affects the horse.", "RobberBaron", false,
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
            
            _lieOfLadyKeystone.Initialize(CareerID, "Ability scales with Roguery. When active, get +20% magic damage and regen.", "LieOfLady", false,
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
            
            _blackGrailVowKeystone.Initialize(CareerID, "Ability scales with Leadership and propagates from all heroes to units in a 5m radius ", "BlackGrailVow", false,
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
            _curseOfMousillonPassive1.Initialize(CareerID, "10% extra melee damage.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee)); //
            _curseOfMousillonPassive2.Initialize(CareerID, "Increases Hitpoints by 40.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health)); // 
            _curseOfMousillonPassive3.Initialize(CareerID, "All Knight troops receive 20 bonus points in One and Two Handed skills.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special)); //
            _curseOfMousillonPassive4.Initialize(CareerID, "All melee troops in the party gain 25 exp per day.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //

            _swampRiderPassive1.Initialize(CareerID, "50% additional Hitpoints for the player's mount.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true)); //
            _swampRiderPassive2.Initialize(CareerID, "10% extra melee damage while on horseback.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, 
                (attacker, victim, mask) => attacker.IsMainAgent&& mask == AttackTypeMask.Melee && attacker.HasMount));
            
            _swampRiderPassive3.Initialize(CareerID, "Upgrade costs are reduced by 25%.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true)); //
            _swampRiderPassive4.Initialize(CareerID, "All Knight troops receive 30 bonus points in their Polearm skill.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special)); //

            _unbreakableArmyPassive1.Initialize(CareerID, "Increases Hitpoints by 40.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health)); //
            _unbreakableArmyPassive2.Initialize(CareerID, "Gain 15% physical resistance to melee and ranged attacks.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _unbreakableArmyPassive3.Initialize(CareerID, "All Knight troops gain 10% physical resistance.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All, 
                (attacker, victim, mask) => !victim.BelongsToMainParty()&&!victim.IsHero&&victim.Character.IsKnightUnit()));
            
            _unbreakableArmyPassive4.Initialize(CareerID, "Hits below 15 damage will not stagger the player.", "UnbreakableArmy", false, ChoiceType.Passive, null); // Agent extension 83,

            _scourgeOfMousillonPassive1.Initialize(CareerID, "10% extra melee fire damage.", "ScourgeOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Melee));
            _scourgeOfMousillonPassive2.Initialize(CareerID, "20% extra armor penetration of melee attacks.", "ScourgeOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _scourgeOfMousillonPassive3.Initialize(CareerID, "40% chance to recruit an extra unit of the same type free of charge.", "ScourgeOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special, true));
            _scourgeOfMousillonPassive4.Initialize(CareerID, "All Peasant troops wages are reduced by 75%.", "ScourgeOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-75, PassiveEffectType.TroopWages, true, 
                characterObject => characterObject.IsKnightUnit()));

            _robberBaronPassive1.Initialize(CareerID, "Horse charge damage is increased by 50%.", "RobberBaron", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.HorseChargeDamage, true));
            _robberBaronPassive2.Initialize(CareerID, "Party movement speed is increased by 2.", "RobberBaron", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.PartyMovementSpeed));
            _robberBaronPassive3.Initialize(CareerID, "+4 health regeneration on the campaign map.", "RobberBaron", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.HealthRegeneration, false));
            _robberBaronPassive4.Initialize(CareerID, "All Knight troops wages are reduced by 25%.", "RobberBaron", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages, true, 
                characterObject => characterObject.IsKnightUnit()));

            _lieOfLadyPassive1.Initialize(CareerID, "{=grail_vow_passive1_str}Increases Hitpoints by 40.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _lieOfLadyPassive2.Initialize(CareerID, "{=grail_vow_passive2_str}20% extra holy damage for Battle pilgrim troops.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee, 
                (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.BelongsToMainParty() && attacker.Character.UnitBelongsToCult("cult_of_lady")));
            _lieOfLadyPassive3.Initialize(CareerID, "{=grail_vow_passive3_str}20% extra melee holy damage.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy, 20), AttackTypeMask.Melee));
            _lieOfLadyPassive4.Initialize(CareerID, "{=grail_vow_passive4_str}Gain 15% Ward save.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All));
            
            _blackGrailVowPassive1.Initialize(CareerID, "{=holy_crusader_passive1_str}Increases Hitpoints by 40.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _blackGrailVowPassive2.Initialize(CareerID, "{=holy_crusader_passive2_str}Companion Health increases by 15 for every 'Knight' Companion.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
            _blackGrailVowPassive3.Initialize(CareerID, "{=holy_crusader_passive3_str}Grail Knights can be upgraded to Companions.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _blackGrailVowPassive4.Initialize(CareerID, "{=holy_crusader_passive4_str}Companion limit of party is increased by 10.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.CompanionLimit));
        }
        
    }
}