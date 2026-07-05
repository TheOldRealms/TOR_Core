using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.UniqueSpawns
{
    public class OrionCampaignBehavior : CampaignBehaviorBase
    {
        public const string OrionSpawnId = "tor_unique_orion";
        public const float OrionAthelLorenSpeedBonus = 4.0f;
        public const float OrionOutsideAthelLorenSpeedBonus = 2.4f;
        public const float OrionRetreatSpeedBonus = 5f;

        private const string OrionClanId = "wildhunt_clan_1";
        private const string OakOfAgesSettlementId = "oak_of_ages";
        private const string OrionSpawnedStoryId = "OrionSpawned";
        private const string OrionPlayerDefeatedStoryId = "OrionDefeatedByPlayer";

        private const int OrionPartySize = 800;
        private const int OrionCampaignStartDiplomacyRepairTicks = 12;
        private const int OrionDefeatedCooldownYears = 3;

        [SaveableField(0)]
        private UniqueSpawnState _orionState = UniqueSpawnState.Inactive;

        [SaveableField(1)]
        private int _orionNextEligibleYear = -1;

        [SaveableField(2)]
        private string _orionPartyId;

        [SaveableField(3)]
        private int _lastCheckedSeasonIndex = -1;

        [SaveableField(4)]
        private int _lastCheckedYear = -1;

        [SaveableField(5)]
        private List<string> _queuedOrionInkStorys = [];

        [SaveableField(6)]
        private int _orionSpawnSerial;

        [SaveableField(7)]
        private int _campaignStartDiplomacyRepairTicksLeft;

        [SaveableField(8)]
        private Dictionary<string, float> _orionWarPressureByFaction = [];

        [SaveableField(9)]
        private string _orionWarTargetFactionId;

        [SaveableField(10)]
        private string _orionWarPrimarySettlementId;

        [SaveableField(11)]
        private string _orionWarSecondarySettlementId;

        [SaveableField(12)]
        private int _orionWarPlanMode = UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol;

        [SaveableField(13)]
        private int _orionWarPlanDaysLeft;

        [SaveableField(14)]
        private int _orionWarTargetSwapIndex;

        private bool _removingOrionByScript;
        private bool _repairingOrionDiplomacy;
        private bool _removingOrionMercenaryContract;

        private static readonly Color SpawnHudMessageColor = new Color(0.0f, 0.78f, 0.47f);
        private static readonly Color RetreatHudMessageColor = new Color(0.35f, 0.70f, 1.0f);
        private static readonly Color DefeatHudMessageColor = new Color(0.65f, 0.02f, 0.02f);

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_orionState", ref _orionState);
            dataStore.SyncData("_orionNextEligibleYear", ref _orionNextEligibleYear);
            dataStore.SyncData("_orionPartyId", ref _orionPartyId);
            dataStore.SyncData("_lastCheckedSeasonIndex", ref _lastCheckedSeasonIndex);
            dataStore.SyncData("_lastCheckedYear", ref _lastCheckedYear);
            dataStore.SyncData("_queuedOrionInkStorys", ref _queuedOrionInkStorys);
            dataStore.SyncData("_orionSpawnSerial", ref _orionSpawnSerial);
            dataStore.SyncData("_campaignStartDiplomacyRepairTicksLeft", ref _campaignStartDiplomacyRepairTicksLeft);
            dataStore.SyncData("_orionWarPressureByFaction", ref _orionWarPressureByFaction);
            dataStore.SyncData("_orionWarTargetFactionId", ref _orionWarTargetFactionId);
            dataStore.SyncData("_orionWarPrimarySettlementId", ref _orionWarPrimarySettlementId);
            dataStore.SyncData("_orionWarSecondarySettlementId", ref _orionWarSecondarySettlementId);
            dataStore.SyncData("_orionWarPlanMode", ref _orionWarPlanMode);
            dataStore.SyncData("_orionWarPlanDaysLeft", ref _orionWarPlanDaysLeft);
            dataStore.SyncData("_orionWarTargetSwapIndex", ref _orionWarTargetSwapIndex);
        }

        public bool IsOrionRetreating(MobileParty party)
        {
            return party != null &&
                   party.StringId == _orionPartyId &&
                   _orionState == UniqueSpawnState.RetreatingToHome;
        }

        public string GetOrionDebugStatus()
        {
            const string noPartyText = "none";
            const string partyStatusText = "{PARTY_ID}, active: {IS_ACTIVE}, position: {POSITION}, count: {COUNT}";
            const string debugStatusText = "orion state: {STATE}, next eligible year: {NEXT_ELIGIBLE_YEAR}, current year: {CURRENT_YEAR}, season: {SEASON}, party: {PARTY_STATUS}, queued InkStory: {QUEUED_InkStoryS}, war target: {WAR_TARGET}, plan: {PLAN}, target a: {TARGET_A}, target b: {TARGET_B}";

            _queuedOrionInkStorys ??= [];

            var orionParty = CurrentOrionParty();
            var partyStatus = new TextObject(noPartyText);

            if (orionParty != null)
            {
                partyStatus = new TextObject(partyStatusText);
                partyStatus.SetTextVariable("PARTY_ID", orionParty.StringId);
                partyStatus.SetTextVariable("IS_ACTIVE", orionParty.IsActive.ToString());
                partyStatus.SetTextVariable("POSITION", orionParty.Position.ToString());
                partyStatus.SetTextVariable("COUNT", orionParty.MemberRoster.TotalManCount.ToString());
            }

            var queuedInkStorys = _queuedOrionInkStorys.Count == 0
                ? new TextObject(noPartyText).ToString()
                : string.Join(", ", _queuedOrionInkStorys);

            var debugStatus = new TextObject(debugStatusText);
            debugStatus.SetTextVariable("STATE", _orionState.ToString());
            debugStatus.SetTextVariable("NEXT_ELIGIBLE_YEAR", _orionNextEligibleYear.ToString());
            debugStatus.SetTextVariable("CURRENT_YEAR", CampaignTime.Now.GetYear.ToString());
            debugStatus.SetTextVariable("SEASON", CampaignTime.Now.GetSeasonOfYear.ToString());
            debugStatus.SetTextVariable("PARTY_STATUS", partyStatus);
            debugStatus.SetTextVariable("QUEUED_InkStoryS", queuedInkStorys);
            debugStatus.SetTextVariable("WAR_TARGET", _orionWarTargetFactionId ?? noPartyText);
            debugStatus.SetTextVariable("PLAN", _orionWarPlanMode.ToString());
            debugStatus.SetTextVariable("TARGET_A", _orionWarPrimarySettlementId ?? noPartyText);
            debugStatus.SetTextVariable("TARGET_B", _orionWarSecondarySettlementId ?? noPartyText);

            return debugStatus.ToString();
        }

        public string TestSpawnOrion()
        {
            var existingParty = CurrentOrionParty();
            if (existingParty != null && existingParty.IsActive)
            {
                RemoveOrionWithoutDefeatLogic(existingParty);
            }

            _orionPartyId = null;
            _orionState = UniqueSpawnState.Inactive;
            _orionNextEligibleYear = -1;

            SpawnOrionAtOak(false);

            return GetOrionDebugStatus();
        }

        public string TestRetreatOrion()
        {
            const string orionNotActiveText = "orion is not active.";

            var orionParty = CurrentOrionParty();
            if (orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            _orionState = UniqueSpawnState.Active;
            CallOrionBackToOak();

            return GetOrionDebugStatus();
        }

        public string TestRetreatCompleteOrion()
        {
            const string orionNotActiveText = "orion is not active.";

            var orionParty = CurrentOrionParty();
            if (orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            _orionState = UniqueSpawnState.RetreatingToHome;
            PutOrionBackIntoOak(orionParty);

            return GetOrionDebugStatus();
        }

        public string TestDefeatOrion()
        {
            const string orionNotActiveText = "orion is not active.";

            var orionParty = CurrentOrionParty();
            if (orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            DefeatOrionAndClearParty(false);

            return GetOrionDebugStatus();
        }

        public string TestResetOrion()
        {
            var orionParty = CurrentOrionParty();
            if (orionParty != null && orionParty.IsActive)
            {
                RemoveOrionWithoutDefeatLogic(orionParty);
            }

            _orionPartyId = null;
            _orionState = UniqueSpawnState.Inactive;
            _orionNextEligibleYear = -1;
            _orionWarPlanDaysLeft = 0;
            _orionWarTargetFactionId = null;
            _orionWarPrimarySettlementId = null;
            _orionWarSecondarySettlementId = null;
            _queuedOrionInkStorys ??= [];
            _queuedOrionInkStorys.Clear();

            return GetOrionDebugStatus();
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            DisableOrionSpawn();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {

            _queuedOrionInkStorys ??= [];
            _orionWarPressureByFaction ??= [];

            _lastCheckedSeasonIndex = (int)CampaignTime.Now.GetSeasonOfYear;
            _lastCheckedYear = CampaignTime.Now.GetYear;

            _campaignStartDiplomacyRepairTicksLeft = OrionCampaignStartDiplomacyRepairTicks;

            if (CurrentOrionParty() == null)
            {
                DisableOrionSpawn();
            }

            KeepOrionClanIndependent();
            RepairOrionDiplomacyAgainstAthelLoren();
        }

        private void DailyTick()
        {
            CheckOrionSeasonTick();
            KeepOrionClanIndependent();
            RepairOrionDiplomacyAgainstAthelLoren();
            DecayOrionWarPressure();
            TickOrionMacroPlan();
            OpenQueuedOrionInkStoryOnMap();
        }

        private void HourlyTick()
        {
            RepairOrionDiplomacyAfterCampaignStart();
            KeepOrionOnOakRoad();
            OpenQueuedOrionInkStoryOnMap();
        }

        private void RepairOrionDiplomacyAfterCampaignStart()
        {
            if (_campaignStartDiplomacyRepairTicksLeft <= 0)
            {
                return;
            }

            _campaignStartDiplomacyRepairTicksLeft--;
            KeepOrionClanIndependent();
            RepairOrionDiplomacyAgainstAthelLoren();
        }

        private void AiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
        {
            if (party.GetUniqueSpawnComponent()?.UniqueSpawnId != OrionSpawnId)
            {
                return;
            }

            if (IsOrionRetreating(party))
            {
                PointOrionAtOak(party);

                var oakOfAges = OakOfAges();
                var goToOakBehavior = new AIBehaviorData(
                    oakOfAges.GatePosition,
                    AiBehavior.GoToPoint,
                    MobileParty.NavigationType.Default,
                    false,
                    false,
                    false);

                // retreat is not a preference
                thinkParams.DoNotChangeBehavior = true;
                UniqueSpawnCampaignBehavior.AddOrUpdateBehaviorScore(thinkParams, goToOakBehavior, 9999f);
                return;
            }

            if (_orionState != UniqueSpawnState.Active)
            {
                return;
            }

            AddOrionMacroPlanScore(thinkParams);
        }

        private void AddOrionMacroPlanScore(PartyThinkParams thinkParams)
        {
            const float patrolPlanScore = 650f;
            const float raidPlanScore = 900f;

            TryPickOrionHomeSiegePlan();

            if (!OrionMacroPlanStillValid())
            {
                PickNewOrionMacroPlan();
            }

            var target = CurrentOrionWarTarget();
            if (target == null)
            {
                return;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid && VillageCanStillBeRaidedByOrion(target))
            {
                var raidBehavior = new AIBehaviorData(target, AiBehavior.RaidSettlement, MobileParty.NavigationType.Default, false, false, false);
                UniqueSpawnCampaignBehavior.AddOrUpdateBehaviorScore(thinkParams, raidBehavior, raidPlanScore);
                return;
            }

            // TBD. macro behaviors are not forced
            var patrolBehavior = new AIBehaviorData(target, AiBehavior.PatrolAroundPoint, MobileParty.NavigationType.Default, false, false, false);
            UniqueSpawnCampaignBehavior.AddOrUpdateBehaviorScore(thinkParams, patrolBehavior, patrolPlanScore);
        }

        private void OnClanChangedKingdom(
            Clan clan,
            Kingdom oldKingdom,
            Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail,
            bool showNotification)
        {
            if (clan.StringId != OrionClanId || newKingdom == null)
            {
                return;
            }

            KeepOrionClanIndependent();
            RepairOrionDiplomacyAgainstAthelLoren();
        }

        private void KeepOrionClanIndependent()
        {
            if (_removingOrionMercenaryContract)
            {
                return;
            }

            var orionClan = Clan.FindFirst(clan => clan.StringId == OrionClanId);
            if (orionClan.Kingdom == null)
            {
                return;
            }

            _removingOrionMercenaryContract = true;
            try
            {
                if (orionClan.IsUnderMercenaryService)
                {
                    ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(orionClan, false);
                }
                else
                {
                    ChangeKingdomAction.ApplyByLeaveKingdom(orionClan, false);
                }
            }
            finally
            {
                _removingOrionMercenaryContract = false;
            }
        }

        private void PullAthelLorenIntoOrionFight(IFaction faction1, IFaction faction2)
        {
            var orionClan = Clan.FindFirst(clan => clan.StringId == OrionClanId);
            var athelLoren = AthelLoren();
            var playerFaction = Hero.MainHero.MapFaction;

            if (playerFaction == athelLoren)
            {
                return;
            }

            var playerDeclaredOnOrion =
                (faction1 == orionClan && faction2 == playerFaction) ||
                (faction2 == orionClan && faction1 == playerFaction);

            if (!playerDeclaredOnOrion || athelLoren.IsAtWarWith(playerFaction))
            {
                return;
            }

            // orion will mimic athel loren diplomacy before it can register a war with player faction
            FactionManager.DeclareWar(athelLoren, playerFaction);
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            PullAthelLorenIntoOrionFight(faction1, faction2);
            RepairOrionDiplomacyAgainstAthelLoren();
        }

        private void OnPeaceMade(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
        {
            RepairOrionDiplomacyAgainstAthelLoren();
        }

        private void CheckOrionSeasonTick()
        {
            var currentSeason = CampaignTime.Now.GetSeasonOfYear;
            var currentSeasonIndex = (int)currentSeason;
            var currentYear = CampaignTime.Now.GetYear;

            if (_lastCheckedSeasonIndex == currentSeasonIndex && _lastCheckedYear == currentYear)
            {
                return;
            }

            _lastCheckedSeasonIndex = currentSeasonIndex;
            _lastCheckedYear = currentYear;

            if (currentSeason == CampaignTime.Seasons.Spring)
            {
                SpringOrionCheck();
            }
            else if (currentSeason == CampaignTime.Seasons.Autumn)
            {
                CallOrionBackToOak();
            }
        }

        private void SpringOrionCheck()
        {
            if (CampaignTime.Now.GetSeasonOfYear != CampaignTime.Seasons.Spring)
            {
                return;
            }

            if (AthelLorenIsGone())
            {
                DisableOrionSpawn();
                return;
            }

            var existingOrionParty = CurrentOrionParty();
            if (existingOrionParty != null)
            {
                if (_orionState != UniqueSpawnState.RetreatingToHome)
                {
                    _orionState = UniqueSpawnState.Active;
                }

                return;
            }

            if (_orionState == UniqueSpawnState.DefeatedCooldown &&
                CampaignTime.Now.GetYear < _orionNextEligibleYear)
            {
                return;
            }

            var returningFromOak = _orionState == UniqueSpawnState.RetreatedToHome;
            SpawnOrionAtOak(returningFromOak);
        }

        private void SpawnOrionAtOak(bool returningFromOak)
        {
            if (AthelLorenIsGone())
            {
                DisableOrionSpawn();
                return;
            }

            var oakOfAges = OakOfAges();
            var orionClan = Clan.FindFirst(clan => clan.StringId == OrionClanId);
            PrepareOrionForUniqueSpawning(orionClan.Leader);

            _orionSpawnSerial++;
            var orionPartyId = $"{OrionSpawnId}_party_{_orionSpawnSerial}";

            var orionParty = UniqueSpawnPartyComponent.CreateUniqueSpawnParty(
                orionPartyId,
                OrionSpawnId,
                oakOfAges,
                orionClan.Name,
                orionClan.DefaultPartyTemplate,
                orionClan,
                OrionPartySize);

            _orionPartyId = orionParty.StringId;
            _orionState = UniqueSpawnState.Active;
            _orionWarPlanDaysLeft = 0;

            orionParty.Ai.SetDoNotMakeNewDecisions(false);
            PickNewOrionMacroPlan();

            ReportOrionSpawn(returningFromOak);
        }

        private void CallOrionBackToOak()
        {
            const string retreatMessageText = "{=str_tor_unique_orion_retreat_started_message}Winter cold orion back";

            if (_orionState != UniqueSpawnState.Active)
            {
                return;
            }

            var orionParty = CurrentOrionParty();
            if (orionParty == null)
            {
                return;
            }

            _orionState = UniqueSpawnState.RetreatingToHome;
            _orionWarPlanDaysLeft = 0;
            PointOrionAtOak(orionParty);

            ShowOrionHudMessage(new TextObject(retreatMessageText), RetreatHudMessageColor);
        }

        private void KeepOrionOnOakRoad()
        {
            const float retreatCompletionDistance = 5f;

            if (_orionState != UniqueSpawnState.RetreatingToHome)
            {
                return;
            }

            var orionParty = CurrentOrionParty();
            if (orionParty == null || orionParty.MapEvent != null)
            {
                return;
            }

            var oakOfAges = OakOfAges();
            PointOrionAtOak(orionParty);

            if (orionParty.Position.DistanceSquared(oakOfAges.GatePosition) > retreatCompletionDistance * retreatCompletionDistance)
            {
                return;
            }

            PutOrionBackIntoOak(orionParty);
        }

        private void PointOrionAtOak(MobileParty orionParty)
        {
            var oakOfAges = OakOfAges();

            orionParty.Aggressiveness = 0f;
            orionParty.Ai.SetDoNotMakeNewDecisions(true);
            orionParty.SetMoveGoToPoint(oakOfAges.GatePosition, MobileParty.NavigationType.Default);
        }

        private void PutOrionBackIntoOak(MobileParty orionParty)
        {
            _orionState = UniqueSpawnState.RetreatedToHome;
            _orionPartyId = null;
            _orionWarPlanDaysLeft = 0;

            RemoveOrionWithoutDefeatLogic(orionParty);
            DisableOrionSpawn();
        }

        private void RemoveOrionWithoutDefeatLogic(MobileParty orionParty)
        {
            if (orionParty == null || !orionParty.IsActive)
            {
                return;
            }

            _removingOrionByScript = true;
            try
            {
                DestroyPartyAction.Apply(null, orionParty);
            }
            finally
            {
                _removingOrionByScript = false;
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            UniqueSpawnCampaignBehavior.RegisterWarPressureForHomeFaction(mapEvent, AthelLoren(), _orionWarPressureByFaction);

            if (_orionState == UniqueSpawnState.DefeatedCooldown || _orionState == UniqueSpawnState.RetreatedToHome)
            {
                return;
            }

            if (!MapEventHasOrionOnSide(mapEvent, mapEvent.DefeatedSide))
            {
                return;
            }

            DefeatOrionAndClearParty(PlayerHelpedDefeatOrion(mapEvent));
        }

        private bool MapEventHasOrionOnSide(MapEvent mapEvent, BattleSideEnum side)
        {
            if (side == BattleSideEnum.None)
            {
                return false;
            }

            return mapEvent.GetMapEventSide(side).Parties.Any(mapEventParty =>
                mapEventParty.Party?.MobileParty?.GetUniqueSpawnComponent()?.UniqueSpawnId == OrionSpawnId);
        }

        private bool PlayerHelpedDefeatOrion(MapEvent mapEvent)
        {
            var defeatedSide = mapEvent.DefeatedSide;
            var winningSide = defeatedSide == BattleSideEnum.Attacker
                ? BattleSideEnum.Defender
                : BattleSideEnum.Attacker;

            if (defeatedSide == BattleSideEnum.None)
            {
                return false;
            }

            return mapEvent.GetMapEventSide(winningSide).Parties.Any(mapEventParty =>
                mapEventParty.Party == PartyBase.MainParty ||
                mapEventParty.Party?.MobileParty == MobileParty.MainParty);
        }

        private void DefeatOrionAndClearParty(bool playerHelpedDefeatOrion)
        {
            if (_orionState == UniqueSpawnState.DefeatedCooldown)
            {
                return;
            }

            var orionParty = CurrentOrionParty();
            if (orionParty != null && orionParty.IsActive)
            {
                RemoveOrionWithoutDefeatLogic(orionParty);
            }

            PutOrionOnDefeatedCooldown(playerHelpedDefeatOrion);
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (destroyedParty?.GetUniqueSpawnComponent()?.UniqueSpawnId != OrionSpawnId)
            {
                return;
            }

            if (_removingOrionByScript)
            {
                return;
            }

            if (_orionState == UniqueSpawnState.RetreatedToHome || _orionState == UniqueSpawnState.DefeatedCooldown)
            {
                return;
            }

            PutOrionOnDefeatedCooldown(false);
        }

        private void PutOrionOnDefeatedCooldown(bool playerHelpedDefeatOrion)
        {
            const string defeatedMessageText = "{=str_tor_unique_orion_defeated_message}Orion gone 3 year";

            _orionPartyId = null;
            _orionState = UniqueSpawnState.DefeatedCooldown;
            _orionNextEligibleYear = CampaignTime.Now.GetYear + OrionDefeatedCooldownYears;
            _orionWarPlanDaysLeft = 0;

            DisableOrionSpawn();

            if (playerHelpedDefeatOrion)
            {
                QueueOrionInkStory(OrionPlayerDefeatedStoryId);
                return;
            }
            ShowOrionHudMessage(new TextObject(defeatedMessageText), DefeatHudMessageColor);
        }

        private MobileParty CurrentOrionParty()
        {
            if (!string.IsNullOrWhiteSpace(_orionPartyId))
            {
                var savedParty = MobileParty.All.FirstOrDefault(party => party.StringId == _orionPartyId && party.IsActive);
                if (savedParty != null)
                {
                    return savedParty;
                }
            }

            return MobileParty.All.FirstOrDefault(party =>
                party.IsActive &&
                party.GetUniqueSpawnComponent()?.UniqueSpawnId == OrionSpawnId);
        }

        private void PrepareOrionForUniqueSpawning(Hero orionLeader)
        {
            orionLeader.ChangeState(Hero.CharacterStates.Active);
        }

        private void DisableOrionSpawn()
        {
            var orionLeader = Clan.FindFirst(clan => clan.StringId == OrionClanId).Leader;

            if (orionLeader.PartyBelongedTo?.GetUniqueSpawnComponent()?.UniqueSpawnId == OrionSpawnId)
            {
                return;
            }

            DisableHeroAction.Apply(orionLeader);
        }

        private void RepairOrionDiplomacyAgainstAthelLoren()
        {
            // mirrored diplomacy calls will between wild hunt and athel loren will stuck in a loop
            if (_repairingOrionDiplomacy)
            {
                return;
            }

            _repairingOrionDiplomacy = true;
            try
            {
                var orionClan = Clan.FindFirst(clan => clan.StringId == OrionClanId);
                var athelLoren = AthelLoren();

                if (orionClan.IsAtWarWith(athelLoren))
                {
                    FactionManager.SetNeutral(orionClan, athelLoren);
                }

                foreach (var faction in UniqueSpawnCampaignBehavior.WarMirrorTargets(orionClan, athelLoren))
                {
                    var athelLorenAtWar = athelLoren.IsAtWarWith(faction);
                    var orionAtWar = orionClan.IsAtWarWith(faction);

                    if (athelLorenAtWar && !orionAtWar)
                    {
                        FactionManager.DeclareWar(orionClan, faction);
                    }
                    else if (!athelLorenAtWar && orionAtWar)
                    {
                        FactionManager.SetNeutral(orionClan, faction);
                    }
                }
            }
            finally
            {
                _repairingOrionDiplomacy = false;
            }
        }

        private void DecayOrionWarPressure()
        {
            const float dailyPressureDecay = 0.97f;
            const float forgottenPressureThreshold = 1f;

            UniqueSpawnCampaignBehavior.DecayWarPressure(
                _orionWarPressureByFaction,
                dailyPressureDecay,
                forgottenPressureThreshold);
        }

        private void TickOrionMacroPlan()
        {
            if (_orionState != UniqueSpawnState.Active || CurrentOrionParty() == null)
            {
                return;
            }

            if (TryPickOrionHomeSiegePlan())
            {
                return;
            }

            if (_orionWarPlanDaysLeft > 0)
            {
                _orionWarPlanDaysLeft--;
            }

            if (_orionWarPlanDaysLeft <= 0 || !OrionMacroPlanStillValid())
            {
                PickNewOrionMacroPlan();
            }
        }
        private bool OrionMacroPlanStillValid()
        {
            var primaryTarget = CurrentOrionWarTarget();

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol)
            {
                return _orionWarPlanDaysLeft > 0 &&
                       UniqueSpawnCampaignBehavior.IsOwnedOriginalHomeFief(
                           primaryTarget,
                           AthelLoren(),
                           IsOriginalAthelLorenFief);
            }

            var targetFaction = OrionWarTargetFaction();
            if (targetFaction == null)
            {
                return false;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid)
            {
                return primaryTarget != null && VillageCanStillBeRaidedByOrion(primaryTarget);
            }

            return primaryTarget != null;
        }
        private void PickNewOrionMacroPlan()
        {
            const int ownVillagePatrolDurationDays = 3;
            const int deepPatrolDurationDays = 3;
            const int raidDurationDays = 2;

            if (TryPickOrionHomeSiegePlan())
            {
                return;
            }

            var targetFaction = PickFactionThatHurtAthelLorenMost();
            if (targetFaction == null)
            {
                ClearOrionWarPlan();
                return;
            }

            _orionWarTargetFactionId = targetFaction.StringId;

            var lootableVillages = EnemyLootableVillages(targetFaction).ToList();
            var plan = PickOrionPlanMode(AthelLoren().CurrentTotalStrength, lootableVillages.Count);

            if (plan == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid && lootableVillages.Count == 0)
            {
                plan = UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol;
            }

            _orionWarPlanMode = plan;
            _orionWarTargetSwapIndex++;

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid)
            {
                var raidTarget = UniqueSpawnCampaignBehavior.ClosestSettlementToAffiliatedBorder(lootableVillages, DistanceToAthelLorenBorder);
                _orionWarPrimarySettlementId = raidTarget?.StringId;
                _orionWarSecondarySettlementId = null;
                _orionWarPlanDaysLeft = raidDurationDays;
                return;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol)
            {
                var ownVillages = AthelLorenBorderVillagesFacing(targetFaction).Take(2).ToList();
                _orionWarPrimarySettlementId = ownVillages.ElementAtOrDefault(0)?.StringId;
                _orionWarSecondarySettlementId = ownVillages.ElementAtOrDefault(1)?.StringId;
                _orionWarPlanDaysLeft = ownVillagePatrolDurationDays;
                return;
            }

            var enemySettlements = EnemyDeepPatrolSettlements(targetFaction).Take(2).ToList();
            _orionWarPrimarySettlementId = enemySettlements.ElementAtOrDefault(0)?.StringId;
            _orionWarSecondarySettlementId = enemySettlements.ElementAtOrDefault(1)?.StringId;
            _orionWarPlanDaysLeft = deepPatrolDurationDays;
        }
        private bool TryPickOrionHomeSiegePlan()
        {
            const int homeSiegePatrolDurationDays = 2;

            var besiegedAthelLorenFief = UniqueSpawnCampaignBehavior.BesiegedOwnedOriginalHomeFief(
                AthelLoren(),
                IsOriginalAthelLorenFief,
                DistanceToAthelLorenBorder);

            if (besiegedAthelLorenFief == null)
            {
                return false;
            }

            // athel loren under siege overrides any macro plan
            _orionWarPlanMode = UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol;
            _orionWarPrimarySettlementId = besiegedAthelLorenFief.StringId;
            _orionWarSecondarySettlementId = null;
            _orionWarTargetFactionId = null;
            _orionWarPlanDaysLeft = homeSiegePatrolDurationDays;

            return true;
        }
        private int PickOrionPlanMode(float athelLorenStrength, int lootableVillageCount)
        {
            const int weakAthelLorenStrengthThreshold = 5000;
            const int manyLootableVillagesThreshold = 10;

            const int weakOwnVillagePatrolWeight = 60;
            const int weakEnemyDeepPatrolWeight = 20;
            const int weakEnemyRaidWeight = 10;

            const int strongManyVillagesOwnVillagePatrolWeight = 35;
            const int strongManyVillagesEnemyDeepPatrolWeight = 35;
            const int strongManyVillagesEnemyRaidWeight = 30;

            const int strongFewVillagesOwnVillagePatrolWeight = 20;
            const int strongFewVillagesEnemyDeepPatrolWeight = 70;
            const int strongFewVillagesEnemyRaidWeight = 10;

            return UniqueSpawnCampaignBehavior.PickWarPlanMode(
                athelLorenStrength,
                lootableVillageCount,
                weakAthelLorenStrengthThreshold,
                manyLootableVillagesThreshold,
                weakOwnVillagePatrolWeight,
                weakEnemyDeepPatrolWeight,
                weakEnemyRaidWeight,
                strongManyVillagesOwnVillagePatrolWeight,
                strongManyVillagesEnemyDeepPatrolWeight,
                strongManyVillagesEnemyRaidWeight,
                strongFewVillagesOwnVillagePatrolWeight,
                strongFewVillagesEnemyDeepPatrolWeight,
                strongFewVillagesEnemyRaidWeight);
        }

        private IFaction PickFactionThatHurtAthelLorenMost()
        {
            var athelLoren = AthelLoren();
            var orionClan = Clan.FindFirst(clan => clan.StringId == OrionClanId);

            // grudges can outlive wars. only current enemies are allowed. TBD
            var validWarFactions = UniqueSpawnCampaignBehavior.WarMirrorTargets(orionClan, athelLoren)
                .Where(faction => athelLoren.IsAtWarWith(faction) && !faction.IsEliminated);

            return UniqueSpawnCampaignBehavior.PickFactionThatHurtHomeFactionMost(
                validWarFactions,
                _orionWarPressureByFaction,
                faction => EnemyDeepPatrolSettlements(faction)
                    .Select(DistanceToAthelLorenBorder)
                    .DefaultIfEmpty(float.MaxValue)
                    .Min());
        }

        private IEnumerable<Settlement> AthelLorenBorderVillagesFacing(IFaction targetFaction)
        {
            return UniqueSpawnCampaignBehavior.AffiliatedBorderVillagesFacing(
                AthelLoren(),
                targetFaction,
                DistanceToAthelLorenBorder);
        }

        private IEnumerable<Settlement> EnemyDeepPatrolSettlements(IFaction targetFaction)
        {
            return UniqueSpawnCampaignBehavior.EnemyDeepPatrolSettlements(
                targetFaction,
                DistanceToAthelLorenBorder);
        }

        private IEnumerable<Settlement> EnemyLootableVillages(IFaction targetFaction)
        {
            return UniqueSpawnCampaignBehavior.EnemyLootableVillages(
                targetFaction,
                VillageCanStillBeRaidedByOrion,
                DistanceToAthelLorenBorder);
        }

        private float DistanceToAthelLorenBorder(Settlement settlement)
        {
            return UniqueSpawnCampaignBehavior.DistanceToHomeFactionBorder(
                AthelLoren(),
                OakOfAges(),
                settlement);
        }

        private bool VillageCanStillBeRaidedByOrion(Settlement settlement)
        {
            return settlement != null &&
                   settlement.IsVillage &&
                   !settlement.IsRaided &&
                   (!settlement.IsUnderRaid || settlement.LastAttackerParty == CurrentOrionParty()) &&
                   settlement.MapFaction != null &&
                   Clan.FindFirst(clan => clan.StringId == OrionClanId).IsAtWarWith(settlement.MapFaction);
        }

        private Settlement CurrentOrionWarTarget()
        {
            return UniqueSpawnCampaignBehavior.CurrentWarTarget(
                _orionWarPrimarySettlementId,
                _orionWarSecondarySettlementId,
                _orionWarTargetSwapIndex);
        }

        private IFaction OrionWarTargetFaction()
        {
            if (string.IsNullOrWhiteSpace(_orionWarTargetFactionId))
            {
                return null;
            }

            var kingdom = Kingdom.All.FirstOrDefault(kingdom => kingdom.StringId == _orionWarTargetFactionId);
            if (kingdom != null)
            {
                return kingdom;
            }

            return Clan.All.FirstOrDefault(clan => clan.StringId == _orionWarTargetFactionId);
        }

        private void ClearOrionWarPlan()
        {
            _orionWarTargetFactionId = null;
            _orionWarPrimarySettlementId = null;
            _orionWarSecondarySettlementId = null;
            _orionWarPlanDaysLeft = 0;
            _orionWarPlanMode = UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol;
        }

        private bool AthelLorenIsGone()
        {
            var athelLoren = AthelLoren();
            return athelLoren.IsEliminated || athelLoren.Settlements.Count == 0;
        }

        private Kingdom AthelLoren()
        {
            return Kingdom.All.First(kingdom => kingdom.StringId == TORConstants.Factions.ATHEL_LOREN);
        }
        private bool IsOriginalAthelLorenFief(Settlement settlement)
        {
            return settlement != null &&
                   settlement.Culture?.StringId == TORConstants.Cultures.ASRAI;
        }

        private Settlement OakOfAges()
        {
            return Settlement.Find(OakOfAgesSettlementId);
        }

        private void ReportOrionSpawn(bool returningFromOak)
        {
            const string spawnedMessageText = "{=str_tor_unique_orion_spawned_message}orion spawns";
            const string returnedMessageText = "{=str_tor_unique_orion_returned_message}orion returns";

            var message = returningFromOak
                ? new TextObject(returnedMessageText)
                : new TextObject(spawnedMessageText);

            if (PlayerShouldSeeOrionInkStory())
            {
                QueueOrionInkStory(OrionSpawnedStoryId);
                return;
            }

            ShowOrionHudMessage(message, SpawnHudMessageColor);
        }

        private bool PlayerShouldSeeOrionInkStory()
        {
            var athelLoren = AthelLoren();
            var playerFaction = Hero.MainHero.MapFaction;

            return Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI ||
                   playerFaction == athelLoren ||
                   playerFaction.IsAtWarWith(athelLoren);
        }

        private void ShowOrionHudMessage(TextObject message, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), color));
        }

        private void QueueOrionInkStory(string storyId)
        {
            _queuedOrionInkStorys ??= [];

            if (_queuedOrionInkStorys.Contains(storyId))
            {
                return;
            }

            _queuedOrionInkStorys.Add(storyId);
        }

        private void OpenQueuedOrionInkStoryOnMap()
        {
            _queuedOrionInkStorys ??= [];

            if (_queuedOrionInkStorys.Count == 0 || ScreenManager.TopScreen is not MapScreen)
            {
                return;
            }

            var inkStoryBehavior = Campaign.Current.GetCampaignBehavior<InkStoryCampaignBehavior>();
            if (inkStoryBehavior?.CurrentStory != null)
            {
                return;
            }

            var storyId = _queuedOrionInkStorys[0];
            _queuedOrionInkStorys.RemoveAt(0);

            if (InkStoryManager.GetStory(storyId) == null)
            {
                return;
            }

            InkStoryManager.OpenStory(storyId);
        }
    }
}