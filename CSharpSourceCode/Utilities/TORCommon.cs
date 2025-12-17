using NLog;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
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

        public static string GetCompleteStringValue(TextObject textObject)
        {
            if (textObject == null) return null;
            List<TextObject> a = new List<TextObject> { textObject };
            List<string> b = TextObject.ConvertToStringList(a);

            return b[0];
        }

        public static Vec3 GetRandomDirection(float deviation, bool fixZ = true)
        {
            float x = MBRandom.RandomFloatRanged(-deviation, deviation);
            var y = MBRandom.RandomFloatRanged(-deviation, deviation);
            var z = fixZ ? 1 : MBRandom.RandomFloatRanged(-deviation, deviation);
            return new Vec3(x, y, z);
        }
        public static Mat3 GetRandomOrientation(Mat3 orientation, float deviation)
        {
            float rand1 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutX(rand1);
            float rand2 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutY(rand2);
            float rand3 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutZ(rand3);
            return orientation;
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
        /// Finds the Settlement closest to the party within the radius given. Lower values for radius will lead to better performance.
        /// </summary>
        public static Settlement FindNearestSettlement(MobileParty party, float radius, Func<Settlement, bool> condition = null)
        {
            LocatableSearchData<Settlement> locatableSearchData = Settlement.StartFindingLocatablesAroundPosition(party.Position.ToVec2(), radius);
            List<Settlement> nearbySettlements = new List<Settlement>();
            for (Settlement settlement = Settlement.FindNextLocatable(ref locatableSearchData); settlement != null; settlement = Settlement.FindNextLocatable(ref locatableSearchData))
            {
                nearbySettlements.Add(settlement);
            }
            if (condition != null)
            {
                nearbySettlements = nearbySettlements.FindAll(x => condition(x));
            }
            if (nearbySettlements.Count == 0) return null;
            // The list of nearbySettlements is unordered, thus we need to find the
            // settlement with minimum distance.
            return nearbySettlements.MinBy(
                settlement => Campaign.Current.Models.MapDistanceModel.GetDistance(party, settlement, false, MobileParty.NavigationType.Default, out _));
        }

        public static MBList<Settlement> FindSettlementsAroundPosition(Vec2 position, float radius, Func<Settlement, bool> condition = null)
        {
            MBList<Settlement> settlements = new MBList<Settlement>();
            LocatableSearchData<Settlement> locatableSearchData = Settlement.StartFindingLocatablesAroundPosition(position, radius);

            for (Settlement settlement = Settlement.FindNextLocatable(ref locatableSearchData); settlement != null; settlement = Settlement.FindNextLocatable(ref locatableSearchData))
            {
                if (condition == null || condition(settlement))
                {
                    settlements.Add(settlement);
                }
            }
            return settlements;
        }

        public static MBReadOnlyList<MobileParty> FindPartiesAroundPosition(Vec2 position, float radius, Func<MobileParty, bool> condition = null)
        {
            MBList<MobileParty> parties = new MBList<MobileParty>();
            LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(position, radius);

            for (MobileParty party = MobileParty.FindNextLocatable(ref locatableSearchData); party != null; party = MobileParty.FindNextLocatable(ref locatableSearchData))
            {
                if (condition == null || condition(party))
                {
                    parties.Add(party);
                }
            }
            return new MBReadOnlyList<MobileParty>(parties);
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