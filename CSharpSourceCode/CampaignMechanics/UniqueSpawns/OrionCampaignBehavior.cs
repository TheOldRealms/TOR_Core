using SandBox.View.Map;
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
        public const float OrionAthelLorenSpeed = 3.8f;
        public const float OrionOutsideAthelLorenSpeed = 2.4f;
        public const float OrionRetreatSpeedBonus = 5f;
        private const int OrionPartySize = 800;
        private const int OrionDefeatedCooldownYears = 4; // after being defeated orion only respawns after 4 respawn cycles have passed
        private const int OrionWarPlanStayDurationDays = 3; // after orion is OrionWarPlanArrivalDistance meters close to the target settlement, time duration to keep that behavior assuming it is not overriden by WarPlanHomeSiegePatrol or WarPlanLordHunt 
        private const float OrionWarPlanArrivalDistance = 40f;
        private const int OrionWarPlanTravelLimitDays = 10;
        private const int OrionHuntPrisonerLordThreshold = 3; // amount of athel loren lords an enemy lord should be holding prisoner at once for orion to start hunting them

        // the minimum and maximum days passed since the detection for orion to start hunting a lord
        private const float OrionHuntCaptureRevengeMinAgeDays = 3f;
        private const float OrionHuntCaptureRevengeMaxAgeDays = 15f;

        private const float OrionHuntArmySizeSoftSkip = 1500f; // party size to postpone orion's hunt in case they are still in an army

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

        [SaveableField(15)]
        private Dictionary<string, float> _orionFiefCaptureDayHeroID = [];

        [SaveableField(16)]
        private string _orionHuntTargetHeroId;

        [SaveableField(17)]
        private bool _orionWarPlanTimerStarted;

        [SaveableField(18)]
        private int _orionLastPickedWarPlanMode = -1;

        [SaveableField(19)]
        private int _orionWarPlanTravelDaysLeft;

        [SaveableField(20)]
        private Dictionary<string, float> _orionLastFiefCaptureDayHeroID = [];

        private bool _removingOrionByScript;
        private bool _repairingOrionDiplomacy;
        private bool _removingOrionMercenaryContract;

        private static readonly Color SpawnHudMessageColor = new Color(0.0f, 0.78f, 0.47f);
        private static readonly Color RetreatHudMessageColor = new Color(0.35f, 0.70f, 1.0f);
        private static readonly Color DefeatHudMessageColor = new Color(0.65f, 0.02f, 0.02f);
        private static readonly Color ThreatHudMessageColor = new Color(0.65f, 0.02f, 0.02f);

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
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
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
            dataStore.SyncData("_orionFiefCaptureDayHeroID", ref _orionFiefCaptureDayHeroID);
            dataStore.SyncData("_orionHuntTargetHeroId", ref _orionHuntTargetHeroId);
            dataStore.SyncData("_orionWarPlanTimerStarted", ref _orionWarPlanTimerStarted);
            dataStore.SyncData("_orionLastPickedWarPlanMode", ref _orionLastPickedWarPlanMode);
            dataStore.SyncData("_orionWarPlanTravelDaysLeft", ref _orionWarPlanTravelDaysLeft);
            dataStore.SyncData("_orionLastFiefCaptureDayHeroID", ref _orionLastFiefCaptureDayHeroID);
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
            const string debugStatusText = "orion state: {STATE}, next eligible year: {NEXT_ELIGIBLE_YEAR}, current year: {CURRENT_YEAR}, season: {SEASON}, party: {PARTY_STATUS}, queued InkStory: {QUEUED_InkStoryS}, war target: {WAR_TARGET}, plan: {PLAN}, target a: {TARGET_A}, target b: {TARGET_B}, hunt target: {HUNT_TARGET}, timer started: {TIMER_STARTED}, travel days left: {TRAVEL_DAYS_LEFT}, days left: {DAYS_LEFT}";

            _queuedOrionInkStorys ??= [];

            var orionParty = CurrentParty();
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
            debugStatus.SetTextVariable("HUNT_TARGET", _orionHuntTargetHeroId ?? noPartyText);
            debugStatus.SetTextVariable("TIMER_STARTED", _orionWarPlanTimerStarted.ToString());
            debugStatus.SetTextVariable("TRAVEL_DAYS_LEFT", _orionWarPlanTravelDaysLeft.ToString());
            debugStatus.SetTextVariable("DAYS_LEFT", _orionWarPlanDaysLeft.ToString());

            return debugStatus.ToString();
        }

        public string TestSpawnOrion()
        {
            var existingParty = CurrentParty();
            if (existingParty != null && existingParty.IsActive)
            {
                RemoveParty(existingParty);
            }

            _orionPartyId = null;
            _orionState = UniqueSpawnState.Inactive;
            _orionNextEligibleYear = -1;

            SpawnAtOak(false);

            return GetOrionDebugStatus();
        }

        public string TestRetreatOrion()
        {
            const string orionNotActiveText = "orion is not active.";

            var orionParty = CurrentParty();
            if (orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            _orionState = UniqueSpawnState.Active;
            CallBackToOak();

            return GetOrionDebugStatus();
        }

        public string TestRetreatCompleteOrion()
        {
            const string orionNotActiveText = "orion is not active.";

            var orionParty = CurrentParty();
            if (orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            _orionState = UniqueSpawnState.RetreatingToHome;
            PutBackIntoOak(orionParty);

            return GetOrionDebugStatus();
        }

        public string TestDefeatOrion()
        {
            const string orionNotActiveText = "orion is not active.";

            var orionParty = CurrentParty();
            if (orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            DefeatAndClearParty(false);

            return GetOrionDebugStatus();
        }

        public string TestResetOrion()
        {
            var orionParty = CurrentParty();
            if (orionParty != null && orionParty.IsActive)
            {
                RemoveParty(orionParty);
            }

            _orionPartyId = null;
            _orionState = UniqueSpawnState.Inactive;
            _orionNextEligibleYear = -1;
            _orionWarPlanDaysLeft = 0;
            _orionWarTargetFactionId = null;
            _orionWarPrimarySettlementId = null;
            _orionWarSecondarySettlementId = null;
            _orionHuntTargetHeroId = null;
            _orionWarPlanTimerStarted = false;
            _orionWarPlanTravelDaysLeft = 0;
            _queuedOrionInkStorys ??= [];
            _queuedOrionInkStorys.Clear();

            return GetOrionDebugStatus();
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            DisableSpawn();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _queuedOrionInkStorys ??= [];
            _orionWarPressureByFaction ??= [];
            _orionFiefCaptureDayHeroID ??= [];
            _orionLastFiefCaptureDayHeroID ??= [];

            SyncSeasonAfterLoad();
            _campaignStartDiplomacyRepairTicksLeft = 12;

            var orionParty = CurrentParty();
            if (orionParty == null)
            {
                DisableSpawn();
            }
            else
            {
                RepairSpellsingers(orionParty);
            }

            KeepClanIndependent();
            RepairDiplomacy();
        }

        private void SyncSeasonAfterLoad()
        {
            var currentSeasonIndex = (int)CampaignTime.Now.GetSeasonOfYear;
            var currentYear = CampaignTime.Now.GetYear;

            if (_lastCheckedSeasonIndex < 0 || _lastCheckedYear < 0)
            {
                _lastCheckedSeasonIndex = currentSeasonIndex;
                _lastCheckedYear = currentYear;
                return;
            }

            CheckSeasonTick();
            FixMissingParty();

            if (_orionWarPlanDaysLeft > 0 && !_orionWarPlanTimerStarted && _orionWarPlanTravelDaysLeft <= 0)
            {
                _orionWarPlanTravelDaysLeft = OrionWarPlanTravelLimitDays;
            }

            if (IsOakSeason() && _orionState == UniqueSpawnState.Active && CurrentParty() != null)
            {
                CallBackToOak();
            }
        }

        private void FixMissingParty()
        {
            if (CurrentParty() != null || (_orionState != UniqueSpawnState.Active && _orionState != UniqueSpawnState.RetreatingToHome))
            {
                return;
            }

            _orionPartyId = null;
            _orionWarPlanDaysLeft = 0;
            _orionState = IsOakSeason()
                ? UniqueSpawnState.RetreatedToHome
                : UniqueSpawnState.Inactive;
        }

        private bool IsOakSeason()
        {
            var currentSeason = CampaignTime.Now.GetSeasonOfYear;
            return currentSeason == CampaignTime.Seasons.Autumn ||
                   currentSeason == CampaignTime.Seasons.Winter;
        }

        private void DailyTick()
        {
            CheckSeasonTick();
            KeepClanIndependent();
            RepairDiplomacy();
            DecayWarPressure();
            ForgetOldCaptureRevenges();
            TickMacroPlan();
            OpenQueuedInkStoryOnMap();
        }

        private void HourlyTick()
        {
            RepairDiplomacyAfterCampaignStart();
            KeepOnOakRoad();
            KeepOnHuntTrail();
            OpenQueuedInkStoryOnMap();
        }

        private void RepairDiplomacyAfterCampaignStart()
        {
            if (_campaignStartDiplomacyRepairTicksLeft <= 0)
            {
                return;
            }

            _campaignStartDiplomacyRepairTicksLeft--;
            KeepClanIndependent();
            RepairDiplomacy();
        }

        private void AiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
        {
            if (party.GetUniqueSpawnComponent()?.UniqueSpawnId != "tor_unique_orion")
            {
                return;
            }

            if (IsOrionRetreating(party))
            {
                PointAtOak(party);

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

            AddMacroPlanScore(thinkParams);
        }

        private void AddMacroPlanScore(PartyThinkParams thinkParams)
        {
            const float patrolPlanScore = 650f; //
            const float raidPlanScore = 900f;

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                return;
            }

            var target = CurrentWarTarget();
            if (target == null)
            {
                return;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid && VillageCanBeRaided(target))
            {
                var raidBehavior = new AIBehaviorData(target, AiBehavior.RaidSettlement, MobileParty.NavigationType.Default, false, false, false);
                UniqueSpawnCampaignBehavior.AddOrUpdateBehaviorScore(thinkParams, raidBehavior, raidPlanScore);
                return;
            }

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
            if (clan.StringId != "wildhunt_clan_1" || newKingdom == null)
            {
                return;
            }

            KeepClanIndependent();
            RepairDiplomacy();
        }

        private void KeepClanIndependent()
        {
            if (_removingOrionMercenaryContract)
            {
                return;
            }

            var orionClan = Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1");
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

        private void AthelLorenJoinsOrion(IFaction faction1, IFaction faction2)
        {
            var orionClan = Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1");
            var athelLoren = AthelLoren();
            var playerFaction = Hero.MainHero.MapFaction;
            var playerClan = Hero.MainHero.Clan;

            if (playerFaction == athelLoren)
            {
                return;
            }

            var playerDeclaredOnOrion =
                (faction1 == orionClan && (faction2 == playerFaction || faction2 == playerClan)) ||
                (faction2 == orionClan && (faction1 == playerFaction || faction1 == playerClan));

            if (!playerDeclaredOnOrion || athelLoren.IsAtWarWith(playerFaction))
            {
                return;
            }

            // orion will mimic athel loren diplomacy before it can register a war with player faction
            DeclareWarAction.ApplyByDefault(athelLoren, playerFaction);
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            AthelLorenJoinsOrion(faction1, faction2);
            RepairDiplomacy();
        }

        private void OnPeaceMade(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
        {
            RepairDiplomacy();

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt && !HuntTargetStillValid())
            {
                RetryHunt();
            }
        }

        private void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege ||
                settlement == null ||
                (!settlement.IsTown && !settlement.IsCastle) ||
                oldOwner?.MapFaction != AthelLoren() ||
                capturerHero == null ||
                !CanHeroBeHuntedAfterCapture(capturerHero))
            {
                return;
            }

            _orionFiefCaptureDayHeroID ??= [];
            _orionLastFiefCaptureDayHeroID ??= [];

            var currentDay = (float)CampaignTime.Now.ToDays;
            var hasOldRevenge = _orionLastFiefCaptureDayHeroID.TryGetValue(capturerHero.StringId, out var latestRevengeDay) ||
                                _orionFiefCaptureDayHeroID.TryGetValue(capturerHero.StringId, out latestRevengeDay);

            if (!hasOldRevenge || currentDay - latestRevengeDay > OrionHuntCaptureRevengeMaxAgeDays)
            {
                _orionFiefCaptureDayHeroID[capturerHero.StringId] = currentDay;
            }

            _orionLastFiefCaptureDayHeroID[capturerHero.StringId] = currentDay;
        }

        private void CheckSeasonTick()
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
                SpringCheck();
            }
            else if (currentSeason == CampaignTime.Seasons.Autumn)
            {
                CallBackToOak();
            }
        }

        private void SpringCheck()
        {
            if (CampaignTime.Now.GetSeasonOfYear != CampaignTime.Seasons.Spring)
            {
                return;
            }

            if (AthelLorenIsGone())
            {
                DisableSpawn();
                return;
            }

            var existingOrionParty = CurrentParty();
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
            SpawnAtOak(returningFromOak);
        }

        private void SpawnAtOak(bool returningFromOak)
        {
            if (AthelLorenIsGone())
            {
                DisableSpawn();
                return;
            }

            var oakOfAges = OakOfAges();
            var orionClan = Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1");
            PrepareForUniqueSpawning(orionClan.Leader);
            PrepareSpellsingersForUniqueSpawning();

            _orionSpawnSerial++;
            var orionPartyId = $"{"tor_unique_orion"}_party_{_orionSpawnSerial}";

            var orionParty = UniqueSpawnPartyComponent.CreateUniqueSpawnParty(
                orionPartyId,
                "tor_unique_orion",
                oakOfAges,
                orionClan.Leader.Name,
                orionClan.DefaultPartyTemplate,
                orionClan,
                OrionPartySize);

            _orionPartyId = orionParty.StringId;
            _orionState = UniqueSpawnState.Active;
            _orionWarPlanDaysLeft = 0;
            _orionHuntTargetHeroId = null;
            _orionWarPlanTimerStarted = false;
            _orionWarPlanTravelDaysLeft = 0;

            RepairSpellsingers(orionParty);
            orionParty.Ai.SetDoNotMakeNewDecisions(false);
            PickNewMacroPlan();

            ReportSpawn(returningFromOak);
        }

        private void CallBackToOak()
        {
            const string retreatMessageText = "{=str_tor_unique_orion_retreat_started_message}Winter cold orion back";

            if (_orionState != UniqueSpawnState.Active)
            {
                return;
            }

            var orionParty = CurrentParty();
            if (orionParty == null)
            {
                return;
            }

            _orionState = UniqueSpawnState.RetreatingToHome;
            _orionWarPlanDaysLeft = 0;
            _orionWarPlanTravelDaysLeft = 0;
            ClearHunt();
            PointAtOak(orionParty);

            ShowHudMessage(new TextObject(retreatMessageText), RetreatHudMessageColor);
        }

        private void KeepOnOakRoad()
        {
            const float retreatCompletionDistance = 5f;

            if (_orionState != UniqueSpawnState.RetreatingToHome)
            {
                return;
            }

            var orionParty = CurrentParty();
            if (orionParty == null || orionParty.MapEvent != null)
            {
                return;
            }

            var oakOfAges = OakOfAges();
            PointAtOak(orionParty);

            if (orionParty.Position.DistanceSquared(oakOfAges.GatePosition) > retreatCompletionDistance * retreatCompletionDistance)
            {
                return;
            }

            PutBackIntoOak(orionParty);
        }

        private void PointAtOak(MobileParty orionParty)
        {
            var oakOfAges = OakOfAges();

            orionParty.Aggressiveness = 0f;
            orionParty.Ai.SetDoNotMakeNewDecisions(true);
            orionParty.SetMoveGoToPoint(oakOfAges.GatePosition, MobileParty.NavigationType.Default);
        }

        private void PutBackIntoOak(MobileParty orionParty)
        {
            _orionState = UniqueSpawnState.RetreatedToHome;
            _orionPartyId = null;
            _orionWarPlanDaysLeft = 0;
            _orionWarPlanTravelDaysLeft = 0;
            ClearHunt();

            RemoveParty(orionParty);
            DisableSpawn();
        }

        private void RemoveParty(MobileParty orionParty)
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
            DropHuntAfterTargetDefeat(mapEvent);

            if (_orionState == UniqueSpawnState.DefeatedCooldown || _orionState == UniqueSpawnState.RetreatedToHome)
            {
                return;
            }

            if (!MapEventHasOrionOnSide(mapEvent, mapEvent.DefeatedSide))
            {
                return;
            }

            DefeatAndClearParty(PlayerHelpedDefeatOrion(mapEvent));
        }

        private bool MapEventHasOrionOnSide(MapEvent mapEvent, BattleSideEnum side)
        {
            if (side == BattleSideEnum.None)
            {
                return false;
            }

            return mapEvent.GetMapEventSide(side).Parties.Any(mapEventParty =>
                mapEventParty.Party?.MobileParty?.GetUniqueSpawnComponent()?.UniqueSpawnId == "tor_unique_orion");
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

        private void DefeatAndClearParty(bool playerHelpedDefeatOrion)
        {
            if (_orionState == UniqueSpawnState.DefeatedCooldown)
            {
                return;
            }

            var orionParty = CurrentParty();
            if (orionParty != null && orionParty.IsActive)
            {
                RemoveParty(orionParty);
            }

            PutOnDefeatedCooldown(playerHelpedDefeatOrion);
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (destroyedParty?.GetUniqueSpawnComponent()?.UniqueSpawnId != "tor_unique_orion")
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

            PutOnDefeatedCooldown(false);
        }

        private void PutOnDefeatedCooldown(bool playerHelpedDefeatOrion)
        {
            const string defeatedMessageText = "{=str_tor_unique_orion_defeated_message}Orion gone 3 year";

            _orionPartyId = null;
            _orionState = UniqueSpawnState.DefeatedCooldown;
            _orionNextEligibleYear = CampaignTime.Now.GetYear + OrionDefeatedCooldownYears;
            _orionWarPlanDaysLeft = 0;
            _orionWarPlanTravelDaysLeft = 0;
            ClearHunt();

            DisableSpawn();

            if (playerHelpedDefeatOrion)
            {
                QueueInkStory("OrionDefeatedByPlayer");
                return;
            }
            ShowHudMessage(new TextObject(defeatedMessageText), DefeatHudMessageColor);
        }

        private MobileParty CurrentParty()
        {
            if (!string.IsNullOrWhiteSpace(_orionPartyId))
            {
                var savedParty = MobileParty.All.FirstOrDefault(party => party.StringId == _orionPartyId && party.IsActive);
                if (savedParty != null)
                {
                    return savedParty;
                }
            }

            var recoveredParty = MobileParty.All.FirstOrDefault(party =>
                party.IsActive &&
                party.GetUniqueSpawnComponent()?.UniqueSpawnId == "tor_unique_orion");

            if (recoveredParty != null)
            {
                _orionPartyId = recoveredParty.StringId;
            }

            return recoveredParty;
        }

        public static void RemoveOrionFromDefeatedPartyRosters(MapEvent mapEvent, MBReadOnlyList<MapEventParty> defeatedParties)
        {
            if (mapEvent.RetreatingSide != BattleSideEnum.None)
            {
                return;
            }

            foreach (var mapEventParty in defeatedParties)
            {
                if (!mapEventParty.Party.IsMobile ||
                    mapEventParty.Party.MobileParty.GetUniqueSpawnComponent()?.UniqueSpawnId != "tor_unique_orion")
                {
                    continue;
                }

                foreach (var orionHero in BattleHeroes())
                {
                    var orionCount = mapEventParty.Party.MemberRoster.GetTroopCount(orionHero.CharacterObject);
                    if (orionCount <= 0)
                    {
                        continue;
                    }

                    mapEventParty.Party.MemberRoster.RemoveTroop(orionHero.CharacterObject, orionCount);
                }
            }
        }

        private void PrepareForUniqueSpawning(Hero orionLeader)
        {
            PrepareHeroForParty(orionLeader);
        }

        private void PrepareHeroForParty(Hero hero)
        {
            DisableHeroAction.Apply(hero);
            hero.ChangeState(Hero.CharacterStates.Active);
            hero.HitPoints = hero.MaxHitPoints;
        }

        private void PrepareSpellsingersForUniqueSpawning()
        {
            foreach (var spellsingerHero in SpellsingerHeroes())
            {
                PrepareHeroForParty(spellsingerHero);
            }
        }

        private void RepairSpellsingers(MobileParty orionParty)
        {
            foreach (var spellsingerHero in SpellsingerHeroes())
            {
                if (spellsingerHero.PartyBelongedTo == orionParty &&
                    orionParty.MemberRoster.GetTroopCount(spellsingerHero.CharacterObject) > 0)
                {
                    spellsingerHero.ChangeState(Hero.CharacterStates.Active);
                    continue;
                }

                var ghostRosterCount = orionParty.MemberRoster.GetTroopCount(spellsingerHero.CharacterObject);
                if (ghostRosterCount > 0)
                {
                    orionParty.MemberRoster.RemoveTroop(spellsingerHero.CharacterObject, ghostRosterCount);
                }

                PrepareHeroForParty(spellsingerHero);
                AddHeroToPartyAction.Apply(spellsingerHero, orionParty, false);
            }
        }

        private static IEnumerable<Hero> BattleHeroes()
        {
            yield return Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1").Leader;

            foreach (var spellsingerHero in SpellsingerHeroes())
            {
                yield return spellsingerHero;
            }
        }

        private static IEnumerable<Hero> SpellsingerHeroes()
        {
            var firstSpellsinger = Hero.Find("tor_we_orion_spellsinger_1");
            if (firstSpellsinger != null)
            {
                yield return firstSpellsinger;
            }

            var secondSpellsinger = Hero.Find("tor_we_orion_spellsinger_2");
            if (secondSpellsinger != null)
            {
                yield return secondSpellsinger;
            }
        }

        private void DisableSpawn()
        {
            foreach (var orionHero in BattleHeroes())
            {
                if (orionHero.PartyBelongedTo?.GetUniqueSpawnComponent()?.UniqueSpawnId == "tor_unique_orion")
                {
                    continue;
                }

                DisableHeroAction.Apply(orionHero);
            }
        }

        private void RepairDiplomacy()
        {
            // mirrored diplomacy calls will between wild hunt and athel loren will stuck in a loop
            if (_repairingOrionDiplomacy)
            {
                return;
            }

            _repairingOrionDiplomacy = true;
            try
            {
                var orionClan = Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1");
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

        private void DecayWarPressure()
        {
            const float dailyPressureDecay = 0.97f;
            const float forgottenPressureThreshold = 1f;

            UniqueSpawnCampaignBehavior.DecayWarPressure(
                _orionWarPressureByFaction,
                dailyPressureDecay,
                forgottenPressureThreshold);
        }

        private void TickMacroPlan()
        {
            var orionParty = CurrentParty();
            if (_orionState != UniqueSpawnState.Active || orionParty == null)
            {
                return;
            }

            if (TryKeepOrStartHunt(orionParty))
            {
                return;
            }

            if (_orionWarPlanMode != UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol && TryPickHomeSiegePlan())
            {
                return;
            }

            if (!MacroPlanStillValid())
            {
                PickNewMacroPlan();
                return;
            }

            TickWarPlanClock(orionParty);

            if (_orionWarPlanDaysLeft <= 0)
            {
                PickNewMacroPlan();
            }
        }

        private bool MacroPlanStillValid()
        {
            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                return HuntTargetStillValid();
            }

            if (_orionWarPlanDaysLeft <= 0)
            {
                return false;
            }

            var primaryTarget = CurrentWarTarget();

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol)
            {
                return UniqueSpawnCampaignBehavior.IsBesiegedOwnedOriginalHomeFief(
                    primaryTarget,
                    AthelLoren(),
                    IsOriginalAthelLorenFief);
            }

            var targetFaction = WarTargetFaction();
            if (targetFaction == null || !AthelLoren().IsAtWarWith(targetFaction))
            {
                return false;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid)
            {
                return primaryTarget != null && VillageCanBeRaided(primaryTarget);
            }

            return primaryTarget != null;
        }

        private void TickWarPlanClock(MobileParty orionParty)
        {
            var target = CurrentWarTarget();
            if (target == null)
            {
                _orionWarPlanDaysLeft = 0;
                return;
            }

            if (!_orionWarPlanTimerStarted)
            {
                if (orionParty.Position.DistanceSquared(target.Position) <= OrionWarPlanArrivalDistance * OrionWarPlanArrivalDistance)
                {
                    _orionWarPlanTimerStarted = true;
                    _orionWarPlanDaysLeft = OrionWarPlanStayDurationDays;
                    return;
                }

                _orionWarPlanTravelDaysLeft--;
                if (_orionWarPlanTravelDaysLeft <= 0)
                {
                    _orionWarPlanDaysLeft = 0;
                }

                return;
            }

            _orionWarPlanDaysLeft--;
        }

        private void PickNewMacroPlan()
        {
            if (TryKeepOrStartHunt(CurrentParty()))
            {
                return;
            }

            if (TryPickHomeSiegePlan())
            {
                return;
            }

            var targetFaction = PickFactionThatHurtAthelLorenMost();
            if (targetFaction == null)
            {
                ClearWarPlan();
                return;
            }

            var lootableVillages = EnemyLootableVillages(targetFaction).ToList();
            var planWeights = WarPlanWeights(AthelLoren().CurrentTotalStrength, lootableVillages.Count, true);
            var rolledPlan = PickPlanMode(planWeights);

            foreach (var plan in PlanFallbackOrder(rolledPlan, planWeights))
            {
                if (TryAssignWarPlan(plan, targetFaction, lootableVillages))
                {
                    return;
                }
            }

            ClearWarPlan();
        }

        private IEnumerable<int> PlanFallbackOrder(int rolledPlan, Dictionary<int, int> planWeights)
        {
            var yieldedPlans = new HashSet<int>();

            if (planWeights.TryGetValue(rolledPlan, out var rolledWeight) && rolledWeight > 0)
            {
                yieldedPlans.Add(rolledPlan);
                yield return rolledPlan;
            }

            foreach (var planWeight in planWeights
                         .Where(pair => pair.Value > 0 && yieldedPlans.Add(pair.Key))
                         .OrderByDescending(pair => pair.Value))
            {
                yield return planWeight.Key;
            }
        }

        private bool TryAssignWarPlan(int plan, IFaction targetFaction, List<Settlement> lootableVillages)
        {
            if (plan == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid)
            {
                var raidTarget = UniqueSpawnCampaignBehavior.ClosestSettlementToAffiliatedBorder(lootableVillages, DistanceToAthelLorenBorder);
                if (raidTarget == null)
                {
                    return false;
                }

                AssignWarPlan(plan, targetFaction.StringId, raidTarget, null);
                return true;
            }

            if (plan == UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol)
            {
                var ownVillages = AthelLorenBorderVillagesFacing(targetFaction).Take(2).ToList();
                if (ownVillages.Count == 0)
                {
                    return false;
                }

                AssignWarPlan(
                    plan,
                    targetFaction.StringId,
                    ownVillages.ElementAtOrDefault(0),
                    ownVillages.ElementAtOrDefault(1));
                return true;
            }

            if (plan == UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol)
            {
                var enemySettlements = EnemyDeepPatrolSettlements(targetFaction).Take(2).ToList();
                if (enemySettlements.Count == 0)
                {
                    return false;
                }

                AssignWarPlan(
                    plan,
                    targetFaction.StringId,
                    enemySettlements.ElementAtOrDefault(0),
                    enemySettlements.ElementAtOrDefault(1));
                return true;
            }

            return false;
        }

        private bool TryPickHomeSiegePlan()
        {
            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                return HuntTargetStillValid();
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol && MacroPlanStillValid())
            {
                return true;
            }

            var besiegedAthelLorenFief = UniqueSpawnCampaignBehavior.BesiegedOwnedOriginalHomeFief(
                AthelLoren(),
                IsOriginalAthelLorenFief,
                DistanceToAthelLorenBorder);

            if (besiegedAthelLorenFief == null)
            {
                return false;
            }

            AssignWarPlan(
                UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol,
                null,
                besiegedAthelLorenFief,
                null);

            return true;
        }

        private void AssignWarPlan(int plan, string targetFactionId, Settlement primaryTarget, Settlement secondaryTarget)
        {
            _orionWarPlanMode = plan;
            _orionWarTargetFactionId = targetFactionId;
            _orionWarPrimarySettlementId = primaryTarget?.StringId;
            _orionWarSecondarySettlementId = secondaryTarget?.StringId;
            _orionWarPlanDaysLeft = OrionWarPlanStayDurationDays;
            _orionWarPlanTimerStarted = false;
            _orionWarPlanTravelDaysLeft = OrionWarPlanTravelLimitDays;
            _orionWarTargetSwapIndex++;

            if (plan != UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol)
            {
                _orionLastPickedWarPlanMode = plan;
            }

            ClearHuntTargetOnly();

            var orionParty = CurrentParty();
            if (orionParty != null)
            {
                orionParty.Aggressiveness = 1f;
                orionParty.Ai.SetDoNotMakeNewDecisions(false);
            }

            ReportMovingToPlayerland(plan, primaryTarget);
        }

        private int PickPlanMode(Dictionary<int, int> planWeights)
        {
            var totalWeight = planWeights.Values.Sum();
            if (totalWeight <= 0)
            {
                return UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol;
            }

            var roll = MBRandom.RandomInt(totalWeight);

            foreach (var planWeight in planWeights)
            {
                if (roll < planWeight.Value)
                {
                    return planWeight.Key;
                }

                roll -= planWeight.Value;
            }

            return UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol;
        }

        private Dictionary<int, int> WarPlanWeights(float athelLorenStrength, int lootableVillageCount, bool avoidLastPlan)
        {
            const int weakAthelLorenStrengthThreshold = 5500; // can be changed to a dynamic value depending on eonir clans migrating to athel loren
            const int manyLootableVillagesThreshold = 20; // can be changed to a dynamic value depending on the fronts open against athel loren

            var planWeights = new Dictionary<int, int>
            {
                [UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol] = 20,
                [UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol] = 70,
                [UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid] = 10
            };

            if (athelLorenStrength < weakAthelLorenStrengthThreshold)
            {
                planWeights[UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol] = 60;
                planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol] = 20;
                planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid] = 10;
            }
            else if (lootableVillageCount >= manyLootableVillagesThreshold)
            {
                planWeights[UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol] = 35;
                planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol] = 35;
                planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid] = 30;
            }

            if (lootableVillageCount <= 0)
            {
                planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol] += planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid];
                planWeights[UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid] = 0;
            }

            if (avoidLastPlan && planWeights.ContainsKey(_orionLastPickedWarPlanMode))
            {
                var otherWeight = planWeights
                    .Where(pair => pair.Key != _orionLastPickedWarPlanMode)
                    .Sum(pair => pair.Value);

                if (otherWeight > 0)
                {
                    planWeights[_orionLastPickedWarPlanMode] = 0;
                }
            }

            return planWeights;
        }

        public string GetOrionPlanWeightDebug()
        {
            const string noTargetText = "no valid war faction.";

            var targetFaction = PickFactionThatHurtAthelLorenMost();
            if (targetFaction == null)
            {
                return noTargetText;
            }

            var lootableVillageCount = EnemyLootableVillages(targetFaction).Count();
            var rawWeights = WarPlanWeights(AthelLoren().CurrentTotalStrength, lootableVillageCount, false);
            var effectiveWeights = WarPlanWeights(AthelLoren().CurrentTotalStrength, lootableVillageCount, true);

            return "target faction: " + targetFaction.Name +
                   "\nathel loren strength: " + AthelLoren().CurrentTotalStrength.ToString("0") +
                   "\nlootable villages: " + lootableVillageCount +
                   "\nlast picked: " + WarPlanName(_orionLastPickedWarPlanMode) +
                   "\nraw weights: " + PlanWeightText(rawWeights) +
                   "\neffective weights: " + PlanWeightText(effectiveWeights) +
                   "\nactive hunt candidates: " + HuntCandidates().Count();
        }

        private string PlanWeightText(Dictionary<int, int> weights)
        {
            return "own=" + weights[UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol] +
                   ", deep=" + weights[UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol] +
                   ", raid=" + weights[UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid];
        }

        private string WarPlanName(int plan)
        {
            if (plan == UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol)
            {
                return "own village patrol";
            }

            if (plan == UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol)
            {
                return "enemy deep patrol";
            }

            if (plan == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid)
            {
                return "enemy village raid";
            }

            if (plan == UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol)
            {
                return "home siege patrol";
            }

            if (plan == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                return "lord hunt";
            }

            return "none";
        }

        public string TestSetOrionPlan(string planName)
        {
            const string orionNotActiveText = "orion is not active.";
            const string noTargetText = "no suitable target found.";

            var orionParty = CurrentParty();
            if (_orionState != UniqueSpawnState.Active || orionParty == null)
            {
                return new TextObject(orionNotActiveText).ToString();
            }

            var normalizedPlanName = planName?.Trim().ToLowerInvariant();
            if (normalizedPlanName == "hunt")
            {
                if (!CanStartHuntFor(Hero.MainHero))
                {
                    return "check CanStartHuntFor for player.";
                }

                StartHunt(Hero.MainHero, true);
                KeepOnHuntTrail(orionParty);
                return GetOrionDebugStatus();
            }

            if (normalizedPlanName == "siege")
            {
                var siegeTarget = UniqueSpawnCampaignBehavior.BesiegedOwnedOriginalHomeFief(
                                      AthelLoren(),
                                      IsOriginalAthelLorenFief,
                                      DistanceToAthelLorenBorder) ??
                                  Settlement.All
                                      .Where(settlement => UniqueSpawnCampaignBehavior.IsOwnedOriginalHomeFief(settlement, AthelLoren(), IsOriginalAthelLorenFief))
                                      .OrderBy(DistanceToAthelLorenBorder)
                                      .FirstOrDefault();

                if (siegeTarget == null)
                {
                    return noTargetText;
                }

                AssignWarPlan(UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol, null, siegeTarget, null);
                return GetOrionDebugStatus();
            }

            var targetFaction = PickFactionThatHurtAthelLorenMost();
            if (targetFaction == null)
            {
                return noTargetText;
            }

            if (normalizedPlanName == "own")
            {
                var ownVillages = AthelLorenBorderVillagesFacing(targetFaction).Take(2).ToList();
                if (ownVillages.Count == 0)
                {
                    return noTargetText;
                }

                AssignWarPlan(
                    UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol,
                    targetFaction.StringId,
                    ownVillages.ElementAtOrDefault(0),
                    ownVillages.ElementAtOrDefault(1));
                return GetOrionDebugStatus();
            }

            if (normalizedPlanName == "deep")
            {
                var enemySettlements = EnemyDeepPatrolSettlements(targetFaction).Take(2).ToList();
                if (enemySettlements.Count == 0)
                {
                    return noTargetText;
                }

                AssignWarPlan(
                    UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol,
                    targetFaction.StringId,
                    enemySettlements.ElementAtOrDefault(0),
                    enemySettlements.ElementAtOrDefault(1));
                return GetOrionDebugStatus();
            }

            if (normalizedPlanName == "raid")
            {
                var raidTarget = UniqueSpawnCampaignBehavior.ClosestSettlementToAffiliatedBorder(
                    EnemyLootableVillages(targetFaction),
                    DistanceToAthelLorenBorder);

                if (raidTarget == null)
                {
                    return noTargetText;
                }

                AssignWarPlan(
                    UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid,
                    targetFaction.StringId,
                    raidTarget,
                    null);
                return GetOrionDebugStatus();
            }

            return "usage: tor.orion_plan own|deep|raid|siege|hunt";
        }

        private bool TryKeepOrStartHunt(MobileParty orionParty)
        {
            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                if (HuntTargetStillValid())
                {
                    KeepOnHuntTrail(orionParty);
                    return true;
                }

                ClearHunt();
            }

            return TryPickHuntTarget(orionParty);
        }

        private bool TryPickHuntTarget(MobileParty orionParty)
        {
            if (orionParty == null)
            {
                return false;
            }

            foreach (var candidate in HuntCandidates())
            {
                if (!CanStartHuntFor(candidate))
                {
                    continue;
                }

                StartHunt(candidate, candidate == Hero.MainHero);
                KeepOnHuntTrail(orionParty);
                return true;
            }

            return false;
        }

        private IEnumerable<Hero> HuntCandidates()
        {
            _orionFiefCaptureDayHeroID ??= [];
            _orionLastFiefCaptureDayHeroID ??= [];

            var yieldedHeroIds = new HashSet<string>();
            var currentDay = (float)CampaignTime.Now.ToDays;

            foreach (var Revenge in _orionFiefCaptureDayHeroID
                         .Where(pair => currentDay - pair.Value >= OrionHuntCaptureRevengeMinAgeDays && currentDay - LatestCaptureRevengeDay(pair.Key, pair.Value) <= OrionHuntCaptureRevengeMaxAgeDays)
                         .OrderByDescending(pair => LatestCaptureRevengeDay(pair.Key, pair.Value)))
            {
                var hero = Hero.Find(Revenge.Key);
                if (hero != null && yieldedHeroIds.Add(hero.StringId))
                {
                    yield return hero;
                }
            }

            foreach (var party in MobileParty.All.Where(party => party.IsActive && party.LeaderHero != null))
            {
                if (CapturedAsraiLordCount(party) < OrionHuntPrisonerLordThreshold)
                {
                    continue;
                }

                if (yieldedHeroIds.Add(party.LeaderHero.StringId))
                {
                    yield return party.LeaderHero;
                }
            }
        }

        private void ForgetOldCaptureRevenges()
        {
            _orionFiefCaptureDayHeroID ??= [];
            _orionLastFiefCaptureDayHeroID ??= [];

            var currentDay = (float)CampaignTime.Now.ToDays;
            foreach (var heroId in _orionFiefCaptureDayHeroID.Keys.ToList())
            {
                if (currentDay - LatestCaptureRevengeDay(heroId, _orionFiefCaptureDayHeroID[heroId]) > OrionHuntCaptureRevengeMaxAgeDays)
                {
                    _orionFiefCaptureDayHeroID.Remove(heroId);
                    _orionLastFiefCaptureDayHeroID.Remove(heroId);
                }
            }
        }

        private float LatestCaptureRevengeDay(string heroId, float fallbackDay)
        {
            _orionLastFiefCaptureDayHeroID ??= [];
            return _orionLastFiefCaptureDayHeroID.TryGetValue(heroId, out var latestRevengeDay)
                ? latestRevengeDay
                : fallbackDay;
        }

        private int CapturedAsraiLordCount(MobileParty party)
        {
            return party.PrisonRoster.GetTroopRoster()
                .Select(rosterElement => rosterElement.Character?.HeroObject)
                .Count(hero => hero != null && hero.IsLord && hero.MapFaction == AthelLoren());
        }

        private bool CanHeroBeHuntedAfterCapture(Hero hero)
        {
            var party = hero.PartyBelongedTo;
            if (party == null)
            {
                return false;
            }

            if (party.Army != null)
            {
                return party.Army.LeaderParty == party && party.LeaderHero == hero;
            }

            return party.LeaderHero == hero;
        }

        private bool CanStartHuntFor(Hero hero)
        {
            if (!HuntTargetHeroIsValid(hero) || hero.MapFaction == null || !hero.MapFaction.IsAtWarWith(AthelLoren()))
            {
                return false;
            }

            var party = HuntTargetParty(hero);
            return party != null && HuntTargetManCount(party) <= OrionHuntArmySizeSoftSkip;
        }

        private bool HuntTargetStillValid()
        {
            var targetHero = CurrentHuntTargetHero();
            if (!HuntTargetHeroIsValid(targetHero) || targetHero.MapFaction == null || !targetHero.MapFaction.IsAtWarWith(AthelLoren()))
            {
                return false;
            }

            return HuntTargetParty(targetHero) != null;
        }

        private bool HuntTargetHeroIsValid(Hero hero)
        {
            return hero != null &&
                   !hero.IsDead &&
                   !hero.IsDisabled &&
                   !hero.IsPrisoner;
        }

        private MobileParty HuntTargetParty(Hero hero)
        {
            var party = hero == Hero.MainHero
                ? MobileParty.MainParty
                : hero?.PartyBelongedTo;

            if (party == null || !party.IsActive || party.IsDisbanding)
            {
                return null;
            }

            return party;
        }

        private int HuntTargetManCount(MobileParty party)
        {
            if (party.Army != null)
            {
                return party.Army.TotalManCount;
            }

            return party.MemberRoster.TotalManCount;
        }

        private Hero CurrentHuntTargetHero()
        {
            return string.IsNullOrWhiteSpace(_orionHuntTargetHeroId)
                ? null
                : Hero.Find(_orionHuntTargetHeroId);
        }

        private void StartHunt(Hero targetHero, bool showPlayerWarning)
        {
            _orionWarPlanMode = UniqueSpawnCampaignBehavior.WarPlanLordHunt;
            _orionHuntTargetHeroId = targetHero.StringId;
            _orionWarTargetFactionId = targetHero.MapFaction?.StringId;
            _orionWarPrimarySettlementId = null;
            _orionWarSecondarySettlementId = null;
            _orionWarPlanDaysLeft = OrionWarPlanStayDurationDays;
            _orionWarPlanTimerStarted = true;
            _orionWarPlanTravelDaysLeft = 0;

            if (showPlayerWarning)
            {
                const string playerHuntMessageText = "{=str_tor_unique_orion_hunt_player}Your scouts report the God-King of the forest is following your trail, seeking vengeance for his slain kin.";
                ShowHudMessage(new TextObject(playerHuntMessageText), ThreatHudMessageColor);
            }
        }

        private void KeepOnHuntTrail()
        {
            KeepOnHuntTrail(CurrentParty());
        }

        private void KeepOnHuntTrail(MobileParty orionParty)
        {
            if (_orionState != UniqueSpawnState.Active ||
                _orionWarPlanMode != UniqueSpawnCampaignBehavior.WarPlanLordHunt ||
                orionParty == null ||
                orionParty.MapEvent != null)
            {
                return;
            }

            if (!HuntTargetStillValid())
            {
                RetryHunt();
                return;
            }

            var targetParty = HuntTargetParty(CurrentHuntTargetHero());
            if (targetParty == null || targetParty == orionParty)
            {
                RetryHunt();
                return;
            }

            orionParty.Aggressiveness = 1f;
            orionParty.Ai.SetDoNotMakeNewDecisions(true);

            if (orionParty.DefaultBehavior != AiBehavior.EngageParty || orionParty.TargetParty != targetParty)
            {
                orionParty.SetMoveEngageParty(targetParty, MobileParty.NavigationType.Default);
            }
        }

        private void DropHuntAfterTargetDefeat(MapEvent mapEvent)
        {
            if (_orionWarPlanMode != UniqueSpawnCampaignBehavior.WarPlanLordHunt || mapEvent.DefeatedSide == BattleSideEnum.None)
            {
                return;
            }

            var targetHero = CurrentHuntTargetHero();
            if (targetHero == null || !MapEventHasHeroOnSide(mapEvent, mapEvent.DefeatedSide, targetHero))
            {
                return;
            }

            RetryHunt();
        }

        private bool MapEventHasHeroOnSide(MapEvent mapEvent, BattleSideEnum side, Hero hero)
        {
            return mapEvent.GetMapEventSide(side).Parties.Any(mapEventParty =>
                (hero == Hero.MainHero && mapEventParty.Party == PartyBase.MainParty) ||
                (mapEventParty.Party != null && mapEventParty.Party.MemberRoster.GetTroopCount(hero.CharacterObject) > 0));
        }

        private void RetryHunt()
        {
            ClearHunt();

            var orionParty = CurrentParty();
            if (_orionState == UniqueSpawnState.Active && orionParty != null)
            {
                TryPickHuntTarget(orionParty);
            }
        }

        private void ClearHunt()
        {
            ClearHuntTargetOnly();

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                _orionWarPlanMode = UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol;
                _orionWarPlanDaysLeft = 0;
                _orionWarPlanTimerStarted = false;
                _orionWarPlanTravelDaysLeft = 0;
            }

            var orionParty = CurrentParty();
            if (_orionState == UniqueSpawnState.Active && orionParty != null)
            {
                orionParty.Aggressiveness = 1f;
                orionParty.Ai.SetDoNotMakeNewDecisions(false);
            }
        }

        private void ClearHuntTargetOnly()
        {
            _orionHuntTargetHeroId = null;
        }

        public TextObject GetOrionBehaviorText(MobileParty party)
        {
            var target = CurrentWarTarget();

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanLordHunt)
            {
                var targetHero = CurrentHuntTargetHero();
                if (targetHero == null)
                {
                    return TextObject.GetEmpty();
                }

                var huntingText = new TextObject("{=str_tor_unique_orion_behavior_hunting}Hunting for {TARGET_HERO}.");
                huntingText.SetTextVariable("TARGET_HERO", targetHero.Name);
                return huntingText;
            }

            if (party != null && (party.ShortTermBehavior == AiBehavior.EngageParty || MobileParty.IsFleeBehavior(party.ShortTermBehavior)))
            {
                return TextObject.GetEmpty();
            }

            if (target == null)
            {
                return TextObject.GetEmpty();
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanHomeSiegePatrol)
            {
                var protectingText = new TextObject("{=str_tor_unique_orion_behavior_protecting}Protecting {TARGET_SETTLEMENT}."); // protecting an under attack settlement of athel loren
                protectingText.SetTextVariable("TARGET_SETTLEMENT", target.Name);
                return protectingText;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol)
            {
                var watchingText = new TextObject("{=str_tor_unique_orion_behavior_keeping_watch}Keeping watch of {TARGET_SETTLEMENT}."); // patrolling athel loren village
                watchingText.SetTextVariable("TARGET_SETTLEMENT", target.Name);
                return watchingText;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol)
            {
                var ravagingText = new TextObject("{=str_tor_unique_orion_behavior_ravaging}Ravaging {TARGET_SETTLEMENT}."); // looking for enemies in enemy territory
                ravagingText.SetTextVariable("TARGET_SETTLEMENT", target.Name);
                return ravagingText;
            }

            if (_orionWarPlanMode == UniqueSpawnCampaignBehavior.WarPlanEnemyVillageRaid)
            {
                var raidingText = new TextObject("{=str_tor_unique_orion_behavior_raiding}Raiding {TARGET_SETTLEMENT}."); // Raiding enemy villages
                raidingText.SetTextVariable("TARGET_SETTLEMENT", target.Name);
                return raidingText;
            }

            return TextObject.GetEmpty();
        }

        private void ReportMovingToPlayerland(int plan, Settlement target)
        {
            if ((plan != UniqueSpawnCampaignBehavior.WarPlanOwnVillagePatrol && plan != UniqueSpawnCampaignBehavior.WarPlanEnemyDeepPatrol) ||
                target == null ||
                target.MapFaction == null)
            {
                return;
            }

            var playerFaction = Hero.MainHero.MapFaction;
            if (target.OwnerClan != Clan.PlayerClan && target.MapFaction != playerFaction)
            {
                return;
            }

            const string playerLandsMessageText = "{=str_tor_unique_orion_to_player_land}Your scouts report The God-King of the Forest is coming towards your lands";
            ShowHudMessage(new TextObject(playerLandsMessageText), ThreatHudMessageColor);
        }

        private IFaction PickFactionThatHurtAthelLorenMost()
        {
            var athelLoren = AthelLoren();
            var orionClan = Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1");

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
                    DistanceToAthelLorenBorder)
                .Where(settlement => settlement.StringId != "castle_BK2");
        }

        private IEnumerable<Settlement> EnemyLootableVillages(IFaction targetFaction)
        {
            return UniqueSpawnCampaignBehavior.EnemyLootableVillages(
                targetFaction,
                VillageCanBeRaided,
                DistanceToAthelLorenBorder);
        }

        private float DistanceToAthelLorenBorder(Settlement settlement)
        {
            return UniqueSpawnCampaignBehavior.DistanceToHomeFactionBorder(
                AthelLoren(),
                OakOfAges(),
                settlement);
        }

        private bool VillageCanBeRaided(Settlement settlement)
        {
            return settlement != null &&
                   settlement.IsVillage &&
                   !settlement.IsRaided &&
                   (!settlement.IsUnderRaid || settlement.LastAttackerParty == CurrentParty()) &&
                   settlement.MapFaction != null &&
                   Clan.FindFirst(clan => clan.StringId == "wildhunt_clan_1").IsAtWarWith(settlement.MapFaction);
        }

        private Settlement CurrentWarTarget()
        {
            return UniqueSpawnCampaignBehavior.CurrentWarTarget(
                _orionWarPrimarySettlementId,
                _orionWarSecondarySettlementId,
                _orionWarTargetSwapIndex);
        }

        private IFaction WarTargetFaction()
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

        private void ClearWarPlan()
        {
            _orionWarTargetFactionId = null;
            _orionWarPrimarySettlementId = null;
            _orionWarSecondarySettlementId = null;
            _orionHuntTargetHeroId = null;
            _orionWarPlanDaysLeft = 0;
            _orionWarPlanTimerStarted = false;
            _orionWarPlanTravelDaysLeft = 0;
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
            return Settlement.Find("oak_of_ages");
        }

        private void ReportSpawn(bool returningFromOak)
        {
            const string spawnedMessageText = "{=str_tor_unique_orion_spawned_message}orion spawns";
            const string returnedMessageText = "{=str_tor_unique_orion_returned_message}orion returns";

            var message = returningFromOak
                ? new TextObject(returnedMessageText)
                : new TextObject(spawnedMessageText);

            if (PlayerShouldSeeInkStory())
            {
                QueueInkStory("OrionSpawned");
                return;
            }

            ShowHudMessage(message, SpawnHudMessageColor);
        }

        private bool PlayerShouldSeeInkStory()
        {
            var athelLoren = AthelLoren();
            var playerFaction = Hero.MainHero.MapFaction;

            return Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI ||
                   playerFaction == athelLoren ||
                   playerFaction.IsAtWarWith(athelLoren);
        }

        private void ShowHudMessage(TextObject message, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), color));
        }

        private void QueueInkStory(string storyId)
        {
            _queuedOrionInkStorys ??= [];

            if (_queuedOrionInkStorys.Contains(storyId))
            {
                return;
            }

            _queuedOrionInkStorys.Add(storyId);
        }

        private void OpenQueuedInkStoryOnMap()
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