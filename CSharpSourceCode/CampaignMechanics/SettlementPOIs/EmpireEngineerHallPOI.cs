using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class EmpireEngineerHallPOI : SettlementPOI, ISkillTrainerHost
    {
        public override string LocationId => "tor_empire_engineer_hall";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_empire_engineer_hall.EnterLabel";
        public override string EnterLabelDefault => "Visit the Empire Engineer Hall";

        public string SkillTrainerTemplateId => "tor_nulnengineernpc_empire";

        public override void EnsurePlaced(Settlement s) => PlaceResident(s, SkillTrainerTemplateId);

        public override string MapIconText => "<img src=\"cannon_icon\" extend=\"15\">";

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
                denialReason = R("SiegeSealed", "The gates of the Engineer Hall are sealed during the siege.");
                return false;
            }
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
            {
                denialReason = R("Rejected", "The engineers turn you away.");
                return false;
            }
            if (h.Culture.StringId != TORConstants.Cultures.EMPIRE)
            {
                denialReason = R("WrongCulture", "The Imperial engineers only serve those of the Empire.");
                return false;
            }

            denialReason = null;
            return true;
        }

        /// <summary>
        /// Master Engineer hub dialog. Was previously the ~450-line quest god-object in
        /// <c>MasterEngineerTownBehavior</c>; the RunawayParts / rogue-engineer quest was
        /// retired entirely (plan: TORTasks/town-pois-and-access-model/engineer-quest-removal.md).
        /// What remains is the pure hub: buy guns, upgrade the shop tier (Prestige-gated),
        /// recruit novice engineers, and two tutorial lines.
        /// </summary>
        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            MBTextManager.SetTextVariable("PRESTIGE_ICON",
                CustomResourceManager.GetResourceObject("Prestige").GetCustomResourceIconAsText());

            // --- greeting / hub entry
            obj.AddDialogLine("empire_engineer_greet", "start", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_hub_greet", "Welcome. What do you need?"),
                IsMasterEngineerPartner, null, 200, null);

            // --- hub player lines
            obj.AddPlayerLine("empire_engineer_buy_cannons", "hub", "opengunshop",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hub_player_cannons", "I would like to buy some equipment."),
                null, null, 200, null);
            obj.AddPlayerLine("empire_engineer_upgrade", "hub", "upgradeshop",
                TORTextHelper.GetTextForNative("tor_engineer_hub_upgrade", "I wish to upgrade my equipment."),
                () => !HasUpgradeTier(3), null, 200, null);
            obj.AddPlayerLine("empire_engineer_recruit", "hub", "recruitengineer",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hub_player_engineers", "I would like to recruit some engineers."),
                null, null, 200, null);
            obj.AddPlayerLine("empire_engineer_tutorial_buy", "hub", "tutorialcannonbuy",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hub_player_cannons_limit_instruction", "How can I buy more cannons?"),
                null, null, 200, null);
            obj.AddPlayerLine("empire_engineer_tutorial_use", "hub", "tutorialcannonuse",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hub_player_cannons_instruction", "How can I use cannons?"),
                null, null, 200, null);
            obj.AddPlayerLine("empire_engineer_leave", "hub", "close_window",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hub_player_leave", "Nothing at the moment, I must leave."),
                null, null, 200, null);

            // --- gun shop
            obj.AddDialogLine("empire_engineer_open_shop", "opengunshop", "opengunshopandclosedialog",
                TORTextHelper.GetTextForNative("tor_engineer_quest_open_shop", "Of course, you'll find only the best from the forges of Nuln!"),
                null, OpenGunShop, 200);
            obj.AddDialogLine("empire_engineer_close_shop", "opengunshopandclosedialog", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_quest_close_shop", "What else can I do for you?"),
                null, null, 200);

            // --- recruitment
            obj.AddDialogLine("empire_engineer_hire_offer", "recruitengineer", "recruitmentoptions",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hire_engineers", "A pair of our novice engineers are eager to join you for the right price ({RECRUITMENT_PRICE})."),
                UpdateRecruitmentPriceText, null, 200);
            obj.AddPlayerLine("empire_engineer_hire_accept", "recruitmentoptions", "opengunshopandclosedialog",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hire_engineers_accept", "Welcome on board."),
                HasEnoughGoldForRecruit, RecruitEngineers, 200);
            obj.AddPlayerLine("empire_engineer_hire_poor", "recruitmentoptions", "opengunshopandclosedialog",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hire_engineers_not_enough_money", "I do not have the funds for them right now."),
                () => !HasEnoughGoldForRecruit(), null, 200);
            obj.AddPlayerLine("empire_engineer_hire_decline", "recruitmentoptions", "opengunshopandclosedialog",
                TORTextHelper.GetTextForNative("tor_engineer_quest_hire_engineers_decline", "On second thought, maybe later."),
                null, null, 200);

            // --- upgrade shop (3-tier Prestige unlock)
            obj.AddDialogLine("empire_engineer_upgrade_1", "upgradeshop", "upgrade_tier1_response",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_1", "For 500{PRESTIGE_ICON} I can finally stock up our buckshot supplies and continue creating blunderbusses and replenish our hochland long rifle stocks."),
                () => !HasUpgradeTier(1), null, 200);
            obj.AddDialogLine("empire_engineer_upgrade_2", "upgradeshop", "upgrade_tier2_response",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_2", "For another 500{PRESTIGE_ICON} I can buy parts from which I can create more advanced pistols and rifles. Meinkhopt would be proud of me."),
                () => !HasUpgradeTier(2) && HasUpgradeTier(1), null, 200);
            obj.AddDialogLine("empire_engineer_upgrade_3", "upgradeshop", "upgrade_tier3_response",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_3", "For a final 500{PRESTIGE_ICON} I can finally stock up our gunpowder laboratory, which gives me the opportunity to procure grenades and cannons."),
                () => !HasUpgradeTier(3) && HasUpgradeTier(2), null, 200);

            obj.AddPlayerLine("empire_engineer_upgrade_1_accept", "upgrade_tier1_response", "upgrade_tier1_done",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_agree", "I hope it is worth it. I will support you on this (Spend 500{PRESTIGE_ICON})"),
                HasEnoughPrestige, () => GrantUpgradeTier(1), 200, null);
            obj.AddPlayerLine("empire_engineer_upgrade_1_decline", "upgrade_tier1_response", "upgrade_decline_response",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_decline", "I can't afford such a venture."),
                null, null, 200, null);
            obj.AddDialogLine("empire_engineer_upgrade_1_result", "upgrade_tier1_done", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_1_response", "May these guns help you succeed in combat. Just do not shoot yourself in the foot with them."),
                null, null, 200);

            obj.AddPlayerLine("empire_engineer_upgrade_2_accept", "upgrade_tier2_response", "upgrade_tier2_done",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_agree", "I hope it is worth it. I will support you on this (Spend 500{PRESTIGE_ICON})"),
                HasEnoughPrestige, () => GrantUpgradeTier(2), 200, null);
            obj.AddPlayerLine("empire_engineer_upgrade_2_decline", "upgrade_tier2_response", "upgrade_decline_response",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_decline", "I can't afford such a venture."),
                null, null, 200, null);
            obj.AddDialogLine("empire_engineer_upgrade_2_result", "upgrade_tier2_done", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_2_response", "It is said Meinkhopt checked the clicking sound of every repeater gun he made."),
                null, null, 200);

            obj.AddPlayerLine("empire_engineer_upgrade_3_accept", "upgrade_tier3_response", "upgrade_tier3_done",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_agree", "I hope it is worth it. I will support you on this (Spend 500{PRESTIGE_ICON})"),
                HasEnoughPrestige, () => GrantUpgradeTier(3), 200, null);
            obj.AddPlayerLine("empire_engineer_upgrade_3_decline", "upgrade_tier3_response", "upgrade_decline_response",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_decline", "I can't afford such a venture."),
                null, null, 200, null);
            obj.AddDialogLine("empire_engineer_upgrade_3_result", "upgrade_tier3_done", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_3_response", "I love the smell of burning black powder in the morning."),
                null, null, 200);

            obj.AddDialogLine("empire_engineer_upgrade_decline_result", "upgrade_decline_response", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_upgrade_decline_response", "What a shame. Is there anything else I can do?"),
                null, null, 200);

            // --- tutorials
            obj.AddDialogLine("empire_engineer_tut_buy_0", "tutorialcannonbuy", "tutorialcannonbuy2",
                TORTextHelper.GetTextForNative("tor_engineer_quest_cannons_limit_instruction_0", "To buy cannons you must be an Imperial, that's the law."),
                null, null, 200);
            obj.AddDialogLine("empire_engineer_tut_buy_1", "tutorialcannonbuy2", "tutorialcannonbuy3",
                TORTextHelper.GetTextForNative("tor_engineer_quest_cannons_limit_instruction_1", "The amount of cannons you can field in your army increases every 50 levels in Engineering skill."),
                null, null, 200);
            obj.AddDialogLine("empire_engineer_tut_buy_2", "tutorialcannonbuy3", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_quest_cannons_limit_instruction_2", "If you have met these requirements, simply speak to me and I'll show you what we have."),
                null, null, 200);

            obj.AddDialogLine("empire_engineer_tut_use_0", "tutorialcannonuse", "tutorialcannonuse2",
                TORTextHelper.GetTextForNative("tor_engineer_quest_cannons_use_0", "Cannons are placed using the spellcasting Mode, but to fire the cannons you will need to hire at least two Cannon Crew"),
                null, null, 200);
            obj.AddDialogLine("empire_engineer_tut_use_1", "tutorialcannonuse2", "tutorialcannonuse3",
                TORTextHelper.GetTextForNative("tor_engineer_quest_cannons_use_1", "You will also need to ensure that the cannon is in your party inventory"),
                null, null, 200);
            obj.AddDialogLine("empire_engineer_tut_use_2", "tutorialcannonuse3", "hub",
                TORTextHelper.GetTextForNative("tor_engineer_quest_cannons_use_2", "Engineers and Cannon Crew can both fire cannons."),
                null, null, 200);
        }

        // ------- local hub helpers (nested so they close over no state)

        private static bool IsMasterEngineerPartner()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            return partner?.HeroObject != null && partner.HeroObject.IsMasterEngineer();
        }

        private static int NoviceEngineerPairPrice()
        {
            var novice = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            return (int)(Campaign.Current.Models.PartyWageModel
                .GetTroopRecruitmentCost(novice, Hero.MainHero, false).ResultNumber * 2 * 10);
        }

        private static bool UpdateRecruitmentPriceText()
        {
            MBTextManager.SetTextVariable("RECRUITMENT_PRICE", NoviceEngineerPairPrice() + "{GOLD_ICON}");
            return true;
        }

        private static bool HasEnoughGoldForRecruit() => Hero.MainHero.Gold > NoviceEngineerPairPrice();

        private static void RecruitEngineers()
        {
            var novice = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            var price = NoviceEngineerPairPrice();
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, price);
            MobileParty.MainParty.MemberRoster.AddToCounts(novice, 2);
        }

        private static bool HasUpgradeTier(int level) =>
            Hero.MainHero.HasAttribute("EngineerUpgrade" + level);

        private static bool HasEnoughPrestige() =>
            Hero.MainHero.GetCustomResourceValue("Prestige") >= 500;

        private static void GrantUpgradeTier(int level)
        {
            var attr = "EngineerUpgrade" + level;
            if (!Hero.MainHero.HasAttribute(attr)) Hero.MainHero.AddAttribute(attr);
            Hero.MainHero.AddCustomResource("Prestige", -500);
        }

        private static void OpenGunShop()
        {
            var tier1 = HasUpgradeTier(1);
            var tier2 = HasUpgradeTier(2);
            var tier3 = HasUpgradeTier(3);

            var allEmpireGuns = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x =>
                x.Culture != null && x.Culture.StringId == "empire" && x.IsTorItem() &&
                !(x.IsCraftedByPlayer || x.HasAnyLootTraits()) &&
                (x.StringId.Contains("gun") || x.StringId.Contains("artillery"))).ToList();

            var filtered = FilterGuns(allEmpireGuns, tier1, tier2);
            var roster = new ItemRoster();
            foreach (var item in filtered)
                roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5)));

            var oldRifle = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_gun_old_rifle");
            if (oldRifle != null) roster.Add(new ItemRosterElement(oldRifle, MBRandom.RandomInt(2, 5)));

            if (tier1)
            {
                var buckshot = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_ammo_musket_ball_scatter");
                if (buckshot != null) roster.Add(new ItemRosterElement(buckshot, MBRandom.RandomInt(2, 5)));
            }
            if (tier2)
            {
                var mortar = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_artillery_mortar_001");
                if (mortar != null) roster.Add(new ItemRosterElement(mortar, 1));
            }
            if (tier3)
            {
                var cannon = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_artillery_cannon_001");
                if (cannon != null) roster.Add(new ItemRosterElement(cannon, 1));
                var grenade = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_weapon_ammo_grenade");
                if (grenade != null) roster.Add(new ItemRosterElement(grenade, MBRandom.RandomInt(2, 5)));
            }

            var ammo = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_ammo_musket_ball");
            if (ammo != null) roster.Add(new ItemRosterElement(ammo, MBRandom.RandomInt(2, 5)));

            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        private static List<ItemObject> FilterGuns(List<ItemObject> items, bool tier1, bool tier2)
        {
            var final = new List<ItemObject>();
            foreach (var item in items)
            {
                if (!tier1 && (item.StringId.Contains("longrifle") || item.StringId.Contains("blunderbuss")
                               || item.StringId.Contains("big_musket") || item.StringId.Contains("special_musket")))
                    continue;
                if (!tier2 && (item.StringId.Contains("tor_empire_weapon_gun_handgun_002")
                               || item.StringId.Contains("repeater")
                               || item.StringId.Contains("flintlock_pistol_007")))
                    continue;
                final.Add(item);
            }
            return final;
        }
    }
}
