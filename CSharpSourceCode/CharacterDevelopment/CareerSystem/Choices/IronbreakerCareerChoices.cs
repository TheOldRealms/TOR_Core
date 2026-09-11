using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

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
        _ironbreakerRoot.Initialize(CareerID, "Khazukan Kazakit-ha! Become Impenetrable for 10 seconds plus 0.05 seconds per Athletics point. Gain +90% personal 'Ward Save', but move 25% slower. (Ability is charged by receiving and blocking damage.)", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Athletics }, 0.05f),
                    MutationType = OperationType.Add
                }
            });

        _nestCleansingKeystone.Initialize(CareerID, "Impenetrable adds 50% fire and explosion resistance to existing protection, and resists knockback and knockdown.", "NestCleansing", false,
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

        _tunnelWatchKeystone.Initialize(CareerID, "Impenetrable gains 0.02 seconds per Scouting point, and begins battle charged.", "TunnelWatch", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Scouting }, 0.02f),
                    MutationType = OperationType.Add
                }
            });

        _ironPriceKeystone.Initialize(CareerID, "Impenetrable gains 0.02 seconds per Leadership point and can be charged by dealing damage.", "IronPrice", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.02f),
                    MutationType = OperationType.Add
                }
            });

        _shieldwallKeystone.Initialize(CareerID, "Impenetrable gains 0.02 seconds per One-Handed point and applies to all allies within a radius of 5 metres.", "ShieldWall", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded }, 0.02f),
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

        _ironDrakesKeystone.Initialize(CareerID, "Impenetrable gains 0.02 seconds per Gunpowder point, and provides increased reload speed.", "IronDrakes", false,
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
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder }, 0.02f),
                    MutationType = OperationType.Add
                }
            });

        _gromrilArmorKeystone.Initialize(CareerID, "At 5s: +5% Physical Resistance per stored enemy melee hit for 10s. +25% reload speed; +0.02s Impenetrable per Gunpowder.", "GromrilArmor", false,
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
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder }, 0.02f),
                    MutationType = OperationType.Add
                }
            });

        _runeWeaponsKeystone.Initialize(CareerID, "At 5s: +5% Physical damage per stored enemy melee hit for 10s. +25% reload speed; +0.02s Impenetrable per Gunpowder.", "RuneWeapons", false,
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
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder }, 0.02f),
                    MutationType = OperationType.Add
                }
            });

    }

    protected override void InitializePassives()
    {
        _nestCleansingPassive1.Initialize(CareerID, "+10 personal Hitpoints.", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
        _nestCleansingPassive2.Initialize(CareerID, "+20% personal 'Fire Resistance'.", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Fire, 20), AttackTypeMask.All));
        _nestCleansingPassive3.Initialize(CareerID, "Your personal explosive charges gain +2 ammunition.", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _nestCleansingPassive4.Initialize(CareerID, "+50% chance of an 'Ironbreaker' troop to not consume an explosive charge when used.", "NestCleansing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

        _tunnelWatchPassive1.Initialize(CareerID, "+15 personal Hitpoints.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _tunnelWatchPassive2.Initialize(CareerID, "+1 party move speed on campaign map.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
        _tunnelWatchPassive3.Initialize(CareerID, "+15% personal 'Physical' damage when facing Greenskins.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.IsMainAgent && victim.Character is CharacterObject victimCharacter && victimCharacter.IsGreenskin()));
        _tunnelWatchPassive4.Initialize(CareerID, "+5% personal melee 'Physical' damage.", "TunnelWatch", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee));

        _ironPricePassive1.Initialize(CareerID, "Hits below 15 damage no longer stagger you.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));
        _ironPricePassive2.Initialize(CareerID, "Personal attacks against shields deal increased 'Physical' damage.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.BonusDamageShield, AttackTypeMask.Melee));
        _ironPricePassive3.Initialize(CareerID, "-25% gold cost when upgrading 'Ironbreaker' troops.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true, characterObject => characterObject.HasAttribute(CharacterAttributes.IRONBREAKER)));
        _ironPricePassive4.Initialize(CareerID, "+10 personal Hitpoints and -25% Oathgold cost when converting Dawi troops into Ironbreakers.", "IronPrice", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));

        _shieldwallPassive1.Initialize(CareerID, "+10 personal Hitpoints.", "ShieldWall", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
        _shieldwallPassive2.Initialize(CareerID, "+20 One-Handed skill for all Dawi troops.", "ShieldWall", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, nameof(DefaultSkills.OneHanded), characterObject => characterObject.Culture.StringId == TORConstants.Cultures.DAWI));
        _shieldwallPassive3.Initialize(CareerID, "+10% personal 'Physical Resistance' when a shield is equipped.", "ShieldWall", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All,
            (attacker, victim, mask) => victim.IsMainAgent && victim.Equipment?.HasShield() == true));
        _shieldwallPassive4.Initialize(CareerID, "Every Smithing level increases your shield's Hitpoints by 0.5%.", "ShieldWall", false, ChoiceType.Passive, null);

        _ironDrakesPassive1.Initialize(CareerID, "+20% Fire damage for 'Irondrake' troops.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Fire, 20), AttackTypeMask.Ranged,
            (attacker, victim, mask) => attacker.IsPlayerUnit && !attacker.IsHero && attacker.Character.IsIronbreakerUnit() && attacker.Character.HasAttribute(CharacterAttributes.DWARF_GUN)));
        _ironDrakesPassive2.Initialize(CareerID, "-25% 'Oathgold' upgrade cost for Ironbreakers, Ironbeards, Irondrakes, and Trollhammer Irondrakes.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true,
            characterObject => characterObject.HasAttribute(CharacterAttributes.IRONBREAKER)));
        _ironDrakesPassive3.Initialize(CareerID, "+12 ammunition for Drakefire canisters carried by you and companions in your party.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _ironDrakesPassive4.Initialize(CareerID, "+1% ammunition for 'Ironbreaker' troops per Ironbeard unit.", "IronDrakes", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

        _gromrilArmorPassive1.Initialize(CareerID, "+20% 'Physical Resistance' for 'Ironbreaker' troops.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.All,
            (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.IsIronbreakerUnit()));

        _gromrilArmorPassive2.Initialize(CareerID, "+15 personal Hitpoints.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _gromrilArmorPassive3.Initialize(CareerID, "+5% personal 'Ward Save' if armour weight exceeds 25.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 5), AttackTypeMask.All,
            (attacker, victim, attackmask) => victim.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(victim, 25, false)));
        _gromrilArmorPassive4.Initialize(CareerID, "-50% damage from friendly fire for 'Ironbreaker' troops.", "GromrilArmor", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special));

        _runeWeaponsPassive1.Initialize(CareerID, "+10% 'Physical' damage for 'Ironbreaker' troops.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.BelongsToMainParty() && attacker.Character.IsIronbreakerUnit()));
        _runeWeaponsPassive2.Initialize(CareerID, "+5% personal 'Magic' damage.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.All));
        _runeWeaponsPassive3.Initialize(CareerID, "+20% personal 'Armour Penetration' for melee attacks.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        _runeWeaponsPassive4.Initialize(CareerID, "+15% personal weapon swing speed.", "RuneWeapons", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed, true));

    }
}