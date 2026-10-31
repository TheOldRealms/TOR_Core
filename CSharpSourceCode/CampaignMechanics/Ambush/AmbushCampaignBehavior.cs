using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        // Distance (metres) the player's party is teleported along the escape
        // vector when picking "escape" from the Ambush Escape menu. Enough to
        // prevent immediate re-engagement by the target party's AI.
        private const float AmbushEscapeTeleportDistance = 50f;

        // First delivered scene from the scener (TOR_Environment/SceneObj/test_ambush/).
        // Has an authored AmbushRetreatZone (Radius=10) + TacticalRegion=Forest.
        // Will become a lookup table (terrain → scene[]) once more scenes land.
        public const string DefaultAmbushScene = "test_ambush";

        private bool _playerInStealth;
        private CampaignTime _stealthEnteredAt = CampaignTime.Zero;
        private CampaignTime _playerAmbushCooldownUntil = CampaignTime.Zero;

        private readonly List<MobileParty> _currentTargets = new List<MobileParty>();
        private CampaignTimeControlMode _cachedTimeControlMode = CampaignTimeControlMode.StoppablePlay;

        public bool IsPlayerInStealth => _playerInStealth;
        public CampaignTime PlayerAmbushCooldownUntil => _playerAmbushCooldownUntil;
        public CampaignTime StealthEnteredAt => _stealthEnteredAt;
        public bool IsOnAmbushCooldown => _playerAmbushCooldownUntil.IsFuture;

        /// <summary>
        /// Outcome of the most recent ambush mission. Set by AmbushEndLogic as the
        /// mission closes; read by Phase E's aftermath handler to pick escape vs
        /// press-attack / wounded-flow. Reset to <c>None</c> once consumed.
        /// </summary>
        public AmbushOutcome LastAmbushOutcome { get; set; } = AmbushOutcome.None;

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

        public bool IsPartyInStealth(MobileParty party)
        {
            if (party == null) return false;
            if (party == MobileParty.MainParty) return _playerInStealth;
            return false;
        }

        public void OnStealthHotkeyPressed()
        {
            if (!CanToggleStealth()) return;

            if (_playerInStealth)
                LeaveStealth(MobileParty.MainParty);
            else if (!IsOnAmbushCooldown)
                EnterStealth(MobileParty.MainParty);
            else
                TORCommon.Say("Stealth is still on cooldown.");
        }

        public void EnterStealth(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (_playerInStealth) return;

            _playerInStealth = true;
            _stealthEnteredAt = CampaignTime.Now;

            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();

            // Phase B: drop the party off the AI target list. AiEngagePartyBehavior skips
            // targets where ShouldBeIgnored is true. Set a far-future expiry so the ignore flag
            // persists until LeaveStealth clears it.
            party.IgnoreByOtherPartiesTill(CampaignTime.YearsFromNow(100f));

            _cachedTimeControlMode = Campaign.Current.TimeControlMode;

            TORCommon.Say("Entered stealth stance.");
            GameMenu.ActivateGameMenu(StealthMenuId);
            // ActivateGameMenu + the wait-menu's internal StartWait set TimeControlMode to
            // UnstoppableFastForward; override back to Stop so the player starts paused and can
            // choose their own speed via the UI arrows.
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
            var radiusSq = StealthRadius * StealthRadius;
            var attackerFaction = attacker.MapFaction;

            return MobileParty.All
                .Where(p => p != null
                            && p.IsActive
                            && p != attacker
                            && p.MapFaction != null
                            && attackerFaction != null
                            && FactionManager.IsAtWarAgainstFaction(attackerFaction, p.MapFaction)
                            && p.Position.ToVec2().DistanceSquared(attackerPos) <= radiusSq)
                .OrderBy(p => p.Position.ToVec2().DistanceSquared(attackerPos))
                .Take(MaxTargetSlots)
                .ToList();
        }

        public void StampAmbushCooldown()
        {
            _playerAmbushCooldownUntil = CampaignTime.HoursFromNow(CooldownHours);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
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
        }

        private void OnQuarterHourlyTick()
        {
            // Only roll detection while the player is actively hiding. Game-time stopped
            // (menu paused) → no quarter-hourly ticks → no rolls, which is intended.
            if (!_playerInStealth) return;

            var mainParty = MobileParty.MainParty;
            if (mainParty == null) return;

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            float scanRadius = model.DetectionScanRadius;
            var mainPos = mainParty.Position.ToVec2();
            float scanRadiusSq = scanRadius * scanRadius;

            // Cheap spatial pre-filter so we don't ask the model about every party on
            // the map each tick. Eligibility (hostility etc.) is the model's CanDetect
            // responsibility, invoked inside TryDetect.
            foreach (var observer in MobileParty.All)
            {
                if (observer == null || !observer.IsActive) continue;
                if (observer.Position.ToVec2().DistanceSquared(mainPos) > scanRadiusSq) continue;

                if (model.TryDetect(observer, mainParty, _stealthEnteredAt, out string reason))
                {
                    TORCommon.Say(reason);
                    LeaveStealth(mainParty);
                    return; // one break per tick is enough — stealth is gone
                }
            }
        }

        private static void OnScreenPushed(ScreenBase pushedScreen)
        {
            if (pushedScreen is not MapScreen mapScreen) return;
            if (mapScreen.GetMapView<AmbushMapView>() == null)
                mapScreen.AddMapView<AmbushMapView>();
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

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (_playerInStealth) LeaveStealth(MobileParty.MainParty);

            // Routes outcome → aftermath. Full map in
            // TORTasks/ambush-feature/mission-design.md.
            switch (LastAmbushOutcome)
            {
                case AmbushOutcome.TotalVictory:
                    // No witnesses, no alarm — stealth is immediately available.
                    //
                    // DO NOT force-destroy the target party here. OnPlayerBattleEnd
                    // fires at vanilla's ApplyResults state, which is MID-flow —
                    // vanilla still has PlayerVictory → LootParty → LootInventory
                    // → End ahead. The End state is where vanilla handles 0-troop
                    // / empty-party cleanup itself. Firing DestroyPartyAction here
                    // crashes vanilla's state machine (observed: encounter menu
                    // opens for loot screen, our destroy fires 2ms later, native
                    // crash). We leave the party alone; vanilla's natural flow
                    // destroys 0-troop parties as part of DoEnd.
                    _playerAmbushCooldownUntil = CampaignTime.Zero;
                    TORCommon.Say("No one escaped the ambush. You can slip back into cover immediately.");
                    if (_lastAmbushTarget != null)
                    {
                        var roster = _lastAmbushTarget.MemberRoster;
                        int total = roster != null ? roster.TotalManCount : -1;
                        bool active = _lastAmbushTarget.IsActive;
                        TaleWorlds.Library.Debug.Print(
                            "[AmbushCampaignBehavior] TotalVictory post-mission:"
                            + " target=" + GetPartyLabel(_lastAmbushTarget)
                            + " active=" + active
                            + " totalManCount=" + total
                            + " (letting vanilla finish the cleanup)");
                    }
                    _lastAmbushTarget = null;
                    break;
                case AmbushOutcome.Extracted:
                    WoundLeftBehindTroops(ExtractionTroopsLeftBehind);
                    GameMenu.ActivateGameMenu(AmbushEscapeMenuId);
                    break;
                case AmbushOutcome.HeroWounded:
                    // Hero down. The "Ambush Failed" menu offers run-with-losses
                    // vs auto-battle fight; vanilla wounded flow for the hero
                    // itself already fired via BattleEndLogic.
                    GameMenu.ActivateGameMenu(AmbushFailedMenuId);
                    break;
                case AmbushOutcome.Defeated:
                case AmbushOutcome.Stalemate:
                    // Vanilla defeat flow already handled casualties / prisoners.
                    break;
            }
            LastAmbushOutcome = AmbushOutcome.None;
            ExtractionTroopsLeftBehind = 0;
            // _lastAmbushTarget is cleared when the escape/engage option fires,
            // not here — the Ambush Escape menu may still need it after this.
        }

        /// <summary>
        /// Convert <paramref name="count"/> healthy survivors in the main party
        /// roster into wounded troops. Used to apply the "left behind at
        /// extraction" penalty: the extraction bar completed with the hero in
        /// the zone, but some troops were outside and didn't make it to the
        /// pickup. Hero itself is unaffected; vanilla casualty flow has already
        /// run by the time this fires.
        /// </summary>
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
        }

        private void RegisterMenus(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                StealthMenuId,
                "{STEALTH_MENU_TEXT}",
                OnStealthMenuInit,
                _ => true,
                null,
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
        }

        private void RegisterAmbushEscapeMenu(CampaignGameStarter starter)
        {
            // Shown after AmbushOutcome.Extracted. The hero made it through the
            // bar; the surviving target party is still at the ambush point.
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
            // Shown after AmbushOutcome.HeroWounded. The hero is down but the
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

        private void OnAmbushEscapeEngage()
        {
            // Open a vanilla field battle at current positions. Target party is
            // whoever we just ambushed.
            if (_lastAmbushTarget == null || !_lastAmbushTarget.IsActive) { GameMenu.ExitToLast(); _lastAmbushTarget = null; return; }
            var target = _lastAmbushTarget;
            _lastAmbushTarget = null;
            EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
        }

        private void OnAmbushEscapeSlip()
        {
            // Teleport the player's party a short distance along the vector
            // pointing away from the target so the AI doesn't immediately
            // re-engage.
            var main = MobileParty.MainParty;
            if (main != null && _lastAmbushTarget != null && _lastAmbushTarget.IsActive)
            {
                var away = CampaignVec2.Normalized(main.Position - _lastAmbushTarget.Position);
                main.Position = main.Position + away * AmbushEscapeTeleportDistance;
            }
            _lastAmbushTarget = null;
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

            _currentTargets.Clear();
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
            // for that option comes from these variables (set at menu-init time
            // to avoid every-tick recomputation of the chance %).
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
                    // Empty — condition returns false, option hides. Variable
                    // still cleared defensively.
                    MBTextManager.SetTextVariable(varName, new TextObject(""));
                }
            }
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

            // Drop stealth before combat; cooldown fires regardless of outcome.
            LeaveStealth(MobileParty.MainParty);
            StampAmbushCooldown();

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
                TORCommon.Say("Ambush failed (" + chancePct + "%) on " + GetPartyLabel(target) + " — regular battle.");
                EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
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
            dataStore.SyncData("_playerAmbushCooldownUntil", ref _playerAmbushCooldownUntil);
        }
    }
}
