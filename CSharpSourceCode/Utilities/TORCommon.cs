using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.Utilities
{
    public static class TORCommon
    {
        private static Random _random = new Random();
        
        /// <summary>
        /// Print a message to the MB2 message window.
        /// </summary>
        /// <param name="text">The text that you want to print to the console.</param>
        public static void Say(string text)
        {
            InformationManager.DisplayMessage(new InformationMessage(text, new Color(134, 114, 250)));
        }

        /// <summary>
        /// Gets the winds of magic icon as an inline string.
        /// </summary>
        /// <returns></returns>
        public static string GetWindsIconAsText()
        {
            return "<img src=\"winds_icon_45\"/>";
        }

        public static void Log(string message, LogLevel severity)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Log(severity, message);
        }

        /// <summary>
        /// Copies the currently equipped equipment items to the Windows Clipboard to enable pasting.
        /// </summary>
        /// <param name="vm">The Inventory ViewModel instance</param>
        public static void CopyEquipmentToClipBoard(SPInventoryVM vm)
        {
            string text = "";
            text += GetText(vm.CharacterWeapon1Slot) + ",";
            text += GetText(vm.CharacterWeapon2Slot) + ",";
            text += GetText(vm.CharacterWeapon3Slot) + ",";
            text += GetText(vm.CharacterWeapon4Slot) + ",";
            text += GetText(vm.CharacterHelmSlot) + ",";
            text += GetText(vm.CharacterTorsoSlot) + ",";
            text += GetText(vm.CharacterCloakSlot) + ",";
            text += GetText(vm.CharacterGloveSlot) + ",";
            text += GetText(vm.CharacterBootSlot) + ",";
            text += GetText(vm.CharacterMountSlot) + ",";
            text += GetText(vm.CharacterMountArmorSlot);
            Clipboard.SetText(text);
            InformationManager.DisplayMessage(new InformationMessage("Equipment items copied!", Colors.Green));
        }

        private static string GetText(SPItemVM slot)
        {
            if (slot.StringId != "" && slot.StringId != null) return "Item." + slot.StringId;
            else return "none";
        }

        /// <summary>
        /// Picks a random scene for the main menu based on the naming convenction of towmm_menuscene_XX where XX are sequential numbers.
        /// </summary>
        /// <returns>The scene's name as string</returns>
        public static string GetRandomSceneForMainMenu()
        {
            var filterednames = new List<string>();
            string pickedname = "towmm_menuscene_01";
            var path = BasePath.Name + "Modules/TOR_Environment/SceneObj/";
            if (Directory.Exists(path))
            {
                var dirnames = Directory.GetDirectories(path);
                filterednames = dirnames.Where(x =>
                {
                    string[] s = x.Split('/');
                    var name = s[s.Length - 1];
                    if (name.StartsWith("towmm_")) return true;
                    else return false;
                }).ToList();
            }
            if (filterednames.Count > 0)
            {
                var index = _random.Next(0, filterednames.Count);
                pickedname = filterednames[index];
                string[] s = pickedname.Split('/');
                pickedname = s[s.Length - 1];

            }
            return pickedname;
        }

        /// <summary>
        /// Finds the Settlement closest to the specified part and within the radius given. Lower values for radius will lead to better performance.
        /// </summary>
        /// <param name="party"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Settlement FindNearestSettlement(MobileParty party, float radius)
        {
            var nearbySettlements =
                Settlement.FindSettlementsAroundPosition(party.GetPosition2D, radius);

            // The list of nearbySettlements is unordered, thus we need to find the
            // settlement with minimum distance.
            return nearbySettlements.MinBy(
                settlement => Campaign.Current.Models.MapDistanceModel.GetDistance(party, settlement));
        }
    }
}
