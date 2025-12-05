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
            _bookOfSigmar.Initialize("{=book_of_sigmar_choice_group_str}Book of Sigmar", TORCareers.WarriorPriest, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _sigmarProclaimer.Initialize("{=sigmar_proclaimer_choice_group_str}Sigmar's Proclaimer", TORCareers.WarriorPriest, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _relentlessFanatic.Initialize("{=relentless_fanatic_choice_group_str}Relentless Fanatic", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _protectorOfTheWeak.Initialize("{=protector_of_the_weak_choice_group_str}Protector of the Weak", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _holyPurge.Initialize("{=holy_purge_choice_group_str}Holy Purge", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _archLector.Initialize("{=arch_lector_choice_group_str}Arch Lector", TORCareers.WarriorPriest, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _twinTailedComet.Initialize("{=arch_lector_choice_group_str}Twin Tailed Comet", TORCareers.WarriorPriest, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Witch Hunter
            _toolsOfJudgement.Initialize("{=tools_of_judgement_choice_group_str}Tools of Judgement", TORCareers.WitchHunter, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _huntTheWicked.Initialize("{=hunt_the_wicked_choice_group_str}Hunt the Wicked", TORCareers.WitchHunter, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _silverHammer.Initialize("{=silver_hammer_choice_group_str}The Silver Hammer", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _noRestAgainstEvil.Initialize("{=no_rest_against_evil_choice_group_str}No Rest Against Evil", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _endsJustifiesMeans.Initialize("{=ends_justifies_means_choice_group_str}Ends Justifies Means", TORCareers.WitchHunter, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _swiftProcedure.Initialize("{=swift_procedure_choice_group_str}Swift Procedure", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _guiltyByAssociation.Initialize("{=guilty_by_association_choice_group_str}Guilty by Association", TORCareers.WitchHunter, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Necromancer
            _liberNecris.Initialize("{=liber_necris_choice_group_str}Liber Necris", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _deArcanisKadon.Initialize("{=de_arcanis_kadon_choice_group_str}De Arcanis Kadon", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _codexMortifica.Initialize("{=codex_mortifica_choice_group_str}Codex Mortifica", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _liberMortis.Initialize("{=liber_mortis_choice_group_str}Liber Mortis", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _bookOfWsoran.Initialize("{=book_of_wsoran_choice_group_str}Book of W'soran", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _grimoireNecris.Initialize("{=grimoire_necris_choice_group_str}Grimore Necris", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _booksOfNagash.Initialize("{=book_of_nagash_choice_group_str}Books of Nagash", TORCareers.Necromancer, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });


            //Vampire Count

            _newBlood.Initialize("{=new_blood_choice_group_str}New Blood", TORCareers.MinorVampire, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _feral.Initialize("{=new_blood_choice_group_str}The Feral", TORCareers.MinorVampire, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _arkayne.Initialize("{=arkayne_choice_group_str}The Arkayne", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Dark Magic";
                return hero.Clan.Tier >= 2;
            });
            _courtley.Initialize("{=courtley_choice_group_str}The Courtley", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _lordly.Initialize("{=lordly_choice_group_str}The Lordly", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _martialle.Initialize("{=martialle_choice_group_str}The Martialle", TORCareers.MinorVampire, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _masterOfDead.Initialize("{=master_of_dead_choice_group_str}Master of the Dead", TORCareers.MinorVampire, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Blood Knight

            _peerlessWarrior.Initialize("{=peerless_warrior_choice_group_str}Peerless Warrior", TORCareers.BloodKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _nightRider.Initialize("{=night_rider_choice_group_str}Night Rider", TORCareers.BloodKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _bladeMaster.Initialize("{=blade_master_choice_group_str}Blade Master", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _doomRider.Initialize("{=doom_rider_choice_group_str}Doom Rider", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _controlledHunger.Initialize("{=controlled_hunger_choice_group_str}Controlled Hunger", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _avatarOfDeath.Initialize("{=avatar_of_death_choice_group_str}Avatar of Death", TORCareers.BloodKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _dreadKnight.Initialize("{=dread_knight_choice_group_str}Dread Knight", TORCareers.BloodKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Mercenary

            _survivalist.Initialize("{=survivalist_choice_group_str}The Survivalist", TORCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _duelist.Initialize("{=duelist_choice_group_str}The Duelist", TORCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _headhunter.Initialize("{=headhunter_choice_group_str}The Headhunter", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _veteran.Initialize("{=veteran_choice_group_str}The Knightly", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _paymaster.Initialize("{=paymaster_choice_group_str}The Paymaster", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _mercenaryLord.Initialize("{=mercenary_lord_choice_group_str}The Mercenary Lord", TORCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _commander.Initialize("{=commander_choice_group_str}The Commander", TORCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Grail Knight

            _errantryWar.Initialize("{=errantry_war_choice_group_str}Errantry War", TORCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _enhancedHorseCombat.Initialize("{=enhanced_horse_combat_choice_group_str}Enhanced Horse Combat", TORCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _questingVow.Initialize("{=questing_vow_choice_group_str}Questing Vow", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _monsterSlayer.Initialize("{=monster_slayer_choice_group_str}Monster Slayer", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _masterHorseman.Initialize("{=master_horseman_choice_group_str}Master Horseman", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _grailVow.Initialize("{=grail_vow_choice_group_str}Grail Vow", TORCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _holyCrusader.Initialize("{=holy_crusader_choice_group_str}Holy Crusader", TORCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Black Grail Knight

            _curseOfMousillon.Initialize("{=curse_of_mousillon_group_str}Curse of Mousillon", TORCareers.BlackGrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _swampRider.Initialize("{=swamp_rider_choice_group_str}Swamp Rider", TORCareers.BlackGrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _unbreakableArmy.Initialize("{uunbreakable_army_choice_group_str}Unbreakable Army", TORCareers.BlackGrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _scourgeOfBretonnia.Initialize("{=scourge_of_Mousillon_choice_group_str}Scourge of Bretonnia", TORCareers.BlackGrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _robberKnight.Initialize("{=robber_baron_choice_group_str}Robber Knight", TORCareers.BlackGrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _lieOfLady.Initialize("{=lie_of_lady_choice_group_str}The Lady’s Lie", TORCareers.BlackGrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _blackGrailVow.Initialize("{=_black_grail_vow_choice_group_str}The Vow of the Black Grail", TORCareers.BlackGrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Grail Damsel

            _inspirationOfTheLady.Initialize("{=inspiration_of_the_lady_choice_group_str}Inspiration of the Lady", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _talesOfGiles.Initialize("{=tales_of_giles_choice_group_str}Tales of Gilles", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _feyEnchantment.Initialize("{=fey_enchantment_choice_group_str}Fey Enchantment", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _vividVisions.Initialize("{=vivid_visions_choice_group_str}Vivid Visions", TORCareers.GrailDamsel, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _justCause.Initialize("{=just_cause_choice_group_str}A Just Cause", TORCareers.GrailDamsel, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks 2nd Lore";
                return hero.Clan.Tier >= 2;
            });

            _secretsOfTheGrail.Initialize("{=secrets_of_the_grail_choice_group_str}Secrets of the Grail", TORCareers.GrailDamsel, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Lore of Heavens";
                return hero.Clan.Tier >= 4;
            });

            _envoyOfTheLady.Initialize("{=envoy_of_the_lady_choice_group_str}Envoy of the Lady", TORCareers.GrailDamsel, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Necrarch

            _discipleOfAccursed.Initialize("{=disciple_of_the_accursed_choice_group_str}Disciple of the Accursed", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _witchSight.Initialize("{=witch_sight_choice_group_str}Witch Sight", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _darkVision.Initialize("{=dark_vision_choice_group_str}Dark Visions", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _unhallowedSoul.Initialize("{=unhallowed_soul_choice_group_str}Unhallowed Soul", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _hungerForKnowledge.Initialize("{=hunger_for_knowledge_choice_group_str}Hunger for Knowledge", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _wellspringOfDhar.Initialize("{=wellspring_of_dhar_choice_group_str}Wellspring of Dhar", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _everlingsSecret.Initialize("{=everlings_secret_choice_group_str}The Everlings Secret", TORCareers.Necrarch, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Warrior priest of Ulric

            _crusherOfTheWeak.Initialize("{=crusher_of_the_weak_choice_group_str}Crusher of the Weak", TORCareers.WarriorPriestUlric, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wildPack.Initialize("{=wild_pack_choice_group_str}Wild Pack", TORCareers.WarriorPriestUlric, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _teachingsOfTheWinterfather.Initialize("{=teachings_of_the_winterfather_group_str}Teachings of the Winterfather", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _frostsBite.Initialize("{=frosts_bite_choice_group_str}Frost’s Bite", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _runesOfTheWhiteWolf.Initialize("{=runes_of_the_white_wolf_choice_group_str}Runes of the White Wolf", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _furyOfWar.Initialize("{=fury_of_war_choice_group_str}Fury of War", TORCareers.WarriorPriestUlric, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _flameOfUlric.Initialize("{=flame_of_ulric_choice_group_str}Flame of Ulric", TORCareers.WarriorPriestUlric, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Imperial College Magister
            _studyAndPractise.Initialize("{=study_and_practise_choice_group_str}Study and Practise", TORCareers.ImperialMagister, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _teclisTeachings.Initialize("{=teclis__teachings_choice_group_str}Teclis' Teachings", TORCareers.ImperialMagister, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _imperialEnchantment.Initialize("{=imperial_enchantment_choice_group_str}Imperial Enchantment", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Greater Powerstones";
                return hero.Clan.Tier >= 2;
            });
            _collegeOrders.Initialize("{=college_orders_choice_group_str}College Orders", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _magicCombatTraining.Initialize("{=magic_combat_training_choice_group_str}Magic Combat Training", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _ancientScrolls.Initialize("{=ancient_scrolls_choice_group_str}Ancient Scrolls", TORCareers.ImperialMagister, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Mighty Powerstones";
                return hero.Clan.Tier >= 2;
            });
            _arcaneKnowledge.Initialize("{=arcane_knowledge_choice_group_str}Arcane Knowledge", TORCareers.ImperialMagister, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });


            //Waywatcher
            _protectorOfTheWoods.Initialize("{=protector_of_the_woods_choice_group_str}Protector of the Woods", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _pathfinder.Initialize("{=pathfinder_choice_group_str}Pathfinder", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _forestStalker.Initialize("{=forest_stalker_choice_group_str}Forest Stalker", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Swiftshiver shards upgrade for troops";
                return true;
            });
            _hailOfArrows.Initialize("{=hail_of_arrows_choice_group_str}Hail of Arrows", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Hagbane Tipps upgrade for troops";
                return hero.Clan.Tier >= 2;
            });

            _hawkeyed.Initialize("{=hawkeyed_choice_group_str}Hawkeyed", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _starfireEssence.Initialize("{=starfire_essence_choice_group_str}Starfire Essence", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _eyeOfTheHunter.Initialize("{=eye_of_the_hunter_choice_group_str}Eye of the Hunter", TORCareers.Waywatcher, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Starfire shafts";
                return hero.Clan.Tier >= 4;
            });


            //Spellsinger
            _pathShaping.Initialize("{=path_shaping_choice_group_str}Path Shaping", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _treeSinging.Initialize("{=tree_singing_choice_group_str}Tree singing", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _vitalSurge.Initialize("{=vital_surge_choice_group_str}Vital Surge", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _heartOfTheTree.Initialize("{=heart_of_the_tree_choice_group_str}Heart of the Tree", TORCareers.Spellsinger, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _arielsBlessing.Initialize("{=ariel_s_blessing_choice_group_str}Ariel's Blessing", TORCareers.Spellsinger, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _magicOfAthelLoren.Initialize("{=fey_magic_choice_group_str}Fey Magic", TORCareers.Spellsinger, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            _furyOfTheForest.Initialize("{=fury_of_the_forest_choice_group_str}Fury of the Forest", TORCareers.Spellsinger, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Grey lord 
            _caelithsWisdom.Initialize("{=caelith_s_wisdom_choice_group_str}Caelith's Wisdom", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _soulBinding.Initialize("{=soul_binding_choice_group_str}Soul Binding", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _legendsOfMalok.Initialize("{=legends_of_malok_choice_group_str}Legends of Malok", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _unrestrictedMagic.Initialize("{=unrestricted_magic_choice_group_str}Unrestricted Magic", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _forbiddenScrollsOfSaphery.Initialize("{=forbidden_scrolls_of_saphery_choice_group_str}Forbidden Scrolls of Saphery", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _byAllMeans.Initialize("{=by_all_means_choice_group_str}By All Means", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _secretOfFellfang.Initialize("{=secret_of_the_fellfang_choice_group_str}Secret of the Fellfang", TORCareers.GreyLord, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Knight of the Old World
            _secularOrders.Initialize("{=secular_orders_choice_group_str}Secular Orders", TORCareers.KnightOldWorld, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _pathOfConquest.Initialize("{=path_of_conquest_choice_group_str}Path of Conquest", TORCareers.KnightOldWorld, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _squires.Initialize("{=squires_choice_group_str}Squires", TORCareers.KnightOldWorld, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _templarOrders.Initialize("{=templar_orders_choice_group_str}Templar Orders", TORCareers.KnightOldWorld, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _pathOfVigilance.Initialize("{=path_of_vigilance_choice_group_str}Path of Vigilance", TORCareers.KnightOldWorld, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _wrathAgainstChaos.Initialize("{=wrath_against_chaos_choice_group_str}Wrath against Chaos", TORCareers.KnightOldWorld, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _pathOfGlory.Initialize("{=path_of_glory_choice_group_str}Path of Glory", TORCareers.KnightOldWorld, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Ironbreaker
            _nestCleansing.Initialize("{=nest_cleansing_choice_group_str}Nest Cleansing", TORCareers.Ironbreaker, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _tunnelWatch.Initialize("{=tunnel_watch_choice_group_str}Tunnel Watch", TORCareers.Ironbreaker, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _ironPrice.Initialize("{=iron_price_choice_group_str}Iron Price", TORCareers.Ironbreaker, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.Clan.Tier >= 2;
            });
            _shieldWall.Initialize("{=shield_wall_choice_group_str}Shield Wall", TORCareers.Ironbreaker, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _ironDrakes.Initialize("{=iron_drakes_choice_group_str}Iron Drakes", TORCareers.Ironbreaker, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _gromrilArmor.Initialize("{=gromril_armor_choice_group_str}Gromril Armor", TORCareers.Ironbreaker, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _runeWeapons.Initialize("{=rune_weapons_choice_group_str}Rune Weapons", TORCareers.Ironbreaker, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            //Slayer

            _axeOfGrimnir.Initialize("{=axe_of_grimnir_choice_group_str}Axe of Grimnir", TORCareers.Slayer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _shameOfTheAncestors.Initialize("{=shame_of_the_ancestors_choice_group_str}Shame of the Ancestors", TORCareers.Slayer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _deadlyDetermination.Initialize("{=deadly_determination_choice_group_str}Deadly Determination", TORCareers.Slayer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _urkSlayer.Initialize("{=urk_slayer_choice_group_str}Urk Slayer", TORCareers.Slayer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _giantSlayer.Initialize("{=giant_slayer_choice_group_str}Giant Slayer", TORCareers.Slayer, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _baneOfChaos.Initialize("{=bane_of_chaos_choice_group_str}Bane of Chaos", TORCareers.Slayer, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _lastJourney.Initialize("{=last_journey_choice_group_str}The Last Journey", TORCareers.Slayer, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });


            //warden of Athel Loren

            _wardenOfCavaroc.Initialize("{=warden_of_cavaroc_choice_group_str}Warden of Cavaroc", TORCareers.Warden, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wardenOfCythral.Initialize("{=warden_of_cythral_choice_group_str}Warden of  Cythral and Anmyr", TORCareers.Warden, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wardenOfWydrioth.Initialize("{=warden_of_wydrioth_choice_group_str}Warden of Wydrioth", TORCareers.Warden, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wardenOfAtylwyth.Initialize("{=warden_of_atylwyth_choice_group_str}Warden of Atylwyth", TORCareers.Warden, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _wardenOfTorgovann.Initialize("{=warden_of_torgovann_choice_group_str}Warden of Torgovann", TORCareers.Warden, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _wardenOfTalsyn.Initialize("{=warden_of_talsyn_choice_group_str}Warden of Talsyn", TORCareers.Warden, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _wardenOfArgwylon.Initialize("{=warden_of_argwylon_choice_group_str}Warden of Argwylon", TORCareers.Warden, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

            _forgefireBurning.Initialize("{=forge_fire_burning_choice_group_str}Forgefire Burning", TORCareers.Runelord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            }, (Hero hero, out string text) =>
            {
                text = "Unlocks Runecraft for Equipment";
                return true;
            });
            _teachingsOfThungni.Initialize("{=teachings_of_thungni_choice_group_str}Teachings of Thungni", TORCareers.Runelord, 2, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerRunesmith");
                text = "";
                if (!hasUnlocked)
                {
                    text = "\n " + GameTexts.FindText("careerunlock_condition_1", "Runelord").ToString();
                }

                return hasUnlocked;
            }, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _chiselAndHammer.Initialize("{=chisel_and_hammer_choice_group_str}Chisel and Hammer", TORCareers.Runelord, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith");
            });
            _forHearthAndHome.Initialize("{=for_hearth_and_home_choice_group_str}For Hearth and Home", TORCareers.Runelord, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith");
            });
            _stoneAndSteel.Initialize("{=stone_and_steel_choice_group_str}Stone and Steel", TORCareers.Runelord, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith") && hero.HasAttribute("PlayerRunelord");
            });

            _legacyOfGrungni.Initialize("{=legacy_of_grugni_choice_group_str}Legacy of Grungni", TORCareers.Runelord, 3, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerRunesmith") && hero.HasAttribute("PlayerRunelord");
                text = "";
                if (!hasUnlocked)
                {
                    text = GameTexts.FindText("careerunlock_condition_2", "Runelord").ToString();
                }

                return hasUnlocked;
            });
            _anvilOfDoom.Initialize("{=_anvil_of_doom_choice_group_str}Anvil of Doom", TORCareers.Runelord, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerRunesmith") && hero.HasAttribute("PlayerRunelord");
            });



            _tufferDanNails.Initialize("{=tuffer_dan_nails_choice_group_str}Tuffer Dan Nails", TORCareers.OrcBoss, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _youAnWotArmour.Initialize("{=you_an_wot_armour_choice_group_str}You An Wot Armour", TORCareers.OrcBoss, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _goodwivBlockas.Initialize("{=goodwiv_blockas_choice_group_str}Goodwiv Blockas", TORCareers.OrcBoss, 2, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcBoss");
                text = "";
                if (!hasUnlocked)
                {
                    text = "\n " + GameTexts.FindText("careerunlock_condition_1", "OrcBoss").ToString();
                }
                return hasUnlocked;
            });

            _meanestanDaBaddest.Initialize("{=meanestan_da_baddest_choice_group_str}Meanestan Da Baddest", TORCareers.OrcBoss, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcBoss");
            });

            _getToDaChoppas.Initialize("{=get_to_da_choppas_choice_group_str}Get To Da Choppas", TORCareers.OrcBoss, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcBoss");
            });

            _leafNuffinBehin.Initialize("{=leaf_nuffin_behin_choice_group_str}Leaf Nuffin Behin", TORCareers.OrcBoss, 3, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcBoss") && hero.HasAttribute("PlayerOrcBigBoss");
                text = "";
                if (!hasUnlocked)
                {
                    text = GameTexts.FindText("careerunlock_condition_2", "OrcBoss").ToString();
                }
                return hasUnlocked;
            });

            _bestofDaBest.Initialize("{=bestof_da_best_choice_group_str}Best of Da Best", TORCareers.OrcBoss, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcBoss") && hero.HasAttribute("PlayerOrcBigBoss");
            });

            // Orc Shaman
            _bonesAnFirepitz.Initialize("{=bones_an_firepitz_choice_group_str}Bones an' Firepitz", TORCareers.OrcShaman, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _visionsUvDaOrcayne.Initialize("{=visions_uv_da_orcayne_choice_group_str}Visions uv da Orc-ayne", TORCareers.OrcShaman, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });

            _giftzFromDaGreatGreen.Initialize("{=giftz_from_da_great_green_choice_group_str}Giftz from Da Great Green", TORCareers.OrcShaman, 1, (Hero hero, out string text) =>
            {

            });

            _brutalCunnin.Initialize("{=brutal_cunnin_choice_group_str}Brutal Cunnin'", TORCareers.OrcShaman, 2, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcShaman");
                text = "";
                if (!hasUnlocked)
                {
                    text = "\n " + GameTexts.FindText("careerunlock_condition_1", "OrcShaman").ToString();
                }
                return hasUnlocked;
            });

            _cunninBrutality.Initialize("{=cunnin_brutality_choice_group_str}Cunnin' Brutality", TORCareers.OrcShaman, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcShaman");
            });

            _gorkAnMorkAreWatchin.Initialize("{=gork_an_mork_are_watchin_choice_group_str}Gork an' Mork are watchin'", TORCareers.OrcShaman, 3, (Hero hero, out string text) =>
            {
                var hasUnlocked = hero.HasAttribute("PlayerOrcShaman") && hero.HasAttribute("PlayerOrcFavouredUvDaGodz");
                text = "";
                if (!hasUnlocked)
                {
                    text = GameTexts.FindText("careerunlock_condition_2", "OrcShaman").ToString();
                }
                return hasUnlocked;
            });

            _powerUvDaWaaagh.Initialize("{=power_uv_da_waaagh_choice_group_str}Power uv da Waaagh!", TORCareers.OrcShaman, 3, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return hero.HasAttribute("PlayerOrcShaman") && hero.HasAttribute("PlayerOrcFavouredUvDaGodz");
            });

        }
    }
}