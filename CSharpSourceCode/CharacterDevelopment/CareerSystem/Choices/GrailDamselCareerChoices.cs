using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Careers;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class GrailDamselCareerChoices : TORCareerChoicesBase
    {
        public GrailDamselCareerChoices(CareerObject id) : base(id) {}
        
        private CareerChoiceObject _grailDamselRootNode;
        
        private CareerChoiceObject _feyEnchantmentKeystone;
        private CareerChoiceObject _feyEnchantmentPassive1;
        private CareerChoiceObject _feyEnchantmentPassive2;
        private CareerChoiceObject _feyEnchantmentPassive3;
        private CareerChoiceObject _feyEnchantmentPassive4;
        
        private CareerChoiceObject _inspirationOfTheLadyKeystone;
        private CareerChoiceObject _inspirationOfTheLadyPassive1;
        private CareerChoiceObject _inspirationOfTheLadyPassive2;
        private CareerChoiceObject _inspirationOfTheLadyPassive3;
        private CareerChoiceObject _inspirationOfTheLadyPassive4;
        
        private CareerChoiceObject _talesOfGilesKeystone;
        private CareerChoiceObject _talesOfGilesPassive1;
        private CareerChoiceObject _talesOfGilesPassive2;
        private CareerChoiceObject _talesOfGilesPassive3;
        private CareerChoiceObject _talesOfGilesPassive4;
        
        private CareerChoiceObject _vividVisionsKeystone;
        private CareerChoiceObject _vividVisionsPassive1;
        private CareerChoiceObject _vividVisionsPassive2;
        private CareerChoiceObject _vividVisionsPassive3;
        private CareerChoiceObject _vividVisionsPassive4;
        
        private CareerChoiceObject _justCauseKeystone;
        private CareerChoiceObject _justCausePassive1;
        private CareerChoiceObject _justCausePassive2;
        private CareerChoiceObject _justCausePassive3;
        private CareerChoiceObject _justCausePassive4;
        
        private CareerChoiceObject _secretsOfTheGrailKeystone;
        private CareerChoiceObject _secretsOfTheGrailPassive1;
        private CareerChoiceObject _secretsOfTheGrailPassive2;
        private CareerChoiceObject _secretsOfTheGrailPassive3;
        private CareerChoiceObject _secretsOfTheGrailPassive4;
        
        private CareerChoiceObject _envoyOfTheLadyPassive1;
        private CareerChoiceObject _envoyOfTheLadyPassive2;
        private CareerChoiceObject _envoyOfTheLadyPassive3;
        private CareerChoiceObject _envoyOfTheLadyPassive4;
        private CareerChoiceObject _envoyOfTheLadyKeystone;


        protected override void RegisterAll()
        {
            _grailDamselRootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailDamselRoot"));
            
            _feyEnchantmentKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEnchantmentKeystone"));
            _feyEnchantmentPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEnchantmentPassive1"));
            _feyEnchantmentPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEnchantmentPassive2"));
            _feyEnchantmentPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEnchantmentPassive3"));
            _feyEnchantmentPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FeyEnchantmentPassive4"));
            
            _inspirationOfTheLadyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyKeystone"));
            _inspirationOfTheLadyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive1"));
            _inspirationOfTheLadyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive2"));
            _inspirationOfTheLadyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive3"));
            _inspirationOfTheLadyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("InspirationOfTheLadyPassive4"));
            
            _talesOfGilesKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesKeystone"));
            _talesOfGilesPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive1"));
            _talesOfGilesPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive2"));
            _talesOfGilesPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive3"));
            _talesOfGilesPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TalesOfGilesPassive4"));
            
            _vividVisionsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsKeystone"));
            _vividVisionsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive1"));
            _vividVisionsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive2"));
            _vividVisionsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive3"));
            _vividVisionsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VividVisionsPassive4"));
            
            _justCauseKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCauseKeystone"));
            _justCausePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive1"));
            _justCausePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive2"));
            _justCausePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive3"));
            _justCausePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("JustCausePassive4"));
            
            _secretsOfTheGrailKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOfTheGrailKeystone"));
            _secretsOfTheGrailPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOfTheGrailPassive1"));
            _secretsOfTheGrailPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOfTheGrailPassive2"));
            _secretsOfTheGrailPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOfTheGrailPassive3"));
            _secretsOfTheGrailPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SecretsOfTheGrailPassive4"));
            
            _envoyOfTheLadyKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyKeystone"));
            _envoyOfTheLadyPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive1"));
            _envoyOfTheLadyPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive2"));
            _envoyOfTheLadyPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive3"));
            _envoyOfTheLadyPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EnvoyOfTheLadyPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _grailDamselRootNode.Initialize(CareerID, "The Damsel wanders on the fey paths. Instantly teleports the player to the targeted ground position. Charges with dealt or healed damage by magic", null, true, ChoiceType.Keystone);
            
            
            _feyEnchantmentKeystone.Initialize(CareerID, "Enemies on the target area of the spell caster are knocked down", "FeyEnchantment", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "FeyPaths",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"fey_paths_enemy"}).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            
            _talesOfGilesKeystone.Initialize(CareerID, "You are immune to physical Melee damage for 5 seconds after using the fey paths", "TalesOfGiles", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "FeyPaths",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"fey_paths_self"}).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _inspirationOfTheLadyKeystone.Initialize(CareerID, "Charge Amount is doubled but companion spell and heal effects count towards the ability", "InspirationOfTheLady", false, ChoiceType.Passive);
            
            _justCauseKeystone.Initialize(CareerID, "Magical damage for troops close to the exit of the teleport  is increased by 30% for 5 secounds", "JustCause", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "FeyPaths",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new []{"fey_paths_friendly"}).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
            
            _vividVisionsKeystone.Initialize(CareerID, "Fey path charges 30% faster", "VividVisions", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special,true));
            
            _secretsOfTheGrailKeystone.Initialize(CareerID, "After using Fey Paths, a second jump is available shortly after the first one.", "SecretsOfTheGrail", false, ChoiceType.Passive);
            
            _envoyOfTheLadyKeystone.Initialize(CareerID, "When you teleport, you take all units in your closest surroundings(5m radius) with you.", "EnvoyOfTheLady", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
            }, new CareerChoiceObject.PassiveEffect());

        }

        protected override void InitializePassives()
        {
            _feyEnchantmentPassive1.Initialize(CareerID, "Increases magic spell damage by 15%.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell));
            _feyEnchantmentPassive2.Initialize(CareerID, "{=fey_enchantment_passive2_str}Increases max Winds of Magic by 10.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _feyEnchantmentPassive3.Initialize(CareerID, "{=fey_enchantment_passive3_str}All troops gain 15% extra magic damage.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.All,
                ((attacker, victim, mask) => IsBretonnianUnit(attacker))));
            _feyEnchantmentPassive4.Initialize(CareerID, "All Knight troops gain 15% Ward save.", "FeyEnchantment", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.All, 
                    (attacker, victim, attackTypeMask) => attacker.BelongsToMainParty()&& attacker.Character.IsKnightUnit()&& IsBretonnianUnit(attacker)));
            
            _inspirationOfTheLadyPassive1.Initialize(CareerID, "25% chance to recruit an extra unit free of charge.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); 
            _inspirationOfTheLadyPassive2.Initialize(CareerID, "Wounded troops in your party heal faster.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
            _inspirationOfTheLadyPassive3.Initialize(CareerID, "All Knight troops wages are reduced by 25%.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.TroopWages, true, 
                characterObject => !characterObject.IsHero && characterObject.IsKnightUnit()));
            _inspirationOfTheLadyPassive4.Initialize(CareerID, "10% Ward save if your armor weight does not exceed 11 weight.", "InspirationOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All, 
                 (attacker, victim, attackmask) => attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightUndershootCheck(attacker,11)));

            _talesOfGilesPassive1.Initialize(CareerID, "{=tales_of_giles_passive1_str}Increases max Winds of Magic by 10.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _talesOfGilesPassive2.Initialize(CareerID, "{=tales_of_giles_passive2_str}Bretonnian units receive 10% Ward save.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.Spell,
                ( (attacker, victim, mask) => IsBretonnianUnit(attacker) )));
            _talesOfGilesPassive3.Initialize(CareerID, "{=tales_of_giles_passive3_str}When praying at a shrine of the Lady, all wounded troops get healed.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
            _talesOfGilesPassive4.Initialize(CareerID, "{=tales_of_giles_passive4_str}20% spell cooldown reduction.", "TalesOfGiles", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.WindsCooldownReduction, true)); 
            
            _vividVisionsPassive1.Initialize(CareerID, "{=vivid_visions_passive1_str}Increases max Winds of Magic by 10.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _vividVisionsPassive2.Initialize(CareerID, "{=vivid_visions_passive2_str}Party movement speed is increased by 1.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
            _vividVisionsPassive3.Initialize(CareerID, "{=vivid_visions_passive3_str}Increases Magic resistance against spells by 25%.", "VividVisions", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,25),AttackTypeMask.Spell));
            _vividVisionsPassive4.Initialize(CareerID, "{=vivid_visions_passive4_str}The Spotting range of the party is increased by 20%.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            
            _justCausePassive1.Initialize(CareerID, "Upgrade costs are halved.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-0.5f, PassiveEffectType.TroopUpgradeCost));
            _justCausePassive2.Initialize(CareerID, "Non-knight units in the party gain 100 XP every day.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, false)); //CareerPerkCampaign Behavior 101
            _justCausePassive3.Initialize(CareerID, "Extra 15% Wardsave if your armor weight does not exceed 11 weight.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.Spell,
                (attacker, victim, attackmask) => attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightUndershootCheck(attacker,11)));
            _justCausePassive4.Initialize(CareerID, "Increases positive Relation gains by 20%.", "JustCause", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true)); //TorDiplomacy model 23
            
            _secretsOfTheGrailPassive1.Initialize(CareerID, "{=secrets_of_the_grail_Passive1_str}Increases lightning spell damage by 30%.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 30), AttackTypeMask.Spell));
            _secretsOfTheGrailPassive2.Initialize(CareerID, "{=secrets_of_the_grail_Passive2_str}20% cost reduction for spells.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.WindsCostReduction, true));
            _secretsOfTheGrailPassive3.Initialize(CareerID, "{=secrets_of_the_grail_Passive3_str}Casting prayers has a 30% chance to restore 10 Winds of Magic.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true)); //AbilityMissionLogic, OnCastComplete
            _secretsOfTheGrailPassive4.Initialize(CareerID, "{=secrets_of_the_grail_Passive4_str}30% prayer cooldown reduction.", "SecretsOfTheGrail", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-30, PassiveEffectType.PrayerCoolDownReduction, true));
            
            _envoyOfTheLadyPassive1.Initialize(CareerID, "{=ambassador_of_the_lady_Passive1_str}Increases Magic damage by 20%.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 20), AttackTypeMask.Spell));
            _envoyOfTheLadyPassive2.Initialize(CareerID, "{=ambassador_of_the_lady_Passive2_str}Knight Companion health points are doubled.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, true));
            _envoyOfTheLadyPassive3.Initialize(CareerID, "{=ambassador_of_the_lady_Passive3_str}Damsel Companion have 25 more Winds of Magic.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, false)); //AbilityMissionLogic, OnCastComplete
            _envoyOfTheLadyPassive4.Initialize(CareerID, "{=ambassador_of_the_lady_Passive4_str}Diplomatic force options for all Brettonnian Leaders.", "EnvoyOfTheLady", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true));
        }

        private static bool IsBretonnianUnit(Agent agent)
        {
            if (agent.IsHero) return false;
            if (!agent.BelongsToMainParty()) return false;
            if (agent.IsMainAgent) return false;
            
            
            return agent.Character.Culture.Name.ToString() == TORConstants.Cultures.BRETONNIA;
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