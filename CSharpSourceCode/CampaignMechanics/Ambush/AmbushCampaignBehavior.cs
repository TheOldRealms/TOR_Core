using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
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
    public class AmbushCampaignBehavior : CampaignBehaviorBase
    {
        public const float StealthRadius = 15f;
        public const float StrikeRadius = 15f;
        public const int CooldownHours = 6;
        public const int MaxTargetSlots = 6;

        // Phase B: observer must be within this map distance to spot a stealthed party.
        // Reserved for the Scouting/terrain-aware detection check in open-questions. Not currently
        // used — AI engagement is gated via MobileParty.IgnoreByOtherPartiesTill (ShouldBeIgnored)
        // which is a hard flag, not a radius. Keeping the constant for later detection work.
        public const float StealthDetectionRadius = 3f;

        public static readonly InputKey StealthToggleKey = InputKey.X;

        public const string StealthMenuId = "tor_ambush_stealth_menu";
        public const string AmbushEscapeMenuId = "tor_ambush_escape_menu";
        public const string AmbushFailedMenuId = "tor_ambush_failed_menu";
        public const string AmbushedIntroMenuId = "tor_ambushed_intro_menu";
        public const string AmbushedFledOutcomeMenuId = "tor_ambushed_fled_outcome_menu";
        // Distance (metres) the player's party is teleported along the escape
        // vector when picking "escape" from the Ambush Escape menu. Enough to
        // prevent immediate re-engagement by the target party's AI.
        // Campaign-map units (approx km). Was 50f which teleported the
        // slipper half the map away on an extraction; the intent is a
        // short hop that just breaks engagement so the ambusher AI
        // doesn't re-stack on player next tick.
        private const float AmbushEscapeTeleportDistance = 3f;

        // First delivered scene from the scener (TOR_Environment/SceneObj/test_ambush/).
        // Has an authored AmbushRetreatZone (Radius=10) + TacticalRegion=Forest.
        // Will become a lookup table (terrain → scene[]) once more scenes land.
        public const string DefaultAmbushScene = "test_ambush";

        private bool _playerInStealth;
        private CampaignTime _stealthEnteredAt = CampaignTime.Zero;

        // Prep state (§19 campaign-stealth-design.md). Player is "setting
        // up the ambush" between hotkey press and the model's
        // CalculatePreparationMinutes elapsing. Not yet IsPlayerInStealth
        // — Scouting bonus (§17) stays off during prep by design.
        // Only start-time is stored; the total prep window is recomputed
        // from the model on each tick (party is held in place during
        // prep, so size/composition don't drift).
        private bool _playerPreppingStealth;
        private CampaignTime _playerStealthPrepStartedAt = CampaignTime.Zero;
        // Player ambush cooldown is derived from the record list on
        // AiAmbushBehavior (one source of truth across the two
        // behaviours). See IsOnAmbushCooldown / PlayerAmbushCooldownUntil
        // below.

        private readonly List<MobileParty> _currentTargets = new List<MobileParty>();
        private CampaignTimeControlMode _cachedTimeControlMode = CampaignTimeControlMode.StoppablePlay;

        // Real-time timestamp (System seconds) of the last stealth-hotkey press
        // that resolved. Used by the 2-second input cooldown that prevents
        // panic double-tap from toggling off stealth you just paid to enter.
        private float _lastHotkeyRealTime = -10f;
        private const float HotkeyInputCooldownSeconds = 2f;

        /// <summary>Loudness tally from the most recent ambush mission —
        /// player-side gunpowder shots + non-Shadow magic casts. Set by
        /// AmbushLoudnessTracker before mission close; consumed by the
        /// resolution event's record (WasLoud flag) which drives the
        /// cooldown length model.</summary>
        public int LastAmbushLoudness { get; set; }

        // ---- Eye-widget state (§21) ----

        /// <summary>Timestamp of the last successful detection-roll against
        /// the main party. Used by the eye-widget VM to light the
        /// "recently spotted" event icon for a model-tunable window after
        /// detection fires. Zero when no detection has fired yet (or the
        /// window has fully decayed before being reset).</summary>
        private CampaignTime _spottedAt = CampaignTime.Zero;

        /// <summary>Hostile observers inside <c>DetectionScanRadius</c> as
        /// of the last <see cref="OnQuarterHourlyTick"/>. Cached here so
        /// the per-frame VM refresh doesn't have to re-iterate
        /// <c>MobileParty.All</c>. Written in the tick's observer loop,
        /// read by <c>AmbushButtonVM.Refresh</c>.</summary>
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

        /// <summary>
        /// Resolution of the most recent ambush mission. Set by
        /// <see cref="Missions.AmbushMission.AmbushEndLogic.SetOutcome"/>
        /// (player as ambusher) or
        /// <see cref="Missions.AmbushMission.AmbushedEndLogic.SetOutcome"/>
        /// (player as victim); consumed in <see cref="OnPlayerBattleEnd"/>
        /// to pick post-mission flow. Reset to <c>None</c> once consumed.
        /// </summary>
        public AmbushResolution LastResolution { get; set; } = AmbushResolution.None;

        /// <summary>
        /// Role flag paired with <see cref="LastResolution"/>: true when
        /// the player was the ambusher (Phase D mission), false when
        /// the player was the victim (Phase F). Set by the mission end
        /// logics alongside <see cref="LastResolution"/>; consumed by
        /// the post-mission switch to pick the right menu branch
        /// (ambusher-win menus vs victim menus).
        /// </summary>
        public bool PlayerWasAmbusher { get; set; }

        /// <summary>Ambusher's hero went down during the engagement.
        /// Set alongside <see cref="LastResolution"/>. In Phase D (player
        /// = ambusher) this means the player's hero dropped mid-strike;
        /// in Phase F / AI-on-AI the AI ambusher's hero is the referent.
        /// Post-mission menu picker reads it to pick the right flavor
        /// without expanding the resolution enum.</summary>
        public bool LastAmbusherLeaderDown { get; set; }

        /// <summary>Victim's hero went down during the engagement.
        /// Mirror of <see cref="LastAmbusherLeaderDown"/> on the other
        /// side.</summary>
        public bool LastVictimLeaderDown { get; set; }

        /// <summary>
        /// Set by <c>AmbushEndLogic.OnMissionResultReady</c> when a TotalVictory
        /// fires. Consumed by <see cref="OnCampaignTick"/> on the first tick
        /// after the mission closes to drive the stalled vanilla state machine:
        /// close the stealth wait-menu (which has TimeControlMode = Stop) and
        /// resume normal time so <c>PlayerEncounter.Update</c> can tick through
        /// to End → loot + party destruction.
        /// </summary>
        public bool PendingPostAmbushCleanup { get; set; }

        // Short burst of forced UnstoppablePlay after the post-ambush cleanup
        // so vanilla's PlayerEncounter.Update state machine actually ticks
        // through to the loot UI without waiting on the player manually
        // resuming time. Decremented each OnCampaignTick; stops forcing once
        // the counter hits 0 so a paused-by-choice player isn't left in
        // UnstoppablePlay forever.
        private const int PostAmbushForceTickCount = 10;
        private int _postAmbushForceTicksRemaining;

        /// <summary>
        /// Number of attacker troops who were outside the retreat zone when the
        /// extraction bar completed. Set by AmbushEndLogic before mission close;
        /// consumed by <see cref="OnPlayerBattleEnd"/> to wound that many healthy
        /// survivors from the main party roster. Reset to 0 on consumption.
        /// </summary>
        public int ExtractionTroopsLeftBehind { get; set; }

        /// <summary>
        /// The target party of the most recent ambush. Needed by the Ambush
        /// Escape menu's "engage now" / "escape" options; AmbushCampaignBehavior
        /// can't rediscover it from the MapEvent after the mission closes.
        /// Set in OnStrike, cleared in OnPlayerBattleEnd.
        /// </summary>
        private MobileParty _lastAmbushTarget;

        /// <summary>Phase F §6: when the player flees a Phase F mission
        /// and the ambusher presses the attack, we open a vanilla field
        /// battle. This flag lets OnPlayerBattleEnd recognise the
        /// follow-up battle and apply the extra double-flee morale
        /// penalty. Transient — not serialised; mission can't survive
        /// save anyway.</summary>
        private bool _followUpFieldBattlePending;

        // §6 bluff mechanic: parties flip MobileParty.IsInfoHidden on
        // when they enter ambush/prep/active-ambusher state, so the
        // vanilla UI renders "?" instead of real numbers. No shadow set
        // — "is this party bluffing?" is derived from live ambush state
        // + the vanilla IsInfoHidden flag itself. Cleanup is unconditional:
        // when ambush state ends, flip IsInfoHidden back to false. Small
        // trade-off: if a party vanilla info-hid for another reason
        // happened to be an ambusher, we'd strip the hiding on cleanup.
        // Lord parties / bandit war-parties never fall into that case so
        // the risk is theoretical.

        // Snapshot of the player's non-hero roster at the moment the
        // ambush mission started — captured by AmbushedEndLogic.AfterStart,
        // handed over in OnEndMission via StashAmbushedRosterSnapshot.
        // Drives the "troops committed to this ambush take a share of
        // wounds (on first flee) or disappear (on double flee)" model.
        // Keyed by CharacterObject; value is the healthy count at
        // commitment time.
        private Dictionary<CharacterObject, int> _ambushedRosterSnapshot;

        /// <summary>Phase F §6: the ambusher that pressed the attack is
        /// stashed here from <c>HandleAmbushedFledFollowUp</c> and the
        /// actual <c>PlayerEncounter.RestartPlayerEncounter</c> call
        /// is deferred to <see cref="OnCampaignTick"/>. Running it
        /// re-entrantly from inside <c>OnPlayerBattleEnd</c> races
        /// vanilla's encounter cleanup — see outcome-graph.md
        /// "Why OpenFollowUpFieldBattle must defer".</summary>
        private MobileParty _pendingFollowUpAmbusher;

        /// <summary>Phase F outcome queue — set by
        /// <c>AmbushedEndLogic.OnMissionResultReady</c> for resolutions
        /// that don't naturally close the vanilla <c>PlayerEncounter</c>
        /// (<c>VictimFled</c>, hero-down <c>VictimDefeated</c>). The
        /// outcome menu is activated from <see cref="OnCampaignTick"/>
        /// on the first tick after <c>Mission.Current == null</c>, so
        /// the mission is fully torn down before we interact with
        /// vanilla UI again. See outcome-graph.md.</summary>
        private AmbushResolution _pendingAmbushedResolution = AmbushResolution.None;
        private MobileParty _pendingAmbushedAttacker;

        /// <summary>Branch result cached by the outcome menu's init
        /// delegate so the "Continue" option knows what to do without
        /// re-running the <c>AmbusherPressesAttack</c> roll (keeps the
        /// menu description and the consequence consistent — one roll,
        /// one outcome).</summary>
        private bool _ambushedOutcomeMenuPressBranch;

        public bool IsPartyInStealth(MobileParty party)
        {
            if (party == null) return false;
            if (party == MobileParty.MainParty) return _playerInStealth;
            return false;
        }

        public void OnStealthHotkeyPressed()
        {
            // 2-second real-time input cooldown so panic double-tap can't
            // accidentally toggle off stealth you just entered. Environment.TickCount
            // is milliseconds since boot — overflows every ~24 days but we only
            // care about short deltas.
            float nowRealTime = System.Environment.TickCount / 1000f;
            if (nowRealTime - _lastHotkeyRealTime < HotkeyInputCooldownSeconds) return;

            if (!CanToggleStealth()) return;

            // Hotkey during prep → cancel setup. No cooldown stamp, nothing
            // happened — the ambush was never set.
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

            // Entry chance roll — Roguery / terrain / night / nearby hostiles
            // (count AND their Scouting totals). Failed entry stamps the
            // cooldown just like a failed strike would.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model != null)
            {
                var hostilesInRange = CollectHostileObserversInScanRadius(model);
                float chance = model.CalculateEntryChance(MobileParty.MainParty, hostilesInRange);
                if (MBRandom.RandomFloat >= chance)
                {
                    TORCommon.Say("You couldn't slip away in time.");
                    // Entry failed — no strike landed. Written as a
                    // solo stealth-exit stamp (Resolution.None, no
                    // victim) so cooldown queries treat it as a
                    // position-revealed record without lighting the
                    // disorganize shield.
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
            // LocatorGrid returns a cell-superset — tighten with a
            // distance check so the penalty math doesn't inflate from
            // observers just outside the actual circle.
            var query = MobileParty.StartFindingLocatablesAroundPosition(mainPos, scanRadius);
            for (var observer = MobileParty.FindNextLocatable(ref query); observer != null;
                 observer = MobileParty.FindNextLocatable(ref query))
            {
                if (observer == null || !observer.IsActive) continue;
                if (observer == mainParty) continue;
                if (!observer.IsVisible) continue;   // fog-of-war hidden parties don't count
                if (observer.Position.ToVec2().DistanceSquared(mainPos) > scanRadiusSq) continue;
                if (!model.CanDetect(observer, mainParty)) continue;
                result.Add(observer);
            }
            return result;
        }

        /// <summary>§19 prep entry — hotkey put us into "setting up the
        /// ambush." Party is held in place; <see cref="IsPlayerInStealth"/>
        /// stays false (so the §17 Scouting bonus doesn't kick in yet).
        /// <see cref="TickPlayerStealthPrep"/> finalizes once the model's
        /// <c>CalculatePreparationMinutes</c> elapses.</summary>
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
            // Same long-ignore pattern as the finalized stealth: pursuers
            // shouldn't engage a party actively prepping an ambush either.
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

        /// <summary>Called from <see cref="OnCampaignTick"/>. If enough
        /// game-time elapsed, flip into full stealth stance. Scouting
        /// bonus (§17) activates here via <see cref="IsPlayerInStealth"/>
        /// flipping true. Total prep window is recomputed from the model
        /// — same inputs as entry, so the result is stable while the
        /// party is held in place.</summary>
        private void TickPlayerStealthPrep()
        {
            if (!_playerPreppingStealth) return;
            float elapsed = (float)(CampaignTime.Now - _playerStealthPrepStartedAt).ToMinutes;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float totalMinutes = model?.CalculatePreparationMinutes(MobileParty.MainParty) ?? 15f;

            // Refresh the stealth menu's prep progress text so the player
            // sees the bar fill. RefreshStealthMenuTargets branches on
            // _playerPreppingStealth to render BuildStealthPrepText.
            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == StealthMenuId)
                RefreshStealthMenuTargets();

            if (elapsed < totalMinutes) return;

            _playerPreppingStealth = false;
            _playerInStealth = true;
            _stealthEnteredAt = CampaignTime.Now;
            TORCommon.Say("Ambush is set.");
            TORCommon.Log(LogLevel.Info, "[Ambush] prep complete: elapsed="
                + elapsed.ToString("F1") + " of " + totalMinutes.ToString("F1"));

            // Final refresh — flip into "in cover" mode text + strike list.
            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == StealthMenuId)
                RefreshStealthMenuTargets();
        }

        /// <summary>Hotkey during prep cancels setup. No cooldown stamp
        /// (nothing happened) — party resumes normal campaign play.</summary>
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

        /// <summary>Legacy entry point — now just a direct finalize for
        /// debug-command / forced entry paths. Normal player entry goes
        /// through <see cref="BeginPlayerStealthPrep"/> → tick-driven
        /// finalize. Kept so the console <c>trigger_stealth</c> command
        /// still works without a prep window.</summary>
        public void EnterStealth(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (_playerInStealth) return;
            _playerPreppingStealth = false;

            _playerInStealth = true;
            _stealthEnteredAt = CampaignTime.Now;

            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();
            party.IgnoreByOtherPartiesTill(CampaignTime.YearsFromNow(100f));

            _cachedTimeControlMode = Campaign.Current.TimeControlMode;

            TORCommon.Say("Entered stealth stance.");
            GameMenu.ActivateGameMenu(StealthMenuId);
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
        }

        public void LeaveStealth(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (!_playerInStealth) return;

            _playerInStealth = false;
            party.Ai.SetDoNotMakeNewDecisions(false);
            // Phase B: clear the AI ignore flag by setting the expiry to the past.
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

        public IReadOnlyList<MobileParty> GetAmbushTargets(MobileParty attacker)
        {
            if (attacker == null) return new List<MobileParty>();
            var attackerPos = attacker.Position.ToVec2();
            var attackerFaction = attacker.MapFaction;
            if (attackerFaction == null) return new List<MobileParty>();

            // Strike range is dynamic — scales with the attacker's
            // effective Scouting via model.CalculateStrikeRange.
            // A well-scouted hero plus Scout companion reaches further.
            float strikeRange = Campaign.Current?.Models?.GetAmbushModel()?.CalculateStrikeRange(attacker) ?? StealthRadius;
            float radiusSq = strikeRange * strikeRange;
            var candidates = new List<(MobileParty party, float distSq)>();
            var query = MobileParty.StartFindingLocatablesAroundPosition(attackerPos, strikeRange);
            for (var p = MobileParty.FindNextLocatable(ref query); p != null;
                 p = MobileParty.FindNextLocatable(ref query))
            {
                if (p == null || !p.IsActive || p == attacker) continue;
                if (!p.IsVisible) continue;   // fog-of-war-hidden parties can't be targeted
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(attackerFaction, p.MapFaction)) continue;
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
            // OnPlayerBattleEnd doesn't fire for Phase F AmbushedFled —
            // vanilla's soft-border retreat path returns to the parent
            // PlayerEncounter without a battle-end dispatch. We need the
            // generic OnMissionEndedEvent to catch that case and open
            // our outcome menu.
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnAnyMissionEnded);
            CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, OnQuarterHourlyTick);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
            // GameMenuOpened fires regardless of TimeControlMode — unlike
            // TickEvent, which stalls when time is Stop. Needed because the
            // stealth wait-menu reopening after a TotalVictory mission puts
            // the campaign into Stop → no TickEvent → the OnCampaignTick
            // cleanup never gets to un-stop time (chicken-and-egg). The menu-
            // opened event is our handle to break that cycle.
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);

            ScreenManager.OnPushScreen -= OnScreenPushed;
            ScreenManager.OnPushScreen += OnScreenPushed;

            // Single subscriber for all three ambush producers (Phase D,
            // Phase F, AI-on-AI). Idempotent re-subscription on save-load.
            TORCampaignEvents.Instance.AmbushResolved -= OnAmbushResolved;
            TORCampaignEvents.Instance.AmbushResolved += OnAmbushResolved;
        }

        /// <summary>
        /// Single subscriber for <see cref="TORCampaignEvents.AmbushResolved"/>.
        /// Appends the carried <see cref="AmbushRecord"/> to the shared
        /// history and dispatches the shared post-strike effects
        /// (disorganize apply / shield) for ambusher-win resolutions.
        /// Producers never write the history list directly — they
        /// construct a record and fire this event.
        /// </summary>
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
            // Rebuild the hostile-observer cache before the stealth branch split.
            // The eye widget reads this every frame (via CachedObserversInRange)
            // to drive the Alert icon and the CalculateEntryChance call that
            // sets the eye's openness — it needs fresh data regardless of
            // whether the player is currently stealthed.
            RefreshCachedObservers();

            // Player stealth and player-side scanning are mutually exclusive:
            // hiding is passive, you can't both lie in wait AND actively
            // scout. When the player is not hiding, run the mirror pass —
            // player's party rolls detection against nearby stealthed AI
            // parties. Vanilla-style "hideout found" notification + sound
            // + Scouting XP, same shape as spotting a bandit hideout.
            if (!_playerInStealth)
            {
                RunPlayerDetectsAiPass();
                return;
            }

            var mainParty = MobileParty.MainParty;
            if (mainParty == null) return;

            // Keep the stealth menu's target list current — parties moving in /
            // out of ambush range during the stealth hold would otherwise go
            // stale until the player re-opens the menu. Piggybacks the detection
            // cadence so refresh happens with the same tempo as party-movement
            // beats; the wait-menu tick-delegate hook raced the condition-
            // delegate renderer and caused strike buttons to disappear.
            RefreshStealthMenuTargets();

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            // Observers were cached at the top of this tick by
            // RefreshCachedObservers — reuse here for both the detection
            // roll AND the hidden-tick Roguery XP calculation.
            int observerCount = _cachedObserversInRange.Count;
            int highestObserverScouting = 0;
            foreach (var observer in _cachedObserversInRange)
            {
                int s = observer.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
                if (s > highestObserverScouting) highestObserverScouting = s;

                if (model.TryDetect(observer, mainParty, _stealthEnteredAt, out string reason))
                {
                    TORCommon.Say(reason);
                    // §21 eye-widget: stamp "recently spotted" timestamp so the
                    // Spotted event icon lights for the model's display window.
                    _spottedAt = CampaignTime.Now;
                    LeaveStealth(mainParty);
                    // Spotted without striking — written as a solo
                    // stealth-exit stamp (Resolution.None) so cooldown
                    // queries treat it as a position-revealed record
                    // without lighting the disorganize shield.
                    var aiStamp = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
                    aiStamp?.RecordAmbush(new AmbushRecord(CampaignTime.Now,
                        MobileParty.MainParty, null, AmbushResolution.None));
                    return;
                }
            }

            // Not revealed and at least one hostile within scan range → Roguery XP tick.
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

        /// <summary>Rebuild <see cref="_cachedObserversInRange"/> — hostile
        /// parties inside the model's detection scan radius that pass the
        /// standard <c>CanDetect</c> filter (visible, at-war, not us).
        /// Called from <see cref="OnQuarterHourlyTick"/> before the
        /// stealth branch split so the eye widget always has fresh data
        /// regardless of current stealth state.</summary>
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

        /// <summary>Player-side mirror of the AI-detects-player roll:
        /// for each AI party in stealth within <c>DetectionScanRadius</c>
        /// of the main party, call <c>TryDetect</c> with the main party
        /// as observer. On success, break the AI party's stealth and
        /// fire a <c>TORMapNotification</c> styled like a vanilla
        /// hideout-spotted event (same sound), plus award Scouting XP
        /// to the player's Scout role.</summary>
        private void RunPlayerDetectsAiPass()
        {
            var mainParty = MobileParty.MainParty;
            if (mainParty == null || !mainParty.IsActive) return;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            if (ai == null) return;

            // ToList() to materialise before breaking stealth — the dict
            // mutates on BreakStealthByDetection, so we can't iterate the
            // live enumerable while mutating.
            var candidates = ai.StealthedPartiesInRange(mainParty, model.DetectionScanRadius).ToList();
            foreach (var stealthed in candidates)
            {
                if (!model.TryDetect(mainParty, stealthed, ai.GetStealthEnteredAt(stealthed), out _))
                    continue;

                // Record detection BEFORE BreakStealthByDetection — the
                // latter clears the party from _stealthedParties, which
                // means the IsPartyStealthed check won't short-circuit
                // IsVisibilityPersistent. The player-detection record
                // takes over for the short reveal window.
                var spotPos = stealthed.Position.ToVec2();
                ai.RecordPlayerDetection(stealthed);
                ai.BreakStealthByDetection(stealthed);

                var who = stealthed.LeaderHero?.Name?.ToString()
                          ?? stealthed.Name?.ToString()
                          ?? "A hostile party";
                var text = new TextObject("{=!}{NAME} was spotted lying in wait!");
                text.SetTextVariable("NAME", who);
                Campaign.Current?.CampaignInformationManager?.NewMapNoticeAdded(
                    new CampaignMechanics.MapNotifications.AmbushDetectedMapNotification(
                        text, "tor_ambush_detected",
                        "event:/ui/notification/hideout_found",
                        spotPos));

                // Mirror vanilla's OnHideoutSpotted → Scouting XP for the
                // Scout role. Partied-hero fallback to MainHero when no
                // dedicated Scout is assigned.
                var scout = mainParty.EffectiveScout ?? Hero.MainHero;
                if (scout != null) scout.AddSkillXp(DefaultSkills.Scouting, 100f);

                // One detection per tick — if several AI parties are
                // stealthed near each other, the player sees one, next
                // tick's roll surfaces the next if still hidden. Keeps
                // the notification shelf from flooding.
                return;
            }
        }

        private static void OnScreenPushed(ScreenBase pushedScreen)
        {
            if (pushedScreen is not MapScreen mapScreen) return;
            if (mapScreen.GetMapView<AmbushMapView>() == null)
                mapScreen.AddMapView<AmbushMapView>();
            if (mapScreen.GetMapView<AmbushRangeDebugMapView>() == null)
                mapScreen.AddMapView<AmbushRangeDebugMapView>();
            if (mapScreen.GetMapView<AmbushPrepOverlayMapView>() == null)
                mapScreen.AddMapView<AmbushPrepOverlayMapView>();
        }

        /// <summary>
        /// Fires every campaign tick while the map is active. One-shot drain
        /// of <see cref="PendingPostAmbushCleanup"/> on the first tick after
        /// the TotalVictory mission ends: closes the stealth wait-menu and
        /// restores the previously-cached time control mode. Why: the wait-
        /// menu reopens after mission close with TimeControlMode = Stop,
        /// which stalls vanilla's PlayerEncounter.Update ticks and leaves
        /// the loot/finalize state machine stuck. Clearing both lets vanilla
        /// drive the state machine to End.
        /// </summary>
        /// <summary>
        /// Fires when any game menu opens, including the stealth wait-menu
        /// restored after a mission closes. Catches the TotalVictory
        /// post-mission moment that TickEvent misses because of the Stop
        /// time-control stall.
        /// </summary>
        private void OnGameMenuOpened(MenuCallbackArgs args)
        {
            if (!PendingPostAmbushCleanup) return;
            // Only react to the stealth wait-menu restoring itself after a
            // mission — if any OTHER menu opens, let it be.
            if (args?.MenuContext?.GameMenu?.StringId != StealthMenuId) return;

            PendingPostAmbushCleanup = false;
            args.MenuContext.GameMenu.EndWait();
            GameMenu.ExitToLast();

            _postAmbushForceTicksRemaining = PostAmbushForceTickCount;
            // Set time control immediately — the OnCampaignTick burst takes
            // over from the next tick (which can now actually fire because
            // time isn't Stop anymore).
            if (Campaign.Current != null)
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
        }

        private void OnCampaignTick(float dt)
        {
            TickPlayerStealthPrep();

            // Phase F §6: open the follow-up field battle one tick AFTER
            // the outcome menu's "Continue" picks the press branch (which
            // stashes the ambusher). Opening the vanilla PlayerEncounter
            // immediately after menu-close races vanilla's menu teardown;
            // one tick of delay is the safety margin.
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

            // Force time to advance for a short window so the state machine
            // can tick through Begin → Wait → PrepareResults → ApplyResults →
            // PlayerVictory → LootParty → LootInventory. Loot UI will block
            // further ticks naturally once it appears.
            if (_postAmbushForceTicksRemaining > 0 && Campaign.Current != null)
            {
                _postAmbushForceTicksRemaining--;
                if (Campaign.Current.TimeControlMode != CampaignTimeControlMode.UnstoppablePlay)
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
                // When the burst runs out we DON'T forcibly restore the time
                // control — vanilla's own menus / loot screen will have set
                // their own mode by then, and overriding would stomp them.
            }
        }

        // ---- Phase F §6: AmbushedFled follow-up ----

        /// <summary>Called right after a Phase F AmbushedFled outcome.
        /// Marks spawned victim troops as wounded, then branches on the
        /// ambusher's press-vs-vanish decision. Press → open vanilla
        /// field battle and let its flee/pursuit flow handle the sequel.
        /// Vanish → light morale penalty, done.</summary>
        private void HandleAmbushedFledFollowUp(MobileParty ambusher)
        {
            WoundAmbushedCommittedTroops();
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null || ambusher == null || !ambusher.IsActive)
            {
                float vanishPenalty = model?.AmbushedFledVanishMoralePenalty ?? 5f;
                ApplyMoraleDelta(-vanishPenalty);
                TORCommon.Log(LogLevel.Info, "[Ambush] AmbushedFled: ambusher gone/null — vanish branch, morale -"
                    + vanishPenalty);
                return;
            }
            bool presses = model.AmbusherPressesAttack(ambusher, MobileParty.MainParty);
            TORCommon.Log(LogLevel.Info, "[Ambush] AmbushedFled branch: ambusher="
                + (ambusher.LeaderHero?.Name?.ToString() ?? ambusher.Name?.ToString() ?? "?")
                + " ambusherStrength=" + (ambusher.Party?.EstimatedStrength ?? 0f).ToString("F0")
                + " playerStrength=" + (MobileParty.MainParty.Party?.EstimatedStrength ?? 0f).ToString("F0")
                + " presses=" + presses);
            if (presses)
            {
                // Stash the ambusher — the actual
                // PlayerEncounter.RestartPlayerEncounter call is deferred
                // to OnCampaignTick next frame so we don't race vanilla's
                // encounter cleanup from the just-closed Phase F mission.
                _pendingFollowUpAmbusher = ambusher;
                TORCommon.Log(LogLevel.Info, "[Ambush] follow-up battle queued; will open on next tick.");
                // Morale on the Press-and-Win case is left to vanilla; the
                // double-flee extra is applied by OnPlayerBattleEnd when it
                // sees _followUpFieldBattlePending + player retreat.
            }
            else
            {
                TORCommon.Say("They melted away. Your troops regroup.");
                ApplyMoraleDelta(-model.AmbushedFledVanishMoralePenalty);
                TORCommon.Log(LogLevel.Info, "[Ambush] vanish branch: morale -"
                    + model.AmbushedFledVanishMoralePenalty);
            }
        }

        /// <summary>Shift the still-healthy counts of each tracked
        /// participant from healthy → wounded on the main party.
        /// Pulls the participant list from the mission controller
        /// before the mission tears down, into
        /// <see cref="_ambushedRosterSnapshot"/>, where
        /// <see cref="OnPlayerBattleEnd"/> can still see them for the
        /// double-flee removal.</summary>
        /// <summary>Called from <c>AmbushedEndLogic.OnEndMission</c> with
        /// the roster snapshot captured at mission-start (AfterStart,
        /// before combat touched the roster). Hands "troops committed to
        /// this ambush" over for the Fled wound/remove flows.</summary>
        public void StashAmbushedRosterSnapshot(Dictionary<CharacterObject, int> snapshot)
        {
            _ambushedRosterSnapshot = (snapshot != null && snapshot.Count > 0)
                ? new Dictionary<CharacterObject, int>(snapshot) : null;
        }

        /// <summary>§6 bluff mechanic — flip
        /// <see cref="MobileParty.IsInfoHidden"/> on so vanilla's UI
        /// pipeline displays "?" instead of troop counts for this party.
        /// Idempotent.</summary>
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

        /// <summary>True when the player's current MapEvent has at least
        /// one enemy party flagged as info-hidden — i.e. we're actively
        /// in a battle against a bluffing ambusher. Scoped to the
        /// current engagement, so unrelated battles (fighting a looter
        /// band while an Ungor warband preps an ambush 500u away) don't
        /// suppress the scoreboard.</summary>
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

        /// <summary>Clear bluff state on every party on the ambusher's
        /// side of a mapEvent. Teardown safety net.</summary>
        public void EndBluffOnAmbusherSide(MapEvent mapEvent)
        {
            if (mapEvent == null) return;
            foreach (var p in mapEvent.InvolvedParties)
            {
                var mp = p?.MobileParty;
                if (mp != null && mp != MobileParty.MainParty) EndBluffOn(mp);
            }
        }

        /// <summary>Bluff every party on the ambusher's side of the
        /// current PlayerEncounter MapEvent. The initial trigger only
        /// knows the lead ambusher party; vanilla pulls in nearby
        /// hostile parties onto the same MapEvent side when the
        /// encounter opens (joined-side reinforcements). Each of them
        /// otherwise exposes real troop counts in the hover tooltip
        /// since IsInfoHidden isn't set on them.</summary>
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

        /// <summary>First-flee cost. For each troop type in the committed
        /// snapshot, shifts <c>ceil(startCount * AmbushedFledWoundShare)</c>
        /// of that type from healthy → wounded on the current roster,
        /// capped at currently-healthy of that type (vanilla's combat
        /// casualty flow may already have taken some).</summary>
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

        /// <summary>Double-flee cost. For each troop type in the committed
        /// snapshot, removes <c>ceil(startCount * AmbushedDoubleFleeRemoveShare)</c>
        /// of that type from the current roster — prefers to drop wounded
        /// first (they were marked wounded by WoundAmbushedCommittedTroops
        /// on the first flee), then healthy.</summary>
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

        /// <summary>Opens a vanilla field battle on the campaign map with
        /// the ambusher as attacker. Sets the follow-up flag so
        /// <see cref="OnPlayerBattleEnd"/> can recognise this battle's
        /// aftermath (double-flee removal + extra morale).</summary>
        private void OpenFollowUpFieldBattle(MobileParty ambusher)
        {
            if (ambusher == null || !ambusher.IsActive) return;
            _followUpFieldBattlePending = true;
            // §6 bluff carries over to the follow-up encounter screen —
            // keep IsInfoHidden on so the "stand or retreat" decision
            // bar shows "?" instead of the ambusher's actual (possibly
            // depleted) count. Idempotent if the initial trigger already
            // set it.
            BeginBluffOn(ambusher);
            TORCommon.Say((ambusher.LeaderHero?.Name?.ToString()
                           ?? ambusher.Name?.ToString()
                           ?? "The ambushers")
                          + " press the attack.");
            // Teleport onto the player if the ambusher drifted during
            // mission transitions, then let vanilla open the encounter.
            ambusher.Position = MobileParty.MainParty.Position;
            TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.RestartPlayerEncounter(
                PartyBase.MainParty, ambusher.Party, forcePlayerOutFromSettlement: false);
            if (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Battle == null)
            {
                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.StartBattle();
                TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Update();
            }
            // Vanilla pulls nearby hostile parties onto the ambusher's
            // MapEvent side as the encounter opens — those joiners have
            // their own IsInfoHidden=false unless we flip them here.
            BeginBluffOnAmbusherSide(ambusher);
        }

        private static void ApplyMoraleDelta(float delta)
        {
            var main = MobileParty.MainParty;
            if (main == null) return;
            main.RecentEventsMorale += delta;
        }

        /// <summary>Fires for every mission end, independent of battle
        /// outcome and PlayerEncounter state. Phase F's VictimFled (and
        /// hero-down VictimDefeated) don't route through
        /// <see cref="OnPlayerBattleEnd"/> because vanilla's soft-border
        /// retreat returns to the parent PlayerEncounter without a
        /// battle-end dispatch. OnMissionEndedEvent catches them.
        /// </summary>
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

            // Clear now so a subsequent mission-end doesn't re-fire.
            // OnPlayerBattleEnd never ran for this resolution; nobody
            // else reads LastResolution on the victim-side path.
            LastResolution = AmbushResolution.None;

            // Pop whatever vanilla just pushed (the parent encounter
            // menu returning after the mission closes), then open our
            // outcome menu on top.
            if (Campaign.Current?.CurrentMenuContext != null)
                GameMenu.ExitToLast();
            GameMenu.ActivateGameMenu(AmbushedFledOutcomeMenuId);
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            TORCommon.Log(LogLevel.Info, "[Ambush] OnPlayerBattleEnd fired. LastResolution="
                + LastResolution + " PlayerWasAmbusher=" + PlayerWasAmbusher
                + " followUpPending=" + _followUpFieldBattlePending);

            // §6 bluff — any player battle end resolves the ambush
            // engagement one way or another (Repelled, Wiped,
            // double-flee, follow-up stand). Troop counts are revealed
            // (via combat) or no longer meaningful; drop IsInfoHidden
            // on every party on the mapEvent's enemy side.
            EndBluffOnAmbusherSide(mapEvent);

            // Phase F §6 double-flee check: fires ONLY when the player
            // pressed retreat during the follow-up field battle — not
            // on stand-and-lose. See outcome-graph.md "The checks that
            // matter" for the full reasoning.
            //
            // RetreatingSide is set by vanilla's EndByRunAway, which
            // runs only when a side explicitly ran. PlayerSide is
            // dynamic (resolves to PartyBase.MainParty.Side) so it
            // survives any attacker/defender swap vanilla might do.
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
                // Teleport the ambusher off the player tile regardless of
                // double-flee outcome — they were last placed onto the
                // player by OpenFollowUpFieldBattle, and vanilla's
                // engagement AI would re-encounter them on the next tick
                // if they stayed stacked. Mirror of the vanish-branch
                // teleport in OnAmbushedFledOutcomeContinue.
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

            // Routes resolution → aftermath. Full map in
            // TORTasks/ambush-feature/mission-design.md. Resolution
            // enum is unified across Phase D / Phase F / AI-on-AI;
            // PlayerWasAmbusher distinguishes the player-role branch
            // for menu selection and record construction.
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
            // _lastAmbushTarget is cleared when the escape/engage option fires,
            // not here — the Ambush Escape menu may still need it after this.
        }

        /// <summary>
        /// Player-ambusher (Phase D) post-mission branch. Fires the
        /// <see cref="TORCampaignEvents.AmbushResolved"/> event with a
        /// fully-populated record and drives the follow-up menu for
        /// non-Extracted outcomes. Extracted was already dispatched
        /// synchronously via <see cref="BeginExtractionTransition"/>
        /// when the mission closed.
        /// </summary>
        private void HandlePlayerAmbusherResolution()
        {
            if (LastResolution == AmbushResolution.None) return;
            if (LastResolution == AmbushResolution.AmbusherPartialVictory)
            {
                // Extracted path already fired its event and opened its
                // menu from BeginExtractionTransition. OnPlayerBattleEnd
                // runs after, with nothing left to do.
                return;
            }

            var missionModel = Campaign.Current?.Models?.GetMissionAmbushModel();
            bool wasLoud = missionModel != null
                           && LastAmbushLoudness >= missionModel.LoudnessEventThreshold;

            // Single event fire. The subscriber appends the record and
            // runs shared post-strike effects for ambusher-win cases.
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, MobileParty.MainParty, _lastAmbushTarget,
                                 LastResolution, wasLoud,
                                 LastAmbusherLeaderDown, LastVictimLeaderDown));

            switch (LastResolution)
            {
                case AmbushResolution.AmbusherTotalVictory:
                    // Quiet TotalVictory: no witnesses — stealth is
                    // immediately available via the instant-ambush
                    // model query. Loud: scouts carried the noise,
                    // cooldown length reflects that via WasLoud.
                    //
                    // DO NOT force-destroy the target party. OnPlayerBattleEnd
                    // runs mid-ApplyResults; vanilla still has PlayerVictory
                    // → LootParty → LootInventory → End ahead. End handles
                    // 0-troop party cleanup. Firing DestroyPartyAction
                    // mid-state-machine crashes native.
                    if (wasLoud)
                    {
                        TORCommon.Say("The noise of your attack carries far. Scouts are looking for you — stealth will be slow to re-establish.");
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
                    // Includes the hero-wounded sub-case (distinguished
                    // by LastAmbusherLeaderDown). Both route to the
                    // same failed-ambush menu.
                    if (LastAmbusherLeaderDown)
                    {
                        GameMenu.ActivateGameMenu(AmbushFailedMenuId);
                    }
                    break;
                case AmbushResolution.Stalemate:
                    // Timed out without clearing the patrol. Vanilla
                    // defeat flow handles casualties.
                    break;
            }
        }

        /// <summary>
        /// Player-victim (Phase F) post-mission branch. Fires
        /// <see cref="TORCampaignEvents.AmbushResolved"/> with the AI
        /// ambusher as the ambusher role, queues the Phase F outcome
        /// menu for resolutions that don't naturally close the
        /// PlayerEncounter, and cleans up the per-encounter ambusher
        /// ref.
        /// </summary>
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
                    TORCommon.Say("You drove them off. You got a good look at them — they won't stay hidden for long.");
                    break;
                case AmbushResolution.VictimFled:
                    // Soft-border exit. Needs a deferred menu because
                    // vanilla's retreat path doesn't fully tear down
                    // the encounter before OnPlayerBattleEnd fires.
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
                        // Hero-down variant — same deferred menu path
                        // as VictimFled, different branch inside the
                        // menu's init delegate.
                        _pendingAmbushedResolution = LastResolution;
                        _pendingAmbushedAttacker = ambusher;
                        TORCommon.Log(LogLevel.Info, "[Ambush] Phase F outcome menu opening for "
                            + LastResolution + " (hero down) ambusher="
                            + (ambusher?.LeaderHero?.Name?.ToString()
                               ?? ambusher?.Name?.ToString() ?? "?"));
                        GameMenu.ActivateGameMenu(AmbushedFledOutcomeMenuId);
                    }
                    // Non-hero-down VictimDefeated: vanilla defeat flow
                    // runs casualties / prisoners, nothing extra.
                    break;
            }

            if (aiBehavior != null) aiBehavior.CurrentAmbusherOnPlayer = null;

            // Close the AmbushedIntroMenu if it survived the mission.
            // OnAmbushedIntroPrepare now ExitToLast()s before opening
            // the mission so the usual case is already fine; this is
            // the belt-and-suspenders half for state-machine corners
            // where vanilla re-pushed the menu during PlayerEncounter
            // cleanup.
            var ambushedCtx = Campaign.Current?.CurrentMenuContext;
            if (ambushedCtx?.GameMenu?.StringId == AmbushedIntroMenuId)
            {
                GameMenu.ExitToLast();
            }
        }

        /// <summary>
        /// Convert <paramref name="count"/> healthy survivors in the main party
        /// roster into wounded troops. Used to apply the "left behind at
        /// extraction" penalty: the extraction bar completed with the hero in
        /// the zone, but some troops were outside and didn't make it to the
        /// pickup. Hero itself is unaffected; vanilla casualty flow has already
        /// run by the time this fires.
        /// </summary>
        /// <summary>
        /// Called synchronously from <see cref="Missions.AmbushMission.AmbushEndLogic"/>
        /// the moment the Extracted outcome is decided, so the post-ambush
        /// menu is already queued before the mission screen tears down.
        /// This matches the brawl / POI-visit pattern where the next menu
        /// is set up inside <c>MissionEnded</c>, not deferred to vanilla's
        /// <c>OnPlayerBattleEnd</c> dispatch. Prevents the scoreboard
        /// interstitial that otherwise wins the race.
        /// </summary>
        public void BeginExtractionTransition(int leftBehind)
        {
            WoundLeftBehindTroops(leftBehind);
            // Phase D Extracted: ambusher = player, victim =
            // _lastAmbushTarget. Loudness reads from LastAmbushLoudness
            // (set during the mission). Subscriber writes the record
            // and runs shared post-strike effects.
            var missionModel = Campaign.Current?.Models?.GetMissionAmbushModel();
            bool wasLoud = missionModel != null
                           && LastAmbushLoudness >= missionModel.LoudnessEventThreshold;
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, MobileParty.MainParty, _lastAmbushTarget,
                                 AmbushResolution.AmbusherPartialVictory, wasLoud));
            GameMenu.ActivateGameMenu(AmbushEscapeMenuId);
        }

        /// <summary>
        /// Shared campaign-side effect for any successful ambush, keyed
        /// on the two role parties (ambusher / ambushed) rather than on
        /// whether the player is involved — the player can be in either
        /// role, and in both cases the ambushed side takes the
        /// disorganized debuff while the ambusher sheds any they were
        /// carrying. Disorganized is vanilla's ~6h -40% campaign-speed
        /// state (<see cref="TaleWorlds.CampaignSystem.ComponentInterfaces.PartyImpairmentModel"/>);
        /// <c>CanGetDisorganized</c> gates the apply so dead parties,
        /// rosters below 10, and non-leader army members are skipped.
        /// </summary>
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
                // Skip hero rows — they're handled by vanilla wounded flow.
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

            // Defensive: a save mid-stealth loads with the flag set but no menu / no movement lock.
            // Clear the flag so the player resumes with a normal map state.
            if (_playerInStealth)
            {
                _playerInStealth = false;
                _currentTargets.Clear();
                MobileParty.MainParty?.Ai?.SetDoNotMakeNewDecisions(false);
                MobileParty.MainParty?.IgnoreByOtherPartiesTill(CampaignTime.Zero);
            }

            // §6 bluff — IsInfoHidden is a vanilla SaveableField, so UI
            // state persists across save/load without any work from us.
            // No shadow set to rebuild.
        }

        private void RegisterMenus(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                StealthMenuId,
                "{STEALTH_MENU_TEXT}",
                OnStealthMenuInit,
                _ => true,
                null,
                // Tick delegate intentionally empty — refresh is driven from
                // OnQuarterHourlyTick instead so the cadence matches the
                // detection roll and we don't clobber the condition-delegate
                // state mid-render (observed: strike buttons failed to appear
                // when the wait-menu tick cleared _currentTargets each frame).
                (args, dt) => { },
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
                GameMenu.MenuOverlayType.Encounter,
                targetWaitHours: 0f,
                GameMenu.MenuFlags.None,
                null);

            for (int i = 0; i < MaxTargetSlots; i++)
            {
                int slot = i;
                // Each strike option gets its own placeholder variable
                // (STRIKE_OPT_TEXT_0 .. STRIKE_OPT_TEXT_<MaxTargetSlots-1>) so
                // we can set per-slot text in OnStealthMenuInit. A single shared
                // placeholder gets overwritten by whichever call fires last and
                // leaves the other options blank — that's the bug we're
                // fixing. Setting args.Text in the condition delegate is NOT a
                // substitute (the menu reads the template, not args.Text, for
                // its label).
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

        /// <summary>
        /// Phase F pre-mission encounter beat. The AI party has sprung on the
        /// player; this menu renders a narrative line + a single "Prepare to
        /// fight!" option that opens the Phase F mission. Matches vanilla's
        /// encounter_meeting cadence (narrative beat before combat) and gives
        /// the MapEvent an extra tick to finalize before the mission opener
        /// reads AttackerSide.TroopCount — fixes an empty-ambusher-team bug
        /// seen when opening the mission directly from the debug trigger.
        /// </summary>
        private void RegisterAmbushedIntroMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                AmbushedIntroMenuId,
                "{=tor_ambushed_intro}Your column has been moving through the pass for some time, no sign of anyone else on the road. A sudden movement in the brush. Steel on steel. Then they are on you — a lord's war band, waiting for exactly this.",
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

        /// <summary>
        /// Consequence of the Phase F intro menu's "Prepare to fight!" option.
        /// By this point PlayerEncounter + MapEvent have been set up by the
        /// caller (debug command or, in Slice 2, the AI decision tick). We
        /// just need to reset the retreat zone registry (shared with Phase D)
        /// and open the mission.
        ///
        /// Close the intro menu first — otherwise the mission screen pushes
        /// on top of it, and when the mission ends vanilla pops back here
        /// (infinite "Prepare to fight?" loop).
        /// </summary>
        private void OnAmbushedIntroPrepare()
        {
            GameMenu.ExitToLast();
            AmbushRetreatZone.ResetRegistry();
            TorMissionManager.OpenAmbushedMission(DefaultAmbushScene);
        }

        private void RegisterAmbushEscapeMenu(CampaignGameStarter starter)
        {
            // Shown after AmbushResolution.AmbusherPartialVictory (hero
            // made it through the extraction bar). The surviving target
            // party is still at the ambush point.
            // Player picks: resume the fight on the campaign map (full battle)
            // or peel away.
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
            // Shown after AmbushResolution.AmbusherDefeated with
            // AmbusherLeaderDown=true (hero down). The hero is out but the
            // surviving party is still in the field. Player picks: cut losses
            // and flee (take more casualties) or let the survivors finish the
            // fight without the hero (auto-resolved battle, can wipe entirely).
            starter.AddGameMenu(
                AmbushFailedMenuId,
                "{=tor_ambush_failed_intro}You are down. Your surviving troops have broken contact and are regrouping around your fallen banner. They can carry you clear at the cost of more wounded, or press the fight without you — if they lose, your army is gone.",
                _ => { },
                GameMenu.MenuOverlayType.None);

            starter.AddGameMenuOption(
                AmbushFailedMenuId,
                "tor_ambush_failed_run",
                "{=tor_ambush_failed_run}Run — carry me clear, cost be damned.",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Escape; return true; },
                _ => OnAmbushFailedRun(),
                isLeave: true, index: 0, isRepeatable: false);

            starter.AddGameMenuOption(
                AmbushFailedMenuId,
                "tor_ambush_failed_fight",
                "{=tor_ambush_failed_fight}Fight on — finish what we started.",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.HostileAction; return true; },
                _ => OnAmbushFailedFight(),
                isLeave: false, index: 1, isRepeatable: false);
        }

        /// <summary>Phase F §6 outcome menu — the graceful transition
        /// between the Phase F mission closing and whatever comes next.
        /// Fires for the two outcomes that don't trigger
        /// <c>OnPlayerBattleEnd</c>:
        /// <list type="bullet">
        ///   <item><c>AmbushedFled</c>: wounded promotion runs in the
        ///   init-delegate, then <c>AmbusherPressesAttack</c> picks
        ///   vanish-vs-press. The flavor text and "Continue"
        ///   consequence both read the branch choice.</item>
        ///   <item><c>AmbushedHeroWounded</c>: flavor-only; "Continue"
        ///   returns to the campaign map without a follow-up battle.</item>
        /// </list>
        /// </summary>
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

        /// <summary>Init-delegate for the outcome menu. Runs once when
        /// the menu opens. Branches on the stashed resolution, prepares
        /// the flavor text via <c>MBTextManager</c>, applies the
        /// wound-promotion for VictimFled, and caches the
        /// press-vs-vanish decision so <c>OnAmbushedFledOutcomeContinue</c>
        /// can act on it. Clears the pending-resolution fields.</summary>
        private void OnAmbushedFledOutcomeMenuInit(MenuCallbackArgs args)
        {
            var resolution = _pendingAmbushedResolution;
            var ambusher = _pendingAmbushedAttacker;
            _pendingAmbushedResolution = AmbushResolution.None;
            _pendingAmbushedAttacker = null;

            // Vanilla re-fires menu init when the campaign screen resumes
            // after the mission screen tears down — the pending fields were
            // already consumed by the first fire, so a second pass arrives
            // with resolution=None. Early-return so we don't clobber the
            // flavor text or the cached press-branch flag.
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
                    flavor = new TextObject("{=tor_ambushed_outcome_press}You broke contact — but the {CULTURE} are not done. They regroup and move to pin you down. They mean to finish what they started.");
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
                // Hero-down sub-case — non-hero-down VictimDefeated
                // doesn't route through this menu (vanilla defeat flow).
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

        /// <summary>"Continue" option consequence. Vanish branch applies
        /// the morale penalty and stamps the pair-record; press branch
        /// queues the follow-up field battle for the next
        /// <see cref="OnCampaignTick"/> (so vanilla's menu teardown
        /// completes before <c>PlayerEncounter.RestartPlayerEncounter</c>
        /// runs).</summary>
        private void OnAmbushedFledOutcomeContinue()
        {
            var aiBehavior = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            var ambusher = aiBehavior?.CurrentAmbusherOnPlayer;

            if (_ambushedOutcomeMenuPressBranch
                && ambusher != null && ambusher.IsActive)
            {
                _pendingFollowUpAmbusher = ambusher;
                TORCommon.Say("The enemy regroups. They are coming for you.");
                TORCommon.Log(LogLevel.Info, "[Ambush] outcome menu continue: press — queued follow-up battle.");
                GameMenu.ExitToLast();
                return;
            }

            // Vanish branch — morale penalty, back to map.
            // Physical separation (player teleport + 2-tick ignore) and
            // the Phase F cooldown pair-record are both already handled
            // by AmbushedEndLogic.OnEndMission before this menu opens.
            // See decompiled PlayerEncounter.cs:1072-1078 for the
            // teleport-and-ignore, and [[feedback_ambush_role_asymmetry]]
            // for the record semantics.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float moraleHit = model?.AmbushedFledVanishMoralePenalty ?? 5f;
            ApplyMoraleDelta(-moraleHit);
            TORCommon.Say("The ambushers melt back into cover. Your troops lick their wounds. (Morale -" + moraleHit + ")");
            // §6 bluff ends on vanish — the ambusher melted away, so
            // their actual size no longer matters to this engagement.
            if (ambusher != null) EndBluffOn(ambusher);
            if (aiBehavior != null && !_ambushedOutcomeMenuPressBranch)
                aiBehavior.CurrentAmbusherOnPlayer = null;
            TORCommon.Log(LogLevel.Info, "[Ambush] outcome menu continue: vanish — morale -" + moraleHit
                + " PlayerEncounter.Current="
                + (TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.Current != null ? "ALIVE" : "null"));

            GameMenu.ExitToLast();
        }

        private void OnAmbushEscapeEngage()
        {
            // Open a vanilla field battle at current positions. Target party is
            // whoever we just ambushed. Close the pre-extraction encounter
            // first; StartPartyEncounter wants a clean slate and vanilla
            // doesn't handle stacking a second encounter on top of a
            // dangling one well.
            if (_lastAmbushTarget == null || !_lastAmbushTarget.IsActive) { GameMenu.ExitToLast(); _lastAmbushTarget = null; return; }
            var target = _lastAmbushTarget;
            _lastAmbushTarget = null;
            if (PlayerEncounter.Current != null) PlayerEncounter.Finish(false);
            EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
        }

        private void OnAmbushEscapeSlip()
        {
            // Teleport the player's party a short distance along the vector
            // pointing away from the target so the AI doesn't immediately
            // re-engage, then close the lingering encounter — the
            // campaign doesn't handle unfinished PlayerEncounters cleanly
            // and the hit-and-run fiction is "the engagement is over."
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
            // Flat casualty hit on top of what the battle already took.
            // Hero itself was already marked wounded by vanilla; we add roster
            // wounds for the survivors.
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
            // Resume as a vanilla battle with the hero already down. Vanilla
            // battle flow will auto-resolve or allow further player involvement
            // if any survivors can continue. If the hero is wounded (not dead),
            // the player watches via spectate mode.
            if (_lastAmbushTarget == null || !_lastAmbushTarget.IsActive) { GameMenu.ExitToLast(); _lastAmbushTarget = null; return; }
            var target = _lastAmbushTarget;
            _lastAmbushTarget = null;
            EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
        }

        private void OnStealthMenuInit(MenuCallbackArgs args)
        {
            // Don't call StartWait() here — ActivateGameMenu already invokes it for wait menus,
            // and StartWait sets TimeControlMode = UnstoppableFastForward. Calling it again is
            // redundant and either way we override to Stop in EnterStealth.

            var culture = Hero.MainHero?.MapFaction?.Culture;
            if (culture != null && args.MenuContext != null)
                args.MenuContext.SetBackgroundMeshName(culture.EncounterBackgroundMesh);

            RefreshStealthMenuTargets();
        }

        /// <summary>
        /// Rebuilds <see cref="_currentTargets"/> and the per-slot menu text
        /// variables from the live world state. Called on menu init AND on
        /// each quarter-hourly tick (while in stealth) so the strike slot
        /// list stays current without racing the condition-delegate render.
        /// </summary>
        private void RefreshStealthMenuTargets()
        {
            _currentTargets.Clear();

            // §19 prep mode — the Gauntlet overlay (AmbushPrepOverlayMapView)
            // handles the visual bar; the stealth menu stays empty so the
            // UI reads as one clean indicator. Strike-slot condition
            // delegates gate on _playerInStealth, which is false during
            // prep, so no stale strike options appear.
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

            // Updating _currentTargets alone doesn't make previously-hidden
            // strike buttons appear: the game-menu UI caches which options are
            // visible when the menu activates and only re-evaluates condition
            // delegates on an explicit refresh. Nudge the manager so the new
            // target slots light up (and emptied slots disappear) mid-wait.
            // RefreshMenuOptions → Handler.OnMenuRefresh → GameMenuDataSource.
            // Refresh(true), which rebuilds ItemList by re-running every
            // option's condition delegate (incl. our StrikeSlotCondition).
            //
            // Handler null-check matters: on the first-time init path we're
            // called via OnStealthMenuInit, which runs inside MenuContext.
            // Refresh() *before* MapScreen's UpdateMenuView has constructed
            // the view-side MenuViewContext — so ctx.Handler is still null
            // and vanilla's RefreshMenuOptions NPEs on it. Skip the refresh
            // in that case; the init path is about to render the options
            // list for the first time with the fresh _currentTargets anyway.
            var ctx = Campaign.Current?.CurrentMenuContext;
            if (ctx?.GameMenu?.StringId == StealthMenuId && ctx.Handler != null)
                Campaign.Current?.GameMenuManager?.RefreshMenuOptions(ctx);
        }

        private bool StrikeSlotCondition(MenuCallbackArgs args, int slot)
        {
            if (slot >= _currentTargets.Count) return false;
            var target = _currentTargets[slot];
            if (target == null || !target.IsActive) return false;

            // Option label text is set via the per-slot MBTextManager variable
            // in OnStealthMenuInit — the menu reads its template from the
            // registered placeholder, not from args.Text.
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

            // Drop stealth before combat. No cooldown stamp here —
            // Phase D records the AmbushRecord at resolution time with
            // the correct AmbusherRepelled flag, so the strike-time
            // pre-stamp (which hardcoded repelled=false) is gone. Every
            // abandon path (crash, quit mid-mission, save+load mid-
            // mission) either rolls back before the strike or resumes
            // in-mission, so there's no "strike happened but no record"
            // scenario to defend against.
            LeaveStealth(MobileParty.MainParty);

            // Teleport the player onto the target before starting the encounter
            // so the campaign-map visualization matches the fiction: the player
            // walked up to the target and struck. Without this the engagement
            // happens at wherever the player was when they entered stealth and
            // the target "snaps" to the player's location — immersion-breaking,
            // and (worse) picks the wrong terrain for vanilla's fail-path field
            // battle.
            MobileParty.MainParty.Position = target.Position;

            if (success)
            {
                TORCommon.Say("Ambush succeeded (" + chancePct + "%) on " + GetPartyLabel(target) + ".");
                _lastAmbushTarget = target;
                // Bypass EncounterManager.StartPartyEncounter — it fires encounter_meeting
                // menu + enemy-leader conversation, both of which deadlock our mission open.
                // RestartPlayerEncounter(defender, attacker) skips straight to encounter
                // state (same pattern TrollCaveMenuLogic uses).
                PlayerEncounter.RestartPlayerEncounter(target.Party, PartyBase.MainParty, false);
                if (PlayerEncounter.Battle == null)
                {
                    PlayerEncounter.StartBattle();
                    PlayerEncounter.Update();
                }
                // Clear any stale AmbushRetreatZone registrations from a prior
                // scene BEFORE OpenNew — the current scene's AmbushRetreatZone
                // OnInit will fire during scene load that follows, so clearing
                // after would wipe the just-registered zone.
                AmbushRetreatZone.ResetRegistry();
                TorMissionManager.OpenAmbushMission(DefaultAmbushScene);
            }
            else
            {
                // Strike roll fail: commit the player to the fight directly
                // instead of routing through EncounterManager.StartPartyEncounter.
                // EncounterManager opens the vanilla encounter_meeting game menu,
                // which includes a Leave option — letting the player bail out of
                // a botched ambush. Design calls for no bail: the attempt costs
                // you the fight regardless of outcome. Same no-menu pattern as
                // the success branch (RestartPlayerEncounter + StartBattle)
                // minus the TorMissionManager.OpenAmbushMission call, so
                // vanilla's field-battle flow takes over.
                TORCommon.Say("Ambush failed (" + chancePct + "%) on " + GetPartyLabel(target) + " — regular battle.");
                PlayerEncounter.RestartPlayerEncounter(target.Party, PartyBase.MainParty, false);
                if (PlayerEncounter.Battle == null)
                {
                    PlayerEncounter.StartBattle();
                    PlayerEncounter.Update();
                }
            }
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
            // _spottedAt intentionally NOT synced — cosmetic UI timestamp
            // with a <1-hour window. Reload after that window is lost
            // anyway; reload inside it just leaves the icon unlit. No
            // gameplay impact either way.
            // No bluff state to sync — IsInfoHidden is a vanilla
            // SaveableField, no shadow dict to persist.
            // _playerAmbushCooldownUntil removed — player cooldown is
            // now derived from AiAmbushBehavior._ambushRecords.
        }
    }
}
