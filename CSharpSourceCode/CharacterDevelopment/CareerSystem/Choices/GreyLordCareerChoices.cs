using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;


namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class GreyLordCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _greyLordRoot;

        private CareerChoiceObject _caelithsWisdomPassive1;
        private CareerChoiceObject _caelithsWisdomPassive2;
        private CareerChoiceObject _caelithsWisdomPassive3;
        private CareerChoiceObject _caelithsWisdomPassive4;
        private CareerChoiceObject _caelithsWisdomKeystone;

        private CareerChoiceObject _soulBindingPassive1;
        private CareerChoiceObject _soulBindingPassive2;
        private CareerChoiceObject _soulBindingPassive3;
        private CareerChoiceObject _soulBindingPassive4;
        private CareerChoiceObject _soulBindingKeystone;

        private CareerChoiceObject _legendsOfMalokPassive1;
        private CareerChoiceObject _legendsOfMalokPassive2;
        private CareerChoiceObject _legendsOfMalokPassive3;
        private CareerChoiceObject _legendsOfMalokPassive4;
        private CareerChoiceObject _legendsOfMalokKeystone;

        private CareerChoiceObject _unrestrictedMagicPassive1;
        private CareerChoiceObject _unrestrictedMagicPassive2;
        private CareerChoiceObject _unrestrictedMagicPassive3;
        private CareerChoiceObject _unrestrictedMagicPassive4;
        private CareerChoiceObject _unrestrictedMagicKeystone;

        private CareerChoiceObject _forbiddenScrollsOfSapheryPassive1;
        private CareerChoiceObject _forbiddenScrollsOfSapheryPassive2;
        private CareerChoiceObject _forbiddenScrollsOfSapheryPassive3;
        private CareerChoiceObject _forbiddenScrollsOfSapheryPassive4;
        private CareerChoiceObject _forbiddenScrollsOfSapheryKeystone;

        private CareerChoiceObject _byAllMeansPassive1;
        private CareerChoiceObject _byAllMeansPassive2;
        private CareerChoiceObject _byAllMeansPassive3;
        private CareerChoiceObject _byAllMeansPassive4;
        private CareerChoiceObject _byAllMeansKeystone;

        private CareerChoiceObject _secretOfFellfangPassive1;
        private CareerChoiceObject _secretOfFellfangPassive2;
        private CareerChoiceObject _secretOfFellfangPassive3;
        private CareerChoiceObject _secretOfFellfangPassive4;
        private CareerChoiceObject _secretOfFellfangKeystone;


        protected override void RegisterAll()
        {
            _greyLordRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GreyLordRoot"));

            _caelithsWisdomPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_caelithsWisdomPassive1).UnderscoreFirstCharToUpper()));
            _caelithsWisdomPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_caelithsWisdomPassive2).UnderscoreFirstCharToUpper()));
            _caelithsWisdomPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_caelithsWisdomPassive3).UnderscoreFirstCharToUpper()));
            _caelithsWisdomPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_caelithsWisdomPassive4).UnderscoreFirstCharToUpper()));
            _caelithsWisdomKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_caelithsWisdomKeystone).UnderscoreFirstCharToUpper()));

            _soulBindingPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_soulBindingPassive1).UnderscoreFirstCharToUpper()));
            _soulBindingPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_soulBindingPassive2).UnderscoreFirstCharToUpper()));
            _soulBindingPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_soulBindingPassive3).UnderscoreFirstCharToUpper()));
            _soulBindingPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_soulBindingPassive4).UnderscoreFirstCharToUpper()));
            _soulBindingKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_soulBindingKeystone).UnderscoreFirstCharToUpper()));

            _legendsOfMalokPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_legendsOfMalokPassive1).UnderscoreFirstCharToUpper()));
            _legendsOfMalokPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_legendsOfMalokPassive2).UnderscoreFirstCharToUpper()));
            _legendsOfMalokPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_legendsOfMalokPassive3).UnderscoreFirstCharToUpper()));
            _legendsOfMalokPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_legendsOfMalokPassive4).UnderscoreFirstCharToUpper()));
            _legendsOfMalokKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_legendsOfMalokKeystone).UnderscoreFirstCharToUpper()));

            _unrestrictedMagicPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_unrestrictedMagicPassive1).UnderscoreFirstCharToUpper()));
            _unrestrictedMagicPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_unrestrictedMagicPassive2).UnderscoreFirstCharToUpper()));
            _unrestrictedMagicPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_unrestrictedMagicPassive3).UnderscoreFirstCharToUpper()));
            _unrestrictedMagicPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_unrestrictedMagicPassive4).UnderscoreFirstCharToUpper()));
            _unrestrictedMagicKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_unrestrictedMagicKeystone).UnderscoreFirstCharToUpper()));

            _forbiddenScrollsOfSapheryPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_forbiddenScrollsOfSapheryPassive1).UnderscoreFirstCharToUpper()));
            _forbiddenScrollsOfSapheryPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_forbiddenScrollsOfSapheryPassive2).UnderscoreFirstCharToUpper()));
            _forbiddenScrollsOfSapheryPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_forbiddenScrollsOfSapheryPassive3).UnderscoreFirstCharToUpper()));
            _forbiddenScrollsOfSapheryPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_forbiddenScrollsOfSapheryPassive4).UnderscoreFirstCharToUpper()));
            _forbiddenScrollsOfSapheryKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_forbiddenScrollsOfSapheryKeystone).UnderscoreFirstCharToUpper()));

            _byAllMeansPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_byAllMeansPassive1).UnderscoreFirstCharToUpper()));
            _byAllMeansPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_byAllMeansPassive2).UnderscoreFirstCharToUpper()));
            _byAllMeansPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_byAllMeansPassive3).UnderscoreFirstCharToUpper()));
            _byAllMeansPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_byAllMeansPassive4).UnderscoreFirstCharToUpper()));
            _byAllMeansKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_byAllMeansKeystone).UnderscoreFirstCharToUpper()));

            _secretOfFellfangPassive1 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_secretOfFellfangPassive1).UnderscoreFirstCharToUpper()));
            _secretOfFellfangPassive2 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_secretOfFellfangPassive2).UnderscoreFirstCharToUpper()));
            _secretOfFellfangPassive3 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_secretOfFellfangPassive3).UnderscoreFirstCharToUpper()));
            _secretOfFellfangPassive4 =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_secretOfFellfangPassive4).UnderscoreFirstCharToUpper()));
            _secretOfFellfangKeystone =
                Game.Current.ObjectManager.RegisterPresumedObject(
                    new CareerChoiceObject(nameof(_secretOfFellfangKeystone).UnderscoreFirstCharToUpper()));


        }

        protected override void InitializeKeyStones()
        {

            _greyLordRoot.Initialize(CareerID, "These feeble-minded rabble can be put to good use! Call upon the forbidden arts of magical manipulation, to seep into the psyche of your lessers. Providing a 10% chance to turn them against one another with the ancient art of Mind Control! For every level of Spellcraft gain 0.06% success chance. (Ability is charged by dealing spell damage.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Spellcraft }, 0.000625f),
                            MutationType = OperationType.Add
                        }
                });
            _caelithsWisdomKeystone.Initialize(CareerID, "Mind control also scales with Leadership, gains +2 targets, can be charged by melee damage.", "CaelithsWisdom", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.000625f),
                            MutationType = OperationType.Add
                        },
                }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _soulBindingKeystone.Initialize(CareerID, "Mind Control also scales with Medicine, and heals affected targets.", "SoulBinding", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Medicine }, 0.000625f),
                            MutationType = OperationType.Add
                        },
                }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _legendsOfMalokKeystone.Initialize(CareerID, "Mind Control begins battle charged, and gains +15% success chance.", "LegendsOfMalok", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.15f,
                            MutationType = OperationType.Add
                        },
                }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _unrestrictedMagicKeystone.Initialize(CareerID, "Mind Controled troops explode on death. Failing to control a troop now hurts them.", "UnrestrictedMagic", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _forbiddenScrollsOfSapheryKeystone.Initialize(CareerID, "Mind Control also scales with Charm, has its range doubled, and Companions help charge.", "ForbiddenScrollsOfSaphery", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "MaxDistance",
                            PropertyValue = (choice, originalValue, agent) =>2f,
                            MutationType = OperationType.Multiply
                        },
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Charm }, 0.000625f),
                            MutationType = OperationType.Add
                        },
                }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _byAllMeansKeystone.Initialize(CareerID, "Mind Control also scales with Roguery, and gains +10% success chance.", "ByAllMeans", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery }, 0.000625f),
                            MutationType = OperationType.Add
                        },
                }, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _secretOfFellfangKeystone.Initialize(CareerID, "Mind Control recharges +1 'Winds of Magic' per succesful target.", "SecretOfFellfang", false,
                ChoiceType.Passive);
        }

        protected override void InitializePassives()
        {
            _caelithsWisdomPassive1.Initialize(CareerID, "+10 personal Hitpoints.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
            _caelithsWisdomPassive2.Initialize(CareerID, "+10% personal 'Fire Resistance'.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.All));
            _caelithsWisdomPassive3.Initialize(CareerID, "-25% wages for 'Cityborn' troops.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages, true,
                characterObject => characterObject.IsEliteTroop() && characterObject.Culture.StringId == TORConstants.Cultures.EONIR));
            _caelithsWisdomPassive4.Initialize(CareerID, "+25% 'Fire Resistance' for 'Cityborn' troops.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Fire, 25), AttackTypeMask.All,
                (attacker, victim, mask) => victim.BelongsToMainParty() && !victim.IsHero && victim.Character.Culture.StringId == TORConstants.Cultures.EONIR));

            _soulBindingPassive1.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _soulBindingPassive2.Initialize(CareerID, "+15% personal 'Spell' resistance.", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.All));
            _soulBindingPassive3.Initialize(CareerID, "Wounded troops heal faster.", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.TroopRegeneration));
            _soulBindingPassive4.Initialize(CareerID, "+35% personal duration of 'Healing' spells if no 'Projectile' spells are equipped.", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.35f, PassiveEffectType.Special));

            _legendsOfMalokPassive1.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _legendsOfMalokPassive2.Initialize(CareerID, "-20% 'Favor' cost for 'Cityborn' troop upgrades.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.CustomResourceUpgradeCostModifier, true));
            _legendsOfMalokPassive3.Initialize(CareerID, "+15% 'Fire' damage for all troops.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Fire, 15), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && attacker.Character.IsEliteTroop() && attacker.Character.Culture.StringId == TORConstants.Cultures.EONIR));
            _legendsOfMalokPassive4.Initialize(CareerID, "+50% personal spell radius if no 'Hex' spells are equipped.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.Special));


            _unrestrictedMagicPassive1.Initialize(CareerID, "+10 personal 'Winds of Magic' capacity.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _unrestrictedMagicPassive2.Initialize(CareerID, "+20% personal spell radius.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius, true));
            _unrestrictedMagicPassive3.Initialize(CareerID, "+20% 'Favor' gained from battles.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _unrestrictedMagicPassive4.Initialize(CareerID, "+150% damage for 'Projectile' spells if no 'Area of Effect' spell is equipped.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.5f, PassiveEffectType.Special));

            _forbiddenScrollsOfSapheryPassive1.Initialize(CareerID, "+0.5 personal 'Winds of Magic' recharge rate.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.WindsRegeneration));
            _forbiddenScrollsOfSapheryPassive2.Initialize(CareerID, "+20% duration of 'Hex' spells.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.DebuffDuration, true));
            _forbiddenScrollsOfSapheryPassive3.Initialize(CareerID, "-1% cost for 'Enchantments' per known spell.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-1, PassiveEffectType.Special));
            _forbiddenScrollsOfSapheryPassive4.Initialize(CareerID, "+50% duration of 'Hex' spells if no 'Healing' spells are equipped.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.Special));

            _byAllMeansPassive1.Initialize(CareerID, "+20% personal 'Ward Save' if armour weight does not exceed 11.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 20), AttackTypeMask.All,
                (attacker, victim, attackmask) => victim.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(victim, 11)));
            _byAllMeansPassive2.Initialize(CareerID, "+5% personal 'Lightning' spell damage.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 5), AttackTypeMask.Spell));
            _byAllMeansPassive3.Initialize(CareerID, "+5% personal 'Magic' spell damage.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell));
            _byAllMeansPassive4.Initialize(CareerID, "+100% of 'Vortex' and 'Area of Effect' spells if no 'Augment' spells are equipped.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.0f, PassiveEffectType.Special));

            _secretOfFellfangPassive1.Initialize(CareerID, "+50% personal spell cooldown reduction if less than 11 spells are equipped.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(11, PassiveEffectType.Special));
            _secretOfFellfangPassive2.Initialize(CareerID, "+10 personal 'Winds of Magic' capacity.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _secretOfFellfangPassive3.Initialize(CareerID, "30% of your max 'Winds of Magic' capacity is restored after battle.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true));
            _secretOfFellfangPassive4.Initialize(CareerID, "+10% personal 'Fire' spell damage.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Spell));
        }


        protected override void UnlockCareerBenefitsTier2()
        {
        }

        protected override void UnlockCareerBenefitsTier3()
        {
            Hero.MainHero.AddKnownLore("DarkMagic");
        }
    }
}