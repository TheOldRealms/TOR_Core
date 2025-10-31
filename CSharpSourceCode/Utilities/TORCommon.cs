using NLog;
using System;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.Utilities
{
    public static class TORCommon
    {
        /// <summary>
        /// Displays the provided <see cref="TextObject"/> in the game's message window.
        /// </summary>
        /// <param name="text">The localized text object to display.</param>
        public static void Say(TextObject text)
        {
            Say(text.ToString());
        }

        /// <summary>
        /// Prints a plain string message to the Mount & Blade II message window.
        /// </summary>
        /// <param name="text">The message text to display.</param>
        public static void Say(string text)
        {
            InformationManager.DisplayMessage(new InformationMessage(text, new Color(134, 114, 250)));
        }

        /// <summary>
        /// Logs a message using NLog with the specified severity level.
        /// </summary>
        /// <param name="message">The message to write to the log.</param>
        /// <param name="severity">The NLog <see cref="LogLevel"/> representing the severity of the message.</param>
        public static void Log(string message, LogLevel severity)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Log(severity, message);
        }

        /// <summary>
        /// Copies the currently equipped character equipment items to the Windows clipboard as a comma-separated list.
        /// </summary>
        /// <param name="vm">The inventory view model containing the character equipment slots to copy.</param>
        /// <remarks>
        /// Each equipped slot is converted to a text identifier. Empty slots are represented by the string "none".
        /// The resulting text can be pasted into other places that accept the same item identifier format.
        /// </remarks>
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

        /// <summary>
        /// Returns a textual identifier for the given inventory slot view model.
        /// </summary>
        /// <param name="slot">The inventory item view model to inspect.</param>
        /// <returns>"Item.{stringId}" if the slot contains an item with a string id; otherwise "none".</returns>
        private static string GetText(SPItemVM slot)
        {
            if (slot.StringId != "" && slot.StringId != null) return "Item." + slot.StringId;
            else return "none";
        }
    }
}