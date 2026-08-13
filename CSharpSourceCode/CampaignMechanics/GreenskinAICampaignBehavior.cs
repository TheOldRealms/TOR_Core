using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    /// <summary>
    /// Comprehensive AI behavior for greenskin factions.
    /// Handles food sustainability, troop spawning, and battle rewards for AI greenskin parties.
    /// </summary>
    public class GreenskinAICampaignBehavior : CampaignBehaviorBase
    {
        // Defensive Waaagh triggers when clan is desperate
        private const int MIN_CASTLES_FOR_STABILITY = 2;
        private const int MIN_TOWNS_FOR_STABILITY = 1;

        // Troop spawning limits
        private const int OCCUPATION_BONUS_MIN_TROOPS = 1;
        private const int OCCUPATION_BONUS_MAX_TROOPS = 3;
        private const int DEFENSIVE_WAAAGH_MIN_TROOPS = 3;
        private const int DEFENSIVE_WAAAGH_MAX_TROOPS = 6;

        // Food injection thresholds
        private const int GARRISON_FOOD_THRESHOLD_DAYS = 0; // Days of food per troop (0.5f in code)
        private const int SETTLEMENT_FOOD_THRESHOLD = 300;
        private const int GARRISON_FOOD_INJECTION = 50;
        private const int SETTLEMENT_FOOD_INJECTION = 75;

        private List<CharacterObject> _greenskinTroops; // Cached list of all greenskin troops

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, OnHourlyTickSettlement);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Initialize greenskin troop list (tier 1-3 orcs and goblins)
            // Same system as player WaaaghBehavior for consistency
            _greenskinTroops = CharacterObject.All
                .WhereQ(x => x.Culture?.StringId == TORConstants.Cultures.GREENSKIN
                    && x.Tier >= 1 && x.Tier <= 3
                    && x.Occupation == Occupation.Soldier
                    && !x.IsHero
                    && (x.IsGoblin() || x.IsOrc()))
                .ToList();
        }

        #region Hourly Settlement Ticks

        private void OnHourlyTickSettlement(Settlement settlement)
        {
            if (settlement.Owner == null) return;
            
            if(!settlement.IsFortification)return;

            // Only process greenskin-owned settlements
            if (settlement.OwnerClan?.Culture?.StringId != TORConstants.Cultures.GREENSKIN) return;
            
            if(!IsTribeInDefensiveWaaagh(settlement.Owner.Clan.Kingdom))
                return;

            ProcessGreenskinFoodInjection(settlement);
        }

        /// <summary>
        /// Ensures greenskin settlements and their parties don't run out of food.
        /// Hourly check that adds meat when supplies run low.
        /// </summary>
        private void ProcessGreenskinFoodInjection(Settlement settlement)
        {
            // Add food to garrison parties if running low
            foreach (var party in settlement.Parties)
            {
                if (settlement.MapFaction == party.MapFaction)
                {
                    var partySize = party.Party.NumberOfAllMembers;
                    var foodThreshold = partySize * 0.5f; // 0.5 days of food per troop

                    if (party.ItemRoster.TotalFood < foodThreshold)
                    {
                        party.ItemRoster.AddToCounts(DefaultItems.Meat, GARRISON_FOOD_INJECTION);
                    }
                }
            }

            // Add food to settlement storage if running low
            if (settlement.ItemRoster.TotalFood < SETTLEMENT_FOOD_THRESHOLD)
            {
                settlement.ItemRoster.AddToCounts(DefaultItems.Meat, SETTLEMENT_FOOD_INJECTION);
            }
        }

        #endregion

        #region Daily AI Waaagh System

        private void OnDailyTick()
        {
            // Process all greenskin AI parties
            foreach (var mobileParty in MobileParty.AllLordParties)
            {
                // Skip player party and non-greenskin parties
                if (mobileParty.IsMainParty) continue;
                if (mobileParty.Party?.Culture?.StringId != TORConstants.Cultures.GREENSKIN) continue;
                
                if(!IsTribeInDefensiveWaaagh(mobileParty.ActualClan.Kingdom))
                    continue;

                // Check for settlement occupation bonus
                ProcessOccupationBonus(mobileParty);

                // Check for defensive Waaagh
                ProcessDefensiveWaaagh(mobileParty);
            }
        }

        /// <summary>
        /// Checks if a greenskin clan is in a desperate state and should trigger defensive Waaagh.
        /// Desperate state: clan owns fewer than 2 castles OR fewer than 1 town.
        /// </summary>
        private bool IsTribeInDefensiveWaaagh(Kingdom kingdom)
        {
            if (kingdom == null) return false;

            var castleCount = kingdom.Fiefs.Count(s => s.IsCastle);
            var townCount = kingdom.Fiefs.Count(s => s.IsTown);

            return castleCount < MIN_CASTLES_FOR_STABILITY || townCount < MIN_TOWNS_FOR_STABILITY;
        }

        /// <summary>
        /// Grants a small troop bonus to AI parties occupying greenskin settlements.
        /// Represents "da boyz" joining the Waaagh when stationed at home.
        /// Only triggers if clan is in defensive Waaagh state.
        /// </summary>
        private void ProcessOccupationBonus(MobileParty mobileParty)
        {
            // Only applies to parties currently at a settlement
            if (mobileParty.CurrentSettlement == null) return;

            // Settlement must be greenskin-owned
            if (mobileParty.CurrentSettlement.OwnerClan?.Culture?.StringId != TORConstants.Cultures.GREENSKIN) return;
            

            // Check if party has room for more troops
            var sizeLimit = Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(mobileParty.Party, false).ResultNumber;
            var currentSize = mobileParty.MemberRoster.TotalManCount;
            var freeSlots = (int)sizeLimit - currentSize;

            if (freeSlots <= 0) return;

            // Spawn 1-3 basic greenskin troops (limited by free slots)
            int troopCount = MBRandom.RandomInt(OCCUPATION_BONUS_MIN_TROOPS, OCCUPATION_BONUS_MAX_TROOPS + 1);
            troopCount = System.Math.Min(troopCount, freeSlots);

            var troopToSpawn = GetRandomGreenskinRecruit();
            if (troopToSpawn != null)
            {
                mobileParty.MemberRoster.AddToCounts(troopToSpawn, troopCount);
            }
        }

        /// <summary>
        /// Defensive Waaagh: Spawns troops for desperate greenskin clans.
        /// Triggers when clan owns fewer than 2 castles OR fewer than 1 town.
        /// Represents greenskins rallying when their territory is threatened.
        /// Black Pit and Revaz Keep clans get goblin-specific units only.
        /// </summary>
        private void ProcessDefensiveWaaagh(MobileParty mobileParty)
        {
            // Only applies to lord parties
            if (!mobileParty.IsLordParty) return;
            if (mobileParty.LeaderHero == null) return;

            var clan = mobileParty.LeaderHero.Clan;
            if (clan == null) return;

            // Check if party has room for more troops
            var sizeLimit = Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(mobileParty.Party, false).ResultNumber;
            var currentSize = mobileParty.MemberRoster.TotalManCount;
            var freeSlots = (int)sizeLimit - currentSize;

            if (freeSlots <= 0) return;

            // Check if this is a special goblin-only clan (Black Pit, Revaz Keep)
            bool isGoblinClan = clan.StringId.Contains(TORConstants.Factions.BLACK_PIT) ||
                                clan.StringId.Contains(TORConstants.Factions.REAVAZ);

            // Spawn troops when desperate - 3-6 troops (limited by free slots)
            int troopCount = MBRandom.RandomInt(DEFENSIVE_WAAAGH_MIN_TROOPS, DEFENSIVE_WAAAGH_MAX_TROOPS + 1);
            troopCount = System.Math.Min(troopCount, freeSlots);

            // Spawn appropriate troops based on clan type
            for (int i = 0; i < troopCount; i++)
            {
                var troopToSpawn = isGoblinClan ? GetRandomGoblinRecruit() : GetRandomGreenskinRecruit();
                if (troopToSpawn != null)
                {
                    mobileParty.MemberRoster.AddToCounts(troopToSpawn, 1);
                }
            }
        }

        /// <summary>
        /// Returns a random greenskin troop (tier 1-3).
        /// Uses all available greenskin troops from the game data, same as player Waaagh.
        /// </summary>
        private CharacterObject GetRandomGreenskinRecruit()
        {
            // Lazy initialize if needed (should already be initialized in OnSessionLaunched)
            if (_greenskinTroops == null || _greenskinTroops.Count == 0)
            {
                _greenskinTroops = CharacterObject.All
                    .WhereQ(x => x.Culture?.StringId == TORConstants.Cultures.GREENSKIN
                        && x.Tier >= 1 && x.Tier <= 3
                        && x.Occupation == Occupation.Soldier
                        && !x.IsHero
                        && (x.IsGoblin() || x.IsOrc()))
                    .ToList();
            }

            if (_greenskinTroops.Count == 0)
            {
                // Fallback to basic goblin if no troops found
                return MBObjectManager.Instance.GetObject<CharacterObject>("tor_gs_goblin");
            }

            // Return random troop from the full list
            return _greenskinTroops.GetRandomElement();
        }

        /// <summary>
        /// Returns a random goblin unit for Black Pit and Revaz Keep factions.
        /// Limited to goblin archers and goblin stikkas only.
        /// </summary>
        private CharacterObject GetRandomGoblinRecruit()
        {
            // 50/50 split between goblin archer and goblin stikka
            if (MBRandom.RandomFloat < 0.5f)
            {
                return MBObjectManager.Instance.GetObject<CharacterObject>("tor_gs_goblin_archer");
            }
            else
            {
                return MBObjectManager.Instance.GetObject<CharacterObject>("tor_gs_goblin_stikka");
            }
        }

        #endregion

        #region Battle Rewards

        /// <summary>
        /// Grants meat to AI greenskin parties when they win battles.
        /// </summary>
        private void OnMapEventEnded(MapEvent mapEvent)
        {
            // Skip if no winner or if player is involved (player handled separately)
            if (mapEvent.DefeatedSide == BattleSideEnum.None) return;
            if (mapEvent.IsPlayerMapEvent && !Hero.MainHero.IsEnlisted()) return;

            // Process each winning party
            var winningParties = mapEvent.PartiesOnSide(mapEvent.WinningSide);

            foreach (var partyBase in winningParties)
            {
                // Only process mobile greenskin parties
                if (partyBase?.Party.MobileParty?.LeaderHero == null) continue;
                if (partyBase.Party.MobileParty.LeaderHero.Culture?.StringId != TORConstants.Cultures.GREENSKIN) continue;

                var mobileParty = partyBase.Party.MobileParty;
                if (mobileParty == MobileParty.MainParty) continue;

                // Calculate meat from battle using shared battle reward model
                
                var rewardModel = (TORBattleRewardModel) Campaign.Current.Models.BattleRewardModel;
                if(rewardModel!=null)
                {
                    int meatGained = rewardModel.CalculateMeatFromBattle(mapEvent);

                    if (meatGained > 0)
                    {
                        // Add meat directly to party inventory
                        mobileParty.ItemRoster.AddToCounts(DefaultItems.Meat, meatGained);
                    }
                }

            }
        }

        #endregion

        public override void SyncData(IDataStore dataStore)
        {
            // No data to sync
        }
    }
}
