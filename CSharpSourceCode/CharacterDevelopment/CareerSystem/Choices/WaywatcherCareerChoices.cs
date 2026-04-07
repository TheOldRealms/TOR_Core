using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
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
            "Soon, the Lumberfoots shall regret their trespass! Fire a Lethal Shot, with deadly precision! +50% 'Physical' damage to your next shot. Every 'Keystone' career perk unlocked increases arrows affected by Lethal Shot by +1, but decreases its recharge rate. (Ability is charged by dealing 'Physical' damage with bows.)",
            null, true, ChoiceType.Keystone, []);

        _protectorOfTheWoodsKeystone.Initialize(CareerID,
            "Lethal Shot can be used on battle start, reduces required damage to use, and affects +1 shot.",
            "ProtectorOfTheWoods", false, ChoiceType.Keystone, []);

        _pathfinderKeystone.Initialize(CareerID,
            "Lethal Shot applies 'Hagbane' poison that slows enemies. Melee attacks also charge ability.",
            "Pathfinder", false, ChoiceType.Keystone, []);

        _forestStalkerKeystone.Initialize(CareerID,
            "+50% 'Physical' damage to Lethal Shot when in stealth. Troop damage also charge ability.",
            "ForestStalker", false, ChoiceType.Keystone, []);

        _hailOfArrowsKeystone.Initialize(CareerID,
            "Lethal Shot splits into 5 arrows, and gives +25% reload speed.",
            "HailOfArrows", false, ChoiceType.Keystone, []);

        _hawkeyedKeystone.Initialize(CareerID,
            "Lethal Shot pierces through enemies, gains +20% missile speed, and +15% 'Magic' damage.",
            "Hawkeyed", false, ChoiceType.Keystone, []);

        _starfireEssenceKeystone.Initialize(CareerID,
            "Lethal Shot pierces shields and gains +25 'Armour Penetration'.",
            "StarfireEssence", false, ChoiceType.Keystone, []);

        _eyeOfTheHunterKeystone.Initialize(CareerID,
            "-2 arrows affected by Lethal Shot. Remaining arrows become imbued with 'Moonfire Explosion'.",
            "EyeOfTheHunter", false, ChoiceType.Keystone, []);
    }

    protected override void InitializePassives()
    {
        _protectorOfTheWoodsPassive1.Initialize(CareerID, "+10% personal ranged 'Physical' damage.", "ProtectorOfTheWoods", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10),
                AttackTypeMask.Ranged));
        _protectorOfTheWoodsPassive2.Initialize(CareerID, "+3 arrows per quiver you have equipped.", "ProtectorOfTheWoods", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Ammo));
        _protectorOfTheWoodsPassive3.Initialize(CareerID, "-20% gold wages for 'Ranged' troops.", "ProtectorOfTheWoods", false,
            ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.TroopWages, true,
                characterObject => !characterObject.IsHero && characterObject.IsRanged));
        _protectorOfTheWoodsPassive4.Initialize(CareerID, "-15% personal accuracy penalty when moving.", "ProtectorOfTheWoods", false,
            ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.RangedMovementPenalty, true));

        _pathfinderPassive1.Initialize(CareerID, "+20% spotting range of party on campaign map.", "Pathfinder",
            false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartySpottingRange, true));
        _pathfinderPassive2.Initialize(CareerID, "+1 party move speed on campaign map.", "Pathfinder", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
        _pathfinderPassive3.Initialize(CareerID, "Party is no longer affected by snow movement penalties.", "Pathfinder", false, ChoiceType.Passive);
        _pathfinderPassive4.Initialize(CareerID, "Once per day, go out on a hunt.", "Pathfinder", false, ChoiceType.Passive);

        _forestStalkerPassive1.Initialize(CareerID, "Can now perform 'Stealth' attacks with bows.", "ForestStalker", false, ChoiceType.Passive);
        _forestStalkerPassive2.Initialize(CareerID, "+20% personal 'Physical' ranged resistance.", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 20),
                AttackTypeMask.Ranged));
        _forestStalkerPassive3.Initialize(CareerID, "{EFFECT_VALUE}% increased Stealth Bonus", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.StealthBonus, true));
        _forestStalkerPassive4.Initialize(CareerID, "-20% weight of equipped gear.", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));

        _hailOfArrowsPassive1.Initialize(CareerID, "+6 arrows per quiver you have equipped.", "HailOfArrows", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(6, PassiveEffectType.Ammo));
        _hailOfArrowsPassive2.Initialize(CareerID, "+25 experience daily for 'Ranged' troops.", "HailOfArrows", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _hailOfArrowsPassive3.Initialize(CareerID, "You are no longer staggered by ranged damage.", "HailOfArrows", false, ChoiceType.Passive);
        _hailOfArrowsPassive4.Initialize(CareerID, "Troops affected by 'Swiftshiver Shards', gain +25% reload speed.", "HailOfArrows", false,
            ChoiceType.Passive);

        _hawkeyedPassive1.Initialize(CareerID, "-20% weight of equipped gear.", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _hawkeyedPassive2.Initialize(CareerID, "Headshots charge Lethal Shot twice as fast as regular shots.", "Hawkeyed", false, ChoiceType.Passive);
        _hawkeyedPassive3.Initialize(CareerID, "+20% personal ranged accuracy.", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.AccuracyPenalty, true));
        _hawkeyedPassive4.Initialize(CareerID, "+20% personal ranged 'Physical' damage against mounted enemies and monsters.", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Ranged && (victim.Character as CharacterObject).IsLargeTarget()));

        _starfireEssencePassive1.Initialize(CareerID, "+10% personal ranged 'Fire' damage.", "StarfireEssence", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Ranged));
        _starfireEssencePassive2.Initialize(CareerID, "+15% personal swing speed.", "StarfireEssence", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SwingSpeed, true));
        _starfireEssencePassive3.Initialize(CareerID, "Troops equipped with 'Starfire Shafts', applies 'Fire Vulnerability' to enemies.", "StarfireEssence", false, ChoiceType.Passive);
        _starfireEssencePassive4.Initialize(CareerID, "Your normal arrows now penetrate shields.", "StarfireEssence", false, ChoiceType.Passive);

        _eyeOfTheHunterPassive1.Initialize(CareerID, "+3 arrows per quiver you have equipped.", "EyeOfTheHunter", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Ammo));
        _eyeOfTheHunterPassive2.Initialize(CareerID, "-20% weight of equipped gear.", "EyeOfTheHunter", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _eyeOfTheHunterPassive3.Initialize(CareerID, "+50 Bow skill for 'Elven' ranged troops.", "EyeOfTheHunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, nameof(DefaultSkills.Bow),
            (characterObject) => characterObject.IsElf() && characterObject.IsRanged));
        _eyeOfTheHunterPassive4.Initialize(CareerID, "Roguery skill allows your attacks to reduce an enemies armour up to 60%.", "EyeOfTheHunter", false, ChoiceType.Passive);
    }
}