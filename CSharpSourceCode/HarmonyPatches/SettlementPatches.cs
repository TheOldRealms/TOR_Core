using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;

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
                var party = ____invokedArgs[0] as MobileParty;
                if(party != null)
                {
                    var info = party.GetPartyInfo();
                    if(info != null && info.CurrentBlessingRemainingDuration > 0 && !string.IsNullOrWhiteSpace(info.CurrentBlessingStringId))
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
                ToRSettlementNameplateVM item = new ToRSettlementNameplateVM(tuple.Item1, tuple.Item2, ____mapCamera, ____fastMoveCameraToPosition);
                __instance.Nameplates.Add(item);
            }
            foreach (Tuple<Settlement, GameEntity> tuple2 in ____allHideouts)
            {
                if (tuple2.Item1.Hideout.IsSpotted)
                {
                    ToRSettlementNameplateVM item2 = new ToRSettlementNameplateVM(tuple2.Item1, tuple2.Item2, ____mapCamera, ____fastMoveCameraToPosition);
                    __instance.Nameplates.Add(item2);
                }
            }
            foreach (SettlementNameplateVM settlementNameplateVM in __instance.Nameplates)
            {
                Settlement settlement = settlementNameplateVM.Settlement;
                if (((settlement != null) ? settlement.SiegeEvent : null) != null)
                {
                    SettlementNameplateVM settlementNameplateVM2 = settlementNameplateVM;
                    Settlement settlement2 = settlementNameplateVM.Settlement;
                    settlementNameplateVM2.OnSiegeEventStartedOnSettlement((settlement2 != null) ? settlement2.SiegeEvent : null);
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
    }
}
