using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class ImperialMagisterCareerChoices : TORCareerChoicesBase
    {
        public ImperialMagisterCareerChoices(CareerObject id) : base(id) { }

        private CareerChoiceObject _imperialMagisterRoot;

        private CareerChoiceObject _studyAndPractiseKeystone;
        private CareerChoiceObject _studyAndPractisePassive1;
        private CareerChoiceObject _studyAndPractisePassive2;
        private CareerChoiceObject _studyAndPractisePassive3;
        private CareerChoiceObject _studyAndPractisePassive4;

        private CareerChoiceObject _teclisTeachingsKeystone;
        private CareerChoiceObject _teclisTeachingsPassive1;
        private CareerChoiceObject _teclisTeachingsPassive2;
        private CareerChoiceObject _teclisTeachingsPassive3;
        private CareerChoiceObject _teclisTeachingsPassive4;

        private CareerChoiceObject _imperialEnchantmentKeystone;
        private CareerChoiceObject _imperialEnchantmentPassive1;
        private CareerChoiceObject _imperialEnchantmentPassive2;
        private CareerChoiceObject _imperialEnchantmentPassive3;
        private CareerChoiceObject _imperialEnchantmentPassive4;

        private CareerChoiceObject _collegeOrdersKeystone;
        private CareerChoiceObject _collegeOrdersPassive1;
        private CareerChoiceObject _collegeOrdersPassive2;
        private CareerChoiceObject _collegeOrdersPassive3;
        private CareerChoiceObject _collegeOrdersPassive4;

        private CareerChoiceObject _magicCombatTrainingKeystone;
        private CareerChoiceObject _magicCombatTrainingPassive1;
        private CareerChoiceObject _magicCombatTrainingPassive2;
        private CareerChoiceObject _magicCombatTrainingPassive3;
        private CareerChoiceObject _magicCombatTrainingPassive4;

        private CareerChoiceObject _ancientScrollsKeystone;
        private CareerChoiceObject _ancientScrollsPassive1;
        private CareerChoiceObject _ancientScrollsPassive2;
        private CareerChoiceObject _ancientScrollsPassive3;
        private CareerChoiceObject _ancientScrollsPassive4;

        private CareerChoiceObject _arcaneKnowledgeKeystone;
        private CareerChoiceObject _arcaneKnowledgePassive1;
        private CareerChoiceObject _arcaneKnowledgePassive2;
        private CareerChoiceObject _arcaneKnowledgePassive3;
        private CareerChoiceObject _arcaneKnowledgePassive4;

        protected override void RegisterAll()
        {

            _imperialMagisterRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ImperialMagisterRoot"));

            _studyAndPractiseKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractiseKeystone).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive1).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive2).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive3).UnderscoreFirstCharToUpper()));
            _studyAndPractisePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_studyAndPractisePassive4).UnderscoreFirstCharToUpper()));

            _teclisTeachingsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsKeystone).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive1).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive2).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive3).UnderscoreFirstCharToUpper()));
            _teclisTeachingsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teclisTeachingsPassive4).UnderscoreFirstCharToUpper()));

            _imperialEnchantmentKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentKeystone).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive1).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive2).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive3).UnderscoreFirstCharToUpper()));
            _imperialEnchantmentPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_imperialEnchantmentPassive4).UnderscoreFirstCharToUpper()));

            _collegeOrdersKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersKeystone).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive1).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive2).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive3).UnderscoreFirstCharToUpper()));
            _collegeOrdersPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_collegeOrdersPassive4).UnderscoreFirstCharToUpper()));

            _magicCombatTrainingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingKeystone).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive1).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive2).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive3).UnderscoreFirstCharToUpper()));
            _magicCombatTrainingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_magicCombatTrainingPassive4).UnderscoreFirstCharToUpper()));

            _ancientScrollsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsKeystone).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive1).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive2).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive3).UnderscoreFirstCharToUpper()));
            _ancientScrollsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_ancientScrollsPassive4).UnderscoreFirstCharToUpper()));

            _arcaneKnowledgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgeKeystone).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive1).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive2).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive3).UnderscoreFirstCharToUpper()));
            _arcaneKnowledgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_arcaneKnowledgePassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _imperialMagisterRoot.Initialize(CareerID, "Channel the 'Winds of Magic' into a wondrous gale! Every second up to 10, regain 2% of your total 'Winds of Magic' pool. While channeling, you are more vulnerable to damage, and your movements become sluggish. Every second 'Keystone' career talent unlocked provides +1 charge of Arcane Conduit. For every 50 levels in Spellcraft, Arcane Conduit's duration increases by +1s. (Ability charges by dealing spell damage.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1f,
                        MutationType = OperationType.Add
                    },
                    new()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_winds_reg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) =>
                        {
                            var hero = agent?.GetHero();
                            if (hero != null)
                            {
                                var maxWinds = hero.GetExtendedInfo()?.MaxWindsOfMagic ?? 50f;
                                return Math.Max(1f, maxWinds * 0.02f);
                            }
                            return 1f;
                        },
                        MutationType = OperationType.Replace
                    }
                });

            _studyAndPractiseKeystone.Initialize(CareerID, "+1 charge of Arcane Conduit, and +50% 'Physical Resistance' while channeling.", "StudyAndPractise", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1.5f,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "arcane_conduit_res_buff" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_res_buff",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => 0.5f,
                        MutationType = OperationType.Add
                    },
                });

            _teclisTeachingsKeystone.Initialize(CareerID, "+1 charge of Arcane Conduit, and gain +25% max channel time.", "TeclisTeachings", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1.5f,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => (float) originalValue * 0.25f ,
                        MutationType = OperationType.Add
                    }
                });

            _imperialEnchantmentKeystone.Initialize(CareerID, "Channeling Arcane Conduit now affects your speed less.", "ImperialEnchantment", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.5f,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) =>
                        {
                            var list = originalValue as List<string>;
                            list.Add("arcane_conduit_slow_light");
                            return list.FindAll(x => x != "arcane_conduit_slow");;
                        }
                        ,
                        MutationType = OperationType.Replace
                    }
                });
            _collegeOrdersKeystone.Initialize(CareerID, "Arcane Conduit also applies to Companions.", "CollegeOrders", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "ArcaneConduit",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => 0.5f,
                    MutationType = OperationType.Add
                },
            }, new CareerChoiceObject.PassiveEffect());

            _magicCombatTrainingKeystone.Initialize(CareerID, "+30% personal damage for all spells while channeling Arcane Conduit.", "MagicCombatTraining", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.5f,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "arcane_conduit_dmg_buff" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });


            _ancientScrollsKeystone.Initialize(CareerID, "+1 charge of Arcane Conduit, and Arcane Conduit recharges 50% faster.", "AncientScrolls", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 1.5f,
                        MutationType = OperationType.Add
                    },
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "CoolDown",
                        PropertyValue = (choice, originalValue, agent) => -((int)originalValue * 0.5f) ,
                        MutationType = OperationType.Add
                    },
                });

            _arcaneKnowledgeKeystone.Initialize(CareerID, "+1 charge of Arcane Conduit. Half its duration, double its regen. Resets spell cooldown on use.", "ArcaneKnowledge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ArcaneConduit",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.5f,
                        MutationType = OperationType.Add
                    },
                    new()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_arcaneconduit",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => -0.5f,
                        MutationType = OperationType.Multiply
                    },
                    new()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "arcane_conduit_winds_reg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) =>
                        {
                            var hero = agent?.GetHero();
                            if (hero != null)
                            {
                                var maxWinds = hero.GetExtendedInfo()?.MaxWindsOfMagic ?? 50f;
                                return Math.Max(2f, maxWinds * 0.04f); // 4% (doubled from 2%), minimum 2
                            }
                            return 2f;
                        },
                        MutationType = OperationType.Replace
                    },
                });//special


        }

        protected override void InitializePassives()
        {
            _studyAndPractisePassive1.Initialize(CareerID, "+3 personal max 'Winds of Magic' capacity.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.WindsOfMagic));
            _studyAndPractisePassive2.Initialize(CareerID,
                "+10% personal 'Ward Save' if armour weight does not exceed 11.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All,
                    (attacker, victim, attackmask) => victim == Agent.Main && CareerChoicesHelper.ArmorWeightCheck(victim, 11)));
            _studyAndPractisePassive3.Initialize(CareerID, "+5% personal 'Magic' spell damage.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell));
            _studyAndPractisePassive4.Initialize(CareerID, "+10% personal 'Magic Resistance'.", "StudyAndPractise", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));

            _teclisTeachingsPassive1.Initialize(CareerID, "+5% personal 'Fire' spell damage.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 5), AttackTypeMask.Spell));
            _teclisTeachingsPassive2.Initialize(CareerID, "+5% personal 'Lightning' spell damage.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 5), AttackTypeMask.Spell));
            _teclisTeachingsPassive3.Initialize(CareerID, "+3 personal max 'Winds of Magic' capacity.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.WindsOfMagic));
            _teclisTeachingsPassive4.Initialize(CareerID, "'Power Stones' cost -35% less 'Prestige'.", "TeclisTeachings", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _imperialEnchantmentPassive1.Initialize(CareerID, "Every week, gain up to 2 Arcane Scrolls per Imperial Wizard in your party.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
            _imperialEnchantmentPassive2.Initialize(CareerID, "-30% friendly damage done by spells.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.Special, true));
            _imperialEnchantmentPassive3.Initialize(CareerID, "-25% cost for enchantments.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.EnchantmentCostReduction, true));
            _imperialEnchantmentPassive4.Initialize(CareerID, "-25% 'Winds of Magic' cost for 'Power Stones'.", "ImperialEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));

            _collegeOrdersPassive1.Initialize(CareerID, "+5 Companion limit.", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _collegeOrdersPassive2.Initialize(CareerID, "+10 max 'Winds of Magic' for Imperial Wizard Companions.", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _collegeOrdersPassive3.Initialize(CareerID, "+10% more 'Prestige' from battles per different type of Imperial Wizard Companion.", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _collegeOrdersPassive4.Initialize(CareerID, "Gain access to the 'Power Stones' of other Imperial College Orders.", "CollegeOrders", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));

            _magicCombatTrainingPassive1.Initialize(CareerID, "+5 personal max 'Winds of Magic' capacity.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _magicCombatTrainingPassive2.Initialize(CareerID, "+25% spell cooldown reduction.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.WindsCooldownReduction, true));
            _magicCombatTrainingPassive3.Initialize(CareerID, "+15% spell power when wielding an off-hand staff.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SpellEffectiveness, true,
                (CharacterObject character) => HasMagicStaff()));
            _magicCombatTrainingPassive4.Initialize(CareerID, "+20% 'Ward Save' if armour weight does not exceed 11.", "MagicCombatTraining", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 20), AttackTypeMask.All,
                (attacker, victim, attackmask) => victim.IsHero && victim.IsMainAgent && CareerChoicesHelper.ArmorWeightCheck(victim, 11)));

            _ancientScrollsPassive1.Initialize(CareerID, "+25% duration increase for 'Hex' spells.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25f, PassiveEffectType.DebuffDuration, true));
            _ancientScrollsPassive2.Initialize(CareerID, "'Winds of Magic' regeneration increased by +1.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.WindsRegeneration));
            _ancientScrollsPassive3.Initialize(CareerID, "80% 'Prestige' is recovered from used 'Power Stones'.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _ancientScrollsPassive4.Initialize(CareerID, "-5% 'Power Stone' upkeep per unique Imperial Wizard they gain +20% spell damage.", "AncientScrolls", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, true));


            _arcaneKnowledgePassive1.Initialize(CareerID, "+10% spell damage of Imperial Wizard Companions.", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _arcaneKnowledgePassive2.Initialize(CareerID, "+10% personal spell effect radius.", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SpellRadius, true));
            _arcaneKnowledgePassive3.Initialize(CareerID, "+10% spell power if armour weight does not exceed 11.", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.SpellEffectiveness, true,
                (characterObject => characterObject.IsHero && characterObject.HeroObject == Hero.MainHero && CareerChoicesHelper.ArmorWeightCheck(Agent.Main, 11))));
            _arcaneKnowledgePassive4.Initialize(CareerID, "+2 personal max 'Winds of Magic' per Imperial Wizard Companion.", "ArcaneKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
        }


        private bool HasMagicStaff()
        {
            var weapons = Hero.MainHero.CharacterObject.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);

            if (weapons.Any(x => x.IsMagicalStaff()))
            {
                return true;
            }

            return false;
        }

    }
}