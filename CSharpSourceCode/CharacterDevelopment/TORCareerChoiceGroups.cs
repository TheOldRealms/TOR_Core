using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoiceGroups
    {
        public static TORCareerChoiceGroups Instance { get; private set; }

        //Warrior Priest
        private CareerChoiceGroupObject _bookOfSigmar;
        private CareerChoiceGroupObject _sigmarProclaimer;
        private CareerChoiceGroupObject _relentlessFanatic;
        private CareerChoiceGroupObject _protectorOfTheWeak;
        private CareerChoiceGroupObject _holyPurge;
        private CareerChoiceGroupObject _archLector;
        private CareerChoiceGroupObject _twinTailedComet;
        //Witch Hunter
        private CareerChoiceGroupObject _toolsOfJudgement;
        private CareerChoiceGroupObject _huntTheWicked;
        private CareerChoiceGroupObject _silverHammer;
        private CareerChoiceGroupObject _endsJustifiesMeans;
        private CareerChoiceGroupObject _swiftProcedure;
        private CareerChoiceGroupObject _guiltyByAssociation;
        private CareerChoiceGroupObject _noRestAgainstEvil;
        //Necromancer
        private CareerChoiceGroupObject _liberNecris;
        private CareerChoiceGroupObject _bookOfWsoran;
        private CareerChoiceGroupObject _deArcanisKadon;
        private CareerChoiceGroupObject _grimoireNecris;
        private CareerChoiceGroupObject _liberMortis;
        private CareerChoiceGroupObject _codexMortifica;
        private CareerChoiceGroupObject _booksOfNagash;

        //Minor Vampire
        private CareerChoiceGroupObject _newBlood;
        private CareerChoiceGroupObject _arkayne;
        private CareerChoiceGroupObject _courtley;
        private CareerChoiceGroupObject _lordly;
        private CareerChoiceGroupObject _martialle;
        private CareerChoiceGroupObject _masterOfDead;
        private CareerChoiceGroupObject _feral;

        //Blood Knight
        private CareerChoiceGroupObject _peerlessWarrior;
        private CareerChoiceGroupObject _nightRider;
        private CareerChoiceGroupObject _bladeMaster;
        private CareerChoiceGroupObject _doomRider;
        private CareerChoiceGroupObject _controlledHunger;
        private CareerChoiceGroupObject _avatarOfDeath;
        private CareerChoiceGroupObject _dreadKnight;

        //Mercenary
        private CareerChoiceGroupObject _survivalist;
        private CareerChoiceGroupObject _duelist;
        private CareerChoiceGroupObject _headhunter;
        private CareerChoiceGroupObject _veteran;
        private CareerChoiceGroupObject _paymaster;
        private CareerChoiceGroupObject _mercenaryLord;
        private CareerChoiceGroupObject _commander;

        //Grail Knight
        private CareerChoiceGroupObject _errantryWar;
        private CareerChoiceGroupObject _enhancedHorseCombat;
        private CareerChoiceGroupObject _questingVow;
        private CareerChoiceGroupObject _monsterSlayer;
        private CareerChoiceGroupObject _masterHorseman;
        private CareerChoiceGroupObject _grailVow;
        private CareerChoiceGroupObject _holyCrusader;

        //Grail Damsel
        private CareerChoiceGroupObject _feyEnchantment;
        private CareerChoiceGroupObject _inspirationOfTheLady;
        private CareerChoiceGroupObject _talesOfGiles;
        private CareerChoiceGroupObject _vividVisions;
        private CareerChoiceGroupObject _justCause;
        private CareerChoiceGroupObject _secretsOfTheGrail;
        private CareerChoiceGroupObject _envoyOfTheLady;

        //Black Grail Knight
        private CareerChoiceGroupObject _curseOfMousillon;
        private CareerChoiceGroupObject _swampRider;
        private CareerChoiceGroupObject _unbreakableArmy;
        private CareerChoiceGroupObject _scourgeOfBretonnia;
        private CareerChoiceGroupObject _robberKnight;
        private CareerChoiceGroupObject _lieOfLady;
        private CareerChoiceGroupObject _blackGrailVow;

        //Necrarch
        private CareerChoiceGroupObject _discipleOfAccursed;
        private CareerChoiceGroupObject _witchSight;
        private CareerChoiceGroupObject _darkVision;
        private CareerChoiceGroupObject _unhallowedSoul;
        private CareerChoiceGroupObject _hungerForKnowledge;
        private CareerChoiceGroupObject _wellspringOfDhar;
        private CareerChoiceGroupObject _everlingsSecret;

        //Warrior Priest of Ulric
        private CareerChoiceGroupObject _crusherOfTheWeak;
        private CareerChoiceGroupObject _wildPack;
        private CareerChoiceGroupObject _teachingsOfTheWinterfather;
        private CareerChoiceGroupObject _frostsBite;
        private CareerChoiceGroupObject _runesOfTheWhiteWolf;
        private CareerChoiceGroupObject _furyOfWar;
        private CareerChoiceGroupObject _flameOfUlric;

        //Imperial Magister
        private CareerChoiceGroupObject _studyAndPractise;
        private CareerChoiceGroupObject _teclisTeachings;
        private CareerChoiceGroupObject _imperialEnchantment;
        private CareerChoiceGroupObject _collegeOrders;
        private CareerChoiceGroupObject _magicCombatTraining;
        private CareerChoiceGroupObject _ancientScrolls;
        private CareerChoiceGroupObject _arcaneKnowledge;

        //Waywatcher
        private CareerChoiceGroupObject _protectorOfTheWoods;
        private CareerChoiceGroupObject _pathfinder;
        private CareerChoiceGroupObject _forestStalker;
        private CareerChoiceGroupObject _hailOfArrows;
        private CareerChoiceGroupObject _hawkeyed;
        private CareerChoiceGroupObject _starfireEssence;
        private CareerChoiceGroupObject _eyeOfTheHunter;

        //Spellsinger
        private CareerChoiceGroupObject _pathShaping;
        private CareerChoiceGroupObject _treeSinging;
        private CareerChoiceGroupObject _vitalSurge;
        private CareerChoiceGroupObject _heartOfTheTree;
        private CareerChoiceGroupObject _arielsBlessing;
        private CareerChoiceGroupObject _magicOfAthelLoren;
        private CareerChoiceGroupObject _furyOfTheForest;

        //Grey lord wizard

        private CareerChoiceGroupObject _caelithsWisdom;
        private CareerChoiceGroupObject _soulBinding;
        private CareerChoiceGroupObject _legendsOfMalok;
        private CareerChoiceGroupObject _unrestrictedMagic;
        private CareerChoiceGroupObject _forbiddenScrollsOfSaphery;
        private CareerChoiceGroupObject _byAllMeans;
        private CareerChoiceGroupObject _secretOfFellfang;

        // Knight of the Old World

        private CareerChoiceGroupObject _secularOrders;
        private CareerChoiceGroupObject _pathOfConquest;
        private CareerChoiceGroupObject _squires;
        private CareerChoiceGroupObject _templarOrders;
        private CareerChoiceGroupObject _pathOfVigilance;
        private CareerChoiceGroupObject _wrathAgainstChaos;
        private CareerChoiceGroupObject _pathOfGlory;


        //Ironbreaker

        private CareerChoiceGroupObject _nestCleansing;
        private CareerChoiceGroupObject _tunnelWatch;
        private CareerChoiceGroupObject _ironPrice;
        private CareerChoiceGroupObject _shieldWall;
        private CareerChoiceGroupObject _ironDrakes;
        private CareerChoiceGroupObject _gromrilArmor;
        private CareerChoiceGroupObject _runeWeapons;

        //Slayer

        private CareerChoiceGroupObject _axeOfGrimnir;
        private CareerChoiceGroupObject _shameOfTheAncestors;
        private CareerChoiceGroupObject _deadlyDetermination;
        private CareerChoiceGroupObject _urkSlayer;
        private CareerChoiceGroupObject _giantSlayer;
        private CareerChoiceGroupObject _baneOfChaos;
        private CareerChoiceGroupObject _lastJourney;


        //Warden

        private CareerChoiceGroupObject _wardenOfCavaroc;
        private CareerChoiceGroupObject _wardenOfCythral;
        private CareerChoiceGroupObject _wardenOfTorgovann;
        private CareerChoiceGroupObject _wardenOfAtylwyth;
        private CareerChoiceGroupObject _wardenOfWydrioth;
        private CareerChoiceGroupObject _wardenOfTalsyn;
        private CareerChoiceGroupObject _wardenOfArgwylon;

        // Runelord
        private CareerChoiceGroupObject _forgefireBurning;
        private CareerChoiceGroupObject _teachingsOfThungni;
        private CareerChoiceGroupObject _chiselAndHammer;
        private CareerChoiceGroupObject _forHearthAndHome;
        private CareerChoiceGroupObject _stoneAndSteel;
        private CareerChoiceGroupObject _legacyOfGrungni;
        private CareerChoiceGroupObject _anvilOfDoom;

        //Orc Orc Boss
        private CareerChoiceGroupObject _tufferDanNails;
        private CareerChoiceGroupObject _youAnWotArmour;
        private CareerChoiceGroupObject _goodwivBlockas;
        private CareerChoiceGroupObject _meanestanDaBaddest;
        private CareerChoiceGroupObject _getToDaChoppas;
        private CareerChoiceGroupObject _leafNuffinBehin;
        private CareerChoiceGroupObject _bestofDaBest;

        //Orc Shaman
        private CareerChoiceGroupObject _bonesAnFirepitz;
        private CareerChoiceGroupObject _visionsUvDaOrcayne;
        private CareerChoiceGroupObject _giftzFromDaGreatGreen;
        private CareerChoiceGroupObject _brutalCunnin;
        private CareerChoiceGroupObject _cunninBrutality;
        private CareerChoiceGroupObject _gorkAnMorkAreWatchin;
        private CareerChoiceGroupObject _powerUvDaWaaagh;


        public TORCareerChoiceGroups()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }

        private void RegisterAll()
        {
            //WarriorPriest
            _bookOfSigmar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BookOfSigmar"));
            _sigmarProclaimer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("SigmarsProclaimer"));
            _relentlessFanatic = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("RelentlessFanatic"));
            _protectorOfTheWeak = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ProtectorOfTheWeak"));
            _holyPurge = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("HolyPurge"));
            _archLector = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ArchLector"));
            _twinTailedComet = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("TwinTailedComet"));

            //Witch Hunter
            _toolsOfJudgement = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ToolsOfJudgement"));
            _huntTheWicked = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("HuntTheWicked"));
            _silverHammer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("SilverHammer"));
            _endsJustifiesMeans = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EndsJustifiesMeans"));
            _swiftProcedure = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("SwiftProcedure"));
            _guiltyByAssociation = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("GuiltyByAssociation"));
            _noRestAgainstEvil = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("NoRestAgainstEvil"));

            //Necromancer

            _liberNecris = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("LiberNecris"));
            _deArcanisKadon = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("DeArcanisKadon"));
            _grimoireNecris = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("GrimoireNecris"));
            _bookOfWsoran = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BookOfWsoran"));
            _liberMortis = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("LiberMortis"));
            _booksOfNagash = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BooksOfNagash"));
            _codexMortifica = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("CodexMortifica"));

            //Vampire Count
            _newBlood = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("NewBlood"));
            _feral = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Feral"));
            _arkayne = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Arkayne"));
            _courtley = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Courtley"));
            _lordly = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Lordly"));
            _martialle = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Martialle"));
            _masterOfDead = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MasterOfDead"));

            //Blood Knight
            _peerlessWarrior = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("PeerlessWarrior"));
            _nightRider = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("NightRider"));
            _bladeMaster = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BladeMaster"));
            _doomRider = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("DoomRider"));
            _controlledHunger = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ControlledHunger"));
            _avatarOfDeath = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("AvatarOfDeath"));
            _dreadKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("DreadKnight"));

            //Mercenary
            _survivalist = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Survivalist"));
            _duelist = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Duelist"));
            _headhunter = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Headhunter"));
            _veteran = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Veteran"));
            _paymaster = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Paymaster"));
            _mercenaryLord = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord"));
            _commander = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Commander"));

            //Grail Knight
            _errantryWar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ErrantryWar"));
            _enhancedHorseCombat = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnhancedHorseCombat")); ;
            _questingVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("QuestingVow"));
            _monsterSlayer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MonsterSlayer"));
            _masterHorseman = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MasterHorseman"));
            _grailVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("GrailVow"));
            _holyCrusader = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("HolyCrusader"));

            //Grail Damsel
            _feyEnchantment = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("FeyEnchantment"));
            _inspirationOfTheLady = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("InspirationOfTheLady"));
            _talesOfGiles = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("TalesOfGiles"));
            _vividVisions = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("VividVisions"));
            _justCause = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("JustCause"));
            _secretsOfTheGrail = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("SecretsOfTheGrail"));
            _envoyOfTheLady = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnvoyOfTheLady"));

            //Black Grail Knight
            _curseOfMousillon = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_curseOfMousillon).UnderscoreFirstCharToUpper()));
            _swampRider = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_swampRider).UnderscoreFirstCharToUpper()));
            _unbreakableArmy = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_unbreakableArmy).UnderscoreFirstCharToUpper()));
            _scourgeOfBretonnia = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_scourgeOfBretonnia).UnderscoreFirstCharToUpper()));
            _robberKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_robberKnight).UnderscoreFirstCharToUpper()));
            _lieOfLady = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_lieOfLady).UnderscoreFirstCharToUpper()));
            _blackGrailVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_blackGrailVow).UnderscoreFirstCharToUpper()));

            //Necrarch
            _discipleOfAccursed = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_discipleOfAccursed).UnderscoreFirstCharToUpper()));
            _witchSight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_witchSight).UnderscoreFirstCharToUpper()));
            _darkVision = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_darkVision).UnderscoreFirstCharToUpper()));
            _unhallowedSoul = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_unhallowedSoul).UnderscoreFirstCharToUpper()));
            _hungerForKnowledge = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_hungerForKnowledge).UnderscoreFirstCharToUpper()));
            _wellspringOfDhar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wellspringOfDhar).UnderscoreFirstCharToUpper()));
            _everlingsSecret = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_everlingsSecret).UnderscoreFirstCharToUpper()));

            //WarriorPriest of Ulric
            _crusherOfTheWeak = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_crusherOfTheWeak).UnderscoreFirstCharToUpper()));
            _wildPack = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wildPack).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterfather = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_teachingsOfTheWinterfather).UnderscoreFirstCharToUpper()));
            _frostsBite = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_frostsBite).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolf = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_runesOfTheWhiteWolf).UnderscoreFirstCharToUpper()));
            _furyOfWar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_furyOfWar).UnderscoreFirstCharToUpper()));
            _flameOfUlric = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_flameOfUlric).UnderscoreFirstCharToUpper()));

            //Imperial Magister
            _studyAndPractise = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_studyAndPractise).UnderscoreFirstCharToUpper()));
            _teclisTeachings = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_teclisTeachings).UnderscoreFirstCharToUpper()));
            _imperialEnchantment = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_imperialEnchantment).UnderscoreFirstCharToUpper()));
            _collegeOrders = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_collegeOrders).UnderscoreFirstCharToUpper()));
            _magicCombatTraining = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_magicCombatTraining).UnderscoreFirstCharToUpper()));
            _ancientScrolls = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_ancientScrolls).UnderscoreFirstCharToUpper()));
            _arcaneKnowledge = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_arcaneKnowledge).UnderscoreFirstCharToUpper()));

            //Waywatcher
            _forestStalker = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_forestStalker).UnderscoreFirstCharToUpper()));
            _pathfinder = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_pathfinder).UnderscoreFirstCharToUpper()));
            _protectorOfTheWoods = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_protectorOfTheWoods).UnderscoreFirstCharToUpper()));
            _hailOfArrows = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_hailOfArrows).UnderscoreFirstCharToUpper()));
            _hawkeyed = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_hawkeyed).UnderscoreFirstCharToUpper()));
            _starfireEssence = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_starfireEssence).UnderscoreFirstCharToUpper()));
            _eyeOfTheHunter = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_eyeOfTheHunter).UnderscoreFirstCharToUpper()));

            //Spellsinger
            _pathShaping = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_pathShaping).UnderscoreFirstCharToUpper()));
            _treeSinging = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_treeSinging).UnderscoreFirstCharToUpper()));
            _vitalSurge = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_vitalSurge).UnderscoreFirstCharToUpper()));
            _heartOfTheTree = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_heartOfTheTree).UnderscoreFirstCharToUpper()));
            _arielsBlessing = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_arielsBlessing).UnderscoreFirstCharToUpper()));
            _magicOfAthelLoren = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_magicOfAthelLoren).UnderscoreFirstCharToUpper()));
            _furyOfTheForest = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_furyOfTheForest).UnderscoreFirstCharToUpper()));

            //Greylord

            _caelithsWisdom = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_caelithsWisdom).UnderscoreFirstCharToUpper()));
            _legendsOfMalok = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_legendsOfMalok).UnderscoreFirstCharToUpper()));
            _soulBinding = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_soulBinding).UnderscoreFirstCharToUpper()));
            _unrestrictedMagic = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_unrestrictedMagic).UnderscoreFirstCharToUpper()));
            _forbiddenScrollsOfSaphery = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_forbiddenScrollsOfSaphery).UnderscoreFirstCharToUpper()));
            _byAllMeans = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_byAllMeans).UnderscoreFirstCharToUpper()));
            _secretOfFellfang = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_secretOfFellfang).UnderscoreFirstCharToUpper()));

            //Knight of The Old World

            _secularOrders = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_secularOrders).UnderscoreFirstCharToUpper()));
            _pathOfConquest = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_pathOfConquest).UnderscoreFirstCharToUpper()));
            _squires = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_squires).UnderscoreFirstCharToUpper()));
            _templarOrders = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_templarOrders).UnderscoreFirstCharToUpper()));
            _pathOfVigilance = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_pathOfVigilance).UnderscoreFirstCharToUpper()));
            _wrathAgainstChaos = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wrathAgainstChaos).UnderscoreFirstCharToUpper()));
            _pathOfGlory = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_pathOfGlory).UnderscoreFirstCharToUpper()));

            //Ironbreaker
            _nestCleansing = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_nestCleansing).UnderscoreFirstCharToUpper()));
            _tunnelWatch = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_tunnelWatch).UnderscoreFirstCharToUpper()));
            _ironPrice = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_ironPrice).UnderscoreFirstCharToUpper()));
            _shieldWall = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_shieldWall).UnderscoreFirstCharToUpper()));
            _ironDrakes = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_ironDrakes).UnderscoreFirstCharToUpper()));
            _gromrilArmor = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_gromrilArmor).UnderscoreFirstCharToUpper()));
            _runeWeapons = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_runeWeapons).UnderscoreFirstCharToUpper()));

            //Slayer
            _axeOfGrimnir = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_axeOfGrimnir).UnderscoreFirstCharToUpper()));
            _shameOfTheAncestors = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_shameOfTheAncestors).UnderscoreFirstCharToUpper()));
            _deadlyDetermination = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_deadlyDetermination).UnderscoreFirstCharToUpper()));
            _urkSlayer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_urkSlayer).UnderscoreFirstCharToUpper()));
            _giantSlayer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_giantSlayer).UnderscoreFirstCharToUpper()));
            _baneOfChaos = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_baneOfChaos).UnderscoreFirstCharToUpper()));
            _lastJourney = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_lastJourney).UnderscoreFirstCharToUpper()));

            //Warden of Athel Loren
            _wardenOfCavaroc = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfCavaroc).UnderscoreFirstCharToUpper()));
            _wardenOfCythral = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfCythral).UnderscoreFirstCharToUpper()));
            _wardenOfTorgovann = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfTorgovann).UnderscoreFirstCharToUpper()));
            _wardenOfAtylwyth = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfAtylwyth).UnderscoreFirstCharToUpper()));
            _wardenOfWydrioth = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfWydrioth).UnderscoreFirstCharToUpper()));
            _wardenOfTalsyn = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfTalsyn).UnderscoreFirstCharToUpper()));
            _wardenOfArgwylon = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_wardenOfArgwylon).UnderscoreFirstCharToUpper()));

            //Runelord

            _forgefireBurning = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_forgefireBurning).UnderscoreFirstCharToUpper()));
            _teachingsOfThungni = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_teachingsOfThungni).UnderscoreFirstCharToUpper()));
            _chiselAndHammer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_chiselAndHammer).UnderscoreFirstCharToUpper()));
            _forHearthAndHome = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_forHearthAndHome).UnderscoreFirstCharToUpper()));
            _stoneAndSteel = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_stoneAndSteel).UnderscoreFirstCharToUpper()));
            _legacyOfGrungni = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_legacyOfGrungni).UnderscoreFirstCharToUpper()));
            _anvilOfDoom = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_anvilOfDoom).UnderscoreFirstCharToUpper()));

            //OrcBoss
            _tufferDanNails = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_tufferDanNails).UnderscoreFirstCharToUpper()));
            _youAnWotArmour = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_youAnWotArmour).UnderscoreFirstCharToUpper()));
            _goodwivBlockas = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_goodwivBlockas).UnderscoreFirstCharToUpper()));
            _meanestanDaBaddest = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_meanestanDaBaddest).UnderscoreFirstCharToUpper()));
            _getToDaChoppas = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_getToDaChoppas).UnderscoreFirstCharToUpper()));
            _leafNuffinBehin = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_leafNuffinBehin).UnderscoreFirstCharToUpper()));
            _bestofDaBest = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_bestofDaBest).UnderscoreFirstCharToUpper()));

            //OrcShaman
            _bonesAnFirepitz = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_bonesAnFirepitz).UnderscoreFirstCharToUpper()));
            _visionsUvDaOrcayne = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_visionsUvDaOrcayne).UnderscoreFirstCharToUpper()));
            _giftzFromDaGreatGreen = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_giftzFromDaGreatGreen).UnderscoreFirstCharToUpper()));
            _brutalCunnin = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_brutalCunnin).UnderscoreFirstCharToUpper()));
            _cunninBrutality = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_cunninBrutality).UnderscoreFirstCharToUpper()));
            _gorkAnMorkAreWatchin = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_gorkAnMorkAreWatchin).UnderscoreFirstCharToUpper()));
            _powerUvDaWaaagh = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_powerUvDaWaaagh).UnderscoreFirstCharToUpper()));

        }

        private void InitializeAll()
        {
            //Warrior Priest of Sigmar
            _bookOfSigmar.Initialize("Book of Sigmar", TORCareers.WarriorPriest, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _sigmarProclaimer.Initialize("Sigmar's Proclaimer", TORCareers.WarriorPriest, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _relentlessFanatic.Initialize("Relentless Fanatic", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _protectorOfTheWeak.Initialize("Protector of the Weak", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _holyPurge.Initialize("Holy Purge", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _archLector.Initialize("Arch Lector", TORCareers.WarriorPriest, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _twinTailedComet.Initialize("Twin Tailed Comet", TORCareers.WarriorPriest, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Witch Hunter
            _toolsOfJudgement.Initialize("{=str_tor_career_choicegroup_tools_of_judgement}Tools of Judgement", TORCareers.WitchHunter, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _huntTheWicked.Initialize("{=str_tor_career_choicegroup_hunt_the_wicked}Hunt the Wicked", TORCareers.WitchHunter, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _silverHammer.Initialize("{=str_tor_career_choicegroup_silver_hammer}The Silver Hammer", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _noRestAgainstEvil.Initialize("{=str_tor_career_choicegroup_no_rest_against_evil}No Rest Against Evil", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _endsJustifiesMeans.Initialize("{=str_tor_career_choicegroup_ends_justifies_means}Ends Justifies Means", TORCareers.WitchHunter, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _swiftProcedure.Initialize("{=str_tor_career_choicegroup_swift_procedure}Swift Procedure", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _guiltyByAssociation.Initialize("{=str_tor_career_choicegroup_guilty_by_association}Guilty by Association", TORCareers.WitchHunter, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Necromancer
            _liberNecris.Initialize("{=str_tor_career_choicegroup_liber_necris}Liber Necris", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _deArcanisKadon.Initialize("{=str_tor_career_choicegroup_de_arcanis_kadon}De Arcanis Kadon", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _codexMortifica.Initialize("{=str_tor_career_choicegroup_codex_mortifica}Codex Mortifica", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _liberMortis.Initialize("{=str_tor_career_choicegroup_liber_mortis}Liber Mortis", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _bookOfWsoran.Initialize("{=str_tor_career_choicegroup_book_of_wsoran}Book of W'soran", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _grimoireNecris.Initialize("{=str_tor_career_choicegroup_grimoire_necris}Grimore Necris", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _booksOfNagash.Initialize("{=str_tor_career_choicegroup_book_of_nagash}Books of Nagash", TORCareers.Necromancer, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });


            //Vampire Count

            _newBlood.Initialize("{=str_tor_career_choicegroup_new_blood}New Blood", TORCareers.MinorVampire, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _feral.Initialize("{=str_tor_career_choicegroup_feral}The Feral", TORCareers.MinorVampire, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _arkayne.Initialize("{=str_tor_career_choicegroup_arkayne}The Arkayne", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Dark Magic";
                return hero.Clan.Tier >= 2;
            });
            _courtley.Initialize("{=str_tor_career_choicegroup_courtley}The Courtley", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _lordly.Initialize("{=str_tor_career_choicegroup_lordly}The Lordly", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _martialle.Initialize("{=str_tor_career_choicegroup_martialle}The Martialle", TORCareers.MinorVampire, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _masterOfDead.Initialize("{=str_tor_career_choicegroup_master_of_dead}Master of the Dead", TORCareers.MinorVampire, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Blood Knight

            _peerlessWarrior.Initialize("{=str_tor_career_choicegroup_peerless_warrior}Peerless Warrior", TORCareers.BloodKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _nightRider.Initialize("{=str_tor_career_choicegroup_night_rider}Night Rider", TORCareers.BloodKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _bladeMaster.Initialize("{=str_tor_career_choicegroup_blade_master}Blade Master", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _doomRider.Initialize("{=str_tor_career_choicegroup_doom_rider}Doom Rider", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _controlledHunger.Initialize("{=str_tor_career_choicegroup_controlled_hunger}Controlled Hunger", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _avatarOfDeath.Initialize("{=str_tor_career_choicegroup_avatar_of_death}Avatar of Death", TORCareers.BloodKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _dreadKnight.Initialize("{=str_tor_career_choicegroup_dread_knight}Dread Knight", TORCareers.BloodKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Mercenary

            _survivalist.Initialize("{=str_tor_career_choicegroup_survivalist}The Survivalist", TORCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _duelist.Initialize("{=str_tor_career_choicegroup_duelist}The Duelist", TORCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _headhunter.Initialize("{=str_tor_career_choicegroup_headhunter}The Headhunter", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _veteran.Initialize("{=str_tor_career_choicegroup_veteran}The Knightly", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _paymaster.Initialize("{=str_tor_career_choicegroup_paymaster}The Paymaster", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _mercenaryLord.Initialize("{=str_tor_career_choicegroup_mercenary_lord}The Mercenary Lord", TORCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _commander.Initialize("{=str_tor_career_choicegroup_commander}The Commander", TORCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Grail Knight

            _errantryWar.Initialize("{=str_tor_career_choicegroup_errantry_war}Errantry War", TORCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _enhancedHorseCombat.Initialize("{=str_tor_career_choicegroup_enhanced_horse_combat}Enhanced Horse Combat", TORCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _questingVow.Initialize("{=str_tor_career_choicegroup_questing_vow}Questing Vow", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _monsterSlayer.Initialize("{=str_tor_career_choicegroup_monster_slayer}Monster Slayer", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _masterHorseman.Initialize("{=str_tor_career_choicegroup_master_horseman}Master Horseman", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _grailVow.Initialize("{=str_tor_career_choicegroup_grail_vow}Grail Vow", TORCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _holyCrusader.Initialize("{=str_tor_career_choicegroup_holy_crusader}Holy Crusader", TORCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Black Grail Knight

            _curseOfMousillon.Initialize("{=str_tor_career_choicegroup_curse_of_mousillon}Curse of Mousillon", TORCareers.BlackGrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _swampRider.Initialize("{=str_tor_career_choicegroup_swamp_rider}Swamp Rider", TORCareers.BlackGrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _unbreakableArmy.Initialize("{=str_tor_career_choicegroup_unbreakable_army}Unbreakable Army", TORCareers.BlackGrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _scourgeOfBretonnia.Initialize("{=str_tor_career_choicegroup_scourge_of_bretonnia}Scourge of Bretonnia", TORCareers.BlackGrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _robberKnight.Initialize("{=str_tor_career_choicegroup_robber_knight}Robber Knight", TORCareers.BlackGrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _lieOfLady.Initialize("{=str_tor_career_choicegroup_lie_of_lady}The Lady’s Lie", TORCareers.BlackGrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _blackGrailVow.Initialize("{=str_tor_career_choicegroup_black_grail_vow}The Vow of the Black Grail", TORCareers.BlackGrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Grail Damsel

            _inspirationOfTheLady.Initialize("{=str_tor_career_choicegroup_inspiration_of_the_lady}Inspiration of the Lady", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _talesOfGiles.Initialize("{=str_tor_career_choicegroup_tales_of_giles}Tales of Gilles", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _feyEnchantment.Initialize("{=str_tor_career_choicegroup_fey_enchantment}Fey Enchantment", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _vividVisions.Initialize("{=str_tor_career_choicegroup_vivid_visions}Vivid Visions", TORCareers.GrailDamsel, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _justCause.Initialize("{=str_tor_career_choicegroup_just_cause}A Just Cause", TORCareers.GrailDamsel, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks 2nd Lore";
                return hero.Clan.Tier >= 2;
            });

            _secretsOfTheGrail.Initialize("{=str_tor_career_choicegroup_secrets_of_the_grail}Secrets of the Grail", TORCareers.GrailDamsel, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Lore of Heavens";
                return hero.Clan.Tier >= 4;
            });

            _envoyOfTheLady.Initialize("{=str_tor_career_choicegroup_envoy_of_the_lady}Envoy of the Lady", TORCareers.GrailDamsel, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Necrarch

            _discipleOfAccursed.Initialize("{=str_tor_career_choicegroup_disciple_of_the_accursed}Disciple of the Accursed", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _witchSight.Initialize("{=str_tor_career_choicegroup_witch_sight}Witch Sight", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _darkVision.Initialize("{=str_tor_career_choicegroup_dark_vision}Dark Visions", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _unhallowedSoul.Initialize("{=str_tor_career_choicegroup_unhallowed_soul}Unhallowed Soul", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _hungerForKnowledge.Initialize("{=str_tor_career_choicegroup_hunger_for_knowledge}Hunger for Knowledge", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _wellspringOfDhar.Initialize("{=str_tor_career_choicegroup_wellspring_of_dhar}Wellspring of Dhar", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _everlingsSecret.Initialize("{=str_tor_career_choicegroup_everlings_secret}The Everlings Secret", TORCareers.Necrarch, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Warrior priest of Ulric

            _crusherOfTheWeak.Initialize("{=str_tor_career_choicegroup_crusher_of_the_weak}Crusher of the Weak", TORCareers.WarriorPriestUlric, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wildPack.Initialize("{=str_tor_career_choicegroup_wild_pack}Wild Pack", TORCareers.WarriorPriestUlric, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _teachingsOfTheWinterfather.Initialize("{=str_tor_career_choicegroup_teachings_of_the_winterfather}Teachings of the Winterfather", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _frostsBite.Initialize("{=str_tor_career_choicegroup_frosts_bite}Frost's Bite", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _runesOfTheWhiteWolf.Initialize("{=str_tor_career_choicegroup_runes_of_the_white_wolf}Runes of the White Wolf", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _furyOfWar.Initialize("{=str_tor_career_choicegroup_fury_of_war}Fury of War", TORCareers.WarriorPriestUlric, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _flameOfUlric.Initialize("{=str_tor_career_choicegroup_flame_of_ulric}Flame of Ulric", TORCareers.WarriorPriestUlric, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Imperial College Magister
            _studyAndPractise.Initialize("{=str_tor_career_choicegroup_study_and_practise}Study and Practise", TORCareers.ImperialMagister, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _teclisTeachings.Initialize("{=str_tor_career_choicegroup_teclis_teachings}Teclis' Teachings", TORCareers.ImperialMagister, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _imperialEnchantment.Initialize("{=str_tor_career_choicegroup_imperial_enchantment}Imperial Enchantment", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Greater Powerstones";
                return hero.Clan.Tier >= 2;
            });
            _collegeOrders.Initialize("{=str_tor_career_choicegroup_college_orders}College Orders", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _magicCombatTraining.Initialize("{=str_tor_career_choicegroup_magic_combat_training}Magic Combat Training", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _ancientScrolls.Initialize("{=str_tor_career_choicegroup_ancient_scrolls}Ancient Scrolls", TORCareers.ImperialMagister, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Mighty Powerstones";
                return hero.Clan.Tier >= 2;
            });
            _arcaneKnowledge.Initialize("{=str_tor_career_choicegroup_arcane_knowledge}Arcane Knowledge", TORCareers.ImperialMagister, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });


            //Waywatcher
            _protectorOfTheWoods.Initialize("{=str_tor_career_choicegroup_protector_of_the_woods}Protector of the Woods", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _pathfinder.Initialize("{=str_tor_career_choicegroup_pathfinder}Pathfinder", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _forestStalker.Initialize("{=str_tor_career_choicegroup_forest_stalker}Forest Stalker", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Swiftshiver shards upgrade for troops";
                return true;
            });
            _hailOfArrows.Initialize("{=str_tor_career_choicegroup_hail_of_arrows}Hail of Arrows", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Hagbane Tipps upgrade for troops";
                return hero.Clan.Tier >= 2;
            });

            _hawkeyed.Initialize("{=str_tor_career_choicegroup_hawkeyed}Hawkeyed", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _starfireEssence.Initialize("{=str_tor_career_choicegroup_starfire_essence}Starfire Essence", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _eyeOfTheHunter.Initialize("{=str_tor_career_choicegroup_eye_of_the_hunter}Eye of the Hunter", TORCareers.Waywatcher, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Starfire shafts";
                return hero.Clan.Tier >= 4;
            });


            //Spellsinger
            _pathShaping.Initialize("{=str_tor_career_choicegroup_path_shaping}Path Shaping", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _treeSinging.Initialize("{=str_tor_career_choicegroup_tree_singing}Tree singing", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _vitalSurge.Initialize("{=str_tor_career_choicegroup_vital_surge}Vital Surge", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _heartOfTheTree.Initialize("{=str_tor_career_choicegroup_heart_of_the_tree}Heart of the Tree", TORCareers.Spellsinger, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _arielsBlessing.Initialize("{=str_tor_career_choicegroup_ariels_blessing}Ariel's Blessing", TORCareers.Spellsinger, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _magicOfAthelLoren.Initialize("{=str_tor_career_choicegroup_magic_of_athel_loren}Fey Magic", TORCareers.Spellsinger, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            _furyOfTheForest.Initialize("{=str_tor_career_choicegroup_fury_of_the_forest}Fury of the Forest", TORCareers.Spellsinger, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Grey lord 
            _caelithsWisdom.Initialize("{=str_tor_career_choicegroup_caeliths_wisdom}Caelith's Wisdom", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _soulBinding.Initialize("{=str_tor_career_choicegroup_soul_binding}Soul Binding", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _legendsOfMalok.Initialize("{=str_tor_career_choicegroup_legends_of_malok}Legends of Malok", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _unrestrictedMagic.Initialize("{=str_tor_career_choicegroup_unrestricted_magic}Unrestricted Magic", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _forbiddenScrollsOfSaphery.Initialize("{=str_tor_career_choicegroup_forbidden_scrolls_of_saphery}Forbidden Scrolls of Saphery", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _byAllMeans.Initialize("{=str_tor_career_choicegroup_by_all_means}By All Means", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _secretOfFellfang.Initialize("{=str_tor_career_choicegroup_secret_of_fellfang}Secret of the Fellfang", TORCareers.GreyLord, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Knight of the Old World
            _secularOrders.Initialize("{=str_tor_career_choicegroup_secular_orders}Secular Orders", TORCareers.KnightOldWorld, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _pathOfConquest.Initialize("{=str_tor_career_choicegroup_path_of_conquest}Path of Conquest", TORCareers.KnightOldWorld, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _squires.Initialize("{=str_tor_career_choicegroup_squires}Squires", TORCareers.KnightOldWorld, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _templarOrders.Initialize("{=str_tor_career_choicegroup_templar_orders}Templar Orders", TORCareers.KnightOldWorld, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _pathOfVigilance.Initialize("{=str_tor_career_choicegroup_path_of_vigilance}Path of Vigilance", TORCareers.KnightOldWorld, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _wrathAgainstChaos.Initialize("{=str_tor_career_choicegroup_wrath_against_chaos}Wrath against Chaos", TORCareers.KnightOldWorld, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _pathOfGlory.Initialize("{=str_tor_career_choicegroup_path_of_glory}Path of Glory", TORCareers.KnightOldWorld, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Ironbreaker
            _nestCleansing.Initialize("{=str_tor_career_choicegroup_nest_cleansing}Nest Cleansing", TORCareers.Ironbreaker, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _tunnelWatch.Initialize("{=str_tor_career_choicegroup_tunnel_watch}Tunnel Watch", TORCareers.Ironbreaker, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _ironPrice.Initialize("{=str_tor_career_choicegroup_iron_price}Iron Price", TORCareers.Ironbreaker, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.Clan.Tier >= 2;
            });
            _shieldWall.Initialize("{=str_tor_career_choicegroup_shield_wall}Shield Wall", TORCareers.Ironbreaker, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _ironDrakes.Initialize("{=str_tor_career_choicegroup_iron_drakes}Iron Drakes", TORCareers.Ironbreaker, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _gromrilArmor.Initialize("{=str_tor_career_choicegroup_gromril_armor}Gromril Armor", TORCareers.Ironbreaker, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _runeWeapons.Initialize("{=str_tor_career_choicegroup_rune_weapons}Rune Weapons", TORCareers.Ironbreaker, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Slayer

            _axeOfGrimnir.Initialize("{=str_tor_career_choicegroup_axe_of_grimnir}Axe of Grimnir", TORCareers.Slayer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _shameOfTheAncestors.Initialize("{=str_tor_career_choicegroup_shame_of_the_ancestors}Shame of the Ancestors", TORCareers.Slayer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _deadlyDetermination.Initialize("{=str_tor_career_choicegroup_deadly_determination}Deadly Determination", TORCareers.Slayer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _urkSlayer.Initialize("{=str_tor_career_choicegroup_urk_slayer}Urk Slayer", TORCareers.Slayer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _giantSlayer.Initialize("{=str_tor_career_choicegroup_giant_slayer}Giant Slayer", TORCareers.Slayer, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _baneOfChaos.Initialize("{=str_tor_career_choicegroup_bane_of_chaos}Bane of Chaos", TORCareers.Slayer, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _lastJourney.Initialize("{=str_tor_career_choicegroup_last_journey}The Last Journey", TORCareers.Slayer, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });


            //warden of Athel Loren

            _wardenOfCavaroc.Initialize("{=str_tor_career_choicegroup_warden_of_cavaroc}Warden of Cavaroc", TORCareers.Warden, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wardenOfCythral.Initialize("{=str_tor_career_choicegroup_warden_of_cythral}Warden of  Cythral and Anmyr", TORCareers.Warden, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wardenOfWydrioth.Initialize("{=str_tor_career_choicegroup_warden_of_wydrioth}Warden of Wydrioth", TORCareers.Warden, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wardenOfAtylwyth.Initialize("{=str_tor_career_choicegroup_warden_of_atylwyth}Warden of Atylwyth", TORCareers.Warden, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _wardenOfTorgovann.Initialize("{=str_tor_career_choicegroup_warden_of_torgovann}Warden of Torgovann", TORCareers.Warden, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _wardenOfTalsyn.Initialize("{=str_tor_career_choicegroup_warden_of_talsyn}Warden of Talsyn", TORCareers.Warden, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _wardenOfArgwylon.Initialize("{=str_tor_career_choicegroup_warden_of_argwylon}Warden of Argwylon", TORCareers.Warden, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            _forgefireBurning.Initialize("{=str_tor_career_choicegroup_forgefire_burning}Forgefire Burning", TORCareers.Runelord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            }, (Hero hero, out string text) =>
            {
                text = "Unlocks Runecraft for Equipment";
                return true;
            });
            _teachingsOfThungni.Initialize("{=str_tor_career_choicegroup_teachings_of_thungni}Teachings of Thungni", TORCareers.Runelord, 2, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerRunesmith");
                text = "";
                if (!hasUnlocked)
                {
                    text = "\n " + GameTexts.FindText("tor_careerunlock_condition_1", "Runelord").ToString();
                }

                return hasUnlocked;
            }, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _chiselAndHammer.Initialize("{=str_tor_career_choicegroup_chisel_and_hammer}Chisel and Hammer", TORCareers.Runelord, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith");
            });
            _forHearthAndHome.Initialize("{=str_tor_career_choicegroup_for_hearth_and_home}For Hearth and Home", TORCareers.Runelord, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith");
            });
            _stoneAndSteel.Initialize("{=str_tor_career_choicegroup_stone_and_steel}Stone and Steel", TORCareers.Runelord, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith") && hero.HasAttribute("PlayerRunelord");
            });

            _legacyOfGrungni.Initialize("{=str_tor_career_choicegroup_legacy_of_grungni}Legacy of Grungni", TORCareers.Runelord, 3, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerRunesmith") && hero.HasAttribute("PlayerRunelord");
                text = "";
                if (!hasUnlocked)
                {
                    text = GameTexts.FindText("tor_careerunlock_condition_2", "Runelord").ToString();
                }

                return hasUnlocked;
            });
            _anvilOfDoom.Initialize("{=str_tor_career_choicegroup_anvil_of_doom}Anvil of Doom", TORCareers.Runelord, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith") && hero.HasAttribute("PlayerRunelord");
            });



            _tufferDanNails.Initialize("{=str_tor_career_choicegroup_tuffer_dan_nails}Tuffer Dan Nails", TORCareers.OrcBoss, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _youAnWotArmour.Initialize("{=str_tor_career_choicegroup_you_an_wot_armour}You an' Wot Armour?", TORCareers.OrcBoss, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _goodwivBlockas.Initialize("{=str_tor_career_choicegroup_goodwiv_blockas}Good wiv Blockas", TORCareers.OrcBoss, 2, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcBoss");
                text = "";
                if (!hasUnlocked)
                {
                    text = "\n " + GameTexts.FindText("tor_careerunlock_condition_1", "OrcBoss").ToString();
                }
                return hasUnlocked;
            });

            _meanestanDaBaddest.Initialize("{=str_tor_career_choicegroup_meanestan_da_baddest}Meanest an' da Baddest", TORCareers.OrcBoss, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcBoss");
            });

            _getToDaChoppas.Initialize("{=str_tor_career_choicegroup_get_to_da_choppas}Get To Da Choppas", TORCareers.OrcBoss, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcBoss");
            });

            _leafNuffinBehin.Initialize("{=str_tor_career_choicegroup_leaf_nuffin_behin}Leave nuffin' behind", TORCareers.OrcBoss, 3, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcBoss") && hero.HasAttribute("PlayerOrcBigBoss");
                text = "";
                if (!hasUnlocked)
                {
                    text = GameTexts.FindText("tor_careerunlock_condition_2", "OrcBoss").ToString();
                }
                return hasUnlocked;
            });

            _bestofDaBest.Initialize("{=str_tor_career_choicegroup_bestof_da_best}Best of da Best!", TORCareers.OrcBoss, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcBoss") && hero.HasAttribute("PlayerOrcBigBoss");
            });

            // Orc Shaman
            _bonesAnFirepitz.Initialize("{=str_tor_career_choicegroup_bones_an_firepitz}Bones an' Firepitz", TORCareers.OrcShaman, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _visionsUvDaOrcayne.Initialize("{=str_tor_career_choicegroup_visions_uv_da_orcayne}Visions uv da Orc-ayne", TORCareers.OrcShaman, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _giftzFromDaGreatGreen.Initialize("{=str_tor_career_choicegroup_giftz_from_da_great_green}Giftz from Da Great Green", TORCareers.OrcShaman, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _brutalCunnin.Initialize("{=str_tor_career_choicegroup_brutal_cunnin}Brutal Cunnin'", TORCareers.OrcShaman, 2, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcShaman");
                text = "";
                if (!hasUnlocked)
                {
                    text = "\n " + GameTexts.FindText("tor_careerunlock_condition_1", "OrcShaman").ToString();
                }
                return hasUnlocked;
            });

            _cunninBrutality.Initialize("{=str_tor_career_choicegroup_cunnin_brutality}Cunnin' Brutality", TORCareers.OrcShaman, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcShaman");
            });

            _gorkAnMorkAreWatchin.Initialize("{=str_tor_career_choicegroup_gork_an_mork_are_watchin}Gork an' Mork are watchin'", TORCareers.OrcShaman, 3, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcShaman") && hero.HasAttribute("PlayerOrcFavouredUvDaGodz");
                text = "";
                if (!hasUnlocked)
                {
                    text = GameTexts.FindText("tor_careerunlock_condition_2", "OrcShaman").ToString();
                }
                return hasUnlocked;
            });

            _powerUvDaWaaagh.Initialize("{=str_tor_career_choicegroup_power_uv_da_waaagh}Power uv da Waaagh!", TORCareers.OrcShaman, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcShaman") && hero.HasAttribute("PlayerOrcFavouredUvDaGodz");
            });

        }
    }
}