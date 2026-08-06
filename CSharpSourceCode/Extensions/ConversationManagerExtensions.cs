using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Conversation;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    /// <summary>
    /// Extension methods for ConversationManager to remove dialog lines.
    /// </summary>
    public static class ConversationManagerExtensions
    {
        /// <summary>
        /// Removes dialog lines by ID.
        /// </summary>
        public static int RemoveDialogLineById(this ConversationManager manager, string dialogId)
        {
            if (string.IsNullOrEmpty(dialogId)) return 0;

            var sentences = GetAllConversationSentences(manager);
            if (sentences == null) return 0;

            var toRemove = sentences.Where(s => s.Id == dialogId).ToList();

            foreach (var sentence in toRemove)
            {
                sentences.Remove(sentence);
            }

            TORCommon.Log($"Removed {toRemove.Count} dialog line(s) with ID '{dialogId}'", NLog.LogLevel.Debug);
            return toRemove.Count;
        }
        

        private static List<ConversationSentence> GetAllConversationSentences(ConversationManager manager)
        {
            try
            {
                var sentencesField = AccessTools.Field(typeof(ConversationManager), "_sentences");
                if (sentencesField == null)
                {
                    TORCommon.Log("Field '_sentences' not found in ConversationManager", NLog.LogLevel.Error);
                    return null;
                }

                var sentences = sentencesField.GetValue(manager) as List<ConversationSentence>;
                if (sentences == null)
                {
                    TORCommon.Log("_sentences field is null or wrong type", NLog.LogLevel.Error);
                }

                return sentences;
            }
            catch (Exception ex)
            {
                TORCommon.Log($"Failed to get conversation sentences: {ex.Message}", NLog.LogLevel.Error);
                return null;
            }
        }
    }
}
