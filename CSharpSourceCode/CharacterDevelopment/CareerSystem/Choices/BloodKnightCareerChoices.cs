using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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
    public class BloodKnightCareerChoices : TORCareerChoicesBase
    {
        public BloodKnightCareerChoices(CareerObject id) : base(id) {}
        
        private CareerChoiceObject _bloodKnightRoot;

        private CareerChoiceObject _peerlessWarriorKeystone;
        private CareerChoiceObject _peerlessWarriorPassive1;
        private CareerChoiceObject _peerlessWarriorPassive2;
        private CareerChoiceObject _peerlessWarriorPassive3;
        private CareerChoiceObject _peerlessWarriorPassive4;
        
        private CareerChoiceObject _nightRiderKeystone;
        private CareerChoiceObject _nightRiderPassive1;
        private CareerChoiceObject _nightRiderPassive2;
        private CareerChoiceObject _nightRiderPassive3;
        private CareerChoiceObject _nightRiderPassive4;

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
            
            _nightRiderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NightRiderKeystone"));
            _nightRiderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NightRiderPassive1"));
            _nightRiderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NightRiderPassive2"));
            _nightRiderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NightRiderPassive3"));
            _nightRiderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NightRiderPassive4"));

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
            _bloodKnightRoot.Initialize(CareerID, "The Blood Knight is channeling focus and rage towards the enemies. Damage increased by 45% and physical resistance by 10% for the next 6 seconds. Both bonuses increase with the skill of the equipped weapon by 0.05% per point. Requires 5 kills to recharge, +1 kill per a final perk picked up to max 10.", null, true,
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
            
            _nightRiderKeystone.Initialize(CareerID, "Companion Kills will count for career charge.","NightRider",false,ChoiceType.Keystone,new List<CareerChoiceObject.MutationObject>(),new CareerChoiceObject.PassiveEffect (0, PassiveEffectType.Special));
            
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
            _avatarOfDeathKeystone.Initialize(CareerID, "Red Fury's resistance is now Ward save. The ability grants a scaling attack speed boost.", "AvatarOfDeath", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_red_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "redfury_effect_ats" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect_res",
                        PropertyName = "DamageType",
                        PropertyValue = (choice, originalValue, agent) => DamageType.All,
                        MutationType = OperationType.Replace
                    }
                });
            _dreadKnightKeystone.Initialize(CareerID, "Red Fury scales with Riding and consecutive kills during ability increase duration by 2 sec each.", "DreadKnight", false,
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
                    }
                }, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true));
        }

        protected override void InitializePassives()
        {
            _peerlessWarriorPassive1.Initialize(CareerID, "{=peerless_warrior_passive1_str}Increases Hitpoints by 25.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _peerlessWarriorPassive2.Initialize(CareerID, "{=peerless_warrior_passive2_str}Extra melee damage (10%).", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _peerlessWarriorPassive3.Initialize(CareerID, "{=peerless_warrior_passive3_str}Every troop of Tier 4 and above gains an extra 20% exp for kills.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _peerlessWarriorPassive4.Initialize(CareerID, "{=peerless_warrior_passive4_str}You gain 100 exp in one of the melee combat skills at random every day.", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, false));  // CareerChoicePerkCampaignBehavior 123
            
            _nightRiderPassive1.Initialize(CareerID, "{=night_rider_passive1_str}Increases Hitpoints by 25.", "NightRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _nightRiderPassive2.Initialize(CareerID, "{=night_rider_passive2_str}All undead and vampires receive 20 points to their melee skills.", "NightRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special));
            _nightRiderPassive3.Initialize(CareerID, "{=night_rider_passive3_str}Raiding is at Night 50% faster.", "NightRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, true));
            _nightRiderPassive4.Initialize(CareerID, "{=night_rider_passive4_str}Attacks deal bonus damage against shields.", "NightRider", false, ChoiceType.Passive, null);
            
            _bladeMasterPassive1.Initialize(CareerID, "{=blade_master_passive1_str}20% extra melee damage.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _bladeMasterPassive2.Initialize(CareerID, "{=blade_master_passive2_str}Increases health regeneration after battles by 5.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.HealthRegeneration));
            _bladeMasterPassive3.Initialize(CareerID, "{=blade_master_passive3_str}Hits below 15 damage will not stagger the player.", "BladeMaster", false, ChoiceType.Passive, null); // Agent extension 83,
            _bladeMasterPassive4.Initialize(CareerID, "{=blade_master_passive4_str}All troops, the player included, gain exp when raiding villages.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorRaidModel 23  AND TorCareerPerkCampaignBehavior 73
            
            _doomRiderPassive1.Initialize(CareerID, "{=doom_rider_passive1_str}20% extra melee damage.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee));
            _doomRiderPassive2.Initialize(CareerID, "Reduce the Dark Energy upkeep for vampire troops by 10%.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.CustomResourceUpkeepModifier,true, 
                characterObject => !characterObject.IsHero && characterObject.IsVampire() && characterObject.IsKnightUnit())); 
            _doomRiderPassive3.Initialize(CareerID, "{=doom_rider_passive3_str}Party speed increases by 2.", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2.0f, PassiveEffectType.PartyMovementSpeed));
            _doomRiderPassive4.Initialize(CareerID, "{=doom_rider_passive4_str}Recruit defeated units as Blood Knights with a chance of 5% (10% for Tier >4).", "DoomRider", false, ChoiceType.Passive, null);

            _controlledHungerPassive1.Initialize(CareerID, "Immune to sunlight malus.", "ControlledHunger", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46
            _controlledHungerPassive2.Initialize(CareerID, "Increases Hitpoints by 50.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _controlledHungerPassive3.Initialize(CareerID, "Mount health is increased by 35%.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(35f, PassiveEffectType.HorseHealth, true));
            _controlledHungerPassive4.Initialize(CareerID, "For every 200 damage hit the player gets healed by 1 Hit point(Maximum 5).", "ControlledHunger", false, ChoiceType.Passive, null);

            _avatarOfDeathPassive1.Initialize(CareerID, "{=avatar_of_death_passive1_str}Gain 25% physical resistance to melee and ranged attacks.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 25), AttackTypeMask.Ranged | AttackTypeMask.Melee));
            _avatarOfDeathPassive2.Initialize(CareerID, "Reduce the Dark Energy upkeep for vampire troops by 20%.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.CustomResourceUpkeepModifier,true, 
                characterObject => !characterObject.IsHero && characterObject.IsVampire() && characterObject.IsKnightUnit())); 
            _avatarOfDeathPassive3.Initialize(CareerID, "{=avatar_of_death_passive3_str}The player gains 35% Magic resistance against spells.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 35), AttackTypeMask.Spell));
            _avatarOfDeathPassive4.Initialize(CareerID, "25% Ward save for all vampire units.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 25), AttackTypeMask.Spell, 
                (attacker, victim, mask) =>  !victim.BelongsToMainParty()&& victim.IsHero && victim.IsVampire()));
            
            _dreadKnightPassive1.Initialize(CareerID, "Increases Hitpoints by 75.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(75, PassiveEffectType.Health));
            _dreadKnightPassive2.Initialize(CareerID, "Horse charge damage is increased by 50%.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseChargeDamage, true));
            _dreadKnightPassive3.Initialize(CareerID, "Cavalry units get a 20% damage increase in damage.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && attacker.HasMount && mask== AttackTypeMask.Melee));
           
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
            
            if (playerHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                CultureObject mousillonCulture= MBObjectManager.Instance.GetObject<CultureObject>("mousillon");
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
            
            playerHero.SetSkillValue(TORSkills.SpellCraft,0);
            var toRemoveSpellcraft= Hero.MainHero.HeroDeveloper.GetFocus(TORSkills.SpellCraft); 
            
            var spendAttributePoints =Hero.MainHero.GetAttributeValue(TORAttributes.Discipline)-1;
            Hero.MainHero.HeroDeveloper.RemoveAttribute(TORAttributes.Discipline,spendAttributePoints);
            Hero.MainHero.HeroDeveloper.UnspentAttributePoints += spendAttributePoints;
            
            
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