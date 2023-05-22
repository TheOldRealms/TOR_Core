using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TOR_Core.CharacterDevelopment
{
    public class TORPerks
    {
        //Gunpowder perks
        private PerkObject _runAndGun;
        private PerkObject _mountedHeritage;
        private PerkObject _firingDrills;
        private PerkObject _ammoWagons;
        private PerkObject _closeQuarters;
        private PerkObject _deadEye;
        private PerkObject _bulletProof;
        private PerkObject _bombingSuit;
        private PerkObject _packItIn;
        private PerkObject _steelTerror;
        private PerkObject _piercingShots;

        //Spellcraft perks
        private PerkObject _entrySpells;
        private PerkObject _adeptSpells;
        private PerkObject _masterSpells;
        private PerkObject _selfish;
        private PerkObject _wellControlled;
        private PerkObject _librarian;
        private PerkObject _storyTeller;
        private PerkObject _overCaster;
        private PerkObject _efficientSpellCaster;
        private PerkObject _improvision;
        private PerkObject _catalyst;
        private PerkObject _dampener;
        private PerkObject _arcaneLink;
        private PerkObject _exchange;

        //Faith perks
        private PerkObject _devotee;
        private PerkObject _divineMission;
        private PerkObject _imperturbable;
        private PerkObject _superstitious;
        private PerkObject _offering;
        private PerkObject _blessed;
        private PerkObject _foreSight;
        private PerkObject _revival;
        private PerkObject _spirit;
        private PerkObject _miracle;

        public static TORPerks Instance { get; private set; }

        public TORPerks()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }

        private void RegisterAll()
        {
            //Gunpowder perks
            _runAndGun = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("RunAndGun"));
            _mountedHeritage = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("MountedHeritage"));
            _firingDrills = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("FiringDrills"));
            _ammoWagons = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("AmmoWagons"));
            _closeQuarters = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("CloseQuarters"));
            _deadEye = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("DeadEye"));
            _bulletProof = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("BulletProof"));
            _bombingSuit = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("BombingSuit"));
            _packItIn = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("PackItIn"));
            _steelTerror = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("SteelTerror"));
            _piercingShots = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("PiercingShots"));

            //Spellcraft perks
            _entrySpells = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("EntrySpells"));
            _adeptSpells = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("AdeptSpells"));
            _masterSpells = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("MasterSpells"));
            _selfish = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Selfish"));
            _wellControlled = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("WellControlled"));
            _librarian = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Librarian"));
            _storyTeller = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("StoryTeller"));
            _overCaster = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("OverCaster"));
            _efficientSpellCaster = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("EfficientSpellCaster"));
            _improvision = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Improvision"));
            _catalyst = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Catalyst"));
            _dampener = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Dampener"));
            _arcaneLink = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("ArcaneLink"));
            _exchange = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Exchange"));

            //Faith perks
            _devotee = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Devotee"));
            _divineMission = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("DivineMission"));
            _imperturbable = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Imperturbable"));
            _superstitious = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Superstitious"));
            _offering = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Offering"));
            _blessed = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Blessed"));
            _foreSight = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("ForeSight"));
            _revival = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Revival"));
            _spirit = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Spirit"));
            _miracle = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Miracle"));
        }

        private void InitializeAll()
        {
            _runAndGun.Initialize("{=!}Run and Gun", TORSkills.GunPowder, 50, _mountedHeritage, 
                "{=!}While on foot and using a pistol, your accuracy penalty for moving is reduced by 20%.", 
                SkillEffect.PerkRole.Personal, -0.2f, SkillEffect.EffectIncrementType.AddFactor, 
                "{=!}Gunpowder infantry troops in your party have their gunpowder skill increased by 30.", 
                SkillEffect.PerkRole.PartyLeader, 30f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.Infantry | TroopClassFlag.Ranged);
            _mountedHeritage.Initialize("{=!}Mounted Heritage", TORSkills.GunPowder, 50, _runAndGun,
                "{=!}Your accuracy is increased by 20% with gunpowder weapons when mounted.",
                SkillEffect.PerkRole.Personal, -0.2f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Mounted ranged troops in your party have their riding skill increased by 30.",
                SkillEffect.PerkRole.PartyLeader, 30f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.Cavalry | TroopClassFlag.Ranged);

            _firingDrills.Initialize("{=!}Firing Drills", TORSkills.GunPowder, 100, _ammoWagons,
                "{=!}Immediately gain +1 Discipline.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=!}Gunpowder troops in your party recieve +5 experience per day.",
                SkillEffect.PerkRole.PartyLeader, 5f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.Ranged);
            _ammoWagons.Initialize("{=!}Ammunition Wagons", TORSkills.GunPowder, 100, _firingDrills,
                "{=!}+50% starting ammunition for all gunpowder troops (including player).",
                SkillEffect.PerkRole.PartyLeader, 0.5f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Increase your party's inventory capacity by 30%.",
                SkillEffect.PerkRole.PartyLeader, 0.3f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _closeQuarters.Initialize("{=!}Close Quarters", TORSkills.GunPowder, 150, _deadEye,
                "{=!}You deal 25% increased damage with gunpowder weapons to enemies within 7 meters.",
                SkillEffect.PerkRole.Personal, 0.25f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Ranged troops in your party have their One-handed skill increased by 30.",
                SkillEffect.PerkRole.PartyLeader, 30f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.Ranged);
            _deadEye.Initialize("{=!}Dead Eye", TORSkills.GunPowder, 150, _closeQuarters,
                "{=!}You deal 30% increased damage with longrifles.",
                SkillEffect.PerkRole.Personal, 0.3f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Two-Handed gunpowder troops in your party have their accuracy increased by 20%.",
                SkillEffect.PerkRole.Captain, -0.2f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.Ranged);

            _bulletProof.Initialize("{=!}Bullet Proof", TORSkills.GunPowder, 200, _bombingSuit,
                "{=!}You take 15% less damage from handheld ranged weapons.",
                SkillEffect.PerkRole.Personal, -0.15f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Troops in your party recieve 5% less damage from handheld ranged weapons.",
                SkillEffect.PerkRole.Captain, -0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _bombingSuit.Initialize("{=!}Bomb Suit", TORSkills.GunPowder, 200, _bulletProof,
                "{=!}You take 25% less damage from siege artillery and explosions.",
                SkillEffect.PerkRole.Personal, -0.25f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Troops in your party recieve 10% less damage from siege artillery and explosions.",
                SkillEffect.PerkRole.Captain, -0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _packItIn.Initialize("{=!}Pack It In", TORSkills.GunPowder, 250, _steelTerror,
                "{=!}When you are using a multi-projectile gunpowder weapon, increase the number of projectiles by 50%.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Troops in your formation gain +10% bonus damage and the damage is converted to Fire Damage when they are using explosive gunpowder weapons.",
                SkillEffect.PerkRole.Captain, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _steelTerror.Initialize("{=!}Steel Terror", TORSkills.GunPowder, 250, _packItIn,
                "{=!}Artillery and Heavy Gunpowder weapons deal +10% morale damage in battle.",
                SkillEffect.PerkRole.Captain, 0.1f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Artillery crews and engineers in your party become unbreakable due to morale loss.",
                SkillEffect.PerkRole.PartyLeader, 1f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.None);

            _piercingShots.Initialize("{=!}Piercing Shots", TORSkills.GunPowder, 300, null, 
                "{=!}Your shots ignore 50% of enemy armor and troops using gunpowder weapons penetrate shields with their shots.", 
                SkillEffect.PerkRole.Personal, -0.5f, SkillEffect.EffectIncrementType.AddFactor);

            _entrySpells.Initialize("{=!}Novice Spellcaster", TORSkills.SpellCraft, 25, null, 
                "{=!}Gain access to entry level spells.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);
            _adeptSpells.Initialize("{=!}Adept Spellcaster", TORSkills.SpellCraft, 75, null,
                "{=!}Gain access to adept level spells.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);
            _masterSpells.Initialize("{=!}Master Spellcaster", TORSkills.SpellCraft, 125, null,
                "{=!}Gain access to master level spells.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);

            _selfish.Initialize("{=!}Selfish", TORSkills.SpellCraft, 50, _wellControlled,
                "{=!}Your damaging spells do 90% reduced damage to yourself.",
                SkillEffect.PerkRole.Personal, -0.9f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Your self targeted buff spells have 50% more duration.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _wellControlled.Initialize("{=!}Well Controlled", TORSkills.SpellCraft, 50, _selfish,
                "{=!}Your damaging spells do 30% less damage to troops in your party.",
                SkillEffect.PerkRole.Personal, -0.3f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Gain 5% advantage in simulation battles.",
                SkillEffect.PerkRole.PartyLeader, 0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _librarian.Initialize("{=!}Librarian", TORSkills.SpellCraft, 100, _storyTeller,
                "{=!}You gain double experience from reading books.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Learning new spells cost 50% less gold.",
                SkillEffect.PerkRole.Personal, -0.5f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _storyTeller.Initialize("{=!}Storyteller", TORSkills.SpellCraft, 100, _librarian,
                "{=!}Every companion in your party gains 1000 experience in a random skill per day.",
                SkillEffect.PerkRole.PartyLeader, 1000f, SkillEffect.EffectIncrementType.Add,
                "{=!}Your party gains a permanent +5 increase to party morale.",
                SkillEffect.PerkRole.PartyLeader, 5f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.None);

            _overCaster.Initialize("{=!}Overcaster", TORSkills.SpellCraft, 150, _efficientSpellCaster,
                "{=!}Your damaging spells do 20% more damage but cost 10% more winds of magic.",
                SkillEffect.PerkRole.Personal, 0.2f, SkillEffect.EffectIncrementType.AddFactor,
                string.Empty,
                SkillEffect.PerkRole.None, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _efficientSpellCaster.Initialize("{=!}Efficient Spellcaster", TORSkills.SpellCraft, 150, _overCaster,
                "{=!}Your damaging spells do 20% less damage, but cost 30% less winds of magic.",
                SkillEffect.PerkRole.Personal, -0.2f, SkillEffect.EffectIncrementType.AddFactor,
                string.Empty,
                SkillEffect.PerkRole.None, -0.3f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _improvision.Initialize("{=!}Improvision", TORSkills.SpellCraft, 200, _catalyst,
                "{=!}Your Winds of Magic is set to 25 if you have less than that at the beginning of the battle.",
                SkillEffect.PerkRole.Personal, 25f, SkillEffect.EffectIncrementType.Add,
                "{=!}+10% Persuasion chance during speech checks.",
                SkillEffect.PerkRole.Personal, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _catalyst.Initialize("{=!}Catalyst", TORSkills.SpellCraft, 200, _improvision,
                "{=!}For every magical item in your equipment slots you gain +5 extra Winds of magic at the start of battle.",
                SkillEffect.PerkRole.Personal, 5f, SkillEffect.EffectIncrementType.Add,
                "{=!}You gain +20% Winds of Magic regeneration while waiting in a town.",
                SkillEffect.PerkRole.Personal, 0.2f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _dampener.Initialize("{=!}Dampener", TORSkills.SpellCraft, 250, _arcaneLink,
                "{=!}Damage dealt by your damaging spells is reduced by 15%, but troops in your formation take 30% less damage from spells.",
                SkillEffect.PerkRole.Personal, -0.15f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}You gain 5% ward save.",
                SkillEffect.PerkRole.Personal, -0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _arcaneLink.Initialize("{=!}Arcane Link", TORSkills.SpellCraft, 250, _dampener,
                "{=!}Any buffs you cast on a friendly unit will now also apply to you even if you are not in range.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=!}As formation Captain, all troops in your formation deal additonal 10% magic damage.",
                SkillEffect.PerkRole.Captain, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _exchange.Initialize("{=!}Exchange", TORSkills.SpellCraft, 300, null,
                "{=!}All physical damage done by your weapons is doubled and dealt again as magical damage.", 
                SkillEffect.PerkRole.Personal, 2f, SkillEffect.EffectIncrementType.AddFactor);

            _devotee.Initialize("{=!}Devotee", TORSkills.Faith, 50, _divineMission,
                "{=!}You gain +3 hitpoints for every point in Discipline.",
                SkillEffect.PerkRole.Personal, 3f, SkillEffect.EffectIncrementType.Add,
                "{=!}Praying at a shrine grants 50% increased devotion towards your chosen religion.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _divineMission.Initialize("{=!}Divine Mission", TORSkills.Faith, 50, _devotee,
                "{=!}You gain 1 focus point in Medicine.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=!}5% increased militia growth in settlements owned by your clan.",
                SkillEffect.PerkRole.ClanLeader, 0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _imperturbable.Initialize("{=!}Imperturbable", TORSkills.Faith, 100, _superstitious,
                "{=!}Gain 500 Faith skill experience every day while waiting at a settlement.",
                SkillEffect.PerkRole.Personal, 500f, SkillEffect.EffectIncrementType.Add,
                "{=!}Religious units in your party gain 10% physical resistance.",
                SkillEffect.PerkRole.PartyLeader, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);
            _superstitious.Initialize("{=!}Superstitious", TORSkills.Faith, 100, _imperturbable,
                "{=!}Cursed regions on the world map have 20% reduced damaging effect on your party.",
                SkillEffect.PerkRole.PartyLeader, -0.2f, SkillEffect.EffectIncrementType.AddFactor,
                "{=!}Religious units in your party gain 10% bonus physical damage.",
                SkillEffect.PerkRole.PartyLeader, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _offering.Initialize("{=!}Offering", TORSkills.Faith, 150, null,
                "{=!}Obtain the ability to donate items at shrines as sacrifical offering to gain Renown.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add);

            _blessed.Initialize("{=!}Blessed", TORSkills.Faith, 200, _foreSight,
                "{=!}Your clan gains 3 Influence every day for every clan party with an active blessing.",
                SkillEffect.PerkRole.ClanLeader, 3f, SkillEffect.EffectIncrementType.Add,
                "{=!}While your party has an active blessing, religious units in your party gain 10xp every day.",
                SkillEffect.PerkRole.PartyMember, 10f, SkillEffect.EffectIncrementType.Add, TroopClassFlag.None, TroopClassFlag.None);
            _foreSight.Initialize("{=!}Foresight", TORSkills.Faith, 200, _blessed,
                "{=!}Gain a free attribute point.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=!}Increase your party's sight range on the campaign map by 10%.",
                SkillEffect.PerkRole.PartyMember, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopClassFlag.None, TroopClassFlag.None);

            _revival.Initialize("{=!}Revival", TORSkills.Faith, 250, _spirit,
                "{=!}After all medicine/healing related skills fail, gain a second 50% chance for characters and units that would be killed to be wounded instead.",
                SkillEffect.PerkRole.PartyLeader, 0.5f, SkillEffect.EffectIncrementType.AddFactor);
            _spirit.Initialize("{=!}Spirit", TORSkills.Faith, 250, _revival,
                "{=!}Whenever a tier 6 or higher unit dies, distribute their experience among the rest of the troops in the party.",
                SkillEffect.PerkRole.PartyLeader, 1f, SkillEffect.EffectIncrementType.Add);

            _miracle.Initialize("{=!}Miracle", TORSkills.Faith, 300, null,
                "{=!}Your faith is so strong that it is able to manifest miraculous events from time to time.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add);
        }

        public static class SpellCraft
        {
            public static PerkObject EntrySpells => Instance._entrySpells;
            public static PerkObject AdeptSpells => Instance._adeptSpells;
            public static PerkObject MasterSpells => Instance._masterSpells;
            public static PerkObject Selfish => Instance._selfish;
            public static PerkObject WellControlled => Instance._wellControlled;
            public static PerkObject Librarian => Instance._librarian;
            public static PerkObject StoryTeller => Instance._storyTeller;
            public static PerkObject OverCaster => Instance._overCaster;
            public static PerkObject EfficientSpellCaster => Instance._efficientSpellCaster;
            public static PerkObject Improvision => Instance._improvision;
            public static PerkObject Catalyst => Instance._catalyst;
            public static PerkObject Dampener => Instance._dampener;
            public static PerkObject ArcaneLink => Instance._arcaneLink;
            public static PerkObject Exchange => Instance._exchange;
        }

        public static class GunPowder
        {
            public static PerkObject RunAndGun => Instance._runAndGun;
            public static PerkObject MountedHeritage => Instance._mountedHeritage;
            public static PerkObject FiringDrills => Instance._firingDrills;
            public static PerkObject AmmoWagons => Instance._ammoWagons;
            public static PerkObject CloseQuarters => Instance._closeQuarters;
            public static PerkObject DeadEye => Instance._deadEye;
            public static PerkObject BulletProof => Instance._bulletProof;
            public static PerkObject BombingSuit => Instance._bombingSuit;
            public static PerkObject PackItIn => Instance._packItIn;
            public static PerkObject SteelTerror => Instance._steelTerror;
            public static PerkObject PiercingShots => Instance._piercingShots;
        }

        public static class Faith
        {
            public static PerkObject Devotee => Instance._devotee;
            public static PerkObject DivineMission => Instance._divineMission;
            public static PerkObject Imperturbable => Instance._imperturbable;
            public static PerkObject Superstitious => Instance._superstitious;
            public static PerkObject Offering => Instance._offering;
            public static PerkObject Blessed => Instance._blessed;
            public static PerkObject ForeSight => Instance._foreSight;
            public static PerkObject Revival => Instance._revival;
            public static PerkObject Spirit => Instance._spirit;
            public static PerkObject Miracle => Instance._miracle;
        }
    }
}
