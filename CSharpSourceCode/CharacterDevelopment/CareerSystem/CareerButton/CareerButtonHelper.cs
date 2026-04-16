using System;
using HarmonyLib;
using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public static class CareerButtonHelper
{

    public static int GetMaximumExchangeTroops(CharacterObject originalTroop, bool isPrisoner, int upperBoundNumberTroops, int goldCost, int customResourceCost)
    {
        var buyable = 1;//maximum affordable

        var count = 1;

        count = isPrisoner ? Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementNumber(originalTroop) : Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(originalTroop);

        var goldBuyable = count;

        if (customResourceCost > 0)
        {
            var pending = CustomResourceManager.GetPendingResources().Values.ToList().Sum();
            var rest = Hero.MainHero.GetCultureSpecificCustomResourceValue() - pending;
            buyable = (int)((rest / customResourceCost >= upperBoundNumberTroops) ? upperBoundNumberTroops : rest / customResourceCost);
        }
        //gold
        if (goldCost > 0)
        {
            var goldPending = PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount;
            var rest = Hero.MainHero.Gold - goldPending;
            goldBuyable = (int)((rest / goldCost >= upperBoundNumberTroops) ? upperBoundNumberTroops : rest / goldCost);

        }

        if (goldCost <= 0 && customResourceCost <= 0)
        {
            return MathF.Min(count, upperBoundNumberTroops);
        }
        buyable = MathF.Min(buyable, goldBuyable);

        buyable = MathF.Min(buyable, count);

        return buyable;
    }

    public static void RemoveUnit(CharacterObject targetCharacterObject, bool updateScreen, bool isPrisoner = false)
    {
        ExchangeUnitForNewUnit(targetCharacterObject, null, updateScreen, isPrisoner);
    }

    public static void ExchangeUnitForNewUnit(CharacterObject targetCharacterObject, CharacterObject newUnit, bool updateScreen, bool isPrisoner = false)
    {
        var partyScreenLogic = PartyScreenHelper.GetActivePartyState().PartyScreenLogic;
        PartyScreenLogic.PresentationUpdate update = partyScreenLogic.UpdateDelegate;
        PartyScreenLogic.PartyCommand command = new PartyScreenLogic.PartyCommand();

        var roster = isPrisoner ? Hero.MainHero.PartyBelongedTo.PrisonRoster : Hero.MainHero.PartyBelongedTo.MemberRoster;

        var indexOfCharacter = roster.FindIndexOfTroop(targetCharacterObject);

        roster.AddToCounts(targetCharacterObject, -1);

        var rosterElment = new TroopRosterElement();
        if (indexOfCharacter != -1)
        {
            rosterElment = roster.GetElementCopyAtIndex(indexOfCharacter);
        }

        var rosterType = isPrisoner ? PartyScreenLogic.TroopType.Prisoner : PartyScreenLogic.TroopType.Member;
        int indexToInsertTroop = partyScreenLogic.GetIndexToInsertTroop(PartyScreenLogic.PartyRosterSide.Right, rosterType, new TroopRosterElement(targetCharacterObject));
        var remainingCount = isPrisoner ? Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementNumber(targetCharacterObject) : Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(targetCharacterObject);

        if (updateScreen)
        {
            command.FillForRecruitTroop(PartyScreenLogic.PartyRosterSide.Right, rosterType, targetCharacterObject, -1, indexToInsertTroop);
            partyScreenLogic.AddCommand(command);    //the command way is needed to keep a log of the pending changes
            if (remainingCount <= 0)
            {
                MBInformationManager.HideInformations();
                var troops = isPrisoner ? PartyVMExtension.ViewModelInstance.MainPartyPrisoners : PartyVMExtension.ViewModelInstance.MainPartyTroops;
                var characterViewModel = troops.FirstOrDefault(x => x.Character == targetCharacterObject);
                troops.Remove(characterViewModel);
                PartyVMExtension.ViewModelInstance.ExecuteRemoveZeroCounts();
            }
            PartyVMExtension.ViewModelInstance.RefreshValues();
        }

        if (newUnit != null)
        {
            Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(newUnit, 1);
            indexToInsertTroop = partyScreenLogic.GetIndexToInsertTroop(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member, new TroopRosterElement(newUnit));
            if (updateScreen)
            {
                command.FillForRecruitTroop(PartyScreenLogic.PartyRosterSide.Right, PartyScreenLogic.TroopType.Member, newUnit, 1, indexToInsertTroop);
                update(command);
            }
            if (remainingCount > 0)
            {
                var troops = isPrisoner ? PartyVMExtension.ViewModelInstance.MainPartyPrisoners : PartyVMExtension.ViewModelInstance.MainPartyTroops;
                //this way is a bit clunky but works best for manually setting visual elements.
                //the update kills the characterViewModel we have to reinitialize it. Note that in the other case we dont do this, this would create a copy roster we dont want.
                var characterViewModel = troops.FirstOrDefault(x => x.Character == targetCharacterObject);
                var insertIndex = indexOfCharacter;
                if (characterViewModel == null)
                {
                    characterViewModel = new PartyCharacterVM(partyScreenLogic, PartyVMExtension.ViewModelInstance, roster, indexOfCharacter, rosterType, PartyScreenLogic.PartyRosterSide.Right, true);
                }

                else
                {
                    insertIndex = troops.IndexOf(characterViewModel);
                    troops.Remove(characterViewModel);
                }
                characterViewModel.Troop = rosterElment;

                troops.Insert(insertIndex, characterViewModel);
            }
        }

        PartyVMExtension.ViewModelInstance.RefreshValues();
    }
    
    
    public const string REMOVE_IDENTIFIER = "remove";

    /// <summary>
    /// Gets the party extended info for the main hero's party.
    /// </summary>
    public static MobilePartyExtendedInfo GetPartyExtendedInfo()
    {
        if (Hero.MainHero?.PartyBelongedTo == null) return null;
        return ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
    }

    /// <summary>
    /// Gets all attribute IDs currently applied to a character.
    /// </summary>
    public static List<string> GetTroopAttributeIds(CharacterObject character)
    {
        if (character == null) return new List<string>();

        var partyInfo = GetPartyExtendedInfo();
        if (partyInfo == null) return new List<string>();

        if (partyInfo.TroopAttributes.TryGetValue(character.StringId, out var attributes))
        {
            return attributes.ToList();
        }
        return new List<string>();
    }

    /// <summary>
    /// Gets current active items for a character by matching attribute IDs against a list of available items.
    /// </summary>
    public static List<T> GetCurrentActiveItems<T>(CharacterObject character, IEnumerable<T> allItems, Func<T, string> idSelector) where T : class
    {
        if (character == null) return null;

        var attributeIds = GetTroopAttributeIds(character);
        if (!attributeIds.Any()) return null;

        var activeItems = new List<T>();
        foreach (var item in allItems)
        {
            if (attributeIds.Contains(idSelector(item)))
            {
                activeItems.Add(item);
            }
        }

        return activeItems.Any() ? activeItems : null;
    }

    /// <summary>
    /// Removes specified attributes from a character.
    /// </summary>
    public static void RemoveTroopAttributes(CharacterObject character, IEnumerable<string> attributeIds)
    {
        if (character == null) return;

        var partyInfo = GetPartyExtendedInfo();
        if (partyInfo == null) return;

        foreach (var id in attributeIds)
        {
            partyInfo.RemoveTroopAttribute(character.StringId, id);
        }
    }

    /// <summary>
    /// Removes all attributes from a character that match the provided items.
    /// </summary>
    public static void RemoveAllCurrentItems<T>(CharacterObject character, List<T> currentItems, Func<T, string> idSelector) where T : class
    {
        if (currentItems == null || !currentItems.Any()) return;

        var idsToRemove = currentItems.Select(idSelector);
        RemoveTroopAttributes(character, idsToRemove);
    }

    /// <summary>
    /// Adds an attribute to a character.
    /// </summary>
    public static void AddTroopAttribute(CharacterObject character, string attributeId)
    {
        if (character == null || string.IsNullOrEmpty(attributeId)) return;

        var partyInfo = GetPartyExtendedInfo();
        partyInfo?.AddTroopAttribute(character, attributeId);
    }

    /// <summary>
    /// Validates party infos and refreshes the party UI.
    /// </summary>
    public static void RefreshPartyAttributesUI()
    {
        ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);

        if (PartyVMExtension.ViewModelInstance != null)
        {
            PartyVMExtension.ViewModelInstance.RefreshValues();
        }
    }

    /// <summary>
    /// Checks if the "remove" option was selected in the inquiry.
    /// </summary>
    public static bool IsRemoveSelected(List<InquiryElement> elements)
    {
        return elements.Any(e => e.Identifier as string == REMOVE_IDENTIFIER);
    }

    /// <summary>
    /// Creates a "Remove" inquiry element for the selection list.
    /// </summary>
    public static InquiryElement CreateRemoveOption(string currentItemNames, string hintTextId = "tor_career_button_remove_hint", string hintTextDefault = "Remove all applied effects without compensation.")
    {
        var removeHint = TORTextHelper.GetText(hintTextId, hintTextDefault);
        var removeText = TORTextHelper.GetText("tor_career_button_remove_option", "Remove") + $" ({currentItemNames})";
        return new InquiryElement(REMOVE_IDENTIFIER, removeText, null, true, removeHint);
    }

    /// <summary>
    /// Builds a display text showing all current items and their descriptions.
    /// </summary>
    public static TextObject BuildCurrentItemsDisplayText<T>(List<T> currentItems, Func<T, string> nameSelector, Func<T, string> descriptionSelector = null) where T : class
    {
        if (currentItems == null || !currentItems.Any()) return null;

        var displayText = TextObject.GetEmpty();
        foreach (var item in currentItems)
        {
            var text = displayText.ToString();
            if (descriptionSelector != null)
            {
                text += nameSelector(item) + ": " + descriptionSelector(item);
            }
            else
            {
                text += nameSelector(item);
            }
            text += "\n";
            displayText = new TextObject(text);
        }

        return displayText;
    }

    /// <summary>
    /// Extracts items from inquiry elements, filtering out the "remove" option.
    /// </summary>
    public static List<T> GetSelectedItems<T>(List<InquiryElement> elements) where T : class
    {
        var selectedItems = new List<T>();
        foreach (var elem in elements)
        {
            if (elem.Identifier as string == REMOVE_IDENTIFIER) continue;

            var item = elem.Identifier as T;
            if (item != null)
            {
                selectedItems.Add(item);
            }
        }
        return selectedItems;
    }

    /// <summary>
    /// Processes a selection: removes current items and adds new ones.
    /// Returns true if items were added, false if only removed.
    /// </summary>
    public static bool ProcessSelection<T>(
        CharacterObject character,
        List<InquiryElement> elements,
        List<T> currentItems,
        Func<T, string> idSelector,
        Action<T> onBeforeAdd = null,
        Action<List<T>> onRemove = null) where T : class
    {
        // Check if remove was selected
        if (IsRemoveSelected(elements))
        {
            if (currentItems != null && currentItems.Any())
            {
                onRemove?.Invoke(currentItems);
                RemoveAllCurrentItems(character, currentItems, idSelector);
            }
            RefreshPartyAttributesUI();
            return false;
        }

        // Get selected items
        var selectedItems = GetSelectedItems<T>(elements);

        // Remove current items first
        if (currentItems != null && currentItems.Any())
        {
            RemoveAllCurrentItems(character, currentItems, idSelector);
        }

        // Add new items
        foreach (var item in selectedItems)
        {
            onBeforeAdd?.Invoke(item);
            AddTroopAttribute(character, idSelector(item));
        }

        RefreshPartyAttributesUI();
        return true;
    }
}