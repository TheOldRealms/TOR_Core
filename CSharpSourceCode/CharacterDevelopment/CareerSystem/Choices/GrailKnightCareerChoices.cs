using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class GrailKnightCareerChoices : TORCareerChoicesBase
    {
        public GrailKnightCareerChoices(CareerObject id) : base(id)
        {
        }

        private CareerChoiceObject _grailKnightRoot;
        
        private CareerChoiceObject _errantryWarKeystone;
        private CareerChoiceObject _errantryWarPassive1;
        private CareerChoiceObject _errantryWarPassive2;
        private CareerChoiceObject _errantryWarPassive3;
        private CareerChoiceObject _errantryWarPassive4;
        
        private CareerChoiceObject _enhancedHorseCombatKeystone;
        private CareerChoiceObject _enhancedHorseCombatPassive1;
        private CareerChoiceObject _enhancedHorseCombatPassive2;
        private CareerChoiceObject _enhancedHorseCombatPassive3;
        private CareerChoiceObject _enhancedHorseCombatPassive4;
        
        private CareerChoiceObject _questingVowKeyStone;
        private CareerChoiceObject _questingVowPassive1;
        private CareerChoiceObject _questingVowPassive2;
        private CareerChoiceObject _questingVowPassive3;
        private CareerChoiceObject _questingVowPassive4;
        
        private CareerChoiceObject _monsterSlayerKeystone;
        private CareerChoiceObject _monsterSlayerPassive1;
        private CareerChoiceObject _monsterSlayerPassive2;
        private CareerChoiceObject _monsterSlayerPassive3;
        private CareerChoiceObject _monsterSlayerPassive4;
        
        private CareerChoiceObject _masterHorsemanKeystone;
        private CareerChoiceObject _masterHorsemanPassive1;
        private CareerChoiceObject _masterHorsemanPassive2;
        private CareerChoiceObject _masterHorsemanPassive3;
        private CareerChoiceObject _masterHorsemanPassive4;
        
        private CareerChoiceObject _grailVowKeystone;
        private CareerChoiceObject _grailVowPassive1;
        private CareerChoiceObject _grailVowPassive2;
        private CareerChoiceObject _grailVowPassive3;
        private CareerChoiceObject _grailVowPassive4;
        
        protected override void RegisterAll()
        {
            _grailKnightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailKnightRoot"));
            
            _errantryWarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarKeystone"));
            _errantryWarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive1"));
            _errantryWarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive2"));
            _errantryWarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive3"));
            _errantryWarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ErrantryWarPassive4"));
            
            _enhancedHorseCombatKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatKeystone"));
            _enhancedHorseCombatPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive1"));
            _enhancedHorseCombatPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive2"));
            _enhancedHorseCombatPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive3"));
            _enhancedHorseCombatPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnhancedHorseCombatPassive4"));
            
            _questingVowKeyStone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowKeyStone"));
            _questingVowPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive1"));
            _questingVowPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive2"));
            _questingVowPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive3"));
            _questingVowPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("QuestingVowPassive4"));
            
            _monsterSlayerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerKeystone"));
            _monsterSlayerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive1"));
            _monsterSlayerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive2"));
            _monsterSlayerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive3"));
            _monsterSlayerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MonsterSlayerPassive4"));
            
            _masterHorsemanKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanKeystone"));
            _masterHorsemanPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive1"));
            _masterHorsemanPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive2"));
            _masterHorsemanPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive3"));
            _masterHorsemanPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MasterHorsemanPassive4"));
            
            _grailVowKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowKeystone"));
            _grailVowPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive1"));
            _grailVowPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive2"));
            _grailVowPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive3"));
            _grailVowPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailVowPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _grailKnightRoot.Initialize(TORCareers.GrailKnight,"No Career Ability",null,true,ChoiceType.Keystone);
        }

        protected override void InitializePassives()
        {
            _errantryWarPassive1.Initialize(CareerID, "Extra melee Damage(10%).", "ErrantryWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Melee)); //
            _errantryWarPassive2.Initialize(CareerID, "Increases Hitpoints by 40.", "ErrantryWar", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health)); // 
            _errantryWarPassive3.Initialize(CareerID, "One handed and Two handed weapon skill of Knight units is increased by 20", "ErrantryWar", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special));//
            _errantryWarPassive4.Initialize(CareerID, "All Melee Troops in the party gain 25 Xp per day.", "ErrantryWar", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //
            
            _enhancedHorseCombatPassive1.Initialize(CareerID, "50% additional Horse health", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.HorseHealth, true)); //
            _enhancedHorseCombatPassive2.Initialize(CareerID, "Extra melee Damage(10%).", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true)); //
            _enhancedHorseCombatPassive3.Initialize(CareerID, "Upgrade costs are 25% reduced", "EnhancedHorseCombat", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopUpgradeCost, true)); //
            _enhancedHorseCombatPassive4.Initialize(CareerID, "All Knights have a 30 point higher polearm skill", "EnhancedHorseCombat",  false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special)); //
            
            _questingVowPassive1.Initialize(CareerID, "Increases Hitpoints by 40.", "QuestingVow", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health)); //
            _questingVowPassive2.Initialize(CareerID, "15% Physical damage reduction from Melee and Ranged attacks.", "QuestingVow", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical,15),AttackTypeMask.Ranged|AttackTypeMask.Melee)); 
            _questingVowPassive3.Initialize(CareerID, "All Knight troops gain 10% Physical resistance.", "QuestingVow", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special,true));
            _questingVowPassive4.Initialize(CareerID, "Hits below 15 damage will not stagger character.", "QuestingVow", false, ChoiceType.Passive, null);  //
            
            _monsterSlayerPassive1.Initialize(CareerID, "Extra melee fire damage(10%).", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire,10),AttackTypeMask.Melee));
            _monsterSlayerPassive2.Initialize(CareerID, "20% Melee Armor Penetration", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _monsterSlayerPassive3.Initialize(CareerID, "40% Chance to recruit free extra troops on recruitment.", "MonsterSlayer", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special,true));
            _monsterSlayerPassive4.Initialize(CareerID, "75% wage reduction for peasant units", "MonsterSlayer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-75, PassiveEffectType.Special, true));
            
            _masterHorsemanPassive1.Initialize(CareerID, "Horse charge damage is increased by 50%.", "MasterHorseman", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.HorseChargeDamage,true));
            _masterHorsemanPassive2.Initialize(CareerID, "Party movement speed is increased by 2", "MasterHorseman", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.PartyMovementSpeed));
            _masterHorsemanPassive3.Initialize(CareerID, "Couched lance attacks have a 30% chance of not bouncing off (Calculated after other chances fail)", "MasterHorseman", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special,true));
            _masterHorsemanPassive4.Initialize(CareerID, "All Knight unit wages are reduced by 25%", "MasterHorseman", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.Special,true));
            
            _grailVowPassive1.Initialize(CareerID, "Increases Hitpoints by 40.", "GrailVow", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _grailVowPassive2.Initialize(CareerID, "Extra holy melee Damage(20%).", "GrailVow", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy,20),AttackTypeMask.Melee));
            _grailVowPassive3.Initialize(CareerID, "15% Wardsave.", "GrailVow", false,ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All,15),AttackTypeMask.All));
            _grailVowPassive4.Initialize(CareerID, "All  melee attacks can crush through.", "GrailVow", false, ChoiceType.Passive, null); 
        }

        
    }
}