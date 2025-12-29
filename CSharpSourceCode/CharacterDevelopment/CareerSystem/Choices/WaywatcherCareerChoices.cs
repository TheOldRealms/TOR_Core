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
            "Lethal Shot empowers your bow with deadly precision. Activating adds +50% physical damage to your arrows for a limited number of shots. Each keystone adds +1 arrow to the ability. For charging ability deal 1200 damage points with bows. Each arrow charges a maximum of 150 points.",
            null, true, ChoiceType.Keystone, []);

        _protectorOfTheWoodsKeystone.Initialize(CareerID,
            "Lethal Shot gains +2 bonus arrows. Reduces the amount of ranged damage needed to unlock ability. Ability starts charged.",
            "ProtectorOfTheWoods", false, ChoiceType.Keystone, []);

        _pathfinderKeystone.Initialize(CareerID,
            "Lethal Shot arrows apply Hagbane poison, slowing enemies on hit. Ability charge scales with Scouting skill.",
            "Pathfinder", false, ChoiceType.Keystone, []);

        _forestStalkerKeystone.Initialize(CareerID,
            "Lethal Shot arrows deal +50% bonus damage to unaware enemies (Loec's Blessing). Allied troops charge ability.",
            "ForestStalker", false, ChoiceType.Keystone, []);

        _hailOfArrowsKeystone.Initialize(CareerID,
            "Lethal Shot arrows split into 5 projectiles on release. Also grants +25% reload speed during the effect.",
            "HailOfArrows", false, ChoiceType.Keystone, []);

        _hawkeyedKeystone.Initialize(CareerID,
            "Lethal Shot arrows can pierce through multiple enemies.",
            "Hawkeyed", false, ChoiceType.Keystone, []);

        _starfireEssenceKeystone.Initialize(CareerID,
            "Lethal Shot arrows gain +25 armor penetration and +20% missile speed.",
            "StarfireEssence", false, ChoiceType.Keystone, []);

        _eyeOfTheHunterKeystone.Initialize(CareerID,
            "Lethal Shot gains +2 bonus arrows. Arrows are imbued with Moonfire: +30% magic damage and explode on impact, applying magic vulnerability to nearby enemies.",
            "EyeOfTheHunter", false, ChoiceType.Keystone, []);
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
            false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartySpottingRange, true));
        _pathfinderPassive2.Initialize(CareerID, "Party movement speed is increased by 1", "Pathfinder", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
        _pathfinderPassive3.Initialize(CareerID, "Party travels unhindered through snow", "Pathfinder", false, ChoiceType.Passive);
        _pathfinderPassive4.Initialize(CareerID, "Once per day, go for a hunt.", "Pathfinder", false, ChoiceType.Passive);

        _forestStalkerPassive1.Initialize(CareerID, "Bows and throwing weapons can perform stealth attacks.", "ForestStalker", false, ChoiceType.Passive);
        _forestStalkerPassive2.Initialize(CareerID, "Gain 20% range resistance.", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 20),
                AttackTypeMask.Ranged));
        _forestStalkerPassive3.Initialize(CareerID, "{EFFECT_VALUE}% increased Stealth Bonus", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.StealthBonus, true));
        _forestStalkerPassive4.Initialize(CareerID, "20% Equipment weight Reduction", "ForestStalker", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));

        _hailOfArrowsPassive1.Initialize(CareerID, "6 extra Arrows per equipped Quiver", "HailOfArrows", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(6, PassiveEffectType.Ammo));
        _hailOfArrowsPassive2.Initialize(CareerID, "Ranged troops gain 25XP daily ", "HailOfArrows", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _hailOfArrowsPassive3.Initialize(CareerID, "Ranged damage is shrugged off", "HailOfArrows", false, ChoiceType.Passive);
        _hailOfArrowsPassive4.Initialize(CareerID, "Troops with Swiftshiver Shards gain 25% reload speed bonus.", "HailOfArrows", false,
            ChoiceType.Passive);

        _hawkeyedPassive1.Initialize(CareerID, "20% Equipment weight Reduction", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _hawkeyedPassive2.Initialize(CareerID, "Headshots double the fill", "Hawkeyed", false, ChoiceType.Passive);
        _hawkeyedPassive3.Initialize(CareerID, "Increased ranged accuracy by 20%", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.AccuracyPenalty, true));
        _hawkeyedPassive4.Initialize(CareerID, "20% extra ranged damage against mounted enemies and monsters", "Hawkeyed", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Ranged && (victim.Character as CharacterObject).IsLargeTarget()));

        _starfireEssencePassive1.Initialize(CareerID, "10% extra fire damage", "StarfireEssence", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Ranged));
        _starfireEssencePassive2.Initialize(CareerID, "15% swing speed", "StarfireEssence", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SwingSpeed, true));
        _starfireEssencePassive3.Initialize(CareerID, "Troops with Starfire Shafts also apply fire vulnerability to enemies.", "StarfireEssence", false, ChoiceType.Passive);
        _starfireEssencePassive4.Initialize(CareerID, "Your arrows can penetrate shields", "StarfireEssence", false, ChoiceType.Passive);

        _eyeOfTheHunterPassive1.Initialize(CareerID, "6 extra Arrows per equipped Quiver", "EyeOfTheHunter", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(6, PassiveEffectType.Ammo));
        _eyeOfTheHunterPassive2.Initialize(CareerID, "20% Equipment weight Reduction", "EyeOfTheHunter", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _eyeOfTheHunterPassive3.Initialize(CareerID, "All elf archer troops gain 50 to their bow skill.", "EyeOfTheHunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, nameof(DefaultSkills.Bow),
            (characterObject) => characterObject.IsElf() && characterObject.IsRanged));
        _eyeOfTheHunterPassive4.Initialize(CareerID, "Roguery skill reduces target armor by up to 60%", "EyeOfTheHunter", false, ChoiceType.Passive);
    }
}