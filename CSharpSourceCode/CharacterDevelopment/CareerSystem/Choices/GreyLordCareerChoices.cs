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
                
                _greyLordRoot.Initialize(CareerID, "As a master of Winds of Magic and forbidden arts, the Grey Lord conjures subtle whispers inside the enemy’s mind and convinces the foes in the area to turn on their allies. After all, a true Grey Lord knows better than to cloud his judgment with trifling matters of morality. {newline} The chance to turn enemies rises with Spellcraft skills, the health and the level of targets as well as each keystone perk selected", null, true,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "MindControl",
                            PropertyName = "ScaleVariable1",
                            PropertyValue = (choice, originalValue, agent) =>0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.SpellCraft }, 0.000625f),
                            MutationType = OperationType.Add
                        }
                    });
                _caelithsWisdomKeystone.Initialize(CareerID, "2 additional overtakes. Ability scales with Leadership. Melee damage can charge ability.", "CaelithsWisdom", false,
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
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _soulBindingKeystone.Initialize(CareerID, "Controlled unit gets healed completely. Ability scales with medicine.", "SoulBinding", false,
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
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _legendsOfMalokKeystone.Initialize(CareerID, "control chance is 15% higher. Ability starts charged. ", "LegendsOfMalok", false,
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
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _unrestrictedMagicKeystone.Initialize(CareerID, "When the controlled dies he explodes. Unsucessful control hurt the enemy by 40 HP.", "UnrestrictedMagic", false,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _forbiddenScrollsOfSapheryKeystone.Initialize(CareerID, "Companion damage can charge ability. Range is doubled. Scale with Charm", "ForbiddenScrollsOfSaphery", false,
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
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _byAllMeansKeystone.Initialize(CareerID, "Ability scales with roguery. Units are easier to overtake", "ByAllMeans", false,
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
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _secretOfFellfangKeystone.Initialize(CareerID, "Successful overtake grants 3 winds of magic.", "SecretOfFellfang", false,
                    ChoiceType.Passive);
            }

            protected override void InitializePassives()
            {
                _caelithsWisdomPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health)); 
                _caelithsWisdomPassive2.Initialize(CareerID, "{=avatar_of_death_passive1_str}Gain 25% fire resistance.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Fire, 25), AttackTypeMask.All));
                _caelithsWisdomPassive3.Initialize(CareerID, "Cityborn upkeep is reduced by 25%.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages,true, 
                    characterObject => characterObject.IsEliteTroop() && characterObject.Culture.StringId == TORConstants.Cultures.EONIR));
                _caelithsWisdomPassive4.Initialize(CareerID, "Cityborn troops gain 50% fire resistance.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Fire, 50), AttackTypeMask.All, 
                    (attacker, victim, mask) => victim.BelongsToMainParty()&& !victim.IsHero && victim.Character.Culture.StringId == TORConstants.Cultures.EONIR ));
                
                _soulBindingPassive1.Initialize(CareerID, "Increases maximum winds of magic capacities by 10.", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
                _soulBindingPassive2.Initialize(CareerID, "Increases Magic resistance against spells by 25%.", "SoulBinding", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,25),AttackTypeMask.All));
                _soulBindingPassive3.Initialize(CareerID, "Wounded troops in your party heal faster.", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration)); 
                _soulBindingPassive4.Initialize(CareerID, "Not wielding projectile spells increase spell duration for healing spells by 50%", "SoulBinding", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special)); 
                
                _legendsOfMalokPassive1.Initialize(CareerID, "Increases maximum winds of magic capacities by 10.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
                _legendsOfMalokPassive2.Initialize(CareerID, "Favor costs for cityborn  troop upgrades is reduced by 20%.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.CustomResourceUpgradeCostModifier,true));
                _legendsOfMalokPassive3.Initialize(CareerID, "Adds 25% fire damage to all troops.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Fire, 25), AttackTypeMask.Melee, 
                    (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && attacker.Character.IsEliteTroop() &&  attacker.Character.Culture.StringId == TORConstants.Cultures.EONIR));
                _legendsOfMalokPassive4.Initialize(CareerID, "If no hex spells are equipped you gain 50% spell radius.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special)); 

                
                _unrestrictedMagicPassive1.Initialize(CareerID, "Increases maximum winds of magic capacities by 15.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
                _unrestrictedMagicPassive2.Initialize(CareerID, "Spell effect radius is increased by 20%.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius,true));
                _unrestrictedMagicPassive3.Initialize(CareerID, "Favor gain from battles is increased by 20%", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
                _unrestrictedMagicPassive4.Initialize(CareerID, "If no AOE effect is equipped, your projectile spells deal 200% extra damage.", "UnrestrictedMagic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special)); 
                
                _forbiddenScrollsOfSapheryPassive1.Initialize(CareerID, "Increases Windsregeneration by 0.5.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.WindsRegeneration));
                _forbiddenScrollsOfSapheryPassive2.Initialize(CareerID, "Increase hex durations by 20%.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.DebuffDuration,true)); 
                _forbiddenScrollsOfSapheryPassive3.Initialize(CareerID, "Increases maximum winds of magic capacities by 15.", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
                _forbiddenScrollsOfSapheryPassive4.Initialize(CareerID, "Not wielding any healing spells increase hex duration by 50%", "ForbiddenScrollsOfSaphery", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
                
                _byAllMeansPassive1.Initialize(CareerID, "Extra 25% Wardsave if your armor weight does not exceed 11 weight.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 25), AttackTypeMask.All,
                    (attacker, victim, attackmask) => victim.IsMainAgent && CareerChoicesHelper.ArmorWeightUndershootCheck(victim,11)));
                _byAllMeansPassive2.Initialize(CareerID, "Increases Lightning spell damage by 10%.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 10), AttackTypeMask.Spell));
                _byAllMeansPassive3.Initialize(CareerID, "Increases magic spell damage by 10%.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
                _byAllMeansPassive4.Initialize(CareerID, "If no buff spells are equipped, duration of vortex and area effects are doubled.", "ByAllMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
                
                _secretOfFellfangPassive1.Initialize(CareerID, "50% cooldown reduction if you wield less than 11 spells", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(11, PassiveEffectType.Special));
                _secretOfFellfangPassive2.Initialize(CareerID, "Increases maximum winds of magic capacities by 25.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.WindsOfMagic));
                _secretOfFellfangPassive3.Initialize(CareerID, "After battle, 30% of your winds are regenerated.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true));
                _secretOfFellfangPassive4.Initialize(CareerID, "Increases fire spell damage by 15%.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Spell));
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