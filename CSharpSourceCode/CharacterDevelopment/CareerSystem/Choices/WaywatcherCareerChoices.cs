using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class WaywatcherCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _wayWatcherRoot;
    private CareerChoiceObject _protectorOfTheWoodsKeystone;
    private CareerChoiceObject _protectorOfTheWoodsPassive1;
    private CareerChoiceObject _protectorOfTheWoodsPassive2;
    private CareerChoiceObject _protectorOfTheWoodsPassive3;
    private CareerChoiceObject _protectorOfTheWoodsPassive4;

    private CareerChoiceObject _pathfinderKeystone;
    private CareerChoiceObject _pathfinderPassive1;
    private CareerChoiceObject _pathfinderPassive2;
    private CareerChoiceObject _pathfinderPassive3;
    private CareerChoiceObject _pathfinderPassive4;

    private CareerChoiceObject _forestStalkerKeystone;
    private CareerChoiceObject _forestStalkerPassive1;
    private CareerChoiceObject _forestStalkerPassive2;
    private CareerChoiceObject _forestStalkerPassive3;
    private CareerChoiceObject _forestStalkerPassive4;

    private CareerChoiceObject _hailOfArrowsKeystone;
    private CareerChoiceObject _hailOfArrowsPassive1;
    private CareerChoiceObject _hailOfArrowsPassive2;
    private CareerChoiceObject _hailOfArrowsPassive3;
    private CareerChoiceObject _hailOfArrowsPassive4;

    private CareerChoiceObject _hawkeyedKeystone;
    private CareerChoiceObject _hawkeyedPassive1;
    private CareerChoiceObject _hawkeyedPassive2;
    private CareerChoiceObject _hawkeyedPassive3;
    private CareerChoiceObject _hawkeyedPassive4;

    private CareerChoiceObject _starfireEssenceKeystone;
    private CareerChoiceObject _starfireEssencePassive1;
    private CareerChoiceObject _starfireEssencePassive2;
    private CareerChoiceObject _starfireEssencePassive3;
    private CareerChoiceObject _starfireEssencePassive4;

    private CareerChoiceObject _eyeOfTheHunterKeystone;
    private CareerChoiceObject _eyeOfTheHunterPassive1;
    private CareerChoiceObject _eyeOfTheHunterPassive2;
    private CareerChoiceObject _eyeOfTheHunterPassive3;
    private CareerChoiceObject _eyeOfTheHunterPassive4;

    protected override void RegisterAll()
    {
        _wayWatcherRoot =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wayWatcherRoot).UnderscoreFirstCharToUpper()));

        _protectorOfTheWoodsKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerChoiceObject(nameof(_protectorOfTheWoodsKeystone).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive1).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive2).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive3).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive4).UnderscoreFirstCharToUpper()));

        _pathfinderKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderKeystone).UnderscoreFirstCharToUpper()));
        _pathfinderPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive1).UnderscoreFirstCharToUpper()));
        _pathfinderPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive2).UnderscoreFirstCharToUpper()));
        _pathfinderPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive3).UnderscoreFirstCharToUpper()));
        _pathfinderPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive4).UnderscoreFirstCharToUpper()));

        _forestStalkerKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerKeystone).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive1).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive2).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive3).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive4).UnderscoreFirstCharToUpper()));

        _hailOfArrowsKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsKeystone).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive1).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive2).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive3).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive4).UnderscoreFirstCharToUpper()));

        _hawkeyedKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedKeystone).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive1).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive2).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive3).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive4).UnderscoreFirstCharToUpper()));

        _starfireEssenceKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssenceKeystone).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive1).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive2).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive3).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive4).UnderscoreFirstCharToUpper()));

        _eyeOfTheHunterKeystone =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterKeystone).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive1 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive1).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive2 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive2).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive3 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive3).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive4 =
            Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive4).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _wayWatcherRoot.Initialize(CareerID,
            "Kurnous, Lord of the Hunt, blesses this fatal shot to travel straight through the heart of the enemy. This missile attack will seek out its target and trigger an explosion upon impact. With every skill point in Archery, the radius increases. For charging ability deal 1200 damage points with bows. Each Arrow charges a maximum of 150 points",
            null, true, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "ArrowOfKurnous",
                    PropertyName = "SeekerParameters",
                    PropertyValue = (choice, originalValue, agent) =>
                    {
                        var seeker = new SeekerParameters();
                        seeker.Derivative = 0;
                        seeker.Proportional = 0.5f;
                        seeker.DisableDistance = 2f;
                        return seeker;
                    },
                    MutationType = OperationType.Replace
                },
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "Radius",
                    PropertyValue =
                        (choice, originalValue, agent) =>
                            CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Bow }, 0.01f, true),
                    MutationType = OperationType.Add
                }
            });

        _protectorOfTheWoodsKeystone.Initialize(CareerID, "Reduces the amount of ranged damage to unlock ability. Ability starts charged.",
            "ProtectorOfTheWoods", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>() { }); //special

        _pathfinderKeystone.Initialize(CareerID, "The range of Arrow of Kournous is doubled. Ability scales with Scouting", "Pathfinder", false,
            ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "ArrowOfKurnous",
                    PropertyName = "MaxDistance",
                    PropertyValue = (choice, originalValue, agent) => 2,
                    MutationType = OperationType.Multiply
                },
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "Radius",
                    PropertyValue =
                        (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent,
                            new List<SkillObject>() { DefaultSkills.Scouting }, 0.01f, true),
                    MutationType = OperationType.Add
                }
            });
        _forestStalkerKeystone.Initialize(CareerID, "All enemies suffer 50% more magical damage for 10 seconds. Allied troops charge ability.", "ForestStalker",
            false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue =
                        (choice, originalValue, agent) =>
                            ((List<string>)originalValue).Concat(new[] { "arrow_of_kurnous_debuff_res" }).ToList(),
                    MutationType = OperationType.Replace
                }
            });
        _hailOfArrowsKeystone.Initialize(CareerID, "Every affected enemy increases reload speed for 4 sec. The damage increased by 50%", "HailOfArrows", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "DamageAmount",
                    PropertyValue = (choice, originalValue, agent) => (int) originalValue*0.5f,
                    MutationType = OperationType.Add
                }
            });
        _hawkeyedKeystone.Initialize(CareerID, "All Enemies in the area are slowed on impact. The damage increased by 50%", "Hawkeyed", false,
            ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue =
                        (choice, originalValue, agent) =>
                            ((List<string>)originalValue).Concat(new[] { "arrow_of_kurnous_debuff_mov" }).ToList(),
                    MutationType = OperationType.Replace
                },
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "DamageAmount",
                    PropertyValue = (choice, originalValue, agent) => (int) originalValue*0.5f,
                    MutationType = OperationType.Add
                }
            });
        _starfireEssenceKeystone.Initialize(CareerID, "Enemies suffer from a damage over time effect on impact.", "StarfireEssence", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_arrow_of_kurnous",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue =
                        (choice, originalValue, agent) =>
                            ((List<string>)originalValue).Concat(new[] { "arrow_of_kurnous_debuff_dot" }).ToList(),
                    MutationType = OperationType.Replace
                }
            });

        _eyeOfTheHunterKeystone.Initialize(CareerID, "Arrow of Kurnous loses it's seeking ability. The damage is doubled.", "EyeOfTheHunter", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>() {new()
            {
                MutationTargetType = typeof(AbilityTemplate),
                MutationTargetOriginalId = "ArrowOfKurnous",
                PropertyName = "CrosshairType",
                PropertyValue = (choice, originalValue, agent) => CrosshairType.Missile,
                MutationType = OperationType.Replace
            },
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "apply_arrow_of_kurnous",
                PropertyName = "DamageAmount",
                PropertyValue = (choice, originalValue, agent) => 1,
                MutationType = OperationType.Multiply
            },
            new()
            {
                MutationTargetType = typeof(AbilityTemplate),
                MutationTargetOriginalId = "ArrowOfKurnous",
                PropertyName = "SeekerParameters",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    var seeker = new SeekerParameters();
                    seeker.Derivative = 0;
                    seeker.Proportional = 0.5f;
                    seeker.DisableDistance = 1000f;
                    return seeker;
                },
                MutationType = OperationType.Replace
            },
            }); //special
    }

    protected override void InitializePassives()
    {
        _protectorOfTheWoodsPassive1.Initialize(CareerID, "Extra ranged damage (10%).", "ProtectorOfTheWoods", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10),
                AttackTypeMask.Ranged));
        _protectorOfTheWoodsPassive2.Initialize(CareerID, "3 extra Arrows per equipped Quiver", "ProtectorOfTheWoods", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Ammo));
        _protectorOfTheWoodsPassive3.Initialize(CareerID, "All ranged troops wages are reduced by 20%", "ProtectorOfTheWoods", false,
            ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.TroopWages, true,
                characterObject => !characterObject.IsHero && characterObject.IsRanged));
        _protectorOfTheWoodsPassive4.Initialize(CareerID, "Reduce range Accuracy movement penalty by 15%.", "ProtectorOfTheWoods", false,
            ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.RangedMovementPenalty, true));

        _pathfinderPassive1.Initialize(CareerID, "The Spotting range of the party is increased by 20%.", "Pathfinder",
            false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
        _pathfinderPassive2.Initialize(CareerID, "Party movement speed is increased by 1.", "Pathfinder", false,
            ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
        _pathfinderPassive3.Initialize(CareerID, "Party travels unhindered through snow", "Pathfinder", false, ChoiceType.Passive);
        _pathfinderPassive4.Initialize(CareerID, "Ranged damage is shrugged off", "Pathfinder", false, ChoiceType.Passive);

        _forestStalkerPassive1.Initialize(CareerID, "Once per day, go for a hunt.", "ForestStalker", false, ChoiceType.Passive);
        _forestStalkerPassive2.Initialize(CareerID, "Gain 20% range resistance.", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 20),
                AttackTypeMask.Ranged));
        _forestStalkerPassive3.Initialize(CareerID, "Increases range damage resistance of melee troops by 20%.", "ForestStalker", false,
            ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20),
                AttackTypeMask.Ranged,
                (attacker, victim, mask) => !victim.BelongsToMainParty() && !(victim.IsMainAgent || victim.IsHero) && !victim.IsRangedCached &&
                                            mask == AttackTypeMask.Melee));
        _forestStalkerPassive4.Initialize(CareerID, "20% Equipment weight Reduction", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));

        _hailOfArrowsPassive1.Initialize(CareerID, "6 extra Arrows per equipped Quiver", "HailOfArrows", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(6, PassiveEffectType.Ammo));
        _hailOfArrowsPassive2.Initialize(CareerID, "Ranged troops gain 25XP daily ", "HailOfArrows", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _hailOfArrowsPassive3.Initialize(CareerID, "Attacking unaware enemies adds 50% extra damage", "HailOfArrows", false, ChoiceType.Passive);
        _hailOfArrowsPassive4.Initialize(CareerID, "Special shot: For every hit arrow, your magic damage increase. Bonus slowly decrease.", "HailOfArrows", false,
            ChoiceType.Passive);

        _hawkeyedPassive1.Initialize(CareerID, "20% Equipment weight Reduction", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _hawkeyedPassive2.Initialize(CareerID, "Headshots double the fill", "Hawkeyed", false, ChoiceType.Passive);
        _hawkeyedPassive3.Initialize(CareerID, "Special shot: Every sixth arrow, applies a slow down effect on impact.", "Hawkeyed", false, ChoiceType.Passive);
        _hawkeyedPassive4.Initialize(CareerID, "While Zoomed in, the time is slowed down. Every second you lose 100 Career Charge.", "Hawkeyed", false, ChoiceType.Passive);

        _starfireEssencePassive1.Initialize(CareerID, "6 extra Arrows per equipped Quiver", "StarfireEssence", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(6, PassiveEffectType.Ammo));
        _starfireEssencePassive2.Initialize(CareerID, "15% swing speed", "StarfireEssence", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SwingSpeed, true));
        _starfireEssencePassive3.Initialize(CareerID, "Your arrows can penetrate shields", "StarfireEssence", false, ChoiceType.Passive);
        _starfireEssencePassive4.Initialize(CareerID, "Special shot: Not shooting an arrows increase the chance your next arrow explode on impact.",
            "StarfireEssence", false, ChoiceType.Passive);

        _eyeOfTheHunterPassive1.Initialize(CareerID, "20% Equipment weight Reduction", "EyeOfTheHunter", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _eyeOfTheHunterPassive2.Initialize(CareerID, "Arrows can pierce multiple targets", "EyeOfTheHunter", false, ChoiceType.Passive);
        _eyeOfTheHunterPassive3.Initialize(CareerID, "Your Archer units gain 50 points in bow skill", "EyeOfTheHunter", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(50));
        _eyeOfTheHunterPassive4.Initialize(CareerID, "Special shot efficiency is doubled", "EyeOfTheHunter", false, ChoiceType.Passive);
    }
}