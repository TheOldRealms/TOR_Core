using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORBattleRewardModel : DefaultBattleRewardModel
    {
        // Retreat routing morale constants (moved from RetreatPatches)
        private const float ROUND_LOSS_MORALE_CHANGE_DEFAULT = -3f; // vanilla
        private const float ROUND_LOSS_MORALE_CHANGE_BELOW_40 = -2f;
        private const float ROUND_LOSS_MORALE_CHANGE_BELOW_30 = -1f;
        private const float ROUND_LOSS_MORALE_THRESHOLD_40 = 40f;
        private const float ROUND_LOSS_MORALE_THRESHOLD_30 = 30f;
        private const float VANILLA_MORALE_CHANGE_WHEN_OPPONENT_IS_RETREATING = -3f;
        private const float VANILLA_MORALE_CHANGE_WHEN_THIS_SIDE_IS_RETREATING = -1f;

        /// <summary>
        /// Custom morale change on round victory for retreat routing system.
        /// Reduces morale loss at low morale to prevent instant routs.
        /// </summary>
        public override float CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner)
        {
            if (!party.IsMobile || roundWinner == BattleSideEnum.None)
            {
                return 0f;
            }

            var mapEvent = partySide.MapEvent;
            if (!ShouldUseRetreatRouting(mapEvent))
            {
                return base.CalculateMoraleChangeOnRoundVictory(party, partySide, roundWinner);
            }

            // Once retreat has started, use vanilla behavior
            if (mapEvent.RetreatingSide != BattleSideEnum.None)
            {
                if (mapEvent.RetreatingSide == party.OpponentSide)
                {
                    return VANILLA_MORALE_CHANGE_WHEN_OPPONENT_IS_RETREATING;
                }
                else if (mapEvent.RetreatingSide == party.Side)
                {
                    return VANILLA_MORALE_CHANGE_WHEN_THIS_SIDE_IS_RETREATING;
                }
                return 0f;
            }

            // Winner of round gets no morale change
            if (roundWinner == party.Side)
            {
                return 0f;
            }

            // Loser gets reduced morale loss at low morale
            var currentMorale = party.MobileParty.Morale;

            if (currentMorale < ROUND_LOSS_MORALE_THRESHOLD_30)
            {
                return ROUND_LOSS_MORALE_CHANGE_BELOW_30;
            }

            if (currentMorale < ROUND_LOSS_MORALE_THRESHOLD_40)
            {
                return ROUND_LOSS_MORALE_CHANGE_BELOW_40;
            }

            return ROUND_LOSS_MORALE_CHANGE_DEFAULT;
        }

        private static bool ShouldUseRetreatRouting(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return false;

            if (mapEvent.EventType == MapEvent.BattleTypes.FieldBattle)
                return true;

            return mapEvent.MapEventSettlement?.SiegeEvent != null;
        }

        //does this have the same effect as the commented out method below (it no longer exists)?
        //We are trying to prevent AI from saving prisoners as members to cultural mismatch and slowing down parties.
        public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement)
        {
            var baseResult = base.GetLootPrisonerChances(winnerParties, prisonerElement);
            var result = new MBReadOnlyList<KeyValuePair<MapEventParty, float>>();
            foreach (var kvp in baseResult)
            {
                result.Add(new KeyValuePair<MapEventParty, float>(kvp.Key, 0f));
            }
            return result;
        }

        //public override float GetPartySavePrisonerAsMemberShareProbability(PartyBase winnerParty, float lootAmount) => 0f;

        //the % of an item's value that the AI gets when it liquidates looted equipment after winning a battle, ish
        public override float GetAITradePenalty()
        {
            return 0.1f;
        }


        public float DropChanceForMagicalItemsLoot(CharacterObject troop, int count, float playerEarnedLootRate)
        {
            var value = (GetDropChanceForTroop(troop) * count) * playerEarnedLootRate;
            return value;
        }

        public override ExplainedNumber CalculateRenownGain(PartyBase party, float renownValueOfBattle, float contributionShare, float renownMultiplier, bool includeDescriptions)
        {
            var result = base.CalculateRenownGain(party, renownValueOfBattle, contributionShare, renownMultiplier, includeDescriptions);

            if (party == PartyBase.MainParty)
            {
                CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref result, PassiveEffectType.BattleRenownGain, true);

                // Myrmidia blessing - bonus renown from battles
                if (MobileParty.MainParty.HasBlessing("cult_of_myrmidia"))
                {
                    result.AddFactor(0.10f);
                }
            }

            return result;
        }
        public override ExplainedNumber CalculateInfluenceGain(PartyBase party, float influenceValueOfBattle, float contributionShare, float influenceMultiplier, bool includeDescriptions)
        {
            var result = base.CalculateInfluenceGain(party, influenceValueOfBattle, contributionShare, influenceMultiplier, includeDescriptions);

            if (ShouldReduceBanditBattleInfluence(party))
            {
                result.AddFactor(-2f / 3f, new TaleWorlds.Localization.TextObject("{=!}bandit battle"));
            }

            return result;
        }
        public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties)
        {
            var baseResult = base.GetLootGoldChances(winnerParties);

            if (!Hero.MainHero.IsEnlisted())
            {
                return baseResult;
            }

            var result = new MBReadOnlyList<KeyValuePair<MapEventParty, float>>();
            float remainingWeight = 0f;

            foreach (var kvp in baseResult)
            {
                if (kvp.Key?.Party == PartyBase.MainParty)
                {
                    continue;
                }

                remainingWeight += kvp.Value;
            }

            if (remainingWeight <= 0f)
            {
                return result;
            }

            foreach (var kvp in baseResult)
            {
                if (kvp.Key?.Party == PartyBase.MainParty)
                {
                    continue;
                }

                result.Add(new KeyValuePair<MapEventParty, float>(kvp.Key, kvp.Value / remainingWeight));
            }

            return result;
        }

        private static bool ShouldReduceBanditBattleInfluence(PartyBase party)
        {
            var mapEvent = party.MapEvent;

            if (mapEvent.MapEventSettlement != null)
            {
                return false;
            }

            var opposingSide = mapEvent.GetMapEventSide(party.OpponentSide);
            var opposingSideHasHeroParty = opposingSide.Parties.Any(x => x.Party.LeaderHero != null);

            return !opposingSideHasHeroParty;
        }

        public int GetTraitCountForTroops(CharacterObject character, int count, float playerEarnedLootRate)
        {
            var t = GetDropChanceForMagicalItemTrait(character);
            var traits = 0;

            if (character.IsHero)
            {
                var chance = MBRandom.RandomFloatNormal;
                if (chance < t)
                {
                    traits = MBRandom.RandomInt(0, MaximumFindableTraitsOnItems());
                }

                return traits;
            }

            var amount = (int)(count * playerEarnedLootRate);

            for (var i = 0; i < amount; i++)
            {
                var chance = MBRandom.RandomFloatRanged(0, 1);
                if (chance < t)
                {
                    traits++;
                }
            }

            return Mathf.Clamp(traits, 0, MaximumFindableTraitsOnItems());
        }

        public string GetNameModifierForTraits(int traitAmount)
        {
            return TORTextHelper.GetText("tor_magical_items_trait_rarity", traitAmount.ToString(), "Magical");
            
        }

        public int MaximumFindableTraitsOnItems()
        {
            return 3;
        }

        private float GetDropChanceForTroop(CharacterObject troop)
        {
            var value = 0f;
            if (troop.Occupation == Occupation.Bandit)
            {
                return 0.0001f;
            }
            if (troop.IsEliteTroop())
            {
                value = 0.0005f;
            }
            if (troop.IsHero)
            {
                value = 0.2f;
            }

            value += (troop.Level * 0.0005f);

            return value;
        }

        private float GetDropChanceForMagicalItemTrait(CharacterObject troop)
        {
            var value = 0f;
            if (troop.Occupation == Occupation.Bandit)
            {
                return 0.0005f;
            }
            if (troop.IsEliteTroop())
            {
                value = 0.05f;
            }
            if (troop.IsHero)
            {
                value = 0.25f;
            }

            value += (troop.Level * 0.0005f);

            return value;
        }

        /// <summary>
        /// Calculates meat gained from battle based on killed troops.
        /// Used for both player and AI greenskin parties.
        /// </summary>
        /// <param name="mapEvent">The battle map event</param>
        /// <returns>Amount of meat gained from killed enemies</returns>
        public int CalculateMeatFromBattle(MapEvent mapEvent)
        {
            int rawMeat = 0;

            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);

            // Collect all potential meat from killed troops
            foreach (var party in partiesOnSide)
            {
                var killedTroops = party.Troops.Where(x => x.IsKilled);

                foreach (var killed in killedTroops)
                {
                    // No meat from undead
                    if (killed.Troop.IsUndead())
                        continue;

                    // 5 meat for large targets (mounted/trolls/minotaurs)
                    if (killed.Troop.IsLargeTarget())
                    {
                        rawMeat += 5;
                        continue;
                    }

                    // 2 meat for beastmen
                    if (killed.Troop.IsBeastman())
                    {
                        rawMeat += 2;
                        continue;
                    }

                    // 1 meat for regular troops
                    rawMeat += 1;
                }
            }

            // Apply random 15%-35% conversion rate (not all kills yield meat)
            float meatPercent = MBRandom.RandomFloatRanged(0.15f, 0.35f);
            int totalMeat = (int)(rawMeat * meatPercent);

            return totalMeat;
        }
    }
}