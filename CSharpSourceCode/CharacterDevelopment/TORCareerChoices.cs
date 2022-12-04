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


        private CareerChoiceObject _knightlyStrike;
        public static CareerChoiceObject KnightlyStrike => Instance._knightlyStrike;

        public TORCareerChoices()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            _readonlyCareerChoices = new MBReadOnlyList<CareerChoiceObject>(_allCareerChoices);
        }

        private void RegisterAll()
        {
            _knightlyStrike = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightlyStrike"));
            _allCareerChoices.Add(_knightlyStrike);
        }

        private void InitializeAll()
        {
            _knightlyStrike.Initialize("KnightlyStrike", "Knightly Strike", TORCareers.GrailKnight, ChoiceType.Keystone);
        }
    }
}
