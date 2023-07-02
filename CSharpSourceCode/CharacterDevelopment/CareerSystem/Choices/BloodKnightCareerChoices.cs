﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class BloodKnightCareerChoices : TORCareerChoicesBase
    {
        public BloodKnightCareerChoices(CareerObject id) : base(id) {}

        
        private CareerChoiceObject _bloodKnightRoot;

        private CareerChoiceObject _peerlessWarriorKeystone;
        private CareerChoiceObject _peerlessWarriorPassive1;
        private CareerChoiceObject _peerlessWarriorPassive2;
        private CareerChoiceObject _peerlessWarriorPassive3;
        private CareerChoiceObject _peerlessWarriorPassive4;

        private CareerChoiceObject _bladeMasterKeystone;
        private CareerChoiceObject _bladeMasterPassive1;
        private CareerChoiceObject _bladeMasterPassive2;
        private CareerChoiceObject _bladeMasterPassive3;
        private CareerChoiceObject _bladeMasterPassive4;

        private CareerChoiceObject _doomRiderKeystone;
        private CareerChoiceObject _doomRiderPassive1;
        private CareerChoiceObject _doomRiderPassive2;
        private CareerChoiceObject _doomRiderPassive3;
        private CareerChoiceObject _doomRiderPassive4;

        private CareerChoiceObject _controlledHungerKeyStone;
        private CareerChoiceObject _controlledHungerPassive1;
        private CareerChoiceObject _controlledHungerPassive2;
        private CareerChoiceObject _controlledHungerPassive3;
        private CareerChoiceObject _controlledHungerPassive4;

        private CareerChoiceObject _avatarOfDeathKeystone;
        private CareerChoiceObject _avatarOfDeathPassive1;
        private CareerChoiceObject _avatarOfDeathPassive2;
        private CareerChoiceObject _avatarOfDeathPassive3;
        private CareerChoiceObject _avatarOfDeathPassive4;

        private CareerChoiceObject _dreadKnightKeystone;
        private CareerChoiceObject _dreadKnightPassive1;
        private CareerChoiceObject _dreadKnightPassive2;
        private CareerChoiceObject _dreadKnightPassive3;
        private CareerChoiceObject _dreadKnightPassive4;


        protected override void RegisterAll()
        {
            _bloodKnightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BloodKnightRoot"));

            _peerlessWarriorKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorKeystone"));
            _peerlessWarriorPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive1"));
            _peerlessWarriorPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive2"));
            _peerlessWarriorPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive3"));
            _peerlessWarriorPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive4"));

            _bladeMasterKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterKeystone"));
            _bladeMasterPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive1"));
            _bladeMasterPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive2"));
            _bladeMasterPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive3"));
            _bladeMasterPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive4"));

            _doomRiderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderKeystone"));
            _doomRiderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive1"));
            _doomRiderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive2"));
            _doomRiderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive3"));
            _doomRiderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive4"));

            _controlledHungerKeyStone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerKeystone"));
            _controlledHungerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive1"));
            _controlledHungerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive2"));
            _controlledHungerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive3"));
            _controlledHungerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive4"));

            _avatarOfDeathKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathKeystone"));
            _avatarOfDeathPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive1"));
            _avatarOfDeathPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive2"));
            _avatarOfDeathPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive3"));
            _avatarOfDeathPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive4"));

            _dreadKnightKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightKeystone"));
            _dreadKnightPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive1"));
            _dreadKnightPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive2"));
            _dreadKnightPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive3"));
            _dreadKnightPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _bloodKnightRoot.Initialize(CareerID, "The Blood Knight is channeling focus and rage towards the enemies. For the next 6 seconds the melee damage is increased by 45%, physical resistance for 10%. Based on the wielded Weaponskill,for both, the ability strength is increased by 0.05%", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, true),
                        MutationType = OperationType.Add
                    }
                });
            _bladeMasterKeystone.Initialize(CareerID, "All melee weapon skills irrespective if the weapon is wielded or not, are counted towards the career ability modification.", "BladeMaster", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),      //Remove previous effect and replace it with new "adding of all weapon perks.
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => - CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => -  CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => - CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    /*new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "Rotation",
                        PropertyValue = (choice, originalValue, agent) => true,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "Rotatio nSpeed",
                        PropertyValue = (choice, originalValue, agent) => 400,
                        MutationType = OperationType.Replace
                    },*/
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "ParticleId",
                        PropertyValue = (choice, originalValue, agent) => "redfury_rage_2",
                        MutationType = OperationType.Replace
                    },
                    
                });
            _peerlessWarriorKeystone.Initialize(CareerID, "Athletics is counted towards the Career Ability. Movement speed is 20 increased during ability", "PeerlessWarrior", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Athletics }, 0.0005f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Athletics }, 0.0005f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Athletics }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_red_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "redfury_effect_mov" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            _doomRiderKeystone.Initialize(CareerID, "Tactics is counted towards Career Ability. Units in proximity will also receive the red fury buff", "DoomRider", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Tactics }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Tactics }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Tactics }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RedFury",
                        PropertyName = "AbilityTargetType",
                        PropertyValue = (choice, originalValue, agent) => AbilityTargetType.AlliesInAOE,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_red_fury",
                        PropertyName = "TargetType",
                        PropertyValue = (choice, originalValue, agent) => TargetType.Friendly,
                        MutationType = OperationType.Replace
                    }
                });
            _controlledHungerKeyStone.Initialize(CareerID, "Duration of Red Fury is doubled", "ControlledHunger", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RedFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => originalValue,
                        MutationType = OperationType.Add
                    }
                });

            _avatarOfDeathKeystone.Initialize(CareerID, "Red Fury also increases attack speed. Scales with skills like base effects.", "AvatarOfDeath", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_red_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "redfury_effect_ats" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            _dreadKnightKeystone.Initialize(CareerID, "Riding skill is counted towards Career Ability. Red Fury resistance effect is now Wardsave. Ability is charged faster.", "DreadKnight", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>() { DefaultSkills.Riding }, 0.0005f, false, false),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "DamageType",
                        PropertyValue = (choice, originalValue, agent) => DamageType.All,
                        MutationType = OperationType.Replace
                    }
                }, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true));
        }

        protected override void InitializePassives()
        {
            _peerlessWarriorPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _peerlessWarriorPassive2.Initialize(CareerID, "Extra melee Damage(10%).", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _peerlessWarriorPassive3.Initialize(CareerID, " For every Troop tier 4 and above, the gained XP is increased by 20% for kills.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _peerlessWarriorPassive4.Initialize(CareerID, "Everyday you gain randomly 100 xp in one of the melee combat skills", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, false));  // CareerChoicePerkCampaignBehavior 123

            _bladeMasterPassive1.Initialize(CareerID, "Extra melee Damage(20%).", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _bladeMasterPassive2.Initialize(CareerID, "Increases health regeneration after battles by 5.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.HealthRegeneration));
            _bladeMasterPassive3.Initialize(CareerID, "Hits below 15 damage will not stagger character.", "BladeMaster", false, ChoiceType.Passive, null); // Agent extension 83,
            _bladeMasterPassive4.Initialize(CareerID, "All troops are gaining XP(including Player) while raiding villages", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorRaidModel 23  AND TorCareerPerkCampaignBehavior 73


            _doomRiderPassive1.Initialize(CareerID, "Extra melee Damage(20%).", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee));
            _doomRiderPassive2.Initialize(CareerID, "Increases Hitpoints by 50.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _doomRiderPassive3.Initialize(CareerID, "Partyspeed Increased by 2", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2.0f, PassiveEffectType.PartyMovementSpeed));
            _doomRiderPassive4.Initialize(CareerID, "Defeated Units can be recruited with 15%(40% for >4Tier) chance to blood knight initates", "DoomRider", false, ChoiceType.Passive, null);

            _controlledHungerPassive1.Initialize(CareerID, "immune to sunlight malus.", "ControlledHunger", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46
            _controlledHungerPassive2.Initialize(CareerID, "Increases hitpoints by 50.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _controlledHungerPassive3.Initialize(CareerID, "Mount health is increased by 35%", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(35f, PassiveEffectType.HorseHealth, true));
            _controlledHungerPassive4.Initialize(CareerID, "Wardsave for all vampire units.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));

            _avatarOfDeathPassive1.Initialize(CareerID, "25% Physical damage reduction from Melee and Ranged attacks.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 25), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _avatarOfDeathPassive2.Initialize(CareerID, "Vampire Wages are reduced by 35%.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.Special, true)); //TORPartyWageModel 85
            _avatarOfDeathPassive3.Initialize(CareerID, "35%. Magical Damage reduction from Spells", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 35), AttackTypeMask.Spell));
            _avatarOfDeathPassive4.Initialize(CareerID, "Attacks deal bonus damage against shields", "AvatarOfDeath", false, ChoiceType.Passive, null);

            _dreadKnightPassive1.Initialize(CareerID, "Increases hitpoints by 50.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _dreadKnightPassive2.Initialize(CareerID, "Horse charge damage is increased by 50%.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseChargeDamage, true));
            _dreadKnightPassive3.Initialize(CareerID, "Mounted units damage is increased by 20%", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _dreadKnightPassive4.Initialize(CareerID, "25% Melee Armor Penetration", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        }
    }
}