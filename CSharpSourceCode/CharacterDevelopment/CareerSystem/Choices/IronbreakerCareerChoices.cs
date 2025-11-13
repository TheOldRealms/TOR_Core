using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class IronbreakerCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _ironbreakerRoot;

    private CareerChoiceObject _nestCleansingPassive1;
    private CareerChoiceObject _nestCleansingPassive2;
    private CareerChoiceObject _nestCleansingPassive3;
    private CareerChoiceObject _nestCleansingPassive4;
    private CareerChoiceObject _nestCleansingKeystone;

    private CareerChoiceObject _tunnelWatchPassive1;
    private CareerChoiceObject _tunnelWatchPassive2;
    private CareerChoiceObject _tunnelWatchPassive3;
    private CareerChoiceObject _tunnelWatchPassive4;
    private CareerChoiceObject _tunnelWatchKeystone;

    private CareerChoiceObject _ironPricePassive1;
    private CareerChoiceObject _ironPricePassive2;
    private CareerChoiceObject _ironPricePassive3;
    private CareerChoiceObject _ironPricePassive4;
    private CareerChoiceObject _ironPriceKeystone;

    private CareerChoiceObject _shieldwallPassive1;
    private CareerChoiceObject _shieldwallPassive2;
    private CareerChoiceObject _shieldwallPassive3;
    private CareerChoiceObject _shieldwallPassive4;
    private CareerChoiceObject _shieldwallKeystone;

    private CareerChoiceObject _ironDrakesPassive1;
    private CareerChoiceObject _ironDrakesPassive2;
    private CareerChoiceObject _ironDrakesPassive3;
    private CareerChoiceObject _ironDrakesPassive4;
    private CareerChoiceObject _ironDrakesKeystone;

    private CareerChoiceObject _gromrilArmorPassive1;
    private CareerChoiceObject _gromrilArmorPassive2;
    private CareerChoiceObject _gromrilArmorPassive3;
    private CareerChoiceObject _gromrilArmorPassive4;
    private CareerChoiceObject _gromrilArmorKeystone;

    private CareerChoiceObject _runeWeaponsPassive1;
    private CareerChoiceObject _runeWeaponsPassive2;
    private CareerChoiceObject _runeWeaponsPassive3;
    private CareerChoiceObject _runeWeaponsPassive4;
    private CareerChoiceObject _runeWeaponsKeystone;



    protected override void RegisterAll()
    {
        _ironbreakerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("IronbreakerRoot"));

        _nestCleansingPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_nestCleansingPassive1).UnderscoreFirstCharToUpper()));
        _nestCleansingPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_nestCleansingPassive2).UnderscoreFirstCharToUpper()));
        _nestCleansingPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_nestCleansingPassive3).UnderscoreFirstCharToUpper()));
        _nestCleansingPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_nestCleansingPassive4).UnderscoreFirstCharToUpper()));
        _nestCleansingKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_nestCleansingKeystone).UnderscoreFirstCharToUpper()));

        _tunnelWatchPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tunnelWatchPassive1).UnderscoreFirstCharToUpper()));
        _tunnelWatchPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tunnelWatchPassive2).UnderscoreFirstCharToUpper()));
        _tunnelWatchPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tunnelWatchPassive3).UnderscoreFirstCharToUpper()));
        _tunnelWatchPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tunnelWatchPassive4).UnderscoreFirstCharToUpper()));
        _tunnelWatchKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tunnelWatchKeystone).UnderscoreFirstCharToUpper()));

        _ironPricePassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironPricePassive1).UnderscoreFirstCharToUpper()));
        _ironPricePassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironPricePassive2).UnderscoreFirstCharToUpper()));
        _ironPricePassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironPricePassive3).UnderscoreFirstCharToUpper()));
        _ironPricePassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironPricePassive4).UnderscoreFirstCharToUpper()));
        _ironPriceKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironPriceKeystone).UnderscoreFirstCharToUpper()));

        _shieldwallPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shieldwallPassive1).UnderscoreFirstCharToUpper()));
        _shieldwallPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shieldwallPassive2).UnderscoreFirstCharToUpper()));
        _shieldwallPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shieldwallPassive3).UnderscoreFirstCharToUpper()));
        _shieldwallPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shieldwallPassive4).UnderscoreFirstCharToUpper()));
        _shieldwallKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shieldwallKeystone).UnderscoreFirstCharToUpper()));

        _ironDrakesPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironDrakesPassive1).UnderscoreFirstCharToUpper()));
        _ironDrakesPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironDrakesPassive2).UnderscoreFirstCharToUpper()));
        _ironDrakesPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironDrakesPassive3).UnderscoreFirstCharToUpper()));
        _ironDrakesPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironDrakesPassive4).UnderscoreFirstCharToUpper()));
        _ironDrakesKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ironDrakesKeystone).UnderscoreFirstCharToUpper()));

        _gromrilArmorPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gromrilArmorPassive1).UnderscoreFirstCharToUpper()));
        _gromrilArmorPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gromrilArmorPassive2).UnderscoreFirstCharToUpper()));
        _gromrilArmorPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gromrilArmorPassive3).UnderscoreFirstCharToUpper()));
        _gromrilArmorPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gromrilArmorPassive4).UnderscoreFirstCharToUpper()));
        _gromrilArmorKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gromrilArmorKeystone).UnderscoreFirstCharToUpper()));

        _runeWeaponsPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runeWeaponsPassive1).UnderscoreFirstCharToUpper()));
        _runeWeaponsPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runeWeaponsPassive2).UnderscoreFirstCharToUpper()));
        _runeWeaponsPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runeWeaponsPassive3).UnderscoreFirstCharToUpper()));
        _runeWeaponsPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runeWeaponsPassive4).UnderscoreFirstCharToUpper()));
        _runeWeaponsKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runeWeaponsKeystone).UnderscoreFirstCharToUpper()));


    }

    protected override void InitializeKeyStones()
    {
        _ironbreakerRoot.Initialize(CareerID, "{=ironbreaker_root_str}The Character will not receive any damage for a short amount of time and will shrug any damage off. The movement speed is reduced by 25% though. The duration expands with growing athletics skill.", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Athletics }, 0.004f),
                    MutationType = OperationType.Add
                }
            });

        _nestCleansingKeystone.Initialize(CareerID, "{=nest_cleansing_keystone_str}Immune to fire damage, explosives and knockback resistance during ability", "NestCleansing", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "impenetrable_fire_res" }).ToList(),
                    MutationType = OperationType.Replace
                },
            });

        _tunnelWatchKeystone.Initialize(CareerID, "{=tunnel_watch_keystone_str}Scouting counts towards ability. Ability starts charged", "TunnelWatch", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Scouting }, 0.004f),
                    MutationType = OperationType.Add
                }
            });

        _ironPriceKeystone.Initialize(CareerID, "{=iron_price_keystone_str}Leadership counts towards Careerability. Charge ability by dealing damage", "IronPrice", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Scouting }, 0.004f),
                    MutationType = OperationType.Add
                }
            });

        _shieldwallKeystone.Initialize(CareerID, "{=shield_wall_keystone_str}Dwarfs in the immediate surrounding also benefit from effect. Scales with onehanded", "ShieldWall", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded }, 0.004f),
                    MutationType = OperationType.Add
                },
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "Impenetrable",
                    PropertyName = "AbilityTargetType",
                    PropertyValue = (choice, originalValue, agent) => AbilityTargetType.AlliesInAOE,
                    MutationType = OperationType.Replace
                },
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "TargetType",
                    PropertyValue = (choice, originalValue, agent) => TargetType.Friendly,
                    MutationType = OperationType.Replace
                }
            });

        _ironDrakesKeystone.Initialize(CareerID, "{=iron_drakes_keystone_str}During ability your reload speed is increased. ability scales with gunpowder skill ", "IronDrakes", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "impenetrable_rls" }).ToList(),
                    MutationType = OperationType.Replace
                },
                new CareerChoiceObject.MutationObject()
                {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "apply_impenetrable",
                PropertyName = "ImbuedStatusEffectDuration",
                PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder }, 0.004f),
                MutationType = OperationType.Add
            }
            });

        _gromrilArmorKeystone.Initialize(CareerID, "{=gromril_armor_keystone_str}Receiving damage during ability will stack upon, and add for each taken hit 0.5% physical resistance post ability.", "GromrilArmor", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "impenetrable_rls" }).ToList(),
                    MutationType = OperationType.Replace
                },
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder }, 0.004f),
                    MutationType = OperationType.Add
                }
            });

        _runeWeaponsKeystone.Initialize(CareerID, "{=rune_weapons_keystone_str}For every taken hit during  your ability, physical damage is increased by 0.5% for the next 5 seconds", "RuneWeapons", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "impenetrable_rls" }).ToList(),
                    MutationType = OperationType.Replace
                },
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder }, 0.004f),
                    MutationType = OperationType.Add
                }
            });

    }

    protected override void InitializePassives()
    {
        _nestCleansingPassive1.Initialize(CareerID, "{=nest_cleansing_passive1_str}Increases Hitpoints by 25.", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _nestCleansingPassive2.Initialize(CareerID, "{=nest_cleansing_passive2_str}Increases Fire resistance by 25%.", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Fire, 25), AttackTypeMask.All));
        _nestCleansingPassive3.Initialize(CareerID, "{=nest_cleansing_passive3_str}Get 2 additional charges for explosive charges", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _nestCleansingPassive4.Initialize(CareerID, "{=nest_cleansing_passive4_str}Iron breaker troops gain 1 explosive charge", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

        _tunnelWatchPassive1.Initialize(CareerID, "{=tunnel_watch_passive1_str}Increases Hitpoints by 25.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _tunnelWatchPassive2.Initialize(CareerID, "{=tunnel_watch_passive2_str}Party movement speed is increased by 1.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
        _tunnelWatchPassive3.Initialize(CareerID, "{=tunnel_watch_passive3_str}15% extra melee damage against greenskins", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
            (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && victim.Character.Culture.StringId == TORConstants.Cultures.GREENSKIN));
        _tunnelWatchPassive4.Initialize(CareerID, "{=tunnel_watch_passive4_str}10% extra melee damage.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));

        _ironPricePassive1.Initialize(CareerID, "{=iron_price_passive1_str}Hits below 15 damage do not stagger the player.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));
        _ironPricePassive2.Initialize(CareerID, "{=iron_price_passive2_str}Attacks deal bonus damage against shields.", "IronPrice", false, ChoiceType.Passive, null);
        _ironPricePassive3.Initialize(CareerID, "{=iron_price_passive3_str}custom resource upgrade costs for Ironbreaker units are reduced by 25%.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true, characterObject => characterObject.HasAttribute("Ironbreaker")));
        _ironPricePassive4.Initialize(CareerID, "{=iron_price_passive4_str}Custom.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true, characterObject => characterObject.HasAttribute("Knightly")));

        _shieldwallPassive1.Initialize(CareerID, "{=shield_wall_passive1_str}Increases Hitpoints by 25.", "ShieldWall", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _shieldwallPassive2.Initialize(CareerID, "{=shield_wall_passive2_str}Onehanded combat skill for dwarf units is increased by 20", "ShieldWall", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, nameof(DefaultSkills.OneHanded), characterObject => characterObject.Culture.StringId == TORConstants.Cultures.EMPIRE));
        _shieldwallPassive3.Initialize(CareerID, "{=shield_wall_passive3_str}20% physical resistance while wearing a shield.", "ShieldWall", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.All,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && victim.IsMainAgent && victim.WieldedOffhandWeapon.IsShield()));
        _shieldwallPassive4.Initialize(CareerID, "{=shield_wall_passive4_str}For every point in smithing gain 0.5% shield hitpoints.", "ShieldWall", false, ChoiceType.Passive, null);

        _ironDrakesPassive1.Initialize(CareerID, "{=iron_drakes_passive1_str}Increases the fire damage of iron drakes by 20%.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Fire, 20), AttackTypeMask.Ranged,
            (attacker, victim, mask) => attacker.IsPlayerUnit && !attacker.IsHero && mask == AttackTypeMask.Ranged && attacker.Character.StringId.Contains("IronDrake")));
        _ironDrakesPassive2.Initialize(CareerID, "{=iron_drakes_passive2_str}Iron drakes custom resource cost is reduced.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true, characterObject => characterObject.StringId.Contains("Irondrake")));
        _ironDrakesPassive3.Initialize(CareerID, "{=iron_drakes_passive3_str}custom resource upgrade costs for knights are reduced by 25%.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true, characterObject => characterObject.HasAttribute("Knightly")));
        _ironDrakesPassive4.Initialize(CareerID, "{=iron_drakes_passive4_str}12 Extra ammo for drakfire gun", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 50), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.Team == victim.Team && attacker.Character.StringId.Contains("ironbreaker")));

        _gromrilArmorPassive1.Initialize(CareerID, "{=gromril_armor_passive1_str}20% extra phyiscal resistance for ironbreaker troops.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee,
            (attacker, victim, mask) => attacker.Team == victim.Team && attacker.Character.StringId.Contains("ironbreaker")));

        _gromrilArmorPassive2.Initialize(CareerID, "{=gromril_armor_passive2_str}Increases Hitpoints by 25.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _gromrilArmorPassive3.Initialize(CareerID, "{=gromril_armor_passive3_str}Extra 15% Wardsave if your armor weight exceed 25 weight.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.Spell,
            (attacker, victim, attackmask) => attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(attacker, 25, false)));
        _gromrilArmorPassive4.Initialize(CareerID, "{=gromril_armor_passive4_str}Iron breaker have 50% damage resistance against friendly fire.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 50), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.Team == victim.Team && attacker.Character.StringId.Contains("ironbreaker")));

        _runeWeaponsPassive1.Initialize(CareerID, "{=rune_weapon_passive1_str}Ironbreakers gain 10% extra damage", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 10), AttackTypeMask.Melee,
            (attacker, victim, mask) => attacker.BelongsToMainParty() && mask == AttackTypeMask.Melee && attacker.Character.StringId == "ironbreaker"));
        _runeWeaponsPassive2.Initialize(CareerID, "{=rune_weapon_passive2_str}Increases Magic damage by 15%.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Melee));
        _runeWeaponsPassive3.Initialize(CareerID, "{=rune_weapon_passive3_str}Extra 25% armor penetration of melee attacks.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        _runeWeaponsPassive4.Initialize(CareerID, "{=rune_weapon_passive4_str}Weapon swing speed increased by 15%.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed, true));

    }
}