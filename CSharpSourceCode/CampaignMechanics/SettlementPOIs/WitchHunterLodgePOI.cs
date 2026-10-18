using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
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
            obj.AddPlayerLine("tor_witchhunter_lodge_hunts", "tor_witchhunter_lodge_hub", "tor_witchhunter_lodge_hunts_stub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.player_hunts",
                    "Inquisitor, I wish to aid the Empire! Do you have anything special that needs doing?"),
                null, null, 200, null);

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

            obj.AddDialogLine("tor_witchhunter_lodge_turnins_todo", "tor_witchhunter_lodge_turnins_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.turnins_todo",
                    "Not yet — the Inquisition's clerks are still cataloguing."),
                null, null, 200);

            obj.AddDialogLine("tor_witchhunter_lodge_store_todo", "tor_witchhunter_lodge_store_stub", "tor_witchhunter_lodge_hub",
                TORTextHelper.GetTextForNative("tor_poi_witchhunter_lodge.store_todo",
                    "Our armaments are being inventoried. Return shortly."),
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
    }
}
