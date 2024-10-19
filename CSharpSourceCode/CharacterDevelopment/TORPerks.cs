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
            _exchange = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("Exchange"));

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
            _runAndGun.Initialize("{=tor_perk_run_and_gun_label_str}Run and Gun", TORSkills.GunPowder, 50, _mountedHeritage, 
                "{=tor_perk_run_and_gun_1_str}While on foot and using a pistol, your accuracy penalty for moving is reduced by 20%.", 
                SkillEffect.PerkRole.Personal, -0.2f, SkillEffect.EffectIncrementType.AddFactor, 
                "{=tor_perk_run_and_gun_2_str}Gunpowder infantry troops in your party have their gunpowder skill increased by 30.", 
                SkillEffect.PerkRole.PartyLeader, 30f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged);
            _mountedHeritage.Initialize("{=tor_perk_mounted_heritage_label_str}Mounted Heritage", TORSkills.GunPowder, 50, _runAndGun,
                "{=tor_perk_mounted_heritage_1_str}Your accuracy is increased by 20% with gunpowder weapons when mounted.",
                SkillEffect.PerkRole.Personal, -0.2f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_mounted_heritage_2_str}Mounted ranged troops in your party have their riding skill increased by 30.",
                SkillEffect.PerkRole.PartyLeader, 30f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.Mounted | TroopUsageFlags.Ranged);

            _firingDrills.Initialize("{=tor_perk_firing_drills_label_str}Firing Drills", TORSkills.GunPowder, 100, _ammoWagons,
                "{=tor_perk_firing_drills_1_str}Immediately gain +1 Discipline.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_firing_drills_2_str}Gunpowder troops in your party recieve +5 experience per day.",
                SkillEffect.PerkRole.PartyLeader, 5f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.Ranged);
            _ammoWagons.Initialize("{=tor_perk_ammunition_wagons_label_str}Ammunition Wagons", TORSkills.GunPowder, 100, _firingDrills,
                "{=tor_perk_ammunition_wagons_1_str}+50% starting ammunition for all gunpowder troops (including player).",
                SkillEffect.PerkRole.PartyLeader, 0.5f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_ammunition_wagons_2_str}Increase your party's inventory capacity by 30%.",
                SkillEffect.PerkRole.PartyLeader, 0.3f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _closeQuarters.Initialize("{=tor_perk_close_quarter_label_str}Close Quarters", TORSkills.GunPowder, 150, _deadEye,
                "{=tor_perk_close_quarter_1_str}You deal 25% increased damage with gunpowder weapons to enemies within 7 meters.",
                SkillEffect.PerkRole.Personal, 0.25f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_close_quarter_2_str}Ranged troops in your party have their One-handed skill increased by 30.",
                SkillEffect.PerkRole.PartyLeader, 30f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.Ranged);
            _deadEye.Initialize("{=tor_perk_dead_eye_label_str}Dead Eye", TORSkills.GunPowder, 150, _closeQuarters,
                "{=tor_perk_dead_eye_1_str}You deal 30% increased damage with longrifles.",
                SkillEffect.PerkRole.Personal, 0.3f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_dead_eye_2_str}Two-Handed gunpowder troops in your party have their accuracy increased by 20%.",
                SkillEffect.PerkRole.Captain, -0.2f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.Ranged);

            _bulletProof.Initialize("{=tor_perk_bullet_proof_label_str}Bullet Proof", TORSkills.GunPowder, 200, _bombingSuit,
                "{=tor_perk_bullet_proof_1_str}You take 15% less damage from handheld ranged weapons.",
                SkillEffect.PerkRole.Personal, -0.15f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_bullet_proof_2_str}Troops in your party recieve 5% less damage from handheld ranged weapons.",
                SkillEffect.PerkRole.Captain, -0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _bombingSuit.Initialize("{=tor_perk_bomb_suit_label_str}Bomb Suit", TORSkills.GunPowder, 200, _bulletProof,
                "{=tor_perk_bomb_suit_1_str}You take 25% less damage from siege artillery and explosions.",
                SkillEffect.PerkRole.Personal, -0.25f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_bomb_suit_2_str}Troops in your party recieve 10% less damage from siege artillery and explosions.",
                SkillEffect.PerkRole.Captain, -0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _packItIn.Initialize("{=tor_perk_pack_it_in_label_str}Pack It In", TORSkills.GunPowder, 250, _steelTerror,
                "{=tor_perk_pack_it_in_1_str}When you are using a multi-projectile gunpowder weapon, increase the number of projectiles by 50%.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_pack_it_in_2_str}Troops in your formation gain +10% bonus damage and the damage is converted to Fire Damage when they are using explosive gunpowder weapons.",
                SkillEffect.PerkRole.Captain, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _steelTerror.Initialize("{=tor_perk_steel_terror_label_str}Steel Terror", TORSkills.GunPowder, 250, _packItIn,
                "{=tor_perk_steel_terror_1_str}Artillery and Heavy Gunpowder weapons deal +10% morale damage in battle.",
                SkillEffect.PerkRole.Captain, 0.1f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_steel_terror_2_str}Artillery crews and engineers in your party become unbreakable due to morale loss.",
                SkillEffect.PerkRole.PartyLeader, 1f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);

            _piercingShots.Initialize("{=tor_perk_piercing_shots_label_str}Piercing Shots", TORSkills.GunPowder, 300, null, 
                "{=tor_perk_piercing_shots_1_str}Your shots ignore 50% of enemy armor and troops using gunpowder weapons penetrate shields with their shots.", 
                SkillEffect.PerkRole.Personal, -0.5f, SkillEffect.EffectIncrementType.AddFactor);

            _entrySpells.Initialize("{=tor_perk_novice_spellcaster_label_str}Novice Spellcaster", TORSkills.SpellCraft, 25, null, 
                "{=tor_perk_novice_spellcaster_1_str}Gain access to entry level spells.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);
            _adeptSpells.Initialize("{=tor_perk_adept_spellcaster_label_str}Adept Spellcaster", TORSkills.SpellCraft, 100, null,
                "{=tor_perk_adept_spellcaster_1_str}Gain access to adept level spells.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);
            _masterSpells.Initialize("{=tor_perk_master_spellcaster_label_str}Master Spellcaster", TORSkills.SpellCraft, 200, null,
                "{=tor_perk_master_spellcaster_1_str}Gain access to master level spells.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);

            _selfish.Initialize("{=tor_perk_selfish_label_str}Selfish", TORSkills.SpellCraft, 50, _wellControlled,
                "{=tor_perk_selfish_1_str}Your damaging spells do 90% reduced damage to yourself.",
                SkillEffect.PerkRole.Personal, -0.9f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_selfish_2_str}Your self targeted buff spells have 50% more duration.",
                SkillEffect.PerkRole.Personal, 0.15f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _wellControlled.Initialize("{=tor_perk_well_controlled_label_str}Well Controlled", TORSkills.SpellCraft, 50, _selfish,
                "{=tor_perk_well_controlled_1_str}Your damaging spells do 30% less damage to troops in your party.",
                SkillEffect.PerkRole.Personal, -0.3f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_well_controlled_2_str}Gain 5% advantage in simulation battles.",
                SkillEffect.PerkRole.PartyLeader, 0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _librarian.Initialize("{=tor_perk_librarian_label_str}Librarian", TORSkills.SpellCraft, 125, _storyTeller,
                "{=tor_perk_librarian_1_str}You gain double experience from reading books.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_librarian_2_str}Learning new spells cost 50% less gold.",
                SkillEffect.PerkRole.Personal, -0.5f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _storyTeller.Initialize("{=tor_perk_story_teller_label_str}Storyteller", TORSkills.SpellCraft, 125, _librarian,
                "{=tor_perk_story_teller_1_str}Every companion in your party gains 1000 experience in a random skill per day.",
                SkillEffect.PerkRole.PartyLeader, 1000f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_story_teller_2_str}Your party gains a permanent +5 increase to party morale.",
                SkillEffect.PerkRole.PartyLeader, 5f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);

            _overCaster.Initialize("{=tor_perk_overcaster_label_str}Overcaster", TORSkills.SpellCraft, 150, _efficientSpellCaster,
                "{=tor_perk_overcaster_1_str}Your damaging spells do 20% more damage but cost 30% more winds of magic.",
                SkillEffect.PerkRole.Personal, 0.2f, SkillEffect.EffectIncrementType.AddFactor,
                string.Empty,
                SkillEffect.PerkRole.None, 0.15f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _efficientSpellCaster.Initialize("{=tor_perk_effective_spellcaster_label_str}Efficient Spellcaster", TORSkills.SpellCraft, 150, _overCaster,
                "{=tor_perk_effective_spellcaster_1_str}Your damaging spells do 20% less damage, but cost 30% less winds of magic.",
                SkillEffect.PerkRole.Personal, -0.2f, SkillEffect.EffectIncrementType.AddFactor,
                string.Empty,
                SkillEffect.PerkRole.None, -0.15f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _improvision.Initialize("{=tor_perk_improvision_label_str}Improvision", TORSkills.SpellCraft, 225, _catalyst,
                "{=tor_perk_improvision_1_str}Your Winds of Magic is set to 25 if you have less than that at the beginning of the battle.",
                SkillEffect.PerkRole.Personal, 25f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_improvision_2_str}+10% Persuasion chance during speech checks.",
                SkillEffect.PerkRole.Personal, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _catalyst.Initialize("{=tor_perk_catalyst_label_str}Catalyst", TORSkills.SpellCraft, 225, _improvision,
                "{=tor_perk_catalyst_1_str}For every magical item in your equipment slots you gain +5 extra Winds of magic at the start of battle.",
                SkillEffect.PerkRole.Personal, 5f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_catalyst_2_str}You gain +20% Winds of Magic regeneration while waiting in a town.",
                SkillEffect.PerkRole.Personal, 0.2f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _dampener.Initialize("{=tor_perk_dampener_label_str}Dampener", TORSkills.SpellCraft, 250, _arcaneLink,
                "{=tor_perk_dampener_1_str}Damage dealt by your damaging spells is reduced by 15%, but troops in your formation take 30% less damage from spells.",
                SkillEffect.PerkRole.Personal, -0.15f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_dampener_2_str}You gain 5% ward save.",
                SkillEffect.PerkRole.Personal, -0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _arcaneLink.Initialize("{=tor_perk_arcane_link_label_str}Arcane Link", TORSkills.SpellCraft, 250, _dampener,
                "{=tor_perk_arcane_link_1_str}Any buffs you cast on a friendly unit will now also apply to you even if you are not in range.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_arcane_link_2_str}As formation Captain, all troops in your formation deal additonal 10% magic damage.",
                SkillEffect.PerkRole.Captain, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _exchange.Initialize("{=tor_perk_exchange_label_str}Exchange", TORSkills.SpellCraft, 300, null,
                "{=tor_perk_exchange_1_str}All physical damage done by your weapons is doubled and dealt again as magical damage.", 
                SkillEffect.PerkRole.Personal, 2f, SkillEffect.EffectIncrementType.AddFactor);

            _novicePrayers.Initialize("{=tor_perk_novice_prayer_label_str}Novice Prayers", TORSkills.Faith, 25, null,
                "{=tor_perk_novice_prayer_1_str}Gain access to all novice level battle prayers.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);
            _adeptPrayers.Initialize("{=tor_perk_adept_prayer_label_str}Adept Prayers", TORSkills.Faith, 75, null,
                "{=tor_perk_adept_prayer_1_str}Gain access to all adept level battle prayers.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);
            _grandPrayers.Initialize("{=tor_perk_grand_prayer_label_str}Grand Prayers", TORSkills.Faith, 125, null,
                "{=tor_perk_grand_prayer_1_str}Gain access to all grand level battle prayers.", SkillEffect.PerkRole.Personal, 0, SkillEffect.EffectIncrementType.Invalid);

            _devotee.Initialize("{=tor_perk_devotee_label_str}Devotee", TORSkills.Faith, 50, _divineMission,
                "{=tor_perk_devotee_1_str}You gain +3 hitpoints for every point in Discipline.",
                SkillEffect.PerkRole.Personal, 3f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_devotee_2_str}Praying at a shrine grants 50% increased devotion towards your chosen religion.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _divineMission.Initialize("{=tor_perk_devine_mission_label_str}Divine Mission", TORSkills.Faith, 50, _devotee,
                "{=tor_perk_devine_mission_1_str}You gain 1 focus point in Medicine.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_devine_mission_2_str}5% increased militia growth in settlements owned by your clan.",
                SkillEffect.PerkRole.ClanLeader, 0.05f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _imperturbable.Initialize("{=tor_perk_imperturbable_label_str}Imperturbable", TORSkills.Faith, 100, _superstitious,
                "{=tor_perk_imperturbable_1_str}Gain 500 Faith skill experience every day while waiting at a settlement.",
                SkillEffect.PerkRole.Personal, 500f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_imperturbable_2_str}Religious units in your party gain 10% physical resistance.",
                SkillEffect.PerkRole.PartyLeader, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
            _superstitious.Initialize("{=tor_perk_superstitious_label_str}Superstitious", TORSkills.Faith, 100, _imperturbable,
                "{=tor_perk_superstitious_1_str}Cursed regions on the world map have 20% reduced damaging effect on your party.",
                SkillEffect.PerkRole.PartyLeader, -0.2f, SkillEffect.EffectIncrementType.AddFactor,
                "{=tor_perk_superstitious_2_str}Religious units in your party gain 10% bonus physical damage.",
                SkillEffect.PerkRole.PartyLeader, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _offering.Initialize("{=tor_perk_offering_label_str}Offering", TORSkills.Faith, 150, null,
                "{=tor_perk_offering_1_str}Obtain the ability to donate items at shrines as sacrifical offering to gain Faith.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add);

            _blessed.Initialize("{=tor_perk_blessed_label_str}Blessed", TORSkills.Faith, 200, _foreSight,
                "{=tor_perk_blessed_1_str}Your clan gains 3 Influence every day for every clan party with an active blessing.",
                SkillEffect.PerkRole.ClanLeader, 3f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_blessed_2_str}While your party has an active blessing, religious units in your party gain 10xp every day.",
                SkillEffect.PerkRole.PartyMember, 10f, SkillEffect.EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);
            _foreSight.Initialize("{=tor_perk_foresight_label_str}Foresight", TORSkills.Faith, 200, _blessed,
                "{=tor_perk_foresight_1_str}Gain a free attribute point.",
                SkillEffect.PerkRole.Personal, 1f, SkillEffect.EffectIncrementType.Add,
                "{=tor_perk_foresight_2_str}Increase your party's sight range on the campaign map by 10%.",
                SkillEffect.PerkRole.PartyMember, 0.1f, SkillEffect.EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);

            _revival.Initialize("{=tor_perk_rivival_label_str}Revival", TORSkills.Faith, 250, _spirit,
                "{=tor_perk_rivival_1_str}After all medicine/healing related skills fail, gain a second 50% chance for characters and units that would be killed to be wounded instead.",
                SkillEffect.PerkRole.PartyLeader, 0.5f, SkillEffect.EffectIncrementType.AddFactor);
            _spirit.Initialize("{=tor_perk_spirit_label_str}Spirit", TORSkills.Faith, 250, _revival,
                "{=tor_perk_spirit_1_str}Whenever a tier 6 or higher unit dies, distribute their experience among the rest of the troops in the party.",
                SkillEffect.PerkRole.PartyLeader, 1f, SkillEffect.EffectIncrementType.Add);

            _miracle.Initialize("{=tor_perk_miracle_label_str}Miracle", TORSkills.Faith, 300, null,
                "{=tor_perk_miracle_1_str}Your faith is so strong that it is able to manifest miraculous events from time to time.",
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
