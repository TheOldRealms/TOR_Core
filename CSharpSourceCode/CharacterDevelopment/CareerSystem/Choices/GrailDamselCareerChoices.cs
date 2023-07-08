﻿using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class GrailDamselCareerChoices : TORCareerChoicesBase
    {
        public GrailDamselCareerChoices(CareerObject id) : base(id) {}


        private CareerChoiceObject _grailDamselRootNode;
        
        
        private CareerChoiceObject _feyEntchantmentPassive1;
        private CareerChoiceObject _feyEntchantmentPassive2;
        private CareerChoiceObject _feyEntchantmentPassive3;
        private CareerChoiceObject _feyEntchantmentPassive4;
        
        private CareerChoiceObject _inspirationOfTheLadyPassive1;
        private CareerChoiceObject _inspirationOfTheLadyPassive2;
        private CareerChoiceObject _inspirationOfTheLadyPassive3;
        private CareerChoiceObject _inspirationOfTheLadyPassive4;
        
        private CareerChoiceObject _talesOfGilesPassive1;
        private CareerChoiceObject _talesOfGilesPassive2;
        private CareerChoiceObject _talesOfGilesPassive3;
        private CareerChoiceObject _talesOfGilesPassive4;
        
        private CareerChoiceObject _vividVisionsPassive1;
        private CareerChoiceObject _vividVisionsPassive2;
        private CareerChoiceObject _vividVisionsPassive3;
        private CareerChoiceObject _vividVisionsPassive4;
        
        private CareerChoiceObject _justCausePassive1;
        private CareerChoiceObject _justCausePassive2;
        private CareerChoiceObject _justCausePassive3;
        private CareerChoiceObject _justCausePassive4;
        
        private CareerChoiceObject _secretsOFTheGrailPassive1;
        private CareerChoiceObject _secretsOFTheGrailPassive2;
        private CareerChoiceObject _secretsOFTheGrailPassive3;
        private CareerChoiceObject _secretsOFTheGrailPassive4;
        
        
        protected override void RegisterAll()
        {
            _grailDamselRootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailDamselRoot"));
            
            _feyEntchantmentPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEntchantmentPassive1"));
            _feyEntchantmentPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEntchantmentPassive2"));
            _feyEntchantmentPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEntchantmentPassive3"));
            _feyEntchantmentPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEntchantmentPassive4"));
            
            _inspirationOfTheLadyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive1"));
            _inspirationOfTheLadyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive2"));
            _inspirationOfTheLadyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive3"));
            _inspirationOfTheLadyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive4"));
            
            _talesOfGilesPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive1"));
            _talesOfGilesPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive2"));
            _talesOfGilesPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive3"));
            _talesOfGilesPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive4"));
            
            _vividVisionsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive1"));
            _vividVisionsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive2"));
            _vividVisionsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive3"));
            _vividVisionsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive4"));
            
            _justCausePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive1"));
            _justCausePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive2"));
            _justCausePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive3"));
            _justCausePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive4"));
            
            _secretsOFTheGrailPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOFTheGrailPassive1"));
            _secretsOFTheGrailPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOFTheGrailPassive2"));
            _secretsOFTheGrailPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOFTheGrailPassive3"));
            _secretsOFTheGrailPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOFTheGrailPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _grailDamselRootNode.Initialize(CareerID, "No Career Ability", null, true, ChoiceType.Keystone);
        }

        protected override void InitializePassives()
        {
            _feyEntchantmentPassive1.Initialize(CareerID, "Extra magic spell damage(15%).", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
            _feyEntchantmentPassive2.Initialize(CareerID, "Increases Winds of Magic by 10.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _feyEntchantmentPassive3.Initialize(CareerID, "All troops gain 15% extra magic damage", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));  
            _feyEntchantmentPassive4.Initialize(CareerID, "15% cost redution for spells", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.WindsCostReduction, true));
            
            _inspirationOfTheLadyPassive1.Initialize(CareerID, "25% extra chance to recruit another unit for free", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); 
            _inspirationOfTheLadyPassive2.Initialize(CareerID, "All Knight troops gain 10% Wardsave", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));
            _inspirationOfTheLadyPassive3.Initialize(CareerID, "All Knight troops wages are reduced by 25%", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); //TODO 
            _inspirationOfTheLadyPassive4.Initialize(CareerID, "30% Wardsave if your armor weight is not exceeding X weight", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true)); //TODO 

            _talesOfGilesPassive1.Initialize(CareerID, "Increases Hitpoints by 50.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _talesOfGilesPassive2.Initialize(CareerID, "Bretonnian Units receive 10% Wardsave", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true)); //TODO 
            _talesOfGilesPassive3.Initialize(CareerID, "Whenever praying at a shrine of the lady, all wounded troops get healed completely", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true)); //TODO 
            _talesOfGilesPassive4.Initialize(CareerID, "20% spell cooldown reduction", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsCooldownReduction, true)); 

            
            
            //_vividVisionsPassive1.Initialize(CareerID, "Increases Winds of Magic by 10.", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            //_vividVisionsPassive2.Initialize(CareerID, "Party Movement speed is increased by 1.5", "Arkayne", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1.5f, PassiveEffectType.PartyMovementSpeed));

            
        }
        
        protected override void UnlockCareerBenefitsTier2()
        {
            if(Hero.MainHero.HasKnownLore("LoreOfLife"))
            {
                Hero.MainHero.AddKnownLore("LoreOfBeasts");     //Known lore check within method, cant be added twice
                return;
            }

            if (Hero.MainHero.HasKnownLore("LoreOfBeasts"))
            {
                Hero.MainHero.AddKnownLore("LoreOfLife");
                return;
            }
        }
        
        protected override void UnlockCareerBenefitsTier3()
        {
            Hero.MainHero.AddKnownLore("LoreOfHeavens");
        }


        public override void ClearCareerBenefits()
        {
            Hero.MainHero.TryRemoveToRemoveKnownLore("LoreOfLife");
            Hero.MainHero.TryRemoveToRemoveKnownLore("LoreOfBeasts");
            Hero.MainHero.TryRemoveToRemoveKnownLore("LoreOfHeavens");
        }
    }
}