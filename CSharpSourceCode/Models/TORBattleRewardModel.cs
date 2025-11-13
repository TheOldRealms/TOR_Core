using System.Collections.Generic;
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

namespace TOR_Core.Models
{
    public class TORBattleRewardModel : DefaultBattleRewardModel
    {
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

        public override float GetAITradePenalty()
        {
            return 0.3f;
        }


        public float DropChanceForMagicalItemsLoot(CharacterObject troop, int count, float lootPercentage)
        {
            var value = (GetDropChanceForTroop(troop) * count) * (lootPercentage / 100);
            return value;
        }

        public override ExplainedNumber CalculateRenownGain(PartyBase party, float renownValueOfBattle, float contributionShare)
        {
            var result = base.CalculateRenownGain(party, renownValueOfBattle, contributionShare);

            if (party == PartyBase.MainParty)
            {
                CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref result, PassiveEffectType.BattleRenownGain, true);
            }

            return result;
        }


        public int GetTraitCountForTroops(CharacterObject character, int count, float playerEarnedLootPercentage)
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

            var amount = (int)(count * playerEarnedLootPercentage);

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
            var text = GameTexts.FindText("str_tor_magical_items_trait_rarity", traitAmount.ToString());
            return text.ToString();
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
    }
}