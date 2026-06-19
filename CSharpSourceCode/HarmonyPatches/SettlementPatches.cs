using HarmonyLib;
using Helpers;
using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class SettlementPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Settlement), "Deserialize")]
        public static void DeserializePostfix(MBObjectManager objectManager, XmlNode node, Settlement __instance)
        {
            if (__instance.SettlementComponent is TORBaseSettlementComponent)
            {
                Clan clan = null;
                if (Campaign.Current.CampaignGameLoadingType == Campaign.GameLoadingType.NewCampaign)
                {
                    clan = MBObjectManager.Instance.ReadObjectReferenceFromXml<Clan>("owner", node);
                }
                else
                {
                    var value = node.Attributes["owner"].Value;
                    var clanName = value.Split('.')[1];
                    clan = Clan.All.FirstOrDefault(x => x.StringId == clanName);
                }
                if (clan != null)
                {
                    var comp = __instance.SettlementComponent as TORBaseSettlementComponent;
                    comp.OwnerClan = clan;
                }
            }
            if (node.Attributes["is_unwalled_settlement"]?.Value?.Trim() == "true")
            {
                ExtendedInfoManager.AddSettlementInfo(__instance, "IsUnwalledSettlement");
            }
            if (node.Attributes["has_sea_port"]?.Value?.Trim() == "true")
            {
                ExtendedInfoManager.AddSettlementInfo(__instance, "HasSeaPort");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("OwnerClan", MethodType.Getter)]
        public static bool OwnerClanPrefix(ref Clan __result, Settlement __instance)
        {
            if (__instance.SettlementComponent is TORBaseSettlementComponent)
            {
                var comp = __instance.SettlementComponent as TORBaseSettlementComponent;
                __result = comp.OwnerClan;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Town), "GetWallLevel")]
        public static void SetWallLevel(ref int __result)
        {
            __result = 3;
        }

        /// <summary>
        /// Restricts asrai caravans to only trading with asrai towns.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CaravansCampaignBehavior), "GetTradeScoreForTown")]
        public static bool NoValueForRestrictedSettlement(ref float __result, MobileParty caravanParty, Town town)
        {
            if (caravanParty.Owner?.Culture?.StringId == TORConstants.Cultures.ASRAI && town.Culture.StringId != TORConstants.Cultures.ASRAI)
            {
                __result = 0;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyBasedTooltipVM), "Refresh")]
        public static void AddExtrasToSettlementInfo(PropertyBasedTooltipVM __instance, Type ____invokedType, object[] ____invokedArgs)
        {
            if (____invokedType == typeof(Settlement))
            {
                var settlement = ____invokedArgs[0] as Settlement;
                if (settlement.SettlementComponent is ShrineComponent)
                {
                    var shrine = settlement.SettlementComponent as ShrineComponent;
                    if (shrine.Religion != null)
                    {
                        var copy = __instance.TooltipPropertyList.Where(x => !string.IsNullOrWhiteSpace(x.ValueLabel)).ToList();
                        copy.Insert(copy.Count - 1, new TooltipProperty("Affiliation", shrine.Religion.Name.ToString(), 0));
                        __instance.TooltipPropertyList.Clear();
                        foreach (var item in copy) __instance.TooltipPropertyList.Add(item);
                    }
                }
            }
            else if (____invokedType == typeof(MobileParty) && __instance.IsExtended)
            {
                if (____invokedArgs[0] is MobileParty party)
                {
                    var info = party.GetPartyInfo();
                    if (info != null && info.CurrentBlessingRemainingDuration > 0 && !string.IsNullOrWhiteSpace(info.CurrentBlessingStringId))
                    {
                        var text = GameTexts.FindText("tor_religion_blessing_name", info.CurrentBlessingStringId);
                        __instance.TooltipPropertyList.Add(new TooltipProperty("Blessing", text.ToString(), 0));
                    }
                }
            }
        }

        // replaces the vanilla SettlementNameplateVM creation with TORSettlementNameplateVM so we can display stuff like ror on the campaign map nameplates
        // naber's changes: prevent duplicate settlement nameplates from accumulating when ui refreshes and rebuilds
        // if a settlement already has a cached nameplate vm, we reuse it instead of creating/adding another one
        // this avoids duplicate rendering and list growth during campaign map ui refresh bursts. it seemed unimportant at first but i can assure you it is not.

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettlementNameplatesVM), "AddNameplate")]
        public static bool AddCustomNamePlateVM(SettlementNameplatesVM __instance, SettlementNameplateVM nameplate,
            MBList<SettlementNameplateVM> ____allNameplates, Dictionary<Settlement, SettlementNameplateVM> ____allNameplatesBySettlements,
            Camera ____mapCamera, Action<CampaignVec2> ____fastMoveCameraToPosition)
        {
            var settlement = nameplate.Settlement;

            if (____allNameplatesBySettlements.TryGetValue(settlement, out var existingNameplate) && existingNameplate != null)
            {
                if (!____allNameplates.Contains(existingNameplate))
                    ____allNameplates.Add(existingNameplate);

                switch (nameplate.SettlementTypeEnum)
                {
                    case SettlementNameplateVM.Type.Village:
                        if (!__instance.SmallNameplates.Contains(existingNameplate))
                            __instance.SmallNameplates.Add(existingNameplate);
                        break;

                    case SettlementNameplateVM.Type.Castle:
                        if (!__instance.MediumNameplates.Contains(existingNameplate))
                            __instance.MediumNameplates.Add(existingNameplate);
                        break;

                    case SettlementNameplateVM.Type.Town:
                        if (!__instance.LargeNameplates.Contains(existingNameplate))
                            __instance.LargeNameplates.Add(existingNameplate);
                        break;
                }

                nameplate.OnFinalize();
                return false;
            }
            GameEntity entity = AccessTools.Field(typeof(SettlementNameplateVM), "_entity").GetValue(nameplate) as GameEntity;
            ToRSettlementNameplateVM torNameplate = new(nameplate.Settlement, entity, ____mapCamera, ____fastMoveCameraToPosition);
            nameplate.OnFinalize(); // delete this if there's any issues with nametemplates

            if (!____allNameplates.Contains(torNameplate)) ____allNameplates.Add(torNameplate);
            ____allNameplatesBySettlements[nameplate.Settlement] = torNameplate;
            switch (nameplate.SettlementTypeEnum)
            {
                case SettlementNameplateVM.Type.Village:
                    __instance.SmallNameplates.Add(torNameplate);
                    return false;
                case SettlementNameplateVM.Type.Castle:
                    __instance.MediumNameplates.Add(torNameplate);
                    return false;
                case SettlementNameplateVM.Type.Town:
                    __instance.LargeNameplates.Add(torNameplate);
                    return false;
                default:
                    return false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SettlementNameplateEventsVM), MethodType.Constructor, typeof(Settlement))]
        public static void AddVillageIconForTorVillages(SettlementNameplateEventsVM __instance, Settlement settlement)
        {
            if (settlement.IsVillage)
            {
                if (__instance.EventsList.AnyQ(x => x.Type == 6))
                {
			        string text = "";
                    if (settlement.Village.VillageType == TORVillageTypes.GreenskinSwineFarm)
                        text = "boarfarm_icon";
                    else if (settlement.Village.VillageType == TORVillageTypes.GreenskinWolfFarm)
                        text = "wolffarm_icon";
                    if (!text.IsEmpty())
                    {
                        __instance.EventsList.Clear();//removes the old symbol, it's possible to display multiple icons
                        __instance.EventsList.Add(new SettlementNameplateEventItemVM(text));
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_town_town_leave_on_condition")]
        public static void DisableEnlistedLeaveTown(ref bool __result)
        {
            if (Hero.MainHero.IsEnlisted()) __result = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "SetIntroductionText")]
        public static bool AddRoRtoIntroductionText(Settlement settlement, bool fromKeep)
        {
            TextObject textObject = new("", null);
            if (settlement.IsTown)
            {
                if (settlement.OwnerClan == Clan.PlayerClan)
                {
                    textObject = TORTextHelper.GetTextObject("tor_settlement_arrival_town_player", "You have arrived at your fief of {SETTLEMENT_LINK}. {PROSPERITY_INFO} {MORALE_INFO} {ROR_INFO}");
                }
                else
                {
                    textObject = TORTextHelper.GetTextObject("tor_settlement_arrival_town_other", "{SETTLEMENT_LINK} is governed by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {PROSPERITY_INFO} {MORALE_INFO} {ROR_INFO}");
                }
            }
            else if (settlement.IsCastle)
            {
                if (settlement.OwnerClan == Clan.PlayerClan)
                {
                    textObject = TORTextHelper.GetTextObject("tor_settlement_arrival_castle_player", "You have arrived at {SETTLEMENT_LINK}. {KEEP_INFO} {ROR_INFO}");
                }
                else
                {
                    textObject = TORTextHelper.GetTextObject("tor_settlement_arrival_castle_other", "The castle of {SETTLEMENT_LINK} is owned by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {KEEP_INFO} {ROR_INFO}");
                }
            }
            else if (settlement.IsVillage)
            {
                if (settlement.OwnerClan == Clan.PlayerClan)
                {
                    textObject = TORTextHelper.GetTextObject("tor_settlement_arrival_village_player", "You have arrived at your fief of {SETTLEMENT_LINK}. {PROSPERITY_INFO} {ROR_INFO}");
                }
                else
                {
                    textObject = TORTextHelper.GetTextObject("tor_settlement_arrival_village_other", "The lands around {SETTLEMENT_LINK} are owned mostly by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {PROSPERITY_INFO} {ROR_INFO}");
                }
            }
            settlement.OwnerClan.Leader.SetPropertiesToTextObject(textObject, "LORD");
            string text = settlement.OwnerClan.Leader.MapFaction.Culture.StringId;
            if (settlement.OwnerClan.Leader.IsFemale)
            {
                text += "_f";
            }
            if (settlement.OwnerClan.Leader == Hero.MainHero && !Hero.MainHero.MapFaction.IsKingdomFaction)
            {
                textObject.SetTextVariable("FACTION_TERM", Hero.MainHero.Clan.EncyclopediaLinkWithName);
                textObject.SetTextVariable("FACTION_OFFICIAL", TORTextHelper.GetTextObject("tor_faction_leader", "leader"));
            }
            else
            {
                textObject.SetTextVariable("FACTION_TERM", settlement.MapFaction.EncyclopediaLinkWithName);
                if (settlement.OwnerClan.MapFaction.IsKingdomFaction && settlement.OwnerClan.Leader == settlement.OwnerClan.Leader.MapFaction.Leader)
                {
                    textObject.SetTextVariable("FACTION_OFFICIAL", GameTexts.FindText("str_faction_ruler", text));
                }
                else
                {
                    textObject.SetTextVariable("FACTION_OFFICIAL", GameTexts.FindText("str_faction_official", text));
                }
            }
            textObject.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
            settlement.SetPropertiesToTextObject(textObject, "SETTLEMENT_OBJECT");
            string variation = settlement.SettlementComponent.GetProsperityLevel().ToString();
            if ((settlement.IsTown && settlement.Town.InRebelliousState) || (settlement.IsVillage && settlement.Village.Bound.Town.InRebelliousState))
            {
                textObject.SetTextVariable("PROSPERITY_INFO", GameTexts.FindText("str_settlement_rebellion", null));
                textObject.SetTextVariable("MORALE_INFO", "");
            }
            else if (settlement.IsTown)
            {
                textObject.SetTextVariable("PROSPERITY_INFO", GameTexts.FindText("str_town_long_prosperity_1", variation));
                textObject.SetTextVariable("MORALE_INFO", SetTownMoraleText(settlement));
            }
            else if (settlement.IsVillage)
            {
                textObject.SetTextVariable("PROSPERITY_INFO", GameTexts.FindText("str_village_long_prosperity", variation));
            }
            textObject.SetTextVariable("KEEP_INFO", "");
            if (fromKeep && LocationComplex.Current != null)
            {
                if (!LocationComplex.Current.GetLocationWithId("lordshall").GetCharacterList().Any((LocationCharacter x) => x.Character.IsHero))
                {
                    textObject.SetTextVariable("KEEP_INFO", "{=OgkSLkFi}There is nobody in the lord's hall.");
                }
            }
            if (settlement.IsRoRSettlement())
            {
                var template = settlement.GetRoRTemplate();
                if (template != null)
                {
                    textObject.SetTextVariable("ROR_INFO", "{newline} " + "{newline}" + template.MenuHeaderText);
                    MBTextManager.SetTextVariable("newline", "\n", false);
                }
            }
            MBTextManager.SetTextVariable("SETTLEMENT_INFO", textObject, false);
            return false;
        }

        private static TextObject SetTownMoraleText(Settlement settlement)
        {
            SettlementComponent.ProsperityLevel prosperityLevel = settlement.SettlementComponent.GetProsperityLevel();
            string id;
            if (settlement.Town.Loyalty < 25f)
            {
                if (prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
                {
                    id = "str_settlement_morale_rebellious_adversity";
                }
                else if (prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
                {
                    id = "str_settlement_morale_rebellious_average";
                }
                else
                {
                    id = "str_settlement_morale_rebellious_prosperity";
                }
            }
            else if (settlement.Town.Loyalty < 65f)
            {
                if (prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
                {
                }
                if (prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
                {
                    id = "str_settlement_morale_medium_average";
                }
                else
                {
                    id = "str_settlement_morale_medium_prosperity";
                }
            }
            else if (prosperityLevel <= SettlementComponent.ProsperityLevel.Low)
            {
                id = "str_settlement_morale_high_adversity";
            }
            else if (prosperityLevel <= SettlementComponent.ProsperityLevel.Mid)
            {
                id = "str_settlement_morale_high_average";
            }
            else
            {
                id = "str_settlement_morale_high_prosperity";
            }
            return GameTexts.FindText(id, null);
        }
    }
}