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
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class MercenaryCareerChoices : TORCareerChoicesBase
    {
        public MercenaryCareerChoices(CareerObject id) : base(id) { }


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

        private CareerChoiceObject _veteranPassive1;
        private CareerChoiceObject _veteranPassive2;
        private CareerChoiceObject _veteranPassive3;
        private CareerChoiceObject _veteranPassive4;
        private CareerChoiceObject _veteranKeystone;

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

            _veteranPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_veteranPassive1).UnderscoreFirstCharToUpper()));
            _veteranPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_veteranPassive2).UnderscoreFirstCharToUpper()));
            _veteranPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_veteranPassive3).UnderscoreFirstCharToUpper()));
            _veteranPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_veteranPassive4).UnderscoreFirstCharToUpper()));
            _veteranKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_veteranKeystone).UnderscoreFirstCharToUpper()));

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
            _mercenaryRootNode.Initialize(CareerID, "Never retreat! Never surrender! Inspire nearby troops with a rallying cry! Making them 'Unbreakable' and 'Unstoppable' for 15s. (60s cooldown.)", null, true,
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

            _survivalistKeystone.Initialize(CareerID, "Let Them Have It! also provides +15% 'Physical Resistance'.", "Survivalist", false,
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

            _duelistKeystone.Initialize(CareerID, "Let Them Have It! also provides +15% swing speed.", "Duelist", false,
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

            _headhunterKeystone.Initialize(CareerID, "Let Them Have It! also provides +15% 'Physical' ranged damage.", "Headhunter", false,
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

            _veteranKeystone.Initialize(CareerID, "Let Them Have It! also provides +15% melee 'Physical' damage.", "Veteran", false,
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

            _paymasterKeystone.Initialize(CareerID, "The effects of Let Them Have It! are doubled.", "Paymaster", false,
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

            _mercenaryLordKeystone.Initialize(CareerID, "Let Them Have It! has its cooldown reduced by 15%", "MercenaryLord", false,
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

            _commanderKeystone.Initialize(CareerID, "The radius of Let Them Have It! is doubled.", "Commander", false,
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
            _survivalistPassive1.Initialize(CareerID, "+3 extra ammo per ammunition pouch, or quiver you equip.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Ammo));
            _survivalistPassive2.Initialize(CareerID, "+5% personal ranged 'Physical' damage.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Ranged));
            _survivalistPassive3.Initialize(CareerID, "+20% party move speed on campaign map when in Forest, Mountain, or Swamp terrain.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartyMovementSpeed, true,
            (characterObject) =>
            {
                if (characterObject.HeroObject != Hero.MainHero) return false;
                var party = characterObject.HeroObject.PartyBelongedTo;
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
                return faceTerrainType == TerrainType.Forest || faceTerrainType == TerrainType.Mountain || faceTerrainType == TerrainType.Swamp;
            }, true));
            _survivalistPassive4.Initialize(CareerID, "Once per day, go out on a hunt.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0));

            _duelistPassive1.Initialize(CareerID, "+10 personal Hitpoints.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
            _duelistPassive2.Initialize(CareerID, "+5% 'Physical Resistance' for melee troops.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee,
                (attacker, victim, mask) => victim.BelongsToMainParty() && !(victim.IsMainAgent || victim.IsHero) && mask == AttackTypeMask.Melee));
            _duelistPassive3.Initialize(CareerID, "+5% personal melee 'Physical' damage.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee));
            _duelistPassive4.Initialize(CareerID, "Personal healing rate increased by +1.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.HealthRegeneration));

            _headhunterPassive1.Initialize(CareerID, "+5 personal ammunition.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _headhunterPassive2.Initialize(CareerID, "+5% personal ranged 'Physical' damage.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Ranged));
            _headhunterPassive3.Initialize(CareerID, "+5 Companion limit.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _headhunterPassive4.Initialize(CareerID, "+10% personal ranged 'Physical Resistance'", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));

            _veteranPassive1.Initialize(CareerID, "+10% personal melee 'Physical' damage.", "Veteran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _veteranPassive2.Initialize(CareerID, "+10% personal melee 'Physical Resistance'.", "Veteran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _veteranPassive3.Initialize(CareerID, "+20 personal Hitpoints.", "Veteran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _veteranPassive4.Initialize(CareerID, "+8% personal 'Armour Penetration' of melee attacks.", "Veteran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-8, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));

            _paymasterPassive1.Initialize(CareerID, "Wounded troops heal faster.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.TroopRegeneration)); //can be more precise
            _paymasterPassive2.Initialize(CareerID, "+40% chance to recruit 2 troops instead of 1.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special, true)); //TORCareerPerkCampaignBehavior 29
            _paymasterPassive3.Initialize(CareerID, "-10% Gold upkeep for tier 4+ troops.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.TroopWages, true,
                 characterObject => !characterObject.IsHero && characterObject.Tier >= 4));
            _paymasterPassive4.Initialize(CareerID, "Promote 'Basic' troops of your culture into Companions.", "Paymaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true)); //TORPartyWageModel 84

            _mercenaryLordPassive1.Initialize(CareerID, "+3 extra ammo per pouch of Grenades or Buckshot.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.Special, false)); //TORAgentStatCalculateModel 97
            _mercenaryLordPassive2.Initialize(CareerID, "+10% 'Physical' damage for ranged troops.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero) && mask == AttackTypeMask.Ranged));
            _mercenaryLordPassive3.Initialize(CareerID, "Mercenary contracts 'Influence' cost, and payout scale with your Trade skill.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true)); // TOR_Core.Models.TORClanFinanceModel. 53
            _mercenaryLordPassive4.Initialize(CareerID, "+100% 'Prestige' from battles. Gain another 100% if Leadership reaches 300.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true)); //CustomResourceManager

            _commanderPassive1.Initialize(CareerID, "+5 Companion limit.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _commanderPassive2.Initialize(CareerID, "+10% 'Physical' damage of 'Melee' troops.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero) && mask == AttackTypeMask.Melee));

            _commanderPassive3.Initialize(CareerID, "Hits below 15 damage no longer stagger you.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.ShruggedOff));
            _commanderPassive4.Initialize(CareerID, "+15 Hitpoints for Companions.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
        }

    }
}