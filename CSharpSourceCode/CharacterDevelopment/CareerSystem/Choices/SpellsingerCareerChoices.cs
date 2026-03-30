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
        _spellSingerRoot.Initialize(CareerID, "The forests are restless! Chant an ancient litany, to bring forth the Wrath of the Wood, summoning Dryads to your aid! For every level of Spellcraft, gain 0.05% chance of an additional Dryad being summoned. (Ability is charged by dealing 'Spell' damage.)", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WrathOfTheWood",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Spellcraft}, 0.001f),
                    MutationType = OperationType.Add
                }
            });

        _pathShapingKeystone.Initialize(CareerID, "Wrath of the Wood also scales with Scouting, and buffs 'Forest Spirits' move speed by +10%.", "PathShaping", false,
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

        _treeSingingKeystone.Initialize(CareerID, "Wrath of the Wood charges twice as fast, and summons +2 more Dryads.", "TreeSinging", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

        _vitalSurgeKeystone.Initialize(CareerID, "Wrath of the Wood also scales with Medicine. Companions heal per summoned Dryad.", "VitalSurge", false,
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

        _heartOfTheTreeKeystone.Initialize(CareerID, "Wrath of the Wood also scales with Leadership, and 'Tree Spirit' troops charge ability.", "HeartOfTheTree", false,
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

        _arielsBlessingKeystone.Initialize(CareerID, "Wrath of the Woods also scales with Faith, and can be cast immediately on battle start.", "ArielsBlessing", false,
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

        _magicOfAthelLorenKeystone.Initialize(CareerID, "Wrath of the Woods Lets kills made by Dryads to recharge 0.5 of your 'Winds of Magic' for 10s.", "MagicOfAthelLoren", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));


        _furyOfTheForestKeystone.Initialize(CareerID, "Wrath of the Woods summons +1 Treeman. 10 Dryads instead if used during a mission.", "FuryOfTheForest", false,
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

        _pathShapingPassive1.Initialize(CareerID, "+1 party move speed on campaign map.", "PathShaping", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
        _pathShapingPassive2.Initialize(CareerID, "-10% gold wages for 'Elven' troops.", "PathShaping", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.TroopWages, true, characterObject => characterObject.IsElf()));
        _pathShapingPassive3.Initialize(CareerID, "+20% spotting range of party on campaign map.", "PathShaping", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartySpottingRange, true));
        _pathShapingPassive4.Initialize(CareerID, "+15% personal 'Ward Save' if armour weight does not exceed 15.", "PathShaping", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All));

        _treeSingingPassive1.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "TreeSinging", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
        _treeSingingPassive2.Initialize(CareerID, "+15 'Harmony' daily.", "TreeSinging", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.CustomResourceGain));
        _treeSingingPassive3.Initialize(CareerID, "-10% 'Harmony' upkeep for Dryads.", "TreeSinging", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.CustomResourceUpkeepModifier, true, characterObject => characterObject.StringId == "tor_we_dryad"));
        _treeSingingPassive4.Initialize(CareerID, "Increase party size by +3 per known spell.", "TreeSinging", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Special, false));

        _vitalSurgePassive1.Initialize(CareerID, "+15 personal Hitpoints.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
        _vitalSurgePassive2.Initialize(CareerID, "Wounded troops heal faster.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.TroopRegeneration));
        _vitalSurgePassive3.Initialize(CareerID, "+20% duration for 'Augment' and 'Healing' spells.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.BuffDuration, true));
        _vitalSurgePassive4.Initialize(CareerID, "Personal healing rate increased by +2.", "VitalSurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.HealthRegeneration));

        _heartOfTheTreePassive1.Initialize(CareerID, "+15% 'Physical' melee damage for 'Tree Spirit' troops.", "HeartOfTheTree", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.Character.Culture.StringId == TORConstants.Cultures.ASRAI && !(attacker.Character as CharacterObject).IsElf()));
        _heartOfTheTreePassive2.Initialize(CareerID, "+15% 'Ward Save' for Dryads.", "HeartOfTheTree", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All,
           (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.StringId == "tor_we_dryad"));
        _heartOfTheTreePassive3.Initialize(CareerID, "-15% 'Harmony' upkeep for 'Tree Spirit' troops.", "HeartOfTheTree", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.CustomResourceUpkeepModifier, true, characterObject => characterObject.Culture.StringId == TORConstants.Cultures.ASRAI && !characterObject.IsElf()));
        _heartOfTheTreePassive4.Initialize(CareerID, "+0.5 personal 'Winds of Magic' capacity, per 'Tree Spirit' troop.", "HeartOfTheTree", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.Special, true));

        _arielsBlessingPassive1.Initialize(CareerID, "+10% personal 'Magic' melee damage.", "ArielsBlessing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee));
        _arielsBlessingPassive2.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "ArielsBlessing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
        _arielsBlessingPassive3.Initialize(CareerID, "+15 'Forest Harmony' daily when under 'Isha's Blessing'.", "ArielsBlessing", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
        _arielsBlessingPassive4.Initialize(CareerID, "+15% personal radius for spells.", "ArielsBlessing", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SpellRadius, true));

        _magicOfAthelLorenPassive1.Initialize(CareerID, "+5% personal 'Spell' damage.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell));
        _magicOfAthelLorenPassive2.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
        _magicOfAthelLorenPassive3.Initialize(CareerID, "'Tree Spirit' troops are not affected by friendly fire.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 100), AttackTypeMask.Spell,
            (attacker, victim, mask) => mask == AttackTypeMask.Spell && attacker.Character.Culture.StringId == TORConstants.Cultures.ASRAI && !(attacker.Character as CharacterObject).IsElf()));
        _magicOfAthelLorenPassive4.Initialize(CareerID, "+20% duration for 'Hex' spells.", "MagicOfAthelLoren", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.DebuffDuration, true));


        _furyOfTheForestPassive1.Initialize(CareerID, "+10% 'Spell' damage for all Companions if one of them is a Darkweaver.", "FuryOfTheForest", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));
        _furyOfTheForestPassive2.Initialize(CareerID, "+15% 'Ward Save' for all Companions if one of them is a Highweaver.", "FuryOfTheForest", false, ChoiceType.Passive,
            null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
        _furyOfTheForestPassive3.Initialize(CareerID, "-20% 'Harmony' upkeep for Treemen.", "FuryOfTheForest", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.CustomResourceUpkeepModifier, true, characterObject => characterObject.StringId.Contains("treeman")));
        _furyOfTheForestPassive4.Initialize(CareerID, "+25% personal spell cooldown reduction.", "FuryOfTheForest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.WindsCooldownReduction, true));

    }
}