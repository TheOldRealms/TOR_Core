using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;

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
        //Minor Vampire
        private CareerChoiceGroupObject _newBlood;
        private CareerChoiceGroupObject _arkayne;
        private CareerChoiceGroupObject _courtley;
        private CareerChoiceGroupObject _lordly;
        private CareerChoiceGroupObject _martialle;
        private CareerChoiceGroupObject _masterOfDead;
        //Blood Knight
        private CareerChoiceGroupObject _peerlessWarrior;
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
        private CareerChoiceGroupObject _mercenaryLord;
        private CareerChoiceGroupObject _commander;
        //Grail Knight
        private CareerChoiceGroupObject _errantryWar;
        private CareerChoiceGroupObject _enhancedHorseCombat;
        private CareerChoiceGroupObject _questingVow;
        private CareerChoiceGroupObject _monsterSlayer;
        private CareerChoiceGroupObject _masterHorseman;
        private CareerChoiceGroupObject _grailVow;
        


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
            
            _newBlood = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("NewBlood"));
            _arkayne = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Arkayne"));
            _courtley = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Courtley"));
            _lordly = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Lordly"));
            _martialle = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Martialle"));
            _masterOfDead = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MasterOfDead"));
            
            _peerlessWarrior = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("PeerlessWarrior"));
            _bladeMaster = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BladeMaster"));
            _doomRider = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("DoomRider"));
            _controlledHunger = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ControlledHunger"));
            _avatarOfDeath = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("AvatarOfDeath"));
            _dreadKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("DreadKnight"));
            
            _survivalist = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Survivalist"));
            _duelist = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Duelist"));
            _headhunter = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Headhunter"));
            _knightly = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Knightly"));
            _mercenaryLord = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord"));
            _commander = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("Commander"));
            
            _errantryWar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ErrantryWar"));
            _enhancedHorseCombat = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnhancedHorseCombat"));; 
            _questingVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("QuestingVow"));
            _monsterSlayer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MonsterSlayer")); 
            _masterHorseman = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MasterHorseman")); 
            _grailVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("GrailVow"));
            
        }

        private void InitializeAll()
        {
            _bookOfSigmar.Initialize("Book of Sigmar", TORCareers.WarriorPriest, 1, (Hero hero, out string text) =>
            {
                text = "Required renown: 1";
                return hero.Clan.Tier >= 1;
            });
            _sigmarProclaimer.Initialize("Sigmar's Proclaimer", TORCareers.WarriorPriest, 1, (Hero hero, out string text) =>
            {
                text = "Required renown: 1";
                return hero.Clan.Tier >= 1;
            });
            _relentlessFanatic.Initialize("Relentless Fanatic", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _protectorOfTheWeak.Initialize("Protector of the Weak", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _holyPurge.Initialize("Holy Purge", TORCareers.WarriorPriest, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _archLector.Initialize("Arch Lector", TORCareers.WarriorPriest, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            
            
            _newBlood.Initialize("New Blood", TORCareers.MinorVampire, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _arkayne.Initialize("The Arkayne", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _courtley.Initialize("The Courtley", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _lordly.Initialize("The Lordly", TORCareers.MinorVampire, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _martialle.Initialize("The Martialle", TORCareers.MinorVampire, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            _masterOfDead.Initialize("Master of Dead", TORCareers.MinorVampire, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            
            //Blood Knight
            _peerlessWarrior.Initialize("Peerless Warrior", TORCareers.BloodKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _bladeMaster.Initialize("Blade Master", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _doomRider.Initialize("Doom Rider", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _controlledHunger.Initialize("Controlled Hunger", TORCareers.BloodKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _avatarOfDeath.Initialize("Avatar of Death", TORCareers.BloodKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            _dreadKnight.Initialize("Dread Knight", TORCareers.BloodKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            
            //Mercenary
            _survivalist.Initialize("The Survivalist", TORCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _duelist.Initialize("The Duelist", TORCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _headhunter.Initialize("The Headhunter", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _knightly.Initialize("The Knightly", TORCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _mercenaryLord.Initialize("The Mercenary Lord", TORCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _commander.Initialize("The Commander", TORCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 4";
                return hero.Clan.Tier >= 4;
            });
            
            //Grail Knight
            
            _errantryWar.Initialize("Errantry War", TORCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _enhancedHorseCombat.Initialize("Enhanced Horse Combat", TORCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _questingVow.Initialize("Questing Vow", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _monsterSlayer.Initialize("Monster Slayer", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 3";
                return hero.Clan.Tier >= 3;
            });
            _masterHorseman.Initialize("Master Horseman", TORCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            _grailVow.Initialize("Grail Vow", TORCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required renown: 5";
                return hero.Clan.Tier >= 5;
            });
            
        }
    }
}
