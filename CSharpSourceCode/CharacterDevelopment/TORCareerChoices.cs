using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoices
    {
        public static TORCareerChoices Instance { get; private set; }
        private List<CareerChoiceObject> _allCareerChoices = new List<CareerChoiceObject>();
        private MBReadOnlyList<CareerChoiceObject> _readonlyCareerChoices;
        public static MBReadOnlyList<CareerChoiceObject> All => Instance._readonlyCareerChoices;


        private CareerChoiceObject _strongerKnightlyStrike;
        public static CareerChoiceObject StrongerKnightlyStrike => Instance._strongerKnightlyStrike;

        public TORCareerChoices()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            _readonlyCareerChoices = new MBReadOnlyList<CareerChoiceObject>(_allCareerChoices);
        }

        private void RegisterAll()
        {
            _strongerKnightlyStrike = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("StrongerKnightlyStrike"));
            _allCareerChoices.Add(_strongerKnightlyStrike);
        }

        private void InitializeAll()
        {
            _strongerKnightlyStrike.Initialize("StrongerKnightlyStrike", 
                "Stronger Knightly Strike", 
                TORCareers.GrailKnight, 
                ChoiceType.Keystone, 
                new CareerChoiceObject.MutationObject()
                {
                    FieldName = "DamageAmount",
                    FieldValue = 0.2f,
                    MutationType = MutationType.Multiply
                });
        }
    }
}
