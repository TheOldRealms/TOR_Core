using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class NecromancerCareerChoices : TORCareerChoicesBase
    {
        public NecromancerCareerChoices(CareerObject id) : base(id) {}
        private CareerChoiceObject _necromancerRoot;
        
        private CareerChoiceObject _liberNecrisKeystone; // 1
        private CareerChoiceObject _liberNecrisPassive1;
        private CareerChoiceObject _liberNecrisPassive2;
        private CareerChoiceObject _liberNecrisPassive3;
        private CareerChoiceObject _liberNecrisPassive4;
        
        private CareerChoiceObject _deArcanisKadonKeystone; // 2
        private CareerChoiceObject _deArcanisKadonPassive1;
        private CareerChoiceObject _deArcanisKadonPassive2;
        private CareerChoiceObject _deArcanisKadonPassive3;
        private CareerChoiceObject _deArcanisKadonPassive4;
        
        private CareerChoiceObject _codexMortificaKeystone;     //3
        private CareerChoiceObject _codexMortificaPassive1;
        private CareerChoiceObject _codexMortificaPassive2;
        private CareerChoiceObject _codexMortificaPassive3;
        private CareerChoiceObject _codexMortificaPassive4;
        
        private CareerChoiceObject _liberMortisKeystone; //4
        private CareerChoiceObject _liberMortisPassive1;
        private CareerChoiceObject _liberMortisPassive2;
        private CareerChoiceObject _liberMortisPassive3;
        private CareerChoiceObject _liberMortisPassive4;
        
        private CareerChoiceObject _bookofWsoranKeystone;        //5
        private CareerChoiceObject _bookofWsoranPassive1;
        private CareerChoiceObject _bookofWsoranPassive2;
        private CareerChoiceObject _bookofWsoranPassive3;
        private CareerChoiceObject _bookofWsoranPassive4;
        
        private CareerChoiceObject _grimoireNecrisKeystone; //6
        private CareerChoiceObject _grimoireNecrisPassive1;
        private CareerChoiceObject _grimoireNecrisPassive2;
        private CareerChoiceObject _grimoireNecrisPassive3;
        private CareerChoiceObject _grimoireNecrisPassive4;
        
        
        private CareerChoiceObject _booksOfNagashKeystone; // 7
        private CareerChoiceObject _booksOfNagashPassive1;
        private CareerChoiceObject _booksOfNagashPassive2;
        private CareerChoiceObject _booksOfNagashPassive3;
        private CareerChoiceObject _booksOfNagashPassive4;


        protected override void RegisterAll()
        {
            _necromancerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NecromancerRoot"));
            
            _liberNecrisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisKeystone"));
            _liberNecrisPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive1"));
            _liberNecrisPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive2"));
            _liberNecrisPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive3"));
            _liberNecrisPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive4"));
            
            _deArcanisKadonKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonKeystone"));
            _deArcanisKadonPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive1"));
            _deArcanisKadonPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive2"));
            _deArcanisKadonPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive3"));
            _deArcanisKadonPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive4"));
            
            _codexMortificaKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaKeystone"));
            _codexMortificaPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive1"));
            _codexMortificaPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive2"));
            _codexMortificaPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive3"));
            _codexMortificaPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive4"));
            
            _liberMortisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisKeystone"));
            _liberMortisPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive1"));
            _liberMortisPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive2"));
            _liberMortisPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive3"));
            _liberMortisPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive4"));
                
            _bookofWsoranKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranKeystone"));
            _bookofWsoranPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive1"));
            _bookofWsoranPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive2"));
            _bookofWsoranPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive3"));
            _bookofWsoranPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive4"));
            
            _grimoireNecrisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisKeystone"));
            _grimoireNecrisPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive1"));
            _grimoireNecrisPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive2"));
            _grimoireNecrisPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive3"));
            _grimoireNecrisPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive4"));
            
            _booksOfNagashKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashKeystone"));
            _booksOfNagashPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive1"));
            _booksOfNagashPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive2"));
            _booksOfNagashPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive3"));
            _booksOfNagashPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive4"));
            
            
        }

        protected override void InitializeKeyStones()
        {
            _necromancerRoot.Initialize(CareerID, "Summon a champion that the necromancer take control of. The Champion loses every 2 seconds 5 health points. For every 3 points in spell casting skill the champion gains 1 health point. Charging: applying spell damage or healing.", null, true,
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
            
            _liberNecrisKeystone.Initialize(CareerID, "Your Harbinger gains a two handed weapon. Ability scales with Roguery", "LiberNecris", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "summon_champion",
                        PropertyName = "TroopIdToSummon",
                        PropertyValue = (choice, originalValue, agent) => originalValue+"_two_handed",
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "GreaterHarbinger",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery}, 0.33f),
                        MutationType = OperationType.Add
                    }
                },new CareerChoiceObject.PassiveEffect()); 
            
            _deArcanisKadonKeystone.Initialize(CareerID, "Pressing Ability key allows to switch between characters; Harbinger acts indenpendant.", "DeArcanisKadon", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                },new CareerChoiceObject.PassiveEffect());  // switch controls
                
            _codexMortificaKeystone.Initialize(CareerID, "During Champion control gain 90% damage resistance for caster. Ability scales with Medicine.", "CodexMortifica", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "GreaterHarbinger",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Medicine}, 0.33f),
                        MutationType = OperationType.Add
                    }
                },new CareerChoiceObject.PassiveEffect()); // add 90% resistence for necromancer while controlled
                
            
            _liberMortisKeystone.Initialize(CareerID, "Your champion gains 25% extra melee damage. Ability starts charged", "LiberMortis", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                },new CareerChoiceObject.PassiveEffect(25,PassiveEffectType.Special,true)); // add 25% extra damage to champion
            
            _bookofWsoranKeystone.Initialize(CareerID, "Your Champion gains 25% Wardsave and can't be staggered.", "BookOfWsoran", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                },new CareerChoiceObject.PassiveEffect(25,PassiveEffectType.Special,true)); // add 25% Wardsave to champion
            
            _grimoireNecrisKeystone.Initialize(CareerID, "Harbinger kills gain 2HP for caster and the Champion. Harbinger gains a plate armor", "GrimoireNecris", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "summon_champion",
                        PropertyName = "TroopIdToSummon",
                        PropertyValue = (choice, originalValue, agent) => originalValue+"_plate",
                        MutationType = OperationType.Replace
                    },
                },new CareerChoiceObject.PassiveEffect()); // For every kill as Harbinger you and the Harbinger regain 2 HP
            
            _booksOfNagashKeystone.Initialize(CareerID, "Regenerate per kill of your champion 1 winds of magic. Adds 20% magical damage to champion", "BooksOfNagash", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                },new CareerChoiceObject.PassiveEffect(25,PassiveEffectType.Special,true));// For every kill as Harbinger the necromancer gains 1 Winds of Magic
        }

        protected override void InitializePassives()
        {
            _liberNecrisPassive1.Initialize(CareerID, "Increases Party size by 10.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _liberNecrisPassive2.Initialize(CareerID, "Increases maximum winds of magic capacities by 5.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _liberNecrisPassive3.Initialize(CareerID, "Increases your health by 25.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _liberNecrisPassive4.Initialize(CareerID, "Increases magic spell damage by 10%.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));

            
            _deArcanisKadonPassive1.Initialize(CareerID, "10% extra melee damage for undead troops.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));
            _deArcanisKadonPassive2.Initialize(CareerID, "Increases Party size by 10.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _deArcanisKadonPassive3.Initialize(CareerID, "15% cost reduction for spells.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.WindsCostReduction, true));
            _deArcanisKadonPassive4.Initialize(CareerID, "Increases maximum winds of magic capacities by 10.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));

            _codexMortificaPassive1.Initialize(CareerID, "Increases Party size by 10.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _codexMortificaPassive2.Initialize(CareerID, "Tier 4 Undead troops can get wounded with a 20% lower chance.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true));
            _codexMortificaPassive3.Initialize(CareerID, "Wounded troops in your party heal faster.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration));
            _codexMortificaPassive4.Initialize(CareerID, "Increases maximum winds of magic capacities by 10.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));

            _liberMortisPassive1.Initialize(CareerID, "Increases Party size by 25.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.PartySize));
            _liberMortisPassive2.Initialize(CareerID, "Attacks of undead troops can penetrate 15% armor.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _liberMortisPassive3.Initialize(CareerID, "Undead troops gain 15% extra physical melee damage.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
            _liberMortisPassive4.Initialize(CareerID, "PLACEHOLDER.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));

            _bookofWsoranPassive1.Initialize(CareerID, "Increases Party size by 50.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.PartySize));
            _bookofWsoranPassive2.Initialize(CareerID, "Increase hex durations by 50%.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.DebuffDuration,true));
            _bookofWsoranPassive3.Initialize(CareerID, "Undead troops gain 25% Ward save.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
            _bookofWsoranPassive4.Initialize(CareerID, "PLACEHOLDER.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            
            _grimoireNecrisPassive1.Initialize(CareerID, "undead troops in your formation gain 15% armor penetration.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special,true));
            _grimoireNecrisPassive2.Initialize(CareerID, "PLACEHOLDER.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _grimoireNecrisPassive3.Initialize(CareerID, "Buffs and healing duration is increased by 50%.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.BuffDuration,true));
            _grimoireNecrisPassive4.Initialize(CareerID, "For every magical item equipped you summon 1 unit more .", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

            _booksOfNagashPassive1.Initialize(CareerID, "Increases Party size by 100.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.PartySize));
            _booksOfNagashPassive2.Initialize(CareerID, "For every fallen higher tier undead, based on the troop tier more skeletons are summoned.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _booksOfNagashPassive3.Initialize(CareerID, "Increases maximum winds of magic capacities by 20.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.WindsOfMagic));
            _booksOfNagashPassive4.Initialize(CareerID, "PLACEHOLDER.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
        }
    }
}