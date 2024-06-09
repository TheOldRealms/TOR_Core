using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class WaywatcherCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _wayWatcherRoot;
    private CareerChoiceObject _protectorOfTheWoodsKeystone;
    private CareerChoiceObject _protectorOfTheWoodsPassive1;
    private CareerChoiceObject _protectorOfTheWoodsPassive2;
    private CareerChoiceObject _protectorOfTheWoodsPassive3;
    private CareerChoiceObject _protectorOfTheWoodsPassive4;
    
    private CareerChoiceObject _pathfinderKeystone;
    private CareerChoiceObject _pathfinderPassive1;
    private CareerChoiceObject _pathfinderPassive2;
    private CareerChoiceObject _pathfinderPassive3;
    private CareerChoiceObject _pathfinderPassive4;
    
    private CareerChoiceObject _forestStalkerKeystone;
    private CareerChoiceObject _forestStalkerPassive1;
    private CareerChoiceObject _forestStalkerPassive2;
    private CareerChoiceObject _forestStalkerPassive3;
    private CareerChoiceObject _forestStalkerPassive4;
    
    private CareerChoiceObject _shiftshiverShardsKeystone;
    private CareerChoiceObject _shiftshiverShardsPassive1;
    private CareerChoiceObject _shiftshiverShardsPassive2;
    private CareerChoiceObject _shiftshiverShardsPassive3;
    private CareerChoiceObject _shiftshiverShardsPassive4;
    
    private CareerChoiceObject _hagbaneTipsKeystone;
    private CareerChoiceObject _hagbaneTipsPassive1;
    private CareerChoiceObject _hagbaneTipsPassive2;
    private CareerChoiceObject _hagbaneTipsPassive3;
    private CareerChoiceObject _hagbaneTipsPassive4;
    
    private CareerChoiceObject _starfireShaftsKeystone;
    private CareerChoiceObject _starfireShaftsPassive1;
    private CareerChoiceObject _starfireShaftsPassive2;
    private CareerChoiceObject _starfireShaftsPassive3;
    private CareerChoiceObject _starfireShaftsPassive4;
    
    private CareerChoiceObject _eyeOfTheHunterKeystone;
    private CareerChoiceObject _eyeOfTheHunterPassive1;
    private CareerChoiceObject _eyeOfTheHunterPassive2;
    private CareerChoiceObject _eyeOfTheHunterPassive3;
    private CareerChoiceObject _eyeOfTheHunterPassive4;

    protected override void RegisterAll()
    {
        _wayWatcherRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wayWatcherRoot).UnderscoreFirstCharToUpper())); 

        _protectorOfTheWoodsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_protectorOfTheWoodsKeystone).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive1).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive2).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive3).UnderscoreFirstCharToUpper()));
        _protectorOfTheWoodsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_protectorOfTheWoodsPassive4).UnderscoreFirstCharToUpper()));

        _pathfinderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderKeystone).UnderscoreFirstCharToUpper()));
        _pathfinderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive1).UnderscoreFirstCharToUpper()));
        _pathfinderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive2).UnderscoreFirstCharToUpper()));
        _pathfinderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive3).UnderscoreFirstCharToUpper()));
        _pathfinderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_pathfinderPassive4).UnderscoreFirstCharToUpper()));

        _forestStalkerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerKeystone).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive1).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive2).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive3).UnderscoreFirstCharToUpper()));
        _forestStalkerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_forestStalkerPassive4).UnderscoreFirstCharToUpper()));

        _shiftshiverShardsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shiftshiverShardsKeystone).UnderscoreFirstCharToUpper()));
        _shiftshiverShardsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shiftshiverShardsPassive1).UnderscoreFirstCharToUpper()));
        _shiftshiverShardsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shiftshiverShardsPassive2).UnderscoreFirstCharToUpper()));
        _shiftshiverShardsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shiftshiverShardsPassive3).UnderscoreFirstCharToUpper()));
        _shiftshiverShardsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_shiftshiverShardsPassive4).UnderscoreFirstCharToUpper()));

        _hagbaneTipsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hagbaneTipsKeystone).UnderscoreFirstCharToUpper()));
        _hagbaneTipsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hagbaneTipsPassive1).UnderscoreFirstCharToUpper()));
        _hagbaneTipsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hagbaneTipsPassive2).UnderscoreFirstCharToUpper()));
        _hagbaneTipsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hagbaneTipsPassive3).UnderscoreFirstCharToUpper()));
        _hagbaneTipsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hagbaneTipsPassive4).UnderscoreFirstCharToUpper()));

        _starfireShaftsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireShaftsKeystone).UnderscoreFirstCharToUpper()));
        _starfireShaftsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireShaftsPassive1).UnderscoreFirstCharToUpper()));
        _starfireShaftsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireShaftsPassive2).UnderscoreFirstCharToUpper()));
        _starfireShaftsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireShaftsPassive3).UnderscoreFirstCharToUpper()));
        _starfireShaftsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireShaftsPassive4).UnderscoreFirstCharToUpper()));

        _eyeOfTheHunterKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterKeystone).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive1).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive2).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive3).UnderscoreFirstCharToUpper()));
        _eyeOfTheHunterPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_eyeOfTheHunterPassive4).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _wayWatcherRoot.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(StatusEffectTemplate),
                    MutationTargetOriginalId = "righteous_fury_effect",
                    PropertyName = "TemporaryAttributes",
                    PropertyValue = (choice, originalValue, agent) => new List<string> { "Unstoppable", "Unbreakable" },
                    MutationType = OperationType.Replace
                },
            });   
    }

    protected override void InitializePassives()
    {
        _protectorOfTheWoodsPassive1.Initialize(CareerID, "Extra ranged damage (10%).", "ProtectorOfTheWoods", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
        _protectorOfTheWoodsPassive2.Initialize(CareerID, "5 extra ammo", "ProtectorOfTheWoods", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
        _protectorOfTheWoodsPassive3.Initialize(CareerID, "All ranged troops wages are reduced by 20%", "ProtectorOfTheWoods", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.TroopWages, true, 
            characterObject => !characterObject.IsHero && characterObject.IsRanged));
        _protectorOfTheWoodsPassive4.Initialize(CareerID, "Reduce range Accuracy movement penalty by 15%.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.RangedMovementPenalty, true));  
        
        _pathfinderPassive1.Initialize(CareerID, "{=vivid_visions_passive4_str}The Spotting range of the party is increased by 20%.", "Pathfinder", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
        _pathfinderPassive2.Initialize(CareerID, "{=vivid_visions_passive2_str}Party movement speed is increased by 1.", "Pathfinder", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
        _pathfinderPassive3.Initialize(CareerID, "Extra ranged damage (10%).", "Pathfinder", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
        _pathfinderPassive4.Initialize(CareerID, "TRAVEL THROUGH SNOW.", "Pathfinder", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed,true)); 
        
        _forestStalkerPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "ForestStalker", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _forestStalkerPassive2.Initialize(CareerID, "Gain 20% range resistance.", "ForestStalker", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged));
        _forestStalkerPassive3.Initialize(CareerID, "Increases range damage resistance of melee troops by 20%.", "ForestStalker", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged, 
            (attacker, victim, mask) => !victim.BelongsToMainParty()&& !(victim.IsMainAgent || victim.IsHero)&& !victim.IsRangedCached &&  mask == AttackTypeMask.Melee ));
        _forestStalkerPassive4.Initialize(CareerID,"20% Equipment weight Reduction","ForestStalker",false,ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        
        _shiftshiverShardsPassive1.Initialize(CareerID, "10 extra ammo", "ShiftshiverShards", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
        _shiftshiverShardsPassive2.Initialize(CareerID,"Ranged troops gain 25XP daily ","ShiftshiverShards", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _shiftshiverShardsPassive3.Initialize(CareerID,"Attacking unaware enemies adds 50% extra damage","ShiftshiverShards",false,ChoiceType.Passive);
        _shiftshiverShardsPassive4.Initialize(CareerID,"PLACEHOLDER","ShiftshiverShards",false,ChoiceType.Passive);
        
        _hagbaneTipsPassive1.Initialize(CareerID, "10 extra ammo", "HagbaneTips", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo)); 
        _hagbaneTipsPassive2.Initialize(CareerID,"20% Equipment weight Reduction","HagbaneTips",false,ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _hagbaneTipsPassive3.Initialize(CareerID,"SPECIAL Headshots double the fill","HagbaneTips",false,ChoiceType.Passive);
        _hagbaneTipsPassive4.Initialize(CareerID,"FOURTH ARROW","HagbaneTips",false,ChoiceType.Passive);
        
        _starfireShaftsPassive1.Initialize(CareerID, "10 extra ammo", "StarfireShafts", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo)); 
        _starfireShaftsPassive2.Initialize(CareerID, "15% swing speed", "StarfireShafts", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SwingSpeed));
        _starfireShaftsPassive3.Initialize(CareerID,"PENETRATE SHIELDS","StarfireShafts",false,ChoiceType.Passive);
        _starfireShaftsPassive4.Initialize(CareerID,"explosive arrow","StarfireShafts",false,ChoiceType.Passive);
        
        _eyeOfTheHunterPassive1.Initialize(CareerID,"20% Equipment weight Reduction","EyeOfTheHunter",false,ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _eyeOfTheHunterPassive2.Initialize(CareerID,"Multiple targets","EyeOfTheHunter",false,ChoiceType.Passive);
        _eyeOfTheHunterPassive3.Initialize(CareerID,"50 Archer skill points","EyeOfTheHunter",false,ChoiceType.Passive , null, new CareerChoiceObject.PassiveEffect(50));
        _eyeOfTheHunterPassive4.Initialize(CareerID,"ALL ARROW EFFECTS DOUBLE","EyeOfTheHunter",false,ChoiceType.Passive);
        
    }
}