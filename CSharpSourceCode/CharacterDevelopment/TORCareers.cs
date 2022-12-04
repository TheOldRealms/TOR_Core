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
    public class TORCareers
    {
        public static TORCareers Instance { get; private set; }
        private CareerObject _grailKnight;
        public static CareerObject GrailKnight => Instance._grailKnight;

        private List<CareerObject> _allCareers = new List<CareerObject>();
        private MBReadOnlyList<CareerObject> _readonlyCareers;
        public static MBReadOnlyList<CareerObject> All => Instance._readonlyCareers;

        public TORCareers()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            _readonlyCareers = new MBReadOnlyList<CareerObject>(_allCareers);
        }

        private void RegisterAll()
        {
            _grailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GrailKnight"));
            _allCareers.Add(_grailKnight);
        }

        private void InitializeAll()
        {
            _grailKnight.Initialize("GrailKnight", "Grail Knight", hero => hero.Clan.Tier > 2, "KnightlyStrike", ChargeType.CooldownOnly);
        }
    }
}
