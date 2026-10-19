using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.WitchHunter;
using TOR_Core.CharacterDevelopment;
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

        /// <summary>
        /// Template id for the lodge's resident Inquisitor. Historically owned by the now-
        /// retired <c>BountyMasterCampaignBehavior</c>; moving the lifecycle into the POI
        /// framework collapsed that behavior but kept the same hero template so existing
        /// saves adopt their current bounty-master hero into the lodge via
        /// <see cref="SettlementPOIManagerCampaignBehavior.GetOrCreateHero"/>'s orphan scan.
        /// </summary>
        public string InquisitorTemplateId => "tor_bountymaster_empire_0";

        public override void EnsurePlaced(Settlement s) => PlaceResident(s, InquisitorTemplateId);

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
        /// Inquisitor hub dialog. Design doc specifies six branches (plus Leave):
        /// Hunts, Bounties, Turn-ins, Store, Companion Recruitment, Witch Hunter
        /// Exclusive. Hunts and Store are rep-gated behind the <c>INQUISITION_KNOWN</c>
        /// attribute (and store further on <c>INQUISITION_RESPECTED</c> for the full
        /// tier — see T6). Witch Hunter Exclusive is career-gated. Companion
        /// Recruitment is open from Unknown rank per the design.
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

            // --- Hunts: three mutually-exclusive variants with identical player text.
            // Known with an issue → vanilla hero_main_options for the issue brief flow.
            // Known with no issue → "Return soon" stub.
            // Not Known yet → shared "earn trust" stub so the player understands why.
            obj.AddPlayerLine("tor_witchhunter_lodge_hunts", "tor_witchhunter_lodge_hub", "hero_main_options",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                () => IsKnown() && InquisitorHasHunt(), null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_hunts_none", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_hunts_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                () => IsKnown() && !InquisitorHasHunt(), null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_hunts_locked", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_notrust_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                () => !IsKnown(), null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_bounties", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_bounties_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_bounties",
                    "Are there any bounties the Inquisition needs done?"),
                null, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_turnins", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_turnins_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_turnins",
                    "Lord Inquisitor! I have some things you might be interested in."),
                null, null, 200, null);

            // --- Store: gated by rank; T6 adds the tiered roster inside the stub.
            obj.AddPlayerLine("tor_witchhunter_lodge_store", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_store_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_store",
                    "I wish to acquire some Inquisition armaments."),
                IsKnown, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_store_locked", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_notrust_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_store",
                    "I wish to acquire some Inquisition armaments."),
                () => !IsKnown(), null, 200, null);

            // --- Companion Recruitment: open at Unknown rank per design; Phase D fills in.
            obj.AddPlayerLine("tor_witchhunter_lodge_companions", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_companions_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_companions",
                    "I seek a witch hunter willing to join me."),
                null, null, 200, null);

            // --- Witch Hunter Exclusive: career-gated; design doc explicitly defers the mechanic.
            obj.AddPlayerLine("tor_witchhunter_lodge_exclusive", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_exclusive_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_exclusive",
                    "What of the Order's business for one of our own?"),
                IsWitchHunterCareer, null, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_leave", "tor_witchhunter_lodge_hub", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_leave",
                    "Have a good day Lord Inquisitor!"),
                null, null, 200, null);

            // --- stub replies
            obj.AddDialogLine("tor_witchhunter_lodge_hunts_todo", "tor_witchhunter_lodge_hunts_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.hunts_todo",
                    "Return soon — I will have hunts to assign."),
                null, null, 200);

            obj.AddDialogLine("tor_witchhunter_lodge_bounties_todo", "tor_witchhunter_lodge_bounties_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.bounties_todo",
                    "Nothing new in the ledger just yet. Return in time."),
                null, null, 200);

            // Shared "not yet trusted" fallback — Hunts and Store both land here below rank.
            obj.AddDialogLine("tor_witchhunter_lodge_notrust", "tor_witchhunter_lodge_notrust_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.notrust",
                    "Return once you have earned our trust."),
                null, null, 200);

            obj.AddDialogLine("tor_witchhunter_lodge_companions_todo", "tor_witchhunter_lodge_companions_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.companions_todo",
                    "No witch hunters are available for the taking right now. Return in time."),
                null, null, 200);

            obj.AddDialogLine("tor_witchhunter_lodge_exclusive_todo", "tor_witchhunter_lodge_exclusive_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.exclusive_todo",
                    "The Order has nothing for you yet, brother. Return in time."),
                null, null, 200);

            // --- turn-ins dispatcher
            // NPC pivots the player into the sub-menu with a short line. The materials
            // and prisoners sub-flows live in WitchHunterTurnInHelper and each take over
            // the UI entirely (inquiry popup / donate-to-town screen), so both target
            // close_window as their consequence point.
            obj.AddDialogLine("tor_witchhunter_lodge_turnins_open", "tor_witchhunter_lodge_turnins_stub", "tor_witchhunter_lodge_turnins_menu",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_open",
                    "What have you brought the Order?"),
                null, null, 200);

            obj.AddPlayerLine("tor_witchhunter_lodge_turnins_materials", "tor_witchhunter_lodge_turnins_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_materials",
                    "Materials for the pyre — scrolls, warpstone."),
                null, WitchHunterTurnInHelper.OpenMaterialTurnIn, 200, null);

            obj.AddPlayerLine("tor_witchhunter_lodge_turnins_prisoners", "tor_witchhunter_lodge_turnins_menu", "close_window",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_prisoners",
                    "Prisoners for the Order to deal with."),
                null, WitchHunterTurnInHelper.OpenPrisonerTurnIn, 200, null);

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

        private static bool IsKnown() =>
            Hero.MainHero?.HasAttribute(WitchHunterInquisitionBehavior.AttrKnown) == true;

        private static bool IsWitchHunterCareer() =>
            Hero.MainHero != null && Hero.MainHero.HasCareer(TORCareers.WitchHunter);

        // Two-tier roster per the design: basic unlocks at Known (INQUISITION_KNOWN),
        // full set adds in at Respected (INQUISITION_RESPECTED). "Basic" is the
        // lower-end witch hunter kit — plain hat, boots, gloves, rapier mk1, handgun
        // mk1, standard ball. "Full-tier adds" layer in the heavier armor pieces,
        // the companion-grade hat, upgraded rapier + flintlock pistols, and the
        // thematic silver bullets.
        private static readonly string[] StoreTierBasic = new[]
        {
            "tor_empire_head_hat_witch_hunter_001",
            "tor_empire_arm_gauntlet_witch_hunter_001",
            "tor_empire_leg_boots_witch_hunter_001",
            "tor_empire_weapon_rapier_001",
            "tor_empire_weapon_gun_handgun_003",
            "tor_neutral_weapon_ammo_musket_ball",
        };

        private static readonly string[] StoreTierFullAdds = new[]
        {
            "tor_empire_head_hat_witch_hunter_002",
            "tor_empire_head_hat_witch_hunter_003",
            "tor_empire_body_armor_witch_hunter_001",
            "tor_empire_shoulder_pauldron_witch_hunter_001",
            "tor_empire_weapon_rapier_002",
            "tor_empire_weapon_gun_flintlock_pistol_007",
            "tor_empire_weapon_gun_flintlock_pistol_008",
            "tor_neutral_weapon_ammo_silver_bullet",
        };

        private static void OpenInquisitionStore()
        {
            var full = Hero.MainHero?.HasAttribute(WitchHunterInquisitionBehavior.AttrRespected) == true;
            var roster = new ItemRoster();
            foreach (var id in StoreTierBasic)
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (item == null) continue;
                roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 4)));
            }
            if (full)
            {
                foreach (var id in StoreTierFullAdds)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                    if (item == null) continue;
                    roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 4)));
                }
            }
            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }
    }
}
