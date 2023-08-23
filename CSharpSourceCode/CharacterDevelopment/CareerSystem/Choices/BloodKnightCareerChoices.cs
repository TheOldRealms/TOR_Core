using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;
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
            _bloodKnightRoot.Initialize(CareerID, "The Blood Knight is channeling focus and rage towards the enemies. Damage increased by 45% and physical resistance by 10% for the next 6 seconds. Both bonuses increase with the skill of the equipped weapon by 0.05% per point.", null, true,
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
            _bladeMasterKeystone.Initialize(CareerID, "All melee weapon skills, wielded or not, count towards the career ability effects.", "BladeMaster", false,
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
            _peerlessWarriorKeystone.Initialize(CareerID, "Career Ability scales with Athletics. Speed increased by 20% when the ability is active.", "PeerlessWarrior", false,
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
            _doomRiderKeystone.Initialize(CareerID, "The Career Ability scales with Tactics. Nearby troops receive the Red Fury buff.", "DoomRider", false,
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
            _controlledHungerKeyStone.Initialize(CareerID, "The duration of Red Fury is doubled.", "ControlledHunger", false,
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

            _avatarOfDeathKeystone.Initialize(CareerID, "Red Fury also increases attack speed. Scales the same as base ability.", "AvatarOfDeath", false,
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
            _dreadKnightKeystone.Initialize(CareerID, "Red Fury effect scales with Riding and its resistance effect is now Ward save. Recharges faster.", "DreadKnight", false,
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
            _peerlessWarriorPassive2.Initialize(CareerID, "Extra melee damage (10%).", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _peerlessWarriorPassive3.Initialize(CareerID, "Every troop of Tier 4 and above gains an extra 20% exp for kills.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _peerlessWarriorPassive4.Initialize(CareerID, "You gain 100 exp in one of the melee combat skills at random every day.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, false));  // CareerChoicePerkCampaignBehavior 123

            _bladeMasterPassive1.Initialize(CareerID, "20% extra melee damage.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _bladeMasterPassive2.Initialize(CareerID, "Increases health regeneration after battles by 5.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.HealthRegeneration));
            _bladeMasterPassive3.Initialize(CareerID, "Hits below 15 damage will not stagger the player.", "BladeMaster", false, ChoiceType.Passive, null); // Agent extension 83,
            _bladeMasterPassive4.Initialize(CareerID, "All troops, the player included, gain exp when raiding villages.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorRaidModel 23  AND TorCareerPerkCampaignBehavior 73


            _doomRiderPassive1.Initialize(CareerID, "20% extra melee damage.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee));
            _doomRiderPassive2.Initialize(CareerID, "Increases Hitpoints by 50.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _doomRiderPassive3.Initialize(CareerID, "Party speed increases by 2.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2.0f, PassiveEffectType.PartyMovementSpeed));
            _doomRiderPassive4.Initialize(CareerID, "Recruit defeated units as Blood Knights with a chance of 15% (40% for Tier >4).", "DoomRider", false, ChoiceType.Passive, null);

            _controlledHungerPassive1.Initialize(CareerID, "Immune to sunlight malus.", "ControlledHunger", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46
            _controlledHungerPassive2.Initialize(CareerID, "Increases Hitpoints by 50.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _controlledHungerPassive3.Initialize(CareerID, "Mount health is increased by 35%.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(35f, PassiveEffectType.HorseHealth, true));
            _controlledHungerPassive4.Initialize(CareerID, "Ward save for all vampire units.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));

            _avatarOfDeathPassive1.Initialize(CareerID, "Gain 25% physical resistance to melee and ranged attacks.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 25), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _avatarOfDeathPassive2.Initialize(CareerID, "All vampire units wages are reduced by 35%.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.Special, true)); //TORPartyWageModel 85
            _avatarOfDeathPassive3.Initialize(CareerID, "The player gains 35% Magic resistance against spells.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 35), AttackTypeMask.Spell));
            _avatarOfDeathPassive4.Initialize(CareerID, "Attacks deal bonus damage against shields.", "AvatarOfDeath", false, ChoiceType.Passive, null);

            _dreadKnightPassive1.Initialize(CareerID, "Increases Hitpoints by 50.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _dreadKnightPassive2.Initialize(CareerID, "Horse charge damage is increased by 50%.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseChargeDamage, true));
            _dreadKnightPassive3.Initialize(CareerID, "Cavalry units get a 20% damage increase in damage.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _dreadKnightPassive4.Initialize(CareerID, "Extra 25% armor penetration of melee attacks.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        }
        
        public override void InitialCareerSetup()
        {
            var playerHero = Hero.MainHero;
            
            playerHero.ClearPerks();
            playerHero.SetSkillValue(TORSkills.Faith, 0);
            var toRemoveFaith= Hero.MainHero.HeroDeveloper.GetFocus(TORSkills.Faith);
            playerHero.HeroDeveloper.RemoveFocus(TORSkills.Faith,toRemoveFaith);
            playerHero.HeroDeveloper.UnspentFocusPoints += toRemoveFaith;
            
            if (playerHero.HasAttribute("Priest"))
            {
                playerHero.RemoveAttribute("Priest");
                playerHero.GetExtendedInfo().RemoveAllPrayers();
            }

            Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 99);
            
            

            playerHero.SetSkillValue(TORSkills.SpellCraft,0);
            var toRemoveSpellcraft= Hero.MainHero.HeroDeveloper.GetFocus(TORSkills.SpellCraft);
            playerHero.HeroDeveloper.RemoveFocus(TORSkills.SpellCraft,toRemoveSpellcraft);
            playerHero.HeroDeveloper.UnspentFocusPoints += toRemoveSpellcraft;
            
            foreach (var lore in LoreObject.GetAll())
            {
                playerHero.GetExtendedInfo().RemoveKnownLore(lore.ID);
            }

            playerHero.GetExtendedInfo().RemoveAllSpells();

            var race = FaceGen.GetRaceOrDefault("vampire");
            Hero.MainHero.CharacterObject.Race = race;
            
            Hero.MainHero.AddAttribute("Necromancer");

            playerHero.RemoveAttribute("SpellCaster");
            
            MBInformationManager.AddQuickInformation(new TextObject(Hero.MainHero.Name+" became a Blood Knight Vampire"), 0, CharacterObject.PlayerCharacter);
        }
    }
}