using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class KnightOldWorldCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _knightOldWorldRoot;

    private CareerChoiceObject _secularOrdersPassive1;
    private CareerChoiceObject _secularOrdersPassive2;
    private CareerChoiceObject _secularOrdersPassive3;
    private CareerChoiceObject _secularOrdersPassive4;
    private CareerChoiceObject _secularOrdersKeystone;

    private CareerChoiceObject _pathOfConquestPassive1;
    private CareerChoiceObject _pathOfConquestPassive2;
    private CareerChoiceObject _pathOfConquestPassive3;
    private CareerChoiceObject _pathOfConquestPassive4;
    private CareerChoiceObject _pathOfConquestKeystone;

    private CareerChoiceObject _squiresPassive1;
    private CareerChoiceObject _squiresPassive2;
    private CareerChoiceObject _squiresPassive3;
    private CareerChoiceObject _squiresPassive4;
    private CareerChoiceObject _squiresKeystone;

    private CareerChoiceObject _templarOrdersKeystone;
    private CareerChoiceObject _templarOrdersPassive1;
    private CareerChoiceObject _templarOrdersPassive2;
    private CareerChoiceObject _templarOrdersPassive3;
    private CareerChoiceObject _templarOrdersPassive4;

    private CareerChoiceObject _pathOfVigilanceKeystone;
    private CareerChoiceObject _pathOfVigilancePassive1;
    private CareerChoiceObject _pathOfVigilancePassive2;
    private CareerChoiceObject _pathOfVigilancePassive3;
    private CareerChoiceObject _pathOfVigilancePassive4;

    private CareerChoiceObject _wrathAgainstChaosKeystone;
    private CareerChoiceObject _wrathAgainstChaosPassive1;
    private CareerChoiceObject _wrathAgainstChaosPassive2;
    private CareerChoiceObject _wrathAgainstChaosPassive3;
    private CareerChoiceObject _wrathAgainstChaosPassive4;

    private CareerChoiceObject _pathOfGloryKeystone;
    private CareerChoiceObject _pathOfGloryPassive1;
    private CareerChoiceObject _pathOfGloryPassive2;
    private CareerChoiceObject _pathOfGloryPassive3;
    private CareerChoiceObject _pathOfGloryPassive4;


    protected override void RegisterAll()
    {
        _knightOldWorldRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightOldWorldRoot"));

        _secularOrdersKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_secularOrdersKeystone).UnderscoreFirstCharToUpper()));
        _secularOrdersPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_secularOrdersPassive1).UnderscoreFirstCharToUpper()));
        _secularOrdersPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_secularOrdersPassive2).UnderscoreFirstCharToUpper()));
        _secularOrdersPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_secularOrdersPassive3).UnderscoreFirstCharToUpper()));
        _secularOrdersPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_secularOrdersPassive4).UnderscoreFirstCharToUpper()));

        _pathOfConquestKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfConquestKeystone).UnderscoreFirstCharToUpper()));
        _pathOfConquestPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfConquestPassive1).UnderscoreFirstCharToUpper()));
        _pathOfConquestPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfConquestPassive2).UnderscoreFirstCharToUpper()));
        _pathOfConquestPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfConquestPassive3).UnderscoreFirstCharToUpper()));
        _pathOfConquestPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfConquestPassive4).UnderscoreFirstCharToUpper()));


        _squiresKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_squiresKeystone).UnderscoreFirstCharToUpper()));
        _squiresPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_squiresPassive1).UnderscoreFirstCharToUpper()));
        _squiresPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_squiresPassive2).UnderscoreFirstCharToUpper()));
        _squiresPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_squiresPassive3).UnderscoreFirstCharToUpper()));
        _squiresPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_squiresPassive4).UnderscoreFirstCharToUpper()));

        _templarOrdersKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_templarOrdersKeystone).UnderscoreFirstCharToUpper()));
        _templarOrdersPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_templarOrdersPassive1).UnderscoreFirstCharToUpper()));
        _templarOrdersPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_templarOrdersPassive2).UnderscoreFirstCharToUpper()));
        _templarOrdersPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_templarOrdersPassive3).UnderscoreFirstCharToUpper()));
        _templarOrdersPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_templarOrdersPassive4).UnderscoreFirstCharToUpper()));

        _pathOfVigilanceKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfVigilanceKeystone).UnderscoreFirstCharToUpper()));
        _pathOfVigilancePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfVigilancePassive1).UnderscoreFirstCharToUpper()));
        _pathOfVigilancePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfVigilancePassive2).UnderscoreFirstCharToUpper()));
        _pathOfVigilancePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfVigilancePassive3).UnderscoreFirstCharToUpper()));
        _pathOfVigilancePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfVigilancePassive4).UnderscoreFirstCharToUpper()));

        _wrathAgainstChaosKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wrathAgainstChaosKeystone).UnderscoreFirstCharToUpper()));
        _wrathAgainstChaosPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wrathAgainstChaosPassive1).UnderscoreFirstCharToUpper()));
        _wrathAgainstChaosPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wrathAgainstChaosPassive2).UnderscoreFirstCharToUpper()));
        _wrathAgainstChaosPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wrathAgainstChaosPassive3).UnderscoreFirstCharToUpper()));
        _wrathAgainstChaosPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wrathAgainstChaosPassive4).UnderscoreFirstCharToUpper()));

        _pathOfGloryKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfGloryKeystone).UnderscoreFirstCharToUpper()));
        _pathOfGloryPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfGloryPassive1).UnderscoreFirstCharToUpper()));
        _pathOfGloryPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfGloryPassive2).UnderscoreFirstCharToUpper()));
        _pathOfGloryPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfGloryPassive3).UnderscoreFirstCharToUpper()));
        _pathOfGloryPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathOfGloryPassive4).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _knightOldWorldRoot.Initialize(CareerID, "Slay the Empire's foes with a Knightly Strike! Gain +20% personal 'Physical' melee damage on the next enemy hit within 15s. Every 'Keystone' career talent unlocked provides +1 charge of Knightly Strike. Each level in the One Handed skill increases the damage of Knightly Strike by 1%. (Ability charges by dealing 'Physical' melee damage.)", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "KnightlyStrike",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.01f, false,false),
                    MutationType = OperationType.Add
                }
            });

        _secularOrdersKeystone.Initialize(CareerID, "Knightly Strike gains a charge every 100 points in a melee weapon skill up to a max of 15.", "SecularOrders", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                
            }, new CareerChoiceObject.PassiveEffect());

        _pathOfConquestKeystone.Initialize(CareerID, "Knightly Strike now cleaves, and can be used at battle start.", "PathOfConquest", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "KnightlyStrike",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.TwoHanded }, 0.01f, false,false),
                    MutationType = OperationType.Add
                }
            });

        _squiresKeystone.Initialize(CareerID, "Knightly Strike lasts +1s longer per 50 Riding, and grants +10% personal 'Physical' melee damage.", "Squires", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "KnightlyStrike",
                    PropertyName = "Duration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Riding }, 0.02f),
                    MutationType = OperationType.Add
                }
            }, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));

        _templarOrdersKeystone.Initialize(CareerID, "Knightly Strike also scales with Faith, and gains +1 charge.", "TemplarOrders", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "KnightlyStrike",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.01f),
                    MutationType = OperationType.Add
                },
            });

        _pathOfVigilanceKeystone.Initialize(CareerID, "+20% personal attack speed during Knightly Strike, and it charges twice as fast.", "PathOfVigilance", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            });

        _wrathAgainstChaosKeystone.Initialize(CareerID, "Knightly Strike gains +25% 'Armour Penetration'.", "WrathAgainstChaos", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect());

        _pathOfGloryKeystone.Initialize(CareerID, "Your patron deity blesses your weapon. If no patron, +20% personal 'Physical' weapon damage.", "PathOfGlory", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {

            }, new CareerChoiceObject.PassiveEffect());

    }

    protected override void InitializePassives()
    {
        _secularOrdersPassive1.Initialize(CareerID, "-25% 'Prestige' cost for upgrading 'Knight' troops.", "SecularOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true,
            characterObject => characterObject.HasAttribute("Knightly")));
        _secularOrdersPassive2.Initialize(CareerID, "+20 One/Two-handed skill for all 'Knight' troops.", "SecularOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, new List<string>() { nameof(DefaultSkills.TwoHanded), nameof(DefaultSkills.OneHanded) }, characterObject =>
            characterObject.HasAttribute("Knightly")));

        _secularOrdersPassive3.Initialize(CareerID, "-25% 'Gold' cost for upgrading 'Knight' troops.", "SecularOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true,
            characterObject => characterObject.HasAttribute("Knightly")));
        _secularOrdersPassive4.Initialize(CareerID, "Can apply 'Secular Seals' to 'Templar Knights' and 'Templar Seals' to 'Secular Knights'.", "SecularOrders", false, ChoiceType.Passive);

        _pathOfConquestPassive1.Initialize(CareerID, "+5% personal 'Physical' melee damage.", "PathOfConquest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee));
        _pathOfConquestPassive2.Initialize(CareerID, "+1 party move speed on campaign map.", "PathOfConquest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
        _pathOfConquestPassive3.Initialize(CareerID, "+40% charge damage bonus for your personal mount.", "PathOfConquest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.HorseChargeDamage, true));
        _pathOfConquestPassive4.Initialize(CareerID, "+30 Polearm weapon skill for 'Knight' troops.", "PathOfConquest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, nameof(DefaultSkills.Polearm),
            (characterObject) => characterObject.HasAttribute("Knightly")));

        _squiresPassive1.Initialize(CareerID, "+15 personal Hitpoints.", "Squires", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _squiresPassive2.Initialize(CareerID, "-25% wages for 'Knight' troops.", "Squires", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages, true,
            characterObject => characterObject.HasAttribute("Knightly")));
        _squiresPassive3.Initialize(CareerID, "Wounded troops heal faster.", "Squires", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
        _squiresPassive4.Initialize(CareerID, "Victories against 'Non-Human' enemies give +100% 'Prestige'.", "Squires", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special));

        _templarOrdersPassive1.Initialize(CareerID, "+15 personal Hitpoints.", "TemplarOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _templarOrdersPassive2.Initialize(CareerID, "Gain Faith experience for slaying the forces of 'Undead' or 'Chaos'.", "TemplarOrders", false, ChoiceType.Passive);
        _templarOrdersPassive3.Initialize(CareerID, "'Knight' troops that follow your patron deity gain +15% 'Physical' melee damage.", "TemplarOrders", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
                (attacker, victim, mask) => attacker.Character.HasAttribute("Knightly") && attacker.BelongsToMainParty() && mask == AttackTypeMask.Melee && Hero.MainHero.HasAnyReligion() && Hero.MainHero.GetDominantReligion().ReligiousTroops.Contains((CharacterObject)attacker.Character)));
        _templarOrdersPassive4.Initialize(CareerID, "+20% personal 'Physical' melee damage against the forces of 'Undead'.", "TemplarOrders", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.All,
                (attacker, victim, mask) => victim.IsUndead() && attacker.IsMainAgent && mask == AttackTypeMask.Melee));

        _pathOfVigilancePassive1.Initialize(CareerID, "+50% personal mount Hitpoints.", "PathOfVigilance", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true));
        _pathOfVigilancePassive2.Initialize(CareerID, "+6% personal 'Physical Resistance'.", "PathOfVigilance", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 6), AttackTypeMask.Ranged | AttackTypeMask.Melee));
        _pathOfVigilancePassive3.Initialize(CareerID, "Hits below 15 damage no longer stagger you.", "PathOfVigilance", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));
        _pathOfVigilancePassive4.Initialize(CareerID, "+15% personal 'Ward Save' when using a shield.", "PathOfVigilance", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All,
                (attacker, victim, mask) => victim.IsMainAgent && victim.WieldedOffhandWeapon.IsShield()));

        _wrathAgainstChaosPassive1.Initialize(CareerID, "Your party deals increased 'Holy' damage when facing the forces of 'Chaos'.", "WrathAgainstChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 15), AttackTypeMask.All,
            (attacker, victim, mask) => victim.Character.Race != 0));
        _wrathAgainstChaosPassive2.Initialize(CareerID, "+15% personal 'Magic Resistance'.", "WrathAgainstChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
        _wrathAgainstChaosPassive3.Initialize(CareerID, "+10% personal weapon swing speed.", "WrathAgainstChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SwingSpeed, true));
        _wrathAgainstChaosPassive4.Initialize(CareerID, "+10% personal 'Armour Penetration' of melee attacks.", "WrathAgainstChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));

        _pathOfGloryPassive1.Initialize(CareerID, "+15 personal Hitpoints.", "PathOfGlory", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _pathOfGloryPassive2.Initialize(CareerID, "+10% 'Ward Save' for all 'Knight' troops.", "PathOfGlory", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All,
            (attacker, victim, mask) => victim.BelongsToMainParty() && !victim.IsHero && victim.Character.IsKnightUnit()));
        _pathOfGloryPassive3.Initialize(CareerID, "+6% personal 'Ward Save'.", "PathOfGlory", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 6), AttackTypeMask.All));
        _pathOfGloryPassive4.Initialize(CareerID, "'Knight' troops can now be given an additional 'Templar or Secular seal'.", "PathOfGlory", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special));


    }


}