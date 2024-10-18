using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helpers;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncyclopediaConceptPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultEncyclopediaConceptPage), "InitializeFilterItems")]
        public static void EncyclopediaPatch(ref IEnumerable<EncyclopediaFilterGroup> __result)
        {
            var result = new EncyclopediaFilterGroup(new List<EncyclopediaFilterItem>()
            {
                new EncyclopediaFilterItem(new TextObject("{=uauMia0D} Characters"), (Predicate<object>)(c => Concept.IsGroupMember("Characters", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=cwRkqIt4} Kingdoms"), (Predicate<object>)(c => Concept.IsGroupMember("Kingdoms", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=x6knoNnC} Clans"), (Predicate<object>)(c => Concept.IsGroupMember("Clans", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=GYzkb4iB} Parties"), (Predicate<object>)(c => Concept.IsGroupMember("Parties", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=u6GM5Spa} Armies"), (Predicate<object>)(c => Concept.IsGroupMember("Armies", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=zPYRGJtD} Troops"), (Predicate<object>)(c => Concept.IsGroupMember("Troops", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=3PUkH5Zf} Items"), (Predicate<object>)(c => Concept.IsGroupMember("Items", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=!} The Old Realms"), (Predicate<object>)(c => Concept.IsGroupMember("The Old Realms", (Concept)c))),
                new EncyclopediaFilterItem(new TextObject("{=xKVBAL3m} Campaign Issues"), (Predicate<object>)(c => Concept.IsGroupMember("CampaignIssues", (Concept)c)))

            }, new TextObject("{=tBx7XXps}Types"));

            var list = new List<EncyclopediaFilterGroup>();
            list.Add(result);
            __result = list;
        }

    }
}