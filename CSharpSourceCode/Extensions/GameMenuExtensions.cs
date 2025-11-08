using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TOR_Core.Extensions
{
    public static class GameMenuExtensions
    {
        public static void RemoveGameMenuOption(this GameMenu gameMenu, string optionId)
        {
            var options = AccessTools.Field(typeof(GameMenu), "_menuItems").GetValue(gameMenu) as List<GameMenuOption>;
            var toRemove = options.FirstOrDefault(x => x.IdString == optionId);
            if (toRemove != null) options.Remove(toRemove);
            AccessTools.Field(typeof(GameMenu), "_menuItems").SetValue(gameMenu, options);
        }
    }
}
