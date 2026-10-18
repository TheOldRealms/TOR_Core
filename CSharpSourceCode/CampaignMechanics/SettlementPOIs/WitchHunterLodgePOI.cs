using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Witch Hunter Lodge POI — the Order of Sigmar chapterhouses. Reachable from the
    /// <c>town_artisan</c> submenu in whichever Empire settlements provide a scene override
    /// for <c>tor_witchhunter_lodge</c> in <c>tor_settlements.xml</c> (currently Wurtbad,
    /// Leicheberg, Meissen, Bögenhafen, Bechafen, Eisental, Ferlangen). Race-locked to
    /// humans; no career gate. The lodge NPC — currently the relocated BountyMaster —
    /// hosts the hub dialog defined in <see cref="RegisterDialogs"/>.
    /// </summary>
    public class WitchHunterLodgePOI : SettlementPOI
    {
        public override string LocationId => "tor_witchhunter_lodge";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;    // sit directly above "Leave"

        public override string EnterLabelId => "tor_poi_witchhunter_lodge.EnterLabel";
        public override string EnterLabelDefault => "Visit the Witch Hunter Lodge";

        public override string MapIconText => "H";   // text fallback until sprite art lands

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s))
            {
                denialReason = R("Undiscovered", "You have not yet found this place.");
                return false;
            }
            if (!AppliesTo(s))
            {
                denialReason = R("Sealed", "This place is sealed.");
                return false;
            }
            if (s.IsUnderSiege)
            {
                denialReason = R("SiegeSealed", "The lodge is closed during the siege.");
                return false;
            }
            // Race lock — humans only. No career gate (the lodge welcomes any human visitor).
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
            {
                denialReason = R("Rejected", "The templars turn you away.");
                return false;
            }

            denialReason = null;
            return true;
        }

        /// <summary>
        /// Inquisitor hub dialog. Mockup: six-option interaction fan — Hunts, Bounties,
        /// Turn-ins, Store, (reserved special), Leave. M1 wires the hub shell; the four
        /// content branches land as stubs that return to the hub, to be filled by M2–M5.
        /// </summary>
        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            // Greeting — only fires when the player speaks to the lodge's resident
            // bounty-master-turned-Inquisitor inside the lodge location. Guards against
            // the hub appearing in a tavern where the bounty master may still live
            // (Empire towns without a lodge).
            obj.AddDialogLine("tor_witchhunter_lodge_greet", "start", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.hub_greet",
                    "What brings you to the Lodge?"),
                IsInquisitorPartner, null, 200, null);

            // --- hub player lines (mockup wording verbatim)
            // Hunts has two variants with identical player text: when the Inquisitor is
            // carrying an issue (HuntCultists / PlaguedVillage / future hunts) we hand off
            // to vanilla's hero_main_options state so the standard issue brief + accept/
            // decline flow runs. Otherwise we fall through to the stub reply.
            obj.AddPlayerLine("tor_witchhunter_lodge_hunts", "tor_witchhunter_lodge_hub", "hero_main_options",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                InquisitorHasHunt, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_hunts_none", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_hunts_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                () => !InquisitorHasHunt(), null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_bounties", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_bounties_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_bounties",
                    "Are there any bounties the Inquisition needs done?"),
                null, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_turnins", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_turnins_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_turnins",
                    "Lord Inquisitor! I have some things you might be interested in."),
                null, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_store", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_store_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_store",
                    "I wish to acquire some Inquisition armaments."),
                null, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_leave", "tor_witchhunter_lodge_hub", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_leave",
                    "Have a good day Lord Inquisitor!"),
                null, null, 200, null);

            // --- stub replies (fill in M2–M5)
            obj.AddDialogLine("tor_witchhunter_lodge_hunts_todo", "tor_witchhunter_lodge_hunts_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.hunts_todo",
                    "Return soon — I will have hunts to assign."),
                null, null, 200);

            obj.AddDialogLine("tor_witchhunter_lodge_bounties_todo", "tor_witchhunter_lodge_bounties_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.bounties_todo",
                    "Nothing new in the ledger just yet. Return in time."),
                null, null, 200);

            // --- turn-ins dispatcher
            // Branch at the stub state: if the player has anything qualifying, drop into
            // the sub-menu; otherwise reply "nothing to burn" and return to the hub.
            obj.AddDialogLine("tor_witchhunter_lodge_turnins_open", "tor_witchhunter_lodge_turnins_stub", "tor_witchhunter_lodge_turnins_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_open",
                    "Show me what you have brought."),
                WitchHunterTurnInHelper.AnyTurnInAvailable, null, 200);

            obj.AddDialogLine("tor_witchhunter_lodge_turnins_nothing", "tor_witchhunter_lodge_turnins_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_todo",
                    "You bring nothing worth burning. Return when you have."),
                () => !WitchHunterTurnInHelper.AnyTurnInAvailable(), null, 200);

            // Chaos — bulk per-head
            obj.AddPlayerLine("tor_witchhunter_lodge_turnins_chaos", "tor_witchhunter_lodge_turnins_menu", "tor_witchhunter_lodge_turnins_chaos_done",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_chaos",
                    "I bring {WHL_CHAOS_COUNT} cultists and heretics for your judgement."),
                WitchHunterTurnInHelper.UpdateChaosLine, WitchHunterTurnInHelper.TurnInChaos, 200, null);
            obj.AddDialogLine("tor_witchhunter_lodge_turnins_chaos_reply", "tor_witchhunter_lodge_turnins_chaos_done", "tor_witchhunter_lodge_turnins_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_chaos_reply",
                    "Good. The purifying flames await them."),
                null, null, 200);

            // Undead — bulk per-head
            obj.AddPlayerLine("tor_witchhunter_lodge_turnins_undead", "tor_witchhunter_lodge_turnins_menu", "tor_witchhunter_lodge_turnins_undead_done",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_undead",
                    "I bring {WHL_UNDEAD_COUNT} undead vermin for cleansing."),
                WitchHunterTurnInHelper.UpdateUndeadLine, WitchHunterTurnInHelper.TurnInUndead, 200, null);
            obj.AddDialogLine("tor_witchhunter_lodge_turnins_undead_reply", "tor_witchhunter_lodge_turnins_undead_done", "tor_witchhunter_lodge_turnins_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_undead_reply",
                    "Sigmar's holy fire shall consume them. Sword and flame, as it must be."),
                null, null, 200);

            // Hero prisoners — close dialog and open a selection popup; execution + rewards
            // apply per-hero. Running the popup from the dialog consequence lets the player
            // pick a subset rather than committing a bulk action in a single line.
            obj.AddPlayerLine("tor_witchhunter_lodge_turnins_heroes", "tor_witchhunter_lodge_turnins_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_heroes",
                    "I have {WHL_HERO_COUNT} enemies of the faith to put on trial."),
                WitchHunterTurnInHelper.UpdateHeroesLine, WitchHunterTurnInHelper.OpenHereticHeroSelection, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_turnins_done", "tor_witchhunter_lodge_turnins_menu", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_done",
                    "That is all for now."),
                null, null, 200, null);

            // --- store (flat roster: witch-hunter kit from the three canonical templates,
            // plus musket-ball + silver-bullet ammo). Opens as trade so the player can both
            // buy and sell gear in one screen.
            obj.AddDialogLine("tor_witchhunter_lodge_store_open", "tor_witchhunter_lodge_store_stub", "tor_witchhunter_lodge_store_reply",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.store_open",
                    "The Order's wares. These will serve you better than tavern-forged scrap."),
                null, OpenInquisitionStore, 200);
            obj.AddDialogLine("tor_witchhunter_lodge_store_reply", "tor_witchhunter_lodge_store_reply", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.store_reply",
                    "What else can I do for you?"),
                null, null, 200);
        }

        private static bool IsInquisitorPartner()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject == null) return false;
            if (!partner.HeroObject.IsBountyMaster()) return false;

            // Scope strictly to the lodge location — the same hero may also appear in a
            // tavern (Empire towns without a lodge), and the hub must not show there.
            var complex = LocationComplex.Current;
            var loc = complex?.GetLocationOfCharacter(partner.HeroObject);
            return loc != null && loc.StringId == "tor_witchhunter_lodge";
        }

        private static bool InquisitorHasHunt()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            return partner?.HeroObject?.Issue != null;
        }

        // Curated witch-hunter trade roster. Pulled from the three canonical equipment
        // templates (wanderer, lord-companion, qnpc) plus silver-bullet ammo as the
        // thematic chaser. Flat tier — no prestige/career gates.
        private static readonly string[] InquisitionStoreItemIds = new[]
        {
            // Armor — hats cycle three tiers from basic to companion-grade.
            "tor_empire_head_hat_witch_hunter_001",
            "tor_empire_head_hat_witch_hunter_002",
            "tor_empire_head_hat_witch_hunter_003",
            "tor_empire_body_armor_witch_hunter_001",
            "tor_empire_shoulder_pauldron_witch_hunter_001",
            "tor_empire_arm_gauntlet_witch_hunter_001",
            "tor_empire_leg_boots_witch_hunter_001",
            // Weapons — rapier for silverware and flintlocks for the heretic.
            "tor_empire_weapon_rapier_001",
            "tor_empire_weapon_rapier_002",
            "tor_empire_weapon_gun_handgun_003",
            "tor_empire_weapon_gun_flintlock_pistol_007",
            "tor_empire_weapon_gun_flintlock_pistol_008",
            // Ammo — includes the silver shot the Order is known for.
            "tor_neutral_weapon_ammo_musket_ball",
            "tor_neutral_weapon_ammo_silver_bullet",
        };

        private static void OpenInquisitionStore()
        {
            var roster = new ItemRoster();
            foreach (var id in InquisitionStoreItemIds)
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (item == null) continue;
                roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 4)));
            }
            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }
    }
}
