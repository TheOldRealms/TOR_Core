using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoices
    {
        public static TORCareerChoices Instance { get; private set; }
        private List<CareerChoiceObject> _allCareerChoices = new List<CareerChoiceObject>();
        private MBReadOnlyList<CareerChoiceObject> _readonlyCareerChoices;
        public static MBReadOnlyList<CareerChoiceObject> All => Instance._readonlyCareerChoices;


        private CareerChoiceObject _warriorPriestRoot;
        public static CareerChoiceObject WarriorPriestRoot => Instance._warriorPriestRoot;

        public TORCareerChoices()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            _readonlyCareerChoices = new MBReadOnlyList<CareerChoiceObject>(_allCareerChoices);
        }

        private void RegisterAll()
        {
            _warriorPriestRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarriorPriestRoot"));
            _allCareerChoices.Add(_warriorPriestRoot);
        }

        private void InitializeAll()
        {
            _warriorPriestRoot.Initialize("Warrior Priest Career Tree Root", 
                "The root of the career choices tree.", 
                TORCareers.WarriorPriest, true,
                ChoiceType.Keystone, 
                new CareerChoiceObject.MutationObject()
                {
                    MutationTarget = typeof(TriggeredEffectTemplate),
                    FieldName = "Radius",
                    FieldValue = 0.2,
                    MutationType = MutationType.Multiply
                });
        }
    }
}
