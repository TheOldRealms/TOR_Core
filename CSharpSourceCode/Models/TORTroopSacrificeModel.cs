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
    /// <summary>
    /// Extends the vanilla troop-sacrifice model so the Diversion perk can
    /// substitute prisoners for the regular-soldier cost of the "try to
    /// break away" encounter option. Vanilla's own methods stay untouched;
    /// we add a prisoner-sacrifice-count query and a matching remove
    /// helper. The menu side reads these; vanilla's troop sacrifice is
    /// bypassed by not calling the vanilla accept consequence.
    /// </summary>
    public class TORTroopSacrificeModel : DefaultTroopSacrificeModel
    {
        /// <summary>How many prisoners the Diversion branch spends to
        /// replicate the same break-away effect as vanilla's troop
        /// sacrifice. Uses the same vanilla formula as
        /// <see cref="GetNumberOfTroopsSacrificedForTryingToGetAway"/> so
        /// the cost scales identically with force ratio and party size;
        /// returns <c>-1</c> if the main party doesn't hold enough
        /// prisoners to cover the full count. Override to tune the
        /// prisoner-side cost independently of the troop-side cost.</summary>
        public virtual int GetNumberOfPrisonersSacrificedForDiversion(
            BattleSideEnum playerBattleSide, MapEvent mapEvent)
        {
            int baseCount = GetNumberOfTroopsSacrificedForTryingToGetAway(playerBattleSide, mapEvent);
            if (baseCount < 0) return -1;
            int available = MobileParty.MainParty?.PrisonRoster?.TotalRegulars ?? 0;
            return baseCount <= available ? baseCount : -1;
        }

        /// <summary>Prisoner-cost mirror of
        /// <see cref="GetLostTroopCountForBreakingInBesiegedSettlement"/>.
        /// Returns <c>-1</c> if the main party doesn't hold enough prisoners
        /// to cover vanilla's troop-loss count for breaking into a besieged
        /// settlement. Lets the Diversion perk substitute prisoners for the
        /// break-in sacrifice the same way it does for the encounter
        /// escape.</summary>
        public virtual int GetNumberOfPrisonersSacrificedForBreakInSiege(
            MobileParty party, SiegeEvent siegeEvent)
        {
            int baseCount = GetLostTroopCountForBreakingInBesiegedSettlement(party, siegeEvent)
                             .RoundedResultNumber;
            if (baseCount <= 0) return -1;
            int available = party?.PrisonRoster?.TotalRegulars ?? 0;
            return baseCount <= available ? baseCount : -1;
        }

        /// <summary>Remove <paramref name="count"/> prisoners from the
        /// main party's prisoner roster. Mirrors vanilla's
        /// <c>SacrificeTroopsWithRatio</c> picker: lowest-level non-hero
        /// prisoner wins each pick, with a small random tie-breaker.
        /// No-op when the party has no eligible prisoners.</summary>
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
