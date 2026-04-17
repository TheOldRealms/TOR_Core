using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class NecrarchCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _necrarchRoot;

        private CareerChoiceObject _discipleOfAccursedKeystone;
        private CareerChoiceObject _discipleOfAccursedPassive1;
        private CareerChoiceObject _discipleOfAccursedPassive2;
        private CareerChoiceObject _discipleOfAccursedPassive3;
        private CareerChoiceObject _discipleOfAccursedPassive4;

        private CareerChoiceObject _witchSightKeystone;
        private CareerChoiceObject _witchSightPassive1;
        private CareerChoiceObject _witchSightPassive2;
        private CareerChoiceObject _witchSightPassive3;
        private CareerChoiceObject _witchSightPassive4;

        private CareerChoiceObject _darkVisionKeystone;
        private CareerChoiceObject _darkVisionPassive1;
        private CareerChoiceObject _darkVisionPassive2;
        private CareerChoiceObject _darkVisionPassive3;
        private CareerChoiceObject _darkVisionPassive4;

        private CareerChoiceObject _unhallowedSoulKeystone;
        private CareerChoiceObject _unhallowedSoulPassive1;
        private CareerChoiceObject _unhallowedSoulPassive2;
        private CareerChoiceObject _unhallowedSoulPassive3;
        private CareerChoiceObject _unhallowedSoulPassive4;

        private CareerChoiceObject _hungerForKnowledgeKeystone;
        private CareerChoiceObject _hungerForKnowledgePassive1;
        private CareerChoiceObject _hungerForKnowledgePassive2;
        private CareerChoiceObject _hungerForKnowledgePassive3;
        private CareerChoiceObject _hungerForKnowledgePassive4;

        private CareerChoiceObject _wellspringOfDharKeystone;
        private CareerChoiceObject _wellspringOfDharPassive1;
        private CareerChoiceObject _wellspringOfDharPassive2;
        private CareerChoiceObject _wellspringOfDharPassive3;
        private CareerChoiceObject _wellspringOfDharPassive4;

        private CareerChoiceObject _everlingsSecretKeystone;
        private CareerChoiceObject _everlingsSecretPassive1;
        private CareerChoiceObject _everlingsSecretPassive2;
        private CareerChoiceObject _everlingsSecretPassive3;
        private CareerChoiceObject _everlingsSecretPassive4;


        protected override void RegisterAll()
        {
            _necrarchRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NecrarchRoot"));

            _discipleOfAccursedKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedKeystone).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive1).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive2).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive3).UnderscoreFirstCharToUpper()));
            _discipleOfAccursedPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_discipleOfAccursedPassive4).UnderscoreFirstCharToUpper()));

            _witchSightKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightKeystone).UnderscoreFirstCharToUpper()));
            _witchSightPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive1).UnderscoreFirstCharToUpper()));
            _witchSightPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive2).UnderscoreFirstCharToUpper()));
            _witchSightPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive3).UnderscoreFirstCharToUpper()));
            _witchSightPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_witchSightPassive4).UnderscoreFirstCharToUpper()));

            _darkVisionKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionKeystone).UnderscoreFirstCharToUpper()));
            _darkVisionPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive1).UnderscoreFirstCharToUpper()));
            _darkVisionPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive2).UnderscoreFirstCharToUpper()));
            _darkVisionPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive3).UnderscoreFirstCharToUpper()));
            _darkVisionPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_darkVisionPassive4).UnderscoreFirstCharToUpper()));

            _unhallowedSoulKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulKeystone).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive1).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive2).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive3).UnderscoreFirstCharToUpper()));
            _unhallowedSoulPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_unhallowedSoulPassive4).UnderscoreFirstCharToUpper()));

            _hungerForKnowledgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgeKeystone).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive1).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive2).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive3).UnderscoreFirstCharToUpper()));
            _hungerForKnowledgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hungerForKnowledgePassive4).UnderscoreFirstCharToUpper()));

            _wellspringOfDharKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharKeystone).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive1).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive2).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive3).UnderscoreFirstCharToUpper()));
            _wellspringOfDharPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wellspringOfDharPassive4).UnderscoreFirstCharToUpper()));

            _everlingsSecretKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretKeystone).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive1).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive2).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive3).UnderscoreFirstCharToUpper()));
            _everlingsSecretPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_everlingsSecretPassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _necrarchRoot.Initialize(CareerID, "Siphon the winds into a Blast of Agony! When used, Blast of Agony hurls a ball of condensed darkness, exploding on impact dealing 80 'Magic' damage. For every level of Spellcraft, increase Blast of Agony's radius by 0.01m. (Ability is charged by dealing 'Spell' damage.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_blastofagony",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Spellcraft}, 0.01f),
                        MutationType = OperationType.Add
                    },
                });

            _discipleOfAccursedKeystone.Initialize(CareerID, "Blast of Agony also scales with Roguery and, 'Lesser Undead' troops charge Blast of Agony.", "DiscipleOfAccursed", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_blastofagony",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery}, 0.01f),
                        MutationType = OperationType.Add
                    }
                });

            _witchSightKeystone.Initialize(CareerID, "+0.15% of personal max 'Winds of Magic' capacity restored, per foe hit with Blast of Agony.", "WitchSight", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_blastofagony",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "netherball_dot" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            _darkVisionKeystone.Initialize(CareerID, "Blast of Agony summons a Wraith on impact and its cooldown is reduced by -25%.", "DarkVision", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "BlastOfAgony",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "summon_wraith" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            _unhallowedSoulKeystone.Initialize(CareerID, "Blast of Agony is now 'Target Seeking', and its damage is increased by 25%.", "UnhallowedSoul", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_blastofagony",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => (int)originalValue*1.25f,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "BlastOfAgony",
                        PropertyName = "SeekerParameters",
                        PropertyValue = (choice, originalValue, agent) =>
                        {
                            var seeker = new SeekerParameters();
                            seeker.Derivative = 0;
                            seeker.Proportional = 0.5f;
                            seeker.DisableDistance = 2f;
                            return seeker;
                        },
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "BlastOfAgony",
                        PropertyName = "CrosshairType",
                        PropertyValue = (choice, originalValue, agent) =>CrosshairType.SingleTarget,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "BlastOfAgony",
                        PropertyName = "AbilityTargetType",
                        PropertyValue = (choice, originalValue, agent) =>AbilityTargetType.EnemiesInAOE,
                        MutationType = OperationType.Replace
                    },

                });
            _hungerForKnowledgeKeystone.Initialize(CareerID, "Blast of Agony passively charges throughout the battle.", "HungerForKnowledge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {

                });
            _wellspringOfDharKeystone.Initialize(CareerID, "Blast of Agony also scales with Medicine, starts charged, and can be charged by Companions.", "WellspringOfDhar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_blastofagony",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Medicine}, 0.01f),
                        MutationType = OperationType.Add
                    }
                });

            _everlingsSecretKeystone.Initialize(CareerID, "After the initial cast, Blast of Agony can be cast again for a short time.", "EverlingsSecret", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>());
        }

        protected override void InitializePassives()
        {


            _discipleOfAccursedPassive1.Initialize(CareerID, "-50% party weight of 'Lesser Undead' troops.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.UnitPartyWeight, false, characterObject => characterObject.IsUndead() && !characterObject.IsGhost()));
            _discipleOfAccursedPassive2.Initialize(CareerID, "-10% personal 'Winds of Magic' cost for spells.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.WindsCostReduction, true));
            _discipleOfAccursedPassive3.Initialize(CareerID, "+7 'Dark Energy' daily.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(7, PassiveEffectType.CustomResourceGain));
            _discipleOfAccursedPassive4.Initialize(CareerID, "+2 personal 'Winds of Magic' capacity per equipped 'Enchantment'.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2));

            _witchSightPassive1.Initialize(CareerID, "+20% spotting range of party on campaign map.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartySpottingRange, true));
            _witchSightPassive2.Initialize(CareerID, "+5% personal 'Spell' damage.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell));
            _witchSightPassive3.Initialize(CareerID, "+25% personal 'Spell Resistance'.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 25), AttackTypeMask.Spell));
            _witchSightPassive4.Initialize(CareerID, "+20 personal 'Winds of Magic' if armour weight does not exceed 11.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.WindsOfMagic, false,
                (characterObject => Hero.MainHero.BattleEquipment.GetTotalWeightOfArmor(true) < 11f)));

            _darkVisionPassive1.Initialize(CareerID, "+10 personal 'Winds of Magic' capacity.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _darkVisionPassive2.Initialize(CareerID, "-35% 'Dark Energy' upkeep for 'Spectral' troops.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.CustomResourceUpkeepModifier, true,
                characterObject => characterObject.IsGhost()));
            _darkVisionPassive3.Initialize(CareerID, "Dealing damage with spells now gives Roguery experience.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
            _darkVisionPassive4.Initialize(CareerID, "+1 personal 'Winds of Magic' capacity per known spell.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special, false));

            _unhallowedSoulPassive1.Initialize(CareerID, "+20% duration for 'Augment' spells.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.BuffDuration, true));
            _unhallowedSoulPassive2.Initialize(CareerID, "Defiling a shrine yeilds increased 'Dark Energy', and can summon 'Spectral' troops.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _unhallowedSoulPassive3.Initialize(CareerID, "+15% personal 'Spell Power' if armour weight does not exceed 11.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SpellEffectiveness, true,
                (characterObject => Hero.MainHero.BattleEquipment.GetTotalWeightOfArmor(true) < 11f)));
            _unhallowedSoulPassive4.Initialize(CareerID, "+5% personal 'Lighting' spell damage.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 5), AttackTypeMask.Spell));

            _hungerForKnowledgePassive1.Initialize(CareerID, "+20% duration of 'Hex' spells.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.DebuffDuration, true));
            _hungerForKnowledgePassive2.Initialize(CareerID, "'Spectral' troops are immune to friendly 'Spell' damage.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 100), AttackTypeMask.Spell,
                (attacker, victim, mask) => mask == AttackTypeMask.Spell && attacker.BelongsToMainParty() && victim.BelongsToMainParty() && victim.Character.IsGhost()));
            _hungerForKnowledgePassive3.Initialize(CareerID, "-20% cost for 'Enchantments'.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EnchantmentCostReduction, true));
            _hungerForKnowledgePassive4.Initialize(CareerID, "+5% personal 'Magic' damage for spells and melee.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee || mask == AttackTypeMask.Spell));

            _wellspringOfDharPassive1.Initialize(CareerID, "+5% personal 'Fire' spell damage.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 5), AttackTypeMask.Spell));
            _wellspringOfDharPassive2.Initialize(CareerID, "+20% chance tier 4+ 'Lesser Undead' troops will be wounded instead of killed.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true));
            _wellspringOfDharPassive3.Initialize(CareerID, "+15 'Winds of Magic' capacity for Necromancer Companions.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, false)); //I'm not sure if this makes sufficient the distinction between necromancers (the companion carrying that name) and Necromancer (anyone who knows the lore) as the latter is what is checked.
            _wellspringOfDharPassive4.Initialize(CareerID, "+0.1 personal 'Winds of Magic' recharge rate per Companion that uses spells.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.1f, PassiveEffectType.Special, false));

            _everlingsSecretPassive1.Initialize(CareerID, "+1 personal 'Winds of Magic' regeneration.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.WindsRegeneration));
            _everlingsSecretPassive2.Initialize(CareerID, "+35% personal spell cooldown reduction.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.WindsCooldownReduction, true));
            _everlingsSecretPassive3.Initialize(CareerID, "When at max 'Winds of Magic', excess generated becomes 'Dark Energy'.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _everlingsSecretPassive4.Initialize(CareerID, "All forms of 'Spell' damage buffs now applies to every lore of magic.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
        }

        public override void InitialCareerSetup()
        {
            var playerHero = Hero.MainHero;

            playerHero.ClearPerks();
            //this should probably be something like a call to ClearPerksForSkill(faith) which would trigger the normal cascade of calls to clear bonuses
            playerHero.SetSkillValue(TORSkills.Faith, 0);
            var toRemoveFaith = Hero.MainHero.HeroDeveloper.GetFocus(TORSkills.Faith);
            Hero.MainHero.HeroDeveloper.RemoveFocus(TORSkills.Faith, toRemoveFaith);

            playerHero.HeroDeveloper.UnspentFocusPoints += toRemoveFaith;

            if (playerHero.HasAttribute("Priest"))//only sigmar/ulric priests have this attribute, but they also have a Priest(God) attribute that isn't removed here and will still return IsPriest() == true
            {
                CareerHelper.RemovePriestAttributes(playerHero);
                playerHero.GetExtendedInfo().RemoveAllPrayers();
            }

            if (playerHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                CultureObject mousillonCulture = MBObjectManager.Instance.GetObject<CultureObject>("mousillon");
                Hero.MainHero.Culture = mousillonCulture;
            }

            if (playerHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
            {
                CultureObject sylvaniaCulture = MBObjectManager.Instance.GetObject<CultureObject>(TORConstants.Cultures.SYLVANIA);
                Hero.MainHero.Culture = sylvaniaCulture;
            }

            // Become hostile to Human pantheon religions (your former faith)
            var religions = ReligionObject.All.FindAll(x => x.Pantheon == Pantheon.Human);
            foreach (var religion in religions)
            {
                Hero.MainHero.AddReligiousInfluence(religion, -100, true);
            }

            ReligionObject nagash = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash");
            if (nagash != null)
            {
                Hero.MainHero.AddReligiousInfluence(nagash, 25, true);
            }

            List<string> allowedLores = new List<string>() { "MinorMagic", "Necromancy", "DarkMagic", "LoreOfMetal", "LoreOfHeavens", "LoreOfDeath", "LoreOfFire", "LoreOfBeasts" };

            foreach (var lore in LoreObject.GetAll())
            {
                if (allowedLores.Contains(lore.ID))
                    continue;

                Hero.MainHero.GetExtendedInfo().RemoveKnownLore(lore.ID);
            }

            Hero.MainHero.GetExtendedInfo().RemoveAllSpells();

            Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("necrarch");
            var equipment = Hero.MainHero.CharacterObject.Equipment;
            var properties = Hero.MainHero.CharacterObject.GetBodyProperties(equipment);
            Hero.MainHero.CharacterObject.UpdatePlayerCharacterBodyProperties(properties, FaceGen.GetRaceOrDefault("necrarch"), false);

            var skill = Hero.MainHero.GetSkillValue(TORSkills.Spellcraft);
            Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, Math.Max(skill, 25));

            Hero.MainHero.AddKnownLore("Necromancy");
            Hero.MainHero.AddAbility("SummonSkeleton");
            Hero.MainHero.AddKnownLore("MinorMagic");
            Hero.MainHero.AddAbility("Dart");

            Hero.MainHero.AddAttribute("Necromancer");
            Hero.MainHero.AddAttribute("SpellCaster");


            var becameNecrarchText = TORTextHelper.GetTextObject("tor_became_necrarch_text", "{HERO_NAME} became a Necrarch");
            becameNecrarchText.SetTextVariable("HERO_NAME", Hero.MainHero.Name);
            MBInformationManager.AddQuickInformation(becameNecrarchText, 0, CharacterObject.PlayerCharacter);
        }


        private static bool HeroArmorWeightUndershootCheck(Agent agent)
        {
            if (!agent.BelongsToMainParty()) return false;
            if (!agent.IsMainAgent) return false;
            var weight = agent.Character.Equipment.GetTotalWeightOfArmor(true);
            return weight <= 11;
        }
    }
}