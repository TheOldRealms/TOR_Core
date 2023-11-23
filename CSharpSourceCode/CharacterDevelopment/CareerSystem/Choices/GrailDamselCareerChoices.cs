using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
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
        private CareerChoiceObject _justCauseKeystone;
        
        private CareerChoiceObject _secretsOFTheGrailPassive1;
        private CareerChoiceObject _secretsOFTheGrailPassive2;
        private CareerChoiceObject _secretsOFTheGrailPassive3;
        private CareerChoiceObject _secretsOFTheGrailPassive4;
        
        private CareerChoiceObject _envoyOfTheLadyPassive1;
        private CareerChoiceObject _envoyOfTheLadyPassive2;
        private CareerChoiceObject _envoyOfTheLadyPassive3;
        private CareerChoiceObject _envoyOfTheLadyPassive4;
        
        
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
            
            _envoyOfTheLadyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive1"));
            _envoyOfTheLadyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive1"));
            _envoyOfTheLadyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive1"));
            _envoyOfTheLadyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive1"));
        }

        protected override void InitializeKeyStones()
        {
            _grailDamselRootNode.Initialize(CareerID, "No Career Ability", null, true, ChoiceType.Keystone);
        }

        protected override void InitializePassives()
        {
            _feyEntchantmentPassive1.Initialize(CareerID, "{=fey_enchantment_passive1_str}Increases magic spell damage by 15%.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
            _feyEntchantmentPassive2.Initialize(CareerID, "{=fey_enchantment_passive2_str}Increases max Winds of Magic by 10.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _feyEntchantmentPassive3.Initialize(CareerID, "{=fey_enchantment_passive3_str}All troops gain 15% extra magic damage.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));  
            _feyEntchantmentPassive4.Initialize(CareerID, "{=fey_enchantment_passive4_str}All Knight troops gain 15% Ward save.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
            
            _inspirationOfTheLadyPassive1.Initialize(CareerID, "{=inspiration_of_the_lady_passive1_str}25% chance to recruit an extra unit free of charge.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); 
            _inspirationOfTheLadyPassive2.Initialize(CareerID, "{=inspiration_of_the_lady_passive2_str}Wounded troops in your party heal faster.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
            _inspirationOfTheLadyPassive3.Initialize(CareerID, "{=inspiration_of_the_lady_passive3_str}All Knight troops wages are reduced by 25%.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
            _inspirationOfTheLadyPassive4.Initialize(CareerID, "{=inspiration_of_the_lady_passive4_str}10% Ward save if your armor weight does not exceed 11 weight.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));

            _talesOfGilesPassive1.Initialize(CareerID, "{=tales_of_giles_passive1_str}Increases Hitpoints by 25.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _talesOfGilesPassive2.Initialize(CareerID, "{=tales_of_giles_passive2_str}Bretonnian units receive 10% Ward save.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));
            _talesOfGilesPassive3.Initialize(CareerID, "{=tales_of_giles_passive3_str}When praying at a shrine of the Lady, all wounded troops get healed.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
            _talesOfGilesPassive4.Initialize(CareerID, "{=tales_of_giles_passive4_str}20% spell cooldown reduction.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.WindsCooldownReduction, true)); 
            
            _vividVisionsPassive1.Initialize(CareerID, "{=vivid_visions_passive1_str}Increases max Winds of Magic by 10.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _vividVisionsPassive2.Initialize(CareerID, "{=vivid_visions_passive2_str}Party movement speed is increased by 1.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
            _vividVisionsPassive3.Initialize(CareerID, "{=vivid_visions_passive3_str}Increases Magic resistance against spells by 25%.", "VividVisions", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,25),AttackTypeMask.Spell));
            _vividVisionsPassive4.Initialize(CareerID, "{=vivid_visions_passive4_str}The Spotting range of the party is increased by 20%.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            
            _justCausePassive1.Initialize(CareerID, "{=just_cause_passive1_str}Upgrade costs are halved.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-0.5f, PassiveEffectType.TroopUpgradeCost));
            _justCausePassive2.Initialize(CareerID, "{=just_cause_passive2_str}Non-knight units in the party gain 100 XP every day.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, false)); //CareerPerkCampaign Behavior 101
            _justCausePassive3.Initialize(CareerID, "{=just_cause_passive3_str}Extra 15% Wardsave if your armor weight does not exceed 11 weight.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
            _justCausePassive4.Initialize(CareerID, "{=just_cause_passive4_str}Increases positive Relation gains by 20%.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorDiplomacy model 23
            
            _secretsOFTheGrailPassive1.Initialize(CareerID, "{=secrets_of_the_grail_Passive1_str}Increases lightning spell damage by 30%.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 30), AttackTypeMask.Spell));
            _secretsOFTheGrailPassive2.Initialize(CareerID, "{=secrets_of_the_grail_Passive2_str}20% cost reduction for spells.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.WindsCostReduction, true));
            _secretsOFTheGrailPassive3.Initialize(CareerID, "{=secrets_of_the_grail_Passive3_str}Casting prayers has a 50% chance to restore 15 Winds of Magic.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, true)); //AbilityMissionLogic, OnCastComplete
            _secretsOFTheGrailPassive4.Initialize(CareerID, "{=secrets_of_the_grail_Passive4_str}30% prayer cooldown reduction.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.PrayerCoolDownReduction, true));
            
            _envoyOfTheLadyPassive1.Initialize(CareerID, "{=ambassador_of_the_lady_Passive1_str}Increases Magic damage by 20%.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 20), AttackTypeMask.Spell));
            _envoyOfTheLadyPassive2.Initialize(CareerID, "{=ambassador_of_the_lady_Passive2_str}Knight Companion health points are doubled.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, true));
            _envoyOfTheLadyPassive3.Initialize(CareerID, "{=ambassador_of_the_lady_Passive3_str}Damsel Companion have 50 more Winds of Magic.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, false)); //AbilityMissionLogic, OnCastComplete
            _envoyOfTheLadyPassive4.Initialize(CareerID, "{=ambassador_of_the_lady_Passive4_str}Diplomatic force options for all Brettonnian Leaders.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true));

        }
        
        protected override void UnlockCareerBenefitsTier2()
        {
            if(Hero.MainHero.HasKnownLore("LoreOfLife"))
            {
                Hero.MainHero.AddKnownLore("LoreOfBeasts");     //Known lore check within method, nothing gets added twice
                return;
            }

            if (Hero.MainHero.HasKnownLore("LoreOfBeasts"))
            {
                Hero.MainHero.AddKnownLore("LoreOfLife");
            }
        }
        
        protected override void UnlockCareerBenefitsTier3()
        {
            Hero.MainHero.AddKnownLore("LoreOfHeavens");
            Hero.MainHero.AddAttribute("SecondLoreForDamselCompanions");
        }


       
    }
}