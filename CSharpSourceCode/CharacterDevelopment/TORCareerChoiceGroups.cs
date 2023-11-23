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
        private CareerChoiceGroupObject _mercenaryLord;
        private CareerChoiceGroupObject _commander;
        //Grail Knight
        private CareerChoiceGroupObject _errantryWar;
        private CareerChoiceGroupObject _enhancedHorseCombat;
        private CareerChoiceGroupObject _questingVow;
        private CareerChoiceGroupObject _monsterSlayer;
        private CareerChoiceGroupObject _masterHorseman;
        private CareerChoiceGroupObject _grailVow;
        //Grail Damsel
        private CareerChoiceGroupObject _feyEnchantment;
        private CareerChoiceGroupObject _inspirationOfTheLady;
        private CareerChoiceGroupObject _talesOfGiles;
        private CareerChoiceGroupObject _vividVisions;
        private CareerChoiceGroupObject _justCause;
        private CareerChoiceGroupObject _secretsOfTheGrail;
        private CareerChoiceGroupObject _envoyOfTheLady;


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
            _nightRider = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("NightRider"));
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
            
            _feyEnchantment = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("FeyEnchantment"));
            _inspirationOfTheLady = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("InspirationOfTheLady")); 
            _talesOfGiles = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("TalesOfGiles"));
            _vividVisions = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("VividVisions")); 
            _justCause = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("JustCause")); 
            _secretsOfTheGrail = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("SecretsOfTheGrail"));
            _envoyOfTheLady = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnvoyOfTheLady"));
            
        }

        private void InitializeAll()
        {
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
            
            
            _newBlood.Initialize("{=new_blood_choice_group_str}New Blood", TORCareers.MinorVampire, 1, (Hero hero, out string text) =>
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
            
            //Grail Damsel
            
            _feyEnchantment.Initialize("{=fey_enchantment_choice_group_str}Fey Enchantment", TORCareers.GrailDamsel, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
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

        }
    }
}
