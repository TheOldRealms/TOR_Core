using System.Collections.Generic;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Phase F counterpart to <see cref="AmbushEndLogic"/>. Role-reversed:
    /// AttackerTeam is the AI ambusher force, DefenderTeam is the player.
    ///
    /// Priority order (first match wins):
    ///  1. Hero (MainAgent) inactive                                 → AmbushedHeroWounded
    ///  2. DefenderTeam (player) 0 active + reinforcements drained   → AmbushedWiped
    ///  3. AttackerTeam (AI) 0 active + reinforcements drained       → AmbushRepelled
    ///  4. Player across soft border for countdown seconds           → AmbushedFled
    ///
    /// Full design in TORTasks/ambush-feature/ai-ambushes-player-design.md § 3.
    /// </summary>
    public class AmbushedEndLogic : MissionLogic
    {
        private bool _outcomeDecided;
        private bool _deploymentFinished;
        private MissionResult _result;
        private TORMissionAmbushModel _model;
        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;

        private float _outsideBoundarySeconds;

        // Snapshot of MainParty's non-hero roster at mission start.
        // Captured in AfterStart (roster still untouched by combat),
        // handed to AmbushCampaignBehavior at OnEndMission for the Fled
        // flow's wound/remove logic. "Troops the player committed to this
        // ambush" — the thing we're trying to tax on vanish / double-flee.
        private Dictionary<CharacterObject, int> _ambushedRosterSnapshot;

        public override void AfterStart()
        {
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();

            _ambushedRosterSnapshot = new Dictionary<CharacterObject, int>();
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    var c = roster.GetCharacterAtIndex(i);
                    if (c == null || c.IsHero) continue;
                    int healthy = roster.GetElementNumber(i) - roster.GetElementWoundedNumber(i);
                    if (healthy <= 0) continue;
                    _ambushedRosterSnapshot[c] = healthy;
                }
            }
            TORCommon.Log(LogLevel.Info, "[AmbushedEndLogic] AfterStart: committed roster snapshot — "
                + _ambushedRosterSnapshot.Count + " non-hero troop types captured");
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            _deploymentFinished = true;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_outcomeDecided) return;
            // Same reasoning as Phase D: before deployment finishes the engine
            // reports 0 active humans while agents are still being spawned.
            if (!_deploymentFinished) return;

            var mainAgent = Mission.MainAgent;

            // Priority 1 — hero down.
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushedOutcome.AmbushedHeroWounded, successful: false,
                    reason: "hero inactive");
                return;
            }

            // Priority 2 — player (defender) team wiped. Needs the
            // reinforcement queue drained too, mirroring Phase D's logic:
            // don't call it a defeat while queued waves are seconds away.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending(BattleSideEnum.Defender))
            {
                SetOutcome(AmbushedOutcome.AmbushedWiped, successful: false,
                    reason: "defender (player) wiped + reinforcement queue drained");
                return;
            }

            // Priority 3 — AI ambusher team wiped. AI sits on AttackerTeam
            // (matches the ambusher = Attacker invariant). AI reinforcement
            // is deferred (design doc § 4) but the check reads IsSideDepleted
            // anyway so the gate stays honest when the model lands later.
            if (CountActiveHumans(Mission.AttackerTeam) == 0
                && !AreReinforcementsStillPending(BattleSideEnum.Attacker))
            {
                SetOutcome(AmbushedOutcome.AmbushRepelled, successful: true,
                    reason: "attacker (AI) wiped + reinforcement queue drained");
                return;
            }

            // Priority 4 — soft border crossed. Accumulate time while the
            // hero's position is outside the mission boundary; reset to 0 the
            // moment they step back in. Fire the outcome when the countdown
            // expires. We only engage the countdown after the hero is spawned
            // (MainAgent != null) so a null main agent during the first tick
            // doesn't count against us.
            if (mainAgent != null)
            {
                if (!Mission.IsPositionInsideBoundaries(mainAgent.Position.AsVec2))
                    _outsideBoundarySeconds += dt;
                else
                    _outsideBoundarySeconds = 0f;

                if (_outsideBoundarySeconds >= _model.AmbushedFledCountdownSeconds)
                {
                    SetOutcome(AmbushedOutcome.AmbushedFled, successful: true,
                        reason: "soft border countdown elapsed ("
                                + _model.AmbushedFledCountdownSeconds + "s)");
                    return;
                }
            }
        }

        private bool AreReinforcementsStillPending(BattleSideEnum side)
        {
            if (_spawnLogic == null) return false;
            return !_spawnLogic.IsSideDepleted(side);
        }

        private static int CountActiveHumans(Team team)
        {
            if (team == null) return 0;
            int n = 0;
            foreach (var a in team.ActiveAgents)
                if (a != null && a.IsHuman && a.IsActive()) n++;
            return n;
        }

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_outcomeDecided) return false;
            missionResult = _result;
            return true;
        }

        /// <summary>
        /// AmbushRepelled is the only outcome that nudges the vanilla
        /// post-battle state machine — same pattern as Phase D's TotalVictory,
        /// since the player's party wins and should see loot / cleanup.
        /// AmbushedWiped goes through vanilla defeat; AmbushedHeroWounded and
        /// AmbushedFled leave PlayerEncounter alive so the campaign-side
        /// aftermath (reveal window, follow-up battle, wounded troops) can
        /// run.
        /// </summary>
        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            TORCommon.Log(LogLevel.Info, "[AmbushedEndLogic] OnMissionResultReady fired. missionResult="
                + (missionResult != null ? "ok" : "null"));
            if (missionResult == null) return;
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return;
            var outcome = behavior.LastAmbushedOutcome;
            TORCommon.Log(LogLevel.Info, "[AmbushedEndLogic] OnMissionResultReady outcome=" + outcome);

            if (outcome == AmbushedOutcome.AmbushRepelled)
            {
                // Nudges vanilla's victory flow → MapEvent closes
                // → OnPlayerBattleEnd fires → repelled record stamped.
                if (PlayerEncounter.Current != null)
                    PlayerEncounter.SetPlayerVictorious();
                behavior.PendingPostAmbushCleanup = true;
                return;
            }
            // AmbushedFled / AmbushedHeroWounded cleanup moved to OnEndMission
            // override — OnMissionResultReady doesn't fire reliably when the
            // mission ends via a path that bypasses MissionResultReady
            // (Mission.EndMission() direct call from retreat/surrender/leave-
            // mission-timer paths — see decompiled Mission.cs lines 2873,
            // 2889, 3876). OnEndMission is called from EndMissionInternal
            // which runs for every teardown path (verified Mission.cs:4658).
            // AmbushedWiped: MissionResult.CreateDefeated — vanilla's
            // defeat flow closes PlayerEncounter and fires
            // OnPlayerBattleEnd naturally.
        }

        private void SetOutcome(AmbushedOutcome outcome, bool successful, string reason)
        {
            _outcomeDecided = true;
            _result = successful
                ? MissionResult.CreateSuccessful(Mission)
                : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushedOutcome = outcome;
            TORCommon.Log(LogLevel.Info,
                "[AmbushedEndLogic] outcome=" + outcome + " successful=" + successful
                + " (" + reason + ")");
        }

        /// <summary>
        /// Guaranteed-fire teardown callback (verified via decompiled
        /// Mission.cs:4658 — <c>EndMissionInternal</c> iterates every
        /// MissionBehavior and calls <c>OnEndMissionInternal</c>, which
        /// dispatches here). Runs regardless of whether the mission ended
        /// via our <c>MissionEnded</c> dispatch (which calls
        /// <c>OnMissionResultReady</c>) or vanilla's direct <c>EndMission()</c>
        /// path (which does not, see Mission.cs:4637/2873/2889/3876).
        ///
        /// Does the Phase F §6 cleanup for Fled/HeroWounded:
        ///  - snapshots the spawned victim-side characters from the
        ///    controller (while Mission is still alive)
        ///  - calls PlayerEncounter.Finish(false) which handles the
        ///    teleport-out and SetDoNotAttackMainParty internally
        ///    (verified PlayerEncounter.cs:1072-1078).
        /// </summary>
        protected override void OnEndMission()
        {
            base.OnEndMission();
            if (!_outcomeDecided) return;

            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return;
            var outcome = behavior.LastAmbushedOutcome;
            if (outcome != AmbushedOutcome.AmbushedFled
                && outcome != AmbushedOutcome.AmbushedHeroWounded) return;

            // Capture ambusher ref BEFORE Finish — vanilla's
            // MapEvent.FinalizeEventAux (MapEvent.cs:1727) can call
            // DestroyPartyAction via HandleMapEventEndForPartyInternal
            // (MapEventSide.cs:447) when a party's roster collapsed in
            // combat. OnPartyRemoved then clears
            // AiAmbushBehavior.CurrentAmbusherOnPlayer, leaving us no ref
            // for the cooldown record.
            var aiBehavior = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            var ambusher = aiBehavior?.CurrentAmbusherOnPlayer;

            TORCommon.Log(LogLevel.Info, "[AmbushedEndLogic] OnEndMission stash+finish: outcome="
                + outcome
                + " committed-types=" + (_ambushedRosterSnapshot?.Count ?? -1)
                + " ambusher=" + (ambusher?.LeaderHero?.Name?.ToString()
                                   ?? ambusher?.Name?.ToString() ?? "?")
                + " PlayerEncounter.Current="
                + (PlayerEncounter.Current != null ? "ALIVE" : "null"));

            behavior.StashAmbushedRosterSnapshot(_ambushedRosterSnapshot);

            // Phase F cooldown — MainParty is VICTIM, not ambusher. Writes
            // the record BEFORE Finish so the pair incident is captured
            // even if the ambusher party gets destroyed during
            // FinalizeEventAux. OnPlayerBattleEndEvent doesn't fire for
            // this exit path (vanilla's Finish on a no-winner MapEvent
            // skips the battle-end dispatch), so the OnPlayerBattleEnd
            // cooldown block is dead for Fled — must stamp here or the
            // quarter-hourly AI tick re-ambushes seconds later.
            // See [[feedback_ambush_role_asymmetry]].
            if (aiBehavior != null)
            {
                aiBehavior.RecordAmbush(
                    ambusher: (ambusher != null && ambusher.IsActive) ? ambusher : null,
                    victim: MobileParty.MainParty,
                    repelled: false,
                    wasLoud: false);
            }

            if (PlayerEncounter.Current != null)
                PlayerEncounter.Finish(forcePlayerOutFromSettlement: false);
        }
    }
}
