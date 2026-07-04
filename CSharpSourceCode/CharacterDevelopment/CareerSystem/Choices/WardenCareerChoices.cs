using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class WardenCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _wardenRoot;

        private CareerChoiceObject _wardenOfCavarocPassive1;
        private CareerChoiceObject _wardenOfCavarocPassive2;
        private CareerChoiceObject _wardenOfCavarocPassive3;
        private CareerChoiceObject _wardenOfCavarocPassive4;
        private CareerChoiceObject _wardenOfCavarocKeystone;

        private CareerChoiceObject _wardenOfCythralPassive1;
        private CareerChoiceObject _wardenOfCythralPassive2;
        private CareerChoiceObject _wardenOfCythralPassive3;
        private CareerChoiceObject _wardenOfCythralPassive4;
        private CareerChoiceObject _wardenOfCythralKeystone;

        private CareerChoiceObject _wardenOfWydriothPassive1;
        private CareerChoiceObject _wardenOfWydriothPassive2;
        private CareerChoiceObject _wardenOfWydriothPassive3;
        private CareerChoiceObject _wardenOfWydriothPassive4;
        private CareerChoiceObject _wardenOfWydriothKeystone;


        private CareerChoiceObject _wardenOfTorgovannPassive1;
        private CareerChoiceObject _wardenOfTorgovannPassive2;
        private CareerChoiceObject _wardenOfTorgovannPassive3;
        private CareerChoiceObject _wardenOfTorgovannPassive4;
        private CareerChoiceObject _wardenOfTorgovannKeystone;

        private CareerChoiceObject _wardenOfAtylwythPassive1;
        private CareerChoiceObject _wardenOfAtylwythPassive2;
        private CareerChoiceObject _wardenOfAtylwythPassive3;
        private CareerChoiceObject _wardenOfAtylwythPassive4;
        private CareerChoiceObject _wardenOfAtylwythKeystone;


        private CareerChoiceObject _wardenOfTalsynPassive1;
        private CareerChoiceObject _wardenOfTalsynPassive2;
        private CareerChoiceObject _wardenOfTalsynPassive3;
        private CareerChoiceObject _wardenOfTalsynPassive4;
        private CareerChoiceObject _wardenOfTalsynKeystone;

        private CareerChoiceObject _wardenOfArgwylonPassive1;
        private CareerChoiceObject _wardenOfArgwylonPassive2;
        private CareerChoiceObject _wardenOfArgwylonPassive3;
        private CareerChoiceObject _wardenOfArgwylonPassive4;
        private CareerChoiceObject _wardenOfArgwylonKeystone;

        protected override void RegisterAll()
        {

            _wardenRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenRoot).UnderscoreFirstCharToUpper()));

            _wardenOfCavarocPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCavarocPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfCavarocPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCavarocPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfCavarocPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCavarocPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfCavarocPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCavarocPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfCavarocKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCavarocKeystone).UnderscoreFirstCharToUpper()));

            _wardenOfCythralPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCythralPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfCythralPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCythralPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfCythralPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCythralPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfCythralPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCythralPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfCythralKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfCythralKeystone).UnderscoreFirstCharToUpper()));

            _wardenOfWydriothPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfWydriothPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfWydriothPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfWydriothPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfWydriothPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfWydriothPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfWydriothPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfWydriothPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfWydriothKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfWydriothKeystone).UnderscoreFirstCharToUpper()));

            _wardenOfTorgovannPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTorgovannPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfTorgovannPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTorgovannPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfTorgovannPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTorgovannPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfTorgovannPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTorgovannPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfTorgovannKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTorgovannKeystone).UnderscoreFirstCharToUpper()));

            _wardenOfAtylwythPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfAtylwythPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfAtylwythPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfAtylwythPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfAtylwythPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfAtylwythPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfAtylwythPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfAtylwythPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfAtylwythKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfAtylwythKeystone).UnderscoreFirstCharToUpper()));

            _wardenOfTalsynPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTalsynPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfTalsynPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTalsynPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfTalsynPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTalsynPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfTalsynPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTalsynPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfTalsynKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfTalsynKeystone).UnderscoreFirstCharToUpper()));

            _wardenOfArgwylonPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfArgwylonPassive1).UnderscoreFirstCharToUpper()));
            _wardenOfArgwylonPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfArgwylonPassive2).UnderscoreFirstCharToUpper()));
            _wardenOfArgwylonPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfArgwylonPassive3).UnderscoreFirstCharToUpper()));
            _wardenOfArgwylonPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfArgwylonPassive4).UnderscoreFirstCharToUpper()));
            _wardenOfArgwylonKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wardenOfArgwylonKeystone).UnderscoreFirstCharToUpper()));

        }

        protected override void InitializeKeyStones()
        {
            _wardenRoot.Initialize(CareerID, "The borders of Athel Loren must stand! Call upon your trusty bird companion Hawk Eye! Designate an area, reducing the 'Physical' melee damage of foes within. For every level of Scouting, gain 0.01% radius of Hawk Eye. (Ability has a 100s cooldown.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "HawkEye",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Scouting },  0.01f),
                        MutationType = OperationType.Add
                    }
                });

            _wardenOfCavarocKeystone.Initialize(CareerID, "Hawk Eye also scales with Riding, and reduces enemy movement speed.", "WardenOfCavaroc", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "HawkEye",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Riding },  0.01f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_hawk_eye",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "hawk_eye_debuff_mvs" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });

            _wardenOfCythralKeystone.Initialize(CareerID, "Hawk Eye also scales with Two-Handed weapon skill, and can be used on battle start.", "WardenOfCythral", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "HawkEye",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.TwoHanded },  0.01f),
                        MutationType = OperationType.Add
                    },
                });

            _wardenOfWydriothKeystone.Initialize(CareerID, "Hawk Eye also scales with Archery, and applies a 25% ranged damage penalty to foes.", "WardenOfWydrioth", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "HawkEye",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Bow },  0.01f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_hawk_eye",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "hawk_eye_debuff_range" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });

            _wardenOfTorgovannKeystone.Initialize(CareerID, "Hawk Eye also scales with One-Handed weapon skill, and reduces enemy attack speed.", "WardenOfTorgovann", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "HawkEye",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded },  0.01f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_hawk_eye",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "hawk_eye_debuff_ats" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });

            _wardenOfAtylwythKeystone.Initialize(CareerID, "Hawk Eye buffs allies within radius with increased movement and swing speed.", "WardenOfAtylwyth", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                   new CareerChoiceObject.MutationObject()
                   {
                       MutationTargetType = typeof(AbilityTemplate),
                       MutationTargetOriginalId = "HawkEye",
                       PropertyName = "TriggeredEffects",
                       PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "apply_hawk_eye_buff" }).ToList(),
                       MutationType = OperationType.Replace
                   },
                });


            _wardenOfTalsynKeystone.Initialize(CareerID, "Hawk Eye also scales with Leadership, and Throwing, its cooldown is 50% shorter.", "WardenOfTalsyn", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                   new CareerChoiceObject.MutationObject()
                   {
                       MutationTargetType = typeof(AbilityTemplate),
                       MutationTargetOriginalId = "HawkEye",
                       PropertyName = "Radius",
                       PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){DefaultSkills.Leadership , DefaultSkills.Throwing },  0.01f),
                       MutationType = OperationType.Add
                   },
                   new CareerChoiceObject.MutationObject()
                   {
                       MutationTargetType = typeof(AbilityTemplate),
                       MutationTargetOriginalId = "HawkEye",
                       PropertyName = "CoolDown",
                       PropertyValue = (choice, originalValue, agent) => (int)((int)originalValue * - 0.5f),
                       MutationType = OperationType.Add
                   },

                });

            _wardenOfArgwylonKeystone.Initialize(CareerID, "Hawk Eye increases 'Magic' damage done to enemies, and causes 'Physical' damage to them.", "WardenOfArgwylon", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                   new CareerChoiceObject.MutationObject()
                   {
                       MutationTargetType = typeof(TriggeredEffectTemplate),
                       MutationTargetOriginalId = "apply_hawk_eye",
                       PropertyName = "DamageAmount",
                       PropertyValue = (choice, originalValue, agent) => 3,
                       MutationType = OperationType.Replace
                   },
                   new CareerChoiceObject.MutationObject()
                   {
                       MutationTargetType = typeof(TriggeredEffectTemplate),
                       MutationTargetOriginalId = "apply_hawk_eye",
                       PropertyName = "ImbuedStatusEffects",
                       PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "hawk_eye_debuff_mag" }).ToList(),
                       MutationType = OperationType.Replace
                   },
                });


        }

        protected override void InitializePassives()
        {
            _wardenOfCavarocPassive1.Initialize(CareerID, "+1 party move speed on campaign map.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
            _wardenOfCavarocPassive2.Initialize(CareerID, "+50% personal mount Hitpoints.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true));
            _wardenOfCavarocPassive3.Initialize(CareerID, "+10% personal melee 'Physical' damage while mounted.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee | AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && attacker.HasMount));
            _wardenOfCavarocPassive4.Initialize(CareerID, "+50% charge damage bonus for your mount.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseChargeDamage, true));

            _wardenOfCythralPassive1.Initialize(CareerID, "+20 Two-Handed skill for all 'Elven' troops.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.TwoHanded) }, characterObject => characterObject.IsElf()));
            _wardenOfCythralPassive2.Initialize(CareerID, "+10% personal weapon swing speed.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SwingSpeed, true));
            _wardenOfCythralPassive3.Initialize(CareerID, "+15 personal Hitpoints.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
            _wardenOfCythralPassive4.Initialize(CareerID, "+10% personal 'Physical' melee and ranged damage against the forces of 'Chaos'.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee | AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && (mask == AttackTypeMask.Melee || mask == AttackTypeMask.Ranged) && (victim.Character.IsBeastman() || victim.Character.IsChaos())));

            _wardenOfWydriothPassive1.Initialize(CareerID, "+3 arrows per quiver you have equipped.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Ammo));
            _wardenOfWydriothPassive2.Initialize(CareerID, "+10% personal 'Physical' ranged damage.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Ranged));
            _wardenOfWydriothPassive3.Initialize(CareerID, "+20 Bow skill for all 'Elven' troops.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.Bow) }, characterObject => characterObject.IsElf()));
            _wardenOfWydriothPassive4.Initialize(CareerID, "+10% 'Physical' ranged damage for all 'Elven' troops.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All, (attacker, victim, mask) => mask == AttackTypeMask.Ranged && attacker.Character.IsElf()));

            _wardenOfTorgovannPassive1.Initialize(CareerID, "+10% personal 'Ward Save' when a shield is equipped.", "WardenOfTorgovann", false, ChoiceType.Passive, null,
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All,
                    (attacker, victim, mask) => victim.IsMainAgent && victim.WieldedOffhandWeapon.IsShield()));
            _wardenOfTorgovannPassive2.Initialize(CareerID, "+20 One-Handed weapon skill for all 'Elven' troops.", "WardenOfTorgovann", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.OneHanded) }, characterObject => characterObject.IsElf()));
            _wardenOfTorgovannPassive3.Initialize(CareerID, "+15 personal Hitpoints.", "WardenOfTorgovann", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
            _wardenOfTorgovannPassive4.Initialize(CareerID, "Hits below 15 damage no longer stagger you.", "WardenOfTorgovann", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));

            _wardenOfAtylwythPassive1.Initialize(CareerID, "+10 increased party size.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _wardenOfAtylwythPassive2.Initialize(CareerID, "+15% 'Physical Resistance' for Eternal Guard.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
                (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.IsElf() && victim.Character.StringId.Contains("eternal")));
            _wardenOfAtylwythPassive3.Initialize(CareerID, "+5 increased party size per Glade Captain Companion.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special));

            _wardenOfAtylwythPassive4.Initialize(CareerID, "+20 Polearm weapon skill for all 'Elven' troops.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.Polearm) }, characterObject => characterObject.IsElf()));

            _wardenOfTalsynPassive1.Initialize(CareerID, "Personal armour weight no longer impedes your 'Winds of Magic' recharge rate.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _wardenOfTalsynPassive2.Initialize(CareerID, "+10% 'Physical' damage for all 'Elven' troops.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All, (attacker, victim, mask) => mask == AttackTypeMask.All && attacker.Character.IsElf()));
            _wardenOfTalsynPassive3.Initialize(CareerID, "+5 Companion limit.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _wardenOfTalsynPassive4.Initialize(CareerID, "Your thrown spears can hit multiple enemies.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
            _wardenOfArgwylonPassive1.Initialize(CareerID, "+10% personal 'Magic' melee, and spell damage when below 25 weight.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee | AttackTypeMask.Spell,
                (attacker, victim, mask) => attacker.IsMainAgent && (mask == AttackTypeMask.Melee || mask == AttackTypeMask.Spell) && CareerChoicesHelper.ArmorWeightCheck(attacker, 25)));
            _wardenOfArgwylonPassive2.Initialize(CareerID, "+10 personal 'Winds of Magic' capacity.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _wardenOfArgwylonPassive3.Initialize(CareerID, "+10 'Harmony' daily.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.CustomResourceGain));
            _wardenOfArgwylonPassive4.Initialize(CareerID, "+15 'Winds of Magic' capacity for all Spellsinger Companions.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
        }



    }
}