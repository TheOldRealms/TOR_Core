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
            _necrarchRoot.Initialize(CareerID, "Summoned directly from the realm of the dead and cast by the Necrarch, this projectile spell deals 80 damage. The Blast of Agony grows in size for every point put into the Spellcraft skill, it can also be modified with several upgrades from the Career tree. It is free of any winds of magic cost to cast.", null, true,
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

            _discipleOfAccursedKeystone.Initialize(CareerID, "Undead troops can charge career ability. Ability scales with Roguery", "DiscipleOfAccursed", false,
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

            _witchSightKeystone.Initialize(CareerID, "For every hit enemy gain 2.5% of your maximum Winds of magic", "WitchSight", false,
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
            _darkVisionKeystone.Initialize(CareerID, "Summons a Wraith on impact. Spell damage charge is increased by 25%", "DarkVision", false,
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
            _unhallowedSoulKeystone.Initialize(CareerID, "Increase impact damage of netherball by 25%. The Projectile is now target seeking", "UnhallowedSoul", false,
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
            _hungerForKnowledgeKeystone.Initialize(CareerID, "The Career ability is constantly slowly charged.", "HungerForKnowledge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {

                });
            _wellspringOfDharKeystone.Initialize(CareerID, "Ability scales with Medicine. Ability starts charged. Companions can charge ability.", "WellspringOfDhar", false,
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

            _everlingsSecretKeystone.Initialize(CareerID, "After using nether ball, a second use is available shortly after the first one.", "EverlingsSecret", false, ChoiceType.Passive);
        }

        protected override void InitializePassives()
        {


            _discipleOfAccursedPassive1.Initialize(CareerID, "Every 2 undead troops (excluding ghosts) gain 1 additional party slot.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.UnitPartyWeight, false, characterObject => characterObject.IsUndead() && !characterObject.IsGhost()));
            _discipleOfAccursedPassive2.Initialize(CareerID, "10% cost reduction for spells.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.WindsCostReduction, true));
            _discipleOfAccursedPassive3.Initialize(CareerID, "Gain 7 Dark Energy daily.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(7, PassiveEffectType.CustomResourceGain));
            _discipleOfAccursedPassive4.Initialize(CareerID, "For every magic trait on your battle equipment you increase your maximum winds by 2.", "DiscipleOfAccursed", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2));

            _witchSightPassive1.Initialize(CareerID, "The Spotting range of the party is increased by 20%.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartySpottingRange, true));
            _witchSightPassive2.Initialize(CareerID, "Increases Magic spell damage by 5%.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell));
            _witchSightPassive3.Initialize(CareerID, "Increases Magic resistance against spells by 25%.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 25), AttackTypeMask.Spell));
            _witchSightPassive4.Initialize(CareerID, "Increases max Winds of Magic by 20 if your armor weight undershoots 11 stones.", "WitchSight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.WindsOfMagic, false,
                (characterObject => Hero.MainHero.BattleEquipment.GetTotalWeightOfArmor(true) < 11f)));

            _darkVisionPassive1.Initialize(CareerID, "Increases max Winds of Magic by 10.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _darkVisionPassive2.Initialize(CareerID, "Reduce the Dark Energy upkeep for ghost troops by 35%.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.CustomResourceUpkeepModifier, true,
                characterObject => characterObject.IsGhost()));
            _darkVisionPassive3.Initialize(CareerID, "Spell damage increase roguery.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
            _darkVisionPassive4.Initialize(CareerID, "For every known spell your winds capacity rises by 1.", "DarkVision", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special, false));

            _unhallowedSoulPassive1.Initialize(CareerID, "Increase buff durations by 20%.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.BuffDuration, true));
            _unhallowedSoulPassive2.Initialize(CareerID, "Defiling shrines yields more Dark Energy and has a chance to summon wraiths.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _unhallowedSoulPassive3.Initialize(CareerID, "Increases Spell effectiveness by 15% if your armor weight undershoots 11 stones.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SpellEffectiveness, true,
                (characterObject => Hero.MainHero.BattleEquipment.GetTotalWeightOfArmor(true) < 11f)));
            _unhallowedSoulPassive4.Initialize(CareerID, "Increases lightning spell damage by 5%.", "UnhallowedSoul", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 5), AttackTypeMask.Spell));

            _hungerForKnowledgePassive1.Initialize(CareerID, "Increase hex durations by 20%.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.DebuffDuration, true));
            _hungerForKnowledgePassive2.Initialize(CareerID, "Wraiths are immune to friendly fire spell damage.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 100), AttackTypeMask.Spell,
                (attacker, victim, mask) => mask == AttackTypeMask.Spell && attacker.BelongsToMainParty() && victim.BelongsToMainParty() && victim.Character.IsGhost()));
            _hungerForKnowledgePassive3.Initialize(CareerID, "Reduce costs for enchantments by 20%.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EnchantmentCostReduction, true));
            _hungerForKnowledgePassive4.Initialize(CareerID, "Increases Magic melee and spell damage by 5%.", "HungerForKnowledge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5), AttackTypeMask.Spell,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee || mask == AttackTypeMask.Spell));

            _wellspringOfDharPassive1.Initialize(CareerID, "Increases fire spell damage by 5%.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 5), AttackTypeMask.Spell));
            _wellspringOfDharPassive2.Initialize(CareerID, "Tier 4 Undead troops can get wounded with a 20% lower chance.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true));
            _wellspringOfDharPassive3.Initialize(CareerID, "Necromancy companions gain 15 more Winds of Magic.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, false)); //I'm not sure if this makes sufficient the distinction between necromancers (the companion carrying that name) and Necromancer (anyone who knows the lore) as the latter is what is checked.
            _wellspringOfDharPassive4.Initialize(CareerID, "Gain 0.1 Winds regeneration for every spellcasting companion.", "WellspringOfDhar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.1f, PassiveEffectType.Special, false));

            _everlingsSecretPassive1.Initialize(CareerID, "Increases Windsregeneration by 1.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.WindsRegeneration));
            _everlingsSecretPassive2.Initialize(CareerID, "35% spell cooldown reduction.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.WindsCooldownReduction, true));
            _everlingsSecretPassive3.Initialize(CareerID, "Once your Winds are full, your winds recharge rate is counted towards your Dark Energy.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _everlingsSecretPassive4.Initialize(CareerID, "Any non-physical damage count towards spell damage type.", "EverlingsSecret", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
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

            List<string> allowedLores = new List<string>() { "MinorMagic", "Necromancy", "DarkMagic", "LoreOfMetal", "LoreOfHeavens", "LoreOfDeath" };

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