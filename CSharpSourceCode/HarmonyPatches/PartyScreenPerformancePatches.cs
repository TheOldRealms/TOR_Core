using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;

namespace TOR_Core.HarmonyPatches
{
    // naber: party screen can have kind of large rosters especially after huge battles. native TransferAllTroops flow clears and recreates partyCharacterVM lists, which causes avoidable
    // allocations and really annoying micro lag in UIs . here we are reusing the existing PartyCharacterVM instances and only syncs the lists, reducing gc pressure during ''all'' transfers

    [HarmonyPatch(typeof(PartyVM), "TransferAllTroops")]
    internal static class PartyVM_TransferAllTroops_PerformancePatch
    {
        private static readonly MethodInfo RefreshPartyInformationMethod =
            AccessTools.DeclaredMethod(typeof(PartyVM), "RefreshPartyInformation");

        private static readonly MethodInfo RefreshTopInformationMethod =
            AccessTools.DeclaredMethod(typeof(PartyVM), "RefreshTopInformation");

        private static bool Prefix(PartyVM __instance, PartyScreenLogic.PartyCommand command)
        {
            __instance.PartyScreenLogic.RemoveZeroCounts();

            var leftList = command.Type == PartyScreenLogic.TroopType.Member
                ? __instance.OtherPartyTroops
                : __instance.OtherPartyPrisoners;

            var rightList = command.Type == PartyScreenLogic.TroopType.Member
                ? __instance.MainPartyTroops
                : __instance.MainPartyPrisoners;

            var leftRoster = __instance.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Left, command.Type);
            var rightRoster = __instance.PartyScreenLogic.GetRoster(PartyScreenLogic.PartyRosterSide.Right, command.Type);

            SyncList(__instance, leftList, leftRoster, command.Type, PartyScreenLogic.PartyRosterSide.Left);
            SyncList(__instance, rightList, rightRoster, command.Type, PartyScreenLogic.PartyRosterSide.Right);

            __instance.OtherPartyComposition.RefreshCounts(__instance.OtherPartyTroops);
            __instance.MainPartyComposition.RefreshCounts(__instance.MainPartyTroops);

            RefreshTopInformationMethod?.Invoke(__instance, null);
            RefreshPartyInformationMethod?.Invoke(__instance, null);
            return false;
        }

        private static void SyncList(
            PartyVM partyVm,
            MBBindingList<PartyCharacterVM> list,
            TroopRoster roster,
            PartyScreenLogic.TroopType troopType,
            PartyScreenLogic.PartyRosterSide rosterSide)
        {
            var existingByCharacter = new Dictionary<object, PartyCharacterVM>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var vm = list[i];
                if (vm?.Character != null)
                {
                    existingByCharacter[vm.Character] = vm;
                }
            }

            var desired = new List<PartyCharacterVM>(roster.Count);
            int sideAsInt = (int)rosterSide;

            for (int i = 0; i < roster.Count; i++)
            {
                var character = roster.GetCharacterAtIndex(i);

                if (!existingByCharacter.TryGetValue(character, out var vm))
                {
                    bool isTransferable = partyVm.PartyScreenLogic.IsTroopTransferable(troopType, character, sideAsInt);
                    vm = new PartyCharacterVM(partyVm.PartyScreenLogic, partyVm, roster, i, troopType, rosterSide, isTransferable);
                }
                else
                {
                    //keep the existing vm instance to avoid recreation
                    vm.Index = i;
                    vm.Troop = roster.GetElementCopyAtIndex(i);
                    vm.ThrowOnPropertyChanged();
                }

                desired.Add(vm);
            }

            ApplyDesiredOrder(list, desired);
        }

        private static void ApplyDesiredOrder(MBBindingList<PartyCharacterVM> list, List<PartyCharacterVM> desired)
        {
            var desiredSet = new HashSet<PartyCharacterVM>(desired);

            //r emove vms that are no longer in the roster
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!desiredSet.Contains(list[i]))
                {
                    var vmToRemove = list[i];
                    vmToRemove.OnFinalize();
                    list.RemoveAt(i);
                }
            }

            // reorder/insert to match roster order without clearing the whole list
            for (int i = 0; i < desired.Count; i++)
            {
                var desiredVm = desired[i];

                if (i < list.Count && ReferenceEquals(list[i], desiredVm))
                {
                    continue;
                }

                int existingIndex = -1;
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (ReferenceEquals(list[j], desiredVm))
                    {
                        existingIndex = j;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    list.RemoveAt(existingIndex);
                    list.Insert(i, desiredVm);
                }
                else
                {
                    list.Insert(i, desiredVm);
                }
            }

            while (list.Count > desired.Count)
            {
                var vmToRemove = list[list.Count - 1];
                vmToRemove.OnFinalize();
                list.RemoveAt(list.Count - 1);
            }
        }
    }
}
