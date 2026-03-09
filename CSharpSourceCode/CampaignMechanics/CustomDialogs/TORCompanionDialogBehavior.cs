using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomDialogs
{
    /// <summary>
    /// Campaign behavior for customizing companion recruitment dialogs.
    /// Removes vanilla companion hire dialogs and replaces them with TOR-specific versions.
    /// </summary>
    public class TORCompanionDialogBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
        }

        private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            // Remove vanilla player dialog options (AddPlayerLine doesn't support priority override)
            // NPC dialog lines (AddDialogLine) can be overridden with priority, so we use vanilla IDs with priority 200
            RemovePlayerLinesForCompanionHiring();
            // Add TOR custom companion dialogs
            AddTORCompanionDialogs(campaignGameStarter);
        }

        /// <summary>
        /// Removes vanilla player dialog lines for companion hiring.
        /// Player lines cannot be overridden with priority like NPC dialog lines can.
        /// Based on LordConversationsCampaignBehavior lines 707-733.
        /// </summary>
        private void RemovePlayerLinesForCompanionHiring()
        {
            var manager = Campaign.Current?.ConversationManager;
            if (manager == null) return;

            int count = 0;

            // Vanilla companion hire player dialog IDs
            var playerDialogIds = new[]
            {
                "main_option_faction_hire",           // Line 707: Player option "I can use someone like you in my company."
                "companion_hire_capacity_full",       // Line 731: Player response when companion limit reached
                "player_companion_hire_response_1",   // Line 732: Player accepts and pays
                "player_companion_hire_response_2"    // Line 733: Player can't afford
            };

            foreach (var id in playerDialogIds)
            {
                count += manager.RemoveDialogLineById(id);
            }

            TORCommon.Log($"Removed {count} vanilla companion player dialog line(s)", NLog.LogLevel.Info);
        }

        /// <summary>
        /// Adds TOR-specific companion recruitment dialogs.
        /// Uses character-specific strings based on companion StringId.
        /// String IDs format: tor_introduction.{characterStringId}, tor_hire_companion_p.{characterStringId}, tor_leave_p.{characterStringId}
        /// </summary>
        private void AddTORCompanionDialogs(CampaignGameStarter campaignGameStarter)
        {
            // Character-specific introduction (NPC greeting at "start")
            // Uses tor_introduction.{characterStringId}
            // SAME ID as vanilla "start_wanderer_unmet" with priority 200 to override (vanilla uses 110)
            campaignGameStarter.AddDialogLine("start_wanderer_unmet", "start", "hero_main_options",
                "{=!}{TOR_INTRO_TEXT}",
                TORWandererIntroductionCondition, null, 200, null);

            // Player option to hire companion (character-specific)
            // Uses tor_hire_companion_p.{characterStringId}
            // SAME ID as vanilla "main_option_faction_hire" with priority 200 to override (vanilla uses ~100)
            campaignGameStarter.AddPlayerLine("main_option_faction_hire", "hero_main_options", "companion_hire",
                "{=!}{TOR_HIRE_TEXT}",
                TORCompanionHireWithTextCondition, null, 200, TORCompanionHireClickable, null);

            // NPC response to hire request
            // SAME ID as vanilla "companion_hire" with priority 200 to override
            campaignGameStarter.AddDialogLine("companion_hire", "companion_hire", "player_companion_hire_response",
                TORTextHelper.GetText("tor_companion_hire_response", "I could be persuaded to join you. My fee is {GOLD_AMOUNT}{GOLD_ICON}."),
                TORCompanionHireGoldCondition, null, 200, null);

            // Player can't afford (capacity check now in clickable condition)
            // SAME ID as vanilla "player_companion_hire_response_2" with priority 200 to override
            campaignGameStarter.AddPlayerLine("player_companion_hire_response_2", "player_companion_hire_response", "hero_main_options",
                TORTextHelper.GetText("tor_companion_hire_cant_afford", "I don't have that much gold right now."),
                () => !TORCompanionCanAffordCondition(), null, 200, null, null);

            // Player accepts gold price
            // SAME ID as vanilla "player_companion_hire_response_1" with priority 200 to override
            campaignGameStarter.AddPlayerLine("player_companion_hire_response_1", "player_companion_hire_response", "hero_leave",
                TORTextHelper.GetText("tor_companion_hire_accept", "Here's your gold. Welcome aboard."),
                TORCompanionCanAffordCondition, TORCompanionHireConsequence, 200, null, null);

            // Wanderer leave dialog (when player leaves without hiring - overrides vanilla with priority 200)
            campaignGameStarter.AddDialogLine("tor_wanderer_leave", "hero_leave", "close_window",
                TORTextHelper.GetText("tor_wanderer_leave", "Safe travels, stranger."),
                TORWandererLeaveCondition, null, 200, null);
        }

        #region Dialog Conditions and Consequences

        private bool TORWandererCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            return hero != null && hero.IsWanderer;
        }

        private bool TORWandererIntroductionCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsWanderer) return false;

            SetIntroductionText();
            return true;
        }

        private bool TORCompanionHireWithTextCondition()
        {
            if (!TORCompanionHireCondition()) return false;

            SetHireText();
            return true;
        }

        private void SetIntroductionText()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return;

            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return;

            var introText = TORTextHelper.GetTextObject("tor_introduction", characterId, "What can I do for you?", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_INTRO_TEXT", introText);
        }

        private void SetHireText()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return;

            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return;

            var hireText = TORTextHelper.GetTextObject("tor_hire_companion_p", characterId, "I am interested in your skills.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_HIRE_TEXT", hireText);
        }

        private void SetLeaveText()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return;

            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return;

            var leaveText = TORTextHelper.GetTextObject("tor_leave_p", characterId, "Nevermind.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_LEAVE_TEXT", leaveText);
        }

        private bool TORCompanionHireCondition()
        {
            var hero = Hero.OneToOneConversationHero;

            if (hero == null) return false;
            if (!hero.IsWanderer) return false;
            if (hero.CompanionOf != null) return false;
            if (Clan.PlayerClan.CompanionLimit <= Clan.PlayerClan.Companions.Count) return false;

            return true;
        }

        private bool TORCompanionHireClickable(out TextObject explanation)
        {
            explanation = TextObject.GetEmpty();

            var hero = Hero.OneToOneConversationHero;

            // Check companion limit
            if (Clan.PlayerClan.CompanionLimit <= Clan.PlayerClan.Companions.Count)
            {
                explanation = new TextObject("{=tor_companion_limit}You have reached your companion limit.");
                return false;
            }

            // Add TOR-specific checks here
            // For example: culture compatibility, career requirements, etc.

            return true;
        }

        private bool TORCompanionHireGoldCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return false;

            int hirePrice = CalculateTORHirePrice(hero);
            MBTextManager.SetTextVariable("GOLD_AMOUNT", hirePrice);

            return true;
        }

        private bool TORCompanionCanAffordCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return false;

            int hirePrice = CalculateTORHirePrice(hero);
            return Hero.MainHero.Gold >= hirePrice;
        }

        private void TORCompanionHireConsequence()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return;

            int hirePrice = CalculateTORHirePrice(hero);

            // Deduct gold
            Hero.MainHero.ChangeHeroGold(-hirePrice);

            // Add companion to clan
            hero.CompanionOf = Clan.PlayerClan;

            // Add to party
            MobileParty.MainParty.MemberRoster.AddToCounts(hero.CharacterObject, 1);

            TORCommon.Log($"TORCompanionDialogBehavior: Hired companion {hero.Name} for {hirePrice} gold", NLog.LogLevel.Info);

            // Add any TOR-specific initialization here
            // For example: apply special traits, adjust equipment, etc.
        }

        private int CalculateTORHirePrice(Hero hero)
        {
            // Base vanilla calculation or custom TOR logic
            int basePrice = 500; // Default base price

            // Adjust based on hero level
            basePrice += hero.Level * 50;

            // Add TOR-specific modifiers here
            // For example: culture bonuses, reputation effects, etc.

            return basePrice;
        }

        private bool TORWandererLeaveCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            return hero != null && hero.IsWanderer && !hero.IsPlayerCompanion;
        }

        #endregion

        public override void SyncData(IDataStore dataStore)
        {
            // No data to sync
        }
    }
}
