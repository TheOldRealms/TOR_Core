using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.HarmonyPatches.AutoResolve
{
    // replaces autores runaway chance with routing based on terrain/enemy comp/formation and tier; and replaces morale loss with a semi dynamic one
    // adjusts post retreat handling, retreating parties can no longer prevent the player from getting prisoners and loots
    public static class TORAutoResolveRetreatPatches
    {
        // round morale loss for losing side
        private const float ROUND_LOSS_MORALE_CHANGE_DEFAULT = -3f; // vanilla
        private const float ROUND_LOSS_MORALE_CHANGE_BELOW_40 = -2f;
        private const float ROUND_LOSS_MORALE_CHANGE_BELOW_30 = -1f;

        private const float ROUND_LOSS_MORALE_THRESHOLD_40 = 40f;
        private const float ROUND_LOSS_MORALE_THRESHOLD_30 = 30f;

        // vanilla values for pursuit stage
        private const float VANILLA_MORALE_CHANGE_WHEN_OPPONENT_IS_RETREATING = -3f;
        private const float VANILLA_MORALE_CHANGE_WHEN_THIS_SIDE_IS_RETREATING = -1f;

        // prevent retreats if the enemy is clearly stronger yet you decided to fight
        private const int RUN_AWAY_DISABLED_MAX_SIDE_TROOPS = 120;
        private const int RUN_AWAY_DISABLED_ENEMY_MULTIPLIER = 5; // if they are at least 5 times your size

        // party formed right after a retreat
        private const float POST_RETREAT_TARGET_MORALE = 40f;
        private const float POST_RETREAT_DISORGANIZED_HOURS = 3f;

        // same attacker cannot trigger another runaway for this party for X hours
        private const float RECENT_RETREAT_COOLDOWN_HOURS = 12f;

        // route attempts done per call
        // morale < 20, attempts = ticks/5. then for every -2 morale +5% attempts
        private const float ROUTE_ATTEMPTS_BASE_FRACTION_OF_SIM_TICKS_AT_MORALE_20 = 0.20f;
        private const float ROUTE_ATTEMPTS_BONUS_MULTIPLIER_PER_2_MORALE_BELOW_20 = 0.05f;
        private const float ROUTE_ATTEMPTS_START_MORALE = 20f;
        private const float ROUTE_ATTEMPTS_MORALE_STEP = 2f;

        // maybe increase this value if you think defensive sieges should be more decisive
        private const int MAX_ROUTE_ATTEMPTS_PER_SIDE_PER_CHECK = 64;

        // tier bias
        private const float ROUTE_SELECTION_TIER_POWER = 1.35f;

        // skip units whose computed chance is too tiny
        private const float MIN_ROUTE_CHANCE_TO_ROLL = 0.04f;

        // selection weighting
        private const float ROUTE_SELECTION_WEIGHT_CHANCE = 0.70f;
        private const float ROUTE_SELECTION_WEIGHT_WOUNDED_RATIO = 0.30f; // if that unit type is doing especially bad

        // losing side formation values, how likely they are to manage to escape
        private const float RETREAT_VALUE_HORSE_ARCHER = 0.80f;
        private const float RETREAT_VALUE_CAVALRY = 0.70f;
        private const float RETREAT_VALUE_ARCHER = 0.40f;
        private const float RETREAT_VALUE_INFANTRY = 0.30f;

        // winning side composition penalties for retreating side, how fast they can chase after retreating soldiers
        private const float ENEMY_VALUE_HORSE_ARCHER = -0.90f;
        private const float ENEMY_VALUE_CAVALRY = -0.60f;
        private const float ENEMY_VALUE_ARCHER = -0.60f;
        private const float ENEMY_VALUE_INFANTRY = -0.10f;

        // valor trait
        private const float VALOR_RETREAT_REDUCTION_PER_LEVEL = 0.05f;

        private static readonly AccessTools.FieldRef<MobileParty, CampaignTime> DisorganizedUntilTime =
            AccessTools.FieldRefAccess<MobileParty, CampaignTime>("_disorganizedUntilTime");

        private static readonly AccessTools.FieldRef<MapEvent, BattleSideEnum> MapEventRetreatingSide =
            AccessTools.FieldRefAccess<MapEvent, BattleSideEnum>("<RetreatingSide>k__BackingField");

        private static readonly AccessTools.FieldRef<MapEvent, int> MapEventPursuitRoundNumber =
            AccessTools.FieldRefAccess<MapEvent, int>("<PursuitRoundNumber>k__BackingField");

        private static readonly AccessTools.FieldRef<MapEventSide, List<UniqueTroopDescriptor>> SimulationTroopList =
            AccessTools.FieldRefAccess<MapEventSide, List<UniqueTroopDescriptor>>("_simulationTroopList");

        private static readonly AccessTools.FieldRef<MapEventSide, int> SelectedSimulationTroopIndex =
             AccessTools.FieldRefAccess<MapEventSide, int>("_selectedSimulationTroopIndex");

        private static readonly AccessTools.FieldRef<MapEventSide, UniqueTroopDescriptor> SelectedSimulationTroopDescriptor =
            AccessTools.FieldRefAccess<MapEventSide, UniqueTroopDescriptor>("_selectedSimulationTroopDescriptor");

        private static readonly AccessTools.FieldRef<MapEventSide, CharacterObject> SelectedSimulationTroop =
            AccessTools.FieldRefAccess<MapEventSide, CharacterObject>("_selectedSimulationTroop");

        // store routed troops so they can be restored afterwards
        private static readonly Dictionary<PartyBase, EscapedTroopsCache> EscapedTroopsByParty =
            new Dictionary<PartyBase, EscapedTroopsCache>();

        private static readonly Dictionary<MobileParty, RecentRetreatInfo> RecentRetreatInfoByParty =
            new Dictionary<MobileParty, RecentRetreatInfo>();

        private static readonly Dictionary<MapEvent, PendingRetreatCooldown> PendingRetreatCooldownByMapEvent =
            new Dictionary<MapEvent, PendingRetreatCooldown>();

        private static readonly Dictionary<MapEvent, RetreatRoutingState> RetreatRoutingStateByMapEvent =
            new Dictionary<MapEvent, RetreatRoutingState>();

        private static readonly Dictionary<MapEvent, List<PendingFugitivePartySpawn>> PendingFugitivePartySpawnsByMapEvent =
            new Dictionary<MapEvent, List<PendingFugitivePartySpawn>>();

        // for vanilla overwriting later
        private static readonly HashSet<MobileParty> PostRetreatPartiesNeedingFinalOverrides =
            new HashSet<MobileParty>();

        private static readonly MethodInfo BattleObserverGetterMethod =
            typeof(MapEvent)
                .GetProperty("BattleObserver", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetGetMethod(nonPublic: true);

        private static readonly Func<MapEvent, IBattleObserver> BattleObserverGetter =
            BattleObserverGetterMethod != null
                ? (Func<MapEvent, IBattleObserver>)Delegate.CreateDelegate(
                    typeof(Func<MapEvent, IBattleObserver>),
                    firstArgument: null,
                    method: BattleObserverGetterMethod,
                    throwOnBindFailure: false)
                : null;

        private sealed class RetreatRoutingState
        {
            public bool IsAttackerInRetreatMode;
            public bool IsDefenderInRetreatMode;

            // CheckSideRunAway same round being called multiple times
            public int LastProcessedWonRoundCountForAttacker;
            public int LastProcessedWonRoundCountForDefender;

            // reuse buffers
            public readonly HashSet<UniqueTroopDescriptor> SimulationTroopsSet = new HashSet<UniqueTroopDescriptor>();

            public readonly List<RoutableTroopCandidate> CandidateBuffer = new List<RoutableTroopCandidate>(64);
            public readonly List<RoutableTroopCandidate> SelectedCandidateBuffer = new List<RoutableTroopCandidate>(64);
            public readonly Dictionary<CharacterObject, float> WoundedRatiosByTroop = new Dictionary<CharacterObject, float>(64);
        }

        private readonly struct PendingRetreatCooldown
        {
            public readonly List<MobileParty> RetreatingParties;
            public readonly MobileParty AttackerParty;

            public PendingRetreatCooldown(List<MobileParty> retreatingParties, MobileParty attackerParty)
            {
                RetreatingParties = retreatingParties;
                AttackerParty = attackerParty;
            }
        }

        private sealed class PendingFugitivePartySpawn
        {
            public readonly Hero Hero;
            public readonly Dictionary<CharacterObject, int> EscapedTroops;

            public PendingFugitivePartySpawn(Hero hero, Dictionary<CharacterObject, int> escapedTroops)
            {
                Hero = hero;
                EscapedTroops = new Dictionary<CharacterObject, int>(escapedTroops);
            }
        }

        private sealed class EscapedTroopsCache
        {
            public readonly bool IsLordParty;
            public readonly Hero OriginalLeaderHero;
            public readonly Dictionary<CharacterObject, int> EscapedTroops;

            public EscapedTroopsCache(PartyBase party)
            {
                IsLordParty = party.IsMobile && party.MobileParty.IsLordParty;
                OriginalLeaderHero = IsLordParty ? party.LeaderHero : null;
                EscapedTroops = new Dictionary<CharacterObject, int>();
            }
        }

        private readonly struct RecentRetreatInfo
        {
            public readonly MobileParty AttackerParty;
            public readonly CampaignTime CooldownUntilTime;

            public RecentRetreatInfo(MobileParty attackerParty, CampaignTime cooldownUntilTime)
            {
                AttackerParty = attackerParty;
                CooldownUntilTime = cooldownUntilTime;
            }
        }

        private readonly struct RoutableTroopCandidate
        {
            public readonly UniqueTroopDescriptor TroopDescriptor;
            public readonly PartyBase TroopParty;
            public readonly CharacterObject Troop;
            public readonly float Chance;
            public readonly float Weight;

            public RoutableTroopCandidate(
                UniqueTroopDescriptor troopDescriptor,
                PartyBase troopParty,
                CharacterObject troop,
                float chance,
                float weight)
            {
                TroopDescriptor = troopDescriptor;
                TroopParty = troopParty;
                Troop = troop;
                Chance = chance;
                Weight = weight;
            }
        }

        private static float GetTerrainRetreatModifier(TerrainType terrainType)
        {
            // everything else is 0 advantage - though there shouldn't be any
            return terrainType switch
            {
                TerrainType.UnderBridge => 0.50f,
                TerrainType.Cliff => -0.90f,
                TerrainType.Bridge => 0.50f,
                TerrainType.RuralArea => 0.80f,
                TerrainType.Swamp => 0.30f,
                TerrainType.Dune => -0.70f,
                TerrainType.Plain => -0.80f,
                TerrainType.Desert => -0.80f,
                TerrainType.Snow => 0.60f,
                TerrainType.Forest => 0.80f,
                TerrainType.Steppe => -0.60f,
                TerrainType.Fording => 0.10f,
                TerrainType.Mountain => 0.70f,
                TerrainType.Lake => 0.10f,
                TerrainType.Water => 0.10f,
                TerrainType.River => 0.10f,
                TerrainType.Canyon => -0.50f,
                _ => 0f,
            };
        }

        private static float GetRetreatFormationValue(FormationClass formationClass)
        {
            return formationClass switch
            {
                FormationClass.HorseArcher => RETREAT_VALUE_HORSE_ARCHER,
                FormationClass.Cavalry => RETREAT_VALUE_CAVALRY,
                FormationClass.Ranged => RETREAT_VALUE_ARCHER,
                FormationClass.Infantry => RETREAT_VALUE_INFANTRY,
                _ => 0f,
            };
        }

        private static float GetCompositionModifier(
            MapEventSide side,
            float horseArcherValue,
            float cavalryValue,
            float archerValue,
            float infantryValue)
        {
            var totalCount = 0;
            var weightedSum = 0f;

            foreach (var mapEventParty in side.Parties)
            {
                var troopRoster = mapEventParty.Party.MemberRoster.GetTroopRoster();
                for (var i = 0; i < troopRoster.Count; i++)
                {
                    var element = troopRoster[i];
                    var healthyCount = element.Number - element.WoundedNumber;
                    if (healthyCount <= 0)
                        continue;

                    totalCount += healthyCount;

                    var formationClass = element.Character.GetFormationClass();
                    weightedSum += formationClass switch
                    {
                        FormationClass.HorseArcher => healthyCount * horseArcherValue,
                        FormationClass.Cavalry => healthyCount * cavalryValue,
                        FormationClass.Ranged => healthyCount * archerValue,
                        FormationClass.Infantry => healthyCount * infantryValue,
                        _ => 0f,
                    };
                }
            }

            return totalCount > 0 ? weightedSum / totalCount : 0f;
        }

        private static int GetSimulationTicksForSide(MapEvent mapEvent, BattleSideEnum side)
        {
            var (defenderTicks, attackerTicks) =
                Campaign.Current.Models.CombatSimulationModel.GetSimulationTicksForBattleRound(mapEvent);

            return side == BattleSideEnum.Attacker ? attackerTicks : defenderTicks;
        }

        private static int GetRouteAttemptCountForSide(MapEventSide retreatingSide, int eligibleCandidateCount)
        {
            if (eligibleCandidateCount <= 0)
                return 0;

            var simulationTicksForSide = GetSimulationTicksForSide(retreatingSide.MapEvent, retreatingSide.MissionSide);
            if (simulationTicksForSide <= 0)
                return 0;

            var baseAttempts = (int)Math.Ceiling(simulationTicksForSide * ROUTE_ATTEMPTS_BASE_FRACTION_OF_SIM_TICKS_AT_MORALE_20);
            if (baseAttempts < 1)
                baseAttempts = 1;

            var moraleBelowThreshold = ROUTE_ATTEMPTS_START_MORALE - retreatingSide.GetSideMorale();
            var stepsBelow = moraleBelowThreshold > 0f
                ? (int)Math.Floor(moraleBelowThreshold / ROUTE_ATTEMPTS_MORALE_STEP)
                : 0;

            var multiplier = 1f + (stepsBelow * ROUTE_ATTEMPTS_BONUS_MULTIPLIER_PER_2_MORALE_BELOW_20);
            var attemptCount = (int)Math.Ceiling(baseAttempts * multiplier);

            attemptCount = Math.Min(attemptCount, MAX_ROUTE_ATTEMPTS_PER_SIDE_PER_CHECK);
            return Math.Min(attemptCount, eligibleCandidateCount);
        }

        private static void BuildWoundedRatioByTroop(PartyBase troopParty, Dictionary<CharacterObject, float> woundedRatiosByTroop)
        {
            woundedRatiosByTroop.Clear();

            var troopRoster = troopParty.MemberRoster.GetTroopRoster();
            for (var i = 0; i < troopRoster.Count; i++)
            {
                var element = troopRoster[i];
                if (element.Number <= 0)
                    continue;

                woundedRatiosByTroop[element.Character] = element.WoundedNumber / (float)element.Number;
            }
        }

        private static void SelectWeightedCandidatesWithoutReplacement(
            List<RoutableTroopCandidate> candidates,
            int selectionCount,
            List<RoutableTroopCandidate> selectedCandidates)
        {
            selectedCandidates.Clear();

            for (var selectionIndex = 0; selectionIndex < selectionCount && candidates.Count > 0; selectionIndex++)
            {
                var totalWeight = 0f;
                for (var i = 0; i < candidates.Count; i++)
                {
                    totalWeight += candidates[i].Weight;
                }

                if (totalWeight <= 0f)
                    return;

                var selectionValue = MBRandom.RandomFloat * totalWeight;

                for (var i = 0; i < candidates.Count; i++)
                {
                    selectionValue -= candidates[i].Weight;
                    if (selectionValue > 0f)
                        continue;

                    selectedCandidates.Add(candidates[i]);
                    candidates.RemoveAt(i);
                    break;
                }
            }
        }

        private static bool IsRecentRetreatCooldownActive(MapEventSide retreatingSide)
        {
            var retreatLeaderParty = retreatingSide.LeaderParty;
            var attackerLeaderParty = retreatingSide.OtherSide.LeaderParty;

            if (retreatLeaderParty == null || attackerLeaderParty == null)
                return false;

            if (!retreatLeaderParty.IsMobile || !attackerLeaderParty.IsMobile)
                return false;

            var retreatingMobileParty = retreatLeaderParty.MobileParty;
            if (retreatingMobileParty == null)
                return false;

            if (!RecentRetreatInfoByParty.TryGetValue(retreatingMobileParty, out var recentRetreatInfo))
                return false;

            if (CampaignTime.Now >= recentRetreatInfo.CooldownUntilTime)
            {
                RecentRetreatInfoByParty.Remove(retreatingMobileParty);
                return false;
            }

            var currentAttackerMobileParty = attackerLeaderParty.MobileParty;
            return currentAttackerMobileParty == recentRetreatInfo.AttackerParty;
        }

        private static bool IsRetreatTriggerSatisfied(MapEvent mapEvent, MapEventSide retreatingSide)
        {
            if (mapEvent.UpdateCount < 8)
                return false;

            if (!retreatingSide.LeaderParty.IsMobile)
                return false;

            if (retreatingSide.GetSideMorale() > ROUTE_ATTEMPTS_START_MORALE)
                return false;

            if (mapEvent.WonRounds.Count < 4)
                return false;

            for (var i = 0; i < 4; i++)
            {
                var wonRoundSide = mapEvent.WonRounds[mapEvent.WonRounds.Count - 1 - i];
                if (wonRoundSide == retreatingSide.MissionSide || wonRoundSide == BattleSideEnum.None)
                    return false;
            }

            return true;
        }

        private static RetreatRoutingState GetOrCreateRetreatRoutingState(MapEvent mapEvent)
        {
            if (!RetreatRoutingStateByMapEvent.TryGetValue(mapEvent, out var state))
            {
                state = new RetreatRoutingState();
                RetreatRoutingStateByMapEvent[mapEvent] = state;
            }

            return state;
        }

        private static bool IsRetreatModeActiveForSide(MapEventSide side, RetreatRoutingState state)
        {
            return side.MissionSide == BattleSideEnum.Attacker
                ? state.IsAttackerInRetreatMode
                : state.IsDefenderInRetreatMode;
        }

        private static void ActivateRetreatModeForSide(MapEventSide side, RetreatRoutingState state)
        {
            if (side.MissionSide == BattleSideEnum.Attacker)
            {
                state.IsAttackerInRetreatMode = true;
                return;
            }

            if (side.MissionSide == BattleSideEnum.Defender)
            {
                state.IsDefenderInRetreatMode = true;
            }
        }

        private static bool ShouldAttemptRoutingAfterThisRoundLoss(MapEvent mapEvent, MapEventSide side, RetreatRoutingState state)
        {
            var wonRoundsCount = mapEvent.WonRounds.Count;
            if (wonRoundsCount <= 0)
                return false;

            var lastWinnerSide = mapEvent.WonRounds[wonRoundsCount - 1];
            if (lastWinnerSide == BattleSideEnum.None)
                return false;

            // only attempt routing if this side lost the last round
            if (lastWinnerSide == side.MissionSide)
                return false;

            if (side.MissionSide == BattleSideEnum.Attacker)
            {
                if (state.LastProcessedWonRoundCountForAttacker == wonRoundsCount)
                    return false;

                state.LastProcessedWonRoundCountForAttacker = wonRoundsCount;
                return true;
            }

            if (side.MissionSide == BattleSideEnum.Defender)
            {
                if (state.LastProcessedWonRoundCountForDefender == wonRoundsCount)
                    return false;

                state.LastProcessedWonRoundCountForDefender = wonRoundsCount;
                return true;
            }

            return false;
        }

        private static void RecordEscapedTroop(PartyBase troopParty, CharacterObject troop)
        {
            if (!EscapedTroopsByParty.TryGetValue(troopParty, out var cache))
            {
                cache = new EscapedTroopsCache(troopParty);
                EscapedTroopsByParty[troopParty] = cache;
            }

            cache.EscapedTroops.TryGetValue(troop, out var currentCount);
            cache.EscapedTroops[troop] = currentCount + 1;
        }

        private static void NotifyTroopRouted(MapEventSide retreatingSide, PartyBase troopParty, CharacterObject troop)
        {
            var mapEvent = retreatingSide.MapEvent;
            var battleObserver = mapEvent != null ? BattleObserverGetter?.Invoke(mapEvent) : null;
            if (battleObserver == null)
                return;

            // number=-1, numberRouted=1
            battleObserver.TroopNumberChanged(retreatingSide.MissionSide, troopParty, troop, -1, 0, 0, 1);
        }

        private static void RemoveTroopFromSimulationList(MapEventSide mapEventSide, UniqueTroopDescriptor troopDescriptor)
        {
            var simulationTroopList = SimulationTroopList(mapEventSide);

            // removing the currently selected troop, swapremove it and wipe selected state
            if (SelectedSimulationTroopDescriptor(mapEventSide) == troopDescriptor)
            {
                var selectedIndex = SelectedSimulationTroopIndex(mapEventSide);
                var lastIndex = simulationTroopList.Count - 1;

                simulationTroopList[selectedIndex] = simulationTroopList[lastIndex];
                simulationTroopList.RemoveAt(lastIndex);

                SelectedSimulationTroopIndex(mapEventSide) = -1;
                SelectedSimulationTroopDescriptor(mapEventSide) = UniqueTroopDescriptor.Invalid;
                SelectedSimulationTroop(mapEventSide) = null;
                return;
            }

            for (var i = 0; i < simulationTroopList.Count; i++)
            {
                if (simulationTroopList[i] != troopDescriptor)
                    continue;

                var lastIndex = simulationTroopList.Count - 1;

                if (i != lastIndex)
                {
                    simulationTroopList[i] = simulationTroopList[lastIndex];

                    var selectedIndex = SelectedSimulationTroopIndex(mapEventSide);
                    if (selectedIndex == lastIndex)
                    {
                        SelectedSimulationTroopIndex(mapEventSide) = i;
                    }
                }

                simulationTroopList.RemoveAt(lastIndex);

                // selectedIndex ended up out bounds
                if (SelectedSimulationTroopIndex(mapEventSide) >= simulationTroopList.Count)
                {
                    SelectedSimulationTroopIndex(mapEventSide) = -1;
                    SelectedSimulationTroopDescriptor(mapEventSide) = UniqueTroopDescriptor.Invalid;
                    SelectedSimulationTroop(mapEventSide) = null;
                }

                return;
            }
        }

        private static bool ApplyPartialRouting(MapEventSide retreatingSide, RetreatRoutingState routingState)
        {
            var mapEvent = retreatingSide.MapEvent;
            if (mapEvent == null)
                return false;

            var simulationTroopList = SimulationTroopList(retreatingSide);
            if (simulationTroopList == null || simulationTroopList.Count <= 0)
                return false;

            var terrainModifier = GetTerrainRetreatModifier(mapEvent.EventTerrainType);
            var enemyCompositionModifier = GetCompositionModifier(
                retreatingSide.OtherSide,
                ENEMY_VALUE_HORSE_ARCHER,
                ENEMY_VALUE_CAVALRY,
                ENEMY_VALUE_ARCHER,
                ENEMY_VALUE_INFANTRY);

            var valorTraitLevel = retreatingSide.LeaderParty.LeaderHero?.GetTraitLevel(DefaultTraits.Valor) ?? 0;
            var valorModifier = -valorTraitLevel * VALOR_RETREAT_REDUCTION_PER_LEVEL;

            var maxTroopTier = Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier;
            if (maxTroopTier < 1)
                maxTroopTier = 1;

            var simulationTroopsSet = routingState.SimulationTroopsSet;
            simulationTroopsSet.Clear();
            for (var i = 0; i < simulationTroopList.Count; i++)
            {
                simulationTroopsSet.Add(simulationTroopList[i]);
            }

            var candidates = routingState.CandidateBuffer;
            candidates.Clear();

            var selectedCandidates = routingState.SelectedCandidateBuffer;
            selectedCandidates.Clear();

            var woundedRatiosByTroop = routingState.WoundedRatiosByTroop;

            for (var partyIndex = 0; partyIndex < retreatingSide.Parties.Count; partyIndex++)
            {
                var mapEventParty = retreatingSide.Parties[partyIndex];
                var troopParty = mapEventParty.Party;

                BuildWoundedRatioByTroop(troopParty, woundedRatiosByTroop);
                foreach (var element in mapEventParty.Troops)
                {
                    if (element.State != RosterTroopState.Active)
                        continue;

                    if (!simulationTroopsSet.Contains(element.Descriptor))
                        continue;

                    var troop = element.Troop;
                    if (troop == null || troop.IsHero)
                        continue;

                    if (troop.HasAttribute(CharacterAttributes.UNBREAKABLE))
                        continue;

                    var formationValue = GetRetreatFormationValue(troop.GetFormationClass());
                    var chance = MBMath.ClampFloat(terrainModifier + enemyCompositionModifier + formationValue + valorModifier, 0f, 1f);
                    if (chance < MIN_ROUTE_CHANCE_TO_ROLL)
                        continue;

                    woundedRatiosByTroop.TryGetValue(troop, out var woundedRatio);

                    var baseWeight =
                        (chance * ROUTE_SELECTION_WEIGHT_CHANCE) +
                        (woundedRatio * ROUTE_SELECTION_WEIGHT_WOUNDED_RATIO);

                    if (baseWeight <= 0f)
                        continue;

                    var troopTier = troop.Tier;
                    if (troopTier < 1)
                        troopTier = 1;
                    if (troopTier > maxTroopTier)
                        troopTier = maxTroopTier;

                    var inverseTier = (maxTroopTier + 1) - troopTier; // tier1 -> maxTier, tierMax -> 1
                    var tierWeight = (float)Math.Pow(inverseTier, ROUTE_SELECTION_TIER_POWER);

                    var finalWeight = baseWeight * tierWeight;
                    if (finalWeight <= 0f)
                        continue;

                    candidates.Add(new RoutableTroopCandidate(element.Descriptor, troopParty, troop, chance, finalWeight));
                }
            }

            if (candidates.Count <= 0)
                return false;

            var attemptCount = GetRouteAttemptCountForSide(retreatingSide, candidates.Count);
            if (attemptCount <= 0)
                return false;

            SelectWeightedCandidatesWithoutReplacement(candidates, attemptCount, selectedCandidates);

            var routedTroopCount = 0;
            for (var i = 0; i < selectedCandidates.Count; i++)
            {
                var candidate = selectedCandidates[i];
                if (MBRandom.RandomFloat >= candidate.Chance)
                    continue;

                // allocated roster stays consistent
                retreatingSide.OnTroopRouted(candidate.TroopDescriptor, isOrderRetreat: false);
                RemoveTroopFromSimulationList(retreatingSide, candidate.TroopDescriptor);

                if (ShouldUsePostRetreatCustom(mapEvent, retreatingSide))
                {
                    RecordEscapedTroop(candidate.TroopParty, candidate.Troop);
                }
                NotifyTroopRouted(retreatingSide, candidate.TroopParty, candidate.Troop);
                routedTroopCount++;
            }

            // if nothing routed dont start pursuit
            return routedTroopCount > 0;
        }

        private static bool ShouldUseRetreatRouting(MapEvent mapEvent, MapEventSide mapEventSide)
        {
            if (mapEvent == null || mapEventSide == null)
                return false;

            if (mapEvent.EventType == MapEvent.BattleTypes.FieldBattle)
                return true;

            return mapEvent.MapEventSettlement?.SiegeEvent != null;
        }

        private static bool ShouldUsePostRetreatCustom(MapEvent mapEvent, MapEventSide mapEventSide)
        {
            if (!ShouldUseRetreatRouting(mapEvent, mapEventSide))
                return false;

            var siegeEvent = mapEvent.MapEventSettlement?.SiegeEvent;
            if (siegeEvent == null)
                return true;

            var besiegerLeaderParty = siegeEvent.BesiegerCamp?.LeaderParty?.Party;
            if (besiegerLeaderParty == null)
                return false;

            return mapEventSide.LeaderParty == besiegerLeaderParty;
        }

        [HarmonyPatch(typeof(MapEvent), "CheckSideRunAway")]
        private static class MapEvent_CheckSideRunAway_Patch
        {
            private static bool Prefix(MapEvent __instance, MapEventSide mapEventSide)
            {
                if (__instance.RetreatingSide != BattleSideEnum.None)
                    return true;

                if (!ShouldUseRetreatRouting(__instance, mapEventSide))
                    return true;

                var retreatLeaderParty = mapEventSide.LeaderParty;
                var attackerLeaderParty = mapEventSide.OtherSide.LeaderParty;

                if (retreatLeaderParty == null || attackerLeaderParty == null)
                    return true;

                // only mobile vs mobile
                if (!retreatLeaderParty.IsMobile || !attackerLeaderParty.IsMobile)
                    return true;

                if (IsRecentRetreatCooldownActive(mapEventSide))
                    return false;

                var retreatingSideTroopCount = mapEventSide.TroopCount;
                var enemySideTroopCount = mapEventSide.OtherSide.TroopCount;

                if (retreatingSideTroopCount > 0 &&
                    retreatingSideTroopCount < RUN_AWAY_DISABLED_MAX_SIDE_TROOPS &&
                    enemySideTroopCount >= retreatingSideTroopCount * RUN_AWAY_DISABLED_ENEMY_MULTIPLIER)
                {
                    return false;
                }

                var routingState = GetOrCreateRetreatRoutingState(__instance);

                // retreat starts after lost 4 rounds in a row and morale is under 20
                if (!IsRetreatModeActiveForSide(mapEventSide, routingState))
                {
                    if (!IsRetreatTriggerSatisfied(__instance, mapEventSide))
                        return false;

                    ActivateRetreatModeForSide(mapEventSide, routingState);
                }

                // if morale is back to over 20
                if (mapEventSide.GetSideMorale() > ROUTE_ATTEMPTS_START_MORALE)
                    return false;

                if (!ShouldAttemptRoutingAfterThisRoundLoss(__instance, mapEventSide, routingState))
                    return false;

                var didRouteAnyTroops = ApplyPartialRouting(mapEventSide, routingState);
                if (!didRouteAnyTroops)
                    return false;

                MapEventRetreatingSide(__instance) = mapEventSide.MissionSide;
                MapEventPursuitRoundNumber(__instance) =
                    Campaign.Current.Models.CombatSimulationModel.GetPursuitRoundCount(__instance);

                var retreatingMobileParties = new List<MobileParty>();

                for (var i = 0; i < mapEventSide.Parties.Count; i++)
                {
                    var party = mapEventSide.Parties[i].Party;
                    if (party == null || !party.IsMobile)
                        continue;

                    var mobileParty = party.MobileParty;
                    if (!retreatingMobileParties.Contains(mobileParty))
                    {
                        retreatingMobileParties.Add(mobileParty);
                    }
                }

                PendingRetreatCooldownByMapEvent[__instance] = new PendingRetreatCooldown(
                    retreatingMobileParties,
                    attackerLeaderParty.MobileParty);

                return false;
            }
        }

        private static void RegisterPendingFugitivePartySpawn(
            MapEvent mapEvent,
            Hero fugitiveHero,
            Dictionary<CharacterObject, int> escapedTroops)
        {
            if (mapEvent == null || fugitiveHero == null || escapedTroops == null || escapedTroops.Count <= 0)
                return;

            if (!PendingFugitivePartySpawnsByMapEvent.TryGetValue(mapEvent, out var pendingSpawns))
            {
                pendingSpawns = new List<PendingFugitivePartySpawn>();
                PendingFugitivePartySpawnsByMapEvent[mapEvent] = pendingSpawns;
            }

            pendingSpawns.Add(new PendingFugitivePartySpawn(fugitiveHero, escapedTroops));
        }

        private static void SpawnPendingFugitivePartiesIfAny(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return;

            if (!PendingFugitivePartySpawnsByMapEvent.TryGetValue(mapEvent, out var pendingSpawns) ||
                pendingSpawns == null || pendingSpawns.Count <= 0)
            {
                PendingFugitivePartySpawnsByMapEvent.Remove(mapEvent);
                return;
            }

            for (var i = 0; i < pendingSpawns.Count; i++)
            {
                var pendingSpawn = pendingSpawns[i];
                var fugitiveHero = pendingSpawn.Hero;
                if (fugitiveHero == null)
                    continue;

                var spawnSettlement = fugitiveHero.CurrentSettlement ?? SettlementHelper.GetBestSettlementToSpawnAround(fugitiveHero);
                if (spawnSettlement == null)
                    continue;

                var targetParty = MobilePartyHelper.SpawnLordParty(fugitiveHero, spawnSettlement);
                if (targetParty == null)
                    continue;

                if (targetParty.LeaderHero != fugitiveHero)
                {
                    fugitiveHero.ChangeState(Hero.CharacterStates.Active);
                    AddHeroToPartyAction.Apply(fugitiveHero, targetParty, showNotification: false);
                    targetParty.ChangePartyLeader(fugitiveHero);
                }

                foreach (var kvp in pendingSpawn.EscapedTroops)
                {
                    targetParty.MemberRoster.AddToCounts(kvp.Key, kvp.Value, insertAtFront: false, woundedCount: 0);
                }

                AdjustMobilePartyMoraleToTarget(targetParty, POST_RETREAT_TARGET_MORALE);
                SetDisorganizedForHours(targetParty, POST_RETREAT_DISORGANIZED_HOURS);
            }

            PendingFugitivePartySpawnsByMapEvent.Remove(mapEvent);
        }
        private static void AdjustMobilePartyMoraleToTarget(MobileParty mobileParty, float targetMorale)
        {
            var moraleDelta = targetMorale - mobileParty.Morale;
            mobileParty.RecentEventsMorale += moraleDelta;
        }

        private static void SetDisorganizedForHours(MobileParty mobileParty, float hours)
        {
            mobileParty.SetDisorganized(true);
            DisorganizedUntilTime(mobileParty) = CampaignTime.HoursFromNow(hours);
        }

        [HarmonyPatch(typeof(MapEvent), "FinishBattle")]
        private static class MapEvent_FinishBattle_Patch
        {
            private static void Postfix(MapEvent __instance)
            {
                if (PendingRetreatCooldownByMapEvent.TryGetValue(__instance, out var pendingCooldown))
                {
                    var cooldownUntilTime = CampaignTime.HoursFromNow(RECENT_RETREAT_COOLDOWN_HOURS);

                    for (var i = 0; i < pendingCooldown.RetreatingParties.Count; i++)
                    {
                        var retreatingParty = pendingCooldown.RetreatingParties[i];
                        RecentRetreatInfoByParty[retreatingParty] =
                            new RecentRetreatInfo(pendingCooldown.AttackerParty, cooldownUntilTime);
                    }
                }

                PendingRetreatCooldownByMapEvent.Remove(__instance);
                SpawnPendingFugitivePartiesIfAny(__instance);
                RetreatRoutingStateByMapEvent.Remove(__instance);
            }
        }

        // Moved to TORBattleRewardModel.CalculateMoraleChangeOnRoundVictory override
        // [HarmonyPatch(typeof(DefaultBattleRewardModel), nameof(DefaultBattleRewardModel.CalculateMoraleChangeOnRoundVictory))]
        // private static class DefaultBattleRewardModel_CalculateMoraleChangeOnRoundVictory_Patch
        // {
        //     private static bool Prefix(
        //         PartyBase party,
        //         MapEventSide partySide,
        //         BattleSideEnum roundWinner,
        //         ref float __result)
        //     {
        //         ...
        //     }
        // }

        // wounded in battle always goes to winner
        [HarmonyPatch(typeof(MapEvent), "CaptureDefeatedPartyMembers")]
        private static class MapEvent_CaptureDefeatedPartyMembers_Patch
        {
            private static void Postfix(
                MapEvent __instance,
                MBReadOnlyList<MapEventParty> winnerParties,
                MBReadOnlyList<MapEventParty> defeatedParties)
            {
                if (defeatedParties == null || defeatedParties.Count <= 0)
                    return;
                if (__instance.EventType != MapEvent.BattleTypes.FieldBattle && __instance.MapEventSettlement?.SiegeEvent == null)
                    return;

                CaptureRetreatingWoundedTroops(__instance, winnerParties, defeatedParties);

                for (var i = 0; i < defeatedParties.Count; i++)
                {
                    var defeatedParty = defeatedParties[i];
                    var partyBase = defeatedParty.Party;
                    if (partyBase == null || !partyBase.IsMobile)
                        continue;

                    if (!EscapedTroopsByParty.TryGetValue(partyBase, out var cache))
                        continue;
                    if (!ShouldUsePostRetreatCustom(__instance, partyBase.MapEventSide))
                    {
                        EscapedTroopsByParty.Remove(partyBase);
                        continue;
                    }

                    var shouldKeepEscapedTroops = !cache.IsLordParty;

                    if (cache.IsLordParty && cache.OriginalLeaderHero != null)
                    {
                        // only restore routed troops if the lord actually escaped after battle
                        shouldKeepEscapedTroops = cache.OriginalLeaderHero.HeroState == Hero.CharacterStates.Fugitive;
                    }

                    if (shouldKeepEscapedTroops)
                    {
                        if (cache.IsLordParty &&
                            cache.OriginalLeaderHero != null &&
                            cache.OriginalLeaderHero.HeroState == Hero.CharacterStates.Fugitive)
                        {
                            RegisterPendingFugitivePartySpawn(__instance, cache.OriginalLeaderHero, cache.EscapedTroops);
                            EscapedTroopsByParty.Remove(partyBase);
                            continue;
                        }

                        foreach (var kvp in cache.EscapedTroops)
                        {
                            partyBase.MemberRoster.AddToCounts(kvp.Key, kvp.Value, insertAtFront: false, woundedCount: 0);
                        }
                        PostRetreatPartiesNeedingFinalOverrides.Add(partyBase.MobileParty);
                    }

                    EscapedTroopsByParty.Remove(partyBase);
                }
            }

            private static void CaptureRetreatingWoundedTroops(
                MapEvent mapEvent,
                MBReadOnlyList<MapEventParty> winnerParties,
                MBReadOnlyList<MapEventParty> defeatedParties)
            {
                if (mapEvent.RetreatingSide == BattleSideEnum.None)
                    return;

                Campaign.Current.Models.BattleRewardModel.GetCaptureMemberChancesForWinnerParties(
                    mapEvent,
                    winnerParties,
                    out var woundedMemberChances,
                    out var healthyMemberChances);

                if (woundedMemberChances.Count <= 0)
                    return;

                for (var partyIndex = 0; partyIndex < defeatedParties.Count; partyIndex++)
                {
                    var partyBase = defeatedParties[partyIndex].Party;
                    if (partyBase == null || !partyBase.IsMobile)
                        continue;

                    if (!ShouldUsePostRetreatCustom(mapEvent, partyBase.MapEventSide))
                        continue;

                    for (var rosterIndex = partyBase.MemberRoster.Count - 1; rosterIndex >= 0; rosterIndex--)
                    {
                        var rosterElement = partyBase.MemberRoster.GetElementCopyAtIndex(rosterIndex);
                        var woundedCount = rosterElement.WoundedNumber;
                        if (woundedCount <= 0)
                            continue;

                        var character = rosterElement.Character;
                        if (character.IsHero || !Campaign.Current.Models.BattleRewardModel.CanTroopBeTakenPrisoner(character))
                            continue;

                        var capturedWoundedCount = 0;
                        for (var woundedIndex = 0; woundedIndex < woundedCount; woundedIndex++)
                        {
                            var winnerParty = SelectWinnerPartyForLoot(woundedMemberChances);
                            var prisonerRoster = winnerParty?.RosterToReceiveLootPrisoners;
                            if (prisonerRoster == null)
                                continue;

                            prisonerRoster.AddToCounts(character, 1, insertAtFront: false, woundedCount: 1);
                            capturedWoundedCount++;
                        }

                        if (capturedWoundedCount > 0)
                        {
                            partyBase.MemberRoster.AddToCountsAtIndex(
                                rosterIndex,
                                -capturedWoundedCount,
                                -capturedWoundedCount,
                                0,
                                removeDepleted: false);
                        }
                    }

                    partyBase.MemberRoster.RemoveZeroCounts();
                }
            }

            private static MapEventParty SelectWinnerPartyForLoot(
                MBReadOnlyList<KeyValuePair<MapEventParty, float>> winnerPartiesLootChances)
            {
                var roll = MBRandom.RandomFloat;
                foreach (var winnerPartyLootChance in winnerPartiesLootChances)
                {
                    roll -= winnerPartyLootChance.Value;
                    if (roll <= 0f)
                    {
                        return winnerPartyLootChance.Key;
                    }
                }

                return null;
            }
        }

        [HarmonyPatch(typeof(CampaignEventDispatcher), nameof(CampaignEventDispatcher.OnMapEventEnded))]
        private static class CampaignEventDispatcher_OnMapEventEnded_Patch
        {
            private static void Postfix(MapEvent mapEvent)
            {
                if (PostRetreatPartiesNeedingFinalOverrides.Count <= 0)
                    return;

                foreach (var mobileParty in PostRetreatPartiesNeedingFinalOverrides)
                {
                    AdjustMobilePartyMoraleToTarget(mobileParty, POST_RETREAT_TARGET_MORALE);
                    SetDisorganizedForHours(mobileParty, POST_RETREAT_DISORGANIZED_HOURS);
                }

                PostRetreatPartiesNeedingFinalOverrides.Clear();
            }
        }

        [HarmonyPatch(typeof(DefaultSettlementValueModel), "GeographicalAdvantageForFaction")]
        private static class DefaultSettlementValueModel_GeographicalAdvantageForFaction_Patch
        {
            private static bool Prefix(IFaction faction, ref float __result)
            {
                if (faction == null)
                    return true;

                if (faction.FactionMidSettlement != null)
                    return true;

                if (faction is Clan clan)
                {
                    clan.CalculateMidSettlement();

                    if (clan.FactionMidSettlement != null)
                        return true;
                }

                __result = 0f;
                return false;
            }
        }
    }
}