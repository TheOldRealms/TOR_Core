using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareers
    {
        public static TORCareers Instance { get; private set; }
        private CareerObject _grailKnight;
        private CareerObject _minorVampire;
        private CareerObject _warriorPriest;
        public static CareerObject GrailKnight => Instance._grailKnight;
        public static CareerObject MinorVampire => Instance._minorVampire;
        public static CareerObject WarriorPriest => Instance._warriorPriest;

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
            _minorVampire = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("MinorVampire"));
            _warriorPriest = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("WarriorPriest"));
            _allCareers.Add(_grailKnight);
            _allCareers.Add(_minorVampire);
            _allCareers.Add(_warriorPriest);
        }

        private void InitializeAll()
        {
            _grailKnight.Initialize("Grail Knight", "Grail Knight career is for those...", hero => hero.Clan.Tier > 2, "ShadowStep", ChargeType.NumberOfKills);
            _minorVampire.Initialize("Minor Vampire", "Minor Vampire is ...", hero => hero.Clan.Tier > 2, "ShadowStep", ChargeType.DamageDone, 100, typeof(ShadowStepScript));
            _warriorPriest.Initialize("Warrior Priest", "Warrior Priest is ...", (hero) => 
            {
                return hero.Culture == MBObjectManager.Instance.GetObject<CultureObject>("empire") && hero.Clan.Tier >= 1;
            }, "RighteousFury", ChargeType.DamageTaken, 50);
        }
    }
}
