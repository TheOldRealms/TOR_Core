using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
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
        private CareerChoiceObject _longerShadowStep;
        public static CareerChoiceObject StrongerKnightlyStrike => Instance._strongerKnightlyStrike;
        public static CareerChoiceObject LongerShadowStep => Instance._longerShadowStep;

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
            _longerShadowStep = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LongerShadowStep"));
            _allCareerChoices.Add(_strongerKnightlyStrike);
            _allCareerChoices.Add(_longerShadowStep);
        }

        private void InitializeAll()
        {
            _strongerKnightlyStrike.Initialize("Stronger Knightly Strike", 
                "Increases the damage done by Knightly Strike by 20%.", 
                TORCareers.GrailKnight, 
                ChoiceType.Keystone, 
                new CareerChoiceObject.MutationObject()
                {
                    FieldName = "DamageAmount",
                    FieldValue = 0.2f,
                    MutationType = MutationType.Multiply
                });

            _longerShadowStep.Initialize("Longer Shadow Step",
                "Increases the duration of Shadow Step by 5 seconds.",
                TORCareers.MinorVampire,
                ChoiceType.Keystone,
                new CareerChoiceObject.MutationObject()
                {
                    MutationTarget = typeof(CareerAbility),
                    FieldName = "Duration",
                    FieldValue = 5,
                    MutationType = MutationType.Add
                });
        }
    }
}
