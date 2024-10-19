using HarmonyLib;
using Helpers;
using SandBox.CampaignBehaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
	public static class ConversationPatches
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_wanderer_introduction_on_condition")]
		public static bool WandererString(ref bool __result, ref Dictionary<CharacterObject, CharacterObject> ____previouslyMetWandererTemplates)
		{
			if (CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.IsHero && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Wanderer && CharacterObject.OneToOneConversationCharacter.HeroObject.HeroState != Hero.CharacterStates.Prisoner)
			{
				StringHelpers.SetCharacterProperties("CONVERSATION_CHARACTER", Hero.OneToOneConversationHero.CharacterObject, null);
				string stringId = Hero.OneToOneConversationHero.Template.StringId;
				CharacterObject characterObject;
				____previouslyMetWandererTemplates.TryGetValue(Hero.OneToOneConversationHero.Template, out characterObject);
				if (characterObject == null || characterObject == Hero.OneToOneConversationHero.CharacterObject)
				{
					if (characterObject == null)
					{
						____previouslyMetWandererTemplates[Hero.OneToOneConversationHero.Template] = Hero.OneToOneConversationHero.CharacterObject;
					}
					MBTextManager.SetTextVariable("IMPERIALCAPITAL", new TextObject("Altdorf"));
					MBTextManager.SetTextVariable("WANDERER_BACKSTORY_A", GameTexts.FindText("backstory_a", stringId), false);
					MBTextManager.SetTextVariable("WANDERER_BACKSTORY_B", GameTexts.FindText("backstory_b", stringId), false);
					MBTextManager.SetTextVariable("WANDERER_BACKSTORY_C", GameTexts.FindText("backstory_c", stringId), false);
					MBTextManager.SetTextVariable("BACKSTORY_RESPONSE_1", GameTexts.FindText("response_1", stringId), false);
					MBTextManager.SetTextVariable("BACKSTORY_RESPONSE_2", GameTexts.FindText("response_2", stringId), false);
					MBTextManager.SetTextVariable("WANDERER_BACKSTORY_D", GameTexts.FindText("backstory_d", stringId), false);
					StringHelpers.SetCharacterProperties("MET_WANDERER", Hero.OneToOneConversationHero.CharacterObject, null);
					if (CampaignMission.Current.Location != null && CampaignMission.Current.Location.StringId != "tavern")
					{
						MBTextManager.SetTextVariable("WANDERER_PREBACKSTORY", GameTexts.FindText("spc_prebackstory_generic", null), false);
					}
					__result = true;
				}
			}
			else __result = false;
			return false;
		}

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
			if(faction is Kingdom)
            {
				var line1 = GameTexts.FindText("tor_feudal_oath_line1", faction.StringId).ToString();
				if(line1 != null && !string.IsNullOrEmpty(line1)) MBTextManager.SetTextVariable("OATH_LINE_1", line1, false);

				var line2 = GameTexts.FindText("tor_feudal_oath_line2", faction.StringId).ToString();
				if (line2 != null && !string.IsNullOrEmpty(line2)) MBTextManager.SetTextVariable("OATH_LINE_2", line2, false);

				var line3 = GameTexts.FindText("tor_feudal_oath_line3", faction.StringId).ToString();
				if (line3 != null && !string.IsNullOrEmpty(line3)) MBTextManager.SetTextVariable("OATH_LINE_3", line3, false);

				var line4 = GameTexts.FindText("tor_feudal_oath_line4", faction.StringId).ToString();
				if (line4 != null && !string.IsNullOrEmpty(line4)) MBTextManager.SetTextVariable("OATH_LINE_4", line4, false);
			}		
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_liege_states_obligations_to_vassal_on_condition")]
		public static void OverridePlayerFactionJoinText()
		{
			var culture = Hero.OneToOneConversationHero.Culture;
			var text = GameTexts.FindText("tor_player_accept_vassalage", culture.StringId);
			MBTextManager.SetTextVariable("PLAYER_ACCEPTED_AS_VASSAL", text, false);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversations_set_voiced_line")]
		public static bool OverrideVoicedLines()
		{
			var hero = Hero.OneToOneConversationHero;
			if(hero != null && hero.IsVampire() && !hero.IsFemale)
            {
				StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
				MBTextManager.SetTextVariable("STR_SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", Hero.OneToOneConversationHero.CharacterObject), false);
				TextObject textObject = Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_context_line_vampire", CharacterObject.OneToOneConversationCharacter);
				MBTextManager.SetTextVariable("VOICED_LINE", textObject ?? TextObject.Empty, false);
				return false;
            }
			else if (hero != null && hero.CharacterObject.IsElf() && !hero.IsFemale)
			{
                StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
                MBTextManager.SetTextVariable("STR_SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", Hero.OneToOneConversationHero.CharacterObject), false);
                TextObject textObject = Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_context_line_elf", CharacterObject.OneToOneConversationCharacter);
                MBTextManager.SetTextVariable("VOICED_LINE", textObject ?? TextObject.Empty, false);
                return false;
            }
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(BanditsCampaignBehavior), "bandit_start_defender_condition")]
		public static void ChaosCultistBanditTextAndVoiceOverride()
		{
			var culture = CharacterObject.OneToOneConversationCharacter.Culture;
			if(culture.StringId == "forest_bandits")
            {
				var text = GameTexts.FindText("ccultist_robbery");
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
