using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class MercenaryCareerChoices : TORCareerChoiceBase
    {
        private CareerChoiceObject _MercenaryrootNode;
        
        private CareerChoiceObject _survivivalistPassive1;
        private CareerChoiceObject _survivivalistPassive2;
        private CareerChoiceObject _survivivalistPassive3;
        private CareerChoiceObject _survivivalistPassive4;
        
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
            _MercenaryrootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryRoot"));
            
            _survivivalistPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive1"));
            _survivivalistPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive2"));
            _survivivalistPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive3"));
            _survivivalistPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SurvivalistPassive4"));
            
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
            base.InitializeKeyStones();
            
            _MercenaryrootNode.Initialize(TORCareers.Mercenary,"No Career Ability",null,true,ChoiceType.Keystone);
        }

        protected override void InitializePassives()
        {
            _survivivalistPassive1.Initialize(TORCareers.Mercenary, "5 extra Ammo.", "Survivalist", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _survivivalistPassive2.Initialize(TORCareers.Mercenary, "Extra ranged damage(10%).", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Ranged));
            _survivivalistPassive3.Initialize(TORCareers.Mercenary, "Increases MovementSpeed inside forests by 20%.", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _survivivalistPassive4.Initialize(TORCareers.Mercenary, "Once per day, you go for a hunt. Your success is determined by your Scouting and ranged and polearm capabilities", "Survivalist", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(0));
            
            _duelistPassive1.Initialize(TORCareers.Mercenary, "Increases Hitpoints by 20.", "Duelist", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _duelistPassive2.Initialize(TORCareers.Mercenary, "extra melee damage(10%).", "Duelist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Melee));
            _duelistPassive3.Initialize(TORCareers.Mercenary, "Infantry wages are reduced by 20%.", "Duelist", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true));
            _duelistPassive4.Initialize(TORCareers.Mercenary, "Health regeneration on campaign map", "Duelist", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.HealthRegeneration));
            
            _headhunterPassive1.Initialize(TORCareers.Mercenary, "10 extra Ammo.", "Headhunter", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
            _headhunterPassive2.Initialize(TORCareers.Mercenary, "Extra ranged Damage(10%).", "Headhunter", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Ranged));
            _headhunterPassive3.Initialize(TORCareers.Mercenary, "You earn 500 gold from high values targets(Lords, Bandit bosses), if you or your party eliminates it.", "Headhunter", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(500, PassiveEffectType.Special));
            _headhunterPassive4.Initialize(TORCareers.Mercenary, "15% Ranged Damage Resistance.", "Headhunter", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Ranged));

            _knightlyPassive1.Initialize(TORCareers.Mercenary, "extra melee damage(15%)", "Knightly", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Melee));
            _knightlyPassive2.Initialize(TORCareers.Mercenary, "extra melee resistance(15%).", "Knightly", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical,25),AttackTypeMask.Melee));
            _knightlyPassive3.Initialize(TORCareers.Mercenary, "Increases Hitpoints by 40.", "Knightly", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _knightlyPassive4.Initialize(TORCareers.Mercenary, "Ignores 15% armor for every melee hit", "Knightly", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.Special,true));
            
            _mercenaryLordPassive1.Initialize(TORCareers.Mercenary, "4 extra special ammunition grenades or buckshots.", "MercenaryLord", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.Special,false));
            _mercenaryLordPassive2.Initialize(TORCareers.Mercenary, "15% extra damage for all ranged units.", "MercenaryLord", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special,true));
            _mercenaryLordPassive3.Initialize(TORCareers.Mercenary, "Payment in a mercenary contract are higher, while less  influence is lost. The effect can be enhanced by a higher Merchant skill.", "MercenaryLord", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special,true));
            _mercenaryLordPassive4.Initialize(TORCareers.Mercenary, "Based on your trading skill, you get a bonus on top for your mercenary contract payment", "MercenaryLord", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
            
            _commanderPassive1.Initialize(TORCareers.Mercenary, "Wages for Tier 4 Units and higher are reduced by 20%", "Commander", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special,true));
            _commanderPassive2.Initialize(TORCareers.Mercenary, "15% extra damage for all melee units.", "Commander", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special,true));
            _commanderPassive3.Initialize(TORCareers.Mercenary, "Damages below 15 points do not stagger character", "Commander", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
            _commanderPassive4.Initialize(TORCareers.Mercenary, "There is a 40% chance when recruiting units to recruit another of the same type for free.", "Commander", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Special,true));
        }
    }
}