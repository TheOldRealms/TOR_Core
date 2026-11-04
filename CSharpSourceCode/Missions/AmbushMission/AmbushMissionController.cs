using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Battle setup for the ambush mission. Clones the useful parts of vanilla
    /// SallyOutMissionController (reinforcement timer, attacker-activation
    /// deployment delay, troop-ratio balancing) and skips the castle-gate code
    /// that doesn't apply to a field ambush.
    ///
    /// All tune-ables pulled from <see cref="TORMissionAmbushModel"/>.
    /// </summary>
    public class AmbushMissionController : MissionLogic
    {
        // Grace window before we decide the scene has no authored retreat zone
        // and set a procedural fallback. ScriptComponentBehavior.OnInit on
        // AmbushRetreatZone isn't guaranteed to run before OnDeploymentFinished,
        // so checking immediately races with the engine and can turn a scene
        // that DOES have an authored zone into "fallback at player position"
        // (= instant extraction victory as soon as the player stands still).
        private const float FallbackGraceSeconds = 2f;

        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;
        private AmbushReinforcementSpawnTimer _reinforcementTimer;
        private BasicMissionTimer _fallbackGraceTimer;
        private bool _fallbackResolved;
        private TORMissionAmbushModel _model;
        // The two parties involved in the ambush. The controller works out
        // which battle side each is on at AfterStart time (reads
        // PartyBase.MapEventSide.MissionSide), so Phase D (player ambushes
        // AI) and Phase F (AI ambushes player) share this class with no
        // per-phase branching.
        private readonly PartyBase _ambusherParty;
        private readonly PartyBase _victimParty;
        private BattleSideEnum _ambusherSide;
        private BattleSideEnum _victimSide;

        // Per-class flat position + yaw lists built from the AmbushFormationLayout
        // result: for each AmbushFormation chunk with N troops bound to a spot
        // at (pos, yaw), the lists hold N copies of pos and yaw. OnAgentCreated
        // round-robins through the matching-class list, so agents distribute
        // across spots in proportion to each chunk's troop count.
        private readonly Dictionary<FormationClass, List<Vec3>> _attackerSpawnPositions
            = new Dictionary<FormationClass, List<Vec3>>();
        private readonly Dictionary<FormationClass, List<float>> _attackerSpawnYaws
            = new Dictionary<FormationClass, List<float>>();
        private readonly Dictionary<FormationClass, int> _attackerSpawnIndex
            = new Dictionary<FormationClass, int>();
        // Legacy fallback for scenes authored before the role-tag scheme —
        // uses the flat `ambush_attacker` tag. Only populated if the role-
        // specific lists come up empty.
        private readonly List<Vec3> _legacyAttackerSpawnPositions = new List<Vec3>();
        private int _legacyAttackerSpawnIndex;

        /// <summary>
        /// Auto-chunked formation list plus default spot assignments from
        /// <see cref="AmbushFormationLayout.Compute"/>. Computed at AfterStart
        /// time. Null if the scene uses the legacy flat <c>ambush_attacker</c>
        /// tag (no spot buckets) or has no attacker spawns at all. Readers:
        /// the deployment UI binds sidebar panels / spot markers to this.
        /// </summary>
        public AmbushLayoutResult Layout { get; private set; }

        // Snapshot of defender agents present at OnDeploymentFinished — the
        // "initial patrol". Reinforcement waves are NOT in this set. Phase 1
        // → Phase 2 transition fires when every member is inactive.
        private readonly List<Agent> _initialPatrol = new List<Agent>();
        private bool _initialPatrolDefeated;

        /// <summary>True once every member of the initial defender spawn is
        /// inactive. Readers: <see cref="AmbushEndLogic"/> gates Phase 2
        /// behaviours on this; <see cref="AmbushReinforcementSpawnTimer"/>
        /// applies the one-shot delay bonus when it flips.</summary>
        public bool IsInitialPatrolDefeated => _initialPatrolDefeated;

        /// <summary>Fires once, the frame <see cref="IsInitialPatrolDefeated"/>
        /// flips from false to true.</summary>
        public event System.Action InitialPatrolDefeated;

        /// <summary>True when the local player's party is the ambusher side
        /// (the player launched the ambush). False when the player is the
        /// victim (an AI party ambushed the player). Drives the deployment-UI
        /// gate — only the ambusher gets a placement screen; the victim is
        /// dropped straight into combat.</summary>
        public bool IsPlayerAmbusher
            => _ambusherParty != null
               && _ambusherParty.MobileParty == MobileParty.MainParty;

        public AmbushMissionController(PartyBase ambusherParty, PartyBase victimParty)
        {
            _ambusherParty = ambusherParty;
            _victimParty = victimParty;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            // DO NOT call AmbushRetreatZone.ResetRegistry() here. Scene scripts
            // (including AmbushRetreatZone.OnInit) fire during scene load, which
            // happens before MissionLogic.OnBehaviorInitialize. Clearing the
            // registry at this point wipes the zone that just registered —
            // every subsequent IsAgentInsideAnyZone check falls through to the
            // procedural fallback. Reset is done by the campaign behavior
            // immediately before MissionState.OpenNew so stale zones from any
            // prior scene are cleared BEFORE this scene's scripts fire.
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            AmbushRetreatZone.ProceduralFallbackRadius = _model.ProceduralFallbackRadius;

            // If the player is the victim (AI ambushed the player), there's no
            // placement choice — the mission kicks straight into combat. Keep
            // the vanilla deployment controller (it owns agent spawn kickoff
            // + player-agent handoff — reimplementing that broke the mission
            // in interesting ways: loading-screen hang when fully bypassed,
            // spectator view when half-done). Subscribe to its OnAfterSetupTeams
            // event instead: it fires on the first mission tick after the
            // scene is ready, which is exactly when vanilla has done the
            // per-side SetSpawnTroops + SetupAgentAIStates work and is just
            // waiting for the player to click Confirm. Call FinishDeployment
            // from there and the mission transitions as if Confirm had been
            // pressed.
            if (!IsPlayerAmbusher)
            {
                var deploymentController =
                    Mission.GetMissionBehavior<BattleDeploymentMissionController>();
                if (deploymentController != null)
                {
                    deploymentController.OnAfterSetupTeams += AutoFinishDeploymentForVictim;
                }
            }
        }

        private void AutoFinishDeploymentForVictim()
        {
            var deploymentController =
                Mission.GetMissionBehavior<BattleDeploymentMissionController>();
            if (deploymentController == null) return;
            deploymentController.OnAfterSetupTeams -= AutoFinishDeploymentForVictim;
            deploymentController.FinishDeployment();
        }

        public override void AfterStart()
        {
            base.AfterStart();
            // Resolve each party's battle side from its MapEventSide. Both
            // parties are expected to be on the PlayerMapEvent at this point
            // (set up by the opener via PlayerEncounter.RestartPlayerEncounter).
            _ambusherSide = _ambusherParty?.MapEventSide?.MissionSide ?? BattleSideEnum.Attacker;
            _victimSide = _victimParty?.MapEventSide?.MissionSide ?? BattleSideEnum.Defender;
            SetupInitialSpawn();
            CollectAttackerSpawnPositions();
        }

        /// <summary>
        /// Collects attacker spot entities from the scene (ambush_attacker_*
        /// tags), runs <see cref="AmbushFormationLayout.Compute"/> against
        /// the ambusher roster, and builds per-class flat teleport lists that
        /// <see cref="OnAgentCreated"/> consumes. Scener convention (short
        /// tags): attacker_infantry / attacker_archer / attacker_cavalry /
        /// attacker_horsearcher. If none exist, falls back to the legacy
        /// flat <c>ambush_attacker</c> tag with no layout computation.
        /// </summary>
        private void CollectAttackerSpawnPositions()
        {
            _attackerSpawnPositions.Clear();
            _attackerSpawnYaws.Clear();
            _attackerSpawnIndex.Clear();
            _legacyAttackerSpawnPositions.Clear();
            _legacyAttackerSpawnIndex = 0;
            Layout = null;

            var spots = CollectAmbushSpots();
            if (spots.Count == 0)
            {
                _legacyAttackerSpawnPositions.AddRange(CollectPositionsForTag("ambush_attacker"));
                Debug.Print(
                    "[AmbushMission] no typed attacker spots, legacy="
                    + _legacyAttackerSpawnPositions.Count);
                return;
            }

            var roster = _ambusherParty?.MemberRoster;
            if (roster == null || roster.Count == 0)
            {
                Debug.Print("[AmbushMission] no ambusher roster, skipping layout");
                return;
            }

            var mode = IsPlayerAmbusher ? LayoutMode.Player : LayoutMode.AI;
            Layout = AmbushFormationLayout.Compute(roster, spots, mode, _model);

            foreach (var chunk in Layout.Formations)
            {
                if (!chunk.AssignedSpot.HasValue) continue;
                var cls = chunk.Class;
                if (!_attackerSpawnPositions.ContainsKey(cls))
                {
                    _attackerSpawnPositions[cls] = new List<Vec3>();
                    _attackerSpawnYaws[cls] = new List<float>();
                    _attackerSpawnIndex[cls] = 0;
                }
                var spot = chunk.AssignedSpot.Value;
                for (int i = 0; i < chunk.Count; i++)
                {
                    _attackerSpawnPositions[cls].Add(spot.Position);
                    _attackerSpawnYaws[cls].Add(spot.Yaw);
                }
            }

            Debug.Print(
                "[AmbushMission] layout: " + Layout.Formations.Count + " formations, "
                + "spots found=" + spots.Count + " mode=" + mode);
        }

        private List<AmbushSpot> CollectAmbushSpots()
        {
            var list = new List<AmbushSpot>();
            AddSpotsForTag(list, "attacker_infantry",    SpotClass.Infantry);
            AddSpotsForTag(list, "attacker_archer",      SpotClass.Archer);
            AddSpotsForTag(list, "attacker_cavalry",     SpotClass.Cavalry);
            AddSpotsForTag(list, "attacker_horsearcher", SpotClass.HorseArcher);
            return list;
        }

        private void AddSpotsForTag(List<AmbushSpot> list, string tag, SpotClass cls)
        {
            foreach (var e in Mission.Scene.FindEntitiesWithTag(tag))
            {
                var frame = e.GetGlobalFrame();
                list.Add(new AmbushSpot
                {
                    Tag = tag,
                    Class = cls,
                    Bucket = cls.Bucket(),
                    Position = frame.origin,
                    Yaw = frame.rotation.f.AsVec2.RotationInRadians,
                });
            }
        }

        private List<Vec3> CollectPositionsForTag(string tag)
        {
            var list = new List<Vec3>();
            foreach (var e in Mission.Scene.FindEntitiesWithTag(tag))
                list.Add(e.GetGlobalFrame().origin);
            return list;
        }

        /// <summary>
        /// Fires per agent at creation time. For attacker humans we route to
        /// a role-matching scener-tagged slot; for defenders we don't touch
        /// anything (vanilla sp_defender_* placement is the authored default
        /// now, no more `dynamic_patrol_area_tag` teleport).
        /// </summary>
        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (agent == null || !agent.IsHuman) return;

            if (agent.Team == null || agent.Team.Side != _ambusherSide) return;

            var formationClass = agent.Character?.DefaultFormationClass ?? FormationClass.Infantry;
            if (_attackerSpawnPositions.TryGetValue(formationClass, out var positions)
                && positions.Count > 0)
            {
                int idx = _attackerSpawnIndex[formationClass]++;
                int at = idx % positions.Count;
                agent.TeleportToPosition(positions[at]);
                if (_attackerSpawnYaws.TryGetValue(formationClass, out var yaws)
                    && at < yaws.Count)
                {
                    float yaw = yaws[at];
                    var dir = new Vec2((float)System.Math.Cos(yaw), (float)System.Math.Sin(yaw));
                    agent.SetMovementDirection(in dir);
                }
                return;
            }

            // Legacy fallback: scene has no role-typed slots but has the flat
            // `ambush_attacker` tag. Mixes all classes in one list; worse but
            // still better than default spawn.
            if (_legacyAttackerSpawnPositions.Count > 0)
            {
                int idx = _legacyAttackerSpawnIndex++;
                agent.TeleportToPosition(
                    _legacyAttackerSpawnPositions[idx % _legacyAttackerSpawnPositions.Count]);
            }
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();

            // Attacker initial placement happens per-agent via OnAgentCreated
            // (role-aware teleport to scener-tagged slots). Defender initial
            // placement is left to vanilla sp_defender_* spawns inside the
            // sp_battle_set subtree — the scener dropped dynamic_patrol_area_tag
            // in favour of this. Reinforcement spawns on both sides use the
            // sp_*_reinforcement subchildren naturally.

            // Snapshot the initial-patrol defender agent set now. Reinforcement
            // waves spawn later via DefaultBattleMissionAgentSpawnLogic and are
            // NOT members. Phase 2 begins when all of these go inactive.
            _initialPatrol.Clear();
            if (Mission.DefenderTeam != null)
            {
                foreach (var a in Mission.DefenderTeam.ActiveAgents)
                {
                    if (a != null && a.IsHuman) _initialPatrol.Add(a);
                }
            }

            // Start the grace clock for the procedural-fallback decision. The
            // actual check happens in OnMissionTick once script OnInits have
            // reliably fired — doing it here races with the script lifecycle
            // and plants the fallback on the player, which auto-triggers
            // extraction. See FallbackGraceSeconds.
            _fallbackGraceTimer = new BasicMissionTimer();
        }

        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            // Tab-leave is disabled. The only exits are the extraction bar,
            // dying, getting wiped, or the stalemate timer. See mission-design.md.
            canLeave = false;
            TOR_Core.Utilities.TORCommon.Say("You can't flee the ambush like this — look for the extraction point.");
            return null;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_fallbackResolved && _fallbackGraceTimer != null
                && _fallbackGraceTimer.ElapsedTime >= FallbackGraceSeconds)
            {
                _fallbackResolved = true;
                if (AmbushRetreatZone.ActiveZones.Count == 0)
                {
                    var mainAgent = Mission.MainAgent;
                    if (mainAgent != null && mainAgent.IsActive())
                        AmbushRetreatZone.ProceduralFallbackPosition = mainAgent.Position;
                }
            }

            // Phase 1 → Phase 2 transition. Check once per tick while Phase 1
            // is still active; cheap since _initialPatrol is small.
            if (!_initialPatrolDefeated && _initialPatrol.Count > 0)
            {
                bool allDown = true;
                for (int i = 0; i < _initialPatrol.Count; i++)
                {
                    var a = _initialPatrol[i];
                    if (a != null && a.IsActive() && a.IsHuman) { allDown = false; break; }
                }
                if (allDown)
                {
                    _initialPatrolDefeated = true;
                    _reinforcementTimer?.OnInitialPatrolDefeated();
                    InitialPatrolDefeated?.Invoke();
                }
            }
        }

        private void SetupInitialSpawn()
        {
            if (_spawnLogic == null) return;

            // Read counts off the two parties directly. MapEventSide.TroopCount
            // is the battle-ready count (vanilla folds in wounded / etc.) so
            // we use that over raw roster size.
            int ambusherTotal = _ambusherParty?.MapEventSide?.TroopCount
                                ?? _ambusherParty?.MemberRoster?.TotalManCount ?? 0;
            int victimTotal = _victimParty?.MapEventSide?.TroopCount
                              ?? _victimParty?.MemberRoster?.TotalManCount ?? 0;

            // Battle-size budget split. Victim side gets the larger slice
            // because the "patrol" is the mission's visible force.
            float ratio = _model.TargetTotalTroopRatio;
            float inverse = 1f - ratio;
            int victimCap = (int)(_spawnLogic.BattleSize * ratio);
            int ambusherCap = (int)(_spawnLogic.BattleSize * inverse);

            // Tactics scaling. The player's hero contributes the bonus; it
            // applies to whichever side the player is on (ambusher in Phase D,
            // victim in Phase F — design doc § 4 asks for Tactics scaling on
            // the Phase F defensive side too).
            int tactics = Hero.MainHero?.GetSkillValue(DefaultSkills.Tactics) ?? 0;
            int tacticsBonus = (int)(tactics * _model.TacticsPerAmbushTroopBonus);
            bool playerIsAmbusher = _ambusherParty == PartyBase.MainParty;
            if (playerIsAmbusher) ambusherCap += tacticsBonus;
            else victimCap += tacticsBonus;

            if (ambusherTotal > ambusherCap) ambusherTotal = ambusherCap;
            if (victimTotal > victimCap) victimTotal = victimCap;

            // Map "ambusher" / "victim" totals back onto the Attacker /
            // Defender slot slots the vanilla spawn pipeline expects.
            int attackerTotal, defenderTotal;
            if (_ambusherSide == BattleSideEnum.Attacker)
            {
                attackerTotal = ambusherTotal;
                defenderTotal = victimTotal;
            }
            else
            {
                attackerTotal = victimTotal;
                defenderTotal = ambusherTotal;
            }

            // Victim (patrol): entire force spawns upfront. Fiction is "the
            // patrol being ambushed" — a 10% scout wave feels like 3 lost
            // scouts, not the real force. Reinforcements arrive as alarm
            // response later; 54 dynamic_patrol_area_tag spawn points.
            // Ambusher: entire force spawns upfront; no reinforcements for
            // the ambusher in the first-pass model.
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);

            var spawnSettings = new MissionSpawnSettings(
                MissionSpawnSettings.InitialSpawnMethod.FreeAllocation,
                MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer,
                MissionSpawnSettings.ReinforcementSpawnMethod.Fixed,
                0f, 0f, 0f, 0f, 0,
                defenderReinforcementBatchPercentage: _model.ReinforcementBatchFraction,
                attackerReinforcementBatchPercentage: _model.PlayerReinforcementBatchFraction);

            _spawnLogic.InitWithSinglePhase(
                defenderTotal, attackerTotal,
                defenderTotal, attackerTotal,
                spawnDefenders: false, spawnAttackers: false,
                in spawnSettings);

            _reinforcementTimer = new AmbushReinforcementSpawnTimer(_model);
            _spawnLogic.SetCustomReinforcementSpawnTimer(_reinforcementTimer);
        }
    }
}
