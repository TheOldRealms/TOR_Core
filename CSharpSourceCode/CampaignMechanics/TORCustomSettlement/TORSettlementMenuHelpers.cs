using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public static class TORSettlementMenuHelpers
    {
        public static void RearrangeTownMenus(GameMenu menu, string entryId, string targetEntryId, bool above = false)
        {
            if (menu == null) return;

            var options = AccessTools.Field(typeof(GameMenu), "_menuItems").GetValue(menu) as List<GameMenuOption>;
            if (options == null) return;

            var targetEntry = options.FirstOrDefault(x => x.IdString == targetEntryId);
            var entry = options.FirstOrDefault(x => x.IdString == entryId);

            if (targetEntry != null && entry != null)
            {
                int positionEntryIndex = -1;
                int entryEntryIndex = -1;
                List<GameMenuOption> newOptions = new List<GameMenuOption>();

                for (int i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    if (option == targetEntry)
                    {
                        positionEntryIndex = i;
                    }
                    else if (option == entry)
                    {
                        entryEntryIndex = i;
                        continue;
                    }

                    var shift = above ? -1 : 1;
                    if (positionEntryIndex > -1 && i == positionEntryIndex + 1)
                    {
                        newOptions.Add(options[entryEntryIndex]);
                    }
                    newOptions.Add(option);
                }

                AccessTools.Field(typeof(GameMenu), "_menuItems").SetValue(menu, newOptions);
            }
        }
    }
}