using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

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
            _vampireRoot.Initialize(CareerID, "The Vampire transforms into a fog, that is impossible to catch and can fly wherever the player wants. The Ability lasts for 5 seconds. The duration can be increased for each point in athletics by 0.03 seconds. The Mistform can be deactivated at will, by pressing attack.", null, true,
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

            _newBloodKeystone.Initialize(CareerID, "Ability needs 20% less damage to get charged. Flying Speed is increased by 20%.", "NewBlood", false, // very hardcoded. I will continue on that after release 
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));

            _lordlyKeystone.Initialize(CareerID, "Mistform heals 3 Healthpoints per second. Your wielded weapon is counted towards the ability length", "Lordly", false,
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

            _arkayneKeystone.Initialize(CareerID, "During the Mistform Winds of Magic is recharged (1/s). Spellcraft is counted towards the Mistform enhancement. Requires 40% additional damage infliction. ", "Arkayne", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_mistwalk",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "mistwalk_wom" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "ShadowStep",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { TORSkills.SpellCraft }, 0.03f),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect(-40, PassiveEffectType.Special, true)); //charge increase

            _courtleyKeystone.Initialize(CareerID, "Mistform is loaded on battle start. Roguery counts towards mistform enchancement", "Courtley", false,
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

            _martialleKeystone.Initialize(CareerID, "Swing speed is increased by 20%, StatusEffect are active after mistform. Requires 30% higher damage infliction.", "Martialle", false,
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
            
            _masterOfDeadKeystone.Initialize(CareerID, "You propergate all effects of your mist form to your souroundings. Requires 30% higher damage infliction.", "MasterOfDead", false,
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
            _newBloodPassive2.Initialize(CareerID, "10% Wardsave at night fights", "NewBlood", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));
            _newBloodPassive3.Initialize(CareerID, "Increases health regeneration after battles by 5.", "NewBlood", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.HealthRegeneration));
            _newBloodPassive4.Initialize(CareerID, "Immune to sunlight speed malus", "NewBlood", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46

            _arkaynePassive1.Initialize(CareerID, "Increases Winds of Magic by 10.", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _arkaynePassive2.Initialize(CareerID, "Extra Magical Spell Damage(10%).", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
            _arkaynePassive3.Initialize(CareerID, "Extra Magical melee Damage(10%).", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee));
            _arkaynePassive4.Initialize(CareerID, "10% Cost reduction for spells", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsCostReduction, true));

            _courtleyPassive1.Initialize(CareerID, "Increases positive relationship gains by 20%", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorDiplomacy model 23
            _courtleyPassive2.Initialize(CareerID, "Increases Hitpoints by 25.", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _courtleyPassive3.Initialize(CareerID, "Extra Magical Spell and Ranged Damage(10%).", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell | AttackTypeMask.Ranged));
            _courtleyPassive4.Initialize(CareerID, "Killing Blows targeted to the Head replenish 3 Winds of Magic", "Courtley", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3)); //CareerPerkMissionBehavior 28

            _lordlyPassive1.Initialize(CareerID, "Partyspeed Increased by 1.5", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.5f, PassiveEffectType.PartyMovementSpeed));
            _lordlyPassive2.Initialize(CareerID, "Increases Hitpoints by 25.", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _lordlyPassive3.Initialize(CareerID, "Wages for Vampires are reduced by 15%", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.Special, true)); //TORPartyWageModel 85
            _lordlyPassive4.Initialize(CareerID, "Upgrade costs are halfed", "Lordly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-0.5f, PassiveEffectType.TroopUpgradeCost));

            _martiallePassive1.Initialize(CareerID, "Extra Melee Damage(10%).", "Martialle", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _martiallePassive2.Initialize(CareerID, "Increases Hitpoints by 25.", "Martialle", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _martiallePassive3.Initialize(CareerID, "10% Extra damage for all troops against humans", "Martialle", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true)); //68 Damage patch
            _martiallePassive4.Initialize(CareerID, "All attacks deal bonus damage against shields", "Martialle", false, ChoiceType.Passive, null); // TorAgentApplyDamageModel 83

            _masterOfDeadPassive1.Initialize(CareerID, "100 XP every day for undead units in party", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, false)); //CareerPerkCampaign Behavior 101
            _masterOfDeadPassive2.Initialize(CareerID, "20% Higher chance for raised dead after battle", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); // HeroExtension 44
            _masterOfDeadPassive3.Initialize(CareerID, "Undead units get 25% Wardsave Resistance", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); // AgentstatCalculator 444, Might be OP , I had fun, i would leave it for the playtest, can be adjusted
            _masterOfDeadPassive4.Initialize(CareerID, "Tier 4 Undead troops can get 'wounded' with a 20% lower chance", "MasterOfDead", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true)); //HealingpartyModel 33
        }
        
        public override void InitialCareerSetup()
        {
            var playerHero = Hero.MainHero;
            
            playerHero.ClearPerks();
            playerHero.SetSkillValue(TORSkills.Faith, 0);
            var toRemoveFaith= Hero.MainHero.HeroDeveloper.GetFocus(TORSkills.Faith);
            Hero.MainHero.HeroDeveloper.RemoveFocus(TORSkills.Faith,toRemoveFaith);

            if (playerHero.HasAttribute("Priest"))
            {
                playerHero.RemoveAttribute("Priest");
                playerHero.GetExtendedInfo().RemoveAllPrayers();
               
            }
            
            List<string> allowedLores = new List<string>() { "MinorMagic", "Necromancy", "DarkMagic" };
            foreach (var lore in LoreObject.GetAll())
            {
                if(allowedLores.Contains(lore.ID))
                    continue;
                
                Hero.MainHero.GetExtendedInfo().RemoveKnownLore(lore.ID);
            }
            
            Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 99);
            
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
        }
        
        protected override void UnlockCareerBenefitsTier2()
        {
            Hero.MainHero.AddKnownLore("DarkMagic");
        }
    }
}