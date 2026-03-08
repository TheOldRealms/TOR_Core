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

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class SlayerCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _slayerRoot;

    private CareerChoiceObject _axeOfGrimnirPassive1;
    private CareerChoiceObject _axeOfGrimnirPassive2;
    private CareerChoiceObject _axeOfGrimnirPassive3;
    private CareerChoiceObject _axeOfGrimnirPassive4;
    private CareerChoiceObject _axeOfGrimnirKeystone;

    private CareerChoiceObject _shameOfTheAncestorsPassive1;
    private CareerChoiceObject _shameOfTheAncestorsPassive2;
    private CareerChoiceObject _shameOfTheAncestorsPassive3;
    private CareerChoiceObject _shameOfTheAncestorsPassive4;
    private CareerChoiceObject _shameOfTheAncestorsKeystone;

    private CareerChoiceObject _deadlyDeterminationPassive1;
    private CareerChoiceObject _deadlyDeterminationPassive2;
    private CareerChoiceObject _deadlyDeterminationPassive3;
    private CareerChoiceObject _deadlyDeterminationPassive4;
    private CareerChoiceObject _deadlyDeterminationKeystone;

    private CareerChoiceObject _urkSlayerPassive1;
    private CareerChoiceObject _urkSlayerPassive2;
    private CareerChoiceObject _urkSlayerPassive3;
    private CareerChoiceObject _urkSlayerPassive4;
    private CareerChoiceObject _urkSlayerKeystone;

    private CareerChoiceObject _giantSlayerPassive1;
    private CareerChoiceObject _giantSlayerPassive2;
    private CareerChoiceObject _giantSlayerPassive3;
    private CareerChoiceObject _giantSlayerPassive4;
    private CareerChoiceObject _giantSlayerKeystone;

    private CareerChoiceObject _baneOfChaosPassive1;
    private CareerChoiceObject _baneOfChaosPassive2;
    private CareerChoiceObject _baneOfChaosPassive3;
    private CareerChoiceObject _baneOfChaosPassive4;
    private CareerChoiceObject _baneOfChaosKeystone;

    private CareerChoiceObject _theLastJourneyPassive1;
    private CareerChoiceObject _theLastJourneyPassive2;
    private CareerChoiceObject _theLastJourneyPassive3;
    private CareerChoiceObject _theLastJourneyPassive4;
    private CareerChoiceObject _theLastJourneyKeystone;

    protected override void RegisterAll()
    {
        _slayerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SlayerRoot"));

        _axeOfGrimnirPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_axeOfGrimnirPassive1).UnderscoreFirstCharToUpper()));
        _axeOfGrimnirPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_axeOfGrimnirPassive2).UnderscoreFirstCharToUpper()));
        _axeOfGrimnirPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_axeOfGrimnirPassive3).UnderscoreFirstCharToUpper()));
        _axeOfGrimnirPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_axeOfGrimnirPassive4).UnderscoreFirstCharToUpper()));
        _axeOfGrimnirKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_axeOfGrimnirKeystone).UnderscoreFirstCharToUpper()));

        _shameOfTheAncestorsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shameOfTheAncestorsPassive1).UnderscoreFirstCharToUpper()));
        _shameOfTheAncestorsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shameOfTheAncestorsPassive2).UnderscoreFirstCharToUpper()));
        _shameOfTheAncestorsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shameOfTheAncestorsPassive3).UnderscoreFirstCharToUpper()));
        _shameOfTheAncestorsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shameOfTheAncestorsPassive4).UnderscoreFirstCharToUpper()));
        _shameOfTheAncestorsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shameOfTheAncestorsKeystone).UnderscoreFirstCharToUpper()));

        _deadlyDeterminationPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_deadlyDeterminationPassive1).UnderscoreFirstCharToUpper()));
        _deadlyDeterminationPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_deadlyDeterminationPassive2).UnderscoreFirstCharToUpper()));
        _deadlyDeterminationPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_deadlyDeterminationPassive3).UnderscoreFirstCharToUpper()));
        _deadlyDeterminationPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_deadlyDeterminationPassive4).UnderscoreFirstCharToUpper()));
        _deadlyDeterminationKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_deadlyDeterminationKeystone).UnderscoreFirstCharToUpper()));

        _urkSlayerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_urkSlayerPassive1).UnderscoreFirstCharToUpper()));
        _urkSlayerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_urkSlayerPassive2).UnderscoreFirstCharToUpper()));
        _urkSlayerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_urkSlayerPassive3).UnderscoreFirstCharToUpper()));
        _urkSlayerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_urkSlayerPassive4).UnderscoreFirstCharToUpper()));
        _urkSlayerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_urkSlayerKeystone).UnderscoreFirstCharToUpper()));

        _giantSlayerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giantSlayerPassive1).UnderscoreFirstCharToUpper()));
        _giantSlayerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giantSlayerPassive2).UnderscoreFirstCharToUpper()));
        _giantSlayerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giantSlayerPassive3).UnderscoreFirstCharToUpper()));
        _giantSlayerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giantSlayerPassive4).UnderscoreFirstCharToUpper()));
        _giantSlayerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giantSlayerKeystone).UnderscoreFirstCharToUpper()));

        _baneOfChaosPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_baneOfChaosPassive1).UnderscoreFirstCharToUpper()));
        _baneOfChaosPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_baneOfChaosPassive2).UnderscoreFirstCharToUpper()));
        _baneOfChaosPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_baneOfChaosPassive3).UnderscoreFirstCharToUpper()));
        _baneOfChaosPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_baneOfChaosPassive4).UnderscoreFirstCharToUpper()));
        _baneOfChaosKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_baneOfChaosKeystone).UnderscoreFirstCharToUpper()));

        _theLastJourneyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_theLastJourneyPassive1).UnderscoreFirstCharToUpper()));
        _theLastJourneyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_theLastJourneyPassive2).UnderscoreFirstCharToUpper()));
        _theLastJourneyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_theLastJourneyPassive3).UnderscoreFirstCharToUpper()));
        _theLastJourneyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_theLastJourneyPassive4).UnderscoreFirstCharToUpper()));
        _theLastJourneyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_theLastJourneyKeystone).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _slayerRoot.Initialize(CareerID, "For a limited time, the Slayer has a increased speed bonus and weapon speed bonus. The kills gained  during the ability, add a doom seeker stack which  permanently increasing the damage of the player. For every Keystone, the damage dealt bonus is increased by 0.05% \\n \\n The ability requires 500 damage to be charged. Every kill adds 100 additional charge. For every keystone(5th perk) you require 10% more damage.", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_impenetrable",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Athletics },  0.02f),
                    MutationType = OperationType.Add
                }
            });

        _axeOfGrimnirKeystone.Initialize(CareerID, "Faith increases duration.", "AxeOfGrimnir", false,
        ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "apply_doom_seeking",
                PropertyName = "ImbuedStatusEffectDuration",
                PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith },  0.02f),
                MutationType = OperationType.Add
            },
        });

        _shameOfTheAncestorsKeystone.Initialize(CareerID, "Losing a slayer units adds a Doom Seeker stack", "ShameOfTheAncestors", false,
        ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
        });

        _deadlyDeterminationKeystone.Initialize(CareerID, "One handed scales ability.", "DeadlyDetermination", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_doom_seeking",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded }, 0.02f),
                    MutationType = OperationType.Add
                }
            });

        _urkSlayerKeystone.Initialize(CareerID, "You deal 50% extra damage during ability", "UrkSlayer", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_doom_seeking",
                    PropertyName = "ImbuedStatusEffects",
                    PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "doom_seeking_dmg" }).ToList(),
                    MutationType = OperationType.Replace
                }
            });

        _giantSlayerKeystone.Initialize(CareerID, "Ability starts charged. Scales with Two handed weapons", "GiantSlayer", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(TriggeredEffectTemplate),
                    MutationTargetOriginalId = "apply_doom_seeking",
                    PropertyName = "ImbuedStatusEffectDuration",
                    PropertyValue = (choice, originalValue, agent) => 0.2f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.TwoHanded }, 0.02f),
                    MutationType = OperationType.Add
                }
            }); //special

        _baneOfChaosKeystone.Initialize(CareerID, "You gain 0.1% physical resistance for every kill during ability.", "BaneOfChaos", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            });

        _theLastJourneyKeystone.Initialize(CareerID, "For every taken hit during  your ability, physical damage is increased by 0.5% for the next 5 seconds", "LastJourney", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            });

    }

    protected override void InitializePassives()
    {
        _axeOfGrimnirPassive1.Initialize(CareerID, "Increases Hitpoints by 10.", "AxeOfGrimnir", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
        _axeOfGrimnirPassive2.Initialize(CareerID, "10% extra melee damage.", "AxeOfGrimnir", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
        _axeOfGrimnirPassive3.Initialize(CareerID, "Every melee kill adds faith", "AxeOfGrimnir", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _axeOfGrimnirPassive4.Initialize(CareerID, "Praying at shrines grants double the amount of religious troops.", "AxeOfGrimnir", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

        _shameOfTheAncestorsPassive1.Initialize(CareerID, "Increases Hitpoints by 10.", "ShameOfTheAncestors", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
        _shameOfTheAncestorsPassive2.Initialize(CareerID, "Party movement speed is increased by 1.", "ShameOfTheAncestors", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
        _shameOfTheAncestorsPassive3.Initialize(CareerID, "Player healing rate increased by 1.5", "ShameOfTheAncestors", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.5f, PassiveEffectType.HealthRegeneration));
        _shameOfTheAncestorsPassive4.Initialize(CareerID, "All non slayer units can be converted to slayers providing extra oath gold.", "ShameOfTheAncestors", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

        _deadlyDeterminationPassive1.Initialize(CareerID, "Increases Hitpoints by 15.", "DeadlyDetermination", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _deadlyDeterminationPassive2.Initialize(CareerID, "Slayer units gain 50 extra one handed-melee skill.", "DeadlyDetermination", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, new List<string>() { nameof(DefaultSkills.OneHanded) }, characterObject => !characterObject.IsHero && characterObject.StringId.Contains("slayer")));
        _deadlyDeterminationPassive3.Initialize(CareerID, "Weapon swing speed increased by 15%.", "DeadlyDetermination", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed, true));
        _deadlyDeterminationPassive4.Initialize(CareerID, "Adds 10% extra base damage after losing 50 healthpoints and armor weight below 9.", "DeadlyDetermination", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(attacker, 9f) && CareerChoicesHelper.HealthLostCheck(attacker, 50)));

        _urkSlayerPassive1.Initialize(CareerID, "15% movement speed", "UrkSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.MovementSpeed, true));
        _urkSlayerPassive2.Initialize(CareerID, "15% extra melee damage against greenskins if armor weight below 9.", "UrkSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(attacker, 9f) && (victim.Character as CharacterObject).IsGreenskin()));
        _urkSlayerPassive3.Initialize(CareerID, "Increases Range resistance by 40%.", "UrkSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 40), AttackTypeMask.Ranged));
        _urkSlayerPassive4.Initialize(CareerID, "Attacks deal double damage vs shields", "UrkSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(200, PassiveEffectType.BonusDamageShield, true));

        _giantSlayerPassive1.Initialize(CareerID, "Increases Hitpoints by 15.", "GiantSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _giantSlayerPassive2.Initialize(CareerID, "25% extra melee damage against mounted enemies and monsters if armor weight below 9.", "GiantSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 25), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(attacker, 9f) && (victim.Character as CharacterObject).IsLargeTarget()));
        _giantSlayerPassive3.Initialize(CareerID, "Slayer units gain 50 extra two-handed  skill.", "GiantSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, new List<string>() { nameof(DefaultSkills.TwoHanded) }, characterObject => !characterObject.IsHero && characterObject.StringId.Contains("slayer")));
        _giantSlayerPassive4.Initialize(CareerID, "Unable to be staggered by any damage.", "GiantSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.ShruggedOff));

        _baneOfChaosPassive1.Initialize(CareerID, "20% spell damage resistance if armor weight below 9.", "BaneOfChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 20), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Spell && victim.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(victim, 9)));

        _baneOfChaosPassive2.Initialize(CareerID, "Player healing rate increased by 2.", "BaneOfChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.HealthRegeneration));
        _baneOfChaosPassive3.Initialize(CareerID, "Extra 25% armor penetration of melee attacks.", "BaneOfChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        _baneOfChaosPassive4.Initialize(CareerID, "20% bonus damage against chaos and beastmen if armor weight below 9.", "BaneOfChaos", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(attacker, 9f) && (victim.Character.Culture.StringId == TORConstants.Cultures.CHAOS || victim.Character.Culture.StringId == TORConstants.Cultures.BEASTMEN || (victim.Character as CharacterObject).IsBeastman())));

        _theLastJourneyPassive1.Initialize(CareerID, "Increases Hitpoints by 15.", "LastJourney", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _theLastJourneyPassive2.Initialize(CareerID, "25% physical melee damage resistance if weight undershoots 9 stones", "LastJourney", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 25), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && victim.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(victim, 9)));
        _theLastJourneyPassive3.Initialize(CareerID, "15% physical melee damage resistance for slayers", "LastJourney", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee,
            (attacker, victim, mask) => mask == AttackTypeMask.Melee && !victim.IsHero && victim.Character.StringId.Contains("slayer")));
        _theLastJourneyPassive4.Initialize(CareerID, "40% Range resistance for slayer units and heroes.", "LastJourney", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 40), AttackTypeMask.Ranged,
            (attacker, victim, mask) => mask == AttackTypeMask.Ranged && !victim.IsHero && victim.Character.StringId.Contains("slayer")));

    }
}