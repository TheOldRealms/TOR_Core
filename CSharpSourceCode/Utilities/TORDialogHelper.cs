using System;
using TaleWorlds.CampaignSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Utilities
{
    /// <summary>
    /// Helper for common dialog removal patterns.
    /// </summary>
    public static class TORDialogHelper
    {
        /// <summary>
        /// Removes vanilla companion recruitment dialogs by ID.
        /// Based on LordConversationsCampaignBehavior lines 707-733.
        /// </summary>
        public static int RemoveVanillaCompanionDialogs()
        {
            var manager = Campaign.Current?.ConversationManager;
            if (manager == null) return 0;

            int count = 0;

            // Actual vanilla companion hire dialog IDs from LordConversationsCampaignBehavior
            var companionDialogIds = new[]
            {
                "main_option_faction_hire",           // Line 707: Player option "I can use someone like you in my company."
                "companion_hire",                     // Line 730: NPC response with hiring cost
                "companion_hire_capacity_full",       // Line 731: Player response when companion limit reached
                "player_companion_hire_response_1",   // Line 732: Player accepts and pays
                "player_companion_hire_response_2"    // Line 733: Player can't afford
            };

            foreach (var id in companionDialogIds)
            {
                count += manager.RemoveDialogLineById(id);
            }

            return count;
        }
    }
}
