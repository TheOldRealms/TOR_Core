using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;
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
        private CareerChoiceGroupObject _knightly;
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
            _endsJustifiesMeans= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EndsJustifiesMeans"));
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
            _knightly = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Knightly"));
            _paymaster = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Paymaster"));
            _mercenaryLord = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord"));
            _commander = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Commander"));
            
            //Grail Knight
            _errantryWar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ErrantryWar"));
            _enhancedHorseCombat = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnhancedHorseCombat"));; 
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
            _forestStalker =  Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject(nameof(_forestStalker).UnderscoreFirstCharToUpper()));
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
            _toolsOfJudgement.Initialize("{=book_of_sigmar_choice_group_str}Tools of Judgement", TORCareers.WitchHunter, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _huntTheWicked.Initialize("{=sigmar_proclaimer_choice_group_str}Hunt the Wicked", TORCareers.WitchHunter, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _silverHammer.Initialize("{=relentless_fanatic_choice_group_str}The Silver Hammer", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _noRestAgainstEvil.Initialize("{=arch_lector_choice_group_str}No Rest Against Evil", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _endsJustifiesMeans.Initialize("{=protector_of_the_weak_choice_group_str}Ends Justifies Means", TORCareers.WitchHunter, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _swiftProcedure.Initialize("{=holy_purge_choice_group_str}Swift Procedure", TORCareers.WitchHunter, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _guiltyByAssociation.Initialize("{=arch_lector_choice_group_str}Guilty by Association", TORCareers.WitchHunter, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            
            //Necromancer
            _liberNecris.Initialize("{=arch_lector_choice_group_str}Liber Necris", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _deArcanisKadon.Initialize("{=arch_lector_choice_group_str}De Arcanis Kardon", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _codexMortifica.Initialize("{=arch_lector_choice_group_str}Codex Mortifica", TORCareers.Necromancer, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            
            _liberMortis.Initialize("{=arch_lector_choice_group_str}Liber Mortis", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });

            _bookOfWsoran.Initialize("{=arch_lector_choice_group_str}Book of W'soran", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _grimoireNecris.Initialize("{=arch_lector_choice_group_str}Grimore Necris", TORCareers.Necromancer, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _booksOfNagash.Initialize("{=arch_lector_choice_group_str}Books of Nagash", TORCareers.Necromancer, 3, (Hero hero, out string text) =>
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
            },(Hero hero, out string unlockText) =>
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
            _knightly.Initialize("{=knightly_choice_group_str}The Knightly", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _paymaster.Initialize("{=knightly_choice_group_str}The Paymaster", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
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
            _talesOfGiles.Initialize("{=tales_of_gilles_choice_group_str}Tales of Gilles", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
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
            },(Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks 2nd Lore";
                return hero.Clan.Tier >= 2;
            });
            
            _secretsOfTheGrail.Initialize("{=secrets_of_the_grail_choice_group_str}Secrets of the Grail", TORCareers.GrailDamsel, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            },(Hero hero, out string unlockText) =>
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
            
            _discipleOfAccursed.Initialize("Disciple of the Accursed", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _witchSight.Initialize("Witch Sight", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _darkVision.Initialize("{=relentless_fanatic_choice_group_str}Dark Visions", TORCareers.Necrarch, 1, (Hero hero, out string text) =>
            {
                text = string.Empty; 
                return true;
            });
            _unhallowedSoul.Initialize("{=protector_of_the_weak_choice_group_str}Unhallowed Soul", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _hungerForKnowledge.Initialize("{=holy_purge_choice_group_str}Hunger for Knowledge", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _wellspringOfDhar.Initialize("{=arch_lector_choice_group_str}Wellspring of Dhar", TORCareers.Necrarch, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _everlingsSecret.Initialize("{=arch_lector_choice_group_str}The Everlings Secret", TORCareers.Necrarch, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            
            //Warrior priest of Ulric
            
            _crusherOfTheWeak.Initialize("Crusher of the Weak", TORCareers.WarriorPriestUlric, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wildPack.Initialize("Wild Pack", TORCareers.WarriorPriestUlric, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _teachingsOfTheWinterfather.Initialize("Teachings of the Winterfather", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _frostsBite.Initialize("Frost’s Bite", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _runesOfTheWhiteWolf.Initialize("Runes of the White Wolf", TORCareers.WarriorPriestUlric, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _furyOfWar.Initialize("Fury of War", TORCareers.WarriorPriestUlric, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _flameOfUlric.Initialize("Flame of Ulric", TORCareers.WarriorPriestUlric, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            
            //Imperial College Magister
            _studyAndPractise.Initialize("Study and Practise", TORCareers.ImperialMagister, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _teclisTeachings.Initialize("Teclis' Teachings", TORCareers.ImperialMagister, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _imperialEnchantment.Initialize("Imperial Enchantment", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Greater Powerstones";
                return hero.Clan.Tier >= 2;
            });
            _collegeOrders.Initialize("College Orders", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _magicCombatTraining.Initialize("Magic Combat Training", TORCareers.ImperialMagister, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _ancientScrolls.Initialize("Ancient Scrolls", TORCareers.ImperialMagister, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Mighty Powerstones";
                return hero.Clan.Tier >= 2;
            });
            _arcaneKnowledge.Initialize("Arcane Knowledge", TORCareers.ImperialMagister, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            
            
            //Waywatcher
            _protectorOfTheWoods.Initialize("Protector of the Woods", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _pathfinder.Initialize("Pathfinder", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _forestStalker.Initialize("Forest Stalker", TORCareers.Waywatcher, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Swiftshiver shards upgrade for troops";
                return true;
            });
            _hailOfArrows.Initialize("Hail of Arrows", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Hagbane Tipps upgrade for troops";
                return hero.Clan.Tier >= 2;
            });
            
            _hawkeyed.Initialize("Hawkeyed", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _starfireEssence.Initialize("Starfire Essence", TORCareers.Waywatcher, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
          
            _eyeOfTheHunter.Initialize("Eye of the Hunter", TORCareers.Waywatcher, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            }, (Hero hero, out string unlockText) =>
            {
                unlockText = "Unlocks Starfire shafts";
                return hero.Clan.Tier >= 4;
            }); 
            
            
            //Spellsinger
            _pathShaping.Initialize("Path Shaping", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _treeSinging.Initialize("Tree singing", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _vitalSurge.Initialize("Vital Surge", TORCareers.Spellsinger, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _heartOfTheTree.Initialize("Heart of the Tree", TORCareers.Spellsinger, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _arielsBlessing.Initialize("Ariel's Blessing", TORCareers.Spellsinger, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _magicOfAthelLoren.Initialize("Fey Magic", TORCareers.Spellsinger, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
          
            _furyOfTheForest.Initialize("Fury of the Forest", TORCareers.Spellsinger, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            
            //Grey lord 
            _caelithsWisdom.Initialize("Caelith's Wisdom", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _soulBinding.Initialize("Soul Binding", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _legendsOfMalok.Initialize("Legends of Malok", TORCareers.GreyLord, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _unrestrictedMagic.Initialize("Unrestricted Magic", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _forbiddenScrollsOfSaphery.Initialize("Forbidden Scrolls of Saphery", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            
            _byAllMeans.Initialize("By All Means", TORCareers.GreyLord, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
          
            _secretOfFellfang.Initialize("Secret of the Fellfang", TORCareers.GreyLord, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });

        }
    }
}
