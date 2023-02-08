using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoiceGroups
    {
        public static TORCareerChoiceGroups Instance { get; private set; }

        private CareerChoiceGroupObject _bookOfSigmar;
        private CareerChoiceGroupObject _sigmarProclaimer;
        private CareerChoiceGroupObject _relentlessFanatic;
        private CareerChoiceGroupObject _protectorOfTheWeak;
        private CareerChoiceGroupObject _holyPurge;
        private CareerChoiceGroupObject _archLector;

        public TORCareerChoiceGroups()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }

        private void RegisterAll()
        {
            _bookOfSigmar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BookOfSigmar"));
            _sigmarProclaimer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("SigmarsProclaimer"));
            _relentlessFanatic = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("RelentlessFanatic"));
            _protectorOfTheWeak = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ProtectorOfTheWeak"));
            _holyPurge = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("HolyPurge"));
            _archLector = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ArchLector"));
        }

        private void InitializeAll()
        {
            _bookOfSigmar.Initialize("Book of Sigmar", TORCareers.WarriorPriest, 1);
            _sigmarProclaimer.Initialize("Sigmar's Proclaimer", TORCareers.WarriorPriest, 1);
            _relentlessFanatic.Initialize("Relentless Fanatic", TORCareers.WarriorPriest, 2);
            _protectorOfTheWeak.Initialize("Protector of the Weak", TORCareers.WarriorPriest, 2);
            _holyPurge.Initialize("Holy Purge", TORCareers.WarriorPriest, 2);
            _archLector.Initialize("Arch Lector", TORCareers.WarriorPriest, 3);
        }
    }
}
