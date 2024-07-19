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
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
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
                    clan = Clan.All.FirstOrDefault(x=>x.StringId == clanName);
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CaravansCampaignBehavior), "GetTradeScoreForTown")]
        public static bool NoValueForRestrictedSettlement(ref float __result, MobileParty caravanParty, Town town)
        {
            if (caravanParty.Owner?.Culture?.StringId != town.Owner?.Culture?.StringId && town.Owner?.Culture?.StringId == TORConstants.Cultures.ASRAI)
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
            if(____invokedType == typeof(Settlement))
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
            else if(____invokedType == typeof(MobileParty) && __instance.IsExtended)
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettlementNameplatesVM), "Initialize")]
        public static bool AddCustomNamePlateVM(SettlementNameplatesVM __instance, IEnumerable<Tuple<Settlement, GameEntity>> settlements, ref IEnumerable<Tuple<Settlement, GameEntity>> ____allHideouts, Camera ____mapCamera, Action<Vec2> ____fastMoveCameraToPosition)
        {
            IEnumerable<Tuple<Settlement, GameEntity>> enumerable = from x in settlements
                                                                    where !x.Item1.IsHideout
                                                                    select x;
            ____allHideouts = from x in settlements
                                where x.Item1.IsHideout
                                select x;

            foreach (Tuple<Settlement, GameEntity> tuple in enumerable)
            {
                ToRSettlementNameplateVM item = new(tuple.Item1, tuple.Item2, ____mapCamera, ____fastMoveCameraToPosition);
                __instance.Nameplates.Add(item);
            }
            foreach (Tuple<Settlement, GameEntity> tuple2 in ____allHideouts)
            {
                if (tuple2.Item1.Hideout.IsSpotted)
                {
                    ToRSettlementNameplateVM item2 = new(tuple2.Item1, tuple2.Item2, ____mapCamera, ____fastMoveCameraToPosition);
                    __instance.Nameplates.Add(item2);
                }
            }
            foreach (SettlementNameplateVM settlementNameplateVM in __instance.Nameplates)
            {
                Settlement settlement = settlementNameplateVM.Settlement;
                if ((settlement?.SiegeEvent) != null)
                {
                    SettlementNameplateVM settlementNameplateVM2 = settlementNameplateVM;
                    Settlement settlement2 = settlementNameplateVM.Settlement;
                    settlementNameplateVM2.OnSiegeEventStartedOnSettlement(settlement2?.SiegeEvent);
                }
                else if (settlementNameplateVM.Settlement.IsTown || settlementNameplateVM.Settlement.IsCastle)
                {
                    Clan ownerClan = settlementNameplateVM.Settlement.OwnerClan;
                    if (ownerClan != null && ownerClan.IsRebelClan)
                    {
                        settlementNameplateVM.OnRebelliousClanFormed(settlementNameplateVM.Settlement.OwnerClan);
                    }
                }
            }
            RefreshRelationsOfNameplates(__instance.Nameplates);

            return false;
        }

        private static void RefreshRelationsOfNameplates(MBBindingList<SettlementNameplateVM> namePlates)
        {
            foreach (SettlementNameplateVM settlementNameplateVM in namePlates)
            {
                settlementNameplateVM.RefreshRelationStatus();
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
                    textObject = new TextObject("{=kXVHwjoV}You have arrived at your fief of {SETTLEMENT_LINK}. {PROSPERITY_INFO} {MORALE_INFO} {ROR_INFO}", null);
                }
                else
                {
                    textObject = new TextObject("{=UWzQsHA2}{SETTLEMENT_LINK} is governed by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {PROSPERITY_INFO} {MORALE_INFO} {ROR_INFO}", null);
                }
            }
            else if (settlement.IsCastle)
            {
                if (settlement.OwnerClan == Clan.PlayerClan)
                {
                    textObject = new TextObject("{=dA8RGoQ1}You have arrived at {SETTLEMENT_LINK}. {KEEP_INFO} {ROR_INFO}", null);
                }
                else
                {
                    textObject = new TextObject("{=4pmvrnmN}The castle of {SETTLEMENT_LINK} is owned by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {KEEP_INFO} {ROR_INFO}", null);
                }
            }
            else if (settlement.IsVillage)
            {
                if (settlement.OwnerClan == Clan.PlayerClan)
                {
                    textObject = new TextObject("{=M5iR1e5h}You have arrived at your fief of {SETTLEMENT_LINK}. {PROSPERITY_INFO} {ROR_INFO}", null);
                }
                else
                {
                    textObject = new TextObject("{=RVDojUOM}The lands around {SETTLEMENT_LINK} are owned mostly by {LORD.LINK}, {FACTION_OFFICIAL} of the {FACTION_TERM}. {PROSPERITY_INFO} {ROR_INFO}", null);
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
                textObject.SetTextVariable("FACTION_OFFICIAL", new TextObject("{=hb30yQPN}leader", null));
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
                if(template != null)
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
