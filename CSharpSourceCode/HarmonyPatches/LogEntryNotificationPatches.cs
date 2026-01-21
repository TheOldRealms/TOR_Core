using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch(typeof(CampaignInformationManager), "NewLogEntryAdded")]
    public static class LogEntryNotificationPatch
    {
        private static readonly bool DebugkingdomDecisions=false;
        
        public static bool Prefix(LogEntry log)
        {
            if (!(log is IChatNotification chatNotification) || !chatNotification.IsVisibleNotification)
                return true;

            switch (log)
            {
                case TakePrisonerLogEntry prisonerLog:
                    return IsRelevantPrisonerLog(prisonerLog);

                case EndCaptivityLogEntry endCaptivityLog:
                    return IsRelevantEndCaptivityLog(endCaptivityLog);

                case CharacterKilledLogEntry killedLog:
                    return IsRelevantKilledLog(killedLog);

                case MakePeaceLogEntry peaceLog:
                    return IsRelevantPeaceLog(peaceLog);

                case DeclareWarLogEntry warLog:
                    return IsRelevantWarLog(warLog);

                case ChangeSettlementOwnerLogEntry settlementLog:
                    return IsRelevantSettlementOwnerLog(settlementLog);

                case BattleStartedLogEntry battleLog:
                    return IsRelevantBattleLog(battleLog);

                case MercenaryClanChangedKingdomLogEntry mercenaryLog:
                    return IsRelevantMercenaryLog(mercenaryLog);

                case ClanChangeKingdomLogEntry clanChangeLog:
                    return IsRelevantClanChangeLog(clanChangeLog);

                case KingdomDecisionConcludedLogEntry decisionLog:
                    return IsRelevantKingdomDecisionLog(decisionLog);

                default:
                    return true;
            }
        }

        private static bool IsRelevantPrisonerLog(TakePrisonerLogEntry log)
        {
            if (TORNotificationHelper.IsHeroRelevantToPlayer(log.Prisoner))
                return true;
            if (TORNotificationHelper.IsHeroRelevantToPlayer(log.CapturerHero))
                return true;
            if (TORNotificationHelper.IsFactionRelevantToPlayer(log.CapturerPartyMapFaction))
                return true;
            return false;
        }

        private static bool IsRelevantEndCaptivityLog(EndCaptivityLogEntry log)
        {
            if (TORNotificationHelper.IsHeroRelevantToPlayer(log.Prisoner))
                return true;
            return false;
        }

        private static bool IsRelevantKilledLog(CharacterKilledLogEntry log)
        {
            if (TORNotificationHelper.IsHeroRelevantToPlayer(log.Victim))
                return true;
            if (log.Killer != null && TORNotificationHelper.IsHeroRelevantToPlayer(log.Killer))
                return true;
            return false;
        }

        private static bool IsRelevantPeaceLog(MakePeaceLogEntry log)
        {
            if (log.Faction1 is Kingdom k1 && TORNotificationHelper.IsKingdomRelevantToPlayer(k1))
                return true;
            if (log.Faction2 is Kingdom k2 && TORNotificationHelper.IsKingdomRelevantToPlayer(k2))
                return true;
            if (TORNotificationHelper.IsFactionRelevantToPlayer(log.Faction1))
                return true;
            if (TORNotificationHelper.IsFactionRelevantToPlayer(log.Faction2))
                return true;
            return false;
        }

        private static bool IsRelevantWarLog(DeclareWarLogEntry log)
        {
            if (log.Faction1 is Kingdom k1 && TORNotificationHelper.IsKingdomRelevantToPlayer(k1))
                return true;
            if (log.Faction2 is Kingdom k2 && TORNotificationHelper.IsKingdomRelevantToPlayer(k2))
                return true;
            if (TORNotificationHelper.IsFactionRelevantToPlayer(log.Faction1))
                return true;
            if (TORNotificationHelper.IsFactionRelevantToPlayer(log.Faction2))
                return true;
            return false;
        }

        private static bool IsRelevantSettlementOwnerLog(ChangeSettlementOwnerLogEntry log)
        {
            if (log.Settlement != null && log.Settlement.OwnerClan == Clan.PlayerClan)
                return true;
            if (log.Settlement?.OwnerClan?.Kingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(log.Settlement.OwnerClan.Kingdom))
                return true;
            return false;
        }

        private static readonly FieldInfo AttackerFactionField = typeof(BattleStartedLogEntry).GetField("_attackerFaction", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo SettlementField = typeof(BattleStartedLogEntry).GetField("_settlement", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool IsRelevantBattleLog(BattleStartedLogEntry log)
        {
            var attackerFaction = AttackerFactionField?.GetValue(log) as IFaction;
            var settlement = SettlementField?.GetValue(log) as Settlement;

            if (attackerFaction != null && TORNotificationHelper.IsFactionRelevantToPlayer(attackerFaction))
                return true;

            if (settlement?.OwnerClan?.Kingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(settlement.OwnerClan.Kingdom))
                return true;

            if (settlement?.MapFaction != null && TORNotificationHelper.IsFactionRelevantToPlayer(settlement.MapFaction))
                return true;

            return false;
        }

        private static bool IsRelevantMercenaryLog(MercenaryClanChangedKingdomLogEntry log)
        {
            if (log.Clan == Clan.PlayerClan)
                return true;
            if (log.OldKingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(log.OldKingdom))
                return true;
            if (log.NewKingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(log.NewKingdom))
                return true;
            return false;
        }

        private static bool IsRelevantClanChangeLog(ClanChangeKingdomLogEntry log)
        {
            if (log.Clan == Clan.PlayerClan)
                return true;
            if (log.OldKingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(log.OldKingdom))
                return true;
            if (log.NewKingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(log.NewKingdom))
                return true;
            return false;
        }

        private static bool IsRelevantKingdomDecisionLog(KingdomDecisionConcludedLogEntry log)
        {
            if (DebugkingdomDecisions) return true;
            
            if (log.Kingdom != null && TORNotificationHelper.IsKingdomRelevantToPlayer(log.Kingdom))
                return true;
            return false;
        }
    }
}
