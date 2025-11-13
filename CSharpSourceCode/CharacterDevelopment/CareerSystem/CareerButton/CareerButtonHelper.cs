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
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

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
                var wasNull = false;
                if (characterViewModel == null)
                {
                    wasNull = true;
                    characterViewModel = new PartyCharacterVM(partyScreenLogic, PartyVMExtension.ViewModelInstance, roster, indexOfCharacter, rosterType, PartyScreenLogic.PartyRosterSide.Right, true);
                }

                if (!wasNull)
                {
                    troops.RemoveAt(characterViewModel.Index);
                }
                characterViewModel.Troop = rosterElment;

                troops.Insert(characterViewModel.Index, characterViewModel);
            }
        }

        PartyVMExtension.ViewModelInstance.RefreshValues();
    }
}