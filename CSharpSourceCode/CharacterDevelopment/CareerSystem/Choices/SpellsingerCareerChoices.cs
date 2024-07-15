using System.Collections.Generic;
using TOR_Core.CampaignMechanics.Choices;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class SpellsingerCareerChoices : TORCareerChoicesBase
{
    public SpellsingerCareerChoices(CareerObject id) : base(id)
    {
        
    }

    private CareerChoiceObject _spellSingerRoot;
    
    private CareerChoiceObject _pathShapingKeystone;
    private CareerChoiceObject _pathShapingPassive1;
    private CareerChoiceObject _pathShapingPassive2;
    private CareerChoiceObject _pathShapingPassive3;
    private CareerChoiceObject _pathShapingPassive4;

    private CareerChoiceObject _treeSingingKeystone;
    private CareerChoiceObject _treeSingingPassive1;
    private CareerChoiceObject _treeSingingPassive2;
    private CareerChoiceObject _treeSingingPassive3;
    private CareerChoiceObject _treeSingingPassive4;

    private CareerChoiceObject _vitalSurgeKeystone;
    private CareerChoiceObject _vitalSurgePassive1;
    private CareerChoiceObject _vitalSurgePassive2;
    private CareerChoiceObject _vitalSurgePassive3;
    private CareerChoiceObject _vitalSurgePassive4;

    private CareerChoiceObject _heartOfTheTreeKeystone;
    private CareerChoiceObject _heartOfTheTreePassive1;
    private CareerChoiceObject _heartOfTheTreePassive2;
    private CareerChoiceObject _heartOfTheTreePassive3;
    private CareerChoiceObject _heartOfTheTreePassive4;

    private CareerChoiceObject _arielsBlessingKeystone;
    private CareerChoiceObject _arielsBlessingPassive1;
    private CareerChoiceObject _arielsBlessingPassive2;
    private CareerChoiceObject _arielsBlessingPassive3;
    private CareerChoiceObject _arielsBlessingPassive4;

    private CareerChoiceObject _magicOfAthelLorenKeystone;
    private CareerChoiceObject _magicOfAthelLorenPassive1;
    private CareerChoiceObject _magicOfAthelLorenPassive2;
    private CareerChoiceObject _magicOfAthelLorenPassive3;
    private CareerChoiceObject _magicOfAthelLorenPassive4;

    private CareerChoiceObject _furyOfTheForestKeystone;
    private CareerChoiceObject _furyOfTheForestPassive1;
    private CareerChoiceObject _furyOfTheForestPassive2;
    private CareerChoiceObject _furyOfTheForestPassive3;
    private CareerChoiceObject _furyOfTheForestPassive4;


    protected override void RegisterAll()
    {
        _spellSingerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SpellSingerRoot"));

        _pathShapingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingKeystone"));
        _pathShapingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive1"));
        _pathShapingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive2"));
        _pathShapingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive3"));
        _pathShapingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PathShapingPassive4"));

        _treeSingingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingKeystone"));
        _treeSingingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive1"));
        _treeSingingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive2"));
        _treeSingingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive3"));
        _treeSingingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TreeSingingPassive4"));

        _vitalSurgeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgeKeystone"));
        _vitalSurgePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive1"));
        _vitalSurgePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive2"));
        _vitalSurgePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive3"));
        _vitalSurgePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VitalSurgePassive4"));

        _heartOfTheTreeKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreeKeystone"));
        _heartOfTheTreePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive1"));
        _heartOfTheTreePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive2"));
        _heartOfTheTreePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive3"));
        _heartOfTheTreePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HeartOfTheTreePassive4"));

        _arielsBlessingKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingKeystone"));
        _arielsBlessingPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive1"));
        _arielsBlessingPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive2"));
        _arielsBlessingPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive3"));
        _arielsBlessingPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ArielsBlessingPassive4"));

        _magicOfAthelLorenKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenKeystone"));
        _magicOfAthelLorenPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive1"));
        _magicOfAthelLorenPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive2"));
        _magicOfAthelLorenPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive3"));
        _magicOfAthelLorenPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MagicOfAthelLorenPassive4"));

        _furyOfTheForestKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestKeystone"));
        _furyOfTheForestPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive1"));
        _furyOfTheForestPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive2"));
        _furyOfTheForestPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive3"));
        _furyOfTheForestPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FuryOfTheForestPassive4"));
    }

    protected override void InitializeKeyStones()
    {
        _spellSingerRoot.Initialize(CareerID, "Summon a champion that the necromancer take control of. The Champion loses every 2 seconds 5 health points. For every 3 points in spell casting skill the champion gains 1 health point. Charging: applying spell- damage or healing. Alternatively, Let undead units inflict damage.", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "GreaterHarbinger",
                    PropertyName = "ScaleVariable1",
                    PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.SpellCraft}, 0.33f),
                    MutationType = OperationType.Add
                }
            });
    }

    protected override void InitializePassives()
    {
        _pathShapingPassive4.Initialize(CareerID, "{=vivid_visions_passive2_str}Party movement speed is increased by 1.", "VividVisions", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));

    }
}