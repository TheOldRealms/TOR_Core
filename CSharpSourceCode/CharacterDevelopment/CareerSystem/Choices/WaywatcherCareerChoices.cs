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
    
    private CareerChoiceObject _hailOfArrowsKeystone;
    private CareerChoiceObject _hailOfArrowsPassive1;
    private CareerChoiceObject _hailOfArrowsPassive2;
    private CareerChoiceObject _hailOfArrowsPassive3;
    private CareerChoiceObject _hailOfArrowsPassive4;
    
    private CareerChoiceObject _hawkeyedKeystone;
    private CareerChoiceObject _hawkeyedPassive1;
    private CareerChoiceObject _hawkeyedPassive2;
    private CareerChoiceObject _hawkeyedPassive3;
    private CareerChoiceObject _hawkeyedPassive4;
    
    private CareerChoiceObject _starfireEssenceKeystone;
    private CareerChoiceObject _starfireEssencePassive1;
    private CareerChoiceObject _starfireEssencePassive2;
    private CareerChoiceObject _starfireEssencePassive3;
    private CareerChoiceObject _starfireEssencePassive4;
    
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

        _hailOfArrowsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsKeystone).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive1).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive2).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive3).UnderscoreFirstCharToUpper()));
        _hailOfArrowsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hailOfArrowsPassive4).UnderscoreFirstCharToUpper()));

        _hawkeyedKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedKeystone).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive1).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive2).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive3).UnderscoreFirstCharToUpper()));
        _hawkeyedPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_hawkeyedPassive4).UnderscoreFirstCharToUpper()));

        _starfireEssenceKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssenceKeystone).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive1).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive2).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive3).UnderscoreFirstCharToUpper()));
        _starfireEssencePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_starfireEssencePassive4).UnderscoreFirstCharToUpper()));

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
        _pathfinderPassive3.Initialize(CareerID,"Party travels unhindered through snow","Pathfinder",false,ChoiceType.Passive);
        _pathfinderPassive4.Initialize(CareerID,"Ranged damage is shrugged off","Pathfinder",false,ChoiceType.Passive);
        
        _forestStalkerPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "ForestStalker", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
        _forestStalkerPassive2.Initialize(CareerID, "Gain 20% range resistance.", "ForestStalker", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged));
        _forestStalkerPassive3.Initialize(CareerID, "Increases range damage resistance of melee troops by 20%.", "ForestStalker", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged, 
            (attacker, victim, mask) => !victim.BelongsToMainParty()&& !(victim.IsMainAgent || victim.IsHero)&& !victim.IsRangedCached &&  mask == AttackTypeMask.Melee ));
        _forestStalkerPassive4.Initialize(CareerID,"20% Equipment weight Reduction","ForestStalker",false,ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        
        _hailOfArrowsPassive1.Initialize(CareerID, "15 extra ammo", "HailOfArrows", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Ammo));
        _hailOfArrowsPassive2.Initialize(CareerID,"Ranged troops gain 25XP daily ","HailOfArrows", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _hailOfArrowsPassive3.Initialize(CareerID,"Attacking unaware enemies adds 50% extra damage","HailOfArrows",false,ChoiceType.Passive);
        _hailOfArrowsPassive4.Initialize(CareerID,"For every hit arrow, your magic damage increase. Bonus slowly decrease.","HailOfArrows",false,ChoiceType.Passive);
        
        _hawkeyedPassive1.Initialize(CareerID,"20% Equipment weight Reduction","Hawkeyed",false,ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _hawkeyedPassive2.Initialize(CareerID,"SPECIAL Headshots double the fill","Hawkeyed",false,ChoiceType.Passive);
        _hawkeyedPassive3.Initialize(CareerID,"While Zoomed in, the time is slowed down","Hawkeyed",false,ChoiceType.Passive);
        _hawkeyedPassive4.Initialize(CareerID,"Every sixth arrow, applies a slow down effect on impact","Hawkeyed",false,ChoiceType.Passive);
        
        _starfireEssencePassive1.Initialize(CareerID, "15 extra ammo", "StarfireEssence", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Ammo)); 
        _starfireEssencePassive2.Initialize(CareerID, "15% swing speed", "StarfireEssence", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.SwingSpeed));
        _starfireEssencePassive3.Initialize(CareerID,"Your arrows can penetrate shields","StarfireEssence",false,ChoiceType.Passive);
        _starfireEssencePassive4.Initialize(CareerID,"Not shooting an arrows increase the chance your next arrow explode on impact.","StarfireEssence",false,ChoiceType.Passive);
        
        _eyeOfTheHunterPassive1.Initialize(CareerID,"20% Equipment weight Reduction","EyeOfTheHunter",false,ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.EquipmentWeightReduction, true));
        _eyeOfTheHunterPassive2.Initialize(CareerID,"Multiple targets","EyeOfTheHunter",false,ChoiceType.Passive);
        _eyeOfTheHunterPassive3.Initialize(CareerID,"50 Archer skill points","EyeOfTheHunter",false,ChoiceType.Passive , null, new CareerChoiceObject.PassiveEffect(50));
        _eyeOfTheHunterPassive4.Initialize(CareerID,"Special shot efficiency is doubled","EyeOfTheHunter",false,ChoiceType.Passive);
        
    }
}