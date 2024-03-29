﻿using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CharacterObjectPatches
    {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CharacterHelper), "GetTroopTree")]
		public static void TroopTreePatch(ref IEnumerable<CharacterObject> __result, CharacterObject baseTroop)
		{
			if (!__result.ToList().Any())
			{
				__result = CharacterHelper.GetTroopTree(baseTroop);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CharacterObject), "CreateFrom")]
		public static void TroopTreePatch(ref CharacterObject __result, CharacterObject character)
		{
			__result.Race = character.Race;
		}

		//Copied and modified from DesertionCampaignBehaviour.PartiesCheckDesertionDueToPartySizeExceedsPaymentRatio
		//reason is to support tier 9 troops. Game is crashing when T9 troops are trying to desert.
		//Must be reviewed if TW changes underlying code signature
		[HarmonyPrefix]
		[HarmonyPatch(typeof(DesertionCampaignBehavior), "PartiesCheckDesertionDueToPartySizeExceedsPaymentRatio")]
		public static bool DesertionTierPrefix(MobileParty mobileParty, ref TroopRoster desertedTroopList)
		{
			int partySizeLimit = mobileParty.Party.PartySizeLimit;
			if ((mobileParty.IsLordParty || mobileParty.IsCaravan) && mobileParty.Party.NumberOfAllMembers > partySizeLimit && mobileParty != MobileParty.MainParty && mobileParty.MapEvent == null)
			{
				int num = mobileParty.Party.NumberOfAllMembers - partySizeLimit;
				for (int i = 0; i < num; i++)
				{
					CharacterObject character = mobileParty.MapFaction.BasicTroop;
					int num2 = 99;
					bool flag = false;
					for (int j = 0; j < mobileParty.MemberRoster.Count; j++)
					{
						CharacterObject characterAtIndex = mobileParty.MemberRoster.GetCharacterAtIndex(j);
						if (!characterAtIndex.IsHero && characterAtIndex.Level < num2 && mobileParty.MemberRoster.GetElementNumber(j) > 0)
						{
							num2 = characterAtIndex.Level;
							character = characterAtIndex;
							flag = (mobileParty.MemberRoster.GetElementWoundedNumber(j) > 0);
						}
					}
					if (num2 < 99)
					{
						if (flag)
						{
							mobileParty.MemberRoster.AddToCounts(character, -1, false, -1, 0, true, -1);
						}
						else
						{
							mobileParty.MemberRoster.AddToCounts(character, -1, false, 0, 0, true, -1);
						}
					}
				}
			}
			bool flag2 = mobileParty.IsWageLimitExceeded();
			if (mobileParty.Party.NumberOfAllMembers > mobileParty.LimitedPartySize || flag2)
			{
				int numberOfDeserters = Campaign.Current.Models.PartyDesertionModel.GetNumberOfDeserters(mobileParty);
				int num3 = 0;
				while (num3 < numberOfDeserters && mobileParty.MemberRoster.TotalRegulars > 0)
				{
					int stackNo = -1;
					int num4 = 9;
					int num5 = 1;
					int num6 = 0;
					while (num6 < mobileParty.MemberRoster.Count && mobileParty.MemberRoster.TotalRegulars > 0)
					{
						CharacterObject characterAtIndex2 = mobileParty.MemberRoster.GetCharacterAtIndex(num6);
						int elementNumber = mobileParty.MemberRoster.GetElementNumber(num6);
						if (!characterAtIndex2.IsHero && elementNumber > 0 && characterAtIndex2.Tier <= num4)
						{
							num4 = characterAtIndex2.Tier;
							stackNo = num6;
							num5 = Math.Min(elementNumber, numberOfDeserters - num3);
						}
						num6++;
					}
					MobilePartyHelper.DesertTroopsFromParty(mobileParty, stackNo, num5, 0, ref desertedTroopList);
					num3 += num5;
				}
			}
			return false;
		}
	}
}
