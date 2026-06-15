using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace TOR_Core.CampaignMechanics.PostBattleLoot
{
    /// <summary>
    /// Central manager for pending loot modifications.
    /// as of 1.4 pending loot modifications are applied to the PlayerEncounter loot rosters before the post battle loot screens.
    /// </summary>
    public static class PendingLootedTroopManager
    {
        private static TroopRoster _memberAdditions;
        private static TroopRoster _memberRemovals;
        private static TroopRoster _prisonerAdditions;
        private static TroopRoster _prisonerRemovals;
        private static bool _clearAllMembers;
        private static bool _clearAllPrisoners;
        private static bool _membersApplied;
        private static bool _prisonersApplied;

        static PendingLootedTroopManager()
        {
            _memberAdditions = TroopRoster.CreateDummyTroopRoster();
            _memberRemovals = TroopRoster.CreateDummyTroopRoster();
            _prisonerAdditions = TroopRoster.CreateDummyTroopRoster();
            _prisonerRemovals = TroopRoster.CreateDummyTroopRoster();
        }

        public static bool HasPendingModifications =>
            _memberAdditions.TotalManCount > 0 ||
            _memberRemovals.TotalManCount > 0 ||
            _prisonerAdditions.TotalManCount > 0 ||
            _prisonerRemovals.TotalManCount > 0 ||
            _clearAllMembers ||
            _clearAllPrisoners;

        public static void AddPendingMember(CharacterObject character, int count)
        {
            if (count <= 0) return;
            _memberAdditions.AddToCounts(character, count);
        }

        public static void RemovePendingMember(CharacterObject character, int count)
        {
            if (count <= 0) return;
            _memberRemovals.AddToCounts(character, count);
        }

        public static void ClearPendingMembers()
        {
            _clearAllMembers = true;
        }

        public static void AddPendingPrisoner(CharacterObject character, int count)
        {
            if (count <= 0) return;
            _prisonerAdditions.AddToCounts(character, count);
        }

        public static void RemovePendingPrisoner(CharacterObject character, int count)
        {
            if (count <= 0) return;
            _prisonerRemovals.AddToCounts(character, count);
        }

        public static void ClearPendingPrisoners()
        {
            _clearAllPrisoners = true;
        }

        public static void ApplyMemberModifications(TroopRoster roster)
        {
            if (_membersApplied) return;

            if (_clearAllMembers)
                roster.Clear();

            ApplyRemovals(roster, _memberRemovals);
            ApplyAdditions(roster, _memberAdditions);

            _membersApplied = true;
            TryClearAfterBothApplied();
        }

        public static void ApplyPrisonerModifications(TroopRoster roster)
        {
            if (_prisonersApplied) return;

            if (_clearAllPrisoners)
                roster.Clear();

            ApplyRemovals(roster, _prisonerRemovals);
            ApplyAdditions(roster, _prisonerAdditions);

            _prisonersApplied = true;
            TryClearAfterBothApplied();
        }

        public static void ConsumeMemberModifications()
        {
            _memberAdditions.Clear();
            _memberRemovals.Clear();
            _clearAllMembers = false;
            _membersApplied = false;
        }

        public static void ConsumePrisonerModifications()
        {
            _prisonerAdditions.Clear();
            _prisonerRemovals.Clear();
            _clearAllPrisoners = false;
            _prisonersApplied = false;
        }
        public static void ResetAllPendingState()
        {
            _memberAdditions.Clear();
            _memberRemovals.Clear();
            _prisonerAdditions.Clear();
            _prisonerRemovals.Clear();
            _clearAllMembers = false;
            _clearAllPrisoners = false;
            _membersApplied = false;
            _prisonersApplied = false;
        }

        private static void TryClearAfterBothApplied()
        {
            if (_membersApplied && _prisonersApplied)
                ClearAll();
        }

        private static void ClearAll()
        {
            _memberAdditions.Clear();
            _memberRemovals.Clear();
            _prisonerAdditions.Clear();
            _prisonerRemovals.Clear();
            _clearAllMembers = false;
            _clearAllPrisoners = false;
            _membersApplied = false;
            _prisonersApplied = false;
        }

        private static void ApplyRemovals(TroopRoster roster, TroopRoster removals)
        {
            if (removals == null || removals.TotalManCount == 0)
                return;

            foreach (var element in removals.GetTroopRoster())
            {
                if (element.Number <= 0)
                    continue;

                int currentCount = roster.GetTroopCount(element.Character);
                int toRemove = Math.Min(currentCount, element.Number);

                if (toRemove > 0)
                    roster.AddToCounts(element.Character, -toRemove);
            }
        }

        private static void ApplyAdditions(TroopRoster roster, TroopRoster additions)
        {
            if (additions == null || additions.TotalManCount == 0)
                return;

            foreach (var element in additions.GetTroopRoster())
            {
                if (element.Number > 0)
                    roster.AddToCounts(element.Character, element.Number);
            }
        }
    }
}
