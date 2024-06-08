using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class MercenaryCareerChoices : TORCareerChoicesBase
    {
        public MercenaryCareerChoices(CareerObject id) : base(id) {}
     

        private CareerChoiceObject _mercenaryRootNode;

        private CareerChoiceObject _survivalistPassive1;
        private CareerChoiceObject _survivalistPassive2;
        private CareerChoiceObject _survivalistPassive3;
        private CareerChoiceObject _survivalistPassive4;
        private CareerChoiceObject _survivalistKeystone;

        private CareerChoiceObject _duelistPassive1;
        private CareerChoiceObject _duelistPassive2;
        private CareerChoiceObject _duelistPassive3;
        private CareerChoiceObject _duelistPassive4;
        private CareerChoiceObject _duelistKeystone;

        private CareerChoiceObject _headhunterPassive1;
        private CareerChoiceObject _headhunterPassive2;
        private CareerChoiceObject _headhunterPassive3;
        private CareerChoiceObject _headhunterPassive4;
        private CareerChoiceObject _headhunterKeystone;

        private CareerChoiceObject _knightlyPassive1;
        private CareerChoiceObject _knightlyPassive2;
        private CareerChoiceObject _knightlyPassive3;
        private CareerChoiceObject _knightlyPassive4;
        private CareerChoiceObject _knightlyKeystone;

        private CareerChoiceObject _mercenaryLordPassive1;
        private CareerChoiceObject _mercenaryLordPassive2;
        private CareerChoiceObject _mercenaryLordPassive3;
        private CareerChoiceObject _mercenaryLordPassive4;
        private CareerChoiceObject _mercenaryLordKeystone;

        private CareerChoiceObject _commanderPassive1;
        private CareerChoiceObject _commanderPassive2;
        private CareerChoiceObject _commanderPassive3;
        private CareerChoiceObject _commanderPassive4;
        private CareerChoiceObject _commanderKeystone;
        
        private CareerChoiceObject _paymasterPassive1;
        private CareerChoiceObject _paymasterPassive2;
        private CareerChoiceObject _paymasterPassive3;
        private CareerChoiceObject _paymasterPassive4;
        private CareerChoiceObject _paymasterKeystone;
        
        protected override void RegisterAll()
        {
            _mercenaryRootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryRoot"));

            _survivalistPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive1"));
            _survivalistPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive2"));
            _survivalistPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive3"));
            _survivalistPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive4"));
            _survivalistKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistKeystone"));

            _duelistPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive1"));
            _duelistPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive2"));
            _duelistPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive3"));
            _duelistPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive4"));
            _duelistKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistKeystone"));

            _headhunterPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive1"));
            _headhunterPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive2"));
            _headhunterPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive3"));
            _headhunterPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive4"));
            _headhunterKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterKeystone"));

            _knightlyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive1"));
            _knightlyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive2"));
            _knightlyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive3"));
            _knightlyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive4"));
            _knightlyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyKeystone"));

            _paymasterPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PaymasterPassive1"));
            _paymasterPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PaymasterPassive2"));
            _paymasterPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PaymasterPassive3"));
            _paymasterPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PaymasterPassive4"));
            _paymasterKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PaymasterKeystone"));

            _mercenaryLordPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1"));
            _mercenaryLordPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2"));
            _mercenaryLordPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive3"));
            _mercenaryLordPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive4"));
            _mercenaryLordKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordKeystone"));

            _commanderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive1"));
            _commanderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive2"));
            _commanderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive3"));
            _commanderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive4"));
            _commanderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderKeystone"));
        }

        protected override void InitializeKeyStones()
        {
            _mercenaryRootNode.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string> { "Unstoppable", "Unbreakable" },
                        MutationType = OperationType.Replace
                    },
                });

            _survivalistKeystone.Initialize(CareerID, "Adds 15% Melee and Ranged Resistance during the career ability.", "Survivalist", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_melee_res", "let_them_have_it_range_res" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _duelistKeystone.Initialize(CareerID, "Increases melee attack speed during career ability by 15%.", "Duelist", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_melee_ats" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });

            _headhunterKeystone.Initialize(CareerID, "Increases range damage during the career ability by 15%.", "Headhunter", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_range_dmg" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _knightlyKeystone.Initialize(CareerID, "Increases melee damage during the career ability by 15%.", "Knightly", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_melee_dmg" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _paymasterKeystone.Initialize(CareerID, "Values for career ability effects are doubled.", "Paymaster", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_range_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_range_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_rls",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    }
                });

            _mercenaryLordKeystone.Initialize(CareerID, "The Career ability reduces reload time by 15%.", "MercenaryLord", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_melee_rls" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _commanderKeystone.Initialize(CareerID, "Radius of ability is doubled.", "Commander", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Replace
                    },
                }, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true));
        }

        protected override void InitializePassives()
        {
            _survivalistPassive1.Initialize(CareerID, "5 extra ammo", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _survivalistPassive2.Initialize(CareerID, "Increases ranged damage by 10%.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
            _survivalistPassive3.Initialize(CareerID, "Party movement speed is increased by 20% in forest, mountain and swamp terrain.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartyMovementSpeed, true,
            (characterObject)=> {
                if (characterObject.HeroObject != Hero.MainHero) return false;
                var party = characterObject.HeroObject.PartyBelongedTo;
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
                return faceTerrainType == TerrainType.Forest || faceTerrainType == TerrainType.Mountain || faceTerrainType == TerrainType.Swamp;
            }, true));
            _survivalistPassive4.Initialize(CareerID, "Go for a hunt once a day (success chance based on Scouting, Polearm and ranged skills).", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0));

            _duelistPassive1.Initialize(CareerID, "Increases Hitpoints by 20.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _duelistPassive2.Initialize(CareerID, "Increases melee damage resistance of melee troops by 10%.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, 
                (attacker, victim, mask) => !victim.BelongsToMainParty()&& !(victim.IsMainAgent || victim.IsHero)&& mask == AttackTypeMask.Melee ));
            _duelistPassive3.Initialize(CareerID, "Increases melee damage by 10%.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _duelistPassive4.Initialize(CareerID, "Increases health regeneration on the campaign map by 3.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.HealthRegeneration));

            _headhunterPassive1.Initialize(CareerID, "10 extra ammo.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
            _headhunterPassive2.Initialize(CareerID, "Increases ranged damage by 10%.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
            _headhunterPassive3.Initialize(CareerID, "Companion limit of party is increased by 5.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _headhunterPassive4.Initialize(CareerID, "Increases ranged damage resistance by 15%.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged));

            _knightlyPassive1.Initialize(CareerID, "Increases melee damage by 15%.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee));
            _knightlyPassive2.Initialize(CareerID, "Increases melee resistance by 15%.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee));
            _knightlyPassive3.Initialize(CareerID, "Increases Hitpoints by 40.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _knightlyPassive4.Initialize(CareerID, "Increases armor penetration of melee attacks by 15%.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));

            _paymasterPassive1.Initialize(CareerID, "Wounded troops in your party heal faster.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
            _paymasterPassive2.Initialize(CareerID, "40% chance to recruit an extra unit of the same type free of charge.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special, true)); //TORCareerPerkCampaignBehavior 29
            _paymasterPassive3.Initialize(CareerID, "Wages of Tier 4 troops and above are reduced by 20%.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.TroopWages, true, 
                 characterObject => !characterObject.IsHero&& characterObject.Tier>4 ));
            _paymasterPassive4.Initialize(CareerID, "Hire your elite troops as companion", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true)); //TORPartyWageModel 84

            _mercenaryLordPassive1.Initialize(CareerID, "4 extra special ammo like grenades or buckshot.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.Special, false)); //TORAgentStatCalculateModel 97
            _mercenaryLordPassive2.Initialize(CareerID, "Increases the damage of all ranged troops by 15%.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged, 
                (attacker, victim, mask) => !attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero)&& mask == AttackTypeMask.Ranged));
            _mercenaryLordPassive3.Initialize(CareerID, "Higher mercenary contract payment, lower Influence loss. Scales with the Trade skill.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true)); // TOR_Core.Models.TORClanFinanceModel. 53
            _mercenaryLordPassive4.Initialize(CareerID, "Ranged shots can penetrate multiple targets.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //TORAgentApplyDamage 29

            _commanderPassive1.Initialize(CareerID, "Companion limit of party is increased by 5.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _commanderPassive2.Initialize(CareerID, "Increases the damage of all melee troops by 15%.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee, 
                (attacker, victim, mask) => !attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero)&& mask == AttackTypeMask.Ranged));
            
            _commanderPassive3.Initialize(CareerID, "Hits below 15 damage do not stagger the player.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); // Agent extension 83
            _commanderPassive4.Initialize(CareerID, "Companion health of party is increased by 25.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
        }
        
    }
}