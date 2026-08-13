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
        private PerkObject _trueTransmutation;

        //Faith perks
        private PerkObject _novicePrayers;
        private PerkObject _adeptPrayers;
        private PerkObject _grandPrayers;
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
            _trueTransmutation = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TrueTransmutation"));
            //Faith perks
            _novicePrayers = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("NovicePrayers"));
            _adeptPrayers = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("AdeptPrayers"));
            _grandPrayers = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("GrandPrayers"));
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
            _runAndGun.Initialize("{=str_tor_perk_run_and_gun_label}Run and Gun", TORSkills.GunPowder, 50, _mountedHeritage,
                "{=str_tor_perk_run_and_gun_1}While on foot and using a pistol, your accuracy penalty for moving is reduced by 20%.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_run_and_gun_2}Gunpowder infantry troops in your party have their gunpowder skill increased by 30.",
                PartyRole.PartyLeader, 30f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged);
            _mountedHeritage.Initialize("{=str_tor_perk_mounted_heritage_label}Mounted Heritage", TORSkills.GunPowder, 50, _runAndGun,
                "{=str_tor_perk_mounted_heritage_1}Your accuracy is increased by 20% with gunpowder weapons when mounted.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_mounted_heritage_2}Mounted ranged troops in your party have their riding skill increased by 30.",
                PartyRole.PartyLeader, 30f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.Mounted | TroopUsageFlags.Ranged);

            _firingDrills.Initialize("{=str_tor_perk_firing_drills_label}Firing Drills", TORSkills.GunPowder, 100, _ammoWagons,
                "{=str_tor_perk_firing_drills_1}Immediately gain +1 Discipline.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_firing_drills_2}Gunpowder troops in your party recieve +5 experience per day.",
                PartyRole.PartyLeader, 5f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.Ranged);
            _ammoWagons.Initialize("{=str_tor_perk_ammunition_wagons_label}Ammunition Wagons", TORSkills.GunPowder, 100, _firingDrills,
                "{=str_tor_perk_ammunition_wagons_1}+50% starting ammunition for all gunpowder troops (including player).",
                PartyRole.PartyLeader, 0.5f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_ammunition_wagons_2}Increase your party's inventory capacity by 30%.",
                PartyRole.PartyLeader, 0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _closeQuarters.Initialize("{=str_tor_perk_close_quarter_label}Close Quarters", TORSkills.GunPowder, 150, _deadEye,
                "{=str_tor_perk_close_quarter_1}You deal 25% increased damage with gunpowder weapons to enemies within 7 meters.",
                PartyRole.Personal, 0.25f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_close_quarter_2}Ranged troops in your party have their One-handed skill increased by 30.",
                PartyRole.PartyLeader, 30f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.Ranged);
            _deadEye.Initialize("{=str_tor_perk_dead_eye_label}Dead Eye", TORSkills.GunPowder, 150, _closeQuarters,
                "{=str_tor_perk_dead_eye_1}You deal 30% increased damage with longrifles.",
                PartyRole.Personal, 0.3f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_dead_eye_2}Two-Handed gunpowder troops in your formation have their accuracy increased by 20%.",
                PartyRole.Captain, -0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.Ranged);

            _bulletProof.Initialize("{=str_tor_perk_bullet_proof_label}Bullet Proof", TORSkills.GunPowder, 200, _bombingSuit,
                "{=str_tor_perk_bullet_proof_1}You take 15% less damage from handheld ranged weapons.",
                PartyRole.Personal, -0.15f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_bullet_proof_2}Troops in your formation receive 5% less damage from handheld ranged weapons.",
                PartyRole.Captain, -0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _bombingSuit.Initialize("{=str_tor_perk_bomb_suit_label}Bomb Suit", TORSkills.GunPowder, 200, _bulletProof,
                "{=str_tor_perk_bomb_suit_1}You take 25% less damage from siege artillery and explosions.",
                PartyRole.Personal, -0.25f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_bomb_suit_2}Troops in your formation receive 10% less damage from siege artillery and explosions.",
                PartyRole.Captain, -0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _packItIn.Initialize("{=str_tor_perk_pack_it_in_label}Pack It In", TORSkills.GunPowder, 250, _steelTerror,
                "{=str_tor_perk_pack_it_in_1}When you are using a multi-projectile gunpowder weapon, increase the number of projectiles by 50%.",
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_pack_it_in_2}Troops in your formation using explosive gunpowder weapons gain +10% bonus damage and the damage is converted to Fire Damage.",
                PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _steelTerror.Initialize("{=str_tor_perk_steel_terror_label}Steel Terror", TORSkills.GunPowder, 250, _packItIn,
                "{=str_tor_perk_steel_terror_1}Artillery and explosive weapons in your party deal +10% morale damage in battle.",
                PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_steel_terror_2}Artillery crews and engineers in your party become unbreakable due to morale loss.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);

            _piercingShots.Initialize("{=str_tor_perk_piercing_shots_label}Piercing Shots", TORSkills.GunPowder, 300, null,
                "{=str_tor_perk_piercing_shots_1}Your shots ignore 50% of enemy armor and penetrate shields.",
                PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, "{=str_tor_perk_piercing_shots_2}Gunpowder troops in your party pierce shields.", PartyRole.PartyLeader, 0, EffectIncrementType.Invalid);

            _entrySpells.Initialize("{=str_tor_perk_novice_spellcaster_label}Novice Spellcaster", TORSkills.Spellcraft, 25, null,
                "{=str_tor_perk_novice_spellcaster_1}Gain access to entry level spells.", PartyRole.Personal, 0, EffectIncrementType.Invalid);
            _adeptSpells.Initialize("{=str_tor_perk_adept_spellcaster_label}Adept Spellcaster", TORSkills.Spellcraft, 100, null,
                "{=str_tor_perk_adept_spellcaster_1}Gain access to adept level spells.", PartyRole.Personal, 0, EffectIncrementType.Invalid);
            _masterSpells.Initialize("{=str_tor_perk_master_spellcaster_label}Master Spellcaster", TORSkills.Spellcraft, 200, null,
                "{=str_tor_perk_master_spellcaster_1}Gain access to master level spells.", PartyRole.Personal, 0, EffectIncrementType.Invalid);

            _selfish.Initialize("{=str_tor_perk_selfish_label}Selfish", TORSkills.Spellcraft, 50, _wellControlled,
                "{=str_tor_perk_selfish_1}Your damaging spells do 90% reduced damage to yourself.",
                PartyRole.Personal, -0.9f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_selfish_2}Your self targeted buff spells have 50% more duration.",
                PartyRole.Personal, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _wellControlled.Initialize("{=str_tor_perk_well_controlled_label}Well Controlled", TORSkills.Spellcraft, 50, _selfish,
                "{=str_tor_perk_well_controlled_1}Your damaging spells do 30% less damage to troops in your party.",
                PartyRole.Personal, -0.3f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_well_controlled_2}Gain 5% advantage in simulation battles.",
                PartyRole.PartyLeader, 0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _librarian.Initialize("{=str_tor_perk_librarian_label}Librarian", TORSkills.Spellcraft, 125, _storyTeller,
                "{=str_tor_perk_librarian_1}You gain double experience from reading books.",
                PartyRole.Personal, 1f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_librarian_2}Learning new spells cost 50% less gold.",
                PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _storyTeller.Initialize("{=str_tor_perk_story_teller_label}Storyteller", TORSkills.Spellcraft, 125, _librarian,
                "{=str_tor_perk_story_teller_1}Every companion in your party gains 1000 experience in a random skill per day.",
                PartyRole.PartyLeader, 1000f, EffectIncrementType.Add,
                "{=str_tor_perk_story_teller_2}Your party gains a permanent +5 increase to party morale.",
                PartyRole.PartyLeader, 5f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);

            _overCaster.Initialize("{=str_tor_perk_overcaster_label}Overcaster", TORSkills.Spellcraft, 150, _efficientSpellCaster,
                "{=str_tor_perk_overcaster_1}Your instant damaging and healing spells are 20% more effective but cost 30% more winds of magic.",
                PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor,
                string.Empty,
                PartyRole.None, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _efficientSpellCaster.Initialize("{=str_tor_perk_effective_spellcaster_label}Efficient Spellcaster", TORSkills.Spellcraft, 150, _overCaster,
                "{=str_tor_perk_effective_spellcaster_1}Your instant damaging and healing spells are 20% less effective, but cost 30% less winds of magic.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                string.Empty,
                PartyRole.None, -0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _improvision.Initialize("{=str_tor_perk_improvision_label}Improvision", TORSkills.Spellcraft, 225, _catalyst,
                "{=str_tor_perk_improvision_1}Your Winds of Magic is set to 25 if you have less than that at the beginning of the battle.",
                PartyRole.Personal, 25f, EffectIncrementType.Add,
                "{=str_tor_perk_improvision_2}+10% Persuasion chance during speech checks.",
                PartyRole.Personal, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _catalyst.Initialize("{=str_tor_perk_catalyst_label}Catalyst", TORSkills.Spellcraft, 225, _improvision,
                "{=str_tor_perk_catalyst_1}For every magical item in your equipment slots you gain +5 extra Winds of magic at the start of battle.",
                PartyRole.Personal, 5f, EffectIncrementType.Add,
                "{=str_tor_perk_catalyst_2}You gain +20% Winds of Magic regeneration while waiting in a town.",
                PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _dampener.Initialize("{=str_tor_perk_dampener_label}Dampener", TORSkills.Spellcraft, 250, _arcaneLink,
                "{=str_tor_perk_dampener_1}Damage dealt by your damaging spells is reduced by 15%, but troops in your formation take 30% less damage from spells.",
                PartyRole.Personal, -0.15f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_dampener_2}You gain 5% ward save.",
                PartyRole.Personal, -0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _arcaneLink.Initialize("{=str_tor_perk_arcane_link_label}Arcane Link", TORSkills.Spellcraft, 250, _dampener,
                "{=str_tor_perk_arcane_link_1}Any buffs you cast on a friendly unit will now also apply to you even if you are not in range.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_arcane_link_2}As formation Captain, all troops in your formation deal additonal 10% magic damage.",
                PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _trueTransmutation.Initialize("{=str_tor_perk_true_transmutation_label}True Transmutation", TORSkills.Spellcraft, 300, null,
                "{=str_tor_perk_true_transmutation_1}Allows you to apply 2 enchantments to weapons. Dwarfs can apply a 3rd enchantment.",
                PartyRole.Personal, 2f, EffectIncrementType.Add);

            _novicePrayers.Initialize("{=str_tor_perk_novice_prayer_label}Novice Prayers", TORSkills.Faith, 25, null,
                "{=str_tor_perk_novice_prayer_1}Gain access to all novice level battle prayers.", PartyRole.Personal, 0, EffectIncrementType.Invalid);
            _adeptPrayers.Initialize("{=str_tor_perk_adept_prayer_label}Adept Prayers", TORSkills.Faith, 75, null,
                "{=str_tor_perk_adept_prayer_1}Gain access to all adept level battle prayers.", PartyRole.Personal, 0, EffectIncrementType.Invalid);
            _grandPrayers.Initialize("{=str_tor_perk_grand_prayer_label}Grand Prayers", TORSkills.Faith, 125, null,
                "{=str_tor_perk_grand_prayer_1}Gain access to all grand level battle prayers.", PartyRole.Personal, 0, EffectIncrementType.Invalid);

            _devotee.Initialize("{=str_tor_perk_devotee_label}Devotee", TORSkills.Faith, 50, _divineMission,
                "{=str_tor_perk_devotee_1}You gain +3 hitpoints for every point in Discipline.",
                PartyRole.Personal, 3f, EffectIncrementType.Add,
                "{=str_tor_perk_devotee_2}Praying at a shrine grants 50% increased devotion towards your chosen religion.",
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _divineMission.Initialize("{=str_tor_perk_divine_mission_label}Divine Mission", TORSkills.Faith, 50, _devotee,
                "{=str_tor_perk_divine_mission_1}You gain 1 focus point in Medicine.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_divine_mission_2}5% increased militia growth in settlements owned by your clan.",
                PartyRole.ClanLeader, 0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _imperturbable.Initialize("{=str_tor_perk_imperturbable_label}Imperturbable", TORSkills.Faith, 100, _superstitious,
                "{=str_tor_perk_imperturbable_1}Gain 500 Faith skill experience every day while waiting in a town.",
                PartyRole.Personal, 500f, EffectIncrementType.Add,
                "{=str_tor_perk_imperturbable_2}Religious units in your party gain 10% physical resistance.",
                PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None); //description should be updated to state town specifically
            //Primary role is implemented as PartyLeader - description or implementation should be changed
            _superstitious.Initialize("{=str_tor_perk_superstitious_label}Superstitious", TORSkills.Faith, 100, _imperturbable,
                "{=str_tor_perk_superstitious_1}Cursed regions on the world map have 20% reduced damaging effect on your party.",
                PartyRole.PartyLeader, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_superstitious_2}Religious units in your party gain 10% bonus physical damage.",
                PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _offering.Initialize("{=str_tor_perk_offering_label}Offering", TORSkills.Faith, 150, null,
                "{=str_tor_perk_offering_1}Obtain the ability to donate items at shrines as sacrifical offering to gain Faith.",
                PartyRole.Personal, 1f, EffectIncrementType.Add);

            _blessed.Initialize("{=str_tor_perk_blessed_label}Blessed", TORSkills.Faith, 200, _foreSight,
                "{=str_tor_perk_blessed_1}Your clan gains 3 Influence every day for every clan party with an active blessing.",
                PartyRole.ClanLeader, 3f, EffectIncrementType.Add,
                "{=str_tor_perk_blessed_2}While your party has an active blessing, religious units in your party gain 10xp every day.",
                PartyRole.PartyMember, 10f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);
            _foreSight.Initialize("{=str_tor_perk_foresight_label}Foresight", TORSkills.Faith, 200, _blessed,
                "{=str_tor_perk_foresight_1}Gain a free attribute point.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_foresight_2}Increase your party's sight range on the campaign map by 10%.",
                PartyRole.PartyMember, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _revival.Initialize("{=str_tor_perk_revival_label}Revival", TORSkills.Faith, 250, _spirit,
                "{=str_tor_perk_revival_1}After all medicine/healing related skills fail, gain a second 30% chance for characters and units that would be killed to be wounded instead.",
                PartyRole.PartyLeader, 0.3f, EffectIncrementType.AddFactor);
            _spirit.Initialize("{=str_tor_perk_spirit_label}Spirit", TORSkills.Faith, 250, _revival,
                "{=str_tor_perk_spirit_1}Whenever a tier 6 or higher unit dies, distribute their experience among the rest of the troops in the party.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add);

            _miracle.Initialize("{=str_tor_perk_miracle_label}Miracle", TORSkills.Faith, 300, null,
                "{=str_tor_perk_miracle_1}Your faith is so strong that it is able to manifest miraculous events from time to time.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_miracle_2}Allows you to apply 2 blessings to weapons.",
                PartyRole.Personal, 2f, EffectIncrementType.Add);
        }

        public static class Spellcraft
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
            public static PerkObject TrueTransmutation => Instance._trueTransmutation;
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
            public static PerkObject NovicePrayers => Instance._novicePrayers;
            public static PerkObject AdeptPrayers => Instance._adeptPrayers;
            public static PerkObject GrandPrayers => Instance._grandPrayers;
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