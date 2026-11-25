using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.Models
{
    /// <summary>Troop-sacrifice model extension letting the Diversion perk substitute prisoners for regular troops on break-away costs.</summary>
    public class TORTroopSacrificeModel : DefaultTroopSacrificeModel
    {
        /// <summary>Prisoner count needed for a Diversion break-away. Returns -1 when the main party lacks enough prisoners.</summary>
        public virtual int GetNumberOfPrisonersSacrificedForDiversion(
            BattleSideEnum playerBattleSide, MapEvent mapEvent)
        {
            int baseCount = GetNumberOfTroopsSacrificedForTryingToGetAway(playerBattleSide, mapEvent);
            if (baseCount < 0) return -1;
            int available = MobileParty.MainParty?.PrisonRoster?.TotalRegulars ?? 0;
            return baseCount <= available ? baseCount : -1;
        }

        /// <summary>Prisoner count needed for a Diversion break-in on a besieged settlement. Returns -1 when the main party lacks enough prisoners.</summary>
        public virtual int GetNumberOfPrisonersSacrificedForBreakInSiege(
            MobileParty party, SiegeEvent siegeEvent)
        {
            int baseCount = GetLostTroopCountForBreakingInBesiegedSettlement(party, siegeEvent)
                             .RoundedResultNumber;
            if (baseCount <= 0) return -1;
            int available = party?.PrisonRoster?.TotalRegulars ?? 0;
            return baseCount <= available ? baseCount : -1;
        }

        /// <summary>Remove <paramref name="count"/> prisoners from the main party's prisoner roster, lowest-level non-hero first with a small random tiebreaker.</summary>
        public virtual void ApplyPrisonerDiversion(int count)
        {
            var party = MobileParty.MainParty;
            if (party?.PrisonRoster == null || count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                TroopRosterElement pick = default;
                float lowest = float.MaxValue;
                bool found = false;
                foreach (var element in party.PrisonRoster.GetTroopRoster())
                {
                    if (element.Character == null || element.Character.IsHero) continue;
                    if (element.Number <= 0) continue;
                    float score = element.Character.Level
                                  - (element.WoundedNumber > 0 ? 0.5f : 0f)
                                  - MBRandom.RandomFloat * 0.5f;
                    if (score < lowest)
                    {
                        lowest = score;
                        pick = element;
                        found = true;
                    }
                }
                if (!found) break;
                party.PrisonRoster.AddToCounts(pick.Character, -1, false,
                    pick.WoundedNumber > 0 ? -1 : 0);
            }
        }
    }
}
