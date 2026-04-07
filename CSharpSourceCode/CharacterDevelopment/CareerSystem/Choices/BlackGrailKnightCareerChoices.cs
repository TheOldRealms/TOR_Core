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
    public class BlackGrailKnightCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
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
                "The lies of the Lady shall blind us no more! Slay your foes with a Knightly Charge! For the next 6s while mounted, gain +20% movement speed and +20% chance for a couched lance to not reset after a hit. For every level of Riding, gain +0.1% chance to not reset a couched lance during Knightly Charge. (60s cooldown)",
                null,
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
                        MutationTargetOriginalId = "black_knightly_charge_phys_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });

            _curseOfMousillonKeystone.Initialize(CareerID, "Knightly Charge also scales with One-Handed skill, and gains +10% 'Physical' melee damage.", "CurseOfMousillon", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_knightly_charge",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_phys_dmg", "knightly_charge_speed_bonus" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _swampRiderKeystone.Initialize(CareerID, "Knightly Charge also scales with Polearm skill, gains +20% speed, can be used on battle start.", "SwampRider", false,
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

            _unbreakableArmyKeystone.Initialize(CareerID, "Knightly Charge gains +20% personal 'Physical Resistance', mount doesn't rear during ability.", "UnbreakableArmy", false,
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

            _scourgeOfBretonniaKeystone.Initialize(CareerID, "Knightly Charge duration scales with Polearm/Riding, its damage also scales with Two-Handed.", "ScourgeOfBretonnia", false,
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
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "knightly_charge_phys_dmg" }).ToList(),
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

            _robberKnightKeystone.Initialize(CareerID, "Knightly Charge's cooldown is 30s shorter, and any healing recieved also affects your horse.", "RobberKnight", false,
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

            _lieOfLadyKeystone.Initialize(CareerID, "Knightly Charge also scales with Roguery, gives regeneration, and gains +20% 'Magic' damage.", "LieOfLady", false,
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

            _blackGrailVowKeystone.Initialize(CareerID, "Knightly Charge also scales with Leadership, and applies to Companions within a 5m radius.", "BlackGrailVow", false,
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
            _curseOfMousillonPassive1.Initialize(CareerID, "+30 personal Hitpoints.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Health));
            _curseOfMousillonPassive2.Initialize(CareerID, "+20 Polearm skill for Mousillon 'Knight' troops.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, nameof(DefaultSkills.Polearm),
                (characterObject) => characterObject.IsKnightUnit()));
            _curseOfMousillonPassive3.Initialize(CareerID, "+15% ranged 'Physical' damage for Mousillon troops.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && mask == AttackTypeMask.Ranged && attacker.Character.Culture.StringId == "mousillon" && attacker.Character.IsRanged));
            _curseOfMousillonPassive4.Initialize(CareerID, "Ill-Fated Knight companions slowly convert Bretonnian peasants to Mousillon Peasents.", "CurseOfMousillon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //

            _swampRiderPassive1.Initialize(CareerID, "+50% personal mount Hitpoints.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true)); //
            _swampRiderPassive2.Initialize(CareerID, "+10% personal melee 'Physical' damage while mounted.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && attacker.HasMount));
            _swampRiderPassive3.Initialize(CareerID, "Melee kills grant Roguery experience.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
            _swampRiderPassive4.Initialize(CareerID, "+20 One-Handed and Two-Handed skill for all 'Knight' troops.", "SwampRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.OneHanded), nameof(DefaultSkills.TwoHanded) }, characterObject => characterObject.IsKnightUnit()));

            _unbreakableArmyPassive1.Initialize(CareerID, "-40% wages for all Mousillon peasant troops.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-40, PassiveEffectType.TroopWages, true,
                characterObject => !characterObject.IsKnightUnit() && characterObject.Culture.StringId == "mousillon"));
            _unbreakableArmyPassive2.Initialize(CareerID, "+1 increased party size per 3 Mousillon peasant troops.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.33f, PassiveEffectType.UnitPartyWeight, false, characterObject => !characterObject.IsKnightUnit() && characterObject.Culture.StringId == "mousillon"));
            _unbreakableArmyPassive3.Initialize(CareerID, "+10% personal 'Physical Resistance'.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _unbreakableArmyPassive4.Initialize(CareerID, "+1 increased party size per 2 'Undead' troops.", "UnbreakableArmy", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.UnitPartyWeight, false, characterObject => characterObject.IsUndead()));

            _scourgeOfBretonniaPassive1.Initialize(CareerID, "+30 personal Hitpoints.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Health));
            _scourgeOfBretonniaPassive2.Initialize(CareerID, "+15 daily 'Dark Energy'.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.CustomResourceGain));
            _scourgeOfBretonniaPassive3.Initialize(CareerID, "+8% melee 'Physical' damage for Mousillon 'Knight' troops.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 8), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && isMousillonKnight(attacker.Character as CharacterObject) && victim.Character.IsInfantry));
            _scourgeOfBretonniaPassive4.Initialize(CareerID, "Ill-Fated Knight companions slowly convert Bretonnian Knights to Mousillon Knights.", "ScourgeOfBretonnia", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true));

            _robberKnightPassive1.Initialize(CareerID, "+50% personal mount charge damage.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseChargeDamage, true));
            _robberKnightPassive2.Initialize(CareerID, "+1 party move speed on campaign map.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
            _robberKnightPassive3.Initialize(CareerID, "+10% 'Physical Resistance' for Mousillon 'Knight' troops.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All,
                    (attacker, victim, mask) => victim.BelongsToMainParty() && isMousillonKnight(victim.Character as CharacterObject)));
            _robberKnightPassive4.Initialize(CareerID, "Pillaging provides 'Dark Energy' and is +50% faster.", "RobberKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-50, PassiveEffectType.Special, true,
                characterObject => isMousillonKnight(characterObject)));

            _lieOfLadyPassive1.Initialize(CareerID, "+10% personal melee 'Magic' damage.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee));
            _lieOfLadyPassive2.Initialize(CareerID, "+10 'Winds of Magic' for Necromancer companions.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, false));
            _lieOfLadyPassive3.Initialize(CareerID, "-12% 'Dark Energy' upkeep for Mousillon 'Knight' troops.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-12, PassiveEffectType.CustomResourceUpkeepModifier, true,
                characterObject => characterObject.StringId.Contains("tor_m_knight_of_the_black_grail")));
            _lieOfLadyPassive4.Initialize(CareerID, "+8% melee 'Magic' damage for Mousillon 'Knight' troops.", "LieOfLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Magical, 8), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && attacker.Character.StringId == "tor_m_knight_of_the_black_grail"));

            _blackGrailVowPassive1.Initialize(CareerID, "+7 Companion limit.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(7, PassiveEffectType.CompanionLimit));
            _blackGrailVowPassive2.Initialize(CareerID, "Hits below 15 damage no longer stagger you.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));
            _blackGrailVowPassive3.Initialize(CareerID, "+8% 'Ward Save' for Mousillon 'Knight' troops.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 8), AttackTypeMask.All,
                (attacker, victim, mask) => victim.BelongsToMainParty() && isMousillonKnight(victim.Character as CharacterObject)));
            _blackGrailVowPassive4.Initialize(CareerID, "+5 daily 'Dark Energy' per Necromancer and Vampire companion.", "BlackGrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special));
        }

        public override void InitialCareerSetup()
        {
            ReligionObject lady = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_lady");
            if (lady != null)
            {
                Hero.MainHero.AddReligiousInfluence(lady, -100, false);
            }
            ReligionObject nagash = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash");
            if (nagash != null)
            {
                Hero.MainHero.AddReligiousInfluence(nagash, 25, true);
            }

            var spendAttributePoints = Hero.MainHero.GetAttributeValue(TORAttributes.Discipline) - 1;
            Hero.MainHero.HeroDeveloper.RemoveAttribute(TORAttributes.Discipline, spendAttributePoints);
            Hero.MainHero.HeroDeveloper.UnspentAttributePoints += spendAttributePoints;


            CultureObject mousillonCulture = MBObjectManager.Instance.GetObject<CultureObject>("mousillon");
            Hero.MainHero.Culture = mousillonCulture;
        }

        private bool isMousillonKnight(CharacterObject characterObject)
        {
            return characterObject.IsKnightUnit() && characterObject.Culture.StringId == "mousillon" ||
                   characterObject.IsVampire() && Hero.MainHero.HasCareerChoice("BlackGrailVowPassive2");
        }
    }
}