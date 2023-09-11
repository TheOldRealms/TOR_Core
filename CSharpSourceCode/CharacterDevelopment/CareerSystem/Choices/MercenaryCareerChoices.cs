using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
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

        private CareerChoiceObject _duelistPassive1;
        private CareerChoiceObject _duelistPassive2;
        private CareerChoiceObject _duelistPassive3;
        private CareerChoiceObject _duelistPassive4;

        private CareerChoiceObject _headhunterPassive1;
        private CareerChoiceObject _headhunterPassive2;
        private CareerChoiceObject _headhunterPassive3;
        private CareerChoiceObject _headhunterPassive4;

        private CareerChoiceObject _knightlyPassive1;
        private CareerChoiceObject _knightlyPassive2;
        private CareerChoiceObject _knightlyPassive3;
        private CareerChoiceObject _knightlyPassive4;

        private CareerChoiceObject _mercenaryLordPassive1;
        private CareerChoiceObject _mercenaryLordPassive2;
        private CareerChoiceObject _mercenaryLordPassive3;
        private CareerChoiceObject _mercenaryLordPassive4;

        private CareerChoiceObject _commanderPassive1;
        private CareerChoiceObject _commanderPassive2;
        private CareerChoiceObject _commanderPassive3;
        private CareerChoiceObject _commanderPassive4;

        protected override void RegisterAll()
        {
            _mercenaryRootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryRoot"));

            _survivalistPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive1"));
            _survivalistPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive2"));
            _survivalistPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive3"));
            _survivalistPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive4"));

            _duelistPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive1"));
            _duelistPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive2"));
            _duelistPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive3"));
            _duelistPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DuelistPassive4"));

            _headhunterPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive1"));
            _headhunterPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive2"));
            _headhunterPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive3"));
            _headhunterPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeadhunterPassive4"));

            _knightlyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive1"));
            _knightlyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive2"));
            _knightlyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive3"));
            _knightlyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyPassive4"));

            _mercenaryLordPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1"));
            _mercenaryLordPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2"));
            _mercenaryLordPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive3"));
            _mercenaryLordPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive4"));

            _commanderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive1"));
            _commanderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive2"));
            _commanderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive3"));
            _commanderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CommanderPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _mercenaryRootNode.Initialize(CareerID, "No Career Ability", null, true, ChoiceType.Keystone);
        }

        protected override void InitializePassives()
        {
            _survivalistPassive1.Initialize(CareerID, "5 extra ammo", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _survivalistPassive2.Initialize(CareerID, "Increases ranged damage by 10%.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
            _survivalistPassive3.Initialize(CareerID, "Party movement speed is increased by 20% in forest terrain.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TORspeedcalculationmodel 58
            _survivalistPassive4.Initialize(CareerID, "Go for a hunt once a day (success chance based on Scouting, Polearm and ranged skills).", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0)); //TORCareerPerkCampaignBehavior 118 

            _duelistPassive1.Initialize(CareerID, "Increases Hitpoints by 20.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _duelistPassive2.Initialize(CareerID, "Increases melee damage by 10%.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _duelistPassive3.Initialize(CareerID, "Infantry wages are reduced by 20%.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true)); //TORPartyWageModel 84
            _duelistPassive4.Initialize(CareerID, "Increases health regeneration on the campaign map by 3.", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.HealthRegeneration));

            _headhunterPassive1.Initialize(CareerID, "10 extra ammo.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
            _headhunterPassive2.Initialize(CareerID, "Increases ranged damage by 10%.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
            _headhunterPassive3.Initialize(CareerID, "Earn 500 gold for every lord or a bandit leader captured or killed.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(500, PassiveEffectType.Special)); //CareerPerkMissionBehavior  39
            _headhunterPassive4.Initialize(CareerID, "Increases ranged damage resistance by 15%.", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged));

            _knightlyPassive1.Initialize(CareerID, "Increases melee damage by 15%.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee));
            _knightlyPassive2.Initialize(CareerID, "Increases melee resistance by 15%.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee));
            _knightlyPassive3.Initialize(CareerID, "Increases Hitpoints by 40.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _knightlyPassive4.Initialize(CareerID, "Increases armor penetration of melee attacks by 15%.", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));

            _mercenaryLordPassive1.Initialize(CareerID, "4 extra special ammo like grenades or buckshot.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.Special, false));  //TORAgentStatCalculateModel 97
            _mercenaryLordPassive2.Initialize(CareerID, "Increases the damage of all ranged troops by 15%.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));          //TORAgentStatCalculateModel 458
            _mercenaryLordPassive3.Initialize(CareerID, "Higher mercenary contract payment, lower Influence loss. Scales with the Trade skill.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true)); // TOR_Core.Models.TORClanFinanceModel. 53
            _mercenaryLordPassive4.Initialize(CareerID, "Ranged shots can penetrate multiple targets.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //TORAgentApplyDamage 29

            _commanderPassive1.Initialize(CareerID, "Wages of Tier 4 troops and above are reduced by 20%.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true)); //TORWageModel 100
            _commanderPassive2.Initialize(CareerID, "Increases the damage of all melee troops by 15%.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true)); //TORAgentStatCalculateModel 467
            _commanderPassive3.Initialize(CareerID, "Hits below 15 damage do not stagger the player.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); // Agent extension 83
            _commanderPassive4.Initialize(CareerID, "40% chance to recruit an extra unit of the same type free of charge.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special, true)); //TORCareerPerkCampaignBehavior 29
        }
        

      
    }
}