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

public class RunelordCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _runelorddRoot;

    private CareerChoiceObject _forgefireBurningPassive1;
    private CareerChoiceObject _forgefireBurningPassive2;
    private CareerChoiceObject _forgefireBurningPassive3;
    private CareerChoiceObject _forgefireBurningPassive4;
    private CareerChoiceObject _forgefireBurningKeystone;

    private CareerChoiceObject _teachingsOfThungniPassive1;
    private CareerChoiceObject _teachingsOfThungniPassive2;
    private CareerChoiceObject _teachingsOfThungniPassive3;
    private CareerChoiceObject _teachingsOfThungniPassive4;
    private CareerChoiceObject _teachingsOfThungniKeystone;

    private CareerChoiceObject _chiselAndHammerPassive1;
    private CareerChoiceObject _chiselAndHammerPassive2;
    private CareerChoiceObject _chiselAndHammerPassive3;
    private CareerChoiceObject _chiselAndHammerPassive4;
    private CareerChoiceObject _chiselAndHammerKeystone;


    private CareerChoiceObject _forHearthAndHomeKeystone;
    private CareerChoiceObject _forHearthAndHomePassive1;
    private CareerChoiceObject _forHearthAndHomePassive2;
    private CareerChoiceObject _forHearthAndHomePassive3;
    private CareerChoiceObject _forHearthAndHomePassive4;

    private CareerChoiceObject _stoneAndSteelKeystone;
    private CareerChoiceObject _stoneAndSteelPassive1;
    private CareerChoiceObject _stoneAndSteelPassive2;
    private CareerChoiceObject _stoneAndSteelPassive3;
    private CareerChoiceObject _stoneAndSteelPassive4;

    private CareerChoiceObject _legacyOfGrungniKeystone;
    private CareerChoiceObject _legacyOfGrungniPassive1;
    private CareerChoiceObject _legacyOfGrungniPassive2;
    private CareerChoiceObject _legacyOfGrungniPassive3;
    private CareerChoiceObject _legacyOfGrungniPassive4;

    private CareerChoiceObject _anvilOfDoomKeystone;
    private CareerChoiceObject _anvilOfDoomPassive1;
    private CareerChoiceObject _anvilOfDoomPassive2;
    private CareerChoiceObject _anvilOfDoomPassive3;
    private CareerChoiceObject _anvilOfDoomPassive4;


    protected override void RegisterAll()
    {
        _runelorddRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RunelordRoot"));

        _forgefireBurningKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forgefireBurningKeystone).UnderscoreFirstCharToUpper()));
        _forgefireBurningPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forgefireBurningPassive1).UnderscoreFirstCharToUpper()));
        _forgefireBurningPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forgefireBurningPassive2).UnderscoreFirstCharToUpper()));
        _forgefireBurningPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forgefireBurningPassive3).UnderscoreFirstCharToUpper()));
        _forgefireBurningPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forgefireBurningPassive4).UnderscoreFirstCharToUpper()));

        _teachingsOfThungniKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfThungniKeystone).UnderscoreFirstCharToUpper()));
        _teachingsOfThungniPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfThungniPassive1).UnderscoreFirstCharToUpper()));
        _teachingsOfThungniPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfThungniPassive2).UnderscoreFirstCharToUpper()));
        _teachingsOfThungniPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfThungniPassive3).UnderscoreFirstCharToUpper()));
        _teachingsOfThungniPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfThungniPassive4).UnderscoreFirstCharToUpper()));


        _chiselAndHammerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_chiselAndHammerKeystone).UnderscoreFirstCharToUpper()));
        _chiselAndHammerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_chiselAndHammerPassive1).UnderscoreFirstCharToUpper()));
        _chiselAndHammerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_chiselAndHammerPassive2).UnderscoreFirstCharToUpper()));
        _chiselAndHammerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_chiselAndHammerPassive3).UnderscoreFirstCharToUpper()));
        _chiselAndHammerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_chiselAndHammerPassive4).UnderscoreFirstCharToUpper()));

        _forHearthAndHomeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forHearthAndHomeKeystone).UnderscoreFirstCharToUpper()));
        _forHearthAndHomePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forHearthAndHomePassive1).UnderscoreFirstCharToUpper()));
        _forHearthAndHomePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forHearthAndHomePassive2).UnderscoreFirstCharToUpper()));
        _forHearthAndHomePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forHearthAndHomePassive3).UnderscoreFirstCharToUpper()));
        _forHearthAndHomePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forHearthAndHomePassive4).UnderscoreFirstCharToUpper()));

        _stoneAndSteelKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_stoneAndSteelKeystone).UnderscoreFirstCharToUpper()));
        _stoneAndSteelPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_stoneAndSteelPassive1).UnderscoreFirstCharToUpper()));
        _stoneAndSteelPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_stoneAndSteelPassive2).UnderscoreFirstCharToUpper()));
        _stoneAndSteelPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_stoneAndSteelPassive3).UnderscoreFirstCharToUpper()));
        _stoneAndSteelPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_stoneAndSteelPassive4).UnderscoreFirstCharToUpper()));

        _legacyOfGrungniKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_legacyOfGrungniKeystone).UnderscoreFirstCharToUpper()));
        _legacyOfGrungniPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_legacyOfGrungniPassive1).UnderscoreFirstCharToUpper()));
        _legacyOfGrungniPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_legacyOfGrungniPassive2).UnderscoreFirstCharToUpper()));
        _legacyOfGrungniPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_legacyOfGrungniPassive3).UnderscoreFirstCharToUpper()));
        _legacyOfGrungniPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_legacyOfGrungniPassive4).UnderscoreFirstCharToUpper()));

        _anvilOfDoomKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_anvilOfDoomKeystone).UnderscoreFirstCharToUpper()));
        _anvilOfDoomPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_anvilOfDoomPassive1).UnderscoreFirstCharToUpper()));
        _anvilOfDoomPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_anvilOfDoomPassive2).UnderscoreFirstCharToUpper()));
        _anvilOfDoomPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_anvilOfDoomPassive3).UnderscoreFirstCharToUpper()));
        _anvilOfDoomPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_anvilOfDoomPassive4).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _runelorddRoot.Initialize(CareerID, "The runes are cast! By the Wisdom of Thungni, reduce the cooldown of the next 'Rune' ability by 15s, and empower all 'Rune' abilities for 5s. For every level of Smithing, Wisdom of Thungni's cooldown is reduced by 0.1s. (60s cooldown.)", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WisdomOfThungni",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) =>  CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Crafting }, 0.009f),
                    MutationType = OperationType.Add
                },
            });

        _forgefireBurningKeystone.Initialize(CareerID, "Wisdom of Thungi now applies to the next 'Rune' in the sequence at half efficency as well.", "ForgefireBurning", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {

            }, new CareerChoiceObject.PassiveEffect()); //special

        _teachingsOfThungniKeystone.Initialize(CareerID, "Wisdom of Thungi also scales with Faith. Kills made by troops with a 'Rune' reduce its cooldown.", "TeachingsOfThungni", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WisdomOfThungni",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) =>  CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.009f),
                    MutationType = OperationType.Add
                },
            });
        //radius increased by how much? AbilityModel implementation adds 2 different amounts
        _chiselAndHammerKeystone.Initialize(CareerID, "Wisdom of Thungi also scales with Spellcraft. Radius of 'Rune' abilities is increased.", "ChiselAndHammer", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WisdomOfThungni",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) =>  CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Spellcraft }, 0.009f),
                    MutationType = OperationType.Add
                },

            }); //special

        _forHearthAndHomeKeystone.Initialize(CareerID, "The 'Rune' Heart and Home also provides +25% 'Physical' ranged damage.", "ForHearthAndHome", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            });

        _stoneAndSteelKeystone.Initialize(CareerID, "The 'Rune' Spellbreaker drains +50% more 'Winds of Magic'.", "StoneAndSteel", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WisdomOfThungni",
                    PropertyName = "CoolDown",
                    PropertyValue = (choice, originalValue, agent) => -((int)originalValue * 0.35f),
                    MutationType = OperationType.Add
                }
            });

        _legacyOfGrungniKeystone.Initialize(CareerID,
            "The 'Rune' Oath and Steel applies 'Fire' damage to troops.",
            "LegacyOfGrungni", false, ChoiceType.Keystone);

        _anvilOfDoomKeystone.Initialize(CareerID, "The 'Rune' Wrath and Ruin's damage increased by 0.05% per point in Spellcraft.", "AnvilOfDoom", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect());

    }

    protected override void InitializePassives()
    {
        _forgefireBurningPassive1.Initialize(CareerID, "+1 charcoal from burning wood.", "ForgefireBurning", false, ChoiceType.Passive, null, null);
        _forgefireBurningPassive2.Initialize(CareerID, "+1 ingot when forging metal.", "ForgefireBurning", false, ChoiceType.Passive, null, null);
        _forgefireBurningPassive3.Initialize(CareerID, "-40% stamina cost for smithing. Stamina now regenerates while traveling.", "ForgefireBurning", false, ChoiceType.Passive, null, null);
        _forgefireBurningPassive4.Initialize(CareerID, "Gain Faith experience when smelting/smithing.", "ForgefireBurning", false, ChoiceType.Passive, null, null);

        _teachingsOfThungniPassive1.Initialize(CareerID, "-25% cost for 'Enchantments'.", "TeachingsOfThungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.EnchantmentCostReduction, true));
        _teachingsOfThungniPassive2.Initialize(CareerID, "Crafting an equipment 'Rune' provides Smithing/Spellcraft experience.", "TeachingsOfThungni", false, ChoiceType.Passive, null, null);
        _teachingsOfThungniPassive3.Initialize(CareerID, "+10% 'Ward Save' for troops affected by a 'Rune'.", "TeachingsOfThungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.Spell,
            (attacker, victim, mask) => victim.BelongsToMainParty() && !victim.IsHero && victim.Character.HasUnitRune()));
        _teachingsOfThungniPassive4.Initialize(CareerID, "+25% party carrying capacity.", "TeachingsOfThungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.InventoryCapacity, true));

        _chiselAndHammerPassive1.Initialize(CareerID, "+15% 'Physical' damage for troops affected by a 'Rune'.", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.Character.HasUnitRune() && victim.Character.Race != 0));
        _chiselAndHammerPassive2.Initialize(CareerID, "Kills made with 'Rune' weapons provide Spellcraft experience.", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
        _chiselAndHammerPassive3.Initialize(CareerID, "+20% personal 'Rune' ability affect radius.", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius, true));
        _chiselAndHammerPassive4.Initialize(CareerID, "-25% 'Oathgold' cost to upgrade 'Elite' troops.", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true,
            characterObject => characterObject.IsEliteTroop()));

        _forHearthAndHomePassive1.Initialize(CareerID, "+10% 'Ward Save' for troops affected by a 'Rune'.", "ForHearthAndHome", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.Spell,
            (attacker, victim, mask) => !victim.BelongsToMainParty() && victim.IsHero && victim.GetHero().CharacterObject.IsRunesmith()));
        _forHearthAndHomePassive2.Initialize(CareerID, "+3 Hitpoints for every equipment 'Rune' to the wearer.", "ForHearthAndHome", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Special));
        _forHearthAndHomePassive3.Initialize(CareerID, "For every troop affected by a 'Rune', the party gains +0.05 healing.", "ForHearthAndHome", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.05f, PassiveEffectType.Special));
        _forHearthAndHomePassive4.Initialize(CareerID, "+10% duration of 'Rune' abilities. Points in Faith increases duration by 0.1%.", "ForHearthAndHome", false,
            ChoiceType.Passive, new List<CareerChoiceObject.MutationObject>()
            {
            }, null);

        _stoneAndSteelPassive1.Initialize(CareerID, "+10% personal 'Magic Resistance'.", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
        _stoneAndSteelPassive2.Initialize(CareerID, "+10% 'Magic Resistance' for all troops.", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell,
            (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.Culture.StringId == TORConstants.Cultures.DAWI));
        _stoneAndSteelPassive3.Initialize(CareerID, "+25 personal Hitpoints.", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _stoneAndSteelPassive4.Initialize(CareerID, "When a 'Rune' ability is refreshed, gain +50% 'Magic' damage for 15s.", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15));

        _legacyOfGrungniPassive1.Initialize(CareerID, "Runesmith Guild provides more 'Oathgold' from delivered steel.", "LegacyOfGrungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _legacyOfGrungniPassive2.Initialize(CareerID, "Ironsmelters within Karaks provide +2 'Oathgold' daily.", "LegacyOfGrungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _legacyOfGrungniPassive3.Initialize(CareerID, "-33% cost to apply a 'Rune' to troops.", "LegacyOfGrungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _legacyOfGrungniPassive4.Initialize(CareerID, "Smithing levels increase personal range/radius of 'Rune' abilities by 0.005%.", "LegacyOfGrungni", false,
            ChoiceType.Passive, new List<CareerChoiceObject.MutationObject>()
            {
            }, null);

        _anvilOfDoomPassive1.Initialize(CareerID, "+15% personal 'Magic' damage.", "AnvilOfDoom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
        _anvilOfDoomPassive2.Initialize(CareerID, "+5 'Oathgold' daily when an 'Anvil of Doom' is present.", "AnvilOfDoom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CustomResourceGain, false, x => x.HeroObject is { PartyBelongedTo: not null } && x.HeroObject.PartyBelongedTo.HasAnvilOfDoom()));
        _anvilOfDoomPassive3.Initialize(CareerID, "-2% cooldown to your runes per Runesmith companion.", "AnvilOfDoom", false, ChoiceType.Passive, null, null);
        _anvilOfDoomPassive4.Initialize(CareerID, "You can now place 2 Unit Runes per unit.", "AnvilOfDoom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special));
    }


    protected override void UnlockCareerBenefitsTier1()
    {

    }

    protected override void UnlockCareerBenefitsTier2()
    {
        Hero.MainHero.AddAttribute("Spellcaster");
    }

    protected override void UnlockCareerBenefitsTier3()
    {

    }
}