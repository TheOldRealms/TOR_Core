using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareers
    {
        public static TORCareers Instance { get; private set; }
        private CareerObject _grailKnight;
        public static CareerObject GrailKnight => Instance._grailKnight;

        public TORCareers()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }

        private void RegisterAll()
        {
            _grailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GrailKnight"));
        }

        private void InitializeAll()
        {
            _grailKnight.Initialize("GrailKnight", "Grail Knight career", hero => hero.Clan.Tier > 2, "WardOfArrows");
        }
    }
}
