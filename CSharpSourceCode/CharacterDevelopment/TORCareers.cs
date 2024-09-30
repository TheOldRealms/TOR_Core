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
using TOR_Core.Utilities;

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
        private CareerObject _blackGrailKnight;
        private CareerObject _necrarch;
        private CareerObject _warriorPriestUlric;
        private CareerObject _imperialMagister;
        private CareerObject _waywatcher;
        private CareerObject _spellsinger;
        private CareerObject _greyLord;

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
                CareerButtons.Instance.GetCareerButton(career);
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
        
        public static CareerObject BlackGrailKnight => Instance._blackGrailKnight;
        public static CareerObject Necrarch => Instance._necrarch;

        public static MBReadOnlyList<CareerObject> All => Instance._allCareers;
        public static CareerObject WarriorPriestUlric => Instance._warriorPriestUlric;

        public static CareerObject ImperialMagister => Instance._imperialMagister;
        
        public static CareerObject Waywatcher => Instance._waywatcher;
        
        public static CareerObject Spellsinger => Instance._spellsinger;

        public static CareerObject GreyLord => Instance._greyLord;

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
            _blackGrailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("BlackGrailKnight"));
            _necrarch = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Necrarch"));
            _warriorPriestUlric = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("WarriorPriestUlric"));
            _imperialMagister = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("ImperialMagister"));
            _waywatcher = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Waywatcher"));
            _spellsinger = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Spellsinger"));
            _greyLord = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GreyLord"));
            
            _allCareers =
            [
                _grailKnight,
                _warriorPriest,
                _minorVampire,
                _bloodKnight,
                _mercenary,
                _grailDamsel,
                _necromancer,
                _witchHunter,
                _blackGrailKnight,
                _necrarch,
                _warriorPriestUlric,
                _imperialMagister,
                _waywatcher,
                _spellsinger,
                _greyLord
            ];
        }

        private void InitializeAll()
        {
            _grailDamsel.Initialize("Damsel of the Lady", null, "FeyPaths", CareerAbilityChargeSupplier.GrailDamselCareerCharge, 2500, typeof(TeleportScript));
            _grailKnight.Initialize("Grail Knight", null, "KnightlyCharge", null,100, typeof(KnightlyChargeScript));
            _bloodKnight.Initialize("Blood Knight", null, "RedFury", CareerAbilityChargeSupplier.BloodKnightCareerCharge, 10, typeof(RedFuryScript));
            _minorVampire.Initialize("Vampire Count", null, "ShadowStep", CareerAbilityChargeSupplier.MinorVampireCareerCharge, 800, typeof(ShadowStepScript));
            _warriorPriest.Initialize("Warrior Priest of Sigmar", hero => { return hero.Culture == MBObjectManager.Instance.GetObject<CultureObject>(TORConstants.Cultures.EMPIRE) && hero.Clan.Tier >= 1; }, "RighteousFury", CareerAbilityChargeSupplier.WarriorPriestCareerCharge, 300 );
            _mercenary.Initialize("Mercenary", null, "LetThemHaveIt" );
            _witchHunter.Initialize("Witch Hunter", null, "Accusation", CareerAbilityChargeSupplier.WitchHunterCareerCharge, 200, typeof(AccusationScript));
            _necromancer.Initialize("Necromancer", null, "GreaterHarbinger", CareerAbilityChargeSupplier.NecromancerCareerCharge, 2000, typeof(SummonChampionScript));
            _blackGrailKnight.Initialize("Knight of the Black Grail", null, "KnightlyCharge",null,100, typeof(KnightlyChargeScript));
            _necrarch.Initialize("Necrarch", null, "BlastOfAgony", CareerAbilityChargeSupplier.NecrarchCareerCharge, 1500, typeof(BlastOfAgonyScript));
            _warriorPriestUlric.Initialize("Warrior Priest of Ulric", null, "AxeOfUlric", CareerAbilityChargeSupplier.WarriorPriestUlricCharge, 400, typeof(AxeOfUlricScript));
            _imperialMagister.Initialize("Imperial Magister", null, "ArcaneConduit", null, 120, typeof(ArcaneConduit));
            _waywatcher.Initialize("Waywatcher", null, "ArrowOfKurnous",CareerAbilityChargeSupplier.WaywatcherCareerCharge, 1200, typeof(ArrowOfKurnousScript));
            _spellsinger.Initialize("Spellsinger", null, "WrathOfTheWood",CareerAbilityChargeSupplier.SpellsingerCareerCharge, 1000, typeof(WrathOfTheWoodScript));
            _greyLord.Initialize("Grey Lord Wizard", null, "MindControl",CareerAbilityChargeSupplier.GreyLordCareerCharge, 1000, typeof(MindControlScript));
        }
    }
}