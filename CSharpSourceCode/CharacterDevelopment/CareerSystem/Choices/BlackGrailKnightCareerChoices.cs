using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class BlackGrailKnightCareerChoices : TORCareerChoicesBase
    {
        public BlackGrailKnightCareerChoices(CareerObject id) : base(id)
        {
        }

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

        private CareerChoiceObject _scourgeOfBretonniaKeystone;
        private CareerChoiceObject _scourgeOfBretonniaPassive1;
        private CareerChoiceObject _scourgeOfBretonniaPassive2;
        private CareerChoiceObject _scourgeOfBretonniaPassive3;
        private CareerChoiceObject _scourgeOfBretonniaPassive4;

        private CareerChoiceObject _robberKnightKeystone;
        private CareerChoiceObject _robberKnightPassive1;
        private CareerChoiceObject _robberKnightPassive2;
        private CareerChoiceObject _robberKnightPassive3;
        private CareerChoiceObject _robberKnightPassive4;

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

            _unbreakableArmyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyKeystone).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive1).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive2).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive3).UnderscoreFirstCharToUpper()));
            _unbreakableArmyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unbreakableArmyPassive4).UnderscoreFirstCharToUpper()));

            _scourgeOfBretonniaKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfBretonniaKeystone).UnderscoreFirstCharToUpper()));
            _scourgeOfBretonniaPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfBretonniaPassive1).UnderscoreFirstCharToUpper()));
            _scourgeOfBretonniaPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfBretonniaPassive2).UnderscoreFirstCharToUpper()));
            _scourgeOfBretonniaPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfBretonniaPassive3).UnderscoreFirstCharToUpper()));
            _scourgeOfBretonniaPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_scourgeOfBretonniaPassive4).UnderscoreFirstCharToUpper()));

            _robberKnightKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberKnightKeystone).UnderscoreFirstCharToUpper()));
            _robberKnightPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberKnightPassive1).UnderscoreFirstCharToUpper()));
            _robberKnightPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberKnightPassive2).UnderscoreFirstCharToUpper()));
            _robberKnightPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberKnightPassive3).UnderscoreFirstCharToUpper()));
            _robberKnightPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_robberKnightPassive4).UnderscoreFirstCharToUpper()));

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
            _blackGrailKnightRoot.Initialize(CareerID,
                "The knight prepares a devastating charge, mounted or on foot, for the next 6 seconds. When mounted, the knight receives perk buffs as well as a 20% chance of his lance not bouncing off after a couched lance attack. The couched lance attack chance increases by 0.1% for every point in Riding. When on foot, the knight only receives perk buffs.",
                null,
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
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
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
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_phys_dmg" }).ToList(),
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
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
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
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_speed" }).ToList(),
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
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
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
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "black_knightly_charge_phys_res" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_horse_steady" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _scourgeOfBretonniaKeystone.Initialize(CareerID, "Ability duration scales with Polearm and Riding. The lance attack scales with Two Handed.", "ScourgeOfBretonnia", false,
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
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.TwoHanded }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });

            _robberKnightKeystone.Initialize(CareerID, "Reduces cooldown of Knightly Charge by 30s. During the ability, all healing affects the horse.", "RobberKnight", false,
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
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_link" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _lieOfLadyKeystone.Initialize(CareerID, "Ability scales with Roguery. When active, get +20% magic damage and health regen.", "LieOfLady", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "knightly_charge_lsc",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Roguery }, 0.001f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_healing_dark", "knightly_charge_magic_dmg" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Roguery }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });

            _blackGrailVowKeystone.Initialize(CareerID, "Ability scales with Leadership and propagates from all heroes to units in a 5m radius.", "BlackGrailVow", false,
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
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
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
            _curseOfMousillonPassive1.Initialize(CareerID, "Increases Hitpoints by 40.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _curseOfMousillonPassive2.Initialize(CareerID, "All Knight troops receive 30 bonus points in their Polearm skill.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special)); //
            _curseOfMousillonPassive3.Initialize(CareerID, "Mousillon ranged troops gain 15% extra ranged damage.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && mask == AttackTypeMask.Ranged && attacker.Character.Culture.StringId == "mousillon" && attacker.Character.IsRanged));
            _curseOfMousillonPassive4.Initialize(CareerID, "Ill fated Knight Companions occasionally train Bretonnian Peasants to Mousillon Peasants.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //

            _swampRiderPassive1.Initialize(CareerID, "50% additional Hitpoints for the player's mount.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true)); //
            _swampRiderPassive2.Initialize(CareerID, "10% extra melee damage while on horseback.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && attacker.HasMount));
            _swampRiderPassive3.Initialize(CareerID, "Every melee kill gives roguery XP.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true)); //
            _swampRiderPassive4.Initialize(CareerID, "All Knight troops receive 20 bonus points in their One and Two-handed skill.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special)); //

            _unbreakableArmyPassive1.Initialize(CareerID, "All mousillon peasant troops wages are reduced by 75%.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-75, PassiveEffectType.TroopWages, true,
                characterObject => !characterObject.IsKnightUnit() && characterObject.Culture.StringId == "mousillon"));
            _unbreakableArmyPassive2.Initialize(CareerID, "40% chance to recruit an extra unit of the same type free of charge.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special, true));
            _unbreakableArmyPassive3.Initialize(CareerID, "Gain 15% physical resistance to melee and ranged attacks.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _unbreakableArmyPassive4.Initialize(CareerID, "Increases Party size by 50.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.PartySize));

            _scourgeOfBretonniaPassive1.Initialize(CareerID, "Increases Hitpoints by 40.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _scourgeOfBretonniaPassive2.Initialize(CareerID, "Gain 25 Dark Energy daily.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.CustomResourceGain));
            _scourgeOfBretonniaPassive3.Initialize(CareerID, "Mousillon Knight damage against infantry  is increased by 15%.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.Spell,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && isMousillonKnight(attacker.Character as CharacterObject) && victim.Character.IsInfantry));
            _scourgeOfBretonniaPassive4.Initialize(CareerID, "Any Bret. Knight units can be transformed to ill-fated mousillon units.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true));

            _robberKnightPassive1.Initialize(CareerID, "Horse charge damage is increased by 50%.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.HorseChargeDamage, true));
            _robberKnightPassive2.Initialize(CareerID, "Party movement speed is increased by 2.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.PartyMovementSpeed));
            _robberKnightPassive3.Initialize(CareerID, "All mousillon Knight units gain 10% Physical resistance.", "RobberKnight", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
                    (attacker, victim, mask) => victim.BelongsToMainParty() && isMousillonKnight(attacker.Character as CharacterObject)));
            _robberKnightPassive4.Initialize(CareerID, "Pillaging is 50% faster  and gains dark energy.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-50, PassiveEffectType.Special, true,
                characterObject => isMousillonKnight(characterObject)));

            _lieOfLadyPassive1.Initialize(CareerID, "15% extra melee magic damage.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Melee));
            _lieOfLadyPassive2.Initialize(CareerID, "Necromancer companions gain 25 Winds of magic.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, false));
            _lieOfLadyPassive3.Initialize(CareerID, "Dark Energy upkeep for Knights of the black grail is reduced by 25%.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpkeepModifier, true,
                characterObject => characterObject.StringId.Contains("tor_m_knight_of_the_black_grail")));
            _lieOfLadyPassive4.Initialize(CareerID, "Knights of the black grail deal 15% magical damage.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && attacker.Character.StringId == "tor_m_knight_of_the_black_grail"));

            _blackGrailVowPassive1.Initialize(CareerID, "{=holy_crusader_passive4_str}Companion limit of party is increased by 10.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.CompanionLimit));
            _blackGrailVowPassive2.Initialize(CareerID, "{=holy_crusader_passive2_str}Hits below 15 damage will not stagger the player.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
            _blackGrailVowPassive3.Initialize(CareerID, "{=holy_crusader_passive3_str}Mousillon Knights gain 15% Wardsave.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All,
                (attacker, victim, mask) => victim.BelongsToMainParty() && isMousillonKnight(victim.Character as CharacterObject)));
            _blackGrailVowPassive4.Initialize(CareerID, "Every necromancer and Vampire companion gains 10 dark energy per day.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.CompanionLimit));
        }
        
        public override void InitialCareerSetup()
        {
            ReligionObject lady= ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_lady");
            if (lady != null)
            {
                Hero.MainHero.AddReligiousInfluence(lady,-100,false);
            }
            ReligionObject nagash= ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash");
            if (nagash != null)
            {
                Hero.MainHero.AddReligiousInfluence(nagash,25,true);
            }
            
            var spendAttributePoints =Hero.MainHero.GetAttributeValue(TORAttributes.Discipline)-1;
            Hero.MainHero.HeroDeveloper.RemoveAttribute(TORAttributes.Discipline,spendAttributePoints);
            Hero.MainHero.HeroDeveloper.UnspentAttributePoints += spendAttributePoints;
            
            
            CultureObject mousillonCulture= MBObjectManager.Instance.GetObject<CultureObject>("mousillon");
            Hero.MainHero.Culture = mousillonCulture;
        }

        private bool isMousillonKnight(CharacterObject characterObject)
        {
            return characterObject.IsKnightUnit() && characterObject.Culture.StringId == "mousillon" ||
                   characterObject.IsVampire() && Hero.MainHero.HasCareerChoice("BlackGrailVowPassive2");
        }
    }
}