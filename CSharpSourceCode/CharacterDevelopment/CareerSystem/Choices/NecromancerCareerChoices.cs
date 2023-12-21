using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CampaignMechanics.Choices;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class NecromancerCareerChoices : TORCareerChoicesBase
    {
        public NecromancerCareerChoices(CareerObject id) : base(id) {}
        private CareerChoiceObject _necromancerRoot;
        
        private CareerChoiceObject _carrionBookOfShyshKeystone;
        private CareerChoiceObject _tomeOfThousandSouls;
        private CareerChoiceObject _deArcanisKadonKeystone;
        
        private CareerChoiceObject _codexMortifica;
        private CareerChoiceObject _grimoireNecrisKeystone;
        private CareerChoiceObject _liberMortisKeystone;
        
        private CareerChoiceObject _bookOfArkhan;
        
        

        
        
        protected override void RegisterAll()
        {
            _necromancerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NecromancerRoot"));
            
            _carrionBookOfShyshKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CarrionBookOfShyshKeystone"));
            _deArcanisKadonKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKeystone"));
            _codexMortifica = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaKeystone"));
            
            _liberMortisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisKeystone"));
            _tomeOfThousandSouls = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("TomeOfThousandSoulsKeystone"));
            _grimoireNecrisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisKeystone"));
            
            
            _bookOfArkhan = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookOfArkhan"));
            
        }

        protected override void InitializeKeyStones()
        {
            _necromancerRoot.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
            
            _carrionBookOfShyshKeystone.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "CarrionBookOfShysh", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
            
            _deArcanisKadonKeystone.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "DeArcanisKadon", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
                
            _codexMortifica.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "CodexMortifica", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
                
            
            _liberMortisKeystone.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "LiberMortisKeystone", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
            
            _tomeOfThousandSouls.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "TomeOfThousandSouls", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
            
            _grimoireNecrisKeystone.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "TomeOfThousandSouls", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
            
            _bookOfArkhan.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", "TomeOfThousandSouls", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string>{"Unstoppable", "Unbreakable"},
                        MutationType = OperationType.Replace
                    },
                    
                });
        }

        protected override void InitializePassives()
        {
            
        }
    }
}