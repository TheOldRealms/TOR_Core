using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
        /// Removes vanilla player dialog lines at hero_main_options.
        /// Player lines cannot be hidden by conditions alone - both vanilla and TOR lines would show.
        /// Our entire dialog chain uses TOR-specific tokens, so only need removal at hero_main_options hub.
        /// Based on LordConversationsCampaignBehavior line 707.
        /// </summary>
        private void RemovePlayerLinesForCompanionHiring()
        {
            var manager = Campaign.Current?.ConversationManager;
            if (manager == null) return;

            int count = 0;

            // Only remove the vanilla hire option at hero_main_options
            // Our complete dialog flow uses tor_* tokens, so no vanilla conflicts elsewhere
            var playerDialogIds = new[]
            {
                "main_option_faction_hire"           // Line 707: Player option "I can use someone like you in my company."
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
            campaignGameStarter.AddDialogLine("tor_wanderer_race_incompatible", "start", "close_window",
                "{=!}{TOR_BEGONE_TEXT}",
                ()=>TORWandererMeetCondition() && TORWandererRaceIncompatibleCondition(), null, 220, null);
            // === WANDERER INTRODUCTION FLOW ===
            // Character-specific introduction (NPC greeting at "start")
            // SAME ID as vanilla "start_wanderer_unmet" with priority 200 to override (vanilla uses 110)
            // Uses tor_introduction.{characterStringId}
            // Output to TOR-specific token to avoid vanilla player line duplication
            campaignGameStarter.AddDialogLine("start_wanderer_unmet", "start", "tor_wanderer_meet_player_response",
                "{=!}{TOR_INTRO_TEXT}",
                TORWandererMeetCondition, null, 200, null);

            // Race/faction incompatibility check - NPC rejects player early if incompatible
            // Uses character-specific tor_begone.{characterStringId}


            // Player response options - using TOR tokens for completely separate dialog chain
            // Uses character-specific tor_player_tell_me_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_wanderer_meet_player_response1", "tor_wanderer_meet_player_response", "tor_wanderer_preintroduction",
                "{=!}{TOR_PLAYER_TELL_ME_TEXT}",
                TORWandererPlayerResponseCondition, null, 100, null, null);

            // Skip intro option - goes to backstory D (final part with job offer)
            // Uses character-specific tor_player_skip_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_wanderer_meet_player_response2", "tor_wanderer_meet_player_response", "tor_wanderer_introduction_d",
                "{=!}{TOR_PLAYER_SKIP_TEXT}",
                TORWandererPlayerResponseCondition, null, 100, null, null);
            
            campaignGameStarter.AddDialogLine("tor_wanderer_prebackstory", "tor_wanderer_preintroduction", "tor_wanderer_introduction_a",
                "{=!}{WANDERER_PREBACKSTORY}",
                TORWandererPrebackstoryCondition, null, 100, null);

            // Backstory A
            campaignGameStarter.AddDialogLine("tor_wanderer_introduction_a", "tor_wanderer_introduction_a", "tor_wanderer_introduction_b",
                "{=!}{WANDERER_BACKSTORY_A}",
                TORWandererBackstoryCondition, null, 100, null);

            // Backstory B
            campaignGameStarter.AddDialogLine("tor_wanderer_introduction_b", "tor_wanderer_introduction_b", "tor_wanderer_introduction_c",
                "{=!}{WANDERER_BACKSTORY_B}",
                null, null, 100, null);

            // Backstory C
            campaignGameStarter.AddDialogLine("tor_wanderer_introduction_c", "tor_wanderer_introduction_c", "tor_wanderer_player_reaction",
                "{=!}{WANDERER_BACKSTORY_C}",
                null, null, 100, null);

            // Player reactions to backstory
            campaignGameStarter.AddPlayerLine("tor_wanderer_player_response1_2", "tor_wanderer_player_reaction", "tor_wanderer_introduction_d",
                "{=!}{BACKSTORY_RESPONSE_1}",
                null, null, 100, null, null);

            campaignGameStarter.AddPlayerLine("tor_wanderer_player_response2_2", "tor_wanderer_player_reaction", "tor_wanderer_introduction_d",
                "{=!}{BACKSTORY_RESPONSE_2}",
                null, null, 100, null, null);

            // Backstory D (final part)
            campaignGameStarter.AddDialogLine("tor_wanderer_introduction_d", "tor_wanderer_introduction_d", "tor_companion_hire_options",
                "{=!}{WANDERER_BACKSTORY_D}",
                null, null, 100, null);


            // Player option to hire companion
            campaignGameStarter.AddPlayerLine("tor_companion_hire_option", "tor_companion_hire_options", "tor_companion_hire",
                "{=!}{TOR_HIRE_TEXT}",
                TORCompanionHireWithTextCondition, null, 100, TORCompanionHireClickable, null);

            // Player option to decline and leave
            // Uses character-specific tor_companion_leave_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_companion_leave_option", "tor_companion_hire_options", "close_window",
                "{=!}{TOR_COMPANION_LEAVE_TEXT}",
                TORCompanionLeaveWithTextCondition, null, 100, null, null);

            // NPC response to hire request (TOR-specific token to avoid vanilla conflicts)
            // Uses character-specific tor_hire_companion_payment.{characterStringId}
            campaignGameStarter.AddDialogLine("tor_companion_hire", "tor_companion_hire", "tor_player_companion_hire_response",
                "{=!}{TOR_HIRE_PAYMENT_TEXT}",
                TORCompanionHireGoldCondition, null, 100, null);

            // Player accepts gold price (mirrors vanilla player_companion_hire_response_1)
            // Uses character-specific tor_hire_accept_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_player_companion_hire_response_1", "tor_player_companion_hire_response", "close_window",
                "{=!}{TOR_HIRE_ACCEPT_TEXT}",
                TORCompanionHireOnCondition, TORCompanionHireOnConsequence, 100, null, null);

            // Player can't afford (mirrors vanilla player_companion_hire_response_2)
            // Uses character-specific tor_hire_decline_p.{characterStringId}
            campaignGameStarter.AddPlayerLine("tor_player_companion_hire_response_2", "tor_player_companion_hire_response", "lord_pretalk",
                "{=!}{TOR_HIRE_DECLINE_TEXT}",
                TORCompanionHireOnCondition, null, 100, null, null);

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

        private bool TORWandererMeetCondition()
        {
            if (!TORWandererCondition()) return false;
            if (Hero.OneToOneConversationHero.HeroState == Hero.CharacterStates.Prisoner) return false;

            // Set TOR-specific introduction text
            var hero = Hero.OneToOneConversationHero;
            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            var introText = TORTextHelper.GetTextObject("tor_hire_companion_introduction", characterId, "What can I do for you?", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_INTRO_TEXT", introText);

            return true;
        }

        /// <summary>
        /// Checks if player and wanderer are racially/factionally incompatible.
        /// Returns true if incompatible (triggers rejection dialog).
        /// </summary>
        private bool TORWandererRaceIncompatibleCondition()
        {
            if (!TORWandererCondition()) return false;

            var hero = Hero.OneToOneConversationHero;
            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            // Check if player can hire this wanderer based on race using the model
            var model = Campaign.Current.Models.GetCompanionHiringCompatibilityModel();
            if (model == null) return false; // Model not available, allow hiring

            bool canHire = model.CanPlayerHireWanderer(Hero.MainHero, hero);

            if (!canHire)
            {
                // Set rejection text
                var begoneText = TORTextHelper.GetTextObject("tor_hire_companion_begone", characterId,
                    "Begone. I have no business with your kind.", skipValidation: true);
                MBTextManager.SetTextVariable("TOR_BEGONE_TEXT", begoneText);
                return true;
            }

            return false;
        }

        private bool TORWandererPlayerResponseCondition()
        {
            if (!TORWandererCondition()) return false;

            var hero = Hero.OneToOneConversationHero;
            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            // Set player response text variables
            var tellMeText = TORTextHelper.GetTextObject("tor_hire_companion_tell_me_p", characterId,
                "My name is {PLAYER.NAME}, {?CONVERSATION_NPC.GENDER}madam{?}sir{\\?}. Tell me about yourself.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_PLAYER_TELL_ME_TEXT", tellMeText.ToString());

            var skipText = TORTextHelper.GetTextObject("tor_hire_companion_skip_p", characterId,
                "I'm {PLAYER.NAME}. Let's skip the pleasantries and get right to business.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_PLAYER_SKIP_TEXT", skipText.ToString());

            return true;
        }

        private bool TORWandererPrebackstoryCondition()
        {
            // Only show prebackstory when NOT in tavern

                var hero = Hero.OneToOneConversationHero;
                var characterId = hero?.Template?.StringId;

                // Use TOR-specific prebackstory text if available, otherwise generic
                var prebackstoryText = !string.IsNullOrEmpty(characterId)
                    ? TORTextHelper.GetTextObject("tor_prebackstory", characterId, "", skipValidation: true)
                    : TextObject.GetEmpty();

                // Fallback to vanilla generic if no TOR text
                if (string.IsNullOrEmpty(prebackstoryText?.ToString()))
                {
                    prebackstoryText = GameTexts.FindText("spc_prebackstory_generic");
                }

                MBTextManager.SetTextVariable("WANDERER_PREBACKSTORY", prebackstoryText);
                
            return true;
        }

        private bool TORWandererBackstoryCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsWanderer) return false;

            StringHelpers.SetCharacterProperties("CONVERSATION_CHARACTER", hero.CharacterObject, null);
            string characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            // Set TOR-specific backstory text variables
            MBTextManager.SetTextVariable("IMPERIALCAPITAL", new TextObject("Altdorf"));
            MBTextManager.SetTextVariable("WANDERER_BACKSTORY_A", TORTextHelper.GetTextObject("tor_backstory_a", characterId, "Backstory A", skipValidation: true));
            MBTextManager.SetTextVariable("WANDERER_BACKSTORY_B", TORTextHelper.GetTextObject("tor_backstory_b", characterId, "Backstory B", skipValidation: true));
            MBTextManager.SetTextVariable("WANDERER_BACKSTORY_C", TORTextHelper.GetTextObject("tor_backstory_c", characterId, "Backstory C", skipValidation: true));
            MBTextManager.SetTextVariable("BACKSTORY_RESPONSE_1", TORTextHelper.GetTextObject("tor_response_1", characterId, "Response 1", skipValidation: true));
            MBTextManager.SetTextVariable("BACKSTORY_RESPONSE_2", TORTextHelper.GetTextObject("tor_response_2", characterId, "Response 2", skipValidation: true));
            MBTextManager.SetTextVariable("WANDERER_BACKSTORY_D", TORTextHelper.GetTextObject("tor_backstory_d", characterId, "Backstory D", skipValidation: true));
            StringHelpers.SetCharacterProperties("MET_WANDERER", hero.CharacterObject, null);

            return true;
        }

        private bool TORCompanionHireWithTextCondition()
        {
            if (!TORCompanionHireCondition()) return false;

            // Set hire text variable
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return false;

            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            var hireText = TORTextHelper.GetTextObject("tor_hire_companion_p", characterId, "I am interested in your skills.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_HIRE_TEXT", hireText);

            return true;
        }

        private bool TORCompanionLeaveWithTextCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsWanderer) return false;

            var characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            var leaveText = TORTextHelper.GetTextObject("tor_companion_leave_p", characterId,
                "Farewell for now.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_COMPANION_LEAVE_TEXT", leaveText);

            return true;
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

        /// <summary>
        /// Sets the hire price text variable for display in NPC response.
        /// Uses character-specific tor_hire_companion_payment.{characterId} text.
        /// Uses vanilla CompanionHiringPriceCalculationModel.
        /// </summary>
        private bool TORCompanionHireGoldCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return false;

            int hirePrice = Campaign.Current.Models.CompanionHiringPriceCalculationModel.GetCompanionHiringPrice(hero);
            MBTextManager.SetTextVariable("GOLD_AMOUNT", hirePrice);

            // Set character-specific hire payment text
            string characterId = hero.Template?.StringId;
            if (string.IsNullOrEmpty(characterId)) return false;

            var paymentText = TORTextHelper.GetTextObject("tor_hire_companion_payment", characterId,
                "I could be persuaded to join you. My fee is {GOLD_AMOUNT}{GOLD_ICON}.", skipValidation: true);
            MBTextManager.SetTextVariable("TOR_HIRE_PAYMENT_TEXT", paymentText);

            return true;
        }

        /// <summary>
        /// Mirrors vanilla conversation_companion_hire_on_condition.
        /// Checks if player can afford companion AND sets GOLD_AMOUNT variable for display.
        /// Sets character-specific player response text variables.
        /// </summary>
        private bool TORCompanionHireOnCondition()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return false;

            int hirePrice = Campaign.Current.Models.CompanionHiringPriceCalculationModel.GetCompanionHiringPrice(hero);

            // Set GOLD_AMOUNT variable for display in dialog text
            GameTexts.SetVariable("STR1", hirePrice);
            GameTexts.SetVariable("STR2", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            MBTextManager.SetTextVariable("GOLD_AMOUNT", GameTexts.FindText("str_STR1_STR2"));

            // Set character-specific player response text variables
            string characterId = hero.Template?.StringId;
            if (!string.IsNullOrEmpty(characterId))
            {
                var acceptText = TORTextHelper.GetTextObject("tor_hire_accept_p", characterId,
                    "Right... {GOLD_AMOUNT} Here you are.", skipValidation: true);
                MBTextManager.SetTextVariable("TOR_HIRE_ACCEPT_TEXT", acceptText);

                var declineText = TORTextHelper.GetTextObject("tor_hire_decline_p", characterId,
                    "I can't afford that just now.", skipValidation: true);
                MBTextManager.SetTextVariable("TOR_HIRE_DECLINE_TEXT", declineText);
            }

            // Check if player can afford AND hasn't reached companion limit
            bool tooManyCompanions = Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit;
            return Hero.MainHero.Gold >= hirePrice && !tooManyCompanions;
        }

        /// <summary>
        /// Mirrors vanilla conversation_companion_hire_on_consequence.
        /// Uses vanilla actions to properly hire the companion.
        /// </summary>
        private void TORCompanionHireOnConsequence()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero == null) return;

            int hirePrice = Campaign.Current.Models.CompanionHiringPriceCalculationModel.GetCompanionHiringPrice(hero);

            // Use vanilla actions (mirrors vanilla exactly)
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, hirePrice);
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);

            TORCommon.Log($"TORCompanionDialogBehavior: Hired companion {hero.Name} for {hirePrice} gold", NLog.LogLevel.Info);

            // Add any TOR-specific initialization here
            // For example: apply special traits, adjust equipment, etc.
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
