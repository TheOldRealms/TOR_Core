using HarmonyLib;
using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ConversationPatches
    {
        /// <summary>
        /// Removes the option to speak with an npc with a generic background scene instead of visiting the npc and speaking at their location in the settlement.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SettlementMenuOverlayVM), "ExecuteOnSetAsActiveContextMenuItem")]
        public static void RemoveQuickTalk(SettlementMenuOverlayVM __instance)
        {
            var itemToRemove = __instance.ContextList.FirstOrDefault(x => x.ActionText == GameTexts.FindText("str_menu_overlay_context_list", "QuickConversation").ToString());
            if (itemToRemove != null) __instance.ContextList.Remove(itemToRemove);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_set_oath_phrases_on_condition")]
        public static void OverrideOathText()
        {
            var faction = Hero.OneToOneConversationHero.MapFaction;
            if (faction is Kingdom)
            {
                var line1 = TORTextHelper.GetText("tor_feudal_oath_line1", faction.StringId, "", skipValidation: true);
                if (!string.IsNullOrEmpty(line1)) MBTextManager.SetTextVariable("OATH_LINE_1", line1, false);

                var line2 = TORTextHelper.GetText("tor_feudal_oath_line2", faction.StringId, "", skipValidation: true);
                if (!string.IsNullOrEmpty(line2)) MBTextManager.SetTextVariable("OATH_LINE_2", line2, false);

                var line3 = TORTextHelper.GetText("tor_feudal_oath_line3", faction.StringId, "", skipValidation: true);
                if (!string.IsNullOrEmpty(line3)) MBTextManager.SetTextVariable("OATH_LINE_3", line3, false);

                var line4 = TORTextHelper.GetText("tor_feudal_oath_line4", faction.StringId, "", skipValidation: true);
                if (!string.IsNullOrEmpty(line4)) MBTextManager.SetTextVariable("OATH_LINE_4", line4, false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_liege_states_obligations_to_vassal_on_condition")]
        public static void OverridePlayerFactionJoinText()
        {
            var culture = Hero.OneToOneConversationHero.Culture;
            var text = TORTextHelper.GetTextObject("tor_player_accept_vassalage", culture.StringId, "You have been accepted as a vassal.", skipValidation: true);
            MBTextManager.SetTextVariable("PLAYER_ACCEPTED_AS_VASSAL", text, false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversations_set_voiced_line")]
        public static bool OverrideVoicedLines()
        {
            var hero = Hero.OneToOneConversationHero;
            if (hero != null && hero.IsVampire() && !hero.IsFemale)
            {
                StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
                MBTextManager.SetTextVariable("STR_SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", Hero.OneToOneConversationHero.CharacterObject), false);
                TextObject textObject = Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_context_line_vampire", CharacterObject.OneToOneConversationCharacter);
                MBTextManager.SetTextVariable("VOICED_LINE", textObject ?? TextObject.GetEmpty(), false);
                return false;
            }
            else if (hero != null && hero.CharacterObject.IsElf() && !hero.IsFemale)
            {
                StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
                MBTextManager.SetTextVariable("STR_SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", Hero.OneToOneConversationHero.CharacterObject), false);
                TextObject textObject = Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_context_line_elf", CharacterObject.OneToOneConversationCharacter);
                MBTextManager.SetTextVariable("VOICED_LINE", textObject ?? TextObject.GetEmpty(), false);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BanditInteractionsCampaignBehavior), "bandit_start_defender_condition")]
        public static void ChaosCultistBanditTextAndVoiceOverride()
        {
            var culture = CharacterObject.OneToOneConversationCharacter.Culture;
            if (culture.StringId == TORConstants.Cultures.CHAOS_CULTIST)
            {
                var text = TORTextHelper.GetTextObject("ccultist_robbery", "Your gold or your life!");
                MBTextManager.SetTextVariable("ROBBERY_THREAT", text, false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_player_want_to_end_service_as_mercenary_on_condition")]
        public static void EndMercenaryContract(ref bool __result)
        {
            if (Hero.MainHero.IsEnlisted())
            {
                __result = false;
            }
        }
    }
}