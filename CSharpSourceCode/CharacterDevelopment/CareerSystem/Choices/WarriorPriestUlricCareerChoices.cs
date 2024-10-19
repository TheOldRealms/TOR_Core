using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
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
    public class WarriorPriestUlricCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _warriorPriestUlricRoot;
        private CareerChoiceObject _crusherOfTheWeakKeystone;
        private CareerChoiceObject _crusherOfTheWeakPassive1;
        private CareerChoiceObject _crusherOfTheWeakPassive2;
        private CareerChoiceObject _crusherOfTheWeakPassive3;
        private CareerChoiceObject _crusherOfTheWeakPassive4;
        
        private CareerChoiceObject _wildPackKeystone;
        private CareerChoiceObject _wildPackPassive1;
        private CareerChoiceObject _wildPackPassive2;
        private CareerChoiceObject _wildPackPassive3;
        private CareerChoiceObject _wildPackPassive4;
        
        private CareerChoiceObject _teachingsOfTheWinterFatherKeystone;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive1;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive2;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive3;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive4;
        
        private CareerChoiceObject _frostsBiteKeystone;
        private CareerChoiceObject _frostsBitePassive1;
        private CareerChoiceObject _frostsBitePassive2;
        private CareerChoiceObject _frostsBitePassive3;
        private CareerChoiceObject _frostsBitePassive4;
        
        private CareerChoiceObject _runesOfTheWhiteWolfKeystone;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive1;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive2;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive3;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive4;
        
        private CareerChoiceObject _furyOfWarKeystone;
        private CareerChoiceObject _furyOfWarPassive1;
        private CareerChoiceObject _furyOfWarPassive2;
        private CareerChoiceObject _furyOfWarPassive3;
        private CareerChoiceObject _furyOfWarPassive4;
        
        private CareerChoiceObject _flameOfUlricKeystone;
        private CareerChoiceObject _flameOfUlricPassive1;
        private CareerChoiceObject _flameOfUlricPassive2;
        private CareerChoiceObject _flameOfUlricPassive3;
        private CareerChoiceObject _flameOfUlricPassive4;


        protected override void RegisterAll()
        {
            _warriorPriestUlricRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_warriorPriestUlricRoot).UnderscoreFirstCharToUpper()));

            _crusherOfTheWeakKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakKeystone).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive1).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive2).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive3).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive4).UnderscoreFirstCharToUpper()));

            _wildPackKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackKeystone).UnderscoreFirstCharToUpper()));
            _wildPackPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive1).UnderscoreFirstCharToUpper()));
            _wildPackPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive2).UnderscoreFirstCharToUpper()));
            _wildPackPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive3).UnderscoreFirstCharToUpper()));
            _wildPackPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive4).UnderscoreFirstCharToUpper()));

            _teachingsOfTheWinterFatherKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherKeystone).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive1).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive2).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive3).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive4).UnderscoreFirstCharToUpper()));

            _frostsBiteKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBiteKeystone).UnderscoreFirstCharToUpper()));
            _frostsBitePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive1).UnderscoreFirstCharToUpper()));
            _frostsBitePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive2).UnderscoreFirstCharToUpper()));
            _frostsBitePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive3).UnderscoreFirstCharToUpper()));
            _frostsBitePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive4).UnderscoreFirstCharToUpper()));

            _runesOfTheWhiteWolfKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfKeystone).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive1).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive2).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive3).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive4).UnderscoreFirstCharToUpper()));

            _furyOfWarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarKeystone).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive1).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive2).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive3).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive4).UnderscoreFirstCharToUpper()));
            
            _flameOfUlricKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricKeystone).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive1= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive1).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive2= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive2).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive3= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive3).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive4= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _warriorPriestUlricRoot.Initialize(CareerID, "The Wolf Priest unleashes a mighty blow in the name of his feral god, bringing the full might of his weapon crashing to the ground as if he were wielding Blitzbeil itself. This attack cannot be parried and affects targets in a 2-meter radius. For every point of the players highest weapon skill, damage is increased by 0.1%.", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.TwoHanded,DefaultSkills.OneHanded,DefaultSkills.Polearm }, 0.06f,true),
                        MutationType = OperationType.Add
                    },
                });
            _crusherOfTheWeakKeystone.Initialize(CareerID, "Enemies are knocked down from the blow of the ability.", "CrusherOfTheWeak", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "AxeOfUlric",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] {"ulric_smash_knockdown" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                },new CareerChoiceObject.PassiveEffect());
            
            _wildPackKeystone.Initialize(CareerID, "Leadership counts towards career ability", "WildPack", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.06f),
                        MutationType = OperationType.Add
                    },
                });
            _teachingsOfTheWinterFatherKeystone.Initialize(CareerID, "The radius of the attack is increased by 1 meters. Faith counts towards ability", "TeachingsOfTheWinterfather", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.06f),
                        MutationType = OperationType.Add
                    },
                });
            _frostsBiteKeystone.Initialize(CareerID, "Enemies affected by the attack are slowed down for 6 seconds. Damage of the attack is now Frost damage.", "FrostsBite", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageType",
                        PropertyValue = (choice, originalValue, agent) => DamageType.Frost,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] {"frost_bite_mov_90" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            _runesOfTheWhiteWolfKeystone.Initialize(CareerID, "All Enemies hit by the Axe of Ulric suffer from a damage over time effect", "RunesOfTheWhiteWolf", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "white_wolf_dot" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            
            _furyOfWarKeystone.Initialize(CareerID, "Required charge for ability is halved.", "FuryOfWar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }); // special
            
            _flameOfUlricKeystone.Initialize(CareerID, "For every executed Axe of Ulric one of your prayer cooldowns gets randomly reset.", "FlameOfUlric", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                });
        }

        protected override void InitializePassives()
        {
            _crusherOfTheWeakPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "CrusherOfTheWeak", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _crusherOfTheWeakPassive2.Initialize(CareerID, "Extra melee damage (10%).", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _crusherOfTheWeakPassive3.Initialize(CareerID, "Extra melee damage if the target is below tier 4 (10%).", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && victim!=null&&!victim.IsHero && victim.Character.Level < 16));
            _crusherOfTheWeakPassive4.Initialize(CareerID, "Prayers are recharged on battle start.", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1,PassiveEffectType.Special,true));
            
            _wildPackPassive1.Initialize(CareerID, "Increases melee physical resistance by 10%.", "WildPack", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Melee));
            _wildPackPassive2.Initialize(CareerID, "Increases Party size by 10.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _wildPackPassive3.Initialize(CareerID, "Increases health regeneration after battles by 2.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.HealthRegeneration));
            _wildPackPassive4.Initialize(CareerID, "Party movement speed is increased by 1.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
            
            _teachingsOfTheWinterFatherPassive1.Initialize(CareerID, "Wounded troops in your party heal faster.", "TeachingsOfTheWinterfather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
            _teachingsOfTheWinterFatherPassive2.Initialize(CareerID,"Praying at a shrine of ulric refills player health completely","TeachingsOfTheWinterfather",false,ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect()); 
            _teachingsOfTheWinterFatherPassive3.Initialize(CareerID, "{=night_rider_passive4_str}Attacks deal bonus damage against shields.", "TeachingsOfTheWinterfather", false, ChoiceType.Passive, null);
            _teachingsOfTheWinterFatherPassive4.Initialize(CareerID, "Increases range damage resistance of melee troops by 20%.", "TeachingsOfTheWinterfather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged, 
                (attacker, victim, mask) => victim.BelongsToMainParty()&& !(victim.IsMainAgent || victim.IsHero)&& mask == AttackTypeMask.Melee ));
            
            _frostsBitePassive1.Initialize(CareerID, "Extra frost damage (10%).", "FrostsBite", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Frost, 10), AttackTypeMask.Melee));
            _frostsBitePassive2.Initialize(CareerID, "Add 10% frost damage to melee troops.", "FrostsBite", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Frost, 10), AttackTypeMask.Melee));
            _frostsBitePassive3.Initialize(CareerID, "Party is not slowed by snow.", "FrostsBite", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46
            _frostsBitePassive4.Initialize(CareerID, "Increase hex durations by 20%.", "FrostsBite", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.DebuffDuration,true)); 
            
            _runesOfTheWhiteWolfPassive1.Initialize(CareerID,"Wearing wolf heads or pelts increases wardsave by 10%","RunesOfTheWhiteWolf",false,ChoiceType.Passive, null , new CareerChoiceObject.PassiveEffect(10,PassiveEffectType.Special,true)); //TODO
            _runesOfTheWhiteWolfPassive2.Initialize(CareerID, "Increases Hitpoints by 50.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _runesOfTheWhiteWolfPassive3.Initialize(CareerID, "Increase prayer durations by 20%.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.BuffDuration,true));
            _runesOfTheWhiteWolfPassive4.Initialize(CareerID, "Ulrican troops gain 20% Ward save.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 20), AttackTypeMask.All, 
                (attacker, victim, mask) => victim.IsPlayerTroop && victim.Character.UnitBelongsToCult("ulric") ));
            
            _furyOfWarPassive1.Initialize(CareerID, "Every  equipped melee weapon increases melee damage by 5%.", "FuryOfWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5,PassiveEffectType.Special,true));
            _furyOfWarPassive2.Initialize(CareerID, "Weapon swing speed increased by 10%.", "FuryOfWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SwingSpeed,true)); 
            _furyOfWarPassive3.Initialize(CareerID,"Battles with even or unfavorable odds refresh your Ulric blessing.","FuryOfWar",false,ChoiceType.Passive);
            _furyOfWarPassive4.Initialize(CareerID,"Hits below 15 damage do not stagger the player.","FuryOfWar",false,ChoiceType.Passive); 
            
            _flameOfUlricPassive1.Initialize(CareerID, "Increases range of prayers by 50%.", "FlameOfUlric", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50f, PassiveEffectType.SpellRadius,true));
            _flameOfUlricPassive2.Initialize(CareerID, "Extra 20% armor penetration of melee attacks.", "FlameOfUlric", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _flameOfUlricPassive3.Initialize(CareerID,"Battles with even or unfavorable odds provide double Prestige.","FlameOfUlric",false,ChoiceType.Passive);
            _flameOfUlricPassive4.Initialize(CareerID,"For every kill through abilities gain 0.25 health points","FlameOfUlric",false,ChoiceType.Passive); 
            
        }
        
    }
}