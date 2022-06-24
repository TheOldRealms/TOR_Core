using HarmonyLib;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class SettlementPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Settlement), "Deserialize")]
        public static void DeserializePostfix(MBObjectManager objectManager, XmlNode node, Settlement __instance)
        {
            if (__instance.SettlementComponent is TORCustomSettlementComponent)
            {
                string clanName = node.Attributes["owner"].Value;
                clanName = clanName.Split('.')[1];
                Clan clan = Clan.FindFirst(x => x.StringId == clanName);
                if (clan != null)
                {
                    var comp = __instance.SettlementComponent as TORCustomSettlementComponent;
                    comp.SetClan(clan);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("OwnerClan", MethodType.Getter)]
        public static bool OwnerClanPrefix(ref Clan __result, Settlement __instance)
        {
            if (__instance.SettlementComponent is TORCustomSettlementComponent)
            {
                var comp = __instance.SettlementComponent as TORCustomSettlementComponent;
                __result = comp.OwnerClan;
                return false;
            }
            return true;
        }
    }
}
