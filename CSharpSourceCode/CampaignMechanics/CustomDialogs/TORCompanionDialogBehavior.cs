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
            // Remove vanilla companion hire dialogs after base game adds them
            RemoveVanillaCompanionDialogs();
            // Add TOR custom companion dialogs
            AddTORCompanionDialogs(campaignGameStarter);
        }

        /// <summary>
        /// Removes vanilla companion hire dialog lines by ID.
        /// </summary>
        private void RemoveVanillaCompanionDialogs()
        {
            int removed = TORDialogHelper.RemoveVanillaCompanionDialogs();
            TORCommon.Log($"Removed {removed} vanilla companion dialog line(s)", NLog.LogLevel.Info);
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
            campaignGameStarter.AddDialogLine("tor_wanderer_introduction", "start", "hero_main_options",
                "{=!}{TOR_INTRO_TEXT}",
                TORWandererIntroductionCondition, null, 110, null);

            // Player option to hire companion (character-specific)
            // Uses tor_hire_companion_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_companion_hire_option", "hero_main_options", "tor_companion_hire_start",
                "{=!}{TOR_HIRE_TEXT}",
                TORCompanionHireWithTextCondition, null, 100, TORCompanionHireClickable, null);

            // Player option to leave conversation (character-specific)
            // Uses tor_leave_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_companion_leave", "hero_main_options", "close_window",
                "{=!}{TOR_LEAVE_TEXT}",
                TORWandererLeaveCondition, null, 85, null, null);

            // NPC response to hire request
            campaignGameStarter.AddDialogLine("tor_companion_hire_response", "tor_companion_hire_start", "tor_companion_hire_gold",
                TORTextHelper.GetText("tor_companion_hire_response", "I could be persuaded to join you. My fee is {GOLD_AMOUNT}{GOLD_ICON}."),
                TORCompanionHireGoldCondition, null, 100, null);

            // Player accepts gold price
            campaignGameStarter.AddPlayerLine("tor_companion_hire_accept", "tor_companion_hire_gold", "close_window",
                TORTextHelper.GetText("tor_companion_hire_accept", "Here's your gold. Welcome aboard."),
                TORCompanionCanAffordCondition, TORCompanionHireConsequence, 100, null, null);

            // Player can't afford
            campaignGameStarter.AddPlayerLine("tor_companion_hire_cant_afford", "tor_companion_hire_gold", "hero_main_options",
                TORTextHelper.GetText("tor_companion_hire_cant_afford", "I don't have that much gold right now."),
                () => !TORCompanionCanAffordCondition(), null, 100, null, null);

            // Player declines
            campaignGameStarter.AddPlayerLine("tor_companion_hire_decline", "tor_companion_hire_gold", "hero_main_options",
                TORTextHelper.GetText("tor_companion_hire_decline", "That's too steep for me. Never mind."),
                null, null, 90, null, null);
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

        private bool TORWandererLeaveCondition()
        {
            if (!TORWandererCondition()) return false;

            SetLeaveText();
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

        #endregion

        public override void SyncData(IDataStore dataStore)
        {
            // No data to sync
        }
    }
}
