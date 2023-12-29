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
        private CareerObject _grailDamsel;
        private CareerObject _minorVampire;
        private CareerObject _warriorPriest;
        private CareerObject _bloodKnight;
        private CareerObject _mercenary;
        private CareerObject _witchHunter;
        private CareerObject _necromancer;
        
        public static CareerObject Necromancer=> Instance._necromancer;
        public static CareerObject WitchHunter=> Instance._witchHunter;
        public static CareerObject GrailDamsel=> Instance._grailDamsel;
        public static CareerObject GrailKnight => Instance._grailKnight;
        public static CareerObject MinorVampire => Instance._minorVampire;
        public static CareerObject WarriorPriest => Instance._warriorPriest;
        public static CareerObject Mercenary => Instance._mercenary;
        public static CareerObject BloodKnight => Instance._bloodKnight;

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
            _grailDamsel = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GrailDamsel"));
            _grailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GrailKnight"));
            _minorVampire = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("MinorVampire"));
            _warriorPriest = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("WarriorPriest"));
            _bloodKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("BloodKnight"));
            _mercenary = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Mercenary"));
            _witchHunter = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("WitchHunter"));
            _necromancer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Necromancer"));
            
            _allCareers.Add(_grailKnight);
            _allCareers.Add(_warriorPriest);
            _allCareers.Add(_minorVampire);
            _allCareers.Add(_bloodKnight);
            _allCareers.Add(_mercenary);
            _allCareers.Add(_grailDamsel);
            _allCareers.Add(_necromancer);
            
            _allCareers.Add(_witchHunter);
        }

        private void InitializeAll()
        {
            _grailDamsel.Initialize("Damsel of the Lady", null,"FeyPaths", ChargeType.DamageDone, 2500, typeof(TeleportScript),true);   
            _grailKnight.Initialize("Grail Knight", null, "KnightlyCharge",ChargeType.CooldownOnly,100);   
            _bloodKnight.Initialize("Blood Knight", null, "RedFury", ChargeType.NumberOfKills,10, typeof(RedFuryScript));
            _minorVampire.Initialize("Vampire Count", null, "ShadowStep", ChargeType.DamageDone, 800, typeof(ShadowStepScript));
            _warriorPriest.Initialize("Warrior Priest", (hero) => 
            {
                return hero.Culture == MBObjectManager.Instance.GetObject<CultureObject>("empire") && hero.Clan.Tier >= 1;
            }, "RighteousFury", ChargeType.DamageTaken, 30);
            
            _mercenary.Initialize("Mercenary", null, "LetThemHaveIt", ChargeType.DamageDone, 300);
            
            _witchHunter.Initialize("Witch Hunter", null, "Accusation", ChargeType.DamageDone, 200, typeof(AccusationScript));

            _necromancer.Initialize("Necromancer", null, "GreaterHarbinger", ChargeType.DamageDone, 2000, typeof(SummonChampionScript), true);
        }
    }
}
