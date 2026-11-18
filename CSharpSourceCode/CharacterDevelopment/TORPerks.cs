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

        //Roguery perks (TOR rework)
        private PerkObject _keenSenses;
        private PerkObject _lowProfile;
        private PerkObject _raider;
        private PerkObject _parlay;
        private PerkObject _highRoller;
        private PerkObject _shakedown;
        private PerkObject _reprisal;
        private PerkObject _byTheRoad;
        private PerkObject _diversion;
        private PerkObject _wolfPack;
        private PerkObject _intimidator;
        private PerkObject _burntEarth;
        private PerkObject _highwayman;
        private PerkObject _disguise;
        private PerkObject _skirmisher;
        private PerkObject _fencer;
        private PerkObject _backstabber;
        private PerkObject _negotiator;
        private PerkObject _fence;
        private PerkObject _unarmed;
        private PerkObject _strider;
        private PerkObject _shadowed;
        private PerkObject _masterThief;

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
            //Roguery perks (TOR rework)
            _keenSenses = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_KeenSenses"));
            _lowProfile = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_LowProfile"));
            _raider = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Raider"));
            _parlay = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Parlay"));
            _highRoller = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_HighRoller"));
            _shakedown = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Shakedown"));
            _reprisal = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Reprisal"));
            _byTheRoad = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_ByTheRoad"));
            _diversion = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Diversion"));
            _wolfPack = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_WolfPack"));
            _intimidator = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Intimidator"));
            _burntEarth = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_BurntEarth"));
            _highwayman = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Highwayman"));
            _disguise = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Disguise"));
            _skirmisher = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Skirmisher"));
            _fencer = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Fencer"));
            _backstabber = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Backstabber"));
            _negotiator = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Negotiator"));
            _fence = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Fence"));
            _unarmed = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Unarmed"));
            _strider = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Strider"));
            _shadowed = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_Shadowed"));
            _masterThief = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("TOR_Roguery_MasterThief"));
        }

        /// <remarks>
        /// TroopUsageFlags are only accounted for if the PartyRole for the effect is Captain.
        /// Undefined : perk won't apply. (Troop can't have a relevant flag)
        /// Any : applies to all troops.
        /// </remarks>
        private void InitializeAll()
        {
            _runAndGun.Initialize("{=str_tor_perk_run_and_gun_label}Run and Gun", TORSkills.GunPowder, 50, _mountedHeritage,
                "{=str_tor_perk_run_and_gun_1}While on foot and using a pistol, your accuracy penalty for moving is reduced by 20%.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_run_and_gun_2}Gunpowder infantry troops in your party have their gunpowder skill increased by 30.",
                PartyRole.PartyLeader, 30f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged);
            _mountedHeritage.Initialize("{=str_tor_perk_mounted_heritage_label}Mounted Heritage", TORSkills.GunPowder, 50, _runAndGun,
                "{=str_tor_perk_mounted_heritage_1}Your accuracy is increased by 20% with gunpowder weapons when mounted.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_mounted_heritage_2}Mounted ranged troops in your party have their riding skill increased by 30.",
                PartyRole.PartyLeader, 30f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Mounted | TroopUsageFlags.Ranged);

            _firingDrills.Initialize("{=str_tor_perk_firing_drills_label}Firing Drills", TORSkills.GunPowder, 100, _ammoWagons,
                "{=str_tor_perk_firing_drills_1}Immediately gain +1 Discipline.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_firing_drills_2}Gunpowder troops in your party recieve +5 experience per day.",
                PartyRole.PartyLeader, 5f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Ranged);
            _ammoWagons.Initialize("{=str_tor_perk_ammunition_wagons_label}Ammunition Wagons", TORSkills.GunPowder, 100, _firingDrills,
                "{=str_tor_perk_ammunition_wagons_1}+50% starting ammunition for all gunpowder troops (including player).",
                PartyRole.PartyLeader, 0.5f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_ammunition_wagons_2}Increase your party's inventory capacity by 30%.",
                PartyRole.PartyLeader, 0.3f, EffectIncrementType.AddFactor);

            _closeQuarters.Initialize("{=str_tor_perk_close_quarter_label}Close Quarters", TORSkills.GunPowder, 150, _deadEye,
                "{=str_tor_perk_close_quarter_1}You deal 25% increased damage with gunpowder weapons to enemies within 7 meters.",
                PartyRole.Personal, 0.25f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_close_quarter_2}Ranged troops in your party have their One-handed skill increased by 30.",
                PartyRole.PartyLeader, 30f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Ranged);
            _deadEye.Initialize("{=str_tor_perk_dead_eye_label}Dead Eye", TORSkills.GunPowder, 150, _closeQuarters,
                "{=str_tor_perk_dead_eye_1}You deal 30% increased damage with longrifles.",
                PartyRole.Personal, 0.3f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_dead_eye_2}Two-Handed gunpowder troops in your formation have their accuracy increased by 20%.",
                PartyRole.Captain, -0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Ranged);

            _bulletProof.Initialize("{=str_tor_perk_bullet_proof_label}Bullet Proof", TORSkills.GunPowder, 200, _bombingSuit,
                "{=str_tor_perk_bullet_proof_1}You take 15% less damage from handheld ranged weapons.",
                PartyRole.Personal, -0.15f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_bullet_proof_2}Troops in your formation receive 5% less damage from handheld ranged weapons.",
                PartyRole.Captain, -0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any);
            _bombingSuit.Initialize("{=str_tor_perk_bomb_suit_label}Bomb Suit", TORSkills.GunPowder, 200, _bulletProof,
                "{=str_tor_perk_bomb_suit_1}You take 25% less damage from siege artillery and explosions.",
                PartyRole.Personal, -0.25f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_bomb_suit_2}Troops in your formation receive 10% less damage from siege artillery and explosions.",
                PartyRole.Captain, -0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any);

            _packItIn.Initialize("{=str_tor_perk_pack_it_in_label}Pack It In", TORSkills.GunPowder, 250, _steelTerror,
                "{=str_tor_perk_pack_it_in_1}When you are using a multi-projectile gunpowder weapon, increase the number of projectiles by 50%.",
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_pack_it_in_2}Troops in your formation using explosive gunpowder weapons gain +10% bonus damage and the damage is converted to Fire Damage.",
                PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any);
            _steelTerror.Initialize("{=str_tor_perk_steel_terror_label}Steel Terror", TORSkills.GunPowder, 250, _packItIn,
                "{=str_tor_perk_steel_terror_1}Artillery and explosive weapons in your party deal +10% morale damage in battle.",
                PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_steel_terror_2}Artillery crews and engineers in your party become unbreakable due to morale loss.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add);

            _piercingShots.Initialize("{=str_tor_perk_piercing_shots_label}Piercing Shots", TORSkills.GunPowder, 300, null,
                "{=str_tor_perk_piercing_shots_1}Your shots ignore 50% of enemy armor and can penetrate shields.",
                PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, "{=str_tor_perk_piercing_shots_2}Gunpowder troops in your party can pierce shields.", PartyRole.PartyLeader, 0, EffectIncrementType.Invalid);

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
                PartyRole.Personal, 0.50f, EffectIncrementType.AddFactor);
            _wellControlled.Initialize("{=str_tor_perk_well_controlled_label}Well Controlled", TORSkills.Spellcraft, 50, _selfish,
                "{=str_tor_perk_well_controlled_1}Your damaging spells do 30% less damage to troops in your party.",
                PartyRole.Personal, -0.3f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_well_controlled_2}Gain 5% advantage in simulation battles.",
                PartyRole.PartyLeader, 0.05f, EffectIncrementType.AddFactor);

            _librarian.Initialize("{=str_tor_perk_librarian_label}Librarian", TORSkills.Spellcraft, 125, _storyTeller,
                "{=str_tor_perk_librarian_1}You gain double experience from reading books.",
                PartyRole.Personal, 1f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_librarian_2}Learning new spells cost 50% less gold.",
                PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor);
            _storyTeller.Initialize("{=str_tor_perk_story_teller_label}Storyteller", TORSkills.Spellcraft, 125, _librarian,
                "{=str_tor_perk_story_teller_1}Every companion in your party gains 1000 experience in a random skill per day.",
                PartyRole.PartyLeader, 1000f, EffectIncrementType.Add,
                "{=str_tor_perk_story_teller_2}Your party gains a permanent +5 increase to party morale.",
                PartyRole.PartyLeader, 5f, EffectIncrementType.Add);

            //Sly : why were these not defined with each half in the respective effect string?
            _overCaster.Initialize("{=str_tor_perk_overcaster_label}Overcaster", TORSkills.Spellcraft, 150, _efficientSpellCaster,
                "{=str_tor_perk_overcaster_1}Your damaging and healing spells are 20% more effective but cost 30% more winds of magic.",
                PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor,
                string.Empty,
                PartyRole.None, 0.3f, EffectIncrementType.AddFactor);
            _efficientSpellCaster.Initialize("{=str_tor_perk_effective_spellcaster_label}Efficient Spellcaster", TORSkills.Spellcraft, 150, _overCaster,
                "{=str_tor_perk_effective_spellcaster_1}Your damaging and healing spells are 20% less effective, but cost 30% less winds of magic.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                string.Empty,
                PartyRole.None, -0.3f, EffectIncrementType.AddFactor);

            _improvision.Initialize("{=str_tor_perk_improvision_label}Improvision", TORSkills.Spellcraft, 225, _catalyst,
                "{=str_tor_perk_improvision_1}Your Winds of Magic is set to 25 if you have less than that at the beginning of the battle.",
                PartyRole.Personal, 25f, EffectIncrementType.Add,
                "{=str_tor_perk_improvision_2}+10% Persuasion chance during speech checks.",
                PartyRole.Personal, 0.1f, EffectIncrementType.AddFactor);
            _catalyst.Initialize("{=str_tor_perk_catalyst_label}Catalyst", TORSkills.Spellcraft, 225, _improvision,
                "{=str_tor_perk_catalyst_1}For every magical item in your equipment slots you gain +5 extra Winds of magic at the start of battle.",
                PartyRole.Personal, 5f, EffectIncrementType.Add,
                "{=str_tor_perk_catalyst_2}You gain +20% Winds of Magic regeneration while waiting in a town.",
                PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor);

            _dampener.Initialize("{=str_tor_perk_dampener_label}Dampener", TORSkills.Spellcraft, 250, _arcaneLink,
                "{=str_tor_perk_dampener_1}Damage dealt by your damaging spells is reduced by 15%, but troops in your formation take 30% less damage from spells.",
                PartyRole.Personal, -0.15f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_dampener_2}You gain 5% ward save.",
                PartyRole.Personal, -0.05f, EffectIncrementType.AddFactor);
            _arcaneLink.Initialize("{=str_tor_perk_arcane_link_label}Arcane Link", TORSkills.Spellcraft, 250, _dampener,
                "{=str_tor_perk_arcane_link_1}Any buffs you cast on a friendly unit will now also apply to you even if you are not in range.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_arcane_link_2}As formation Captain, all troops in your formation deal additonal 10% magic damage.",
                PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.Undefined, TroopUsageFlags.Any);

            _trueTransmutation.Initialize("{=str_tor_perk_true_transmutation_label}True Transmutation", TORSkills.Spellcraft, 300, null,
                "{=str_tor_perk_true_transmutation_1}Allows you to apply 2 enchantments. Dwarfs can apply a 3rd enchantment.",
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
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor);
            _divineMission.Initialize("{=str_tor_perk_divine_mission_label}Divine Mission", TORSkills.Faith, 50, _devotee,
                "{=str_tor_perk_divine_mission_1}You gain 1 focus point in Medicine.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_divine_mission_2}+1 militia growth in settlements owned by your clan.",
                PartyRole.ClanLeader, 1f, EffectIncrementType.Add);

            _imperturbable.Initialize("{=str_tor_perk_imperturbable_label}Imperturbable", TORSkills.Faith, 100, _superstitious,
                "{=str_tor_perk_imperturbable_1}Gain 500 Faith skill experience every day while waiting in a town.",
                PartyRole.Personal, 500f, EffectIncrementType.Add,
                "{=str_tor_perk_imperturbable_2}Religious units in your party gain 10% physical resistance.",
                PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor);
            _superstitious.Initialize("{=str_tor_perk_superstitious_label}Superstitious", TORSkills.Faith, 100, _imperturbable,
                "{=str_tor_perk_superstitious_1}Cursed regions on the world map have 20% reduced damaging effect on your party.",
                PartyRole.PartyLeader, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_superstitious_2}Religious units in your party gain 10% bonus physical damage.",
                PartyRole.PartyLeader, 0.1f, EffectIncrementType.AddFactor);

            _offering.Initialize("{=str_tor_perk_offering_label}Offering", TORSkills.Faith, 150, null,
                "{=str_tor_perk_offering_1}Obtain the ability to donate items at shrines as sacrifical offering to gain Faith.",
                PartyRole.Personal, 1f, EffectIncrementType.Add);

            _blessed.Initialize("{=str_tor_perk_blessed_label}Blessed", TORSkills.Faith, 200, _foreSight,
                "{=str_tor_perk_blessed_1}Your clan gains 3 Influence every day for every clan party with an active blessing.",
                PartyRole.ClanLeader, 3f, EffectIncrementType.Add,
                "{=str_tor_perk_blessed_2}While your party has an active blessing, religious units in your party gain 10xp every day.",
                PartyRole.PartyMember, 10f, EffectIncrementType.Add);
            _foreSight.Initialize("{=str_tor_perk_foresight_label}Foresight", TORSkills.Faith, 200, _blessed,
                "{=str_tor_perk_foresight_1}Gain a free attribute point.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_foresight_2}Increase your party's sight range on the campaign map by 10%.",
                PartyRole.PartyMember, 0.1f, EffectIncrementType.AddFactor);

            _revival.Initialize("{=str_tor_perk_revival_label}Revival", TORSkills.Faith, 250, _spirit,
                "{=str_tor_perk_revival_1}After all medicine/healing related skills fail, gain a second 30% chance for characters and units that would be killed to be wounded instead.",
                PartyRole.PartyLeader, 0.3f, EffectIncrementType.AddFactor);
            _spirit.Initialize("{=str_tor_perk_spirit_label}Spirit", TORSkills.Faith, 250, _revival,
                "{=str_tor_perk_spirit_1}Whenever a tier 6 or higher unit dies, distribute their experience among the rest of the troops in the party.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add);

            _miracle.Initialize("{=str_tor_perk_miracle_label}Miracle", TORSkills.Faith, 300, null,
                "{=str_tor_perk_miracle_1}Your faith is so strong that it is able to manifest miraculous events from time to time.",
                PartyRole.Personal, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_miracle_2}Allows you to apply 2 blessings.",
                PartyRole.PartyMember, 2f, EffectIncrementType.Add);

            _raider.Initialize("{=str_tor_perk_raider_label}Raider", DefaultSkills.Roguery, 25, _parlay,
                "{=str_tor_perk_raider_1}Your party moves 5% faster while raiding villages.",
                PartyRole.PartyLeader, 0.05f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_raider_2}Your quartermaster earns Tactics experience from raid operations.",
                PartyRole.Quartermaster, 1f, EffectIncrementType.Add);
            // Parlay secondary stash is not implemented yet.
            _parlay.Initialize("{=str_tor_perk_parlay_label}Parlay", DefaultSkills.Roguery, 25, _raider,
                "{=str_tor_perk_parlay_1}20% chance to end a bandit encounter peacefully during conversation.",
                PartyRole.PartyLeader, 0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_parlay_2}You keep 50% of your inventory when taken prisoner.",
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor);

            _highRoller.Initialize("{=str_tor_perk_high_roller_label}High Roller", DefaultSkills.Roguery, 50, _shakedown,
                "{=str_tor_perk_high_roller_1}Your maximum tournament bet is doubled.",
                PartyRole.Personal, 1f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_high_roller_2}Wages for your Master Ambusher troops are reduced by 20%.",
                PartyRole.Quartermaster, -0.2f, EffectIncrementType.AddFactor);
            _shakedown.Initialize("{=str_tor_perk_shakedown_label}Shakedown", DefaultSkills.Roguery, 50, _highRoller,
                "{=str_tor_perk_shakedown_1}Villagers are 15% less likely to resist when you demand goods.",
                PartyRole.Quartermaster, -0.15f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_shakedown_2}Nearby villages reduce your ambush stealth by 10% less.",
                PartyRole.PartyLeader, -0.1f, EffectIncrementType.AddFactor);

            _reprisal.Initialize("{=str_tor_perk_reprisal_label}Reprisal", DefaultSkills.Roguery, 75, _byTheRoad,
                "{=str_tor_perk_reprisal_1}Recover 15% faster from a failed ambush.",
                PartyRole.PartyLeader, -0.15f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_reprisal_2}Enemy reinforcements take 20 seconds longer to arrive during your ambush attacks.",
                PartyRole.Quartermaster, 20f, EffectIncrementType.Add);
            _byTheRoad.Initialize("{=str_tor_perk_by_the_road_label}By the Road", DefaultSkills.Roguery, 75, _reprisal,
                "{=str_tor_perk_by_the_road_1}+5% loot from caravans and villages.",
                PartyRole.PartyLeader, 0.05f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_by_the_road_2}+1 security per day in settlements you govern.",
                PartyRole.Governor, 1f, EffectIncrementType.Add);

            _keenSenses.Initialize("{=str_tor_perk_keen_senses_label}Keen Senses", DefaultSkills.Roguery, 100, _lowProfile,
                "{=str_tor_perk_keen_senses_1}Your party's spotting range is increased by 25%.",
                PartyRole.PartyLeader, 0.25f, EffectIncrementType.AddFactor);
            _lowProfile.Initialize("{=str_tor_perk_low_profile_label}Low Profile", DefaultSkills.Roguery, 100, _keenSenses,
                "{=str_tor_perk_low_profile_1}Your party is 25% harder to spot by enemy parties.",
                PartyRole.PartyLeader, 0.25f, EffectIncrementType.AddFactor);

            _diversion.Initialize("{=str_tor_perk_diversion_label}Diversion", DefaultSkills.Roguery, 125, _wolfPack,
                "{=str_tor_perk_diversion_1}Prisoners in your party can create a diversion, increasing your chance to escape from captivity.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_diversion_2}+10 prisoner capacity.",
                PartyRole.PartyLeader, 10f, EffectIncrementType.Add);
            _wolfPack.Initialize("{=str_tor_perk_wolf_pack_label}Wolf Pack", DefaultSkills.Roguery, 125, _diversion,
                "{=str_tor_perk_wolf_pack_1}Bring 5 additional ambushers to your ambush attacks.",
                PartyRole.PartyLeader, 5f, EffectIncrementType.Add,
                "{=str_tor_perk_wolf_pack_2}Your party consumes 10% less food while in ambush stance.",
                PartyRole.PartyLeader, -0.1f, EffectIncrementType.AddFactor);

            _intimidator.Initialize("{=str_tor_perk_intimidator_label}Intimidator", DefaultSkills.Roguery, 150, _burntEarth,
                "{=str_tor_perk_intimidator_1}30% chance for villagers, caravans, and bandits to surrender when you demand it.",
                PartyRole.Personal, 0.3f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_intimidator_2}Tavern mercenaries cost 20% less to hire.",
                PartyRole.PartyLeader, -0.2f, EffectIncrementType.AddFactor);
            _burntEarth.Initialize("{=str_tor_perk_burnt_earth_label}Burnt Earth", DefaultSkills.Roguery, 150, _intimidator,
                "{=str_tor_perk_burnt_earth_1}Your crime rating rises 20% more slowly.",
                PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_burnt_earth_2}Village notables lose 75% less relation with you after their village is raided.",
                PartyRole.PartyLeader, -0.75f, EffectIncrementType.AddFactor);

            _highwayman.Initialize("{=str_tor_perk_highwayman_label}Highwayman", DefaultSkills.Roguery, 175, _disguise,
                "{=str_tor_perk_highwayman_1}Fully wiping out a caravan during an ambush leaves no witnesses: no crime rating, no relation loss, no war declared.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_highwayman_2}Your Master Ambusher troops gain daily experience while your party is in ambush stance.",
                PartyRole.PartyLeader, 20f, EffectIncrementType.Add);
            _disguise.Initialize("{=str_tor_perk_disguise_label}Disguise", DefaultSkills.Roguery, 175, _highwayman,
                "{=str_tor_perk_disguise_1}You can trade in towns while disguised.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add,
                "{=str_tor_perk_disguise_2}Trade penalties with factions where you have a crime rating are reduced by 50%.",
                PartyRole.ClanLeader, -0.5f, EffectIncrementType.AddFactor);

            _skirmisher.Initialize("{=str_tor_perk_skirmisher_label}Skirmisher", DefaultSkills.Roguery, 200, _fencer,
                "{=str_tor_perk_skirmisher_1}+10 vigor and control for your Master Ambusher troops in your formation.",
                PartyRole.Captain, 10f, EffectIncrementType.Add,
                "{=str_tor_perk_skirmisher_2}+1 recruitment slot when recruiting from gang leaders.",
                PartyRole.PartyLeader, 1f, EffectIncrementType.Add, TroopUsageFlags.Any, TroopUsageFlags.Undefined);
            _fencer.Initialize("{=str_tor_perk_fencer_label}Fencer", DefaultSkills.Roguery, 200, _skirmisher,
                "{=str_tor_perk_fencer_1}+20% loot when villagers comply with your hostile action.",
                PartyRole.PartyLeader, 0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_fencer_2}+5 tariff revenue from towns you govern.",
                PartyRole.Governor, 5f, EffectIncrementType.Add);

            _backstabber.Initialize("{=str_tor_perk_backstabber_label}Backstabber", DefaultSkills.Roguery, 225, _negotiator,
                "{=str_tor_perk_backstabber_1}+10 damage on your thrust attacks.",
                PartyRole.PartyLeader, 10f, EffectIncrementType.Add,
                "{=str_tor_perk_backstabber_2}Troops in your formation deal +4 damage on thrust attacks.",
                PartyRole.Captain, 4f, EffectIncrementType.Add, TroopUsageFlags.Undefined, TroopUsageFlags.Any);
            _negotiator.Initialize("{=str_tor_perk_negotiator_label}Negotiator", DefaultSkills.Roguery, 225, _backstabber,
                "{=str_tor_perk_negotiator_1}25% better deals when ransoming heroes through brokers.",
                PartyRole.Quartermaster, 0.25f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_negotiator_2}+30% escape chance for your hero prisoners.",
                PartyRole.Quartermaster, 0.3f, EffectIncrementType.AddFactor);

            _fence.Initialize("{=str_tor_perk_fence_label}Fence", DefaultSkills.Roguery, 250, _unarmed,
                "{=str_tor_perk_fence_1}-20% weapon sell-price penalty.",
                PartyRole.PartyLeader, -0.2f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_fence_2}+2 militia per day in besieged towns you govern.",
                PartyRole.Governor, 2f, EffectIncrementType.Add);
            _unarmed.Initialize("{=str_tor_perk_unarmed_label}Unarmed", DefaultSkills.Roguery, 250, _fence,
                "{=str_tor_perk_unarmed_1}50% longer stun duration on kick attacks.",
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_unarmed_2}+2 food in besieged towns you govern.",
                PartyRole.Governor, 2f, EffectIncrementType.Add);

            _strider.Initialize("{=str_tor_perk_strider_label}Strider", DefaultSkills.Roguery, 275, _shadowed,
                "{=str_tor_perk_strider_1}+50% damage bonus from speed while fighting on foot.",
                PartyRole.Personal, 0.5f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_strider_2}Cities you govern produce 250 denars per day while a tournament is active.",
                PartyRole.Governor, 250f, EffectIncrementType.Add);
            _shadowed.Initialize("{=str_tor_perk_shadowed_label}Shadowed", DefaultSkills.Roguery, 275, _strider,
                "{=str_tor_perk_shadowed_1}+10% combat speed for your Master Ambusher troops in formation.",
                PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor,
                "{=str_tor_perk_shadowed_2}+30% escape chance when you are captured by mobile parties.",
                PartyRole.Personal, 0.3f, EffectIncrementType.AddFactor, TroopUsageFlags.Any, TroopUsageFlags.Undefined);

            _masterThief.Initialize("{=str_tor_perk_master_thief_label}Master Thief", DefaultSkills.Roguery, 300, null,
                "{=str_tor_perk_master_thief_1}You gain +1% loot for every Roguery skill point above 200.",
                PartyRole.Personal, 0.01f, EffectIncrementType.AddFactor);
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

        public static class Roguery
        {
            public static PerkObject KeenSenses => Instance._keenSenses;
            public static PerkObject LowProfile => Instance._lowProfile;
            public static PerkObject Raider => Instance._raider;
            public static PerkObject Parlay => Instance._parlay;
            public static PerkObject HighRoller => Instance._highRoller;
            public static PerkObject Shakedown => Instance._shakedown;
            public static PerkObject Reprisal => Instance._reprisal;
            public static PerkObject ByTheRoad => Instance._byTheRoad;
            public static PerkObject Diversion => Instance._diversion;
            public static PerkObject WolfPack => Instance._wolfPack;
            public static PerkObject Intimidator => Instance._intimidator;
            public static PerkObject BurntEarth => Instance._burntEarth;
            public static PerkObject Highwayman => Instance._highwayman;
            public static PerkObject Disguise => Instance._disguise;
            public static PerkObject Skirmisher => Instance._skirmisher;
            public static PerkObject Fencer => Instance._fencer;
            public static PerkObject Backstabber => Instance._backstabber;
            public static PerkObject Negotiator => Instance._negotiator;
            public static PerkObject Fence => Instance._fence;
            public static PerkObject Unarmed => Instance._unarmed;
            public static PerkObject Strider => Instance._strider;
            public static PerkObject Shadowed => Instance._shadowed;
            public static PerkObject MasterThief => Instance._masterThief;
        }
    }
}