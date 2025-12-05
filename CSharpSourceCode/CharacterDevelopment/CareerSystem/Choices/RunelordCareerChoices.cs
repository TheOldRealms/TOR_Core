using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
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
        _runelorddRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightOldWorldRoot"));

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
        _runelorddRoot.Initialize(CareerID, "{=runelord_root_str}The Runesmih utters ancient incantations passed down by venerable Thungni himself - the Ancestor God of Runecraft.\nReduce the cooldown of the next Rune ability by 15 seconds. Every point of Smithing reduces the cooldown of Wisdom of Thungni by 0.1 seconds.\nIf a Rune ability is available to cast, the next Rune cast in 5 seconds shall be empowered.", null, true,
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

        _forgefireBurningKeystone.Initialize(CareerID, "{=forge_fire_burning_keystone_str}Cooldown of the next ability in line gets reduced by 50% of the current CD.", "ForgefireBurning", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {

            }, new CareerChoiceObject.PassiveEffect()); //special

        _teachingsOfThungniKeystone.Initialize(CareerID, "{=path_of_conquest_keystone_str}Faith counts towards career ability. Units with runes can charge career ability.", "TeachingsOfThungni", false,
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
        _chiselAndHammerKeystone.Initialize(CareerID, "{=chiselAndHammer_keystone_str}Spellcraft counts towards career ability. the radius of all abilities is increased", "ChiselAndHammer", false,
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

        _forHearthAndHomeKeystone.Initialize(CareerID, "{=for_hearth_and_home_keystone_str}Heart and Home also adds now 25% extra ranged damage", "ForHearthAndHome", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            });

        _stoneAndSteelKeystone.Initialize(CareerID, "{=stone_and_steel_keystone_str}The Spellbreaker rune now drains 50% more Winds of Magic", "StoneAndSteel", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "WisdomOfThungni",
                    PropertyName = "CoolDown",
                    PropertyValue = (choice, originalValue, agent) => -(float)originalValue*0.35f,
                    MutationType = OperationType.Add
                }
            });

        _legacyOfGrungniKeystone.Initialize(CareerID,
            "{=legacy_of_grungni_keystone_str}The Rune of Oath and Steel also increases fire damage of troops",
            "LegacyOfGrungni", false, ChoiceType.Keystone);

        _anvilOfDoomKeystone.Initialize(CareerID, "{=path_of_glory_keystone_str}Every point in spellcraft increase the damage of Wrath and Ruin by 0.05%", "AnvilOfDoom", false,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect());

    }

    protected override void InitializePassives()
    {
        _forgefireBurningPassive1.Initialize(CareerID, "{=forge_fire_Burning_passive1_str}Gain 1 additional Charcoal from burning wood", "ForgefireBurning", false, ChoiceType.Passive, null, null);
        _forgefireBurningPassive2.Initialize(CareerID, "{=forge_fire_Burning_passive2_str}Gain a second Ingot from forging metal.", "ForgefireBurning", false, ChoiceType.Passive, null, null);
        _forgefireBurningPassive3.Initialize(CareerID, "{=forge_fire_Burning_passive3_str}Stamina reduction 40% for smithing. Regenerate stamina while traveling", "ForgefireBurning", false, ChoiceType.Passive, null, null);
        _forgefireBurningPassive4.Initialize(CareerID, "{=forge_fire_Burning_passive4_str}smithing and smelting also increases faith for the smelter", "ForgefireBurning", false, ChoiceType.Passive, null, null);

        _teachingsOfThungniPassive1.Initialize(CareerID, "{=teachings_Of_thungni_passive1}Reduce costs for enchantments by 25%", "TeachingsOfThungni", false, ChoiceType.Passive, null, null);
        _teachingsOfThungniPassive2.Initialize(CareerID, "{=teachings_Of_thungni_passive2}Crafting Equipment runes also adds smithing and spellcraft skill.", "TeachingsOfThungni", false, ChoiceType.Passive, null, null);
        _teachingsOfThungniPassive3.Initialize(CareerID, "{=teachings_Of_thungni_passive3}10% Wardsave for Unit wearing runes.", "TeachingsOfThungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.Spell,
            (attacker, victim, mask) => victim.BelongsToMainParty() && !victim.IsHero && victim.Character.HasUnitRune()));
        _teachingsOfThungniPassive4.Initialize(CareerID, "{=teachings_Of_thungni_passive4}Increase carrying capacity by 25%", "TeachingsOfThungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.InventoryCapacity, true));

        _chiselAndHammerPassive1.Initialize(CareerID, "{=wrath_against_chaos_passive1_str}15% extra damage for rune enchanted troops", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 15), AttackTypeMask.All,
            (attacker, victim, mask) => attacker.Character.HasUnitRune() && victim.Character.Race != 0));
        _chiselAndHammerPassive2.Initialize(CareerID, "{=chiselAndHammer_passive2_str}Eliminations with magical weapons give Spellcraft exp", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
        _chiselAndHammerPassive3.Initialize(CareerID, "{=chiselAndHammer_passive3_str}Spell effect radius is increased by 20%.", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius, true));
        _chiselAndHammerPassive4.Initialize(CareerID, "{=secular_orders_passive3_str}Oathgold upgrade costs for Elite Units is reduced", "ChiselAndHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true,
            characterObject => characterObject.HasAttribute("Knightly")));

        _forHearthAndHomePassive1.Initialize(CareerID, "{=path_of_glory_passive2_str}10% Wardsave for Unit wearing runes.", "ForHearthAndHome", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.Spell,
            (attacker, victim, mask) => !victim.BelongsToMainParty() && victim.IsHero && victim.GetHero().CharacterObject.IsRunesmith()));
        _forHearthAndHomePassive2.Initialize(CareerID, "{=for_hearth_and_home_passive2_str}For every magic rune equipped your and companion rune HP increased by 3.", "ForHearthAndHome", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Special));
        _forHearthAndHomePassive3.Initialize(CareerID, "{=for_hearth_and_home_passive3_str}Units with runes heal 50% faster after battles.", "ForHearthAndHome", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.TroopRegeneration, true,
            characterObject => characterObject.HasUnitRune())); //specific troops getting bonuses requires patches or implementing a PartyHeal behavior
        _forHearthAndHomePassive4.Initialize(CareerID, "{=for_hearth_and_home_passive4_str}Rune magic lasts 10% longer + 0.01% for every point in faith", "ForHearthAndHome", false,
            ChoiceType.Passive, new List<CareerChoiceObject.MutationObject>()
            {
            }, null);

        _stoneAndSteelPassive1.Initialize(CareerID, "{=stone_and_steel_passive1_str}Gain 10% magic resistance to spells.", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
        _stoneAndSteelPassive2.Initialize(CareerID, "{=stone_and_steel_passive2_str}10% magic resistance for troops", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell,
            (attacker, victim, mask) => victim.BelongsToMainParty() && victim.Character.Culture.StringId == TORConstants.Cultures.DAWI));
        _stoneAndSteelPassive3.Initialize(CareerID, "{=stone_and_steel_passive3_str}Increases Hitpoints by 25.", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _stoneAndSteelPassive4.Initialize(CareerID, "{=stone_and_steel_passive4_str}reducing the cooldown to a rune to 0 gives you for 15 seconds 50% extra magical damage", "StoneAndSteel", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15));

        _legacyOfGrungniPassive1.Initialize(CareerID, "{=wrath_against_chaos_passive1_str}Increased gain of Oathgold from delivered steel.", "LegacyOfGrungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _legacyOfGrungniPassive2.Initialize(CareerID, "{=wrath_against_chaos_passive2_str}For every owned Ironsmelter workshop in a Karak gain 2 Oath Gold per day.", "LegacyOfGrungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 35), AttackTypeMask.Spell));
        _legacyOfGrungniPassive3.Initialize(CareerID, "{=wrath_against_chaos_passive4_str}Unit runes are 1/3 cheaper", "LegacyOfGrungni", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        _legacyOfGrungniPassive4.Initialize(CareerID, "For every point in smithing , range and radius of magic runes rises by 0.005", "LegacyOfGrungni", false,
            ChoiceType.Passive, new List<CareerChoiceObject.MutationObject>()
            {
            }, null);

        _anvilOfDoomPassive1.Initialize(CareerID, "{=anvil_of_doom_passive1_str}15% extra magical damage.", "AnvilOfDoom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
        _anvilOfDoomPassive2.Initialize(CareerID, "{=chiselAndHammer_passive3_str}Gain daily 5 Oathgold if an Anvil of Doom is present", "AnvilOfDoom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.CustomResourceGain, false, x => x.HeroObject is { PartyBelongedTo: not null } && x.HeroObject.PartyBelongedTo.HasAnvilOfDoom()));
        _anvilOfDoomPassive3.Initialize(CareerID, "{=anvil_of_doom_passive3_str}Every alive rune smith in the battlefield reduce Rune magic cooldown by 2%", "AnvilOfDoom", false, ChoiceType.Passive, null, null);
        _anvilOfDoomPassive4.Initialize(CareerID, "{=anvil_of_doom_passive4_str}Gain the option to add an additional seal on a troop.", "AnvilOfDoom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special)); //
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