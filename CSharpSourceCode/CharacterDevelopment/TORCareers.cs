using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareers
    {
        private MBReadOnlyList<CareerObject> _allCareers;
        private CareerObject _bloodKnight;
        private CareerObject _grailDamsel;
        private CareerObject _grailKnight;
        private CareerObject _mercenary;
        private CareerObject _minorVampire;
        private CareerObject _necromancer;
        private CareerObject _warriorPriest;
        private CareerObject _witchHunter;
        public TORCareers()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            AssignCareerButtons();
        }

        private void AssignCareerButtons()
        {
            foreach (var career in All)
            {
                career.CareerButtonBehavior = CareerButtons.Instance.GetCareerButtons(career);
            }
        }

        public static TORCareers Instance { get; private set; }

        public static CareerObject Necromancer => Instance._necromancer;
        public static CareerObject WitchHunter => Instance._witchHunter;
        public static CareerObject GrailDamsel => Instance._grailDamsel;
        public static CareerObject GrailKnight => Instance._grailKnight;
        public static CareerObject MinorVampire => Instance._minorVampire;
        public static CareerObject WarriorPriest => Instance._warriorPriest;
        public static CareerObject Mercenary => Instance._mercenary;
        public static CareerObject BloodKnight => Instance._bloodKnight;

        public static MBReadOnlyList<CareerObject> All => Instance._allCareers;

        private void RegisterAll()
        {
            _allCareers = new MBReadOnlyList<CareerObject>();
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
            _grailDamsel.Initialize("Damsel of the Lady", null, "FeyPaths", ChargeType.DamageDone, CareerAbilityChargeSupplier.GrailDamselCareerCharge, 2500, typeof(TeleportScript), true);
            _grailKnight.Initialize("Grail Knight", null, "KnightlyCharge");
            _bloodKnight.Initialize("Blood Knight", null, "RedFury", ChargeType.NumberOfKills, CareerAbilityChargeSupplier.BloodKnightCareerCharge, 10, typeof(RedFuryScript));
            _minorVampire.Initialize("Vampire Count", null, "ShadowStep", ChargeType.DamageDone, CareerAbilityChargeSupplier.MinorVampireCareerCharge, 800, typeof(ShadowStepScript));
            _warriorPriest.Initialize("Warrior Priest", hero => { return hero.Culture == MBObjectManager.Instance.GetObject<CultureObject>("empire") && hero.Clan.Tier >= 1; }, "RighteousFury", ChargeType.DamageTaken, CareerAbilityChargeSupplier.WarriorPriestCareerCharge, 30);
            _mercenary.Initialize("Mercenary", null, "LetThemHaveIt", ChargeType.DamageDone, null, 300);
            _witchHunter.Initialize("Witch Hunter", null, "Accusation", ChargeType.DamageDone, CareerAbilityChargeSupplier.WitchHunterCareerCharge, 200, typeof(AccusationScript));
            _necromancer.Initialize("Necromancer", null, "GreaterHarbinger", ChargeType.DamageDone, CareerAbilityChargeSupplier.NecromancerCareerCharge, 2000, typeof(SummonChampionScript), true);
        }
    }
}