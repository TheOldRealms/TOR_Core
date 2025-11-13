using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class SpellsingerCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _spellSingerRoot;

    private CareerChoiceObject _pathShapingKeystone;
    private CareerChoiceObject _pathShapingPassive1;
    private CareerChoiceObject _pathShapingPassive2;
    private CareerChoiceObject _pathShapingPassive3;
    private CareerChoiceObject _pathShapingPassive4;

    private CareerChoiceObject _treeSingingKeystone;
    private CareerChoiceObject _treeSingingPassive1;
    private CareerChoiceObject _treeSingingPassive2;
    private CareerChoiceObject _treeSingingPassive3;
    private CareerChoiceObject _treeSingingPassive4;

    private CareerChoiceObject _vitalSurgeKeystone;
    private CareerChoiceObject _vitalSurgePassive1;
    private CareerChoiceObject _vitalSurgePassive2;
    private CareerChoiceObject _vitalSurgePassive3;
    private CareerChoiceObject _vitalSurgePassive4;

    private CareerChoiceObject _heartOfTheTreeKeystone;
    private CareerChoiceObject _heartOfTheTreePassive1;
    private CareerChoiceObject _heartOfTheTreePassive2;
    private CareerChoiceObject _heartOfTheTreePassive3;
    private CareerChoiceObject _heartOfTheTreePassive4;

    private CareerChoiceObject _arielsBlessingKeystone;
    private CareerChoiceObject _arielsBlessingPassive1;
    private CareerChoiceObject _arielsBlessingPassive2;
    private CareerChoiceObject _arielsBlessingPassive3;
    private CareerChoiceObject _arielsBlessingPassive4;

    private CareerChoiceObject _magicOfAthelLorenKeystone;
    private CareerChoiceObject _magicOfAthelLorenPassive1;
    private CareerChoiceObject _magicOfAthelLorenPassive2;
    private CareerChoiceObject _magicOfAthelLorenPassive3;
    private CareerChoiceObject _magicOfAthelLorenPassive4;

    private CareerChoiceObject _furyOfTheForestKeystone;
    private CareerChoiceObject _furyOfTheForestPassive1;
    private CareerChoiceObject _furyOfTheForestPassive2;
    private CareerChoiceObject _furyOfTheForestPassive3;
    private CareerChoiceObject _furyOfTheForestPassive4;


    protected override void RegisterAll()
    {
        _spellSingerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SpellSingerRoot"));

        _pathShapingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingKeystone"));
        _pathShapingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive1"));
        _pathShapingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive2"));
        _pathShapingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive3"));
        _pathShapingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive4"));

        _treeSingingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingKeystone"));
        _treeSingingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive1"));
        _treeSingingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive2"));
        _treeSingingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive3"));
        _treeSingingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive4"));

        _vitalSurgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgeKeystone"));
        _vitalSurgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive1"));
        _vitalSurgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive2"));
        _vitalSurgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive3"));
        _vitalSurgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive4"));

        _heartOfTheTreeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreeKeystone"));
        _heartOfTheTreePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive1"));
        _heartOfTheTreePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive2"));
        _heartOfTheTreePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive3"));
        _heartOfTheTreePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive4"));

        _arielsBlessingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingKeystone"));
        _arielsBlessingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive1"));
        _arielsBlessingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive2"));
        _arielsBlessingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive3"));
        _arielsBlessingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive4"));

        _magicOfAthelLorenKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenKeystone"));
        _magicOfAthelLorenPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive1"));
        _magicOfAthelLorenPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive2"));
        _magicOfAthelLorenPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive3"));
        _magicOfAthelLorenPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive4"));

        _furyOfTheForestKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestKeystone"));
        _furyOfTheForestPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive1"));
        _furyOfTheForestPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive2"));
        _furyOfTheForestPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive3"));
        _furyOfTheForestPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive4"));
    }

    protected override void InitializeKeyStones()
    {
        _spellSingerRoot.Initialize(CareerID, "{=spell_singer_root_str}Calls treespirits of the surouding forests. Every point in Spellcraft increases the chance by 0.05% additional treespirits join the combat.", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WrathOfTheWood",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.SpellCraft}, 0.001f),
                    MutationType = OperationType.Add
                }
            });

        _pathShapingKeystone.Initialize(CareerID, "{=path_shaping_keystone_str}Upon casting, all forest spirits are moving faster for 10 seconds. Ability scales with Scouting", "PathShaping", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WrathOfTheWood",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Scouting}, 0.001f),
                    MutationType = OperationType.Add
                }
            }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

        _treeSingingKeystone.Initialize(CareerID, "{=tree_singing_keystone_str}Charge is increased by 50%. +5 base troops for summoning.", "TreeSinging", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

        _vitalSurgeKeystone.Initialize(CareerID, "{=vital_surge_keystone_str}Every spawned unit heals 1 HP for all heroes. Ability scales with Medicine", "VitalSurge", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WrathOfTheWood",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Medicine}, 0.001f),
                    MutationType = OperationType.Add
                }
            });

        _heartOfTheTreeKeystone.Initialize(CareerID, "{=heart_of_the_tree_keystone_str}Treespirit attacks charge Career Ability. Ability scales with Leadership", "HeartOfTheTree", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WrathOfTheWood",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership}, 0.001f),
                    MutationType = OperationType.Add
                }
            });

        _arielsBlessingKeystone.Initialize(CareerID, "{=ariels_blessing_keystone_str}Ability starts charged. Ability scales with Faith", "ArielsBlessing", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WrathOfTheWood",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith}, 0.001f),
                    MutationType = OperationType.Add
                }
            });

        _magicOfAthelLorenKeystone.Initialize(CareerID, "{=magic_of_athel_loren_keystone_str}for 10 seconds after casting, dryad kills gain 1 Wind.", "MagicOfAthelLoren", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));


        _furyOfTheForestKeystone.Initialize(CareerID, "{=fury_of_the_forest_keystone_str}Call 1 Treeman with ability (or 10 dryads in close quarter Missions)", "FuryOfTheForest", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                MutationTargetType = typeof(AbilityTemplate),
                MutationTargetOriginalId = "WrathOfTheWood",
                PropertyName = "TriggeredEffects",
                PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"summon_treeman"}).ToList(),
                MutationType = OperationType.Replace
            },
            }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));


    }

    protected override void InitializePassives()
    {

        _pathShapingPassive1.Initialize(CareerID, "{=path_shaping_passive1_str}Party movement speed is increased by 1.", "PathShaping", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
        _pathShapingPassive2.Initialize(CareerID, "{=path_shaping_passive2_str}Upkeep for elven units is reduced by 15%.", "PathShaping", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.TroopWages, true, characterObject => characterObject.IsElf()));
        _pathShapingPassive3.Initialize(CareerID, "{=path_shaping_passive3_str}The Spotting range of the party is increased by 20%.", "PathShaping", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
        _pathShapingPassive4.Initialize(CareerID, "{=path_shaping_passive4_str}Gain 15% Ward save.", "PathShaping", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All));

        _treeSingingPassive1.Initialize(CareerID, "{=tree_singing_passive1_str}Increases maximum winds of magic capacities by 10.", "TreeSinging", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
        _treeSingingPassive2.Initialize(CareerID, "{=tree_singing_passive2_str}Gain 20 Harmony daily.", "TreeSinging", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.CustomResourceGain));
        _treeSingingPassive3.Initialize(CareerID, "{=tree_singing_passive3_str}Upkeep for dryads units is reduced by 10%.", "TreeSinging", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.CustomResourceUpkeepModifier, true, characterObject => characterObject.Culture.StringId == TORConstants.Cultures.ASRAI && !characterObject.IsElf()));
        _treeSingingPassive4.Initialize(CareerID, "{=tree_singing_passive4_str}For every known spell increase party size by 3", "TreeSinging", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, false));

        _vitalSurgePassive1.Initialize(CareerID, "{=vital_surge_passive1_str}Increases Hitpoints by 25.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _vitalSurgePassive2.Initialize(CareerID, "{=vital_surge_passive2_str}Increases troop regeneration by 2.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
        _vitalSurgePassive3.Initialize(CareerID, "{=vital_surge_passive3_str}Buffs and healing duration is increased by 25%.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.BuffDuration, true));
        _vitalSurgePassive4.Initialize(CareerID, "{=vital_surge_passive4_str}Player healing rate increased by 4", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.HealthRegeneration));

        _heartOfTheTreePassive1.Initialize(CareerID, "{=heart_of_the_tree_passive1_str}tree spirits deal 15% extra damage.", "HeartOfTheTree", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.Character.Culture.StringId == TORConstants.Cultures.ASRAI && !(attacker.Character as CharacterObject).IsElf()));
        _heartOfTheTreePassive2.Initialize(CareerID, "{=heart_of_the_tree_passive2_str}dryads gain 25% Wardsave.", "HeartOfTheTree", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All,
           (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.StringId == "tor_we_dryad"));
        _heartOfTheTreePassive3.Initialize(CareerID, "{=heart_of_the_tree_passive3_str}Upkeep for tree spirit units is reduced by 15%.", "HeartOfTheTree", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.CustomResourceUpkeepModifier, true, characterObject => characterObject.Culture.StringId == TORConstants.Cultures.ASRAI && !characterObject.IsElf()));
        _heartOfTheTreePassive4.Initialize(CareerID, "{=heart_of_the_tree_passive4_str}Every joining friendly tree spirit unit has a 25% chance to provide 1 wind.", "HeartOfTheTree", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));

        _arielsBlessingPassive1.Initialize(CareerID, "{=ariels_blessing_passive1_str}20% extra magical melee damage.", "ArielsBlessing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 20), AttackTypeMask.Melee));
        _arielsBlessingPassive2.Initialize(CareerID, "{=ariels_blessing_passive2_str}Increases maximum winds of magic capacities by 15.", "ArielsBlessing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
        _arielsBlessingPassive3.Initialize(CareerID, "{=ariels_blessing_passive3_str}Isha's blessing also provides 20 Forest harmony daily", "ArielsBlessing", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special));
        _arielsBlessingPassive4.Initialize(CareerID, "{=ariels_blessing_passive4_str}Spell effect radius is increased by 20%.", "ArielsBlessing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius, true));

        _magicOfAthelLorenPassive1.Initialize(CareerID, "{=magic_of_athel_loren_passive1_str}15% extra magical spell damage.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
        _magicOfAthelLorenPassive2.Initialize(CareerID, "{=magic_of_athel_loren_passive2_str}Increases maximum winds of magic capacities by 15.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
        _magicOfAthelLorenPassive3.Initialize(CareerID, "{=magic_of_athel_loren_passive3_str}tree spirits are not affected by friendly fire.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 100), AttackTypeMask.Spell,
            (attacker, victim, mask) => mask == AttackTypeMask.Spell && attacker.Character.Culture.StringId == TORConstants.Cultures.ASRAI && !(attacker.Character as CharacterObject).IsElf()));
        _magicOfAthelLorenPassive4.Initialize(CareerID, "{=magic_of_athel_loren_passive4_str}Increase hex durations by 35%.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(35f, PassiveEffectType.DebuffDuration, true));


        _furyOfTheForestPassive1.Initialize(CareerID, "{=fury_of_the_forest_passive1_str}A Dark weaver provides 15% extra Maigcal damage to all heroes.", "FuryOfTheForest", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
        _furyOfTheForestPassive2.Initialize(CareerID, "{=fury_of_the_forest_passive2_str}A High weaver provides 25% extra wardsave to all heroes.", "FuryOfTheForest", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _furyOfTheForestPassive3.Initialize(CareerID, "{=fury_of_the_forest_passive3_str}Treeman Upkeep is reduced by 25%", "FuryOfTheForest", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.CustomResourceUpkeepModifier, true, characterObject => characterObject.StringId.Contains("treeman")));
        _furyOfTheForestPassive4.Initialize(CareerID, "{=fury_of_the_forest_passive4_str}35% Spell cooldown reduction.", "FuryOfTheForest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.WindsCooldownReduction, true));

    }
}