using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
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
    public class VampireCountCareerChoices : TORCareerChoicesBase
    {
        public VampireCountCareerChoices(CareerObject id) : base(id)
        {
        }

        private CareerChoiceObject _vampireRoot;

        private CareerChoiceObject _newBloodKeystone;
        private CareerChoiceObject _newBloodPassive1;
        private CareerChoiceObject _newBloodPassive2;
        private CareerChoiceObject _newBloodPassive3;
        private CareerChoiceObject _newBloodPassive4;
        
        private CareerChoiceObject _feralKeystone;
        private CareerChoiceObject _feralPassive1;
        private CareerChoiceObject _feralPassive2;
        private CareerChoiceObject _feralPassive3;
        private CareerChoiceObject _feralPassive4;

        private CareerChoiceObject _arkayneKeystone;
        private CareerChoiceObject _arkaynePassive1;
        private CareerChoiceObject _arkaynePassive2;
        private CareerChoiceObject _arkaynePassive3;
        private CareerChoiceObject _arkaynePassive4;

        private CareerChoiceObject _courtleyKeystone;
        private CareerChoiceObject _courtleyPassive1;
        private CareerChoiceObject _courtleyPassive2;
        private CareerChoiceObject _courtleyPassive3;
        private CareerChoiceObject _courtleyPassive4;

        private CareerChoiceObject _lordlyKeystone;
        private CareerChoiceObject _lordlyPassive1;
        private CareerChoiceObject _lordlyPassive2;
        private CareerChoiceObject _lordlyPassive3;
        private CareerChoiceObject _lordlyPassive4;

        private CareerChoiceObject _martialleKeystone;
        private CareerChoiceObject _martiallePassive1;
        private CareerChoiceObject _martiallePassive2;
        private CareerChoiceObject _martiallePassive3;
        private CareerChoiceObject _martiallePassive4;

        private CareerChoiceObject _masterOfDeadKeystone;
        private CareerChoiceObject _masterOfDeadPassive1;
        private CareerChoiceObject _masterOfDeadPassive2;
        private CareerChoiceObject _masterOfDeadPassive3;
        private CareerChoiceObject _masterOfDeadPassive4;

        protected override void RegisterAll()
        {
            _vampireRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VampireCountRoot"));

            _newBloodKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NewBloodKeystone"));
            _newBloodPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NewBloodPassive1"));
            _newBloodPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NewBloodPassive2"));
            _newBloodPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NewBloodPassive3"));
            _newBloodPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NewBloodPassive4"));
            
            _feralKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeralKeystone"));
            _feralPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeralPassive1"));
            _feralPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeralPassive2"));
            _feralPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeralPassive3"));
            _feralPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeralPassive4"));

            _arkayneKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArkayneKeystone"));
            _arkaynePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArkaynePassive1"));
            _arkaynePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArkaynePassive2"));
            _arkaynePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArkaynePassive3"));
            _arkaynePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArkaynePassive4"));

            _courtleyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CourtleyKeystone"));
            _courtleyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CourtleyPassive1"));
            _courtleyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CourtleyPassive2"));
            _courtleyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CourtleyPassive3"));
            _courtleyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CourtleyPassive4"));

            _lordlyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LordlyKeystone"));
            _lordlyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LordlyPassive1"));
            _lordlyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LordlyPassive2"));
            _lordlyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LordlyPassive3"));
            _lordlyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LordlyPassive4"));

            _martialleKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MartialleKeystone"));
            _martiallePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MartiallePassive1"));
            _martiallePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MartiallePassive2"));
            _martiallePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MartiallePassive3"));
            _martiallePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MartiallePassive4"));

            _masterOfDeadKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterOfDeadKeystone"));
            _masterOfDeadPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterOfDeadPassive1"));
            _masterOfDeadPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterOfDeadPassive2"));
            _masterOfDeadPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterOfDeadPassive3"));
            _masterOfDeadPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterOfDeadPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _vampireRoot.Initialize(CareerID, "The player transforms into fog, which is impossible to catch and can fly in every direction. The ability lasts 5 seconds and the duration increases by 0.03 seconds for each point in Athletics. Mist Form can be deactivated at will by pressing attack.", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ShadowStep",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Athletics }, 0.03f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "mistwalk_base" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            
            _newBloodKeystone.Initialize(CareerID, "Ability needs 20% less damage to get charged.", "NewBlood", false, 
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            
            _feralKeystone.Initialize(CareerID, "Mistform speed is increased by 20%.", "Feral", false, 
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));

            _lordlyKeystone.Initialize(CareerID, "Mist Form heals 3 HP per second. The ability duration scales with the wielded weapon skill.", "Lordly", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "mistwalk_healing" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Athletics }, 0.03f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ShadowStep",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.03f, false, true),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //charge reduction

            _arkayneKeystone.Initialize(CareerID, "Mistform scales with Spellcraft. Spell damage can charge ability with 90% reduced power.", "Arkayne", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ShadowStep",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { TORSkills.SpellCraft }, 0.03f),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect(0));

            _courtleyKeystone.Initialize(CareerID, "Mist Form is recharged on battle start and now also scales with Roguery.", "Courtley", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ShadowStep",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Roguery }, 0.03f),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special)); // cool down is reset on beginning

            _martialleKeystone.Initialize(CareerID, "+20% swing speed. Buffs are active after Mist Form (requires extra 30% damage dealt).", "Martialle", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "mistwalk_swingspeed" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => 10f,
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.Special, true)); //charge increase
            
            _masterOfDeadKeystone.Initialize(CareerID, "All buffs are propagated while remaining in Mist Form (requires extra 30% damage dealt).", "MasterOfDead", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ShadowStep",
                        PropertyName = "AbilityTargetType",
                        PropertyValue = (choice, originalValue, agent) => AbilityTargetType.AlliesInAOE,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "TargetType",
                        PropertyValue = (choice, originalValue, agent) => TargetType.Friendly,
                        MutationType = OperationType.Replace
                    }
                }, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.Special, true));
        }

        protected override void InitializePassives()
        {
            _newBloodPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "NewBlood", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _newBloodPassive2.Initialize(CareerID, "Increases Party size by 25.", "NewBlood", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.PartySize, false));
            _newBloodPassive3.Initialize(CareerID, "Increases max Winds of Magic by 15.", "NewBlood", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
            _newBloodPassive4.Initialize(CareerID, "Immune to sunlight speed malus.", "NewBlood", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46

            _feralPassive1.Initialize(CareerID, "10% extra melee damage.", "Feral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _feralPassive2.Initialize(CareerID, "Increases health regeneration on the campaign map by 3.", "Feral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.HealthRegeneration));
            _feralPassive3.Initialize(CareerID, "Party movement speed is increased by 1.5.", "Feral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.5f, PassiveEffectType.PartyMovementSpeed,false));
            _feralPassive4.Initialize(CareerID, "Increases Hitpoints by 25.", "Feral", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            
            _arkaynePassive1.Initialize(CareerID, "Armor weight doesn't affect winds recharge rate", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
            _arkaynePassive2.Initialize(CareerID, "Increases Magical spell damage by 10%.", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
            _arkaynePassive3.Initialize(CareerID, "10% extra Magical melee damage.", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee));
            _arkaynePassive4.Initialize(CareerID, "10% cost reduction for spells.", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.WindsCostReduction, true));

            _courtleyPassive1.Initialize(CareerID, "Increases positive Relation gains by 20%.", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorDiplomacy model 23
            _courtleyPassive2.Initialize(CareerID, "Increases Hitpoints by 25.", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _courtleyPassive3.Initialize(CareerID, "10% extra Magical spell and ranged damage.", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell | AttackTypeMask.Ranged));
            _courtleyPassive4.Initialize(CareerID, "Killing blows in the head replenish 3 Winds of Magic.", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3)); //CareerPerkMissionBehavior 28

            _lordlyPassive1.Initialize(CareerID, "Increases Companion Limit by 5.", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _lordlyPassive2.Initialize(CareerID, "Party size is increased by 75.", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(75, PassiveEffectType.PartySize));
            _lordlyPassive3.Initialize(CareerID, "All Vampire troops wages are reduced by 15%.", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.TroopWages, true, LordlyPassive3));
            _lordlyPassive4.Initialize(CareerID, "Upgrade costs are halved.", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-0.5f, PassiveEffectType.TroopUpgradeCost));

            _martiallePassive1.Initialize(CareerID, "10% extra melee damage.", "Martialle", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _martiallePassive2.Initialize(CareerID, "Increases Hitpoints by 50.", "Martialle", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _martiallePassive3.Initialize(CareerID, "All troops gain 10% extra damage against human enemies.", "Martialle", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical,10), AttackTypeMask.All, MartiallePassive3));
            _martiallePassive4.Initialize(CareerID, "All attacks deal bonus damage against shields.", "Martialle", false, ChoiceType.Passive, null); // TorAgentApplyDamageModel 83

            _masterOfDeadPassive1.Initialize(CareerID, "Increases Party size by 100.", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.PartySize));
            _masterOfDeadPassive2.Initialize(CareerID, "Undead troops gain 25% Ward save.", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 25), AttackTypeMask.All, MasterOfDeadPassive2));
            _masterOfDeadPassive3.Initialize(CareerID, "20% higher chance for raised dead after battle.", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); // HeroExtension 44
            _masterOfDeadPassive4.Initialize(CareerID, "Tier 4 Undead troops can get wounded with a 20% lower chance.", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true)); //HealingpartyModel 33
        }
        
        private static bool LordlyPassive3(CharacterObject characterObject)
        {
            if(characterObject.IsHero) return false;
            return characterObject.IsVampire();
        }
        
        private static bool MasterOfDeadPassive2(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            if (!victim.BelongsToMainParty()) return false;
            if (victim.IsHero) return false;
            return victim.IsUndead();
        }

        private static bool MartiallePassive3(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            return victim.Character.Race == 0||victim.Character.IsCultist(); //other humans should be added if applicable
        }
        
        public override void InitialCareerSetup()
        {
            var playerHero = Hero.MainHero;
            
            playerHero.ClearPerks();
            playerHero.SetSkillValue(TORSkills.Faith, 0);
            var toRemoveFaith= Hero.MainHero.HeroDeveloper.GetFocus(TORSkills.Faith);
            Hero.MainHero.HeroDeveloper.RemoveFocus(TORSkills.Faith,toRemoveFaith);
            
            playerHero.HeroDeveloper.UnspentFocusPoints += toRemoveFaith;

            if (playerHero.HasAttribute("Priest"))
            {
                playerHero.RemoveAttribute("Priest");
                playerHero.GetExtendedInfo().RemoveAllPrayers();
               
            }

            if (playerHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                CultureObject mousillonCulture= MBObjectManager.Instance.GetObject<CultureObject>(TORConstants.Cultures.MOUSILLON);
                Hero.MainHero.Culture = mousillonCulture;
            }
            
            if (playerHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
            {
                CultureObject sylvaniaCulture= MBObjectManager.Instance.GetObject<CultureObject>(TORConstants.Cultures.SYLVANIA);
                Hero.MainHero.Culture = sylvaniaCulture;
            }
            
            
            var religions = ReligionObject.All.FindAll(x => x.Affinity == ReligionAffinity.Order);

            foreach (var religion in religions)
            {
                Hero.MainHero.AddReligiousInfluence(religion,-100,true);
            }
            
            ReligionObject nagash= ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash");
            if (nagash != null)
            {
                Hero.MainHero.AddReligiousInfluence(nagash,25,true);
            }
            
            List<string> allowedLores = new List<string>() { "MinorMagic", "Necromancy", "DarkMagic" };
            
            foreach (var lore in LoreObject.GetAll())
            {
                if(allowedLores.Contains(lore.ID))
                    continue;
                
                Hero.MainHero.GetExtendedInfo().RemoveKnownLore(lore.ID);
            }

            Hero.MainHero.GetExtendedInfo().RemoveAllSpells();
            
            var race = FaceGen.GetRaceOrDefault("vampire");
            Hero.MainHero.CharacterObject.Race = race;
                
            var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
            Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
     
            Hero.MainHero.AddKnownLore("Necromancy");
            Hero.MainHero.AddAbility("SummonSkeleton");
            Hero.MainHero.AddKnownLore("MinorMagic");
            Hero.MainHero.AddAbility("Dart");
            
            Hero.MainHero.AddAttribute("Necromancer");
            Hero.MainHero.AddAttribute("SpellCaster");
            
            
            MBInformationManager.AddQuickInformation(new TextObject(Hero.MainHero.Name+" became a Vampire"), 0, CharacterObject.PlayerCharacter);
        }
        
        protected override void UnlockCareerBenefitsTier2()
        {
            Hero.MainHero.AddKnownLore("DarkMagic");
        }
    }
}