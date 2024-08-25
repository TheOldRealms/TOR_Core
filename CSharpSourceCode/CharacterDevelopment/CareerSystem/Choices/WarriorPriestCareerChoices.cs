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
    public class WarriorPriestCareerChoices : TORCareerChoicesBase
    {
        public WarriorPriestCareerChoices(CareerObject id) : base(id) {}
        
        private CareerChoiceObject _warriorPriestRoot;

        private CareerChoiceObject _bookOfSigmarKeystone;
        private CareerChoiceObject _bookOfSigmarPassive1;
        private CareerChoiceObject _bookOfSigmarPassive2;
        private CareerChoiceObject _bookOfSigmarPassive3;
        private CareerChoiceObject _bookOfSigmarPassive4;

        private CareerChoiceObject _sigmarsProclaimerKeystone;
        private CareerChoiceObject _sigmarsProclaimerPassive1;
        private CareerChoiceObject _sigmarsProclaimerPassive2;
        private CareerChoiceObject _sigmarsProclaimerPassive3;
        private CareerChoiceObject _sigmarsProclaimerPassive4;

        private CareerChoiceObject _relentlessFanaticKeystone;
        private CareerChoiceObject _relentlessFanaticPassive1;
        private CareerChoiceObject _relentlessFanaticPassive2;
        private CareerChoiceObject _relentlessFanaticPassive3;
        private CareerChoiceObject _relentlessFanaticPassive4;

        private CareerChoiceObject _protectorOfTheWeakKeystone;
        private CareerChoiceObject _protectorOfTheWeakPassive1;
        private CareerChoiceObject _protectorOfTheWeakPassive2;
        private CareerChoiceObject _protectorOfTheWeakPassive3;
        private CareerChoiceObject _protectorOfTheWeakPassive4;

        private CareerChoiceObject _holyPurgeKeystone;
        private CareerChoiceObject _holyPurgePassive1;
        private CareerChoiceObject _holyPurgePassive2;
        private CareerChoiceObject _holyPurgePassive3;
        private CareerChoiceObject _holyPurgePassive4;

        private CareerChoiceObject _archLectorKeystone;
        private CareerChoiceObject _archLectorPassive1;
        private CareerChoiceObject _archLectorPassive2;
        private CareerChoiceObject _archLectorPassive3;
        private CareerChoiceObject _archLectorPassive4;
        
        private CareerChoiceObject _twinTailedCometKeystone;
        private CareerChoiceObject _twinTailedCometPassive1;
        private CareerChoiceObject _twinTailedCometPassive2;
        private CareerChoiceObject _twinTailedCometPassive3;
        private CareerChoiceObject _twinTailedCometPassive4;


        protected override void RegisterAll()
        {
            _warriorPriestRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarriorPriestRoot"));

            _bookOfSigmarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarKeyStone"));
            _bookOfSigmarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive1"));
            _bookOfSigmarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive2"));
            _bookOfSigmarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive3"));
            _bookOfSigmarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfSigmarPassive4"));

            _sigmarsProclaimerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_sigmarsProclaimerKeystone).UnderscoreFirstCharToUpper()));
            _sigmarsProclaimerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_sigmarsProclaimerPassive1).UnderscoreFirstCharToUpper()));
            _sigmarsProclaimerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_sigmarsProclaimerPassive2).UnderscoreFirstCharToUpper()));
            _sigmarsProclaimerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_sigmarsProclaimerPassive3).UnderscoreFirstCharToUpper()));
            _sigmarsProclaimerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_sigmarsProclaimerPassive4).UnderscoreFirstCharToUpper()));

            _relentlessFanaticKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticKeyStone"));
            _relentlessFanaticPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive1"));
            _relentlessFanaticPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive2"));
            _relentlessFanaticPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive3"));
            _relentlessFanaticPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("RelentlessFanaticPassive4"));

            _protectorOfTheWeakKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakKeyStone"));
            _protectorOfTheWeakPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive1"));
            _protectorOfTheWeakPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive2"));
            _protectorOfTheWeakPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive3"));
            _protectorOfTheWeakPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ProtectorOfTheWeakPassive4"));

            _holyPurgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgeKeyStone"));
            _holyPurgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive1"));
            _holyPurgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive2"));
            _holyPurgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive3"));
            _holyPurgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HolyPurgePassive4"));

            _archLectorKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorKeyStone"));
            _archLectorPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive1"));
            _archLectorPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive2"));
            _archLectorPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive3"));
            _archLectorPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArchLectorPassive4"));
            
            _twinTailedCometPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TwinTailedCometPassive1"));
            _twinTailedCometPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TwinTailedCometPassive2"));
            _twinTailedCometPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TwinTailedCometPassive3"));
            _twinTailedCometPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TwinTailedCometPassive4"));
            _twinTailedCometKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TwinTailedCometKeystone"));
        }

        protected override  void InitializeKeyStones()
        {
            _warriorPriestRoot.Initialize(CareerID, "root", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.0005f),
                        MutationType = OperationType.Add
                    }
                });
            _bookOfSigmarKeystone.Initialize(CareerID, "Ability can also be charged by applying damage.", "BookOfSigmar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                },new CareerChoiceObject.PassiveEffect());
            
            _sigmarsProclaimerKeystone.Initialize(CareerID, "Doubles the aura size of Righteous Fury.", "SigmarsProclaimer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Replace
                    },
                });
            _relentlessFanaticKeystone.Initialize(CareerID, "Righteous Fury scales with Leadership. Troops affected get unbreakable for the duration.", "RelentlessFanatic", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.05f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.0005f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.05f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                });
            _protectorOfTheWeakKeystone.Initialize(CareerID, "Righteous Fury scales with the highest melee weapon skill. Adds 20% physical resistance.", "ProtectorOfTheWeak", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.05f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.0005f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm }, 0.05f, true),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"physical_resistance_20"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            _holyPurgeKeystone.Initialize(CareerID, "All units affected by Righteous Fury receive a burning weapon buff.", "HolyPurge", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"apply_fury_sword_trait"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            
            _archLectorKeystone.Initialize(CareerID, "Adds a healing buff to Righteous Fury that restores 3 Hitpoints per second.", "ArchLector", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"righteous_fury_regeneration"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            
            _twinTailedCometKeystone.Initialize(CareerID, "Righteous Fury adds a damaging aura. Its radius increases slightly when raising relevant skills.", "TwinTailedComet", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"apply_righteous_fury_pulse"}).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_righteous_fury_pulse",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.01f),
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RighteousFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                        MutationType = OperationType.Add
                    },
                });
        }

        protected override void InitializePassives()
        {
            _bookOfSigmarPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "BookOfSigmar", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _bookOfSigmarPassive2.Initialize(CareerID, "Increases morale for all troops by 10.", "BookOfSigmar", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.TroopMorale, true));
            _bookOfSigmarPassive3.Initialize(CareerID, "After battle, all wounded companions are healed for 20 Hitpoints.", "BookOfSigmar", false, ChoiceType.Passive,null,new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special)); // PostBattleCampaignBehavior 30 
            _bookOfSigmarPassive4.Initialize(CareerID, "Wounded troops in your party heal faster.", "BookOfSigmar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration)); 

            _sigmarsProclaimerPassive1.Initialize(CareerID, "10% extra holy melee damage.", "SigmarsProclaimer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy,10),AttackTypeMask.Melee));
            _sigmarsProclaimerPassive2.Initialize(CareerID, "All Sigmarite troops wages are reduced by 20%", "SigmarsProclaimer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.TroopWages, true, 
                    characterObject => !characterObject.IsHero && IsSigmariteTroop(characterObject)));
            _sigmarsProclaimerPassive3.Initialize(CareerID, "Sigmarite troops get 25% resistance to physical ranged attacks.", "SigmarsProclaimer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Holy,10),AttackTypeMask.Ranged,
                (attacker, victim, mask) =>mask == AttackTypeMask.Ranged &&victim.BelongsToMainParty() && IsSigmariteTroop(victim.Character as CharacterObject)));
            _sigmarsProclaimerPassive4.Initialize(CareerID, "When praying at a shrine of Sigmar, all characters restore 50 Hitpoints.", "SigmarsProclaimer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special));//TORCustomSettlementCampaignBehavior 429

            _relentlessFanaticPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "RelentlessFanatic", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _relentlessFanaticPassive2.Initialize(CareerID, "10% extra holy melee damage.", "RelentlessFanatic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy,10),AttackTypeMask.Melee));
            _relentlessFanaticPassive3.Initialize(CareerID, "Party movement speed is increased by 1.", "RelentlessFanatic", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));
            _relentlessFanaticPassive4.Initialize(CareerID, "Prayers aren't affected by global cooldowns.", "RelentlessFanatic", false, ChoiceType.Passive, null);  
            
            _protectorOfTheWeakPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "ProtectorOfTheWeak", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _protectorOfTheWeakPassive2.Initialize(CareerID, "Increases melee physical resistance by 15%.", "ProtectorOfTheWeak", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical,15),AttackTypeMask.Melee));
            _protectorOfTheWeakPassive3.Initialize(CareerID, "Increases Magic resistance against spells by 25%.", "ProtectorOfTheWeak", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,25),AttackTypeMask.Spell));
            _protectorOfTheWeakPassive4.Initialize(CareerID, "Hits below 15 damage do not stagger the player.", "ProtectorOfTheWeak", false, ChoiceType.Passive, null); // Agent extension 83,
            
            _holyPurgePassive1.Initialize(CareerID, "10% extra holy melee damage.", "HolyPurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy,10),AttackTypeMask.Melee));
            _holyPurgePassive2.Initialize(CareerID, "All battles against forces of Chaos and undead earn 20% more prestige.", "HolyPurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special));
            _holyPurgePassive3.Initialize(CareerID, "All troops deal 10% more melee damage to non-human enemies.", "HolyPurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Melee,HolyPurgePassive3)); 
            _holyPurgePassive4.Initialize(CareerID, "All Sigmarite troops gain 10% extra Holy damage.", "HolyPurge", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 10), AttackTypeMask.Melee, HolyPurgePassive4));

            _archLectorPassive1.Initialize(CareerID, "Prayers are recharged on battle start.", "ArchLector", false, ChoiceType.Passive, null); // AbilityMissionLogic 534
            _archLectorPassive2.Initialize(CareerID, "All neutral Empire troops now count as Sigmarite troops.", "ArchLector", false, ChoiceType.Passive, null);
            _archLectorPassive3.Initialize(CareerID, "Gain 15% Ward save.", "ArchLector", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All,15),AttackTypeMask.All));
            _archLectorPassive4.Initialize(CareerID, "All Sigmarite troops gain 20% Ward save.", "ArchLector", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 20), AttackTypeMask.Melee, HolyPurgePassive2));
            
            _twinTailedCometPassive1.Initialize(CareerID, "10% extra holy melee damage.", "TwinTailedComet", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy,10),AttackTypeMask.Melee));
            _twinTailedCometPassive2.Initialize(CareerID, "Increases Companion Limit by 5.", "TwinTailedComet", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _twinTailedCometPassive3.Initialize(CareerID, "Extra 20% armor penetration of melee attacks.", "TwinTailedComet", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _twinTailedCometPassive4.Initialize(CareerID, "Increases Hitpoints by 30.", "TwinTailedComet", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Health));
        }

        private static bool IsSigmariteTroop(CharacterObject troop)
        {
            return troop.UnitBelongsToCult("cult_of_sigmar") ||  (!troop.IsReligiousUnit()&& Hero.MainHero.HasCareerChoice("ArchLectorPassive2"));
        }
        
        private static bool HolyPurgePassive2(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            if (!victim.BelongsToMainParty()) return false;
            if (victim.IsMainAgent) return false;

            return IsSigmariteTroop(victim.Character as CharacterObject);
        }
        
        private static bool HolyPurgePassive3(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            return victim.Character.Race != 0; 
        }
        
        private static bool HolyPurgePassive4(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            if (mask != AttackTypeMask.Melee) return false;
            if (attacker.IsMainAgent) return false;
            if (!attacker.BelongsToMainParty()) return false;

            return attacker.Character.UnitBelongsToCult("cult_of_sigmar") ||  !victim.Character.IsReligiousUnit()&& Hero.MainHero.HasCareerChoice("ArchLector2");
        }
        
    }
}
