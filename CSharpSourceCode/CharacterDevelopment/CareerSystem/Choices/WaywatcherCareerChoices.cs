using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

public class WaywatcherCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _mageOfLightRoot;
    private CareerChoiceObject _archerByNatureKeystone;
    private CareerChoiceObject _archerByNaturePassive1;
    private CareerChoiceObject _archerByNaturePassive2;
    private CareerChoiceObject _archerByNaturePassive3;
    private CareerChoiceObject _archerByNaturePassive4;
    
    private CareerChoiceObject _trackerKeystone;
    private CareerChoiceObject _trackerPassive1;
    private CareerChoiceObject _trackerPassive2;
    private CareerChoiceObject _trackerPassive3;
    private CareerChoiceObject _trackerPassive4;
    
    private CareerChoiceObject _survivalKeystone;
    private CareerChoiceObject _survivalPassive1;
    private CareerChoiceObject _survivalPassive2;
    private CareerChoiceObject _survivalPassive3;
    private CareerChoiceObject _survivalPassive4;
    
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
        _mageOfLightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_mageOfLightRoot).UnderscoreFirstCharToUpper()));

        _archerByNatureKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_archerByNatureKeystone).UnderscoreFirstCharToUpper()));
        _archerByNaturePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_archerByNaturePassive1).UnderscoreFirstCharToUpper()));
        _archerByNaturePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_archerByNaturePassive2).UnderscoreFirstCharToUpper()));
        _archerByNaturePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_archerByNaturePassive3).UnderscoreFirstCharToUpper()));
        _archerByNaturePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_archerByNaturePassive4).UnderscoreFirstCharToUpper()));

        _trackerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_trackerKeystone).UnderscoreFirstCharToUpper()));
        _trackerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_trackerPassive1).UnderscoreFirstCharToUpper()));
        _trackerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_trackerPassive2).UnderscoreFirstCharToUpper()));
        _trackerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_trackerPassive3).UnderscoreFirstCharToUpper()));
        _trackerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_trackerPassive4).UnderscoreFirstCharToUpper()));

        _survivalKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_survivalKeystone).UnderscoreFirstCharToUpper()));
        _survivalPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_survivalPassive1).UnderscoreFirstCharToUpper()));
        _survivalPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_survivalPassive2).UnderscoreFirstCharToUpper()));
        _survivalPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_survivalPassive3).UnderscoreFirstCharToUpper()));
        _survivalPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_survivalPassive4).UnderscoreFirstCharToUpper()));

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
        
    }

    protected override void InitializePassives()
    {
        _archerByNaturePassive1.Initialize(CareerID, "Extra ranged damage (10%).", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
        _archerByNaturePassive2.Initialize(CareerID,"PLACEHOLDER","TEST",false,ChoiceType.Passive);
        _archerByNaturePassive3.Initialize(CareerID, "5 extra ammo", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
        _archerByNaturePassive4.Initialize(CareerID, "All ranged troops wages are reduced by 20%", "SigmarsProclaimer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.TroopWages, true, 
            characterObject => !characterObject.IsHero && characterObject.IsRanged));
        
        _trackerPassive1.Initialize(CareerID, "{=vivid_visions_passive4_str}The Spotting range of the party is increased by 20%.", "Tracking", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
        _trackerPassive2.Initialize(CareerID, "{=vivid_visions_passive2_str}Party movement speed is increased by 1.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
        _trackerPassive3.Initialize(CareerID, "Extra ranged damage (10%).", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
        _trackerPassive4.Initialize(CareerID,"WEIGHT REDUCTION","TEST",false,ChoiceType.Passive);
        
        _survivalPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "CrusherOfTheWeak", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _survivalPassive2.Initialize(CareerID, "Increases range damage resistance of melee troops by 10%.", "TeachingsOfTheWinterfather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
        _survivalPassive3.Initialize(CareerID, "Increases range damage resistance of melee troops by 20%.", "TeachingsOfTheWinterfather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged, 
            (attacker, victim, mask) => !victim.BelongsToMainParty()&& !(victim.IsMainAgent || victim.IsHero)&& !victim.IsRangedCached &&  mask == AttackTypeMask.Melee ));
        _survivalPassive4.Initialize(CareerID, "Weapon swing speed increased by 15%.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15f, PassiveEffectType.SwingSpeed,true)); 
        
        _hagbaneTipsPassive1.Initialize(CareerID, "5 extra ammo", "Survivalist", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
        _hagbaneTipsPassive2.Initialize(CareerID,"WEIGHT REDUCTION","TEST",false,ChoiceType.Passive);
        

        
        
    }
}
