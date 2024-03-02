using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    internal sealed class TORCooldownManager
    {
        private static readonly Lazy<TORCooldownManager> lazy = new Lazy<TORCooldownManager>(() => new TORCooldownManager());
        public static TORCooldownManager Instance { get => lazy.Value; }

        internal static Dictionary<Kingdom, CampaignTime> LastPeaceProposalTime => Instance._lastPeaceProposalTime;
        internal static Dictionary<string, CampaignTime> LastAllianceFormedTime => Instance._lastAllianceFormedTime;

        [SaveableField(2)]
        [UsedImplicitly]
        private Dictionary<Kingdom, CampaignTime> _lastPeaceProposalTime;

        [SaveableField(3)]
        [UsedImplicitly]
        private Dictionary<string, CampaignTime> _lastAllianceFormedTime;

        private static float MinimumDaysBetweenPeaceProposals => 5f;

        internal TORCooldownManager()
        {
            _lastPeaceProposalTime = new Dictionary<Kingdom, CampaignTime>();
            _lastAllianceFormedTime = new Dictionary<string, CampaignTime>();
        }

        public void UpdateLastPeaceProposalTime(Kingdom kingdom, CampaignTime campaignTime)
        {
            _lastPeaceProposalTime[kingdom] = campaignTime;
        }

        public void UpdateLastAllianceFormedTime(Kingdom kingdom1, Kingdom kingdom2, CampaignTime campaignTime)
        {
            var key = CreateKey(kingdom1, kingdom2);
            _lastAllianceFormedTime[key] = campaignTime;
        }

        public static bool HasBreakAllianceCooldown(Kingdom kingdom1, Kingdom kingdom2, out float elapsedDaysUntilNow)
        {
            var hasBreakAllianceCooldown = false;
            if (LastAllianceFormedTime.TryGetValue(CreateKey(kingdom1, kingdom2), out var value))
            {
                hasBreakAllianceCooldown = (elapsedDaysUntilNow = value.ElapsedDaysUntilNow) < 42;
            }
            else
            {
                elapsedDaysUntilNow = default;
            }

            return hasBreakAllianceCooldown;
        }

        private static string CreateKey(IFaction faction1, IFaction faction2)
        {
            var factions = new List<IFaction> { faction1, faction2 };
            var keyArguments = factions.OrderBy(faction => faction.StringId).Select(faction => faction.StringId);
            return string.Join("+", keyArguments);
        }

        public static CampaignTime GetLastWarTimeBetweenFactions(IFaction faction1, IFaction faction2)
        {
            var stanceLink = faction1.GetStanceWith(faction2);
            if ((stanceLink?.IsNeutral ?? false) || (stanceLink?.IsAllied ?? false))
            {
                return stanceLink.PeaceDeclarationDate;
            }
            else
            {
                return CampaignTime.Zero;
            }
        }

        public static bool HasDeclareWarCooldown(IFaction faction1, IFaction faction2, out float elapsedTime)
        {
            var campaignTime = GetLastWarTimeBetweenFactions(faction1, faction2);
            if (campaignTime != CampaignTime.Zero)
            {
                elapsedTime = campaignTime.ElapsedDaysUntilNow;
                return campaignTime.ElapsedDaysUntilNow < 21;
            }
            else
            {
                elapsedTime = default;
                return false;
            }
        }

        public static bool HasPeaceProposalCooldownWithPlayerKingdom(Kingdom kingdom)
        {
            return GetLastPeaceProposalTime(kingdom) != CampaignTime.Zero && GetLastPeaceProposalTime(kingdom).ElapsedDaysUntilNow < MinimumDaysBetweenPeaceProposals;
        }

        public static bool HasExceededMinimumWarDuration(IFaction faction1, IFaction faction2, out float elapsedTime)
        {
            elapsedTime = -1f;
            var stanceLink = faction1.GetStanceWith(faction2);
            if (stanceLink?.IsAtWar ?? false)
            {
                elapsedTime = stanceLink.WarStartDate.ElapsedDaysUntilNow;
                var minimumWarDurationInDays = 21;
                return elapsedTime >= minimumWarDurationInDays;
            }
            return true;
        }

        public static CampaignTime GetLastPeaceProposalTime(Kingdom kingdom)
        {
            if (LastPeaceProposalTime.TryGetValue(kingdom, out var value))
            {
                return value;
            }
            else
            {
                return CampaignTime.Zero;
            }
        }
    }
}