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
            _wardenRoot.Initialize(CareerID, "The Warden orders his trusty hawk to scout an area. The hawk marks enemies caught in the area as prey. Its ferocious dives instill fear in the foe, which causes penalties to received melee damage. The radius of the ability increases with every point in the Scouting skill.  This ability refreshes automatically and gains additional effects with more Career perks unlocked.", null, true,
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

            _wardenOfCavarocKeystone.Initialize(CareerID, "Riding counts towards ability. Enemy movement speed is reduced for all units in the zone.", "WardenOfCavaroc", false,
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

            _wardenOfCythralKeystone.Initialize(CareerID, "Ability scales with two handed weapon skill. ability starts charged", "WardenOfCythral", false,
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

            _wardenOfWydriothKeystone.Initialize(CareerID, "Archery skill counts towards ability. Enemies in the zone suffer 25% more ranged damage", "WardenOfWydrioth", false,
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

            _wardenOfTorgovannKeystone.Initialize(CareerID, "One Handed counts towards ability. Enemy attack speed is reduced for all units in the zone.", "WardenOfTorgovann", false,
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

            _wardenOfAtylwythKeystone.Initialize(CareerID, " Swing and movement speed increase for all friendly units in the zone.", "WardenOfAtylwyth", false,
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


            _wardenOfTalsynKeystone.Initialize(CareerID, " Leadership and Throwing counts towards ability. Cooldown reduction by 50%.", "WardenOfTalsyn", false,
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

            _wardenOfArgwylonKeystone.Initialize(CareerID, "Magical Damage against affected enemies is increased. Small direct damage effects for every enemy in the zone.", "WardenOfArgwylon", false,
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
            _wardenOfCavarocPassive1.Initialize(CareerID, "Party speed increases by 2.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.PartyMovementSpeed));
            _wardenOfCavarocPassive2.Initialize(CareerID, "50% additional Hitpoints for the player's mount.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true));
            _wardenOfCavarocPassive3.Initialize(CareerID, "10% extra damage while on horseback.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee | AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && attacker.HasMount));
            _wardenOfCavarocPassive4.Initialize(CareerID, "Horse charge damage is increased by 50%.", "WardenOfCavaroc", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseChargeDamage, true));

            _wardenOfCythralPassive1.Initialize(CareerID, "All Elves receive 20 bonus points in their Two-handed skill.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.TwoHanded) }, characterObject => characterObject.IsElf()));
            _wardenOfCythralPassive2.Initialize(CareerID, "Weapon swing speed increased by 15%.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed, true));
            _wardenOfCythralPassive3.Initialize(CareerID, "Increases Hitpoints by 25.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _wardenOfCythralPassive4.Initialize(CareerID, "10% extra melee damage against chaos and beastmen.", "WardenOfCythral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee & AttackTypeMask.Ranged,
                (attacker, victim, mask) => victim.Character.Culture.StringId == TORConstants.Cultures.BEASTMEN || victim.Character.Culture.StringId == TORConstants.Cultures.CHAOS && attacker.IsMainAgent || attacker.Character.IsElf() && mask == (AttackTypeMask.Melee & AttackTypeMask.Ranged)));

            _wardenOfWydriothPassive1.Initialize(CareerID, "3 extra Arrows per equipped Quiver", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Ammo));
            _wardenOfWydriothPassive2.Initialize(CareerID, "10% extra range damage.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Ranged));
            _wardenOfWydriothPassive3.Initialize(CareerID, "All Elves receive 20 bonus points in their  bow skill.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.Bow) }, characterObject => characterObject.IsElf()));
            _wardenOfWydriothPassive4.Initialize(CareerID, "All troops gain 10% extra damage with bows.", "WardenOfWydrioth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All, (attacker, victim, mask) => mask == AttackTypeMask.Ranged && attacker.Character.IsElf()));

            _wardenOfTorgovannPassive1.Initialize(CareerID, "Wielding a shield increases wardsave.", "WardenOfTorgovann", false, ChoiceType.Passive, null,
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All,
                    (attacker, victim, mask) => victim.IsMainAgent && victim.WieldedOffhandWeapon.IsShield()));
            _wardenOfTorgovannPassive2.Initialize(CareerID, "All Elves receive 20 bonus points in their  One-handed skill.", "WardenOfTorgovann", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.OneHanded) }, characterObject => characterObject.IsElf()));
            _wardenOfTorgovannPassive3.Initialize(CareerID, "Increases Hitpoints by 25.", "WardenOfTorgovann", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _wardenOfTorgovannPassive4.Initialize(CareerID, "Hits below 15 damage do not stagger the player.", "WardenOfTorgovann", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));

            _wardenOfAtylwythPassive1.Initialize(CareerID, "Increases Party size by 10.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _wardenOfAtylwythPassive2.Initialize(CareerID, "Eternal guard troops gain 15% physical resistance.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
                (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.IsElf() && victim.Character.StringId.Contains("eternal")));
            _wardenOfAtylwythPassive3.Initialize(CareerID, "For every Glade Captain in your party gain 10 party size.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));

            _wardenOfAtylwythPassive4.Initialize(CareerID, "All Elves receive 20 bonus points in their polearm skill.", "WardenOfAtylwyth", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.Polearm) }, characterObject => characterObject.IsElf()));

            _wardenOfTalsynPassive1.Initialize(CareerID, "Armor weight doesn't affect winds regeneration.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _wardenOfTalsynPassive2.Initialize(CareerID, "All troops gain 10% extra damage.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All, (attacker, victim, mask) => mask == AttackTypeMask.All && attacker.Character.IsElf()));
            _wardenOfTalsynPassive3.Initialize(CareerID, "Companion limit of party is increased by 5.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _wardenOfTalsynPassive4.Initialize(CareerID, "Thrown spears can penetrate through multiple enemies.", "WardenOfTalsyn", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

            _wardenOfArgwylonPassive1.Initialize(CareerID, "10% extra magical melee and spell damage when weight undershoots 15", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee & AttackTypeMask.Spell,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == (AttackTypeMask.Melee & AttackTypeMask.Spell) && CareerChoicesHelper.ArmorWeightCheck(attacker, 15)));
            _wardenOfArgwylonPassive2.Initialize(CareerID, "Increases maximum winds of magic capacities by 15.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
            _wardenOfArgwylonPassive3.Initialize(CareerID, "Gain 20 Harmony daily.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.CustomResourceGain));
            _wardenOfArgwylonPassive4.Initialize(CareerID, "25 extra winds for all Spellsingers.", "WardenOfArgwylon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
        }



    }
}