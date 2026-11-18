using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Helpers;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using TOR_Core.Extensions;
using TOR_Core.Missions;
using TOR_Core.Missions.AmbushMission;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Player side of the ambush system. Manages stealth state, menus, cooldowns, and mission handoff for both ambusher and victim flows.</summary>
    public class AmbushCampaignBehavior : CampaignBehaviorBase
    {
        public const float StealthRadius = 15f;
        public const float StrikeRadius = 15f;
        public const int CooldownHours = 6;
        public const int MaxTargetSlots = 6;

        /// <summary>Reserved for future terrain-aware detection checks.</summary>
        public const float StealthDetectionRadius = 3f;

        public static readonly InputKey StealthToggleKey = InputKey.X;

        public const string StealthMenuId = "tor_ambush_stealth_menu";
        public const string AmbushEscapeMenuId = "tor_ambush_escape_menu";
        public const string AmbushFailedMenuId = "tor_ambush_failed_menu";
        public const string AmbushedIntroMenuId = "tor_ambushed_intro_menu";
        public const string AmbushedFledOutcomeMenuId = "tor_ambushed_fled_outcome_menu";
        // Short hop (map units) to break engagement when picking "escape".
        private const float AmbushEscapeTeleportDistance = 3f;

        public static readonly string[] AmbushScenes =
        {
            "TOR_ambush_forest_001",
            "TOR_ambush_forest_002",
            "TOR_ambush_forest_003",
        };

        /// <summary>Pick a random forest ambush scene.</summary>
        public static string PickAmbushScene()
        {
            var scene = AmbushScenes[MBRandom.RandomInt(AmbushScenes.Length)];
            TORCommon.Log(NLog.LogLevel.Info, "[Ambush] PickAmbushScene -> " + scene);
            return scene;
        }

        private bool _playerInStealth;
        private CampaignTime _stealthEnteredAt = CampaignTime.Zero;

        // Prep state. Party is held between hotkey and finalize; IsPlayerInStealth stays false until finalize.
        private bool _playerPreppingStealth;
        private CampaignTime _playerStealthPrepStartedAt = CampaignTime.Zero;

        private readonly List<MobileParty> _currentTargets = new List<MobileParty>();
        private CampaignTimeControlMode _cachedTimeControlMode = CampaignTimeControlMode.StoppablePlay;

        // Real-time hotkey cooldown so panic double-tap can't toggle off stealth.
        private float _lastHotkeyRealTime = -10f;
        private const float HotkeyInputCooldownSeconds = 2f;

        /// <summary>Loudness tally from the most recent ambush mission. Drives the cooldown length.</summary>
        public int LastAmbushLoudness { get; set; }

        /// <summary>Timestamp of the last successful detection roll against the main party.</summary>
        private CampaignTime _spottedAt = CampaignTime.Zero;

        /// <summary>Cached hostile observers in detection range. Rebuilt each quarter-hour tick.</summary>
        private readonly List<MobileParty> _cachedObserversInRange = new List<MobileParty>();

        public CampaignTime PlayerSpottedAt => _spottedAt;
        public IReadOnlyList<MobileParty> CachedObserversInRange => _cachedObserversInRange;

        public bool IsPlayerInStealth => _playerInStealth;
        public bool IsPlayerPreppingStealth => _playerPreppingStealth;
        public CampaignTime PlayerStealthPrepStartedAt => _playerStealthPrepStartedAt;
        public CampaignTime PlayerAmbushCooldownUntil
            => Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>()
                ?.GetCooldownExpiresAt(MobileParty.MainParty) ?? CampaignTime.Zero;
        public CampaignTime StealthEnteredAt => _stealthEnteredAt;
        public bool IsOnAmbushCooldown
            => Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>()
                ?.IsOnAmbushCooldown(MobileParty.MainParty) ?? false;

        /// <summary>Resolution of the most recent ambush mission. Consumed by the post-mission flow.</summary>
        public AmbushResolution LastResolution { get; set; } = AmbushResolution.None;

        /// <summary>True when the player was the ambusher on the most recent mission.</summary>
        public bool PlayerWasAmbusher { get; set; }

        /// <summary>Ambusher's hero went down during the engagement.</summary>
        public bool LastAmbusherLeaderDown { get; set; }

        /// <summary>Victim's hero went down during the engagement.</summary>
        public bool LastVictimLeaderDown { get; set; }

        /// <summary>Set when a TotalVictory fires. Drives the post-mission cleanup to unstall vanilla's encounter state machine.</summary>
        public bool PendingPostAmbushCleanup { get; set; }

        // Forced UnstoppablePlay for a short burst so vanilla's encounter state machine ticks to the loot UI.
        private const int PostAmbushForceTickCount = 10;
        private int _postAmbushForceTicksRemaining;

        /// <summary>Attacker troops outside the retreat zone when extraction completed. Consumed to wound survivors.</summary>
        public int ExtractionTroopsLeftBehind { get; set; }

        /// <summary>Target of the most recent ambush, needed by the Escape menu since MapEvent is already torn down.</summary>
        private MobileParty _lastAmbushTarget;

        /// <summary>Flag so OnPlayerBattleEnd can recognise the follow-up field battle after a victim flee.</summary>
        private bool _followUpFieldBattlePending;

        // Bluff mechanic: flip IsInfoHidden on while ambushing so vanilla UI renders "?" for troop counts.

        // Roster snapshot captured at mission-start. Drives wound-on-flee and remove-on-double-flee.
        private Dictionary<CharacterObject, int> _ambushedRosterSnapshot;

        /// <summary>Ambusher pending a follow-up battle. Deferred one tick to avoid racing vanilla encounter cleanup.</summary>
        private MobileParty _pendingFollowUpAmbusher;

        /// <summary>Queued victim-side resolution waiting for mission teardown before activating the outcome menu.</summary>
        private AmbushResolution _pendingAmbushedResolution = AmbushResolution.None;
        private MobileParty _pendingAmbushedAttacker;

        /// <summary>Cached press-vs-vanish branch so the Continue option doesn't re-roll.</summary>
        private bool _ambushedOutcomeMenuPressBranch;

        /// <summary>Handle a stealth hotkey press. Branches on prep/stealth/cooldown and triggers the entry roll.</summary>
        public void OnStealthHotkeyPressed()
        {
            // Real-time input cooldown against panic double-tap.
            float nowRealTime = System.Environment.TickCount / 1000f;
            if (nowRealTime - _lastHotkeyRealTime < HotkeyInputCooldownSeconds) return;

            if (!CanToggleStealth()) return;

            // Hotkey during prep cancels setup. No cooldown stamp.
            if (_playerPreppingStealth)
            {
                CancelPlayerStealthPrep();
                _lastHotkeyRealTime = nowRealTime;
                return;
            }

            if (_playerInStealth)
            {
                LeaveStealth(MobileParty.MainParty);
                _lastHotkeyRealTime = nowRealTime;
                return;
            }

            if (IsOnAmbushCooldown)
            {
                TORCommon.Say("Stealth is still on cooldown.");
                _lastHotkeyRealTime = nowRealTime;
                return;
            }

            // Entry chance roll. Failed entry stamps the cooldown as a solo stealth-exit record.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model != null)
            {
                var hostilesInRange = CollectHostileObserversInScanRadius(model);
                float chance = model.CalculateEntryChance(MobileParty.MainParty, hostilesInRange);
                if (MBRandom.RandomFloat >= chance)
                {
                    TORCommon.Say("You couldn't slip away in time.");
                    var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
                    ai?.RecordAmbush(new AmbushRecord(CampaignTime.Now,
                        MobileParty.MainParty, null, AmbushResolution.None));
                    _lastHotkeyRealTime = nowRealTime;
                    return;
                }
            }

            BeginPlayerStealthPrep(MobileParty.MainParty);

            if (model != null)
            {
                var main = MobileParty.MainParty;
                TORCommon.Log(LogLevel.Info, "[Ambush] entry: pos=" + main.Position.ToVec2()
                    + " strikeRange=" + model.CalculateStrikeRange(main).ToString("F2")
                    + " effectiveScouting=" + model.GetAmbusherScouting(main)
                    + " seeingRange=" + main.SeeingRange.ToString("F2"));
            }

            _lastHotkeyRealTime = nowRealTime;
        }

        private List<MobileParty> CollectHostileObserversInScanRadius(TORAmbushModel model)
        {
            var result = new List<MobileParty>();
            var mainParty = MobileParty.MainParty;
            if (mainParty == null) return result;
            float scanRadius = model.DetectionScanRadius;
            float scanRadiusSq = scanRadius * scanRadius;
            var mainPos = mainParty.Position.ToVec2();
            // LocatorGrid returns a cell-superset; tighten with a distance check.
            var query = MobileParty.StartFindingLocatablesAroundPosition(mainPos, scanRadius);
            for (var observer = MobileParty.FindNextLocatable(ref query); observer != null;
                 observer = MobileParty.FindNextLocatable(ref query))
            {
                if (observer == null || !observer.IsActive) continue;
                if (observer == mainParty) continue;
                if (!observer.IsVisible) continue;
                if (observer.Position.ToVec2().DistanceSquared(mainPos) > scanRadiusSq) continue;
                if (!model.CanDetect(observer, mainParty)) continue;
                result.Add(observer);
            }
            return result;
        }

        /// <summary>Start the player's stealth prep window.</summary>
        private void BeginPlayerStealthPrep(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (_playerInStealth || _playerPreppingStealth) return;

            var model = Campaign.Current?.Models?.GetAmbushModel();
            float totalMinutes = model?.CalculatePreparationMinutes(party) ?? 15f;
            _playerStealthPrepStartedAt = CampaignTime.Now;
            _playerPreppingStealth = true;

            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();
            // Long ignore so pursuers don't engage during prep.
            party.IgnoreByOtherPartiesTill(CampaignTime.YearsFromNow(100f));

            _cachedTimeControlMode = Campaign.Current.TimeControlMode;

            TORCommon.Say("Setting up the ambush (~"
                + totalMinutes.ToString("F0") + " minutes).");
            GameMenu.ActivateGameMenu(StealthMenuId);
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;

            TORCommon.Log(LogLevel.Info, "[Ambush] prep begin: minutes="
                + totalMinutes.ToString("F1")
                + " size=" + (party.MemberRoster?.TotalHealthyCount ?? 0));
        }

        /// <summary>Advance the prep window; finalize stealth once elapsed.</summary>
        private void TickPlayerStealthPrep()
        {
            if (!_playerPreppingStealth) return;
            float elapsed = (float)(CampaignTime.Now - _playerStealthPrepStartedAt).ToMinutes;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float totalMinutes = model?.CalculatePreparationMinutes(MobileParty.MainParty) ?? 15f;

            // Refresh prep bar in the stealth menu.
            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == StealthMenuId)
                RefreshStealthMenuTargets();

            if (elapsed < totalMinutes) return;

            _playerPreppingStealth = false;
            _playerInStealth = true;
            _stealthEnteredAt = CampaignTime.Now;
            TORCommon.Say("Ambush is set.");
            TORCommon.Log(LogLevel.Info, "[Ambush] prep complete: elapsed="
                + elapsed.ToString("F1") + " of " + totalMinutes.ToString("F1"));

            // Final refresh for the strike list.
            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == StealthMenuId)
                RefreshStealthMenuTargets();
        }

        /// <summary>Cancel setup during the prep window. No cooldown is stamped.</summary>
        private void CancelPlayerStealthPrep()
        {
            if (!_playerPreppingStealth) return;
            _playerPreppingStealth = false;

            var party = MobileParty.MainParty;
            if (party != null)
            {
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.IgnoreByOtherPartiesTill(CampaignTime.Zero);
            }
            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == StealthMenuId)
                GameMenu.ExitToLast();
            Campaign.Current.TimeControlMode = _cachedTimeControlMode;

            TORCommon.Say("Ambush setup aborted.");
            TORCommon.Log(LogLevel.Info, "[Ambush] prep cancelled");
        }

        /// <summary>Leave stealth stance and resume normal AI engagement.</summary>
        public void LeaveStealth(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (!_playerInStealth) return;

            _playerInStealth = false;
            party.Ai.SetDoNotMakeNewDecisions(false);
            // Clear the AI ignore flag.
            party.IgnoreByOtherPartiesTill(CampaignTime.Zero);
            _currentTargets.Clear();

            TryCloseStealthMenu();
            Campaign.Current.TimeControlMode = _cachedTimeControlMode;
            TORCommon.Say("Left stealth stance.");
        }

        private static void TryCloseStealthMenu()
        {
            var ctx = Campaign.Current?.CurrentMenuContext;
            if (ctx == null || ctx.GameMenu?.StringId != StealthMenuId) return;
            ctx.GameMenu.EndWait();
            GameMenu.ExitToLast();
        }

        /// <summary>Enumerate ambush-eligible targets near the attacker within effective strike range.</summary>
        public IReadOnlyList<MobileParty> GetAmbushTargets(MobileParty attacker)
        {
            if (attacker == null) return new List<MobileParty>();
            var attackerPos = attacker.Position.ToVec2();
            var attackerFaction = attacker.MapFaction;
            if (attackerFaction == null) return new List<MobileParty>();

            // Strike range scales with the attacker's effective Scouting.
            float strikeRange = Campaign.Current?.Models?.GetAmbushModel()?.CalculateStrikeRange(attacker) ?? StealthRadius;
            float radiusSq = strikeRange * strikeRange;
            var candidates = new List<(MobileParty party, float distSq)>();
            var query = MobileParty.StartFindingLocatablesAroundPosition(attackerPos, strikeRange);
            for (var p = MobileParty.FindNextLocatable(ref query); p != null;
                 p = MobileParty.FindNextLocatable(ref query))
            {
                if (p == null || !p.IsActive || p == attacker) continue;
                if (!p.IsVisible) continue;
                if (p.MapFaction == null) continue;
                // Neutral caravans are ambushable by the main party; vanilla crime-rating handles fallout.
                bool atWar = FactionManager.IsAtWarAgainstFaction(attackerFaction, p.MapFaction);
                bool neutralCaravanPlayerTarget = !atWar && p.IsCaravan && attacker.IsMainParty;
                if (!atWar && !neutralCaravanPlayerTarget) continue;
                float distSq = p.Position.ToVec2().DistanceSquared(attackerPos);
                if (distSq > radiusSq) continue;
                candidates.Add((p, distSq));
            }
            return candidates
                .OrderBy(c => c.distSq)
                .Take(MaxTargetSlots)
                .Select(c => c.party)
                .ToList();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
            // OnMissionEndedEvent catches victim-flee flows that don't fire OnPlayerBattleEnd.
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnAnyMissionEnded);
            CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, OnQuarterHourlyTick);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
            // GameMenuOpened fires even when TimeControlMode is Stop. TickEvent does not.
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);

            ScreenManager.OnPushScreen -= OnScreenPushed;
            ScreenManager.OnPushScreen += OnScreenPushed;

            // Idempotent re-subscription of the shared AmbushResolved handler.
            TORCampaignEvents.Instance.AmbushResolved -= OnAmbushResolved;
            TORCampaignEvents.Instance.AmbushResolved += OnAmbushResolved;
        }

        /// <summary>Append the record to history and dispatch shared post-strike effects on successful ambushes.</summary>
        private void OnAmbushResolved(object sender, AmbushResolvedEventArgs args)
        {
            var record = args?.Record;
            if (record == null) return;

            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            ai?.RecordAmbush(record);

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model != null && model.IsSuccessfulStrike(record))
            {
                ApplySuccessfulAmbushEffects(record.Ambusher, record.Victim);
            }
        }

        private void OnQuarterHourlyTick()
        {
            // Cache observers every tick; the eye widget reads this regardless of stealth state.
            RefreshCachedObservers();

            // When not stealthed, run the mirror player-detects-AI pass.
            if (!_playerInStealth)
            {
                RunPlayerDetectsAiPass();
                return;
            }

            var mainParty = MobileParty.MainParty;
            if (mainParty == null) return;

            // Keep the stealth menu's target list current.
            RefreshStealthMenuTargets();

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            // Reuse cached observers for detection roll and Roguery XP.
            int observerCount = _cachedObserversInRange.Count;
            int highestObserverScouting = 0;
            foreach (var observer in _cachedObserversInRange)
            {
                int s = observer.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
                if (s > highestObserverScouting) highestObserverScouting = s;

                if (model.TryDetect(observer, mainParty, _stealthEnteredAt, out string reason))
                {
                    TORCommon.Say(reason);
                    _spottedAt = CampaignTime.Now;
                    LeaveStealth(mainParty);
                    // Spotted without striking: write a solo stealth-exit stamp.
                    var aiStamp = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
                    aiStamp?.RecordAmbush(new AmbushRecord(CampaignTime.Now,
                        MobileParty.MainParty, null, AmbushResolution.None));
                    return;
                }
            }

            // Not revealed and at least one hostile in range: Roguery XP tick.
            if (observerCount > 0 && Hero.MainHero != null)
            {
                int hostileBonus = System.Math.Min(observerCount * model.RogueryXpPerHostileInRange,
                                                   model.RogueryXpPerHostileCap);
                int scoutingBonus = model.RogueryXpScoutingDivisor > 0
                    ? highestObserverScouting / model.RogueryXpScoutingDivisor
                    : 0;
                int xp = model.RogueryXpBasePerHiddenTick + hostileBonus + scoutingBonus;
                Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, xp);
            }
        }

        /// <summary>Rebuild the cached hostile observer list.</summary>
        private void RefreshCachedObservers()
        {
            _cachedObserversInRange.Clear();
            var mainParty = MobileParty.MainParty;
            if (mainParty == null || !mainParty.IsActive) return;

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            float scanRadius = model.DetectionScanRadius;
            float scanRadiusSq = scanRadius * scanRadius;
            var mainPos = mainParty.Position.ToVec2();

            foreach (var observer in MobileParty.All)
            {
                if (observer == null || !observer.IsActive) continue;
                if (!observer.IsVisible) continue;
                if (observer.Position.ToVec2().DistanceSquared(mainPos) > scanRadiusSq) continue;
                if (!model.CanDetect(observer, mainParty)) continue;
                _cachedObserversInRange.Add(observer);
            }
        }

        /// <summary>Player-side detection pass against nearby stealthed AI parties. One detection per tick.</summary>
        private void RunPlayerDetectsAiPass()
        {
            var mainParty = MobileParty.MainParty;
            if (mainParty == null || !mainParty.IsActive) return;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            if (ai == null) return;

            // Materialise before breaking stealth; the dict mutates on BreakStealthByDetection.
            var candidates = ai.StealthedPartiesInRange(mainParty, model.DetectionScanRadius).ToList();
            foreach (var stealthed in candidates)
            {
                if (!model.TryDetect(mainParty, stealthed, ai.GetStealthEnteredAt(stealthed), out _))
                    continue;

                // Record detection before breaking stealth so the reveal window stays valid.
                ai.RecordPlayerDetection(stealthed);
                ai.BreakStealthByDetection(stealthed);

                // Vanilla hideout-spotted audio cue.
                TaleWorlds.Engine.SoundEvent.PlaySound2D("event:/ui/notification/hideout_found");

                // Scouting XP, same shape as OnHideoutSpotted.
                var scout = mainParty.EffectiveScout ?? Hero.MainHero;
                if (scout != null) scout.AddSkillXp(DefaultSkills.Scouting, 100f);

                return;
            }
        }

        private static void OnScreenPushed(ScreenBase pushedScreen)
        {
            if (pushedScreen is not MapScreen mapScreen) return;
            if (mapScreen.GetMapView<AmbushMapView>() == null)
                mapScreen.AddMapView<AmbushMapView>();
            if (mapScreen.GetMapView<AmbushRangeMapView>() == null)
                mapScreen.AddMapView<AmbushRangeMapView>();
            if (mapScreen.GetMapView<AmbushPrepOverlayMapView>() == null)
                mapScreen.AddMapView<AmbushPrepOverlayMapView>();
        }

        /// <summary>Drain PendingPostAmbushCleanup when the stealth menu reopens after a mission.</summary>
        private void OnGameMenuOpened(MenuCallbackArgs args)
        {
            if (!PendingPostAmbushCleanup) return;
            // Only react when the stealth wait-menu is the one restoring.
            if (args?.MenuContext?.GameMenu?.StringId != StealthMenuId) return;

            PendingPostAmbushCleanup = false;
            args.MenuContext.GameMenu.EndWait();
            GameMenu.ExitToLast();

            _postAmbushForceTicksRemaining = PostAmbushForceTickCount;
            // Set time control immediately so OnCampaignTick can take over.
            if (Campaign.Current != null)
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
        }

        private void OnCampaignTick(float dt)
        {
            TickPlayerStealthPrep();

            // Open the follow-up field battle one tick after the press branch stashes the ambusher.
            if (_pendingFollowUpAmbusher != null
                && TaleWorlds.MountAndBlade.Mission.Current == null
                && MapEvent.PlayerMapEvent == null)
            {
                var ambusher = _pendingFollowUpAmbusher;
                _pendingFollowUpAmbusher = null;
                OpenFollowUpFieldBattle(ambusher);
            }

            if (PendingPostAmbushCleanup && TaleWorlds.MountAndBlade.Mission.Current == null)
            {
                PendingPostAmbushCleanup = false;

                var ctx = Campaign.Current?.CurrentMenuContext;
                if (ctx?.GameMenu?.StringId == StealthMenuId)
                {
                    ctx.GameMenu.EndWait();
                    GameMenu.ExitToLast();
                }

                _postAmbushForceTicksRemaining = PostAmbushForceTickCount;
            }

            // Short burst of forced time so the encounter state machine reaches the loot UI.
            if (_postAmbushForceTicksRemaining > 0 && Campaign.Current != null)
            {
                _postAmbushForceTicksRemaining--;
                if (Campaign.Current.TimeControlMode != CampaignTimeControlMode.UnstoppablePlay)
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
                // Don't restore time control at end of burst; vanilla will set its own.
            }
        }

        /// <summary>Stash the mission-start roster snapshot for later wound/remove passes.</summary>
        public void StashAmbushedRosterSnapshot(Dictionary<CharacterObject, int> snapshot)
        {
            _ambushedRosterSnapshot = (snapshot != null && snapshot.Count > 0)
                ? new Dictionary<CharacterObject, int>(snapshot) : null;
        }

        /// <summary>Flip IsInfoHidden on so vanilla UI shows "?" instead of troop counts. Idempotent.</summary>
        public void BeginBluffOn(MobileParty party)
        {
            if (party == null || !party.IsActive) return;
            party.IsInfoHidden = true;
        }

        /// <summary>Clear the bluff on this party.</summary>
        public void EndBluffOn(MobileParty party)
        {
            if (party == null || !party.IsActive) return;
            party.IsInfoHidden = false;
        }

        /// <summary>True when the player's current MapEvent has a bluffing ambusher.</summary>
        public bool IsCurrentMissionBluffed
        {
            get
            {
                var mapEvent = MapEvent.PlayerMapEvent;
                if (mapEvent == null) return false;
                foreach (var p in mapEvent.InvolvedParties)
                {
                    var mp = p?.MobileParty;
                    if (mp != null && mp != MobileParty.MainParty && mp.IsInfoHidden)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Clear bluff on every party on the ambusher's side of a mapEvent.</summary>
        public void EndBluffOnAmbusherSide(MapEvent mapEvent)
        {
            if (mapEvent == null) return;
            foreach (var p in mapEvent.InvolvedParties)
            {
                var mp = p?.MobileParty;
                if (mp != null && mp != MobileParty.MainParty) EndBluffOn(mp);
            }
        }

        /// <summary>Bluff every party on the ambusher's side, including vanilla-joined reinforcements.</summary>
        public void BeginBluffOnAmbusherSide(MobileParty ambusher)
        {
            if (ambusher == null) return;
            BeginBluffOn(ambusher);
            var mapEvent = ambusher.MapEvent;
            if (mapEvent == null) return;
            var ambusherSide = ambusher.Party?.Side ?? BattleSideEnum.None;
            if (ambusherSide == BattleSideEnum.None) return;
            var side = mapEvent.GetMapEventSide(ambusherSide);
            if (side == null) return;
            foreach (var mep in side.Parties)
            {
                var p = mep.Party?.MobileParty;
                if (p != null && p != ambusher) BeginBluffOn(p);
            }
        }

        /// <summary>First-flee cost. Wound a share of each tracked troop type on the current roster.</summary>
        private void WoundAmbushedCommittedTroops()
        {
            if (_ambushedRosterSnapshot == null || _ambushedRosterSnapshot.Count == 0)
            {
                TORCommon.Log(LogLevel.Info, "[Ambush] wound-committed: no snapshot (0 troops wounded)");
                return;
            }
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null) return;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float share = model?.AmbushedFledWoundShare ?? 0.3f;

            int typesProcessed = 0;
            int totalWounded = 0;
            foreach (var kv in _ambushedRosterSnapshot)
            {
                var c = kv.Key;
                int startCount = kv.Value;
                if (c == null || c.IsHero) continue;
                int idx = roster.FindIndexOfTroop(c);
                if (idx < 0) continue;
                int currentlyHealthy = roster.GetElementNumber(idx) - roster.GetElementWoundedNumber(idx);
                if (currentlyHealthy <= 0) continue;

                int wantToWound = (int)System.Math.Ceiling(startCount * share);
                int actually = System.Math.Min(currentlyHealthy, wantToWound);
                if (actually <= 0) continue;
                roster.AddToCountsAtIndex(idx, 0, actually);
                typesProcessed++;
                totalWounded += actually;
            }
            TORCommon.Log(LogLevel.Info, "[Ambush] wound-committed: wounded "
                + totalWounded + " troops across " + typesProcessed + " types"
                + " (snapshot had " + _ambushedRosterSnapshot.Count + " types, share=" + share + ")");
        }

        /// <summary>Double-flee cost. Remove a share of each tracked troop type, preferring wounded first.</summary>
        private void RemoveAmbushedCommittedTroops()
        {
            if (_ambushedRosterSnapshot == null || _ambushedRosterSnapshot.Count == 0)
            {
                TORCommon.Log(LogLevel.Info, "[Ambush] double-flee: no snapshot (0 troops removed)");
                return;
            }
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null) return;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float share = model?.AmbushedDoubleFleeRemoveShare ?? 0.3f;

            int typesProcessed = 0;
            int totalRemoved = 0;
            foreach (var kv in _ambushedRosterSnapshot)
            {
                var c = kv.Key;
                int startCount = kv.Value;
                if (c == null || c.IsHero) continue;
                int idx = roster.FindIndexOfTroop(c);
                if (idx < 0) continue;

                int wantToRemove = (int)System.Math.Ceiling(startCount * share);
                int presentNow = roster.GetElementNumber(idx);
                int actually = System.Math.Min(presentNow, wantToRemove);
                if (actually <= 0) continue;

                int wounded = roster.GetElementWoundedNumber(idx);
                int takeFromWounded = System.Math.Min(wounded, actually);
                roster.AddToCountsAtIndex(idx, -actually, woundedCountChange: -takeFromWounded);
                typesProcessed++;
                totalRemoved += actually;
            }
            TORCommon.Log(LogLevel.Info, "[Ambush] double-flee: removed "
                + totalRemoved + " troops across " + typesProcessed + " types"
                + " (share=" + share + ")");
        }

        /// <summary>Open a follow-up field battle with the ambusher as attacker.</summary>
        private void OpenFollowUpFieldBattle(MobileParty ambusher)
        {
            if (ambusher == null || !ambusher.IsActive) return;
            _followUpFieldBattlePending = true;
            // Keep the bluff active through the encounter screen.
            BeginBluffOn(ambusher);
            TORCommon.Say((ambusher.LeaderHero?.Name?.ToString()
                           ?? ambusher.Name?.ToString()
                           ?? "The ambushers")
                          + " press the attack.");
            // Teleport in case of drift, then open the vanilla encounter.
            ambusher.Position = MobileParty.MainParty.Position;
            TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.RestartPlayerEncounter(
                PartyBase.MainParty, ambusher.Party, forcePlayerOutFromSettlement: false);
            if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Battle == null)
            {
                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.StartBattle();
                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Update();
            }
            // Bluff the whole ambusher side, including vanilla-joined reinforcements.
            BeginBluffOnAmbusherSide(ambusher);
        }

        private static void ApplyMoraleDelta(float delta)
        {
            var main = MobileParty.MainParty;
            if (main == null) return;
            main.RecentEventsMorale += delta;
        }

        /// <summary>Catches victim-side resolutions whose soft-border retreat skips OnPlayerBattleEnd.</summary>
        private void OnAnyMissionEnded(IMission mission)
        {
            if (PlayerWasAmbusher) return;
            bool needsMenu = LastResolution == AmbushResolution.VictimFled
                             || (LastResolution == AmbushResolution.VictimDefeated
                                 && LastVictimLeaderDown);
            if (!needsMenu) return;

            var ambusher = Campaign.Current
                ?.GetCampaignBehavior<AiAmbushBehavior>()?.CurrentAmbusherOnPlayer;
            _pendingAmbushedResolution = LastResolution;
            _pendingAmbushedAttacker = ambusher;
            TORCommon.Log(LogLevel.Info, "[Ambush] OnMissionEnded: activating menu for "
                + LastResolution + " ambusher="
                + (ambusher?.LeaderHero?.Name?.ToString()
                   ?? ambusher?.Name?.ToString() ?? "?"));

            // Clear so a later mission-end doesn't re-fire.
            LastResolution = AmbushResolution.None;

            // Pop vanilla's just-pushed parent menu and open the outcome menu.
            if (Campaign.Current?.CurrentMenuContext != null)
                GameMenu.ExitToLast();
            GameMenu.ActivateGameMenu(AmbushedFledOutcomeMenuId);
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            TORCommon.Log(LogLevel.Info, "[Ambush] OnPlayerBattleEnd fired. LastResolution="
                + LastResolution + " PlayerWasAmbusher=" + PlayerWasAmbusher
                + " followUpPending=" + _followUpFieldBattlePending);

            // Drop the bluff on the enemy side; the battle has resolved.
            EndBluffOnAmbusherSide(mapEvent);

            // Double-flee check fires only when the player explicitly retreated during the follow-up battle.
            if (_followUpFieldBattlePending && mapEvent != null)
            {
                bool playerDoubleFled =
                    mapEvent.PlayerSide != BattleSideEnum.None
                    && mapEvent.RetreatingSide == mapEvent.PlayerSide;
                TORCommon.Log(LogLevel.Info, "[Ambush] follow-up end: PlayerSide="
                    + mapEvent.PlayerSide + " WinningSide=" + mapEvent.WinningSide
                    + " RetreatingSide=" + mapEvent.RetreatingSide
                    + " doubleFled=" + playerDoubleFled);
                if (playerDoubleFled)
                {
                    var model = Campaign.Current?.Models?.GetAmbushModel();
                    if (model != null)
                        ApplyMoraleDelta(-model.AmbushedFledDoubleFleeExtraMoralePenalty);
                    RemoveAmbushedCommittedTroops();
                    TORCommon.Say("The second rout breaks your band. Troops scatter.");
                    TORCommon.Log(LogLevel.Info, "[Ambush] double-flee penalty applied: -"
                        + (model?.AmbushedFledDoubleFleeExtraMoralePenalty ?? 0f)
                        + " to RecentEventsMorale");
                }
                // Teleport ambusher off the player tile; otherwise vanilla re-engages next tick.
                var aiBehavior = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
                var followUpAmbusher = aiBehavior?.CurrentAmbusherOnPlayer;
                if (followUpAmbusher != null && followUpAmbusher.IsActive)
                {
                    var main = MobileParty.MainParty;
                    if (main != null)
                    {
                        var away = CampaignVec2.Normalized(followUpAmbusher.Position - main.Position);
                        var awayV = away.ToVec2();
                        if (awayV.x == 0f && awayV.y == 0f) away = new CampaignVec2(new Vec2(1f, 0f), true);
                        followUpAmbusher.Position = main.Position + away * AmbushEscapeTeleportDistance;
                    }
                }
                if (aiBehavior != null) aiBehavior.CurrentAmbusherOnPlayer = null;
                _followUpFieldBattlePending = false;
                _ambushedRosterSnapshot = null;
            }

            if (_playerInStealth) LeaveStealth(MobileParty.MainParty);

            // Route resolution to the right aftermath branch.
            if (PlayerWasAmbusher)
            {
                HandlePlayerAmbusherResolution();
            }
            else if (LastResolution != AmbushResolution.None)
            {
                HandlePlayerVictimResolution();
            }

            LastResolution = AmbushResolution.None;
            PlayerWasAmbusher = false;
            LastAmbusherLeaderDown = false;
            LastVictimLeaderDown = false;
            ExtractionTroopsLeftBehind = 0;
            LastAmbushLoudness = 0;
            // _lastAmbushTarget clears when the escape/engage option fires.
        }

        /// <summary>Player-ambusher post-mission branch: fire the resolution event and open any follow-up menu.</summary>
        private void HandlePlayerAmbusherResolution()
        {
            if (LastResolution == AmbushResolution.None) return;
            if (LastResolution == AmbushResolution.AmbusherPartialVictory)
            {
                // Extraction path already handled itself in BeginExtractionTransition.
                return;
            }

            var missionModel = Campaign.Current?.Models?.GetMissionAmbushModel();
            bool wasLoud = missionModel != null
                           && LastAmbushLoudness >= missionModel.LoudnessEventThreshold;

            // Single event fire; subscriber appends the record and runs shared effects.
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, MobileParty.MainParty, _lastAmbushTarget,
                                 LastResolution, wasLoud,
                                 LastAmbusherLeaderDown, LastVictimLeaderDown));

            switch (LastResolution)
            {
                case AmbushResolution.AmbusherTotalVictory:
                    // Don't force-destroy the target; vanilla's state machine still has PlayerVictory ahead.
                    if (wasLoud)
                    {
                        TORCommon.Say("The noise of your attack carries far. Scouts are looking for you. Stealth will be slow to re-establish.");
                    }
                    {
                        int eliminated = _lastAmbushTarget?.MemberRoster?.TotalManCount ?? 0;
                        var ambushModel = Campaign.Current?.Models?.GetAmbushModel();
                        if (ambushModel != null && Hero.MainHero != null)
                        {
                            int xp = ambushModel.RogueryXpTotalVictoryBase
                                   + (int)(eliminated * ambushModel.RogueryXpTotalVictoryPerMember);
                            Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, xp);
                        }
                        if (_lastAmbushTarget != null)
                        {
                            TORCommon.Log(LogLevel.Info,
                                "[AmbushCampaignBehavior] TotalVictory post-mission:"
                                + " target=" + GetPartyLabel(_lastAmbushTarget)
                                + " active=" + _lastAmbushTarget.IsActive
                                + " totalManCount=" + (_lastAmbushTarget.MemberRoster?.TotalManCount ?? -1)
                                + " loudness=" + LastAmbushLoudness
                                + (wasLoud ? " (loud: cooldown extended)" : " (quiet: cooldown zeroed)"));
                        }
                    }
                    _lastAmbushTarget = null;
                    break;
                case AmbushResolution.AmbusherDefeated:
                    // Hero-down variant routes to the failed-ambush menu.
                    if (LastAmbusherLeaderDown)
                    {
                        GameMenu.ActivateGameMenu(AmbushFailedMenuId);
                    }
                    break;
                case AmbushResolution.Stalemate:
                    // Vanilla defeat flow handles casualties.
                    break;
            }
        }

        /// <summary>Player-victim post-mission branch: fire the resolution event and open any follow-up menu.</summary>
        private void HandlePlayerVictimResolution()
        {
            var aiBehavior = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            var ambusher = aiBehavior?.CurrentAmbusherOnPlayer;

            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, ambusher, MobileParty.MainParty,
                                 LastResolution, wasLoud: false,
                                 LastAmbusherLeaderDown, LastVictimLeaderDown));

            switch (LastResolution)
            {
                case AmbushResolution.AmbusherDefeated:
                    TORCommon.Say("You drove them off. You got a good look at them. They won't stay hidden for long.");
                    break;
                case AmbushResolution.VictimFled:
                    // Deferred menu; retreat path isn't fully torn down when this fires.
                    _pendingAmbushedResolution = LastResolution;
                    _pendingAmbushedAttacker = ambusher;
                    TORCommon.Log(LogLevel.Info, "[Ambush] Phase F outcome menu opening for "
                        + LastResolution + " ambusher="
                        + (ambusher?.LeaderHero?.Name?.ToString()
                           ?? ambusher?.Name?.ToString() ?? "?"));
                    GameMenu.ActivateGameMenu(AmbushedFledOutcomeMenuId);
                    break;
                case AmbushResolution.VictimDefeated:
                    if (LastVictimLeaderDown)
                    {
                        // Hero-down variant; same deferred menu path, different init branch.
                        _pendingAmbushedResolution = LastResolution;
                        _pendingAmbushedAttacker = ambusher;
                        TORCommon.Log(LogLevel.Info, "[Ambush] Phase F outcome menu opening for "
                            + LastResolution + " (hero down) ambusher="
                            + (ambusher?.LeaderHero?.Name?.ToString()
                               ?? ambusher?.Name?.ToString() ?? "?"));
                        GameMenu.ActivateGameMenu(AmbushedFledOutcomeMenuId);
                    }
                    // Non-hero-down VictimDefeated runs vanilla casualty flow only.
                    break;
            }

            if (aiBehavior != null) aiBehavior.CurrentAmbusherOnPlayer = null;

            // Safety net: close the intro menu if vanilla re-pushed it during cleanup.
            var ambushedCtx = Campaign.Current?.CurrentMenuContext;
            if (ambushedCtx?.GameMenu?.StringId == AmbushedIntroMenuId)
            {
                GameMenu.ExitToLast();
            }
        }

        /// <summary>Queue the post-extraction menu synchronously from the mission, before the scoreboard races in.</summary>
        public void BeginExtractionTransition(int leftBehind)
        {
            WoundLeftBehindTroops(leftBehind);
            // Fire the resolution event; subscriber writes the record and runs shared effects.
            var missionModel = Campaign.Current?.Models?.GetMissionAmbushModel();
            bool wasLoud = missionModel != null
                           && LastAmbushLoudness >= missionModel.LoudnessEventThreshold;
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, MobileParty.MainParty, _lastAmbushTarget,
                                 AmbushResolution.AmbusherPartialVictory, wasLoud));
            GameMenu.ActivateGameMenu(AmbushEscapeMenuId);
        }

        /// <summary>Shared post-strike effects: disorganize the ambushed side, clear it off the ambusher.</summary>
        private static void ApplySuccessfulAmbushEffects(MobileParty ambusher, MobileParty ambushed)
        {
            if (ambushed != null && ambushed.IsActive
                && ambushed.Party != null
                && Campaign.Current?.Models?.PartyImpairmentModel != null
                && Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(ambushed.Party))
            {
                ambushed.SetDisorganized(true);
            }
            if (ambusher != null && ambusher.IsActive && ambusher.IsDisorganized)
                ambusher.SetDisorganized(false);
        }

        private static void WoundLeftBehindTroops(int count)
        {
            if (count <= 0) return;
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster == null) return;
            int remaining = count;
            for (int i = 0; i < roster.Count && remaining > 0; i++)
            {
                // Skip hero rows; vanilla wounded flow handles them.
                if (roster.GetCharacterAtIndex(i).IsHero) continue;
                int healthy = roster.GetElementNumber(i) - roster.GetElementWoundedNumber(i);
                if (healthy <= 0) continue;
                int toWound = healthy < remaining ? healthy : remaining;
                roster.AddToCountsAtIndex(i, 0, toWound);
                remaining -= toWound;
            }
            if (count - remaining > 0)
                TORCommon.Say((count - remaining) + " of your troops were left behind and will take time to recover.");
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RegisterMenus(starter);

            // Resume stealth/prep stance on save load.
            if (_playerInStealth || _playerPreppingStealth)
            {
                var main = MobileParty.MainParty;
                if (main != null)
                {
                    main.Ai.SetDoNotMakeNewDecisions(true);
                    main.SetMoveModeHold();
                    main.IgnoreByOtherPartiesTill(CampaignTime.YearsFromNow(100f));
                }
                _cachedTimeControlMode = Campaign.Current.TimeControlMode;
                GameMenu.ActivateGameMenu(StealthMenuId);
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }

            // IsInfoHidden is vanilla-saveable, nothing to rebuild.
        }

        private void RegisterMenus(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                StealthMenuId,
                "{STEALTH_MENU_TEXT}",
                OnStealthMenuInit,
                _ => true,
                null,
                // Tick delegate empty; refresh runs from OnQuarterHourlyTick instead.
                (args, dt) => { },
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
                GameMenu.MenuOverlayType.Encounter,
                targetWaitHours: 0f,
                GameMenu.MenuFlags.None,
                null);

            for (int i = 0; i < MaxTargetSlots; i++)
            {
                int slot = i;
                // Per-slot placeholder so OnStealthMenuInit can set each label independently.
                starter.AddGameMenuOption(
                    StealthMenuId,
                    "tor_ambush_strike_" + slot,
                    "{STRIKE_OPT_TEXT_" + slot + "}",
                    args => StrikeSlotCondition(args, slot),
                    args => OnStrike(slot),
                    isLeave: false,
                    index: slot,
                    isRepeatable: false);
            }

            starter.AddGameMenuOption(
                StealthMenuId,
                "tor_ambush_leave",
                "{=tor_ambush_leave}Break cover and move on.",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => LeaveStealth(MobileParty.MainParty),
                isLeave: true,
                index: -1,
                isRepeatable: false);

            RegisterAmbushEscapeMenu(starter);
            RegisterAmbushFailedMenu(starter);
            RegisterAmbushedIntroMenu(starter);
            RegisterAmbushedFledOutcomeMenu(starter);
        }

        /// <summary>Victim-side intro menu: narrative beat then Prepare to fight. Gives the MapEvent an extra tick to finalize.</summary>
        private void RegisterAmbushedIntroMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                AmbushedIntroMenuId,
                "{=tor_ambushed_intro}Your column has been moving through the pass for some time, no sign of anyone else on the road. A sudden movement in the brush. Steel on steel. Then they are on you, a lord's war band, waiting for exactly this.",
                _ => { },
                GameMenu.MenuOverlayType.Encounter);

            starter.AddGameMenuOption(
                AmbushedIntroMenuId,
                "tor_ambushed_intro_prepare",
                "{=tor_ambushed_intro_prepare}Prepare to fight!",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.HostileAction; return true; },
                _ => OnAmbushedIntroPrepare(),
                isLeave: false, index: 0, isRepeatable: false);
        }

        /// <summary>Close the intro menu and open the victim-side ambush mission.</summary>
        private void OnAmbushedIntroPrepare()
        {
            GameMenu.ExitToLast();
            AmbushRetreatZone.ResetRegistry();
            TorMissionManager.OpenAmbushedMission(PickAmbushScene());
        }

        private void RegisterAmbushEscapeMenu(CampaignGameStarter starter)
        {
            // Shown after partial-victory extraction; player presses or slips away.
            starter.AddGameMenu(
                AmbushEscapeMenuId,
                "{=tor_ambush_escape_intro}Your party pulls back from the ambush site. The remainder of the enemy column is forming up where you struck. You can press the attack while they are disorganised, or slip away before they regroup.",
                _ => { },
                GameMenu.MenuOverlayType.None);

            starter.AddGameMenuOption(
                AmbushEscapeMenuId,
                "tor_ambush_escape_engage",
                "{=tor_ambush_escape_engage}Press the attack now.",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.HostileAction; return true; },
                _ => OnAmbushEscapeEngage(),
                isLeave: false, index: 0, isRepeatable: false);

            starter.AddGameMenuOption(
                AmbushEscapeMenuId,
                "tor_ambush_escape_slip",
                "{=tor_ambush_escape_slip}Slip away before they regroup.",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Escape; return true; },
                _ => OnAmbushEscapeSlip(),
                isLeave: true, index: 1, isRepeatable: false);
        }

        private void RegisterAmbushFailedMenu(CampaignGameStarter starter)
        {
            // Shown after hero-down defeat; player flees with wounds or fights on without the hero.
            starter.AddGameMenu(
                AmbushFailedMenuId,
                "{=tor_ambush_failed_intro}You are down. Your surviving troops have broken contact and are regrouping around your fallen banner. They can carry you clear at the cost of more wounded, or press the fight without you. If they lose, your army is gone.",
                _ => { },
                GameMenu.MenuOverlayType.None);

            starter.AddGameMenuOption(
                AmbushFailedMenuId,
                "tor_ambush_failed_run",
                "{=tor_ambush_failed_run}Run. Carry me clear, cost be damned.",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Escape; return true; },
                _ => OnAmbushFailedRun(),
                isLeave: true, index: 0, isRepeatable: false);

            starter.AddGameMenuOption(
                AmbushFailedMenuId,
                "tor_ambush_failed_fight",
                "{=tor_ambush_failed_fight}Fight on. Finish what we started.",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.HostileAction; return true; },
                _ => OnAmbushFailedFight(),
                isLeave: false, index: 1, isRepeatable: false);
        }

        /// <summary>Victim-side outcome menu for flee/hero-down resolutions. Branches on press-vs-vanish.</summary>
        private void RegisterAmbushedFledOutcomeMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                AmbushedFledOutcomeMenuId,
                "{=!}{AMBUSH_OUTCOME_TEXT}",
                OnAmbushedFledOutcomeMenuInit,
                GameMenu.MenuOverlayType.None);

            starter.AddGameMenuOption(
                AmbushedFledOutcomeMenuId,
                "tor_ambushed_fled_outcome_continue",
                "{=tor_ambushed_fled_outcome_continue}Continue.",
                args =>
                {
                    args.optionLeaveType = _ambushedOutcomeMenuPressBranch
                        ? GameMenuOption.LeaveType.HostileAction
                        : GameMenuOption.LeaveType.Continue;
                    return true;
                },
                _ => OnAmbushedFledOutcomeContinue(),
                isLeave: true, index: 0, isRepeatable: false);
        }

        /// <summary>Init delegate for the outcome menu: pick flavor, wound committed troops, cache the branch.</summary>
        private void OnAmbushedFledOutcomeMenuInit(MenuCallbackArgs args)
        {
            var resolution = _pendingAmbushedResolution;
            var ambusher = _pendingAmbushedAttacker;
            _pendingAmbushedResolution = AmbushResolution.None;
            _pendingAmbushedAttacker = null;

            // Vanilla re-fires menu init on screen resume; early-return on re-entry.
            if (resolution == AmbushResolution.None) return;

            TextObject flavor;
            if (resolution == AmbushResolution.VictimFled)
            {
                WoundAmbushedCommittedTroops();
                var model = Campaign.Current?.Models?.GetAmbushModel();
                bool presses = model != null
                               && ambusher != null && ambusher.IsActive
                               && model.AmbusherPressesAttack(ambusher, MobileParty.MainParty);
                _ambushedOutcomeMenuPressBranch = presses;

                if (presses)
                {
                    var culture = ambusher.MapFaction?.Culture?.Name
                                 ?? new TextObject("{=!}enemy");
                    flavor = new TextObject("{=tor_ambushed_outcome_press}You broke contact, but the {CULTURE} are not done. They regroup and move to pin you down. They mean to finish what they started.");
                    flavor.SetTextVariable("CULTURE", culture);
                }
                else
                {
                    flavor = new TextObject("{=tor_ambushed_outcome_vanish}You broke contact. The enemy melted away. Your troops are shattered, but the survivors are getting picked up and tending to their wounds.");
                }
                TORCommon.Log(LogLevel.Info,
                    "[Ambush] outcome menu opened: VictimFled presses=" + presses
                    + " ambusher=" + (ambusher?.LeaderHero?.Name?.ToString()
                                      ?? ambusher?.Name?.ToString() ?? "?"));
            }
            else if (resolution == AmbushResolution.VictimDefeated)
            {
                // Hero-down sub-case; non-hero-down is handled by vanilla defeat flow.
                _ambushedOutcomeMenuPressBranch = false;
                flavor = new TextObject("{=tor_ambushed_outcome_hero_wounded}You were cut down in the ambush. Your troops carried you clear of the fighting; the enemy did not pursue.");
                TORCommon.Log(LogLevel.Info, "[Ambush] outcome menu opened: VictimDefeated (hero down)");
            }
            else
            {
                _ambushedOutcomeMenuPressBranch = false;
                flavor = new TextObject("{=tor_ambushed_outcome_generic}The ambush is over.");
                TORCommon.Log(LogLevel.Info, "[Ambush] outcome menu opened: fallthrough resolution=" + resolution);
            }
            MBTextManager.SetTextVariable("AMBUSH_OUTCOME_TEXT", flavor);
        }

        /// <summary>Continue option: queue follow-up battle on press, apply morale penalty on vanish.</summary>
        private void OnAmbushedFledOutcomeContinue()
        {
            var aiBehavior = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            var ambusher = aiBehavior?.CurrentAmbusherOnPlayer;

            if (_ambushedOutcomeMenuPressBranch
                && ambusher != null && ambusher.IsActive)
            {
                _pendingFollowUpAmbusher = ambusher;
                TORCommon.Say("The enemy regroups. They are coming for you.");
                TORCommon.Log(LogLevel.Info, "[Ambush] outcome menu continue: press, queued follow-up battle.");
                GameMenu.ExitToLast();
                return;
            }

            // Vanish branch: morale penalty, back to map. Separation + pair-record already handled by AmbushedEndLogic.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float moraleHit = model?.AmbushedFledVanishMoralePenalty ?? 5f;
            ApplyMoraleDelta(-moraleHit);
            TORCommon.Say("The ambushers melt back into cover. Your troops lick their wounds. (Morale -" + moraleHit + ")");
            // End the bluff; ambusher melted away.
            if (ambusher != null) EndBluffOn(ambusher);
            if (aiBehavior != null && !_ambushedOutcomeMenuPressBranch)
                aiBehavior.CurrentAmbusherOnPlayer = null;
            TORCommon.Log(LogLevel.Info, "[Ambush] outcome menu continue: vanish, morale -" + moraleHit
                + " PlayerEncounter.Current="
                + (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null ? "ALIVE" : "null"));

            GameMenu.ExitToLast();
        }

        private void OnAmbushEscapeEngage()
        {
            // Open a vanilla field battle; close the pre-extraction encounter first.
            if (_lastAmbushTarget == null || !_lastAmbushTarget.IsActive) { GameMenu.ExitToLast(); _lastAmbushTarget = null; return; }
            var target = _lastAmbushTarget;
            _lastAmbushTarget = null;
            if (PlayerEncounter.Current != null) PlayerEncounter.Finish(false);
            EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
        }

        private void OnAmbushEscapeSlip()
        {
            // Short teleport away from the target to break engagement, then close the encounter.
            var main = MobileParty.MainParty;
            if (main != null && _lastAmbushTarget != null && _lastAmbushTarget.IsActive)
            {
                var away = CampaignVec2.Normalized(main.Position - _lastAmbushTarget.Position);
                main.Position = main.Position + away * AmbushEscapeTeleportDistance;
            }
            _lastAmbushTarget = null;
            if (PlayerEncounter.Current != null) PlayerEncounter.Finish(false);
            GameMenu.ExitToLast();
        }

        private void OnAmbushFailedRun()
        {
            // Flat casualty hit on survivor roster; hero already wounded by vanilla.
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    if (roster.GetCharacterAtIndex(i).IsHero) continue;
                    int healthy = roster.GetElementNumber(i) - roster.GetElementWoundedNumber(i);
                    if (healthy > 0) roster.AddToCountsAtIndex(i, 0, healthy);
                }
            }
            _lastAmbushTarget = null;
            TORCommon.Say("Your surviving troops carry you clear. The ambush is over.");
            GameMenu.ExitToLast();
        }

        private void OnAmbushFailedFight()
        {
            // Resume as a vanilla battle with the hero down; player watches via spectate mode.
            if (_lastAmbushTarget == null || !_lastAmbushTarget.IsActive) { GameMenu.ExitToLast(); _lastAmbushTarget = null; return; }
            var target = _lastAmbushTarget;
            _lastAmbushTarget = null;
            EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
        }

        private void OnStealthMenuInit(MenuCallbackArgs args)
        {
            // Don't call StartWait(); ActivateGameMenu already invoked it for wait menus.

            var culture = Hero.MainHero?.MapFaction?.Culture;
            if (culture != null && args.MenuContext != null)
                args.MenuContext.SetBackgroundMeshName(culture.EncounterBackgroundMesh);

            RefreshStealthMenuTargets();
        }

        /// <summary>Rebuild the strike-slot list from live world state.</summary>
        private void RefreshStealthMenuTargets()
        {
            _currentTargets.Clear();

            // During prep, Gauntlet overlay shows the bar; the stealth menu stays empty.
            if (_playerPreppingStealth)
            {
                MBTextManager.SetTextVariable("STEALTH_MENU_TEXT",
                    new TextObject("{=tor_ambush_menu_preparing}Preparing ambush..."));
                return;
            }

            _currentTargets.AddRange(GetAmbushTargets(MobileParty.MainParty));

            var intro = new TextObject("{=tor_ambush_menu_intro}You crouch in cover. Nothing moves nearby.");
            if (_currentTargets.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("You crouch in cover. Enemies within strike range:");
                for (int i = 0; i < _currentTargets.Count; i++)
                {
                    var p = _currentTargets[i];
                    sb.AppendLine("- " + GetPartyLabel(p) + " (" + p.MemberRoster.TotalManCount + ")");
                }
                intro = new TextObject(sb.ToString());
            }
            MBTextManager.SetTextVariable("STEALTH_MENU_TEXT", intro);

            // Populate the per-slot label variables. StrikeSlotCondition shows
            // the option when _currentTargets[slot] is valid; the label text
            // comes from these variables so chance % updates per refresh too.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            for (int i = 0; i < MaxTargetSlots; i++)
            {
                string varName = "STRIKE_OPT_TEXT_" + i;
                if (i < _currentTargets.Count)
                {
                    var p = _currentTargets[i];
                    float chance = model != null
                        ? model.CalculateAmbushSuccessChance(MobileParty.MainParty, p, _stealthEnteredAt)
                        : 0f;
                    int chancePct = (int)(chance * 100f);
                    MBTextManager.SetTextVariable(varName,
                        new TextObject("Strike at " + GetPartyLabel(p) + " (" + chancePct + "% chance)."));
                }
                else
                {
                    MBTextManager.SetTextVariable(varName, new TextObject(""));
                }
            }

            // Explicit menu refresh to reveal newly-visible strike options; null-check Handler for first init.
            var ctx = Campaign.Current?.CurrentMenuContext;
            if (ctx?.GameMenu?.StringId == StealthMenuId && ctx.Handler != null)
                Campaign.Current?.GameMenuManager?.RefreshMenuOptions(ctx);
        }

        private bool StrikeSlotCondition(MenuCallbackArgs args, int slot)
        {
            if (slot >= _currentTargets.Count) return false;
            var target = _currentTargets[slot];
            if (target == null || !target.IsActive) return false;

            // Label text comes from the per-slot MBTextManager variable set in OnStealthMenuInit.
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            return true;
        }

        private void OnStrike(int slot)
        {
            if (slot >= _currentTargets.Count) return;
            var target = _currentTargets[slot];
            if (target == null || !target.IsActive) return;

            float chance = (Campaign.Current.Models.GetAmbushModel()?.CalculateAmbushSuccessChance(
                MobileParty.MainParty, target, _stealthEnteredAt) ?? 0f);
            bool success = MBRandom.RandomFloat < chance;
            int chancePct = (int)(chance * 100f);

            if (success)
            {
                TORCommon.Say("Ambush succeeded (" + chancePct + "%) on " + GetPartyLabel(target) + ".");
                _lastAmbushTarget = target;
                OpenAmbushTroopSelection();
            }
            else
            {
                // Strike fail burns stealth; target decides to engage via its own AI tick. Direct SetDisorganized bypasses the TotalManCount >= 10 gate.
                LeaveStealth(MobileParty.MainParty);
                TORCommon.Say("Ambush failed (" + chancePct + "%) on " + GetPartyLabel(target)
                              + ". You've been spotted. Your formation is broken.");
                MobileParty.MainParty.SetDisorganized(true);
            }
        }

        /// <summary>Open the troop-selection overlay with a Tactics-scaled cap. Heroes excluded.</summary>
        private void OpenAmbushTroopSelection()
        {
            var ctx = Campaign.Current?.CurrentMenuContext;
            var model = Campaign.Current?.Models?.GetMissionAmbushModel();
            int cap = model?.ComputeAmbusherInMissionCap(Hero.MainHero) ?? 25;
            if (ctx == null)
            {
                // Fallback for missing menu context; supplier uses vanilla allocation.
                BeginAmbushMission(null);
                return;
            }
            var preSelected = TroopRoster.CreateDummyTroopRoster();
            preSelected.Add(MobilePartyHelper.GetStrongestAndPriorTroops(
                MobileParty.MainParty, cap, false));
            ctx.OpenTroopSelection(
                MobileParty.MainParty.MemberRoster,
                preSelected,
                CanSelectTroopForAmbush,
                OnAmbushTroopSelectionDone,
                cap,
                0);
        }

        private static bool CanSelectTroopForAmbush(CharacterObject c)
        {
            if (c == null) return false;
            if (c.IsPlayerCharacter) return false;
            if (c.IsHero) return false;
            return true;
        }

        private void OnAmbushTroopSelectionDone(TroopRoster selected)
        {
            if (selected == null)
            {
                // Player cancelled; stealth stays active, clear target.
                _lastAmbushTarget = null;
                return;
            }
            BeginAmbushMission(selected);
        }

        private void BeginAmbushMission(TroopRoster selected)
        {
            var target = _lastAmbushTarget;
            if (target == null || !target.IsActive) return;

            // Drop stealth; record is written at resolution time.
            LeaveStealth(MobileParty.MainParty);

            // Teleport onto the target to match the map fiction.
            MobileParty.MainParty.Position = target.Position;

            // Skip EncounterManager.StartPartyEncounter; it would open the meeting menu and deadlock.
            PlayerEncounter.RestartPlayerEncounter(target.Party, PartyBase.MainParty, false);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            // Clear stale retreat-zone registrations before scene load.
            AmbushRetreatZone.ResetRegistry();
            TorMissionManager.OpenAmbushMission(PickAmbushScene(), selected);
        }

        private static string GetPartyLabel(MobileParty p)
        {
            if (p.LeaderHero != null) return p.LeaderHero.Name.ToString();
            return p.Name.ToString();
        }

        private static bool CanToggleStealth()
        {
            if (Mission.Current != null) return false;
            if (Hero.MainHero == null || Hero.MainHero.IsPrisoner) return false;
            if (MobileParty.MainParty == null || !MobileParty.MainParty.IsActive) return false;
            if (MobileParty.MainParty.CurrentSettlement != null) return false;
            if (MapEvent.PlayerMapEvent != null) return false;
            return true;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_playerInStealth", ref _playerInStealth);
            dataStore.SyncData("_stealthEnteredAt", ref _stealthEnteredAt);
            dataStore.SyncData("_playerPreppingStealth", ref _playerPreppingStealth);
            dataStore.SyncData("_playerStealthPrepStartedAt", ref _playerStealthPrepStartedAt);
        }
    }
}
